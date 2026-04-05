using System.Collections;
using UnityEngine;

public class NotesMenuUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject notesMenu;
    public CanvasGroup menuCanvasGroup;
    public RectTransform panelContainer;

    [Header("Configuración")]
    public KeyCode toggleKey = KeyCode.Tab;
    public bool freezeGameWhenOpen = true;
    public bool showCursorWhenOpen = true;

    [Header("Animación")]
    public float openDuration = 0.25f;
    public float closeDuration = 0.2f;
    public Vector3 closedScale = new Vector3(0.9f, 0.9f, 0.9f);
    public Vector3 openScale = Vector3.one;

    private bool isOpen = false;
    private bool isAnimating = false;
    private Coroutine animationCoroutine;

    void Start()
    {
        ImmediateCloseState();
    }

    void Update()
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

        while (time < openDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / openDuration);

            // Ease Out
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            menuCanvasGroup.alpha = Mathf.Lerp(0f, 1f, eased);
            panelContainer.localScale = Vector3.Lerp(closedScale, openScale, eased);

            yield return null;
        }

        menuCanvasGroup.alpha = 1f;
        panelContainer.localScale = openScale;

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

        while (time < closeDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / closeDuration);

            // Ease In
            float eased = t * t;

            menuCanvasGroup.alpha = Mathf.Lerp(1f, 0f, eased);
            panelContainer.localScale = Vector3.Lerp(openScale, closedScale, eased);

            yield return null;
        }

        menuCanvasGroup.alpha = 0f;
        panelContainer.localScale = closedScale;

        notesMenu.SetActive(false);

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
            panelContainer.localScale = closedScale;

        if (notesMenu != null)
            notesMenu.SetActive(false);

        Time.timeScale = 1f;
    }
}