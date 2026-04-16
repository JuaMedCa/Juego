using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TutorialHintOverlay : MonoBehaviour
{
    private const string RuntimeObjectName = "TutorialHintOverlay";

    private static TutorialHintOverlay instance;

    private CanvasGroup canvasGroup;
    private Text messageText;
    private Coroutine currentRoutine;

    public static void ShowHint(string message, float duration = 3f)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        EnsureInstance().Display(message.Trim(), duration);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        EnsureInstance();
    }

    private static TutorialHintOverlay EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        GameObject existing = GameObject.Find(RuntimeObjectName);
        if (existing != null)
        {
            instance = existing.GetComponent<TutorialHintOverlay>();
            if (instance != null)
            {
                return instance;
            }
        }

        GameObject root = new GameObject(RuntimeObjectName);
        instance = root.AddComponent<TutorialHintOverlay>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildOverlay();
    }

    private void Display(string message, float duration)
    {
        BuildOverlay();

        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        currentRoutine = StartCoroutine(ShowRoutine(message, Mathf.Max(1f, duration)));
    }

    private IEnumerator ShowRoutine(string message, float duration)
    {
        messageText.text = message;
        gameObject.SetActive(true);

        yield return Fade(0f, 1f, 0.18f);
        yield return new WaitForSecondsRealtime(duration);
        yield return Fade(1f, 0f, 0.25f);

        currentRoutine = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (canvasGroup == null)
        {
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

    private void BuildOverlay()
    {
        if (canvasGroup != null && messageText != null)
        {
            return;
        }

        Canvas canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;

        CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        RectTransform rootRect = gameObject.GetComponent<RectTransform>();
        Stretch(rootRect);

        GameObject textObject = new GameObject("HintText", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0f);
        textRect.anchorMax = new Vector2(0.5f, 0f);
        textRect.pivot = new Vector2(0.5f, 0f);
        textRect.anchoredPosition = new Vector2(0f, 132f);
        textRect.sizeDelta = new Vector2(1200f, 110f);

        messageText = textObject.GetComponent<Text>();
        messageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        messageText.fontSize = 30;
        messageText.fontStyle = FontStyle.Bold;
        messageText.alignment = TextAnchor.MiddleCenter;
        messageText.color = new Color(0.96f, 0.94f, 0.88f, 1f);
        messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
        messageText.verticalOverflow = VerticalWrapMode.Overflow;
        messageText.raycastTarget = false;

        Shadow textShadow = textObject.AddComponent<Shadow>();
        textShadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
        textShadow.effectDistance = new Vector2(2f, -2f);
    }

    private static void Stretch(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }
}
