using UnityEngine;
using TMPro;

public class NoteSystem : MonoBehaviour
{
    [Header("UI")]
    public GameObject interactText;
    public GameObject notePanel;
    public TMP_Text noteText;

    private InteractableNote currentNote;
    private bool isReading = false;

    void Update()
    {
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
        isReading = true;
        note.MarkAsCollected();
        ObjectiveSystem.EnsureInstance().RegisterNoteRead(note.NoteId);

        notePanel.SetActive(true);
        noteText.text = BuildNoteContent(note);

        interactText.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void CloseNote()
    {
        isReading = false;

        notePanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
