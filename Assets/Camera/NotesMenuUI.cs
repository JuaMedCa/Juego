using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class NotesMenuUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject notesMenu;
    public CanvasGroup menuCanvasGroup;
    public RectTransform panelContainer;

    [Header("Configuracion")]
    public KeyCode toggleKey = KeyCode.Tab;
    public bool freezeGameWhenOpen = true;
    public bool showCursorWhenOpen = true;

    [Header("Animacion")]
    public float openDuration = 0.25f;
    public float closeDuration = 0.2f;
    public Vector3 closedScale = new Vector3(0.97f, 0.97f, 0.97f);
    public Vector3 openScale = Vector3.one;
    [SerializeField] private bool slideFromLeft = true;
    [SerializeField] private float slideOffset = 420f;
    [SerializeField] private Vector2 closedOffset = new Vector2(-420f, 0f);

    [Header("Inventario")]
    [SerializeField] private TMP_Text inventoryCounterText;
    [SerializeField] private TMP_Text inventorySummaryText;
    [SerializeField] private bool autoFindInventoryCounter = true;
    [SerializeField] private string emptyInventoryMessage = "Sin documentos";
    [SerializeField] private float notesFirstRowY = -170f;
    [SerializeField] private float notesRowSpacing = 120f;

    [Header("Tarjetas de notas")]
    [SerializeField] private Color discoveredSlotColor = new Color(0.18f, 0.23f, 0.30f, 0.95f);
    [SerializeField] private Color pendingSlotColor = new Color(0.18f, 0.16f, 0.14f, 0.82f);
    [SerializeField] private Color discoveredAccentColor = new Color(0.82f, 0.74f, 0.55f, 1f);
    [SerializeField] private Color pendingAccentColor = new Color(0.45f, 0.41f, 0.36f, 1f);
    [SerializeField] private Color discoveredTextColor = new Color(0.95f, 0.92f, 0.86f, 1f);
    [SerializeField] private Color pendingTextColor = new Color(0.67f, 0.64f, 0.59f, 1f);
    [SerializeField] private Color tabBackgroundColor = new Color(0.69f, 0.58f, 0.35f, 0.92f);
    [SerializeField] private Color tabTextColor = new Color(0.10f, 0.09f, 0.08f, 1f);

    [Header("Estilo Archivo")]
    [SerializeField] private Color menuBackdropColor = new Color(0.01f, 0.02f, 0.03f, 0.76f);
    [SerializeField] private Color menuBackdropGlowColor = new Color(0.28f, 0.08f, 0.04f, 0.08f);
    [SerializeField] private Color panelBackgroundColor = new Color(0.05f, 0.06f, 0.08f, 0.95f);
    [SerializeField] private Color panelBorderColor = new Color(0.76f, 0.69f, 0.49f, 0.20f);
    [SerializeField] private Color panelShadowColor = new Color(0f, 0f, 0f, 0.60f);
    [SerializeField] private Color archiveAccentColor = new Color(0.69f, 0.58f, 0.35f, 0.92f);
    [SerializeField] private Color sectionRuleColor = new Color(0.72f, 0.65f, 0.49f, 0.20f);
    [SerializeField] private Color counterCardColor = new Color(0.10f, 0.12f, 0.16f, 0.98f);
    [SerializeField] private Color counterLabelColor = new Color(0.82f, 0.77f, 0.64f, 0.78f);
    [SerializeField] private Color closeHintColor = new Color(0.78f, 0.74f, 0.66f, 0.68f);
    [SerializeField] private Color slotBorderColor = new Color(0.76f, 0.69f, 0.49f, 0.12f);
    [SerializeField] private Color slotShadowColor = new Color(0f, 0f, 0f, 0.42f);
    [SerializeField] private Color discoveredBadgeColor = new Color(0.74f, 0.62f, 0.36f, 0.96f);
    [SerializeField] private Color pendingBadgeColor = new Color(0.34f, 0.31f, 0.28f, 0.92f);
    [SerializeField] private Color badgeTextColor = new Color(0.07f, 0.06f, 0.05f, 1f);
    [SerializeField] private Color slotIndexColor = new Color(0.78f, 0.74f, 0.64f, 0.48f);
    [SerializeField] private Vector2 slotCardSize = new Vector2(240f, 108f);

    private bool isOpen = false;
    private bool isAnimating = false;
    private Coroutine animationCoroutine;
    private readonly List<NoteSlotView> noteSlots = new List<NoteSlotView>();
    private Vector2 openAnchoredPosition;
    private Vector2 hiddenAnchoredPosition;
    private RectTransform titleRect;
    private RectTransform notesGridRect;
    private TMP_Text titleText;
    private TMP_Text tabLabelText;
    private TMP_Text menuInventoryCardText;
    private GridLayoutGroup notesGridLayout;

    private void Awake()
    {
        InventoryManager.EnsureInstance();
        ResolveInventoryCounter();
        ResolveHeaderElements();
        ResolveNoteSlots();
        BuildArchiveFrame();
        CachePanelPositions();
        BuildHeaderTab();
        LayoutNoteSlots();
    }

    private void OnEnable()
    {
        InventoryManager.EnsureInstance().InventoryChanged += RefreshInventoryText;
        RefreshInventoryText();
    }

    private void OnDisable()
    {
        if (InventoryManager.HasInstance)
        {
            InventoryManager.Instance.InventoryChanged -= RefreshInventoryText;
        }
    }

    private void Start()
    {
        ImmediateCloseState();
        RefreshInventoryText();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey) && !isAnimating)
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        if (isOpen)
            CloseMenu();
        else
            OpenMenu();
    }

    public void OpenMenu()
    {
        if (notesMenu == null || menuCanvasGroup == null || panelContainer == null) return;

        ResolveInventoryCounter();
        RefreshInventoryText();

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(OpenMenuRoutine());
    }

    public void CloseMenu()
    {
        if (notesMenu == null || menuCanvasGroup == null || panelContainer == null) return;

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(CloseMenuRoutine());
    }

    private IEnumerator OpenMenuRoutine()
    {
        isAnimating = true;
        isOpen = true;

        notesMenu.SetActive(true);
        ObjectiveSystem.EnsureInstance().SetHudVisible(false);

        if (freezeGameWhenOpen)
            Time.timeScale = 0f;

        if (showCursorWhenOpen)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        float time = 0f;

        menuCanvasGroup.alpha = 0f;
        panelContainer.localScale = closedScale;
        panelContainer.anchoredPosition = hiddenAnchoredPosition;

        while (time < openDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / openDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            menuCanvasGroup.alpha = Mathf.Lerp(0f, 1f, eased);
            panelContainer.localScale = Vector3.Lerp(closedScale, openScale, eased);
            panelContainer.anchoredPosition = Vector2.Lerp(hiddenAnchoredPosition, openAnchoredPosition, eased);

            yield return null;
        }

        menuCanvasGroup.alpha = 1f;
        panelContainer.localScale = openScale;
        panelContainer.anchoredPosition = openAnchoredPosition;

        isAnimating = false;
        animationCoroutine = null;
    }

    private IEnumerator CloseMenuRoutine()
    {
        isAnimating = true;
        isOpen = false;

        float time = 0f;

        menuCanvasGroup.alpha = 1f;
        panelContainer.localScale = openScale;
        panelContainer.anchoredPosition = openAnchoredPosition;

        while (time < closeDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / closeDuration);
            float eased = t * t;

            menuCanvasGroup.alpha = Mathf.Lerp(1f, 0f, eased);
            panelContainer.localScale = Vector3.Lerp(openScale, closedScale, eased);
            panelContainer.anchoredPosition = Vector2.Lerp(openAnchoredPosition, hiddenAnchoredPosition, eased);

            yield return null;
        }

        menuCanvasGroup.alpha = 0f;
        panelContainer.localScale = closedScale;
        panelContainer.anchoredPosition = hiddenAnchoredPosition;

        notesMenu.SetActive(false);
        ObjectiveSystem.EnsureInstance().SetHudVisible(true);

        if (freezeGameWhenOpen)
            Time.timeScale = 1f;

        if (showCursorWhenOpen)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        isAnimating = false;
        animationCoroutine = null;
    }

    private void ImmediateCloseState()
    {
        isOpen = false;
        isAnimating = false;

        if (menuCanvasGroup != null)
            menuCanvasGroup.alpha = 0f;

        if (panelContainer != null)
        {
            panelContainer.localScale = closedScale;
            panelContainer.anchoredPosition = hiddenAnchoredPosition;
        }

        if (notesMenu != null)
            notesMenu.SetActive(false);

        ObjectiveSystem.EnsureInstance().SetHudVisible(true);
        Time.timeScale = 1f;
    }

    private void RefreshInventoryText()
    {
        if (inventoryCounterText == null)
        {
            ResolveInventoryCounter();
        }

        if (!InventoryManager.HasInstance)
        {
            if (inventoryCounterText != null)
            {
                inventoryCounterText.text = emptyInventoryMessage;
            }

            UpdateMenuInventoryCard("--/--");
            if (inventorySummaryText != null)
            {
                inventorySummaryText.text = emptyInventoryMessage;
            }

            RefreshNoteSlots(null);
            return;
        }

        InventoryManager inventory = InventoryManager.Instance;
        if (inventoryCounterText != null)
        {
            inventoryCounterText.text = $"ARCHIVO\n<size=40>{inventory.CollectedNotesCount}/{inventory.TotalRegisteredNotes}</size> documentos";
            inventoryCounterText.enableWordWrapping = true;
            inventoryCounterText.alignment = TextAlignmentOptions.TopLeft;
        }

        UpdateMenuInventoryCard($"{inventory.CollectedNotesCount:00}/{inventory.TotalRegisteredNotes:00}");

        if (inventorySummaryText != null)
        {
            inventorySummaryText.text = inventory.GetNotesDetailedSummary(emptyInventoryMessage);
        }

        RefreshNoteSlots(inventory.GetOrderedNotes());
    }

    private void ResolveInventoryCounter()
    {
        if ((!autoFindInventoryCounter) && inventoryCounterText != null && inventorySummaryText != null)
        {
            return;
        }

        TMP_Text[] textComponents = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < textComponents.Length; i++)
        {
            if (textComponents[i].name == "Txt_NotasContador")
            {
                inventoryCounterText = textComponents[i];
            }

            if (textComponents[i].name == "Txt_Subtitulo")
            {
                inventorySummaryText = textComponents[i];
            }

        }
    }

    private void ResolveHeaderElements()
    {
        notesGridRect = FindNamedRectTransform("NotesGrid");
        if (notesGridRect != null)
        {
            notesGridLayout = notesGridRect.GetComponent<GridLayoutGroup>();
        }

        TMP_Text[] textComponents = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < textComponents.Length; i++)
        {
            if (textComponents[i].name == "Txt_Titulo")
            {
                titleText = textComponents[i];
                titleRect = textComponents[i].rectTransform;
            }
        }
    }

    private void ResolveNoteSlots()
    {
        noteSlots.Clear();

        RectTransform[] rectTransforms = GetComponentsInChildren<RectTransform>(true);
        List<RectTransform> slotRoots = new List<RectTransform>();

        for (int i = 0; i < rectTransforms.Length; i++)
        {
            if (rectTransforms[i].name.StartsWith("NoteSlot_"))
            {
                slotRoots.Add(rectTransforms[i]);
            }
        }

        slotRoots.Sort((left, right) => string.CompareOrdinal(left.name, right.name));

        for (int i = 0; i < slotRoots.Count; i++)
        {
            noteSlots.Add(BuildSlotView(slotRoots[i], i + 1));
        }
    }

    private NoteSlotView BuildSlotView(RectTransform slotRoot, int slotNumber)
    {
        Image background = slotRoot.GetComponent<Image>();
        TMP_Text title = FindText(slotRoot, "SlotTitle");
        TMP_Text status = FindText(slotRoot, "SlotStatus");
        Image accent = EnsureImage(slotRoot, "SlotAccent");
        Image badgeBackground = EnsureImage(slotRoot, "SlotBadge");
        TMP_Text badgeText = EnsureText(badgeBackground.rectTransform, "SlotBadgeText");
        TMP_Text indexText = EnsureText(slotRoot, "SlotIndex");

        StyleSlotChrome(slotRoot, background, accent, badgeBackground, badgeText, indexText);

        if (title == null)
        {
            title = CreateSlotText("SlotTitle", slotRoot, 18f, 16f, 18f, 46f, 22, FontStyles.Bold);
        }

        if (status == null)
        {
            status = CreateSlotText("SlotStatus", slotRoot, 18f, 52f, 18f, 16f, 16, FontStyles.Normal);
        }

        ConfigureSlotText(title, 24f, 18f, 112f, 52f, 21f, FontStyles.Bold, discoveredTextColor);
        ConfigureSlotText(status, 24f, 54f, 18f, 16f, 15f, FontStyles.Normal, discoveredAccentColor);

        return new NoteSlotView(slotNumber, slotRoot, background, accent, badgeBackground, badgeText, indexText, title, status, slotRoot.anchoredPosition, slotRoot.sizeDelta);
    }

    private TMP_Text FindText(RectTransform root, string objectName)
    {
        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].name == objectName)
            {
                return texts[i];
            }
        }

        return null;
    }

    private TMP_Text CreateSlotText(string objectName, RectTransform parent, float left, float top, float right, float bottom, float fontSize, FontStyles style)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.layer = parent.gameObject.layer;

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(left, bottom);
        rectTransform.offsetMax = new Vector2(-right, -top);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.enableWordWrapping = true;
        text.alignment = TextAlignmentOptions.TopLeft;

        return text;
    }

    private void CachePanelPositions()
    {
        if (panelContainer == null)
        {
            return;
        }

        openAnchoredPosition = panelContainer.anchoredPosition;
        hiddenAnchoredPosition = slideFromLeft
            ? openAnchoredPosition + new Vector2(-Mathf.Abs(slideOffset), 0f)
            : openAnchoredPosition + closedOffset;
    }

    private void BuildArchiveFrame()
    {
        if (notesMenu == null || panelContainer == null)
        {
            return;
        }

        RectTransform menuRect = notesMenu.GetComponent<RectTransform>();
        StretchRect(menuRect);

        RectTransform backdropRect = FindNamedRectTransform("Panel_Background");
        if (backdropRect != null)
        {
            StretchRect(backdropRect);
            Image backdropImage = GetOrAddComponent<Image>(backdropRect.gameObject);
            backdropImage.color = menuBackdropColor;
            backdropImage.raycastTarget = true;
        }

        Image backdropGlow = EnsureImage(notesMenu.transform as RectTransform, "BackdropGlow");
        StretchRect(backdropGlow.rectTransform);
        backdropGlow.color = menuBackdropGlowColor;
        backdropGlow.rectTransform.SetSiblingIndex(1);
        backdropGlow.raycastTarget = false;

        panelContainer.anchorMin = panelContainer.anchorMax = new Vector2(0f, 0.5f);
        panelContainer.pivot = new Vector2(0f, 0.5f);
        panelContainer.anchoredPosition = new Vector2(48f, 0f);
        panelContainer.sizeDelta = new Vector2(620f, 736f);

        Image panelImage = GetOrAddComponent<Image>(panelContainer.gameObject);
        panelImage.color = panelBackgroundColor;

        Outline panelOutline = GetOrAddComponent<Outline>(panelContainer.gameObject);
        panelOutline.effectColor = panelBorderColor;
        panelOutline.effectDistance = new Vector2(1.5f, -1.5f);

        Shadow panelShadow = GetOrAddComponent<Shadow>(panelContainer.gameObject);
        panelShadow.effectColor = panelShadowColor;
        panelShadow.effectDistance = new Vector2(12f, -12f);

        Image leftAccent = EnsureImage(panelContainer, "ArchiveLeftAccent");
        ConfigureVisualRect(leftAccent.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(10f, 0f));
        leftAccent.color = archiveAccentColor;
        leftAccent.raycastTarget = false;

        Image headerGlow = EnsureImage(panelContainer, "ArchiveHeaderGlow");
        ConfigureVisualRect(headerGlow.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(-20f, 88f));
        headerGlow.color = new Color(1f, 1f, 1f, 0.03f);
        headerGlow.raycastTarget = false;

        Image divider = EnsureImage(panelContainer, "ArchiveDivider");
        ConfigureVisualRect(divider.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -166f), new Vector2(-56f, 2f));
        divider.color = sectionRuleColor;
        divider.raycastTarget = false;

        TMP_Text sectionLabel = EnsureText(panelContainer, "ArchiveSectionLabel");
        ConfigureTextRect(sectionLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -188f), new Vector2(260f, 24f));
        sectionLabel.text = "REGISTROS RECUPERADOS";
        sectionLabel.fontSize = 14f;
        sectionLabel.fontStyle = FontStyles.Bold;
        sectionLabel.characterSpacing = 3f;
        sectionLabel.alignment = TextAlignmentOptions.Left;
        sectionLabel.color = counterLabelColor;
        sectionLabel.enableWordWrapping = false;

        RectTransform counterCard = EnsureRectTransform(panelContainer, "ArchiveCounterCard");
        ConfigureVisualRect(counterCard, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -38f), new Vector2(146f, 78f));
        Image counterCardImage = GetOrAddComponent<Image>(counterCard.gameObject);
        counterCardImage.color = counterCardColor;
        counterCardImage.raycastTarget = false;

        Outline counterOutline = GetOrAddComponent<Outline>(counterCard.gameObject);
        counterOutline.effectColor = panelBorderColor;
        counterOutline.effectDistance = new Vector2(1f, -1f);

        Shadow counterShadow = GetOrAddComponent<Shadow>(counterCard.gameObject);
        counterShadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        counterShadow.effectDistance = new Vector2(6f, -6f);

        TMP_Text counterLabel = EnsureText(counterCard, "ArchiveCounterLabel");
        ConfigureTextRect(counterLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(-18f, 18f));
        counterLabel.text = "DOCUMENTOS";
        counterLabel.fontSize = 11f;
        counterLabel.fontStyle = FontStyles.Bold;
        counterLabel.characterSpacing = 2.2f;
        counterLabel.alignment = TextAlignmentOptions.Center;
        counterLabel.color = counterLabelColor;
        counterLabel.enableWordWrapping = false;

        menuInventoryCardText = EnsureText(counterCard, "ArchiveCounterValue");
        ConfigureTextRect(menuInventoryCardText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(-18f, -22f));
        menuInventoryCardText.fontSize = 28f;
        menuInventoryCardText.fontStyle = FontStyles.Bold;
        menuInventoryCardText.alignment = TextAlignmentOptions.Center;
        menuInventoryCardText.color = discoveredAccentColor;
        menuInventoryCardText.enableWordWrapping = false;

        TMP_Text closeHintText = EnsureText(panelContainer, "ArchiveCloseHint");
        ConfigureTextRect(closeHintText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 22f), new Vector2(180f, 20f));
        closeHintText.text = "TAB cerrar archivo";
        closeHintText.fontSize = 15f;
        closeHintText.fontStyle = FontStyles.Italic;
        closeHintText.alignment = TextAlignmentOptions.Right;
        closeHintText.color = closeHintColor;
        closeHintText.enableWordWrapping = false;

        if (notesGridRect != null)
        {
            notesGridRect.anchorMin = Vector2.zero;
            notesGridRect.anchorMax = Vector2.one;
            notesGridRect.pivot = new Vector2(0.5f, 0.5f);
            notesGridRect.offsetMin = new Vector2(28f, 70f);
            notesGridRect.offsetMax = new Vector2(-28f, -226f);
        }

        if (notesGridLayout != null)
        {
            notesGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            notesGridLayout.constraintCount = 1;
            notesGridLayout.spacing = new Vector2(0f, 14f);
            notesGridLayout.padding = new RectOffset(0, 0, 0, 0);
        }

        if (Mathf.Approximately(notesFirstRowY, -170f))
        {
            notesFirstRowY = -50f;
        }

        if (Mathf.Approximately(notesRowSpacing, 120f))
        {
            notesRowSpacing = 118f;
        }

        if (slotCardSize.x < 400f)
        {
            slotCardSize = new Vector2(564f, 96f);
        }
    }

    private void BuildHeaderTab()
    {
        if (panelContainer == null)
        {
            return;
        }

        RectTransform tabRoot = FindNamedRectTransform("Txt_TabHint");
        if (tabRoot == null)
        {
            return;
        }

        tabRoot.anchorMin = new Vector2(0f, 1f);
        tabRoot.anchorMax = new Vector2(0f, 1f);
        tabRoot.pivot = new Vector2(0f, 1f);
        tabRoot.anchoredPosition = new Vector2(28f, -26f);
        tabRoot.sizeDelta = new Vector2(168f, 38f);

        Image background = tabRoot.GetComponent<Image>();
        if (background == null)
        {
            background = tabRoot.gameObject.AddComponent<Image>();
        }

        background.color = tabBackgroundColor;
        background.raycastTarget = false;

        Outline outline = GetOrAddComponent<Outline>(tabRoot.gameObject);
        outline.effectColor = new Color(0f, 0f, 0f, 0.18f);
        outline.effectDistance = new Vector2(1f, -1f);

        tabLabelText = FindText(tabRoot, "TabLabel");
        if (tabLabelText == null)
        {
            tabLabelText = CreateSlotText("TabLabel", tabRoot, 14f, 7f, 14f, 6f, 18, FontStyles.Bold);
        }

        tabLabelText.text = "ARCHIVO";
        tabLabelText.color = tabTextColor;
        tabLabelText.alignment = TextAlignmentOptions.Center;
        tabLabelText.enableWordWrapping = false;
        tabLabelText.characterSpacing = 4f;
        tabLabelText.fontSize = 16f;

        if (titleRect != null)
        {
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(28f, -74f);
            titleRect.sizeDelta = new Vector2(-214f, 42f);
        }

        if (titleText != null)
        {
            titleText.text = "Registro de notas";
            titleText.fontSize = 31;
            titleText.characterSpacing = 3f;
            titleText.alignment = TextAlignmentOptions.Left;
            titleText.color = discoveredTextColor;
        }

        if (inventorySummaryText != null)
        {
            RectTransform summaryRect = inventorySummaryText.rectTransform;
            summaryRect.anchorMin = new Vector2(0f, 1f);
            summaryRect.anchorMax = new Vector2(1f, 1f);
            summaryRect.pivot = new Vector2(0f, 1f);
            summaryRect.anchoredPosition = new Vector2(28f, -118f);
            summaryRect.sizeDelta = new Vector2(-214f, 34f);
            inventorySummaryText.alignment = TextAlignmentOptions.Left;
            inventorySummaryText.fontSize = 15;
            inventorySummaryText.color = new Color(0.72f, 0.71f, 0.67f, 1f);
            inventorySummaryText.enableWordWrapping = false;
            inventorySummaryText.overflowMode = TextOverflowModes.Ellipsis;
        }

        LayoutNoteSlots();
    }

    private RectTransform FindNamedRectTransform(string objectName)
    {
        RectTransform[] rectTransforms = GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rectTransforms.Length; i++)
        {
            if (rectTransforms[i].name == objectName)
            {
                return rectTransforms[i];
            }
        }

        return null;
    }

    private void RefreshNoteSlots(List<InventoryManager.NoteSnapshot> orderedNotes)
    {
        if (noteSlots.Count == 0)
        {
            ResolveNoteSlots();
            LayoutNoteSlots();
        }

        for (int i = 0; i < noteSlots.Count; i++)
        {
            if (orderedNotes != null && i < orderedNotes.Count)
            {
                ApplyNoteToSlot(noteSlots[i], orderedNotes[i]);
            }
            else
            {
                ApplyEmptyState(noteSlots[i], i + 1);
            }
        }
    }

    private void ApplyNoteToSlot(NoteSlotView slot, InventoryManager.NoteSnapshot note)
    {
        if (slot.Background != null)
        {
            slot.Background.color = note.Collected ? discoveredSlotColor : pendingSlotColor;
        }

        if (slot.Accent != null)
        {
            slot.Accent.color = note.Collected ? discoveredBadgeColor : pendingBadgeColor;
        }

        if (slot.BadgeBackground != null)
        {
            slot.BadgeBackground.color = note.Collected ? discoveredBadgeColor : pendingBadgeColor;
        }

        if (slot.BadgeText != null)
        {
            slot.BadgeText.text = note.Collected ? "RECUPERADO" : "PENDIENTE";
            slot.BadgeText.color = note.Collected ? badgeTextColor : discoveredTextColor;
        }

        slot.Title.color = note.Collected ? discoveredTextColor : pendingTextColor;
        slot.Status.color = note.Collected ? discoveredAccentColor : pendingAccentColor;
        slot.Title.text = note.DisplayName;
        slot.Status.text = note.Collected ? note.FullText : "Pendiente por inspeccionar";
        slot.Status.enableWordWrapping = true;
        slot.Status.overflowMode = TextOverflowModes.Ellipsis;
        slot.Status.maxVisibleLines = 2;
        slot.Title.enableWordWrapping = false;
        slot.Title.overflowMode = TextOverflowModes.Ellipsis;

        if (slot.IndexText != null)
        {
            slot.IndexText.text = $"#{slot.SlotNumber:00}";
            slot.IndexText.color = slotIndexColor;
        }
    }

    private void ApplyEmptyState(NoteSlotView slot, int index)
    {
        if (slot.Background != null)
        {
            slot.Background.color = pendingSlotColor;
        }

        if (slot.Accent != null)
        {
            slot.Accent.color = pendingBadgeColor;
        }

        if (slot.BadgeBackground != null)
        {
            slot.BadgeBackground.color = pendingBadgeColor;
        }

        if (slot.BadgeText != null)
        {
            slot.BadgeText.text = "VACIO";
            slot.BadgeText.color = discoveredTextColor;
        }

        slot.Title.color = pendingTextColor;
        slot.Status.color = pendingAccentColor;
        slot.Title.text = $"Expediente {index:00}";
        slot.Status.text = "Sin registro";

        if (slot.IndexText != null)
        {
            slot.IndexText.text = $"#{index:00}";
            slot.IndexText.color = slotIndexColor;
        }
    }

    private void LayoutNoteSlots()
    {
        if (notesGridLayout != null)
        {
            notesGridLayout.cellSize = slotCardSize;
        }

        if (noteSlots.Count == 0)
        {
            return;
        }

        if (notesGridLayout != null)
        {
            for (int i = 0; i < noteSlots.Count; i++)
            {
                noteSlots[i].Root.sizeDelta = slotCardSize;
            }

            return;
        }

        for (int i = 0; i < noteSlots.Count; i++)
        {
            NoteSlotView slot = noteSlots[i];
            int row = i / 2;
            Vector2 anchoredPosition = slot.OriginalAnchoredPosition;
            anchoredPosition.y = notesFirstRowY - (row * notesRowSpacing);
            slot.Root.anchoredPosition = anchoredPosition;
            slot.Root.sizeDelta = slotCardSize;
        }
    }

    private void StyleSlotChrome(RectTransform slotRoot, Image background, Image accent, Image badgeBackground, TMP_Text badgeText, TMP_Text indexText)
    {
        if (background == null)
        {
            background = slotRoot.gameObject.AddComponent<Image>();
        }

        background.color = pendingSlotColor;
        background.raycastTarget = false;

        Outline outline = GetOrAddComponent<Outline>(slotRoot.gameObject);
        outline.effectColor = slotBorderColor;
        outline.effectDistance = new Vector2(1f, -1f);

        Shadow shadow = GetOrAddComponent<Shadow>(slotRoot.gameObject);
        shadow.effectColor = slotShadowColor;
        shadow.effectDistance = new Vector2(5f, -5f);

        ConfigureVisualRect(accent.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(8f, 0f));
        accent.color = pendingBadgeColor;
        accent.raycastTarget = false;

        ConfigureVisualRect(badgeBackground.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -12f), new Vector2(102f, 24f));
        badgeBackground.color = pendingBadgeColor;
        badgeBackground.raycastTarget = false;

        ConfigureTextRect(badgeText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-16f, -8f));
        badgeText.fontSize = 12f;
        badgeText.fontStyle = FontStyles.Bold;
        badgeText.characterSpacing = 1.6f;
        badgeText.alignment = TextAlignmentOptions.Center;
        badgeText.enableWordWrapping = false;

        ConfigureTextRect(indexText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-16f, 12f), new Vector2(46f, 16f));
        indexText.fontSize = 13f;
        indexText.fontStyle = FontStyles.Bold;
        indexText.characterSpacing = 2f;
        indexText.alignment = TextAlignmentOptions.Right;
        indexText.enableWordWrapping = false;
    }

    private void ConfigureSlotText(TMP_Text text, float left, float top, float right, float bottom, float fontSize, FontStyles style, Color color)
    {
        ConfigureTextRect(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-left - right, -top - bottom));
        text.rectTransform.offsetMin = new Vector2(left, bottom);
        text.rectTransform.offsetMax = new Vector2(-right, -top);
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.raycastTarget = false;
    }

    private void UpdateMenuInventoryCard(string value)
    {
        if (menuInventoryCardText != null)
        {
            menuInventoryCardText.text = value;
        }
    }

    private RectTransform EnsureRectTransform(Transform parent, string objectName)
    {
        Transform child = parent.Find(objectName);
        if (child != null)
        {
            return child as RectTransform;
        }

        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private Image EnsureImage(Transform parent, string objectName)
    {
        RectTransform rect = EnsureRectTransform(parent, objectName);
        Image image = rect.GetComponent<Image>();
        if (image == null)
        {
            image = rect.gameObject.AddComponent<Image>();
        }

        return image;
    }

    private TMP_Text EnsureText(Transform parent, string objectName)
    {
        RectTransform rect = EnsureRectTransform(parent, objectName);
        TextMeshProUGUI text = rect.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        }

        text.raycastTarget = false;
        return text;
    }

    private void StretchRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    private void ConfigureVisualRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private void ConfigureTextRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        if (component == null)
        {
            component = go.AddComponent<T>();
        }

        return component;
    }

    private struct NoteSlotView
    {
        public readonly int SlotNumber;
        public readonly RectTransform Root;
        public readonly Image Background;
        public readonly Image Accent;
        public readonly Image BadgeBackground;
        public readonly TMP_Text BadgeText;
        public readonly TMP_Text IndexText;
        public readonly TMP_Text Title;
        public readonly TMP_Text Status;
        public readonly Vector2 OriginalAnchoredPosition;
        public readonly Vector2 OriginalSizeDelta;

        public NoteSlotView(int slotNumber, RectTransform root, Image background, Image accent, Image badgeBackground, TMP_Text badgeText, TMP_Text indexText, TMP_Text title, TMP_Text status, Vector2 originalAnchoredPosition, Vector2 originalSizeDelta)
        {
            SlotNumber = slotNumber;
            Root = root;
            Background = background;
            Accent = accent;
            BadgeBackground = badgeBackground;
            BadgeText = badgeText;
            IndexText = indexText;
            Title = title;
            Status = status;
            OriginalAnchoredPosition = originalAnchoredPosition;
            OriginalSizeDelta = originalSizeDelta;
        }
    }
}
