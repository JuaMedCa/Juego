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

    private bool isOpen = false;
    private bool isAnimating = false;
    private Coroutine animationCoroutine;
    private readonly List<NoteSlotView> noteSlots = new List<NoteSlotView>();
    private Vector2 openAnchoredPosition;
    private Vector2 hiddenAnchoredPosition;
    private RectTransform titleRect;
    private TMP_Text titleText;
    private TMP_Text tabLabelText;

    private void Awake()
    {
        InventoryManager.EnsureInstance();
        CachePanelPositions();
        ResolveInventoryCounter();
        ResolveHeaderElements();
        ResolveNoteSlots();
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

        if (inventoryCounterText == null)
        {
            return;
        }

        if (!InventoryManager.HasInstance)
        {
            inventoryCounterText.text = emptyInventoryMessage;
            if (inventorySummaryText != null)
            {
                inventorySummaryText.text = emptyInventoryMessage;
            }

            RefreshNoteSlots(null);
            return;
        }

        InventoryManager inventory = InventoryManager.Instance;
        inventoryCounterText.text = $"ARCHIVO\n<size=40>{inventory.CollectedNotesCount}/{inventory.TotalRegisteredNotes}</size> documentos";
        inventoryCounterText.enableWordWrapping = true;
        inventoryCounterText.alignment = TextAlignmentOptions.TopLeft;

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
            noteSlots.Add(BuildSlotView(slotRoots[i]));
        }
    }

    private NoteSlotView BuildSlotView(RectTransform slotRoot)
    {
        Image background = slotRoot.GetComponent<Image>();
        TMP_Text title = FindText(slotRoot, "SlotTitle");
        TMP_Text status = FindText(slotRoot, "SlotStatus");

        if (title == null)
        {
            title = CreateSlotText("SlotTitle", slotRoot, 18f, 16f, 18f, 46f, 22, FontStyles.Bold);
        }

        if (status == null)
        {
            status = CreateSlotText("SlotStatus", slotRoot, 18f, 52f, 18f, 16f, 16, FontStyles.Normal);
        }

        return new NoteSlotView(slotRoot, background, title, status, slotRoot.anchoredPosition, slotRoot.sizeDelta);
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
        tabRoot.sizeDelta = new Vector2(150f, 36f);

        Image background = tabRoot.GetComponent<Image>();
        if (background == null)
        {
            background = tabRoot.gameObject.AddComponent<Image>();
        }

        background.color = tabBackgroundColor;
        background.raycastTarget = false;

        tabLabelText = FindText(tabRoot, "TabLabel");
        if (tabLabelText == null)
        {
            tabLabelText = CreateSlotText("TabLabel", tabRoot, 14f, 7f, 14f, 6f, 18, FontStyles.Bold);
        }

        tabLabelText.text = "ARCHIVO";
        tabLabelText.color = tabTextColor;
        tabLabelText.alignment = TextAlignmentOptions.Center;
        tabLabelText.enableWordWrapping = false;

        if (titleRect != null)
        {
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(28f, -74f);
            titleRect.sizeDelta = new Vector2(-56f, 44f);
        }

        if (titleText != null)
        {
            titleText.text = "Registro de notas";
            titleText.fontSize = 28;
            titleText.characterSpacing = 1.5f;
            titleText.alignment = TextAlignmentOptions.Left;
        }

        if (inventorySummaryText != null)
        {
            RectTransform summaryRect = inventorySummaryText.rectTransform;
            summaryRect.anchorMin = new Vector2(0f, 1f);
            summaryRect.anchorMax = new Vector2(1f, 1f);
            summaryRect.pivot = new Vector2(0f, 1f);
            summaryRect.anchoredPosition = new Vector2(28f, -112f);
            summaryRect.sizeDelta = new Vector2(-56f, 28f);
            inventorySummaryText.alignment = TextAlignmentOptions.Left;
            inventorySummaryText.fontSize = 18;
            inventorySummaryText.color = new Color(0.72f, 0.71f, 0.67f, 1f);
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

        slot.Title.color = note.Collected ? discoveredTextColor : pendingTextColor;
        slot.Status.color = note.Collected ? discoveredAccentColor : pendingAccentColor;
        slot.Title.text = note.DisplayName;
        slot.Status.text = note.Collected ? note.FullText : "Pendiente por inspeccionar";
        slot.Status.enableWordWrapping = true;
        slot.Status.overflowMode = TextOverflowModes.Ellipsis;
    }

    private void ApplyEmptyState(NoteSlotView slot, int index)
    {
        if (slot.Background != null)
        {
            slot.Background.color = pendingSlotColor;
        }

        slot.Title.color = pendingTextColor;
        slot.Status.color = pendingAccentColor;
        slot.Title.text = $"Espacio {index:00}";
        slot.Status.text = "Sin registro";
    }

    private void LayoutNoteSlots()
    {
        if (noteSlots.Count == 0)
        {
            return;
        }

        for (int i = 0; i < noteSlots.Count; i++)
        {
            NoteSlotView slot = noteSlots[i];
            int row = i / 2;
            Vector2 anchoredPosition = slot.OriginalAnchoredPosition;
            anchoredPosition.y = notesFirstRowY - (row * notesRowSpacing);
            slot.Root.anchoredPosition = anchoredPosition;
            slot.Root.sizeDelta = slot.OriginalSizeDelta;
        }
    }

    private struct NoteSlotView
    {
        public readonly RectTransform Root;
        public readonly Image Background;
        public readonly TMP_Text Title;
        public readonly TMP_Text Status;
        public readonly Vector2 OriginalAnchoredPosition;
        public readonly Vector2 OriginalSizeDelta;

        public NoteSlotView(RectTransform root, Image background, TMP_Text title, TMP_Text status, Vector2 originalAnchoredPosition, Vector2 originalSizeDelta)
        {
            Root = root;
            Background = background;
            Title = title;
            Status = status;
            OriginalAnchoredPosition = originalAnchoredPosition;
            OriginalSizeDelta = originalSizeDelta;
        }
    }
}
