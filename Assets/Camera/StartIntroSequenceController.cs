using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class StartIntroSequenceController : MonoBehaviour
{
    const string ResourcePath = "StartIntroSequence";
    const int OverlaySortingOrder = 5000;
    const float SkipHoldDuration = 4f;

    StartIntroSequenceDefinition definition;
    GameObject overlayRoot;
    CanvasGroup overlayCanvasGroup;
    CanvasGroup contentCanvasGroup;
    Image backdropImage;
    Image slideImage;
    Text subtitleText;
    Text skipStatusText;
    AudioSource narrationSource;
    Camera introCamera;
    AudioListener introAudioListener;
    Font uiFont;
    Sprite currentSlideSprite;
    Coroutine playRoutine;
    bool hasCompleted;
    bool previousAudioPauseState;
    bool skipRequested;
    float skipHoldProgress;

    public static StartIntroSequenceController GetOrCreate()
    {
        StartIntroSequenceController existing = FindObjectOfType<StartIntroSequenceController>(true);
        if (existing != null)
        {
            return existing.EnsureInitialized() ? existing : null;
        }

        GameObject root = new GameObject("StartIntroSequenceController");
        StartIntroSequenceController controller = root.AddComponent<StartIntroSequenceController>();
        return controller.EnsureInitialized() ? controller : null;
    }

    public bool TryPlay(Action onComplete)
    {
        if (!EnsureInitialized() || definition == null || !definition.HasSteps || hasCompleted)
        {
            return false;
        }

        if (playRoutine != null)
        {
            return true;
        }

        playRoutine = StartCoroutine(PlaySequence(onComplete));
        return true;
    }

    void OnDestroy()
    {
        if (narrationSource != null)
        {
            narrationSource.Stop();
        }

        DestroyCurrentSlideSprite();
    }

    void Update()
    {
        MaintainIntroAudioState();
        UpdateSkipInput();
    }

    bool EnsureInitialized()
    {
        if (definition == null)
        {
            definition = Resources.Load<StartIntroSequenceDefinition>(ResourcePath);
        }

        if (definition == null)
        {
            return false;
        }

        if (uiFont == null)
        {
            uiFont = LoadUiFont();
        }

        EnsureOverlay();
        EnsureAudioSource();
        EnsureIntroCamera();
        return true;
    }

    void EnsureOverlay()
    {
        if (overlayRoot != null)
        {
            return;
        }

        overlayRoot = CreateUiObject("StartIntroOverlay", transform);
        Canvas canvas = overlayRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = OverlaySortingOrder;

        CanvasScaler scaler = overlayRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        overlayRoot.AddComponent<GraphicRaycaster>();

        overlayCanvasGroup = overlayRoot.AddComponent<CanvasGroup>();
        overlayCanvasGroup.alpha = 0f;
        overlayCanvasGroup.interactable = false;
        overlayCanvasGroup.blocksRaycasts = true;

        backdropImage = CreateFullImage("Backdrop", overlayRoot.transform, definition.BackdropColor);
        backdropImage.raycastTarget = true;

        GameObject contentRoot = CreateUiObject("Content", overlayRoot.transform);
        StretchFull(contentRoot.GetComponent<RectTransform>());
        contentCanvasGroup = contentRoot.AddComponent<CanvasGroup>();
        contentCanvasGroup.alpha = 0f;
        contentCanvasGroup.interactable = false;
        contentCanvasGroup.blocksRaycasts = false;

        slideImage = CreateFullImage("Slide", contentRoot.transform, Color.white);
        slideImage.preserveAspect = false;
        slideImage.raycastTarget = false;

        GameObject subtitlePanel = CreatePanel("SubtitlePanel", contentRoot.transform, new Color(0f, 0f, 0f, 0.68f));
        RectTransform subtitlePanelRect = subtitlePanel.GetComponent<RectTransform>();
        subtitlePanelRect.anchorMin = new Vector2(0.5f, 1f);
        subtitlePanelRect.anchorMax = new Vector2(0.5f, 1f);
        subtitlePanelRect.pivot = new Vector2(0.5f, 1f);
        subtitlePanelRect.anchoredPosition = new Vector2(0f, -820f);
        subtitlePanelRect.sizeDelta = new Vector2(1260f, 180f);

        subtitleText = CreateText("SubtitleText", subtitlePanel.transform, string.Empty, 24, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, -20f), new Vector2(1160f, 140f));
        subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        subtitleText.verticalOverflow = VerticalWrapMode.Overflow;
        subtitleText.lineSpacing = 1.1f;

        GameObject skipPanel = CreatePanel("SkipPanel", contentRoot.transform, new Color(0f, 0f, 0f, 0.68f));
        RectTransform skipPanelRect = skipPanel.GetComponent<RectTransform>();
        skipPanelRect.anchorMin = new Vector2(1f, 0f);
        skipPanelRect.anchorMax = new Vector2(1f, 0f);
        skipPanelRect.pivot = new Vector2(1f, 0f);
        skipPanelRect.anchoredPosition = new Vector2(-42f, 42f);
        skipPanelRect.sizeDelta = new Vector2(560f, 74f);

        skipStatusText = CreateText("SkipStatusText", skipPanel.transform, string.Empty, 18, FontStyle.Bold, new Color(1f, 0.9f, 0.6f), TextAnchor.MiddleCenter, new Vector2(0f, -13f), new Vector2(520f, 44f));

        UpdateSkipUi();

        overlayRoot.SetActive(false);
    }

    void EnsureAudioSource()
    {
        if (narrationSource != null)
        {
            return;
        }

        narrationSource = gameObject.GetComponent<AudioSource>();
        if (narrationSource == null)
        {
            narrationSource = gameObject.AddComponent<AudioSource>();
        }

        narrationSource.playOnAwake = false;
        narrationSource.loop = false;
        narrationSource.ignoreListenerPause = true;
        narrationSource.spatialBlend = 0f;
        narrationSource.volume = 1f;
        narrationSource.bypassEffects = true;
        narrationSource.bypassListenerEffects = true;
        narrationSource.bypassReverbZones = true;
        narrationSource.dopplerLevel = 0f;
        narrationSource.priority = 0;
    }

    void EnsureIntroCamera()
    {
        if (introCamera != null)
        {
            introCamera.backgroundColor = definition.BackdropColor;
            introCamera.transform.position = definition.IntroCameraPosition;

            if (introAudioListener == null)
            {
                introAudioListener = introCamera.GetComponent<AudioListener>();
                if (introAudioListener == null)
                {
                    introAudioListener = introCamera.gameObject.AddComponent<AudioListener>();
                }
            }

            introAudioListener.enabled = false;
            return;
        }

        GameObject cameraObject = new GameObject("StartIntroCamera", typeof(Camera));
        cameraObject.transform.SetParent(transform, false);
        cameraObject.transform.position = definition.IntroCameraPosition;
        cameraObject.transform.rotation = Quaternion.identity;

        introCamera = cameraObject.GetComponent<Camera>();
        introCamera.enabled = false;
        introCamera.clearFlags = CameraClearFlags.SolidColor;
        introCamera.backgroundColor = definition.BackdropColor;
        introCamera.cullingMask = 0;
        introCamera.nearClipPlane = 0.3f;
        introCamera.farClipPlane = 100f;
        introCamera.depth = 200f;
        introCamera.tag = "Untagged";

        introAudioListener = cameraObject.AddComponent<AudioListener>();
        introAudioListener.enabled = false;
    }

    IEnumerator PlaySequence(Action onComplete)
    {
        if (overlayRoot == null || overlayCanvasGroup == null || contentCanvasGroup == null)
        {
            playRoutine = null;
            yield break;
        }

        overlayRoot.SetActive(true);
        overlayCanvasGroup.alpha = 0f;
        contentCanvasGroup.alpha = 0f;
        overlayCanvasGroup.blocksRaycasts = true;
        skipRequested = false;
        skipHoldProgress = 0f;
        UpdateSkipUi();
        previousAudioPauseState = AudioListener.pause;
        AudioListener.pause = false;

        if (backdropImage != null)
        {
            backdropImage.color = definition.BackdropColor;
        }

        if (introCamera != null)
        {
            introCamera.backgroundColor = definition.BackdropColor;
            introCamera.transform.position = definition.IntroCameraPosition;
            introCamera.enabled = true;
        }

        if (introAudioListener != null)
        {
            introAudioListener.enabled = true;
        }

        yield return FadeCanvasGroup(overlayCanvasGroup, 0f, 1f, definition.FadeDuration);
        if (skipRequested)
        {
            yield return ExitSequence(onComplete);
            yield break;
        }

        yield return new WaitForSecondsRealtime(0.1f);

        for (int i = 0; i < definition.StepCount; i++)
        {
            ApplyStep(i);
            yield return FadeCanvasGroup(contentCanvasGroup, 0f, 1f, definition.FadeDuration);
            if (skipRequested)
            {
                break;
            }

            yield return PlayNarration(i);
            if (skipRequested)
            {
                break;
            }

            yield return FadeCanvasGroup(contentCanvasGroup, 1f, 0f, definition.FadeDuration * 0.75f);
            if (skipRequested)
            {
                break;
            }

            if (i < definition.StepCount - 1 && definition.BetweenSlidesDelay > 0f)
            {
                float delayElapsed = 0f;
                while (delayElapsed < definition.BetweenSlidesDelay && !skipRequested)
                {
                    delayElapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
        }

        yield return ExitSequence(onComplete);
    }

    IEnumerator ExitSequence(Action onComplete)
    {
        if (narrationSource != null && narrationSource.isPlaying)
        {
            narrationSource.Stop();
        }

        float fromAlpha = overlayCanvasGroup != null ? overlayCanvasGroup.alpha : 0f;
        yield return FadeCanvasGroup(overlayCanvasGroup, fromAlpha, 0f, definition.FadeDuration);
        CleanupPresentation();

        hasCompleted = true;
        playRoutine = null;
        onComplete?.Invoke();
    }

    void ApplyStep(int index)
    {
        SetSlideTexture(definition.GetSlideTexture(index));

        if (subtitleText != null)
        {
            subtitleText.text = definition.GetSubtitle(index);
        }
    }

    void CleanupPresentation()
    {
        if (overlayRoot != null)
        {
            overlayRoot.SetActive(false);
        }

        if (introCamera != null)
        {
            introCamera.enabled = false;
        }

        if (introAudioListener != null)
        {
            introAudioListener.enabled = false;
        }

        if (subtitleText != null)
        {
            subtitleText.text = string.Empty;
        }

        skipRequested = false;
        skipHoldProgress = 0f;
        UpdateSkipUi();
        SetSlideTexture(null);
        AudioListener.pause = previousAudioPauseState;
    }

    void SetSlideTexture(Texture2D texture)
    {
        if (slideImage == null)
        {
            return;
        }

        DestroyCurrentSlideSprite();

        if (texture == null)
        {
            slideImage.sprite = null;
            slideImage.color = Color.clear;
            return;
        }

        currentSlideSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f));

        slideImage.sprite = currentSlideSprite;
        slideImage.color = Color.white;
    }

    void DestroyCurrentSlideSprite()
    {
        if (currentSlideSprite == null)
        {
            return;
        }

        Destroy(currentSlideSprite);
        currentSlideSprite = null;
    }

    IEnumerator PlayNarration(int index)
    {
        AudioClip clip = definition.GetNarrationClip(index);
        if (clip == null || narrationSource == null)
        {
            yield return WaitForRealtimeOrSkip(definition.FallbackStepDuration);
            yield break;
        }

        if (clip.loadState == AudioDataLoadState.Unloaded)
        {
            clip.LoadAudioData();
        }

        float loadElapsed = 0f;
        while (clip.loadState == AudioDataLoadState.Loading && loadElapsed < 3f)
        {
            if (skipRequested)
            {
                yield break;
            }

            loadElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (clip.loadState == AudioDataLoadState.Failed)
        {
            yield return WaitForRealtimeOrSkip(definition.FallbackStepDuration);
            yield break;
        }

        narrationSource.Stop();
        narrationSource.clip = clip;
        narrationSource.volume = definition.GetNarrationVolume(index);
        narrationSource.Play();

        float elapsed = 0f;
        float playbackDuration = clip.length > 0.05f ? clip.length : definition.FallbackStepDuration;
        while (elapsed < playbackDuration)
        {
            if (skipRequested)
            {
                narrationSource.Stop();
                yield break;
            }

            if (!narrationSource.isPlaying && elapsed < playbackDuration - 0.05f)
            {
                narrationSource.clip = clip;
                narrationSource.time = Mathf.Clamp(elapsed, 0f, Mathf.Max(0f, clip.length - 0.05f));
                narrationSource.UnPause();

                if (!narrationSource.isPlaying)
                {
                    narrationSource.Play();
                }
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    IEnumerator WaitForRealtimeOrSkip(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (skipRequested)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float from, float to, float duration)
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        canvasGroup.alpha = from;

        while (elapsed < duration)
        {
            if (skipRequested)
            {
                break;
            }

            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        canvasGroup.alpha = to;
    }

    void MaintainIntroAudioState()
    {
        if (playRoutine == null || overlayRoot == null || !overlayRoot.activeSelf)
        {
            return;
        }

        if (AudioListener.pause)
        {
            AudioListener.pause = false;
        }

        if (introAudioListener != null && !introAudioListener.enabled)
        {
            introAudioListener.enabled = true;
        }
    }

    void UpdateSkipInput()
    {
        if (playRoutine == null || overlayRoot == null || !overlayRoot.activeSelf)
        {
            if (skipHoldProgress > 0f || skipRequested)
            {
                skipRequested = false;
                skipHoldProgress = 0f;
                UpdateSkipUi();
            }

            return;
        }

        if (!skipRequested && Input.GetKey(KeyCode.Space))
        {
            skipHoldProgress = Mathf.Min(SkipHoldDuration, skipHoldProgress + Time.unscaledDeltaTime);
            if (skipHoldProgress >= SkipHoldDuration)
            {
                skipRequested = true;
            }
        }
        else if (!skipRequested)
        {
            skipHoldProgress = 0f;
        }

        UpdateSkipUi();
    }

    void UpdateSkipUi()
    {
        if (skipStatusText == null)
        {
            return;
        }

        if (skipRequested)
        {
            skipStatusText.text = "Saltando...";
            return;
        }

        if (skipHoldProgress > 0f)
        {
            skipStatusText.text = $"Manten presionado ESPACIO para saltar {skipHoldProgress:0.0}/{SkipHoldDuration:0.0}s";
            return;
        }

        skipStatusText.text = $"Manten presionado ESPACIO para saltar 0.0/{SkipHoldDuration:0.0}s";
    }

    Font LoadUiFont()
    {
        Font builtIn = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (builtIn != null)
        {
            return builtIn;
        }

        return Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Tahoma", "Arial" }, 16);
    }

    GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    Image CreateFullImage(string name, Transform parent, Color color)
    {
        Image image = CreateUiObject(name, parent).AddComponent<Image>();
        StretchFull(image.rectTransform);
        image.color = color;
        return image;
    }

    GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panel = CreateUiObject(name, parent);
        Image image = panel.AddComponent<Image>();
        image.color = color;
        StretchFull(panel.GetComponent<RectTransform>());
        return panel;
    }

    Text CreateText(string name, Transform parent, string content, int fontSize, FontStyle fontStyle, Color color, TextAnchor alignment, Vector2 position, Vector2 size)
    {
        GameObject textObject = CreateUiObject(name, parent);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text text = textObject.AddComponent<Text>();
        text.font = uiFont;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = alignment;

        Shadow shadow = textObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.68f);
        shadow.effectDistance = new Vector2(2f, -2f);

        return text;
    }

    void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
