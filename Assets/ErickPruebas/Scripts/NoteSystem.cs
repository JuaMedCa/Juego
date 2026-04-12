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
            interactText.GetComponent<TMP_Text>().text = "Presiona E para inspeccionar";

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

        note.MarkAsCollected();
        ShowNote(note);
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
        ObjectiveSystem.EnsureInstance().RegisterNoteRead(note.NoteId);

        if (notePanel != null)
        {
            notePanel.SetActive(true);
        }

        if (noteText != null)
        {
            noteText.text = BuildNoteContent(note);
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

    private string BuildNoteContent(InteractableNote note)
    {
        if (note == null || note.noteData == null)
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(note.noteData.title))
        {
            return note.noteData.noteText;
        }

        return $"<b>{note.noteData.title}</b>\n\n{note.noteData.noteText}";
    }
}
