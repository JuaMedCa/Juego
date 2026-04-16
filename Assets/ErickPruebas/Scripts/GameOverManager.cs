using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GameOverManager : MonoBehaviour
{
    public static GameOverManager instance;

    [Header("Referencias")]
    public GameObject gameOverPanel;
    public AudioSource screamAudio;

    [Header("Textos")]
    [SerializeField] private string gameOverTitle = "GAME OVER";
    [SerializeField] private string gameOverSubtitle = "La oscuridad te encontro antes de que pudieras escapar.";
    [SerializeField] private string gameOverHint = "Respira hondo, ajusta tu ruta y vuelve a intentarlo.";
    [SerializeField] private string restartButtonLabel = "NUEVA PARTIDA";
    [SerializeField] private string ambienceLabel = "SENAL PERDIDA // REINTENTO DISPONIBLE";

    [Header("Animacion")]
    [SerializeField] private float fadeDuration = 0.35f;

    [Header("Estilo")]
    [SerializeField] private Color backdropColor = new Color(0.02f, 0.01f, 0.02f, 0.9f);
    [SerializeField] private Color backdropGlowColor = new Color(0.42f, 0.04f, 0.06f, 0.14f);
    [SerializeField] private Color cardColor = new Color(0.11f, 0.12f, 0.14f, 0.97f);
    [SerializeField] private Color cardShadowColor = new Color(0f, 0f, 0f, 0.58f);
    [SerializeField] private Color borderColor = new Color(1f, 1f, 1f, 0.08f);
    [SerializeField] private Color accentColor = new Color(0.72f, 0.08f, 0.11f, 0.95f);
    [SerializeField] private Color titleColor = new Color(0.96f, 0.95f, 0.93f, 1f);
    [SerializeField] private Color subtitleColor = new Color(0.82f, 0.81f, 0.78f, 1f);
    [SerializeField] private Color hintColor = new Color(0.66f, 0.66f, 0.63f, 1f);

    private bool isGameOver;
    private CanvasGroup panelCanvasGroup;
    private Button restartButton;
    private Text restartButtonText;
    private Font uiFont;
    private Sprite whiteSprite;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        uiFont = LoadUiFont();
        whiteSprite = CreateWhiteSprite();
        EnsureGameOverUi();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public void TriggerGameOver()
    {
        if (isGameOver)
        {
            return;
        }

        isGameOver = true;
        AudioListener.pause = false;
        EnsureGameOverUi();

        if (screamAudio != null)
        {
            screamAudio.Play();
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            gameOverPanel.transform.SetAsLastSibling();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(FadeGameOverIn());
        SelectRestartButton();
    }

    public void RestartGame()
    {
        GameSessionRestart.RestartCurrentScene();
    }

    private IEnumerator FadeGameOverIn()
    {
        if (panelCanvasGroup == null)
        {
            yield break;
        }

        panelCanvasGroup.alpha = 0f;

        if (fadeDuration <= 0f)
        {
            panelCanvasGroup.alpha = 1f;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            panelCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }

        panelCanvasGroup.alpha = 1f;
    }

    private void EnsureGameOverUi()
    {
        if (gameOverPanel == null)
        {
            gameOverPanel = new GameObject("GameOverPanel", typeof(RectTransform));
        }

        RectTransform rootRect = GetOrAddComponent<RectTransform>(gameOverPanel);
        rootRect.SetParent(null, false);
        StretchRect(rootRect);

        Canvas canvas = GetOrAddComponent<Canvas>(gameOverPanel);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 4000;

        CanvasScaler scaler = GetOrAddComponent<CanvasScaler>(gameOverPanel);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GetOrAddComponent<GraphicRaycaster>(gameOverPanel);

        panelCanvasGroup = GetOrAddComponent<CanvasGroup>(gameOverPanel);
        panelCanvasGroup.interactable = true;
        panelCanvasGroup.blocksRaycasts = true;

        RectTransform layoutRoot = EnsureRectTransform(gameOverPanel.transform, "GameOverLayout");
        HideLegacyChildren(layoutRoot);

        BuildBackdrop(layoutRoot);
        BuildCard(layoutRoot);
    }

    private void HideLegacyChildren(Transform layoutRoot)
    {
        for (int i = 0; i < gameOverPanel.transform.childCount; i++)
        {
            Transform child = gameOverPanel.transform.GetChild(i);
            child.gameObject.SetActive(child == layoutRoot);
        }
    }

    private void BuildBackdrop(RectTransform layoutRoot)
    {
        Image backdrop = EnsureImage(layoutRoot, "Backdrop");
        StretchRect(backdrop.rectTransform);
        backdrop.sprite = whiteSprite;
        backdrop.color = backdropColor;
        backdrop.raycastTarget = true;

        Image glow = EnsureImage(layoutRoot, "BackdropGlow");
        StretchRect(glow.rectTransform);
        glow.sprite = whiteSprite;
        glow.color = backdropGlowColor;
        glow.raycastTarget = false;

        Image vignetteTop = EnsureImage(layoutRoot, "VignetteTop");
        ConfigureRect(vignetteTop.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(2200f, 420f));
        vignetteTop.sprite = whiteSprite;
        vignetteTop.color = new Color(0f, 0f, 0f, 0.18f);
        vignetteTop.raycastTarget = false;

        Image vignetteBottom = EnsureImage(layoutRoot, "VignetteBottom");
        ConfigureRect(vignetteBottom.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(2200f, 420f));
        vignetteBottom.sprite = whiteSprite;
        vignetteBottom.color = new Color(0f, 0f, 0f, 0.26f);
        vignetteBottom.raycastTarget = false;
    }

    private void BuildCard(RectTransform layoutRoot)
    {
        RectTransform cardRect = EnsureRectTransform(layoutRoot, "Card");
        ConfigureRect(cardRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(860f, 470f));

        Image cardImage = GetOrAddComponent<Image>(cardRect.gameObject);
        cardImage.sprite = whiteSprite;
        cardImage.color = cardColor;
        cardImage.raycastTarget = true;

        Outline outline = GetOrAddComponent<Outline>(cardRect.gameObject);
        outline.effectColor = borderColor;
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        Shadow shadow = GetOrAddComponent<Shadow>(cardRect.gameObject);
        shadow.effectColor = cardShadowColor;
        shadow.effectDistance = new Vector2(0f, -14f);

        Image accentBar = EnsureImage(cardRect, "AccentBar");
        ConfigureRect(accentBar.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(10f, 0f));
        accentBar.sprite = whiteSprite;
        accentBar.color = accentColor;
        accentBar.raycastTarget = false;

        Image topRule = EnsureImage(cardRect, "TopRule");
        ConfigureRect(topRule.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(300f, 2f));
        topRule.sprite = whiteSprite;
        topRule.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.55f);
        topRule.raycastTarget = false;

        Text signalText = EnsureText(cardRect, "SignalLabel");
        ConfigureRect(signalText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(640f, 24f));
        ConfigureText(signalText, ambienceLabel, 16, FontStyle.Bold, new Color(0.80f, 0.77f, 0.73f, 0.78f));

        Text titleText = EnsureText(cardRect, "Title");
        ConfigureRect(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(640f, 90f));
        ConfigureText(titleText, gameOverTitle, 64, FontStyle.Bold, titleColor, true);

        Text subtitleText = EnsureText(cardRect, "Subtitle");
        ConfigureRect(subtitleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 34f), new Vector2(620f, 80f));
        ConfigureText(subtitleText, gameOverSubtitle, 24, FontStyle.Normal, subtitleColor);
        subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        subtitleText.verticalOverflow = VerticalWrapMode.Overflow;

        Text hintText = EnsureText(cardRect, "Hint");
        ConfigureRect(hintText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -36f), new Vector2(560f, 40f));
        ConfigureText(hintText, gameOverHint, 19, FontStyle.Italic, hintColor);

        BuildRestartButton(cardRect);
    }

    private void BuildRestartButton(RectTransform cardRect)
    {
        RectTransform buttonRect = EnsureRectTransform(cardRect, "RestartButton");
        ConfigureRect(buttonRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 56f), new Vector2(320f, 40f));

        Image buttonImage = GetOrAddComponent<Image>(buttonRect.gameObject);
        buttonImage.sprite = whiteSprite;
        buttonImage.color = new Color(0.36f, 0.08f, 0.10f, 0.82f);
        buttonImage.raycastTarget = true;

        Outline buttonOutline = GetOrAddComponent<Outline>(buttonRect.gameObject);
        buttonOutline.effectColor = new Color(0.92f, 0.66f, 0.66f, 0.22f);
        buttonOutline.effectDistance = new Vector2(1f, -1f);

        Shadow buttonShadow = GetOrAddComponent<Shadow>(buttonRect.gameObject);
        buttonShadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
        buttonShadow.effectDistance = new Vector2(0f, -4f);

        restartButton = GetOrAddComponent<Button>(buttonRect.gameObject);
        restartButton.targetGraphic = buttonImage;
        restartButton.transition = Selectable.Transition.None;
        restartButton.onClick.RemoveAllListeners();
        restartButton.onClick.AddListener(RestartGame);

        restartButtonText = EnsureText(buttonRect, "Label");
        ConfigureRect(restartButtonText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        ConfigureText(restartButtonText, restartButtonLabel, 18, FontStyle.Bold, titleColor);

        EventTrigger trigger = GetOrAddComponent<EventTrigger>(buttonRect.gameObject);
        trigger.triggers.Clear();
        AddEvent(trigger, EventTriggerType.PointerEnter, () => SetRestartButtonHovered(true));
        AddEvent(trigger, EventTriggerType.PointerExit, () => SetRestartButtonHovered(false));
        AddEvent(trigger, EventTriggerType.Select, () => SetRestartButtonHovered(true));
        AddEvent(trigger, EventTriggerType.Deselect, () => SetRestartButtonHovered(false));
        SetRestartButtonHovered(false);
    }

    private void SetRestartButtonHovered(bool hovered)
    {
        if (restartButton == null)
        {
            return;
        }

        Image buttonImage = restartButton.GetComponent<Image>();
        RectTransform rect = restartButton.GetComponent<RectTransform>();
        if (buttonImage != null)
        {
            buttonImage.color = hovered
                ? new Color(0.62f, 0.05f, 0.07f, 0.94f)
                : new Color(0.36f, 0.08f, 0.10f, 0.82f);
        }

        if (rect != null)
        {
            rect.localScale = hovered ? new Vector3(1.03f, 1.03f, 1f) : Vector3.one;
        }

        if (restartButtonText != null)
        {
            restartButtonText.color = hovered ? Color.white : titleColor;
        }
    }

    private void SelectRestartButton()
    {
        EnsureEventSystem();

        if (EventSystem.current != null && restartButton != null)
        {
            EventSystem.current.SetSelectedGameObject(restartButton.gameObject);
        }
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystem.transform.SetParent(null, false);
    }

    private RectTransform EnsureRectTransform(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            return existing as RectTransform;
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
        return GetOrAddComponent<Image>(rect.gameObject);
    }

    private Text EnsureText(Transform parent, string objectName)
    {
        RectTransform rect = EnsureRectTransform(parent, objectName);
        return GetOrAddComponent<Text>(rect.gameObject);
    }

    private void ConfigureText(Text text, string content, int fontSize, FontStyle style, Color color, bool heavyShadow = false)
    {
        text.font = uiFont;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        Shadow shadow = GetOrAddComponent<Shadow>(text.gameObject);
        shadow.effectColor = heavyShadow ? new Color(0f, 0f, 0f, 0.65f) : new Color(0f, 0f, 0f, 0.38f);
        shadow.effectDistance = heavyShadow ? new Vector2(3f, -3f) : new Vector2(1f, -1f);
    }

    private void ConfigureRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        rect.localScale = Vector3.one;
    }

    private void StretchRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private void AddEvent(EventTrigger trigger, EventTriggerType eventType, UnityEngine.Events.UnityAction action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
        entry.callback.AddListener(_ => action.Invoke());
        trigger.triggers.Add(entry);
    }

    private Font LoadUiFont()
    {
        try
        {
            Font builtIn = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (builtIn != null)
            {
                return builtIn;
            }
        }
        catch
        {
        }

        return Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Tahoma", "Arial" }, 16);
    }

    private Sprite CreateWhiteSprite()
    {
        return Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(0.5f, 0.5f));
    }

    private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }

        return component;
    }
}
