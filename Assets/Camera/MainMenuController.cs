using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class MainMenuController : MonoBehaviour
{
    [Header("Branding")]
    [SerializeField] private string gameTitle = "DARK FALL";
    [SerializeField] private string gameSubtitle = "NO DEBERÍAS ESTAR AQUÍ";
    [SerializeField] private Sprite logoSprite;

    [Header("Background")]
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private Color backgroundTint = new Color(1f, 1f, 1f, 0.16f);
    [SerializeField] private Color fallbackBackgroundColor = new Color(0.01f, 0.01f, 0.01f, 1f);
    [SerializeField] private Color fogColorA = new Color(0.02f, 0.02f, 0.02f, 0.50f);
    [SerializeField] private Color fogColorB = new Color(0.09f, 0.00f, 0.00f, 0.08f);
    [SerializeField] private Color vignetteColor = new Color(0f, 0f, 0f, 0.82f);

    [Header("Theme")]
    [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.55f);
    [SerializeField] private Color buttonNormalColor = new Color(1f, 1f, 1f, 0.03f);
    [SerializeField] private Color buttonHoverColor = new Color(0.35f, 0.03f, 0.03f, 0.16f);
    [SerializeField] private Color accentColor = new Color(0.55f, 0.02f, 0.02f, 0.90f);
    [SerializeField] private Color titleColor = new Color(0.86f, 0.86f, 0.84f, 1f);
    [SerializeField] private Color textColor = new Color(0.60f, 0.60f, 0.60f, 1f);
    [SerializeField] private Color faintTextColor = new Color(0.38f, 0.38f, 0.38f, 0.95f);
    [SerializeField] private Color lineColor = new Color(0.25f, 0.02f, 0.02f, 0.35f);

    [Header("Defaults")]
    [SerializeField] private float defaultMasterVolume = 0.8f;
    [SerializeField] private float defaultCameraSensitivity = 6f;

    [Header("Animation")]
    [SerializeField] private float introFadeDuration = 2.8f;
    [SerializeField] private float backgroundDriftAmount = 8f;
    [SerializeField] private float backgroundDriftSpeed = 0.08f;
    [SerializeField] private float titleBreathAmount = 4f;
    [SerializeField] private float titleBreathSpeed = 0.7f;
    [SerializeField] private float glitchChancePerSecond = 0.20f;
    [SerializeField] private float glitchDuration = 0.06f;
    [SerializeField] private float glitchIntensity = 10f;
    [SerializeField] private float flickerChancePerSecond = 0.07f;

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private GameObject menuRoot;
    private GameObject optionsPanel;
    private GameObject extrasPanel;

    private Text titleText;
    private Text subtitleText;
    private Text whisperText;
    private Text volumeValueText;
    private Text sensitivityValueText;
    private Text sceneLabelText;
    private Text aerialModeButtonText;
    private Text firstPersonModeButtonText;
    private Image aerialModeButtonImage;
    private Image firstPersonModeButtonImage;

    private PlayerMovemnt playerMovement;
    private IsoCameraOrbit isoCameraOrbit;

    private Font uiFont;
    private Sprite whiteSprite;
    private float masterVolume;

    private RectTransform backgroundRect;
    private Image fogA;
    private Image fogB;
    private Image flashImage;
    private RectTransform titleRect;
    private Vector2 titleBasePos;
    private RectTransform menuPanelRect;
    private Vector2 menuPanelBasePos;

    private readonly List<RectTransform> glitchTargets = new List<RectTransform>();
    private readonly Dictionary<RectTransform, Vector2> originalPositions = new Dictionary<RectTransform, Vector2>();

    private readonly string[] whisperLines =
    {
        "NO ESTÁS SOLO",
        "ALGO TE ESTÁ OBSERVANDO",
        "NO MIRES DETRÁS DE TI",
        "YA ES DEMASIADO TARDE",
        "ELLOS ESCUCHAN TODO",
        "NO DEBISTE ENTRAR",
        "SIGUE RESPIRANDO",
        "AÚN NO TE HA VISTO"
    };

    private Coroutine introRoutine;
    private Coroutine ambienceRoutine;
    private Coroutine glitchRoutine;
    private Coroutine flickerRoutine;
    private Coroutine whisperRoutine;
    private bool hasStartedGame;

    private void Awake()
    {
        uiFont = LoadUiFont();
        whiteSprite = CreateWhiteSprite();

        FindGameplayReferences();
        BuildMenu();
        InitializeValues();
        OpenMenu();

        introRoutine = StartCoroutine(PlayIntroFade());
        ambienceRoutine = StartCoroutine(AmbienceLoop());
        glitchRoutine = StartCoroutine(GlitchLoop());
        flickerRoutine = StartCoroutine(FlickerLoop());
        whisperRoutine = StartCoroutine(WhisperLoop());
    }

    private void Update()
    {
        HandleEscapeMenu();
        AnimateBackground();
        AnimateFog();
        AnimateTitle();
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    private void FindGameplayReferences()
    {
        playerMovement = FindObjectOfType<PlayerMovemnt>();
        isoCameraOrbit = FindObjectOfType<IsoCameraOrbit>();
    }

    private void InitializeValues()
    {
        masterVolume = Mathf.Clamp01(AudioListener.volume);
        if (Mathf.Approximately(masterVolume, 0f))
        {
            masterVolume = defaultMasterVolume;
            AudioListener.volume = masterVolume;
        }

        if (isoCameraOrbit != null)
        {
            isoCameraOrbit.MouseSensitivity = defaultCameraSensitivity;
        }

        ApplyCameraMode(CameraSwitchTrigger.CurrentMode);
        RefreshOptionLabels();
    }

    private void OpenMenu()
    {
        Time.timeScale = 0f;
        SetGameplayEnabled(false);

        if (menuRoot != null) menuRoot.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (extrasPanel != null) extrasPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        CameraSwitchTrigger.SetMenuOpen(true);

        RefreshSceneLabel();
    }

    private void StartGame()
    {
        hasStartedGame = true;
        CloseMenu();
    }

    private void CloseMenu()
    {
        Time.timeScale = 1f;
        SetGameplayEnabled(true);

        if (menuRoot != null) menuRoot.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (extrasPanel != null) extrasPanel.SetActive(false);

        CameraSwitchTrigger.SetMenuOpen(false);
        CameraSwitchTrigger.RefreshGlobalCameraState();
    }

    private void SetGameplayEnabled(bool enabled)
    {
        if (playerMovement != null) playerMovement.enabled = enabled;
        if (isoCameraOrbit != null) isoCameraOrbit.enabled = enabled;
    }

    private void ToggleOptions()
    {
        bool nextState = !optionsPanel.activeSelf;
        optionsPanel.SetActive(nextState);
        extrasPanel.SetActive(false);
        RefreshOptionLabels();
    }

    private void ToggleExtras()
    {
        bool nextState = !extrasPanel.activeSelf;
        extrasPanel.SetActive(nextState);
        optionsPanel.SetActive(false);
    }

    private void ChangeVolume(float delta)
    {
        masterVolume = Mathf.Clamp01(masterVolume + delta);
        AudioListener.volume = masterVolume;
        RefreshOptionLabels();
    }

    private void ChangeSensitivity(float delta)
    {
        if (isoCameraOrbit == null) return;

        isoCameraOrbit.MouseSensitivity = Mathf.Max(0.5f, isoCameraOrbit.MouseSensitivity + delta);
        RefreshOptionLabels();
    }

    private void RefreshOptionLabels()
    {
        if (volumeValueText != null)
            volumeValueText.text = Mathf.RoundToInt(masterVolume * 100f) + "%";

        if (sensitivityValueText != null)
            sensitivityValueText.text = isoCameraOrbit != null ? isoCameraOrbit.MouseSensitivity.ToString("0.0") : "--";

        RefreshCameraModeButtons();
    }

    private void RefreshSceneLabel()
    {
        if (sceneLabelText != null)
            sceneLabelText.text = "SEÑAL PERDIDA // " + SceneManager.GetActiveScene().name.ToUpperInvariant();
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void BuildMenu()
    {
        EnsureEventSystem();

        GameObject canvasObject = CreateUiObject("HorrorMenuCanvas", null);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        menuRoot = canvasObject;

        RectTransform rootRect = canvasObject.GetComponent<RectTransform>();
        StretchFull(rootRect);

        CreateBackground(canvasObject.transform);
        CreateFogOverlay(canvasObject.transform, fogColorA, "FogA", out fogA);
        CreateFogOverlay(canvasObject.transform, fogColorB, "FogB", out fogB);
        CreateNoiseLines(canvasObject.transform);
        CreateVignette(canvasObject.transform);
        CreateFlashOverlay(canvasObject.transform);

        GameObject centerPanel = CreatePanel("CenterPanel", canvasObject.transform, panelColor);
        menuPanelRect = centerPanel.GetComponent<RectTransform>();
        menuPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        menuPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        menuPanelRect.pivot = new Vector2(0.5f, 0.5f);
        menuPanelRect.sizeDelta = new Vector2(700f, 650f);
        menuPanelRect.anchoredPosition = new Vector2(0f, 20f);
        menuPanelBasePos = menuPanelRect.anchoredPosition;
        RegisterGlitchTarget(menuPanelRect);

        AddOutline(centerPanel, new Color(1f, 1f, 1f, 0.04f), new Vector2(1f, -1f));

        bool hasLogo = logoSprite != null;
        float subtitleY = hasLogo ? -330f : -270f;
        float separatorY = hasLogo ? -375f : -315f;
        float firstButtonY = hasLogo ? -440f : -380f;

        if (hasLogo)
        {
            titleRect = CreateLogo("Logo", centerPanel.transform, logoSprite, new Vector2(0f, -80f), new Vector2(220f, 220f));
            titleBasePos = titleRect.anchoredPosition;
        }
        else
        {
            titleText = CreateText(
                "Title",
                centerPanel.transform,
                gameTitle,
                68,
                FontStyle.Bold,
                titleColor,
                TextAnchor.MiddleCenter,
                new Vector2(0f, -210f),
                new Vector2(560f, 90f),
                true);

            titleRect = titleText.rectTransform;
            titleBasePos = titleRect.anchoredPosition;
            RegisterGlitchTarget(titleRect);
        }

        subtitleText = CreateText(
            "Subtitle",
            centerPanel.transform,
            gameSubtitle,
            18,
            FontStyle.Italic,
            accentColor,
            TextAnchor.MiddleCenter,
            new Vector2(0f, subtitleY),
            new Vector2(520f, 30f),
            false);

        CreateSeparator(centerPanel.transform, new Vector2(0f, separatorY), new Vector2(300f, 2f));

        CreateMenuButton("JUGAR", new Vector2(0f, firstButtonY), 30, centerPanel.transform, StartGame, true);
        CreateMenuButton("OPCIONES", new Vector2(0f, firstButtonY - 70f), 24, centerPanel.transform, ToggleOptions, false);
        CreateMenuButton("EXTRAS", new Vector2(0f, firstButtonY - 130f), 24, centerPanel.transform, ToggleExtras, false);
        CreateMenuButton("SALIR", new Vector2(0f, firstButtonY - 190f), 24, centerPanel.transform, QuitGame, false);

        whisperText = CreateText(
            "WhisperText",
            canvasObject.transform,
            "...",
            20,
            FontStyle.Italic,
            faintTextColor,
            TextAnchor.MiddleCenter,
            new Vector2(0f, -980f),
            new Vector2(800f, 40f),
            false);

        sceneLabelText = CreateText(
            "SceneLabel",
            canvasObject.transform,
            "",
            14,
            FontStyle.Normal,
            faintTextColor,
            TextAnchor.MiddleRight,
            new Vector2(-40f, -40f),
            new Vector2(400f, 24f),
            false);
        sceneLabelText.rectTransform.anchorMin = new Vector2(1f, 1f);
        sceneLabelText.rectTransform.anchorMax = new Vector2(1f, 1f);
        sceneLabelText.rectTransform.pivot = new Vector2(1f, 1f);

        optionsPanel = CreatePopupCard("OptionsPanel", canvasObject.transform, new Vector2(0.83f, 0.52f), new Vector2(430f, 340f));
        RegisterGlitchTarget(optionsPanel.GetComponent<RectTransform>());

        CreateText("OptionsTitle", optionsPanel.transform, "OPCIONES", 28, FontStyle.Bold, titleColor, TextAnchor.UpperLeft, new Vector2(24f, -22f), new Vector2(200f, 40f), false);
        CreateSeparator(optionsPanel.transform, new Vector2(0f, -65f), new Vector2(360f, 2f));
        volumeValueText = CreateStepperRow(optionsPanel.transform, "VOLUMEN", new Vector2(26f, -110f), () => ChangeVolume(-0.1f), () => ChangeVolume(0.1f));
        sensitivityValueText = CreateStepperRow(optionsPanel.transform, "SENSIBILIDAD", new Vector2(26f, -175f), () => ChangeSensitivity(-0.5f), () => ChangeSensitivity(0.5f));
        CreateText("CameraModeLabel", optionsPanel.transform, "MODO CAMARA", 17, FontStyle.Bold, textColor, TextAnchor.MiddleLeft, new Vector2(24f, -230f), new Vector2(220f, 30f), false);
        CreateModeOptionButton("AerialModeButton", optionsPanel.transform, "AEREA", new Vector2(24f, -270f), new Vector2(120f, 36f), () => ApplyCameraMode(CameraSwitchTrigger.CameraMode.Aerea), out aerialModeButtonImage, out aerialModeButtonText);
        CreateModeOptionButton("FirstPersonModeButton", optionsPanel.transform, "PRIMERA PERSONA", new Vector2(156f, -270f), new Vector2(200f, 36f), () => ApplyCameraMode(CameraSwitchTrigger.CameraMode.PrimeraPersona), out firstPersonModeButtonImage, out firstPersonModeButtonText);
        optionsPanel.SetActive(false);

        extrasPanel = CreatePopupCard("ExtrasPanel", canvasObject.transform, new Vector2(0.83f, 0.52f), new Vector2(470f, 320f));
        RegisterGlitchTarget(extrasPanel.GetComponent<RectTransform>());

        CreateText("ExtrasTitle", extrasPanel.transform, "EXTRAS", 28, FontStyle.Bold, titleColor, TextAnchor.UpperLeft, new Vector2(24f, -22f), new Vector2(200f, 40f), false);
        CreateSeparator(extrasPanel.transform, new Vector2(0f, -65f), new Vector2(390f, 2f));

        Text extrasBody = CreateText(
            "ExtrasBody",
            extrasPanel.transform,
            "OBJETIVO\n\nEncuentra una salida.\n\nRECUERDA\n\nNo todo lo que escuches es real.\nNo todo lo que veas está muerto.\n\nCONSEJO\n\nSi el silencio cambia... corre.",
            18,
            FontStyle.Normal,
            textColor,
            TextAnchor.UpperLeft,
            new Vector2(24f, -90f),
            new Vector2(390f, 210f),
            false);
        extrasBody.horizontalOverflow = HorizontalWrapMode.Wrap;
        extrasBody.verticalOverflow = VerticalWrapMode.Overflow;
        extrasPanel.SetActive(false);
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(eventSystemObject);
    }

    private void CreateBackground(Transform parent)
    {
        GameObject background = CreateUiObject("Background", parent);
        backgroundRect = background.GetComponent<RectTransform>();
        StretchFull(backgroundRect);

        Image image = background.AddComponent<Image>();
        image.preserveAspect = false;

        if (backgroundSprite != null)
        {
            image.sprite = backgroundSprite;
            image.color = backgroundTint;
        }
        else
        {
            image.sprite = whiteSprite;
            image.color = fallbackBackgroundColor;
        }
    }

    private void CreateFogOverlay(Transform parent, Color color, string name, out Image imageOut)
    {
        GameObject overlay = CreatePanel(name, parent, color);
        RectTransform rect = overlay.GetComponent<RectTransform>();
        StretchFull(rect);
        imageOut = overlay.GetComponent<Image>();
    }

    private void CreateFlashOverlay(Transform parent)
    {
        GameObject flash = CreatePanel("FlashOverlay", parent, new Color(0.35f, 0f, 0f, 0f));
        RectTransform rect = flash.GetComponent<RectTransform>();
        StretchFull(rect);
        flashImage = flash.GetComponent<Image>();
    }

    private void CreateNoiseLines(Transform parent)
    {
        for (int i = 0; i < 22; i++)
        {
            GameObject line = CreatePanel("Noise_" + i, parent, new Color(1f, 1f, 1f, Random.Range(0.004f, 0.015f)));
            RectTransform rect = line.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, Random.Range(0f, 1f));
            rect.anchorMax = new Vector2(1f, rect.anchorMin.y);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(0f, Random.Range(1f, 2f));
        }
    }

    private void CreateVignette(Transform parent)
    {
        CreateEdge("TopVignette", parent, new Vector2(0f, 0.80f), new Vector2(1f, 1f), new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, 0.45f));
        CreateEdge("BottomVignette", parent, new Vector2(0f, 0f), new Vector2(1f, 0.20f), new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, 0.65f));
        CreateEdge("LeftVignette", parent, new Vector2(0f, 0f), new Vector2(0.14f, 1f), new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, 0.45f));
        CreateEdge("RightVignette", parent, new Vector2(0.86f, 0f), new Vector2(1f, 1f), new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, 0.45f));
    }

    private void CreateEdge(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject edge = CreatePanel(name, parent, color);
        RectTransform rect = edge.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private GameObject CreatePopupCard(string name, Transform parent, Vector2 anchor, Vector2 size)
    {
        GameObject card = CreatePanel(name, parent, new Color(0f, 0f, 0f, 0.78f));
        RectTransform rect = card.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        AddOutline(card, lineColor, new Vector2(1f, -1f));
        Shadow shadow = card.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        shadow.effectDistance = new Vector2(0f, -6f);

        return card;
    }

    private void CreateSeparator(Transform parent, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject separator = CreatePanel("Separator", parent, lineColor);
        RectTransform rect = separator.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private Text CreateStepperRow(Transform parent, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onMinus, UnityEngine.Events.UnityAction onPlus)
    {
        CreateText(label + "Label", parent, label, 18, FontStyle.Bold, textColor, TextAnchor.MiddleLeft, anchoredPosition, new Vector2(180f, 30f), false);
        CreateCompactButton("-", parent, anchoredPosition + new Vector2(200f, 0f), onMinus);
        Text valueText = CreateText(label + "Value", parent, "", 18, FontStyle.Bold, titleColor, TextAnchor.MiddleCenter, anchoredPosition + new Vector2(260f, 0f), new Vector2(80f, 30f), false);
        CreateCompactButton("+", parent, anchoredPosition + new Vector2(330f, 0f), onPlus);
        return valueText;
    }

    private void CreateModeOptionButton(string name, Transform parent, string label, Vector2 anchoredPosition, Vector2 size, UnityEngine.Events.UnityAction action, out Image buttonImage, out Text buttonText)
    {
        GameObject buttonObject = CreateUiObject(name, parent);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.sprite = whiteSprite;
        buttonImage.color = buttonNormalColor;

        AddOutline(buttonObject, new Color(1f, 1f, 1f, 0.05f), new Vector2(1f, -1f));

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(action);

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.95f, 0.95f, 1f);
        colors.pressedColor = new Color(0.85f, 0.80f, 0.80f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        buttonText = CreateText(name + "Text", buttonObject.transform, label, 15, FontStyle.Bold, titleColor, TextAnchor.MiddleCenter, Vector2.zero, rect.sizeDelta, false);
    }

    private void CreateCompactButton(string label, Transform parent, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = CreateUiObject(label + "Button", parent);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(42f, 34f);

        Image image = buttonObject.AddComponent<Image>();
        image.sprite = whiteSprite;
        image.color = new Color(1f, 1f, 1f, 0.04f);

        AddOutline(buttonObject, new Color(accentColor.r, accentColor.g, accentColor.b, 0.25f), new Vector2(1f, -1f));

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.9f, 0.9f, 1f);
        colors.pressedColor = new Color(0.8f, 0.7f, 0.7f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        CreateText(label + "Text", buttonObject.transform, label, 20, FontStyle.Bold, titleColor, TextAnchor.MiddleCenter, Vector2.zero, rect.sizeDelta, false);
    }

    private void CreateMenuButton(string label, Vector2 anchoredPosition, int fontSize, Transform parent, UnityEngine.Events.UnityAction action, bool primary)
    {
        GameObject buttonObject = CreateUiObject(label + "Button", parent);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(320f, primary ? 52f : 46f);

        Image image = buttonObject.AddComponent<Image>();
        image.sprite = whiteSprite;
        image.color = primary ? new Color(accentColor.r, accentColor.g, accentColor.b, 0.12f) : buttonNormalColor;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 1.05f);
        colors.pressedColor = new Color(0.85f, 0.80f, 0.80f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        AddOutline(buttonObject, primary ? new Color(accentColor.r, accentColor.g, accentColor.b, 0.40f) : new Color(1f, 1f, 1f, 0.05f), new Vector2(1f, -1f));

        Text txt = CreateText(label + "Text", buttonObject.transform, label, fontSize, FontStyle.Bold, titleColor, TextAnchor.MiddleCenter, Vector2.zero, rect.sizeDelta, false);

        EventTrigger trigger = buttonObject.AddComponent<EventTrigger>();

        AddEvent(trigger, EventTriggerType.PointerEnter, () =>
        {
            image.color = primary ? new Color(accentColor.r, accentColor.g, accentColor.b, 0.22f) : buttonHoverColor;
            txt.color = Color.Lerp(titleColor, new Color(1f, 0.85f, 0.85f, 1f), 0.35f);
        });

        AddEvent(trigger, EventTriggerType.PointerExit, () =>
        {
            image.color = primary ? new Color(accentColor.r, accentColor.g, accentColor.b, 0.12f) : buttonNormalColor;
            txt.color = titleColor;
        });

        RegisterGlitchTarget(rect);
    }

    private void AddEvent(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = type;
        entry.callback.AddListener(_ => action.Invoke());
        trigger.triggers.Add(entry);
    }

    private Text CreateText(
        string name,
        Transform parent,
        string content,
        int fontSize,
        FontStyle fontStyle,
        Color color,
        TextAnchor alignment,
        Vector2 anchoredPosition,
        Vector2 size,
        bool heavyShadow)
    {
        GameObject textObject = CreateUiObject(name, parent);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Text text = textObject.AddComponent<Text>();
        text.font = uiFont;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = alignment;

        Shadow shadow = textObject.AddComponent<Shadow>();
        shadow.effectColor = heavyShadow ? new Color(0f, 0f, 0f, 0.8f) : new Color(0f, 0f, 0f, 0.35f);
        shadow.effectDistance = heavyShadow ? new Vector2(3f, -3f) : new Vector2(1f, -1f);

        return text;
    }

    private RectTransform CreateLogo(string name, Transform parent, Sprite sprite, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject logoObject = CreateUiObject(name, parent);
        RectTransform rect = logoObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = logoObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = true;

        Shadow shadow = logoObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.70f);
        shadow.effectDistance = new Vector2(8f, -8f);

        RegisterGlitchTarget(rect);
        return rect;
    }

    private GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panel = CreateUiObject(name, parent);
        Image image = panel.AddComponent<Image>();
        image.sprite = whiteSprite;
        image.color = color;
        return panel;
    }

    private void AddOutline(GameObject go, Color color, Vector2 distance)
    {
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
    }

    private GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    private void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private Sprite CreateWhiteSprite()
    {
        Rect rect = new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height);
        return Sprite.Create(Texture2D.whiteTexture, rect, new Vector2(0.5f, 0.5f));
    }

    private void RegisterGlitchTarget(RectTransform rect)
    {
        if (rect == null || glitchTargets.Contains(rect)) return;

        glitchTargets.Add(rect);
        originalPositions[rect] = rect.anchoredPosition;
    }

    private void ApplyCameraMode(CameraSwitchTrigger.CameraMode mode)
    {
        CameraSwitchTrigger.SetCameraMode(mode);
        RefreshCameraModeButtons();
    }

    private void RefreshCameraModeButtons()
    {
        RefreshModeButtonVisual(aerialModeButtonImage, aerialModeButtonText, CameraSwitchTrigger.CurrentMode == CameraSwitchTrigger.CameraMode.Aerea);
        RefreshModeButtonVisual(firstPersonModeButtonImage, firstPersonModeButtonText, CameraSwitchTrigger.CurrentMode == CameraSwitchTrigger.CameraMode.PrimeraPersona);
    }

    private void RefreshModeButtonVisual(Image backgroundImage, Text label, bool selected)
    {
        if (backgroundImage == null || label == null)
        {
            return;
        }

        backgroundImage.color = selected
            ? new Color(accentColor.r, accentColor.g, accentColor.b, 0.22f)
            : new Color(1f, 1f, 1f, 0.04f);

        label.color = selected
            ? Color.Lerp(titleColor, new Color(1f, 0.86f, 0.86f, 1f), 0.25f)
            : titleColor;
    }

    private void HandleEscapeMenu()
    {
        if (!hasStartedGame || !Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        if (menuRoot != null && menuRoot.activeSelf)
        {
            CloseMenu();
        }
        else
        {
            OpenMenu();
        }
    }

    private void AnimateBackground()
    {
        if (backgroundRect == null || menuRoot == null || !menuRoot.activeInHierarchy) return;

        float x = Mathf.Sin(Time.unscaledTime * backgroundDriftSpeed) * backgroundDriftAmount;
        float y = Mathf.Cos(Time.unscaledTime * backgroundDriftSpeed * 0.7f) * (backgroundDriftAmount * 0.55f);
        backgroundRect.localPosition = new Vector3(x, y, 0f);
    }

    private void AnimateFog()
    {
        if (fogA != null)
        {
            float a = fogColorA.a * (0.90f + Mathf.Sin(Time.unscaledTime * 0.8f) * 0.10f);
            fogA.color = new Color(fogColorA.r, fogColorA.g, fogColorA.b, a);
        }

        if (fogB != null)
        {
            float a = fogColorB.a * (0.85f + Mathf.Cos(Time.unscaledTime * 0.55f) * 0.20f);
            fogB.color = new Color(fogColorB.r, fogColorB.g, fogColorB.b, a);
        }
    }

    private void AnimateTitle()
    {
        if (titleRect == null || menuRoot == null || !menuRoot.activeInHierarchy) return;

        titleRect.anchoredPosition = titleBasePos + new Vector2(0f, Mathf.Sin(Time.unscaledTime * titleBreathSpeed) * titleBreathAmount);
    }

    private IEnumerator PlayIntroFade()
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < introFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / introFadeDuration);
            canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private IEnumerator AmbienceLoop()
    {
        while (true)
        {
            if (menuPanelRect != null && menuRoot != null && menuRoot.activeInHierarchy)
            {
                float driftX = Mathf.Sin(Time.unscaledTime * 0.25f) * 1.2f;
                float driftY = Mathf.Cos(Time.unscaledTime * 0.18f) * 1.6f;
                menuPanelRect.anchoredPosition = menuPanelBasePos + new Vector2(driftX, driftY);
            }

            yield return null;
        }
    }

    private IEnumerator GlitchLoop()
    {
        float chance = Mathf.Max(0.01f, glitchChancePerSecond);

        while (true)
        {
            yield return new WaitForSecondsRealtime(Random.Range(1.5f, 4f) / chance);

            if (menuRoot == null || !menuRoot.activeInHierarchy || glitchTargets.Count == 0) continue;

            for (int i = 0; i < glitchTargets.Count; i++)
            {
                RectTransform rect = glitchTargets[i];
                if (rect == null || !originalPositions.ContainsKey(rect)) continue;

                rect.anchoredPosition = originalPositions[rect] + Random.insideUnitCircle * glitchIntensity;
            }

            if (titleText != null)
                titleText.color = Color.Lerp(titleColor, accentColor, 0.28f);

            if (flashImage != null)
                flashImage.color = new Color(0.35f, 0f, 0f, Random.Range(0.03f, 0.09f));

            yield return new WaitForSecondsRealtime(glitchDuration);

            for (int i = 0; i < glitchTargets.Count; i++)
            {
                RectTransform rect = glitchTargets[i];
                if (rect == null || !originalPositions.ContainsKey(rect)) continue;

                rect.anchoredPosition = originalPositions[rect];
            }

            if (titleText != null)
                titleText.color = titleColor;

            if (flashImage != null)
                flashImage.color = new Color(0.35f, 0f, 0f, 0f);
        }
    }

    private IEnumerator FlickerLoop()
    {
        float chance = Mathf.Max(0.01f, flickerChancePerSecond);

        while (true)
        {
            yield return new WaitForSecondsRealtime(Random.Range(2f, 5f) / chance);

            if (canvasGroup == null || menuRoot == null || !menuRoot.activeInHierarchy || canvasGroup.alpha < 0.99f)
                continue;

            float originalAlpha = canvasGroup.alpha;

            canvasGroup.alpha = Mathf.Clamp01(originalAlpha - Random.Range(0.05f, 0.14f));
            yield return new WaitForSecondsRealtime(0.04f);
            canvasGroup.alpha = originalAlpha;

            if (Random.value > 0.65f)
            {
                canvasGroup.alpha = Mathf.Clamp01(originalAlpha - Random.Range(0.02f, 0.08f));
                yield return new WaitForSecondsRealtime(0.03f);
                canvasGroup.alpha = originalAlpha;
            }
        }
    }

    private IEnumerator WhisperLoop()
    {
        while (true)
        {
            if (whisperText != null)
            {
                whisperText.text = whisperLines[Random.Range(0, whisperLines.Length)];
                whisperText.color = new Color(faintTextColor.r, faintTextColor.g, faintTextColor.b, Random.Range(0.55f, 0.95f));
            }

            yield return new WaitForSecondsRealtime(Random.Range(3f, 6f));
        }
    }

    private Font LoadUiFont()
    {
        Font font = null;

        try
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch { }

        if (font != null) return font;

        return Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Segoe UI", "Tahoma" }, 16);
    }
}
