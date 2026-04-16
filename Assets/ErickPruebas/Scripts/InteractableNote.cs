using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class InteractableNote : MonoBehaviour
{
    public static event Action<InteractableNote> NoteCollected;

    [Header("Identidad")]
    [SerializeField] private string noteId;
    [SerializeField] private string displayName;
    [SerializeField] private bool autoRegisterOnAwake = true;
    [SerializeField] private string bookChildName = "Book04";

    [Header("Cinematica")]
    [FormerlySerializedAs("pickupVideoPath")]
    [SerializeField] private string noteVideoPath;
    [SerializeField] private string closingVideoEndMessage;
    [SerializeField] private float closingVideoEndMessageDuration = 4f;

    public NoteData noteData;

    [HideInInspector]
    public bool playerInside = false;

    public string NoteId => ResolveNoteId();
    public string DisplayName => ResolveDisplayName();
    public string PreviewText => ResolvePreviewText();
    public string FullText => ResolveFullText();
    public bool HasClosingVideo => !string.IsNullOrWhiteSpace(noteVideoPath);
    public bool HasClosingVideoEndMessage => !string.IsNullOrWhiteSpace(closingVideoEndMessage);
    public string ClosingVideoEndMessage => string.IsNullOrWhiteSpace(closingVideoEndMessage) ? string.Empty : closingVideoEndMessage.Trim();
    public float ClosingVideoEndMessageDuration => Mathf.Max(0.5f, closingVideoEndMessageDuration);

    private void Awake()
    {
        if (autoRegisterOnAwake)
        {
            RegisterInInventory();
        }
    }

    public string ResolveVideoPath()
    {
        if (string.IsNullOrWhiteSpace(noteVideoPath))
        {
            return string.Empty;
        }

        string rawPath = noteVideoPath.Trim();
        if (Path.IsPathRooted(rawPath) || rawPath.Contains("://"))
        {
            return rawPath.Replace("\\", "/");
        }

        return Path.Combine(Application.streamingAssetsPath, rawPath).Replace("\\", "/");
    }

    public void RegisterInInventory()
    {
        InventoryManager.EnsureInstance().RegisterNote(NoteId, DisplayName, PreviewText, FullText);
    }

    public bool MarkAsCollected()
    {
        bool wasCollectedNow = InventoryManager.EnsureInstance().DiscoverNote(NoteId, DisplayName, PreviewText, FullText);
        if (wasCollectedNow)
        {
            NoteCollected?.Invoke(this);

            if (GameplayRunState.TryConsumeNotesTabHint() && MessageSystem.instance != null)
            {
                MessageSystem.instance.ShowTypewriterMessage("Presiona TAB para abrir las notas.", 3f, 0.02f);
            }
        }

        return wasCollectedNow;
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
        if (TryGetNoteNumber(out int noteNumber))
        {
            return $"Documento {noteNumber}";
        }

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
        if (TryGetNoteMetadata(out string generatedPreview))
        {
            return generatedPreview;
        }

        if (noteData == null)
        {
            return "Pendiente por inspeccionar";
        }

        string preferredPreview = !string.IsNullOrWhiteSpace(noteData.previewText)
            ? noteData.previewText
            : noteData.noteText;

        if (string.IsNullOrWhiteSpace(preferredPreview))
        {
            return "Pendiente por inspeccionar";
        }

        return BuildShortPreview(preferredPreview, 8);
    }

    private string ResolveFullText()
    {
        if (noteData == null || string.IsNullOrWhiteSpace(noteData.noteText))
        {
            return "Sin contenido";
        }

        return noteData.noteText.Trim();
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

    private bool TryGetNoteNumber(out int noteNumber)
    {
        noteNumber = 0;

        string[] candidates =
        {
            gameObject.name,
            noteId,
            displayName,
            noteData != null ? noteData.title : string.Empty
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            string digits = ExtractTrailingDigits(candidates[i]);
            if (int.TryParse(digits, out noteNumber) && noteNumber > 0)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryGetNoteMetadata(out string previewText)
    {
        previewText = string.Empty;
        if (!TryGetNoteNumber(out int noteNumber))
        {
            return false;
        }

        previewText = noteNumber switch
        {
            1 => "La primera pista apunta hacia la gasolinera abandonada.",
            2 => "El libro sellado deja claro que algo desperto en la casa.",
            3 => "Otra nota insiste en revisar el cuarto del fondo con cuidado.",
            4 => "Las marcas del interior revelan un recorrido mas profundo.",
            5 => "Las hojas sueltas senalan un escondite aun mas adentro.",
            6 => "El penultimo documento habla de la salida y del auto.",
            7 => "La nota final confirma que es hora de volver al jeep.",
            _ => $"Fragmento clave del Documento {noteNumber}."
        };

        return true;
    }

    private static string BuildShortPreview(string sourceText, int maxWords)
    {
        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return "Pendiente por inspeccionar";
        }

        string cleanText = sourceText.Replace("\r", " ").Replace("\n", " ").Trim();
        string[] words = cleanText.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= maxWords)
        {
            return cleanText;
        }

        return string.Join(" ", words, 0, maxWords).TrimEnd() + "...";
    }
}
