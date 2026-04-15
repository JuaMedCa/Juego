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
    [SerializeField] private int notesPerPage = 4;
    [SerializeField] private Color pageTabActiveColor = new Color(0.69f, 0.58f, 0.35f, 0.96f);
    [SerializeField] private Color pageTabInactiveColor = new Color(0.16f, 0.17f, 0.20f, 0.96f);
    [SerializeField] private Color detailPanelColor = new Color(0.08f, 0.09f, 0.12f, 0.98f);
    [SerializeField] private Color detailBackdropColor = new Color(0.01f, 0.02f, 0.04f, 0.72f);

    private bool isOpen = false;
    private bool isAnimating = false;
    private Coroutine animationCoroutine;
    private readonly List<NoteSlotView> noteSlots = new List<NoteSlotView>();
    private readonly List<InventoryManager.NoteSnapshot> cachedNotes = new List<InventoryManager.NoteSnapshot>();
    private readonly List<PageTabView> pageTabs = new List<PageTabView>();
    private Vector2 openAnchoredPosition;
    private Vector2 hiddenAnchoredPosition;
    private RectTransform titleRect;
    private RectTransform notesGridRect;
    private TMP_Text titleText;
    private TMP_Text tabLabelText;
    private TMP_Text menuInventoryCardText;
    private GridLayoutGroup notesGridLayout;
    private RectTransform pageTabsContainer;
    private GameObject noteReaderOverlay;
    private TMP_Text noteReaderTitle;
    private TMP_Text noteReaderBody;
    private ScrollRect noteReaderScrollRect;
    private LayoutElement noteReaderBodyLayoutElement;
    private int currentPageIndex;

    private void Awake()
    {
        InventoryManager.EnsureInstance();
        ResolveInventoryCounter();
        ResolveHeaderElements();
        ResolveNoteSlots();
        BuildArchiveFrame();
        CachePanelPositions();
        BuildHeaderTab();
        BuildPageTabsContainer();
        BuildNoteReaderOverlay();
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

        CloseNoteReader();
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

        CloseNoteReader();
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
            noteSlots.Add(BuildSlotView(slotRoots[i], i, i + 1));
        }
    }

    private NoteSlotView BuildSlotView(RectTransform slotRoot, int slotIndex, int slotNumber)
    {
        Image background = slotRoot.GetComponent<Image>();
        TMP_Text title = FindText(slotRoot, "SlotTitle");
        TMP_Text status = FindText(slotRoot, "SlotStatus");
        Image accent = EnsureImage(slotRoot, "SlotAccent");
        Image badgeBackground = EnsureImage(slotRoot, "SlotBadge");
        TMP_Text badgeText = EnsureText(badgeBackground.rectTransform, "SlotBadgeText");
        TMP_Text indexText = EnsureText(slotRoot, "SlotIndex");
        Button button = GetOrAddComponent<Button>(slotRoot.gameObject);

        StyleSlotChrome(slotRoot, background, accent, badgeBackground, badgeText, indexText);
        ConfigureSlotButton(button, background, slotIndex);

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

        return new NoteSlotView(slotNumber, slotRoot, background, accent, badgeBackground, badgeText, indexText, title, status, button, slotRoot.anchoredPosition, slotRoot.sizeDelta);
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

        cachedNotes.Clear();
        if (orderedNotes != null)
        {
            cachedNotes.AddRange(orderedNotes);
        }

        int totalPages = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(1, cachedNotes.Count) / (float)Mathf.Max(1, notesPerPage)));
        currentPageIndex = Mathf.Clamp(currentPageIndex, 0, totalPages - 1);
        UpdatePageTabs(totalPages);

        int pageStartIndex = currentPageIndex * Mathf.Max(1, notesPerPage);
        for (int i = 0; i < noteSlots.Count; i++)
        {
            int noteIndex = pageStartIndex + i;
            if (noteIndex >= 0 && noteIndex < cachedNotes.Count)
            {
                ApplyNoteToSlot(noteSlots[i], cachedNotes[noteIndex]);
            }
            else
            {
                ApplyEmptyState(noteSlots[i], noteIndex + 1);
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
        slot.Status.text = note.Collected ? note.PreviewText : "Pendiente por inspeccionar";
        slot.Status.enableWordWrapping = true;
        slot.Status.overflowMode = TextOverflowModes.Ellipsis;
        slot.Status.maxVisibleLines = 2;
        slot.Title.enableWordWrapping = false;
        slot.Title.overflowMode = TextOverflowModes.Ellipsis;
        if (slot.Button != null)
        {
            slot.Button.interactable = note.Collected;
        }

        if (slot.IndexText != null)
        {
            slot.IndexText.text = $"#{note.Order:00}";
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
        if (slot.Button != null)
        {
            slot.Button.interactable = false;
        }

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

    private void OnNoteSlotClicked(int slotIndex)
    {
        int absoluteIndex = currentPageIndex * Mathf.Max(1, notesPerPage) + slotIndex;
        if (absoluteIndex < 0 || absoluteIndex >= cachedNotes.Count)
        {
            return;
        }

        InventoryManager.NoteSnapshot note = cachedNotes[absoluteIndex];
        if (!note.Collected)
        {
            return;
        }

        OpenNoteReader(note);
    }

    private void BuildPageTabsContainer()
    {
        if (panelContainer == null)
        {
            return;
        }

        pageTabsContainer = EnsureRectTransform(panelContainer, "ArchivePageTabs");
        ConfigureVisualRect(pageTabsContainer, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(28f, 22f), new Vector2(220f, 30f));

        HorizontalLayoutGroup layoutGroup = GetOrAddComponent<HorizontalLayoutGroup>(pageTabsContainer.gameObject);
        layoutGroup.childAlignment = TextAnchor.MiddleLeft;
        layoutGroup.spacing = 8f;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;

        ContentSizeFitter fitter = GetOrAddComponent<ContentSizeFitter>(pageTabsContainer.gameObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void UpdatePageTabs(int totalPages)
    {
        if (pageTabsContainer == null)
        {
            BuildPageTabsContainer();
        }

        if (pageTabsContainer == null)
        {
            return;
        }

        totalPages = Mathf.Max(1, totalPages);

        while (pageTabs.Count < totalPages)
        {
            int pageIndex = pageTabs.Count;
            pageTabs.Add(CreatePageTab(pageIndex));
        }

        for (int i = 0; i < pageTabs.Count; i++)
        {
            bool visible = i < totalPages;
            pageTabs[i].Root.gameObject.SetActive(visible);
            if (!visible)
            {
                continue;
            }

            bool isActive = i == currentPageIndex;
            pageTabs[i].Background.color = isActive ? pageTabActiveColor : pageTabInactiveColor;
            pageTabs[i].Label.text = $"{i + 1:00}";
            pageTabs[i].Label.color = isActive ? tabTextColor : discoveredTextColor;
        }

        pageTabsContainer.gameObject.SetActive(totalPages > 1);
    }

    private PageTabView CreatePageTab(int pageIndex)
    {
        RectTransform root = EnsureRectTransform(pageTabsContainer, $"PageTab_{pageIndex + 1:00}");
        root.sizeDelta = new Vector2(42f, 28f);

        Image background = GetOrAddComponent<Image>(root.gameObject);
        background.color = pageTabInactiveColor;
        background.raycastTarget = true;

        Outline outline = GetOrAddComponent<Outline>(root.gameObject);
        outline.effectColor = new Color(0f, 0f, 0f, 0.18f);
        outline.effectDistance = new Vector2(1f, -1f);

        Button button = GetOrAddComponent<Button>(root.gameObject);
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.95f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 0.85f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.RemoveAllListeners();
        int capturedPageIndex = pageIndex;
        button.onClick.AddListener(() => SetCurrentPage(capturedPageIndex));

        TMP_Text label = EnsureText(root, "PageTabLabel");
        ConfigureTextRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        label.fontSize = 15f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = false;

        return new PageTabView(root, background, label, button);
    }

    private void SetCurrentPage(int pageIndex)
    {
        currentPageIndex = Mathf.Max(0, pageIndex);
        CloseNoteReader();
        RefreshNoteSlots(new List<InventoryManager.NoteSnapshot>(cachedNotes));
    }

    private void BuildNoteReaderOverlay()
    {
        if (panelContainer == null || noteReaderOverlay != null)
        {
            return;
        }

        Color parchmentColor = new Color(0.92f, 0.87f, 0.73f, 0.98f);
        Color parchmentShadowColor = new Color(0f, 0f, 0f, 0.35f);
        Color inkColor = new Color(0.16f, 0.12f, 0.08f, 1f);
        Color accentInkColor = new Color(0.46f, 0.31f, 0.16f, 0.86f);

        RectTransform overlayRoot = EnsureRectTransform(panelContainer, "NoteReaderOverlay");
        StretchRect(overlayRoot);
        overlayRoot.SetAsLastSibling();

        Image overlayImage = GetOrAddComponent<Image>(overlayRoot.gameObject);
        overlayImage.color = detailBackdropColor;
        overlayImage.raycastTarget = true;
        noteReaderOverlay = overlayRoot.gameObject;

        RectTransform detailCard = EnsureRectTransform(overlayRoot, "NoteReaderCard");
        ConfigureVisualRect(detailCard, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 760f));
        Image detailCardImage = GetOrAddComponent<Image>(detailCard.gameObject);
        detailCardImage.color = parchmentColor;
        detailCardImage.raycastTarget = true;

        Outline detailOutline = GetOrAddComponent<Outline>(detailCard.gameObject);
        detailOutline.effectColor = new Color(0.33f, 0.23f, 0.12f, 0.34f);
        detailOutline.effectDistance = new Vector2(1.5f, -1.5f);

        Shadow detailShadow = GetOrAddComponent<Shadow>(detailCard.gameObject);
        detailShadow.effectColor = parchmentShadowColor;
        detailShadow.effectDistance = new Vector2(10f, -10f);

        RectTransform edgeTop = EnsureRectTransform(detailCard, "NoteReaderEdgeTop");
        ConfigureVisualRect(edgeTop, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(-30f, 26f));
        Image edgeTopImage = GetOrAddComponent<Image>(edgeTop.gameObject);
        edgeTopImage.color = new Color(0.63f, 0.48f, 0.25f, 0.14f);
        edgeTopImage.raycastTarget = false;

        RectTransform edgeBottom = EnsureRectTransform(detailCard, "NoteReaderEdgeBottom");
        ConfigureVisualRect(edgeBottom, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(-42f, 34f));
        Image edgeBottomImage = GetOrAddComponent<Image>(edgeBottom.gameObject);
        edgeBottomImage.color = new Color(0.39f, 0.27f, 0.14f, 0.15f);
        edgeBottomImage.raycastTarget = false;

        RectTransform sideShadeLeft = EnsureRectTransform(detailCard, "NoteReaderSideShadeLeft");
        ConfigureVisualRect(sideShadeLeft, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(28f, -70f));
        Image sideShadeLeftImage = GetOrAddComponent<Image>(sideShadeLeft.gameObject);
        sideShadeLeftImage.color = new Color(0.34f, 0.23f, 0.11f, 0.08f);
        sideShadeLeftImage.raycastTarget = false;

        RectTransform sideShadeRight = EnsureRectTransform(detailCard, "NoteReaderSideShadeRight");
        ConfigureVisualRect(sideShadeRight, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(30f, -90f));
        Image sideShadeRightImage = GetOrAddComponent<Image>(sideShadeRight.gameObject);
        sideShadeRightImage.color = new Color(0.28f, 0.19f, 0.09f, 0.10f);
        sideShadeRightImage.raycastTarget = false;

        RectTransform centerCrease = EnsureRectTransform(detailCard, "NoteReaderCenterCrease");
        ConfigureVisualRect(centerCrease, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(2f, -142f));
        Image centerCreaseImage = GetOrAddComponent<Image>(centerCrease.gameObject);
        centerCreaseImage.color = new Color(0.39f, 0.28f, 0.16f, 0.08f);
        centerCreaseImage.raycastTarget = false;

        TMP_Text archiveLabel = EnsureText(detailCard, "NoteReaderArchiveLabel");
        ConfigureTextRect(archiveLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(-120f, 18f));
        archiveLabel.text = "ARCHIVO RECUPERADO";
        archiveLabel.fontSize = 13f;
        archiveLabel.fontStyle = FontStyles.Bold;
        archiveLabel.characterSpacing = 4.4f;
        archiveLabel.alignment = TextAlignmentOptions.Center;
        archiveLabel.color = accentInkColor;
        archiveLabel.enableWordWrapping = false;

        noteReaderTitle = EnsureText(detailCard, "NoteReaderTitle");
        ConfigureTextRect(noteReaderTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(-132f, 48f));
        noteReaderTitle.fontSize = 32f;
        noteReaderTitle.fontStyle = FontStyles.Bold | FontStyles.SmallCaps;
        noteReaderTitle.alignment = TextAlignmentOptions.Center;
        noteReaderTitle.color = inkColor;
        noteReaderTitle.enableWordWrapping = true;
        noteReaderTitle.characterSpacing = 2f;

        RectTransform divider = EnsureRectTransform(detailCard, "NoteReaderDivider");
        ConfigureVisualRect(divider, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -118f), new Vector2(-104f, 2f));
        Image dividerImage = GetOrAddComponent<Image>(divider.gameObject);
        dividerImage.color = new Color(accentInkColor.r, accentInkColor.g, accentInkColor.b, 0.32f);
        dividerImage.raycastTarget = false;

        RectTransform closeButtonRect = EnsureRectTransform(detailCard, "NoteReaderCloseButton");
        ConfigureVisualRect(closeButtonRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -18f), new Vector2(34f, 34f));
        Image closeButtonImage = GetOrAddComponent<Image>(closeButtonRect.gameObject);
        closeButtonImage.color = new Color(0.34f, 0.21f, 0.12f, 0.88f);
        closeButtonImage.raycastTarget = true;

        Button closeButton = GetOrAddComponent<Button>(closeButtonRect.gameObject);
        closeButton.targetGraphic = closeButtonImage;
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(CloseNoteReader);

        TMP_Text closeLabel = EnsureText(closeButtonRect, "Label");
        ConfigureTextRect(closeLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        closeLabel.text = "X";
        closeLabel.fontSize = 20f;
        closeLabel.fontStyle = FontStyles.Bold;
        closeLabel.alignment = TextAlignmentOptions.Center;
        closeLabel.color = new Color(0.95f, 0.91f, 0.83f, 1f);
        closeLabel.enableWordWrapping = false;

        RectTransform scrollRoot = EnsureRectTransform(detailCard, "NoteReaderScroll");
        ConfigureVisualRect(scrollRoot, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), new Vector2(-86f, -186f));
        noteReaderScrollRect = GetOrAddComponent<ScrollRect>(scrollRoot.gameObject);
        noteReaderScrollRect.horizontal = false;
        noteReaderScrollRect.movementType = ScrollRect.MovementType.Clamped;
        noteReaderScrollRect.scrollSensitivity = 24f;

        RectTransform viewport = EnsureRectTransform(scrollRoot, "Viewport");
        StretchRect(viewport);
        Image viewportImage = GetOrAddComponent<Image>(viewport.gameObject);
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        viewportImage.raycastTarget = true;
        Mask viewportMask = GetOrAddComponent<Mask>(viewport.gameObject);
        viewportMask.showMaskGraphic = false;

        RectTransform content = EnsureRectTransform(viewport, "Content");
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(-18f, 0f);

        VerticalLayoutGroup contentLayout = GetOrAddComponent<VerticalLayoutGroup>(content.gameObject);
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.padding = new RectOffset(0, 0, 0, 8);
        contentLayout.spacing = 0;

        ContentSizeFitter contentSizeFitter = GetOrAddComponent<ContentSizeFitter>(content.gameObject);
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        noteReaderBody = EnsureText(content, "Body");
        noteReaderBody.rectTransform.anchorMin = new Vector2(0f, 1f);
        noteReaderBody.rectTransform.anchorMax = new Vector2(1f, 1f);
        noteReaderBody.rectTransform.pivot = new Vector2(0.5f, 1f);
        noteReaderBody.rectTransform.anchoredPosition = Vector2.zero;
        noteReaderBody.rectTransform.sizeDelta = new Vector2(0f, 0f);
        noteReaderBody.fontSize = 20f;
        noteReaderBody.fontStyle = FontStyles.Normal;
        noteReaderBody.alignment = TextAlignmentOptions.TopLeft;
        noteReaderBody.color = inkColor;
        noteReaderBody.enableWordWrapping = true;
        noteReaderBody.overflowMode = TextOverflowModes.Overflow;
        noteReaderBody.characterSpacing = 0.45f;
        noteReaderBody.lineSpacing = 12f;
        noteReaderBody.paragraphSpacing = 14f;
        noteReaderBody.margin = new Vector4(26f, 18f, 32f, 28f);

        noteReaderBodyLayoutElement = GetOrAddComponent<LayoutElement>(noteReaderBody.gameObject);
        noteReaderBodyLayoutElement.minHeight = 0f;
        noteReaderBodyLayoutElement.flexibleHeight = 0f;

        RectTransform scrollbarRect = EnsureRectTransform(scrollRoot, "Scrollbar");
        ConfigureVisualRect(scrollbarRect, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(14f, -8f));
        Image scrollbarTrack = GetOrAddComponent<Image>(scrollbarRect.gameObject);
        scrollbarTrack.color = new Color(0.28f, 0.20f, 0.11f, 0.18f);
        scrollbarTrack.raycastTarget = true;

        RectTransform slidingArea = EnsureRectTransform(scrollbarRect, "SlidingArea");
        StretchRect(slidingArea);
        slidingArea.offsetMin = new Vector2(0f, 8f);
        slidingArea.offsetMax = new Vector2(0f, -8f);

        RectTransform handleRect = EnsureRectTransform(slidingArea, "Handle");
        ConfigureVisualRect(handleRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 72f));
        Image handleImage = GetOrAddComponent<Image>(handleRect.gameObject);
        handleImage.color = new Color(0.42f, 0.28f, 0.15f, 0.88f);
        handleImage.raycastTarget = true;

        Scrollbar scrollbar = GetOrAddComponent<Scrollbar>(scrollbarRect.gameObject);
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleImage;
        scrollbar.size = 0.25f;

        noteReaderScrollRect.viewport = viewport;
        noteReaderScrollRect.content = content;
        noteReaderScrollRect.verticalScrollbar = scrollbar;
        noteReaderScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        noteReaderScrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        noteReaderOverlay.SetActive(false);
    }

    private void OpenNoteReader(InventoryManager.NoteSnapshot note)
    {
        if (noteReaderOverlay == null)
        {
            BuildNoteReaderOverlay();
        }

        if (noteReaderOverlay == null || noteReaderTitle == null || noteReaderBody == null)
        {
            return;
        }

        noteReaderTitle.text = note.DisplayName;
        noteReaderBody.text = note.FullText;
        noteReaderOverlay.SetActive(true);
        noteReaderOverlay.transform.SetAsLastSibling();
        RefreshNoteReaderLayout();
        Canvas.ForceUpdateCanvases();
        if (noteReaderScrollRect != null)
        {
            noteReaderScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void CloseNoteReader()
    {
        if (noteReaderOverlay != null)
        {
            noteReaderOverlay.SetActive(false);
        }
    }

    private void RefreshNoteReaderLayout()
    {
        if (noteReaderBody == null || noteReaderBodyLayoutElement == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();

        RectTransform bodyRect = noteReaderBody.rectTransform;
        float availableWidth = Mathf.Max(260f, bodyRect.rect.width - noteReaderBody.margin.x - noteReaderBody.margin.z);
        float preferredHeight = noteReaderBody.GetPreferredValues(noteReaderBody.text, availableWidth, 0f).y + noteReaderBody.margin.y + noteReaderBody.margin.w + 20f;

        noteReaderBodyLayoutElement.preferredHeight = Mathf.Max(340f, preferredHeight);
        LayoutRebuilder.ForceRebuildLayoutImmediate(bodyRect);

        if (noteReaderScrollRect != null && noteReaderScrollRect.content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(noteReaderScrollRect.content);
        }
    }

    private void StyleSlotChrome(RectTransform slotRoot, Image background, Image accent, Image badgeBackground, TMP_Text badgeText, TMP_Text indexText)
    {
        if (background == null)
        {
            background = slotRoot.gameObject.AddComponent<Image>();
        }

        background.color = pendingSlotColor;
        background.raycastTarget = true;

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

    private void ConfigureSlotButton(Button button, Image background, int slotIndex)
    {
        if (button == null)
        {
            return;
        }

        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.95f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.92f, 0.92f, 0.92f, 0.9f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0.5f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        button.onClick.RemoveAllListeners();
        int capturedIndex = slotIndex;
        button.onClick.AddListener(() => OnNoteSlotClicked(capturedIndex));
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
        public readonly Button Button;
        public readonly Vector2 OriginalAnchoredPosition;
        public readonly Vector2 OriginalSizeDelta;

        public NoteSlotView(int slotNumber, RectTransform root, Image background, Image accent, Image badgeBackground, TMP_Text badgeText, TMP_Text indexText, TMP_Text title, TMP_Text status, Button button, Vector2 originalAnchoredPosition, Vector2 originalSizeDelta)
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
            Button = button;
            OriginalAnchoredPosition = originalAnchoredPosition;
            OriginalSizeDelta = originalSizeDelta;
        }
    }

    private struct PageTabView
    {
        public readonly RectTransform Root;
        public readonly Image Background;
        public readonly TMP_Text Label;
        public readonly Button Button;

        public PageTabView(RectTransform root, Image background, TMP_Text label, Button button)
        {
            Root = root;
            Background = background;
            Label = label;
            Button = button;
        }
    }
}
