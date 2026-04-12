using System.Collections;
using Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class MainMenuController : MonoBehaviour
{
    const string VolumeKey = "menu.volume";
    const string SensitivityKey = "menu.sensitivity";
    const string QualityKey = "menu.quality";
    const string FullscreenKey = "menu.fullscreen";
    const string VSyncKey = "menu.vsync";
    const string DefaultMenuMusicPath = "Assets/MaxStack/Abyss of Negative Existence/Audio/Music/ANE Oppressive Absolution.wav";
    static readonly Color MenuCardColor = new Color(0.11f, 0.12f, 0.14f, 0.96f);
    static readonly Color MenuCardShadowColor = new Color(0f, 0f, 0f, 0.55f);

    [Header("Branding")]
    [SerializeField] string gameTitle = "DARK FALL";
    [SerializeField] string gameSubtitle = "NO DEBERIAS ESTAR AQUI";
    [SerializeField] Sprite logoSprite;

    [Header("Theme")]
    [SerializeField] Sprite backgroundSprite;
    [SerializeField] bool useSceneBackgroundCamera = true;
    [SerializeField] Color panelColor = new Color(0.03f, 0.04f, 0.05f, 0.80f);
    [SerializeField] Color accentColor = new Color(0.62f, 0.05f, 0.07f, 0.98f);
    [SerializeField] Color accentSoftColor = new Color(0.62f, 0.05f, 0.07f, 0.24f);
    [SerializeField] Color buttonColor = new Color(1f, 1f, 1f, 0.05f);
    [SerializeField] Color buttonHoverColor = new Color(0.70f, 0.08f, 0.10f, 0.65f);
    [SerializeField] Color titleColor = new Color(0.94f, 0.94f, 0.92f, 1f);
    [SerializeField] Color textColor = new Color(0.78f, 0.78f, 0.76f, 1f);
    [SerializeField] Color faintTextColor = new Color(0.50f, 0.50f, 0.50f, 1f);
    [SerializeField] Color lineColor = new Color(1f, 1f, 1f, 0.08f);
    [SerializeField] Color pauseBackdropColor = new Color(0.01f, 0.01f, 0.02f, 0.42f);
    [SerializeField] bool keepCurrentViewOnPause = true;

    [Header("Menu Audio")]
    [SerializeField] AudioClip menuMusicClip;
    [SerializeField, Range(0f, 1f)] float menuMusicVolume = 0.18f;

    [Header("Atmosphere")]
    [SerializeField] bool applySceneAtmosphere = true;
    [SerializeField] Color outdoorFogColor = new Color(0.06f, 0.08f, 0.09f, 1f);
    [SerializeField, Range(0f, 0.1f)] float outdoorFogDensity = 0.02f;
    [SerializeField, Range(0f, 1f)] float bounceLightIntensityMultiplier = 0.55f;
    [SerializeField, Range(0.5f, 1f)] float bounceLightRangeMultiplier = 0.8f;
    [SerializeField] Vector3 rogueLightBounds = new Vector3(2500f, 500f, 2500f);

    [Header("Defaults")]
    [SerializeField] float defaultMasterVolume = 0.8f;
    [SerializeField] float defaultCameraSensitivity = 6f;
    [SerializeField] int defaultQualityLevel = 2;
    [SerializeField] bool defaultFullscreen = true;
    [SerializeField] bool defaultVSync = false;

    CanvasGroup canvasGroup;
    GameObject menuRoot;
    GameObject optionsPanel;
    GameObject extrasPanel;
    GameObject pauseBackdrop;
    GameObject pauseFocusArea;
    RectTransform backgroundRect;
    RectTransform titleRect;
    Vector2 titleBasePos;
    Text whisperText;
    Text sceneLabelText;
    Text playButtonText;
    GameObject playButton;
    GameObject newGameButton;
    GameObject optionsButton;
    GameObject extrasButton;
    GameObject quitButton;
    Text volumeValueText;
    Text sensitivityValueText;
    Text qualityValueText;
    Text fullscreenValueText;
    Text vSyncValueText;
    Text aerialModeButtonText;
    Text firstPersonModeButtonText;
    Image aerialModeButtonImage;
    Image firstPersonModeButtonImage;
    Slider volumeSlider;
    Slider sensitivitySlider;
    AudioSource menuMusicSource;
    Camera gameplayCamera;
    AudioListener gameplayAudioListener;
    CinemachineBrain gameplayCinemachineBrain;
    Camera menuBackgroundCamera;
    Transform menuCameraRigRoot;
    Transform menuCameraTarget;
    GameObject notesHudRoot;
    GameObject interactHudRoot;
    GameObject messageHudRoot;
    GameObject fpsHudRoot;
    Font uiFont;
    Sprite whiteSprite;
    PlayerMovemnt playerMovement;
    IsoCameraOrbit isoCameraOrbit;
    float masterVolume;
    float cameraSensitivity;
    int qualityLevel;
    bool fullscreenEnabled;
    bool vSyncEnabled;
    bool hasStartedGame;

    void Awake()
    {
        uiFont = LoadUiFont();
        whiteSprite = CreateWhiteSprite();
        playerMovement = FindObjectOfType<PlayerMovemnt>();
        isoCameraOrbit = FindObjectOfType<IsoCameraOrbit>();
        gameplayCamera = GetComponent<Camera>();
        gameplayAudioListener = GetComponent<AudioListener>();
        gameplayCinemachineBrain = GetComponent<CinemachineBrain>();
        CacheGameplayHudRoots();
        EnsureMenuCameraRig();
        EnsureMenuAudio();
        TuneSceneAtmosphere();
        BuildMenu();
        LoadSettings();
        ApplyAllSettings();
        OpenMenu();
        StartCoroutine(FadeIn());
        StartCoroutine(WhisperLoop());
    }

    void Update()
    {
        bool animateTitleScene = !hasStartedGame;

        if (backgroundRect != null)
        {
            backgroundRect.localPosition = animateTitleScene
                ? new Vector3(Mathf.Sin(Time.unscaledTime * 0.08f) * 10f, Mathf.Cos(Time.unscaledTime * 0.05f) * 6f, 0f)
                : Vector3.zero;
        }
        if (titleRect != null)
        {
            titleRect.anchoredPosition = animateTitleScene
                ? titleBasePos + new Vector2(0f, Mathf.Sin(Time.unscaledTime * 0.75f) * 4f)
                : titleBasePos;
        }

        if (!hasStartedGame || !Input.GetKeyDown(KeyCode.Escape)) return;

        if (menuRoot.activeSelf) CloseMenu();
        else OpenMenu();
    }

    void OnDestroy()
    {
        SetGamePaused(false);
        SetMenuMusicPlaying(false);
    }

    void LoadSettings()
    {
        masterVolume = PlayerPrefs.GetFloat(VolumeKey, defaultMasterVolume);
        cameraSensitivity = PlayerPrefs.GetFloat(SensitivityKey, defaultCameraSensitivity);
        qualityLevel = PlayerPrefs.GetInt(QualityKey, Mathf.Clamp(defaultQualityLevel, 0, QualitySettings.names.Length - 1));
        fullscreenEnabled = PlayerPrefs.GetInt(FullscreenKey, defaultFullscreen ? 1 : 0) == 1;
        vSyncEnabled = PlayerPrefs.GetInt(VSyncKey, defaultVSync ? 1 : 0) == 1;
    }

    void SaveSettings()
    {
        PlayerPrefs.SetFloat(VolumeKey, masterVolume);
        PlayerPrefs.SetFloat(SensitivityKey, cameraSensitivity);
        PlayerPrefs.SetInt(QualityKey, qualityLevel);
        PlayerPrefs.SetInt(FullscreenKey, fullscreenEnabled ? 1 : 0);
        PlayerPrefs.SetInt(VSyncKey, vSyncEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    void ApplyAllSettings()
    {
        AudioListener.volume = masterVolume;
        if (isoCameraOrbit != null) isoCameraOrbit.MouseSensitivity = cameraSensitivity;
        qualityLevel = Mathf.Clamp(qualityLevel, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
        PerformanceBootstrap.ApplyQualityProfile(qualityLevel);
        QualitySettings.vSyncCount = vSyncEnabled ? 1 : 0;
        Screen.fullScreen = fullscreenEnabled;
        ApplyCameraMode(CameraSwitchTrigger.CurrentMode);
        RefreshOptionLabels();
    }

    void OpenMenu()
    {
        SetGamePaused(true);
        SetGameplayEnabled(false);
        menuRoot.SetActive(true);
        optionsPanel.SetActive(false);
        extrasPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        CameraSwitchTrigger.SetMenuOpen(true);
        RefreshMenuVisualState();
        SetMenuSceneCameraActive(true);
        SetHudVisible(false);
        SetMenuMusicPlaying(true);
        RefreshPlayButtonLabel();
        if (sceneLabelText != null) sceneLabelText.text = "SENAL PERDIDA // " + SceneManager.GetActiveScene().name.ToUpperInvariant();
    }

    void CloseMenu()
    {
        SetGamePaused(false);
        SetGameplayEnabled(true);
        menuRoot.SetActive(false);
        optionsPanel.SetActive(false);
        extrasPanel.SetActive(false);
        CameraSwitchTrigger.SetMenuOpen(false);
        SetMenuSceneCameraActive(false);
        CameraSwitchTrigger.RefreshGlobalCameraState();
        SetHudVisible(true);
        SetMenuMusicPlaying(false);
    }

    void SetGameplayEnabled(bool enabled)
    {
        if (playerMovement != null) playerMovement.enabled = enabled;
        if (isoCameraOrbit != null) isoCameraOrbit.enabled = enabled;
    }

    void StartGame()
    {
        hasStartedGame = true;
        RefreshPlayButtonLabel();
        RefreshMenuVisualState();
        CloseMenu();
    }

    void StartNewGame()
    {
        SetGamePaused(false);
        SetMenuMusicPlaying(false);
        Scene activeScene = SceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(activeScene.name))
        {
            SceneManager.LoadScene(activeScene.name);
        }
    }

    void SetGamePaused(bool paused)
    {
        Time.timeScale = paused ? 0f : 1f;
        AudioListener.pause = paused;
    }

    void ToggleOptions() { optionsPanel.SetActive(!optionsPanel.activeSelf); extrasPanel.SetActive(false); RefreshOptionLabels(); }
    void ToggleExtras() { extrasPanel.SetActive(!extrasPanel.activeSelf); optionsPanel.SetActive(false); }
    void ChangeVolume(float delta) { masterVolume = Mathf.Clamp01(masterVolume + delta); AudioListener.volume = masterVolume; SaveSettings(); RefreshOptionLabels(); }
    void ChangeSensitivity(float delta) { cameraSensitivity = Mathf.Clamp(cameraSensitivity + delta, 1f, 20f); if (isoCameraOrbit != null) isoCameraOrbit.MouseSensitivity = cameraSensitivity; SaveSettings(); RefreshOptionLabels(); }
    void SetVolume(float value) { masterVolume = Mathf.Clamp01(value); AudioListener.volume = masterVolume; SaveSettings(); RefreshOptionLabels(); }
    void SetSensitivity(float value) { cameraSensitivity = Mathf.Clamp(value, 1f, 20f); if (isoCameraOrbit != null) isoCameraOrbit.MouseSensitivity = cameraSensitivity; SaveSettings(); RefreshOptionLabels(); }
    void CycleQuality() { qualityLevel = (qualityLevel + 1) % Mathf.Max(1, QualitySettings.names.Length); PerformanceBootstrap.ApplyQualityProfile(qualityLevel); SaveSettings(); RefreshOptionLabels(); }
    void ToggleFullscreen() { fullscreenEnabled = !fullscreenEnabled; Screen.fullScreen = fullscreenEnabled; SaveSettings(); RefreshOptionLabels(); }
    void ToggleVSync() { vSyncEnabled = !vSyncEnabled; QualitySettings.vSyncCount = vSyncEnabled ? 1 : 0; SaveSettings(); RefreshOptionLabels(); }
    void ResetDefaults()
    {
        masterVolume = defaultMasterVolume;
        cameraSensitivity = defaultCameraSensitivity;
        qualityLevel = Mathf.Clamp(defaultQualityLevel, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
        fullscreenEnabled = defaultFullscreen;
        vSyncEnabled = defaultVSync;
        ApplyAllSettings();
        SaveSettings();
    }
    void QuitGame() {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void RefreshOptionLabels()
    {
        if (volumeValueText != null) volumeValueText.text = Mathf.RoundToInt(masterVolume * 100f) + "%";
        if (sensitivityValueText != null) sensitivityValueText.text = cameraSensitivity.ToString("0.0");
        if (volumeSlider != null && !Mathf.Approximately(volumeSlider.value, masterVolume)) volumeSlider.SetValueWithoutNotify(masterVolume);
        if (sensitivitySlider != null && !Mathf.Approximately(sensitivitySlider.value, cameraSensitivity)) sensitivitySlider.SetValueWithoutNotify(cameraSensitivity);
        if (qualityValueText != null) qualityValueText.text = QualitySettings.names.Length > 0 ? QualitySettings.names[qualityLevel].ToUpperInvariant() : "N/A";
        if (fullscreenValueText != null) fullscreenValueText.text = fullscreenEnabled ? "PANTALLA COMPLETA" : "VENTANA";
        if (vSyncValueText != null) vSyncValueText.text = vSyncEnabled ? "ACTIVADO" : "DESACTIVADO";
        RefreshModeButton(aerialModeButtonImage, aerialModeButtonText, CameraSwitchTrigger.CurrentMode == CameraSwitchTrigger.CameraMode.Aerea);
        RefreshModeButton(firstPersonModeButtonImage, firstPersonModeButtonText, CameraSwitchTrigger.CurrentMode == CameraSwitchTrigger.CameraMode.PrimeraPersona);
    }

    void RefreshPlayButtonLabel()
    {
        if (playButtonText == null) return;
        playButtonText.text = hasStartedGame ? "REANUDAR" : "JUGAR";
    }

    void ApplyCameraMode(CameraSwitchTrigger.CameraMode mode)
    {
        CameraSwitchTrigger.SetCameraMode(mode);
        RefreshOptionLabels();
    }

    void RefreshModeButton(Image image, Text label, bool selected)
    {
        if (image == null || label == null) return;
        image.color = selected ? new Color(accentColor.r, accentColor.g, accentColor.b, 0.72f) : new Color(0.22f, 0.24f, 0.28f, 0.74f);
        label.color = selected ? Color.white : titleColor;
    }

    void BuildMenu()
    {
        EnsureEventSystem();
        GameObject canvasObject = CreateUiObject("HorrorMenuCanvas", null);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
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

        Color backgroundColor = useSceneBackgroundCamera
            ? new Color(0f, 0f, 0f, backgroundSprite != null ? 0.22f : 0.34f)
            : backgroundSprite != null ? Color.white : new Color(0.02f, 0.02f, 0.03f, 1f);
        Sprite backgroundAsset = useSceneBackgroundCamera ? null : backgroundSprite;
        backgroundRect = CreateFullImage("Background", canvasObject.transform, backgroundAsset, backgroundColor).rectTransform;
        CreateFullPanel("FogA", canvasObject.transform, new Color(0.02f, 0.03f, 0.04f, 0.50f));
        CreateFullPanel("FogB", canvasObject.transform, new Color(0.12f, 0.02f, 0.02f, 0.08f));
        pauseBackdrop = CreateFullPanel("PauseBackdrop", canvasObject.transform, pauseBackdropColor).gameObject;
        pauseBackdrop.SetActive(false);

        Color effectivePanelColor = new Color(0f, 0f, 0f, 0f);
        GameObject center = CreatePanel("CenterPanel", canvasObject.transform, effectivePanelColor);
        RectTransform centerRect = center.GetComponent<RectTransform>();
        centerRect.anchorMin = centerRect.anchorMax = new Vector2(0.5f, 0.5f);
        centerRect.pivot = new Vector2(0.5f, 0.5f);
        centerRect.sizeDelta = new Vector2(760f, 720f);
        centerRect.anchoredPosition = new Vector2(0f, 12f);
        pauseFocusArea = CreatePanel("PauseFocusArea", center.transform, MenuCardColor);
        RectTransform pauseFocusRect = pauseFocusArea.GetComponent<RectTransform>();
        pauseFocusRect.SetAnchored(new Vector2(0f, -286f), new Vector2(420f, 300f));
        AddOutline(pauseFocusArea, lineColor, new Vector2(1f, -1f));
        AddShadow(pauseFocusArea, MenuCardShadowColor, new Vector2(0f, -8f));
        pauseFocusArea.SetActive(false);
        if (logoSprite != null)
        {
            titleRect = CreateImage("Logo", center.transform, logoSprite, new Vector2(0f, -78f), new Vector2(180f, 180f)).rectTransform;
        }
        else
        {
            titleRect = CreateText("Title", center.transform, gameTitle, 74, FontStyle.Bold, titleColor, TextAnchor.MiddleCenter, new Vector2(0f, -160f), new Vector2(620f, 100f), true).rectTransform;
        }
        titleBasePos = titleRect.anchoredPosition;
        CreateText("Subtitle", center.transform, gameSubtitle, 18, FontStyle.Italic, accentColor, TextAnchor.MiddleCenter, new Vector2(0f, -244f), new Vector2(540f, 28f), false);
        CreatePanel("Separator", center.transform, new Color(1f, 1f, 1f, 0.14f)).GetComponent<RectTransform>().SetAnchored(new Vector2(0f, -286f), new Vector2(220f, 1.5f));
        playButton = CreateMenuButton(center.transform, "JUGAR", new Vector2(0f, -346f), 28, StartGame, true, out playButtonText);
        newGameButton = CreateMenuButton(center.transform, "NUEVA PARTIDA", new Vector2(0f, -396f), 18, StartNewGame, false);
        optionsButton = CreateMenuButton(center.transform, "OPCIONES", new Vector2(0f, -446f), 20, ToggleOptions, false);
        extrasButton = CreateMenuButton(center.transform, "EXTRAS", new Vector2(0f, -496f), 20, ToggleExtras, false);
        quitButton = CreateMenuButton(center.transform, "SALIR", new Vector2(0f, -546f), 20, QuitGame, false);

        whisperText = CreateText("Whisper", canvasObject.transform, "...", 20, FontStyle.Italic, faintTextColor, TextAnchor.MiddleCenter, new Vector2(0f, -990f), new Vector2(900f, 40f), false);
        sceneLabelText = CreateText("SceneLabel", canvasObject.transform, "", 14, FontStyle.Normal, faintTextColor, TextAnchor.MiddleRight, new Vector2(-40f, -40f), new Vector2(420f, 24f), false);
        sceneLabelText.rectTransform.anchorMin = sceneLabelText.rectTransform.anchorMax = new Vector2(1f, 1f);
        sceneLabelText.rectTransform.pivot = new Vector2(1f, 1f);

        optionsPanel = CreateCard(canvasObject.transform, "OptionsPanel", new Vector2(0.82f, 0.52f), new Vector2(500f, 580f));
        BuildOptions(optionsPanel.transform);
        optionsPanel.SetActive(false);
        extrasPanel = CreateCard(canvasObject.transform, "ExtrasPanel", new Vector2(0.82f, 0.52f), new Vector2(500f, 340f));
        BuildExtras(extrasPanel.transform);
        extrasPanel.SetActive(false);
        RefreshMenuVisualState();
    }

    void BuildOptions(Transform parent)
    {
        CreateText("OptionsTitle", parent, "AJUSTES", 30, FontStyle.Bold, titleColor, TextAnchor.MiddleCenter, new Vector2(0f, -24f), new Vector2(280f, 40f), false);
        CreatePanel("Sep", parent, lineColor).GetComponent<RectTransform>().SetAnchored(new Vector2(0f, -76f), new Vector2(420f, 2f));
        volumeValueText = CreateSliderRow(parent, "VOLUMEN GENERAL", new Vector2(30f, -122f), 0f, 1f, value => SetVolume(value), out volumeSlider);
        sensitivityValueText = CreateSliderRow(parent, "SENSIBILIDAD", new Vector2(30f, -182f), 1f, 20f, value => SetSensitivity(value), out sensitivitySlider);
        qualityValueText = CreateActionRow(parent, "GRAFICOS", new Vector2(30f, -242f), CycleQuality);
        fullscreenValueText = CreateActionRow(parent, "PANTALLA", new Vector2(30f, -302f), ToggleFullscreen);
        vSyncValueText = CreateActionRow(parent, "VSYNC", new Vector2(30f, -362f), ToggleVSync);
        CreateLabel(parent, "MODO DE CAMARA", new Vector2(30f, -414f), new Vector2(220f, 28f));
        CreateModeButton(parent, "AEREA", new Vector2(30f, -452f), new Vector2(174f, 36f), () => ApplyCameraMode(CameraSwitchTrigger.CameraMode.Aerea), out aerialModeButtonImage, out aerialModeButtonText);
        CreateModeButton(parent, "PRIMERA PERSONA", new Vector2(220f, -452f), new Vector2(220f, 36f), () => ApplyCameraMode(CameraSwitchTrigger.CameraMode.PrimeraPersona), out firstPersonModeButtonImage, out firstPersonModeButtonText);
        CreateButton(parent, "DefaultsButton", new Vector2(30f, -508f), new Vector2(410f, 38f), ResetDefaults);
        CreateText("DefaultsLabel", parent.Find("DefaultsButton"), "RESTAURAR PREDETERMINADOS", 14, FontStyle.Bold, titleColor, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(410f, 38f), false);
    }

    void BuildExtras(Transform parent)
    {
        CreateText("ExtrasTitle", parent, "INFORMACION", 30, FontStyle.Bold, titleColor, TextAnchor.MiddleCenter, new Vector2(0f, -24f), new Vector2(320f, 40f), false);
        CreatePanel("Sep", parent, lineColor).GetComponent<RectTransform>().SetAnchored(new Vector2(0f, -72f), new Vector2(420f, 2f));
        Text body = CreateTopLeftText("ExtrasBody", parent, "OBJETIVO\n\nEncuentra gasolina, consigue el mapa y reune documentos.\n\nCONTROLES\n\nWASD mover\nE interactuar\nM mapa\nTAB notas\nESC menu\n\nCONSEJO\n\nSi el silencio cambia, corre.", 18, FontStyle.Normal, textColor, TextAnchor.UpperLeft, new Vector2(28f, -100f), new Vector2(420f, 180f), false);
        body.horizontalOverflow = HorizontalWrapMode.Wrap;
        body.verticalOverflow = VerticalWrapMode.Truncate;
    }

    Text CreateSliderRow(Transform parent, string label, Vector2 pos, float minValue, float maxValue, UnityAction<float> onValueChanged, out Slider slider)
    {
        CreateLabel(parent, label, pos, new Vector2(200f, 28f));
        slider = CreateSlider(parent, label + "Slider", pos + new Vector2(214f, -2f), new Vector2(172f, 34f), minValue, maxValue, onValueChanged);
        Text value = CreateTopLeftText(label + "Value", parent, "", 18, FontStyle.Bold, titleColor, TextAnchor.MiddleCenter, pos + new Vector2(392f, 0f), new Vector2(54f, 28f), false);
        return value;
    }

    Text CreateActionRow(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction action)
    {
        CreateLabel(parent, label, pos, new Vector2(180f, 28f));
        GameObject button = CreateButton(parent, label + "Action", pos + new Vector2(248f, -2f), new Vector2(198f, 36f), action);
        return CreateText(label + "ActionText", button.transform, "", 15, FontStyle.Bold, titleColor, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(198f, 36f), false);
    }

    void CreateLabel(Transform parent, string text, Vector2 pos, Vector2 size)
    {
        CreateTopLeftText(text + "Label", parent, text, 16, FontStyle.Bold, textColor, TextAnchor.MiddleLeft, pos, size, false);
    }

    void CreateModeButton(Transform parent, string text, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction action, out Image image, out Text label)
    {
        GameObject button = CreateButton(parent, text + "Mode", pos, size, action);
        image = button.GetComponent<Image>();
        label = CreateText(text + "ModeText", button.transform, text, 14, FontStyle.Bold, titleColor, TextAnchor.MiddleCenter, Vector2.zero, size, false);
    }

    GameObject CreateMenuButton(Transform parent, string label, Vector2 pos, int fontSize, UnityEngine.Events.UnityAction action, bool primary)
    {
        return CreateMenuButton(parent, label, pos, fontSize, action, primary, out _);
    }

    GameObject CreateMenuButton(Transform parent, string label, Vector2 pos, int fontSize, UnityEngine.Events.UnityAction action, bool primary, out Text labelText)
    {
        GameObject button = CreateButton(parent, label + "Button", pos, new Vector2(320f, primary ? 42f : 34f), action);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = pos;
        Image image = button.GetComponent<Image>();
        image.color = primary ? new Color(0.56f, 0.04f, 0.06f, 0.92f) : new Color(0.36f, 0.08f, 0.10f, 0.82f);
        Outline outline = button.GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = primary ? new Color(1f, 0.86f, 0.86f, 0.42f) : new Color(0.92f, 0.66f, 0.66f, 0.22f);
            outline.effectDistance = new Vector2(1f, -1f);
        }
        Text buttonLabel = CreateText(label + "Text", button.transform, label, fontSize, FontStyle.Bold, primary ? Color.white : titleColor, TextAnchor.MiddleCenter, Vector2.zero, rect.sizeDelta, false);
        labelText = buttonLabel;
        EventTrigger trigger = button.AddComponent<EventTrigger>();
        AddEvent(trigger, EventTriggerType.PointerEnter, () =>
        {
            image.color = primary ? new Color(0.78f, 0.10f, 0.12f, 0.98f) : new Color(0.62f, 0.05f, 0.07f, 0.94f);
            rect.localScale = new Vector3(1.03f, 1.03f, 1f);
            buttonLabel.color = Color.white;
        });
        AddEvent(trigger, EventTriggerType.PointerExit, () =>
        {
            image.color = primary ? new Color(0.56f, 0.04f, 0.06f, 0.92f) : new Color(0.36f, 0.08f, 0.10f, 0.82f);
            rect.localScale = Vector3.one;
            buttonLabel.color = primary ? Color.white : titleColor;
        });
        return button;
    }

    void CreateSmallButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction action)
    {
        GameObject button = CreateButton(parent, label + "SmallButton", pos, new Vector2(40f, 34f), action);
        CreateText(label + "SmallText", button.transform, label, 20, FontStyle.Bold, titleColor, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(40f, 34f), false);
    }

    GameObject CreateButton(Transform parent, string name, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        GameObject button = CreateUiObject(name, parent);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        Image image = button.AddComponent<Image>();
        image.sprite = whiteSprite;
        image.color = buttonColor;
        AddOutline(button, new Color(1f, 1f, 1f, 0.05f), new Vector2(1f, -1f));
        Button uiButton = button.AddComponent<Button>();
        uiButton.targetGraphic = image;
        uiButton.onClick.AddListener(action);
        return button;
    }

    GameObject CreateCard(Transform parent, string name, Vector2 anchor, Vector2 size)
    {
        GameObject card = CreatePanel(name, parent, MenuCardColor);
        RectTransform rect = card.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        AddOutline(card, lineColor, new Vector2(1f, -1f));
        AddShadow(card, MenuCardShadowColor, new Vector2(0f, -8f));
        return card;
    }

    Slider CreateSlider(Transform parent, string name, Vector2 pos, Vector2 size, float minValue, float maxValue, UnityAction<float> onValueChanged)
    {
        GameObject sliderObject = CreateUiObject(name, parent);
        RectTransform rect = sliderObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        Image background = sliderObject.AddComponent<Image>();
        background.sprite = whiteSprite;
        background.color = new Color(1f, 1f, 1f, 0.09f);

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.targetGraphic = background;

        RectTransform fillArea = CreateUiObject("Fill Area", sliderObject.transform).GetComponent<RectTransform>();
        fillArea.anchorMin = Vector2.zero;
        fillArea.anchorMax = Vector2.one;
        fillArea.offsetMin = new Vector2(6f, 8f);
        fillArea.offsetMax = new Vector2(-22f, -8f);

        Image fill = CreateUiObject("Fill", fillArea).AddComponent<Image>();
        fill.sprite = whiteSprite;
        fill.color = new Color(0.88f, 0.34f, 0.34f, 0.95f);
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        RectTransform handleSlideArea = CreateUiObject("Handle Slide Area", sliderObject.transform).GetComponent<RectTransform>();
        handleSlideArea.anchorMin = Vector2.zero;
        handleSlideArea.anchorMax = Vector2.one;
        handleSlideArea.offsetMin = new Vector2(10f, 0f);
        handleSlideArea.offsetMax = new Vector2(-10f, 0f);

        Image handle = CreateUiObject("Handle", handleSlideArea).AddComponent<Image>();
        handle.sprite = whiteSprite;
        handle.color = Color.white;
        RectTransform handleRect = handle.rectTransform;
        handleRect.sizeDelta = new Vector2(14f, 34f);

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.onValueChanged.AddListener(onValueChanged);

        ColorBlock colors = slider.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.92f, 0.92f, 1f);
        colors.pressedColor = new Color(1f, 0.78f, 0.78f, 1f);
        colors.selectedColor = colors.highlightedColor;
        slider.colors = colors;

        return slider;
    }

    void EnsureMenuAudio()
    {
        menuMusicSource = GetComponent<AudioSource>();
        if (menuMusicSource == null)
        {
            menuMusicSource = gameObject.AddComponent<AudioSource>();
        }

        menuMusicSource.playOnAwake = false;
        menuMusicSource.loop = true;
        menuMusicSource.ignoreListenerPause = true;
        menuMusicSource.spatialBlend = 0f;
        menuMusicSource.volume = menuMusicVolume;

        if (menuMusicClip != null)
        {
            menuMusicSource.clip = menuMusicClip;
        }
    }

    void SetMenuMusicPlaying(bool isPlaying)
    {
        if (menuMusicSource == null || menuMusicSource.clip == null)
        {
            return;
        }

        menuMusicSource.volume = menuMusicVolume;

        if (isPlaying)
        {
            if (!menuMusicSource.isPlaying)
            {
                menuMusicSource.Play();
            }
        }
        else if (menuMusicSource.isPlaying)
        {
            menuMusicSource.Stop();
        }
    }

    void RefreshMenuVisualState()
    {
        bool isPauseMenu = hasStartedGame;

        if (pauseBackdrop != null)
        {
            pauseBackdrop.SetActive(isPauseMenu);
        }

        if (pauseFocusArea != null)
        {
            pauseFocusArea.SetActive(isPauseMenu);
        }

        if (newGameButton != null)
        {
            newGameButton.SetActive(isPauseMenu);
        }

        if (whisperText != null)
        {
            whisperText.gameObject.SetActive(!isPauseMenu);
        }

        RefreshMenuButtonLayout(isPauseMenu);
    }

    void RefreshMenuButtonLayout(bool isPauseMenu)
    {
        SetMenuButtonPosition(playButton, new Vector2(0f, -346f));

        if (isPauseMenu)
        {
            SetMenuButtonPosition(newGameButton, new Vector2(0f, -396f));
            SetMenuButtonPosition(optionsButton, new Vector2(0f, -446f));
            SetMenuButtonPosition(extrasButton, new Vector2(0f, -496f));
            SetMenuButtonPosition(quitButton, new Vector2(0f, -546f));
            SetPanelSize(pauseFocusArea, new Vector2(420f, 352f));
        }
        else
        {
            SetMenuButtonPosition(optionsButton, new Vector2(0f, -404f));
            SetMenuButtonPosition(extrasButton, new Vector2(0f, -454f));
            SetMenuButtonPosition(quitButton, new Vector2(0f, -504f));
            SetPanelSize(pauseFocusArea, new Vector2(420f, 300f));
        }
    }

    void SetMenuButtonPosition(GameObject button, Vector2 position)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = position;
        }
    }

    void SetPanelSize(GameObject panel, Vector2 size)
    {
        if (panel == null)
        {
            return;
        }

        RectTransform rect = panel.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = size;
        }
    }

    void TuneSceneAtmosphere()
    {
        if (!applySceneAtmosphere)
        {
            return;
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = outdoorFogColor;
        RenderSettings.fogDensity = outdoorFogDensity;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.05f, 0.05f, 0.06f, 1f);

        Light[] sceneLights = FindObjectsOfType<Light>(true);
        foreach (Light sceneLight in sceneLights)
        {
            TuneSceneLight(sceneLight);
        }
    }

    void TuneSceneLight(Light sceneLight)
    {
        if (sceneLight == null)
        {
            return;
        }

        Vector3 position = sceneLight.transform.position;
        string lightName = sceneLight.name.ToLowerInvariant();

        bool isRogueLight =
            Mathf.Abs(position.x) > rogueLightBounds.x ||
            Mathf.Abs(position.y) > rogueLightBounds.y ||
            Mathf.Abs(position.z) > rogueLightBounds.z;

        if (isRogueLight || lightName.StartsWith("cube"))
        {
            sceneLight.enabled = false;
            return;
        }

        if (lightName.StartsWith("pointlight_bounce1"))
        {
            sceneLight.intensity *= bounceLightIntensityMultiplier;
            sceneLight.range *= bounceLightRangeMultiplier;
            sceneLight.color = Color.Lerp(sceneLight.color, new Color(0.28f, 0.34f, 0.40f, 1f), 0.35f);
            return;
        }

        if (sceneLight.type == LightType.Point && sceneLight.range <= 8f && sceneLight.intensity >= 1.2f)
        {
            sceneLight.intensity *= 0.72f;
            sceneLight.range *= 0.9f;
            sceneLight.color = Color.Lerp(sceneLight.color, new Color(0.88f, 0.48f, 0.20f, 1f), 0.2f);
        }

        if (sceneLight.type == LightType.Spot && lightName.Contains("spot light"))
        {
            sceneLight.intensity *= 0.65f;
            sceneLight.spotAngle = Mathf.Min(sceneLight.spotAngle, 42f);
        }
    }

    Image CreateImage(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 size)
    {
        Image image = CreateUiObject(name, parent).AddComponent<Image>();
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        image.sprite = sprite;
        image.preserveAspect = true;
        return image;
    }

    Image CreateFullImage(string name, Transform parent, Sprite sprite, Color color)
    {
        Image image = CreateUiObject(name, parent).AddComponent<Image>();
        StretchFull(image.rectTransform);
        image.sprite = sprite != null ? sprite : whiteSprite;
        image.color = color;
        return image;
    }

    Image CreateFullPanel(string name, Transform parent, Color color)
    {
        Image image = CreateUiObject(name, parent).AddComponent<Image>();
        StretchFull(image.rectTransform);
        image.sprite = whiteSprite;
        image.color = color;
        return image;
    }

    GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panel = CreateUiObject(name, parent);
        Image image = panel.AddComponent<Image>();
        image.sprite = whiteSprite;
        image.color = color;
        return panel;
    }

    void AddEvent(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(_ => action.Invoke());
        trigger.triggers.Add(entry);
    }

    void AddOutline(GameObject go, Color color, Vector2 distance)
    {
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
    }

    void AddShadow(GameObject go, Color color, Vector2 distance)
    {
        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
    }

    GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;
        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(eventSystemObject);
    }

    void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    Text CreateText(string name, Transform parent, string content, int fontSize, FontStyle style, Color color, TextAnchor alignment, Vector2 pos, Vector2 size, bool heavy)
    {
        GameObject go = CreateUiObject(name, parent);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        Text text = go.AddComponent<Text>();
        text.font = uiFont;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = heavy ? new Color(0f, 0f, 0f, 0.8f) : new Color(0f, 0f, 0f, 0.40f);
        shadow.effectDistance = heavy ? new Vector2(3f, -3f) : new Vector2(1f, -1f);
        return text;
    }

    Text CreateTopLeftText(string name, Transform parent, string content, int fontSize, FontStyle style, Color color, TextAnchor alignment, Vector2 pos, Vector2 size, bool heavy)
    {
        GameObject go = CreateUiObject(name, parent);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        Text text = go.AddComponent<Text>();
        text.font = uiFont;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = heavy ? new Color(0f, 0f, 0f, 0.8f) : new Color(0f, 0f, 0f, 0.35f);
        shadow.effectDistance = heavy ? new Vector2(3f, -3f) : new Vector2(1f, -1f);
        return text;
    }

    IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < 2.2f)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, elapsed / 2.2f);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    IEnumerator WhisperLoop()
    {
        string[] lines = { "NO ESTAS SOLO", "ALGO TE OBSERVA", "NO MIRES DETRAS DE TI", "SIGUE RESPIRANDO", "NO DEBISTE ENTRAR" };
        while (true)
        {
            if (whisperText != null) whisperText.text = lines[Random.Range(0, lines.Length)];
            yield return new WaitForSecondsRealtime(Random.Range(3f, 6f));
        }
    }

    Font LoadUiFont()
    {
        try
        {
            Font builtIn = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (builtIn != null) return builtIn;
        }
        catch { }
        return Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Tahoma", "Arial" }, 16);
    }

    Sprite CreateWhiteSprite()
    {
        return Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), new Vector2(0.5f, 0.5f));
    }

    void EnsureMenuCameraRig()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (gameObject == null ||
                EditorUtility.IsPersistent(gameObject) ||
                PrefabUtility.IsPartOfPrefabAsset(gameObject))
            {
                return;
            }
        }
#endif

        menuCameraRigRoot = transform.Find("MenuCameraRig");
        if (menuCameraRigRoot == null)
        {
            GameObject rigObject = new GameObject("MenuCameraRig", typeof(Transform));
            rigObject.transform.SetParent(transform, false);
            rigObject.transform.localPosition = new Vector3(-12f, 5f, -10f);
            rigObject.transform.localRotation = Quaternion.identity;
            menuCameraRigRoot = rigObject.transform;
        }

        menuCameraTarget = menuCameraRigRoot.Find("MenuCameraTarget");
        if (menuCameraTarget == null)
        {
            GameObject targetObject = new GameObject("MenuCameraTarget", typeof(Transform));
            targetObject.transform.SetParent(menuCameraRigRoot, false);
            targetObject.transform.localPosition = new Vector3(0f, 1.6f, 6f);
            targetObject.transform.localRotation = Quaternion.identity;
            menuCameraTarget = targetObject.transform;
        }

        Transform cameraTransform = menuCameraRigRoot.Find("MenuBackgroundCamera");
        if (cameraTransform == null)
        {
            GameObject cameraObject = new GameObject("MenuBackgroundCamera", typeof(Camera), typeof(MenuSceneCameraMotion));
            cameraObject.transform.SetParent(menuCameraRigRoot, false);
            cameraObject.transform.localPosition = Vector3.zero;
            cameraObject.transform.localRotation = Quaternion.identity;
            cameraTransform = cameraObject.transform;
        }

        menuBackgroundCamera = cameraTransform.GetComponent<Camera>();
        if (menuBackgroundCamera == null)
        {
            menuBackgroundCamera = cameraTransform.gameObject.AddComponent<Camera>();
        }

        menuBackgroundCamera.tag = "Untagged";
        menuBackgroundCamera.enabled = false;
        menuBackgroundCamera.clearFlags = CameraClearFlags.Skybox;
        menuBackgroundCamera.orthographic = false;
        menuBackgroundCamera.fieldOfView = 48f;
        menuBackgroundCamera.nearClipPlane = 0.1f;
        menuBackgroundCamera.farClipPlane = 1000f;
        menuBackgroundCamera.depth = 5f;
        menuBackgroundCamera.allowHDR = true;
        menuBackgroundCamera.allowMSAA = true;

        AudioListener menuListener = cameraTransform.GetComponent<AudioListener>();
        if (menuListener != null)
        {
            menuListener.enabled = false;
        }

        MenuSceneCameraMotion motion = cameraTransform.GetComponent<MenuSceneCameraMotion>();
        if (motion == null)
        {
            motion = cameraTransform.gameObject.AddComponent<MenuSceneCameraMotion>();
        }

        motion.LookTarget = menuCameraTarget;
    }

    void SetMenuSceneCameraActive(bool isActive)
    {
        bool useBackgroundCameraForThisMenu = useSceneBackgroundCamera && (!hasStartedGame || !keepCurrentViewOnPause);

        if (!useBackgroundCameraForThisMenu)
        {
            if (menuBackgroundCamera != null)
            {
                menuBackgroundCamera.enabled = false;
            }

            if (gameplayCamera != null)
            {
                gameplayCamera.enabled = true;
            }

            if (gameplayAudioListener != null)
            {
                gameplayAudioListener.enabled = true;
            }

            if (gameplayCinemachineBrain != null)
            {
                gameplayCinemachineBrain.enabled = true;
            }

            return;
        }

        EnsureMenuCameraRig();

        if (menuBackgroundCamera != null)
        {
            menuBackgroundCamera.enabled = isActive;
        }

        if (gameplayCamera != null)
        {
            gameplayCamera.enabled = !isActive;
        }

        if (gameplayAudioListener != null)
        {
            gameplayAudioListener.enabled = !isActive;
        }

        if (gameplayCinemachineBrain != null)
        {
            gameplayCinemachineBrain.enabled = !isActive;
        }
    }

    void SetHudVisible(bool visible)
    {
        if (notesHudRoot != null)
        {
            notesHudRoot.SetActive(visible);
        }

        if (interactHudRoot != null)
        {
            interactHudRoot.SetActive(visible);
        }

        if (messageHudRoot != null)
        {
            messageHudRoot.SetActive(visible);
        }

        if (fpsHudRoot != null)
        {
            fpsHudRoot.SetActive(visible);
        }

        if (ObjectiveSystem.HasInstance)
        {
            ObjectiveSystem.Instance.SetHudVisible(visible);
        }
    }

    void CacheGameplayHudRoots()
    {
        notesHudRoot = GameObject.Find("CanvasNotas");
        interactHudRoot = GameObject.Find("InteractText");
        messageHudRoot = GameObject.Find("Mensajes");
        fpsHudRoot = GameObject.Find("FPSDisplay");
    }

#if UNITY_EDITOR
    void Reset()
    {
        AssignDefaultMenuMusic();
    }

    void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        menuMusicVolume = Mathf.Clamp(menuMusicVolume, 0f, 1f);
        AssignDefaultMenuMusic();
        EditorApplication.delayCall -= DelayedEnsureMenuRig;
        EditorApplication.delayCall += DelayedEnsureMenuRig;
    }

    void DelayedEnsureMenuRig()
    {
        if (this == null || gameObject == null)
        {
            return;
        }

        if (EditorUtility.IsPersistent(gameObject) || PrefabUtility.IsPartOfPrefabAsset(gameObject))
        {
            return;
        }

        EnsureMenuCameraRig();
    }

    void AssignDefaultMenuMusic()
    {
        if (menuMusicClip != null)
        {
            return;
        }

        menuMusicClip = AssetDatabase.LoadAssetAtPath<AudioClip>(DefaultMenuMusicPath);
    }
#endif
}

static class RectTransformMenuExtensions
{
    public static void SetAnchored(this RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
