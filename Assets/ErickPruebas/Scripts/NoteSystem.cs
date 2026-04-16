using System.IO;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Video;

public class NoteSystem : MonoBehaviour
{
    [Header("UI")]
    public GameObject interactText;
    public GameObject notePanel;
    public TMP_Text noteText;

    [Header("Flujo")]
    [SerializeField] private bool openNoteReaderOnPickup = false;

    [Header("Estilo Nota")]
    [SerializeField] private Color noteBackdropColor = new Color(0.01f, 0.02f, 0.03f, 0.72f);
    [SerializeField] private Color notePaperColor = new Color(0.92f, 0.87f, 0.73f, 0.98f);
    [SerializeField] private Color notePaperShadowColor = new Color(0f, 0f, 0f, 0.35f);
    [SerializeField] private Color noteInkColor = new Color(0.16f, 0.12f, 0.08f, 1f);
    [SerializeField] private Color noteAccentColor = new Color(0.45f, 0.31f, 0.16f, 0.82f);
    [SerializeField] private Vector2 notePaperSize = new Vector2(740f, 860f);

    private InteractableNote currentNote;
    private InteractableNote openedNote;
    private InteractableNote activeVideoNote;
    private bool isReading = false;
    private bool isPlayingVideo = false;

    private GameObject videoOverlayRoot;
    private RawImage videoImage;
    private AspectRatioFitter videoAspectFitter;
    private VideoPlayer videoPlayer;
    private AudioSource videoAudioSource;
    private TMP_Text noteTitleText;
    private ScrollRect noteScrollRect;
    private Button noteCloseButton;
    private LayoutElement noteBodyLayoutElement;
    private bool noteUiPrepared;

    void Update()
    {
        if (isPlayingVideo)
        {
            return;
        }

        if (isReading)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
            {
                CloseNote();
            }
            return;
        }

        InteractableNote[] notes = FindObjectsOfType<InteractableNote>();
        currentNote = null;

        foreach (var note in notes)
        {
            if (note.playerInside)
            {
                currentNote = note;
                break;
            }
        }

        if (currentNote != null)
        {
            interactText.SetActive(true);
            interactText.GetComponent<TMP_Text>().text = openNoteReaderOnPickup
                ? "Presiona E para inspeccionar"
                : "Presiona E para recoger";

            if (Input.GetKeyDown(KeyCode.E))
            {
                OpenNote(currentNote);
            }
        }
        else
        {
            interactText.SetActive(false);
        }
    }

    void OpenNote(InteractableNote note)
    {
        if (note == null)
        {
            return;
        }

        bool collectedNow = note.MarkAsCollected();
        if (collectedNow)
        {
            ObjectiveSystem.EnsureInstance().RegisterNoteRead(note.NoteId);
        }

        if (interactText != null)
        {
            interactText.SetActive(false);
        }

        currentNote = null;
        note.playerInside = false;
        note.gameObject.SetActive(false);

        if (openNoteReaderOnPickup)
        {
            ShowNote(note);
            return;
        }

        isReading = false;
        openedNote = null;

        if (notePanel != null)
        {
            notePanel.SetActive(false);
        }

        if (TryPlayClosingVideo(note))
        {
            return;
        }
    }

    void CloseNote()
    {
        isReading = false;
        InteractableNote noteToClose = openedNote;
        openedNote = null;

        if (notePanel != null)
        {
            notePanel.SetActive(false);
        }

        if (TryPlayClosingVideo(noteToClose))
        {
            return;
        }

        RestoreGameplayCursor();
    }

    private void ShowNote(InteractableNote note)
    {
        openedNote = note;
        isReading = true;
        EnsureStyledNotePanel();

        if (notePanel != null)
        {
            notePanel.SetActive(true);
        }

        if (noteTitleText != null)
        {
            noteTitleText.text = ResolveNoteTitle(note);
        }

        if (noteText != null)
        {
            noteText.text = BuildNoteBody(note);
        }

        RefreshRuntimeNoteLayout();
        Canvas.ForceUpdateCanvases();
        if (noteScrollRect != null)
        {
            noteScrollRect.verticalNormalizedPosition = 1f;
        }

        if (interactText != null)
        {
            interactText.SetActive(false);
        }

        ShowUiCursor();
    }

    private bool TryPlayClosingVideo(InteractableNote note)
    {
        if (note == null || !note.HasClosingVideo)
        {
            return false;
        }

        string videoPath = note.ResolveVideoPath();
        if (string.IsNullOrWhiteSpace(videoPath) || !File.Exists(videoPath))
        {
            Debug.LogWarning($"No se encontro el video configurado para '{note.name}': {videoPath}");
            return false;
        }

        EnsureVideoOverlay();
        activeVideoNote = note;
        isPlayingVideo = true;

        if (notePanel != null)
        {
            notePanel.SetActive(false);
        }

        if (interactText != null)
        {
            interactText.SetActive(false);
        }

        videoImage.texture = null;
        videoOverlayRoot.SetActive(true);

        ShowUiCursor();

        videoPlayer.Stop();
        videoPlayer.url = videoPath;
        videoPlayer.Prepare();
        return true;
    }

    private void EnsureVideoOverlay()
    {
        if (videoOverlayRoot != null)
        {
            return;
        }

        EnsureVideoPlayer();

        videoOverlayRoot = new GameObject("NoteVideoOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        videoOverlayRoot.transform.SetParent(transform, false);

        Canvas overlayCanvas = videoOverlayRoot.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 500;

        CanvasScaler canvasScaler = videoOverlayRoot.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.matchWidthOrHeight = 0.5f;

        RectTransform overlayRect = videoOverlayRoot.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
        backgroundObject.transform.SetParent(videoOverlayRoot.transform, false);

        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        Image backgroundImage = backgroundObject.GetComponent<Image>();
        backgroundImage.color = Color.black;
        backgroundImage.raycastTarget = false;

        GameObject videoObject = new GameObject("VideoImage", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        videoObject.transform.SetParent(videoOverlayRoot.transform, false);

        RectTransform videoRect = videoObject.GetComponent<RectTransform>();
        videoRect.anchorMin = Vector2.zero;
        videoRect.anchorMax = Vector2.one;
        videoRect.offsetMin = Vector2.zero;
        videoRect.offsetMax = Vector2.zero;

        videoImage = videoObject.GetComponent<RawImage>();
        videoImage.color = Color.white;
        videoImage.raycastTarget = false;

        videoAspectFitter = videoObject.GetComponent<AspectRatioFitter>();
        videoAspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        videoAspectFitter.aspectRatio = 16f / 9f;

        videoOverlayRoot.SetActive(false);
    }

    private void EnsureVideoPlayer()
    {
        if (videoPlayer != null)
        {
            return;
        }

        videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null)
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }

        videoAudioSource = GetComponent<AudioSource>();
        if (videoAudioSource == null)
        {
            videoAudioSource = gameObject.AddComponent<AudioSource>();
        }

        videoAudioSource.playOnAwake = false;
        videoAudioSource.loop = false;

        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = true;
        videoPlayer.isLooping = false;
        videoPlayer.source = VideoSource.Url;
        videoPlayer.renderMode = VideoRenderMode.APIOnly;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, videoAudioSource);

        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.loopPointReached -= OnVideoFinished;
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.errorReceived -= OnVideoError;
        videoPlayer.errorReceived += OnVideoError;
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        if (!isPlayingVideo || videoImage == null)
        {
            return;
        }

        videoImage.texture = source.texture;
        if (source.texture != null && videoAspectFitter != null)
        {
            videoAspectFitter.aspectRatio = (float)source.texture.width / Mathf.Max(1, source.texture.height);
        }

        source.Play();
    }

    private void OnVideoFinished(VideoPlayer source)
    {
        if (!isPlayingVideo)
        {
            return;
        }

        FinishVideoPlayback(true);
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        if (!isPlayingVideo)
        {
            return;
        }

        Debug.LogWarning($"Error reproduciendo el video '{source.url}': {message}");
        FinishVideoPlayback(false);
    }

    private void FinishVideoPlayback(bool showEndMessage)
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        if (videoAudioSource != null)
        {
            videoAudioSource.Stop();
        }

        if (videoOverlayRoot != null)
        {
            videoOverlayRoot.SetActive(false);
        }

        if (videoImage != null)
        {
            videoImage.texture = null;
        }

        InteractableNote finishedVideoNote = activeVideoNote;
        activeVideoNote = null;
        isPlayingVideo = false;
        RestoreGameplayCursor();

        if (showEndMessage)
        {
            ShowClosingVideoMessage(finishedVideoNote);
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.loopPointReached -= OnVideoFinished;
        videoPlayer.errorReceived -= OnVideoError;
    }

    private void ShowUiCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void RestoreGameplayCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ShowClosingVideoMessage(InteractableNote note)
    {
        if (note == null || !note.HasClosingVideoEndMessage || MessageSystem.instance == null)
        {
            return;
        }

        MessageSystem.instance.ShowTypewriterMessage(note.ClosingVideoEndMessage, note.ClosingVideoEndMessageDuration);
    }

    private string ResolveNoteTitle(InteractableNote note)
    {
        if (note == null)
        {
            return "Documento";
        }

        return note.DisplayName;
    }

    private string BuildNoteBody(InteractableNote note)
    {
        if (note == null || note.noteData == null || string.IsNullOrWhiteSpace(note.noteData.noteText))
        {
            return string.Empty;
        }

        return note.noteData.noteText.Trim();
    }

    private void EnsureStyledNotePanel()
    {
        if (noteUiPrepared || notePanel == null || noteText == null)
        {
            return;
        }

        RectTransform panelRect = notePanel.GetComponent<RectTransform>();
        if (panelRect == null)
        {
            noteUiPrepared = true;
            return;
        }

        Image backdropImage = notePanel.GetComponent<Image>();
        if (backdropImage == null)
        {
            backdropImage = notePanel.AddComponent<Image>();
        }

        backdropImage.color = noteBackdropColor;
        backdropImage.raycastTarget = true;

        RectTransform paperRoot = EnsureRectTransform(panelRect, "StyledNotePaper");
        paperRoot.SetAsLastSibling();
        ConfigureRect(paperRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), notePaperSize);

        Image paperImage = GetOrAddComponent<Image>(paperRoot.gameObject);
        paperImage.color = notePaperColor;
        paperImage.raycastTarget = true;

        Outline paperOutline = GetOrAddComponent<Outline>(paperRoot.gameObject);
        paperOutline.effectColor = new Color(0.33f, 0.23f, 0.12f, 0.36f);
        paperOutline.effectDistance = new Vector2(1.5f, -1.5f);

        Shadow paperShadow = GetOrAddComponent<Shadow>(paperRoot.gameObject);
        paperShadow.effectColor = notePaperShadowColor;
        paperShadow.effectDistance = new Vector2(10f, -10f);

        RectTransform edgeTop = EnsureRectTransform(paperRoot, "PaperEdgeTop");
        ConfigureRect(edgeTop, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(-30f, 28f));
        Image edgeTopImage = GetOrAddComponent<Image>(edgeTop.gameObject);
        edgeTopImage.color = new Color(0.63f, 0.48f, 0.25f, 0.14f);
        edgeTopImage.raycastTarget = false;

        RectTransform edgeBottom = EnsureRectTransform(paperRoot, "PaperEdgeBottom");
        ConfigureRect(edgeBottom, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(-44f, 34f));
        Image edgeBottomImage = GetOrAddComponent<Image>(edgeBottom.gameObject);
        edgeBottomImage.color = new Color(0.39f, 0.27f, 0.14f, 0.15f);
        edgeBottomImage.raycastTarget = false;

        RectTransform sideShadeLeft = EnsureRectTransform(paperRoot, "PaperSideShadeLeft");
        ConfigureRect(sideShadeLeft, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(28f, -64f));
        Image sideShadeLeftImage = GetOrAddComponent<Image>(sideShadeLeft.gameObject);
        sideShadeLeftImage.color = new Color(0.34f, 0.23f, 0.11f, 0.08f);
        sideShadeLeftImage.raycastTarget = false;

        RectTransform sideShadeRight = EnsureRectTransform(paperRoot, "PaperSideShadeRight");
        ConfigureRect(sideShadeRight, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(32f, -84f));
        Image sideShadeRightImage = GetOrAddComponent<Image>(sideShadeRight.gameObject);
        sideShadeRightImage.color = new Color(0.28f, 0.19f, 0.09f, 0.10f);
        sideShadeRightImage.raycastTarget = false;

        RectTransform stainTop = EnsureRectTransform(paperRoot, "PaperStainTop");
        ConfigureRect(stainTop, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(-60f, 34f));
        Image stainTopImage = GetOrAddComponent<Image>(stainTop.gameObject);
        stainTopImage.color = new Color(0.55f, 0.38f, 0.19f, 0.10f);
        stainTopImage.raycastTarget = false;

        RectTransform stainBottom = EnsureRectTransform(paperRoot, "PaperStainBottom");
        ConfigureRect(stainBottom, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(-90f, 42f));
        Image stainBottomImage = GetOrAddComponent<Image>(stainBottom.gameObject);
        stainBottomImage.color = new Color(0.38f, 0.24f, 0.12f, 0.09f);
        stainBottomImage.raycastTarget = false;

        RectTransform centerCrease = EnsureRectTransform(paperRoot, "PaperCenterCrease");
        ConfigureRect(centerCrease, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(2f, -132f));
        Image centerCreaseImage = GetOrAddComponent<Image>(centerCrease.gameObject);
        centerCreaseImage.color = new Color(0.39f, 0.28f, 0.16f, 0.08f);
        centerCreaseImage.raycastTarget = false;

        TMP_Text archiveLabel = EnsureText(paperRoot, "NoteArchiveLabel");
        ConfigureRect(archiveLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(-120f, 18f));
        archiveLabel.text = "ARCHIVO RECUPERADO";
        archiveLabel.fontSize = 13f;
        archiveLabel.fontStyle = FontStyles.Bold;
        archiveLabel.characterSpacing = 4.4f;
        archiveLabel.alignment = TextAlignmentOptions.Center;
        archiveLabel.color = noteAccentColor;
        archiveLabel.enableWordWrapping = false;

        noteTitleText = EnsureText(paperRoot, "NoteTitle");
        ConfigureRect(noteTitleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(-132f, 50f));
        noteTitleText.fontSize = 33f;
        noteTitleText.fontStyle = FontStyles.Bold | FontStyles.SmallCaps;
        noteTitleText.characterSpacing = 2.1f;
        noteTitleText.alignment = TextAlignmentOptions.Center;
        noteTitleText.color = noteInkColor;
        noteTitleText.enableWordWrapping = true;

        RectTransform divider = EnsureRectTransform(paperRoot, "NoteDivider");
        ConfigureRect(divider, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -118f), new Vector2(-112f, 2f));
        Image dividerImage = GetOrAddComponent<Image>(divider.gameObject);
        dividerImage.color = new Color(noteAccentColor.r, noteAccentColor.g, noteAccentColor.b, 0.35f);
        dividerImage.raycastTarget = false;

        RectTransform closeButtonRect = EnsureRectTransform(paperRoot, "NoteCloseButton");
        ConfigureRect(closeButtonRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -18f), new Vector2(34f, 34f));
        Image closeButtonImage = GetOrAddComponent<Image>(closeButtonRect.gameObject);
        closeButtonImage.color = new Color(0.34f, 0.21f, 0.12f, 0.88f);
        closeButtonImage.raycastTarget = true;

        noteCloseButton = GetOrAddComponent<Button>(closeButtonRect.gameObject);
        noteCloseButton.targetGraphic = closeButtonImage;
        noteCloseButton.onClick.RemoveAllListeners();
        noteCloseButton.onClick.AddListener(CloseNote);

        TMP_Text closeLabel = EnsureText(closeButtonRect, "Label");
        ConfigureRect(closeLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        closeLabel.text = "X";
        closeLabel.fontSize = 18f;
        closeLabel.fontStyle = FontStyles.Bold;
        closeLabel.alignment = TextAlignmentOptions.Center;
        closeLabel.color = new Color(0.95f, 0.91f, 0.83f, 1f);
        closeLabel.enableWordWrapping = false;

        RectTransform scrollRoot = EnsureRectTransform(paperRoot, "NoteScrollRoot");
        ConfigureRect(scrollRoot, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), new Vector2(-92f, -184f));

        noteScrollRect = GetOrAddComponent<ScrollRect>(scrollRoot.gameObject);
        noteScrollRect.horizontal = false;
        noteScrollRect.movementType = ScrollRect.MovementType.Clamped;
        noteScrollRect.scrollSensitivity = 26f;

        RectTransform viewport = EnsureRectTransform(scrollRoot, "Viewport");
        StretchRect(viewport);
        Image viewportImage = GetOrAddComponent<Image>(viewport.gameObject);
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        viewportImage.raycastTarget = true;
        Mask viewportMask = GetOrAddComponent<Mask>(viewport.gameObject);
        viewportMask.showMaskGraphic = false;

        RectTransform content = EnsureRectTransform(viewport, "Content");
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(-22f, 0f);

        VerticalLayoutGroup contentLayout = GetOrAddComponent<VerticalLayoutGroup>(content.gameObject);
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.padding = new RectOffset(0, 0, 0, 8);
        contentLayout.spacing = 0f;

        ContentSizeFitter contentFitter = GetOrAddComponent<ContentSizeFitter>(content.gameObject);
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform noteBodyRect = noteText.rectTransform;
        noteBodyRect.SetParent(content, false);
        noteBodyRect.anchorMin = new Vector2(0f, 1f);
        noteBodyRect.anchorMax = new Vector2(1f, 1f);
        noteBodyRect.pivot = new Vector2(0.5f, 1f);
        noteBodyRect.anchoredPosition = Vector2.zero;
        noteBodyRect.sizeDelta = Vector2.zero;

        noteText.fontSize = 20f;
        noteText.fontStyle = FontStyles.Normal;
        noteText.characterSpacing = 0.45f;
        noteText.lineSpacing = 12f;
        noteText.paragraphSpacing = 14f;
        noteText.color = noteInkColor;
        noteText.alignment = TextAlignmentOptions.TopLeft;
        noteText.enableWordWrapping = true;
        noteText.overflowMode = TextOverflowModes.Overflow;
        noteText.margin = new Vector4(26f, 18f, 32f, 28f);

        noteBodyLayoutElement = GetOrAddComponent<LayoutElement>(noteText.gameObject);
        noteBodyLayoutElement.minHeight = 0f;
        noteBodyLayoutElement.flexibleHeight = 0f;

        RectTransform scrollbarRect = EnsureRectTransform(scrollRoot, "Scrollbar");
        ConfigureRect(scrollbarRect, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-6f, 0f), new Vector2(14f, -6f));
        Image scrollbarTrack = GetOrAddComponent<Image>(scrollbarRect.gameObject);
        scrollbarTrack.color = new Color(0.28f, 0.20f, 0.11f, 0.18f);
        scrollbarTrack.raycastTarget = true;

        RectTransform slidingArea = EnsureRectTransform(scrollbarRect, "SlidingArea");
        StretchRect(slidingArea);
        slidingArea.offsetMin = new Vector2(0f, 8f);
        slidingArea.offsetMax = new Vector2(0f, -8f);

        RectTransform handleRect = EnsureRectTransform(slidingArea, "Handle");
        ConfigureRect(handleRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 72f));
        Image handleImage = GetOrAddComponent<Image>(handleRect.gameObject);
        handleImage.color = new Color(0.42f, 0.28f, 0.15f, 0.88f);
        handleImage.raycastTarget = true;

        Scrollbar scrollbar = GetOrAddComponent<Scrollbar>(scrollbarRect.gameObject);
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleImage;
        scrollbar.size = 0.25f;

        noteScrollRect.viewport = viewport;
        noteScrollRect.content = content;
        noteScrollRect.verticalScrollbar = scrollbar;
        noteScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        noteScrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        noteUiPrepared = true;
    }

    private void RefreshRuntimeNoteLayout()
    {
        if (noteText == null || noteBodyLayoutElement == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();

        RectTransform textRect = noteText.rectTransform;
        float availableWidth = Mathf.Max(280f, textRect.rect.width - noteText.margin.x - noteText.margin.z);
        float preferredHeight = noteText.GetPreferredValues(noteText.text, availableWidth, 0f).y + noteText.margin.y + noteText.margin.w + 20f;

        noteBodyLayoutElement.preferredHeight = Mathf.Max(340f, preferredHeight);
        LayoutRebuilder.ForceRebuildLayoutImmediate(textRect);

        if (noteScrollRect != null && noteScrollRect.content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(noteScrollRect.content);
        }
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

    private TMP_Text EnsureText(Transform parent, string objectName)
    {
        RectTransform rect = EnsureRectTransform(parent, objectName);
        TextMeshProUGUI text = rect.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        }

        text.raycastTarget = false;
        return text;
    }

    private T GetOrAddComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }

        return component;
    }

    private void ConfigureRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private void StretchRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
