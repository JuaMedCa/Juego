using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObjectiveSystem : MonoBehaviour
{
    private static ObjectiveSystem instance;
    public event System.Action ObjectivesChanged;

    public static bool HasInstance => instance != null;

    public static ObjectiveSystem Instance
    {
        get
        {
            if (instance == null)
            {
                EnsureInstance();
            }

            return instance;
        }
    }

    public static ObjectiveSystem EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindObjectOfType<ObjectiveSystem>();
        if (instance == null)
        {
            GameObject systemObject = new GameObject("ObjectiveSystem");
            instance = systemObject.AddComponent<ObjectiveSystem>();
        }

        return instance;
    }

    [Header("Configuracion")]
    [SerializeField] private int totalFuelRequired = 7;
    [SerializeField] private int totalNotesTarget = 7;
    [SerializeField] private bool unlockHouseObjectiveOnMapPickup = true;
    [SerializeField] private bool unlockHouseObjectiveOnFirstNote = true;

    [Header("Textos")]
    [SerializeField] private string objectiveHeader = "OBJETIVOS";
    [SerializeField] private string fuelObjectiveLabel = "Encuentra gasolina";
    [SerializeField] private string houseObjectiveLabel = "Ve a la casa";
    [SerializeField] private string returnObjectiveLabel = "Regresa a la gasolinera";
    [SerializeField] private string notesObjectiveLabel = "Lee documentos";

    private readonly HashSet<string> collectedFuelIds = new HashSet<string>();
    private readonly HashSet<string> readNoteIds = new HashSet<string>();

    private bool mapCollected;
    private bool houseObjectiveUnlocked;
    private bool returnObjectiveUnlocked;

    private GameObject hudRoot;
    private Canvas hudCanvas;
    private TMP_Text objectiveText;
    private Image objectivePanel;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureHud();
        RefreshObjectiveText();
    }

    public void RegisterFuelPickup(string fuelId)
    {
        string normalizedId = NormalizeId(fuelId);
        if (string.IsNullOrWhiteSpace(normalizedId))
        {
            return;
        }

        if (collectedFuelIds.Add(normalizedId) && collectedFuelIds.Count >= totalFuelRequired)
        {
            returnObjectiveUnlocked = true;
        }

        RefreshObjectiveText();
    }

    public void RegisterMapPickup()
    {
        mapCollected = true;

        if (unlockHouseObjectiveOnMapPickup)
        {
            houseObjectiveUnlocked = true;
        }

        RefreshObjectiveText();
    }

    public void RegisterNoteRead(string noteId)
    {
        string normalizedId = NormalizeId(noteId);
        if (string.IsNullOrWhiteSpace(normalizedId))
        {
            return;
        }

        if (readNoteIds.Add(normalizedId) && unlockHouseObjectiveOnFirstNote)
        {
            houseObjectiveUnlocked = true;
        }

        RefreshObjectiveText();
    }

    public int FuelCollectedCount => collectedFuelIds.Count;
    public int NotesReadCount => readNoteIds.Count;
    public bool HasCollectedMap => mapCollected;
    public bool IsHouseObjectiveUnlocked => houseObjectiveUnlocked;
    public bool IsReturnObjectiveUnlocked => returnObjectiveUnlocked;

    public void SetHudVisible(bool visible)
    {
        EnsureHud();

        if (hudRoot != null)
        {
            hudRoot.SetActive(visible);
        }
    }

    private void EnsureHud()
    {
        if (objectiveText != null)
        {
            return;
        }

        GameObject existingHud = GameObject.Find("ObjectiveCanvas");
        if (existingHud != null)
        {
            hudRoot = existingHud;
            hudCanvas = existingHud.GetComponent<Canvas>();
        }
        else
        {
            GameObject canvasObject = new GameObject("ObjectiveCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            hudRoot = canvasObject;
            hudCanvas = canvasObject.GetComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudCanvas.sortingOrder = 25;

            DontDestroyOnLoad(canvasObject);

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        RectTransform panelRoot = FindOrCreateChildRect("ObjectivePanel", hudCanvas.transform);
        panelRoot.anchorMin = new Vector2(0f, 1f);
        panelRoot.anchorMax = new Vector2(0f, 1f);
        panelRoot.pivot = new Vector2(0f, 1f);
        panelRoot.anchoredPosition = new Vector2(28f, -96f);
        panelRoot.sizeDelta = new Vector2(360f, 180f);

        objectivePanel = panelRoot.GetComponent<Image>();
        if (objectivePanel == null)
        {
            objectivePanel = panelRoot.gameObject.AddComponent<Image>();
        }

        objectivePanel.color = new Color(0.02f, 0.03f, 0.06f, 0.48f);
        objectivePanel.raycastTarget = false;

        RectTransform textRoot = FindOrCreateChildRect("Txt_Objectives", panelRoot);
        textRoot.anchorMin = Vector2.zero;
        textRoot.anchorMax = Vector2.one;
        textRoot.offsetMin = new Vector2(18f, 16f);
        textRoot.offsetMax = new Vector2(-18f, -16f);

        objectiveText = textRoot.GetComponent<TextMeshProUGUI>();
        if (objectiveText == null)
        {
            objectiveText = textRoot.gameObject.AddComponent<TextMeshProUGUI>();
        }

        objectiveText.raycastTarget = false;
        objectiveText.fontSize = 24f;
        objectiveText.color = new Color(0.94f, 0.92f, 0.86f, 1f);
        objectiveText.alignment = TextAlignmentOptions.TopLeft;
        objectiveText.enableWordWrapping = true;

        SetHudVisible(true);
    }

    private RectTransform FindOrCreateChildRect(string objectName, Transform parent)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            return existing as RectTransform;
        }

        GameObject child = new GameObject(objectName, typeof(RectTransform));
        child.layer = parent.gameObject.layer;
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private void RefreshObjectiveText()
    {
        EnsureHud();

        if (objectiveText == null)
        {
            return;
        }

        List<string> hudLines = new List<string>();
        hudLines.Add($"<b>{objectiveHeader}</b>");
        hudLines.Add(string.Empty);
        hudLines.AddRange(GetActiveObjectiveLines());

        objectiveText.text = string.Join("\n", hudLines);
        ObjectivesChanged?.Invoke();
    }

    public List<string> GetActiveObjectiveLines()
    {
        List<string> lines = new List<string>();
        lines.Add($"{fuelObjectiveLabel} ({FuelCollectedCount}/{totalFuelRequired})");

        if (!mapCollected)
        {
            lines.Add("Recoge el mapa");
        }

        if (houseObjectiveUnlocked)
        {
            lines.Add(houseObjectiveLabel);
        }

        if (returnObjectiveUnlocked)
        {
            lines.Add(returnObjectiveLabel);
        }

        if (TotalNotesTargetVisible())
        {
            lines.Add($"{notesObjectiveLabel} ({NotesReadCount}/{totalNotesTarget})");
        }

        return lines;
    }

    public string GetObjectiveSummary(string emptyMessage = "Sin objetivos activos")
    {
        List<string> lines = GetActiveObjectiveLines();
        if (lines.Count == 0)
        {
            return emptyMessage;
        }

        return string.Join("\n", lines);
    }

    private bool TotalNotesTargetVisible()
    {
        return NotesReadCount > 0 || totalNotesTarget > 0;
    }

    private static string NormalizeId(string rawId)
    {
        return string.IsNullOrWhiteSpace(rawId) ? string.Empty : rawId.Trim().ToLowerInvariant();
    }
}
