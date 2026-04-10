using UnityEngine;
using UnityEngine.SceneManagement;

public class InteractableNote : MonoBehaviour
{
    [Header("Identidad")]
    [SerializeField] private string noteId;
    [SerializeField] private string displayName;
    [SerializeField] private bool autoRegisterOnAwake = true;
    [SerializeField] private string bookChildName = "Book04";

    public NoteData noteData;

    [HideInInspector]
    public bool playerInside = false;

    public string NoteId => ResolveNoteId();
    public string DisplayName => ResolveDisplayName();
    public string PreviewText => ResolvePreviewText();

    private void Awake()
    {
        if (autoRegisterOnAwake)
        {
            RegisterInInventory();
        }
    }

    public void RegisterInInventory()
    {
        InventoryManager.EnsureInstance().RegisterNote(NoteId, DisplayName, PreviewText);
    }

    public bool MarkAsCollected()
    {
        return InventoryManager.EnsureInstance().DiscoverNote(NoteId, DisplayName, PreviewText);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
        }
    }

    private string ResolveNoteId()
    {
        if (!string.IsNullOrWhiteSpace(noteId))
        {
            return noteId.Trim();
        }

        Scene scene = gameObject.scene;
        return $"{scene.name}:{BuildHierarchyPath(transform)}";
    }

    private string ResolveDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName.Trim();
        }

        if (noteData != null && !string.IsNullOrWhiteSpace(noteData.title))
        {
            return noteData.title.Trim();
        }

        string numericSuffix = ExtractTrailingDigits(gameObject.name);
        if (!string.IsNullOrWhiteSpace(numericSuffix))
        {
            return $"Documento {numericSuffix}";
        }

        Transform bookChild = FindBookChild(transform);
        if (bookChild != null)
        {
            return bookChild.name.Replace("(Clone)", string.Empty).Trim();
        }

        return gameObject.name;
    }

    private string ResolvePreviewText()
    {
        if (noteData == null || string.IsNullOrWhiteSpace(noteData.noteText))
        {
            return "Pendiente por inspeccionar";
        }

        string cleanText = noteData.noteText.Replace("\r", " ").Replace("\n", " ").Trim();
        if (cleanText.Length <= 48)
        {
            return cleanText;
        }

        return cleanText.Substring(0, 48).TrimEnd() + "...";
    }

    private Transform FindBookChild(Transform root)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name.StartsWith(bookChildName))
            {
                return child;
            }
        }

        return null;
    }

    private static string BuildHierarchyPath(Transform current)
    {
        if (current == null)
        {
            return "Note";
        }

        string path = current.name;
        Transform parent = current.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    private static string ExtractTrailingDigits(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        System.Text.StringBuilder digits = new System.Text.StringBuilder();
        for (int i = 0; i < value.Length; i++)
        {
            if (char.IsDigit(value[i]))
            {
                digits.Append(value[i]);
            }
        }

        return digits.ToString();
    }
}
