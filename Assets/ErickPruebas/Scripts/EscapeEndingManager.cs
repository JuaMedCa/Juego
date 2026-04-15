using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class EscapeEndingManager : MonoBehaviour
{
    private static EscapeEndingManager instance;

    private Canvas endingCanvas;
    private GameObject endingRoot;
    private Font uiFont;
    private bool isEndingActive;

    public static bool IsEndingActive => instance != null && instance.isEndingActive;

    public static EscapeEndingManager EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindObjectOfType<EscapeEndingManager>();
        if (instance == null)
        {
            GameObject managerObject = new GameObject("EscapeEndingManager");
            instance = managerObject.AddComponent<EscapeEndingManager>();
        }

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
        uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public void TriggerEscape()
    {
        if (isEndingActive)
        {
            return;
        }

        EnsureUi();

        isEndingActive = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        MainMenuController menuController = FindObjectOfType<MainMenuController>();
        if (menuController != null)
        {
            menuController.enabled = false;
        }

        GameObject interactText = GameObject.Find("InteractText");
        if (interactText != null)
        {
            interactText.SetActive(false);
        }

        GameObject notesCounter = GameObject.Find("Txt_NotasContador");
        if (notesCounter != null)
        {
            notesCounter.SetActive(false);
        }

        ObjectiveSystem.EnsureInstance().SetHudVisible(false);
        endingRoot.SetActive(true);
    }

    private void EnsureUi()
    {
        if (endingRoot != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("EscapeEndingCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        endingCanvas = canvasObject.GetComponent<Canvas>();
        endingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        endingCanvas.sortingOrder = 400;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        endingRoot = CreatePanel("EscapeEndingRoot", canvasObject.transform, new Color(0.02f, 0.02f, 0.03f, 0.88f));
        RectTransform rootRect = endingRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        GameObject card = CreatePanel("EscapeEndingCard", endingRoot.transform, new Color(0.09f, 0.09f, 0.11f, 0.96f));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(560f, 360f);
        cardRect.anchoredPosition = Vector2.zero;

        CreateLabel("EscapeTitle", card.transform, "ESCAPASTE", 44, FontStyle.Bold, new Vector2(0f, 94f), new Vector2(420f, 70f), new Color(0.96f, 0.94f, 0.90f, 1f));
        CreateLabel("EscapeSubtitle", card.transform, "Lograste poner en marcha el jeep y salir de la zona.", 23, FontStyle.Normal, new Vector2(0f, 24f), new Vector2(460f, 90f), new Color(0.77f, 0.77f, 0.75f, 1f));

        CreateButton("RestartButton", card.transform, "REINICIAR", new Vector2(0f, -58f), RestartScene);
        CreateButton("QuitButton", card.transform, "SALIR", new Vector2(0f, -132f), QuitGame);

        endingRoot.SetActive(false);
    }

    private GameObject CreatePanel(string objectName, Transform parent, Color color)
    {
        GameObject panel = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        Image image = panel.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return panel;
    }

    private void CreateLabel(string objectName, Transform parent, string textValue, int fontSize, FontStyle fontStyle, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject labelObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        Text label = labelObject.GetComponent<Text>();
        label.font = uiFont;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = color;
        label.text = textValue;
    }

    private void CreateButton(string objectName, Transform parent, string buttonText, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(280f, 50f);
        rect.anchoredPosition = anchoredPosition;

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.42f, 0.08f, 0.10f, 0.95f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = buttonImage.color;
        colors.highlightedColor = new Color(0.62f, 0.10f, 0.12f, 1f);
        colors.pressedColor = new Color(0.28f, 0.05f, 0.06f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.25f, 0.25f, 0.25f, 0.5f);
        button.colors = colors;
        button.onClick.AddListener(action);

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(buttonObject.transform, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text label = textObject.GetComponent<Text>();
        label.font = uiFont;
        label.fontSize = 22;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.96f, 0.94f, 0.90f, 1f);
        label.text = buttonText;
    }

    private void RestartScene()
    {
        AudioListener.pause = false;
        Time.timeScale = 1f;

        Scene currentScene = SceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(currentScene.name))
        {
            SceneManager.LoadScene(currentScene.name);
        }
    }

    private void QuitGame()
    {
        AudioListener.pause = false;
        Time.timeScale = 1f;

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
