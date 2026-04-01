using UnityEngine;
using TMPro;

public class NoteSystem : MonoBehaviour
{
    public GameObject interactText;
    public GameObject notePanel;
    public TMP_Text noteUIText;

    private InteractableNote currentNote;
    private bool isReading = false;

    void Start()
    {
        interactText.SetActive(false);
        notePanel.SetActive(false);
    }

    void Update()
    {
        if (isReading)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
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
        notePanel.SetActive(true);
        noteUIText.text = note.noteData.noteText;
        interactText.SetActive(false);
        isReading = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0f;
    }

    void CloseNote()
    {
        notePanel.SetActive(false);
        isReading = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Time.timeScale = 1f;
    }
}