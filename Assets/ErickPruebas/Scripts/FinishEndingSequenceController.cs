using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FinishEndingSequenceController : MonoBehaviour
{
    private const string ResourcePath = "FinishEndingSequence";
    private const int OverlaySortingOrder = 5100;

    private StartIntroSequenceDefinition definition;
    private GameObject overlayRoot;
    private CanvasGroup overlayCanvasGroup;
    private CanvasGroup contentCanvasGroup;
    private Image backdropImage;
    private RawImage slideImage;
    private Text subtitleText;
    private AudioSource narrationSource;
    private Camera endingCamera;
    private AudioListener endingAudioListener;
    private Coroutine playRoutine;
    private bool hasCompleted;
    private bool previousAudioPauseState;

    public static FinishEndingSequenceController GetOrCreate()
    {
        FinishEndingSequenceController existing = FindObjectOfType<FinishEndingSequenceController>(true);
        if (existing != null)
        {
            return existing.EnsureInitialized() ? existing : null;
        }

        GameObject root = new GameObject("FinishEndingSequenceController");
        FinishEndingSequenceController controller = root.AddComponent<FinishEndingSequenceController>();
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

    private bool EnsureInitialized()
    {
        if (definition == null)
        {
            definition = Resources.Load<StartIntroSequenceDefinition>(ResourcePath);
        }

        if (definition == null)
        {
            return false;
        }

        EnsureOverlay();
        EnsureAudioSource();
        EnsureEndingCamera();
        return true;
    }

    private void OnDestroy()
    {
        if (narrationSource != null)
        {
            narrationSource.Stop();
        }
    }

    private void EnsureOverlay()
    {
        if (overlayRoot != null)
        {
            return;
        }

        overlayRoot = CreateUiObject("FinishEndingOverlay", transform);
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

        slideImage = CreateUiObject("Slide", contentRoot.transform).AddComponent<RawImage>();
        StretchFull(slideImage.rectTransform);
        slideImage.color = Color.white;
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

        overlayRoot.SetActive(false);
    }

    private void EnsureAudioSource()
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

    private void EnsureEndingCamera()
    {
        if (endingCamera != null)
        {
            endingCamera.backgroundColor = definition.BackdropColor;
            endingCamera.transform.position = definition.IntroCameraPosition;

            if (endingAudioListener == null)
            {
                endingAudioListener = endingCamera.GetComponent<AudioListener>();
                if (endingAudioListener == null)
                {
                    endingAudioListener = endingCamera.gameObject.AddComponent<AudioListener>();
                }
            }

            endingAudioListener.enabled = false;
            return;
        }

        GameObject cameraObject = new GameObject("FinishEndingCamera", typeof(Camera));
        cameraObject.transform.SetParent(transform, false);
        cameraObject.transform.position = definition.IntroCameraPosition;
        cameraObject.transform.rotation = Quaternion.identity;

        endingCamera = cameraObject.GetComponent<Camera>();
        endingCamera.enabled = false;
        endingCamera.clearFlags = CameraClearFlags.SolidColor;
        endingCamera.backgroundColor = definition.BackdropColor;
        endingCamera.cullingMask = 0;
        endingCamera.nearClipPlane = 0.3f;
        endingCamera.farClipPlane = 100f;
        endingCamera.depth = 200f;
        endingCamera.tag = "Untagged";

        endingAudioListener = cameraObject.AddComponent<AudioListener>();
        endingAudioListener.enabled = false;
    }

    private IEnumerator PlaySequence(Action onComplete)
    {
        overlayRoot.SetActive(true);
        overlayCanvasGroup.alpha = 0f;
        contentCanvasGroup.alpha = 0f;
        overlayCanvasGroup.blocksRaycasts = true;

        previousAudioPauseState = AudioListener.pause;
        AudioListener.pause = false;

        if (backdropImage != null)
        {
            backdropImage.color = definition.BackdropColor;
        }

        if (endingCamera != null)
        {
            endingCamera.backgroundColor = definition.BackdropColor;
            endingCamera.transform.position = definition.IntroCameraPosition;
            endingCamera.enabled = true;
        }

        if (endingAudioListener != null)
        {
            endingAudioListener.enabled = true;
        }

        yield return FadeCanvasGroup(overlayCanvasGroup, 0f, 1f, definition.FadeDuration);
        yield return new WaitForSecondsRealtime(0.1f);

        for (int i = 0; i < definition.StepCount; i++)
        {
            ApplyStep(i);
            yield return FadeCanvasGroup(contentCanvasGroup, 0f, 1f, definition.FadeDuration);
            yield return PlayNarration(i);
            yield return FadeCanvasGroup(contentCanvasGroup, 1f, 0f, definition.FadeDuration * 0.75f);

            if (i < definition.StepCount - 1 && definition.BetweenSlidesDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(definition.BetweenSlidesDelay);
            }
        }

        if (narrationSource != null && narrationSource.isPlaying)
        {
            narrationSource.Stop();
        }

        yield return FadeCanvasGroup(overlayCanvasGroup, 1f, 0f, definition.FadeDuration);
        CleanupPresentation();

        hasCompleted = true;
        playRoutine = null;
        onComplete?.Invoke();
    }

    private void ApplyStep(int index)
    {
        if (slideImage == null)
        {
            return;
        }

        slideImage.texture = definition.GetSlideTexture(index);
        slideImage.color = slideImage.texture != null ? Color.white : Color.clear;

        if (subtitleText != null)
        {
            subtitleText.text = definition.GetSubtitle(index);
        }
    }

    private void CleanupPresentation()
    {
        if (overlayRoot != null)
        {
            overlayRoot.SetActive(false);
        }

        if (endingCamera != null)
        {
            endingCamera.enabled = false;
        }

        if (endingAudioListener != null)
        {
            endingAudioListener.enabled = false;
        }

        if (slideImage != null)
        {
            slideImage.texture = null;
            slideImage.color = Color.clear;
        }

        if (subtitleText != null)
        {
            subtitleText.text = string.Empty;
        }

        AudioListener.pause = previousAudioPauseState;
    }

    private IEnumerator PlayNarration(int index)
    {
        AudioClip clip = definition.GetNarrationClip(index);
        if (clip == null || narrationSource == null)
        {
            yield return new WaitForSecondsRealtime(definition.FallbackStepDuration);
            yield break;
        }

        if (clip.loadState == AudioDataLoadState.Unloaded)
        {
            clip.LoadAudioData();
        }

        float loadElapsed = 0f;
        while (clip.loadState == AudioDataLoadState.Loading && loadElapsed < 3f)
        {
            loadElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (clip.loadState == AudioDataLoadState.Failed)
        {
            yield return new WaitForSecondsRealtime(definition.FallbackStepDuration);
            yield break;
        }

        narrationSource.Stop();
        narrationSource.clip = clip;
        narrationSource.volume = definition.GetNarrationVolume(index);
        narrationSource.Play();

        float elapsed = 0f;
        float maxDuration = Mathf.Max(definition.FallbackStepDuration, clip.length + 0.1f);
        while (elapsed < maxDuration)
        {
            if (!narrationSource.isPlaying && elapsed > 0.05f)
            {
                break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float from, float to, float duration)
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
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        canvasGroup.alpha = to;
    }

    private GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private Image CreateFullImage(string name, Transform parent, Color color)
    {
        Image image = CreateUiObject(name, parent).AddComponent<Image>();
        StretchFull(image.rectTransform);
        image.color = color;
        return image;
    }

    private GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panel = CreateUiObject(name, parent);
        Image image = panel.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    private Text CreateText(string name, Transform parent, string content, int fontSize, FontStyle fontStyle, Color color, TextAnchor alignment, Vector2 position, Vector2 size)
    {
        GameObject textObject = CreateUiObject(name, parent);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text text = textObject.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = alignment;

        Shadow shadow = textObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.68f);
        shadow.effectDistance = new Vector2(2f, -2f);
        return text;
    }

    private void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
