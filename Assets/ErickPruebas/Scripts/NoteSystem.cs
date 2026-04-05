using UnityEngine;
using TMPro;

public class NoteSystem : MonoBehaviour
{
    [Header("UI")]
    public GameObject interactText;
    public GameObject notePanel;
    public TMP_Text noteText;

    private InteractableNote currentNote;
    private PickupItem currentItem;

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

        // Buscar notas (creo)
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

        // Buscar objetos (creo)
        PickupItem[] items = FindObjectsOfType<PickupItem>();
        currentItem = null;

        foreach (var item in items)
        {
            if (item.playerInside)
            {
                currentItem = item;
                break;
            }
        }

        // UI (en proceso)
        if (currentNote != null)
        {
            interactText.SetActive(true);
            interactText.GetComponent<TMP_Text>().text = "Presiona E para inspeccionar";

            if (Input.GetKeyDown(KeyCode.E))
            {
                OpenNote(currentNote);
            }
        }
        else if (currentItem != null)
        {
            interactText.SetActive(true);
            interactText.GetComponent<TMP_Text>().text = "Presiona E para recoger";

            if (Input.GetKeyDown(KeyCode.E))
            {
                Pickup(currentItem);
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

        notePanel.SetActive(true);

        noteText.text = note.noteData.noteText;

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

    void Pickup(PickupItem item)
    {
        Destroy(item.gameObject);
    }
}