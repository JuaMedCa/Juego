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

            if (GameplayRunState.TryConsumeNotesTabHint())
            {
                TutorialHintOverlay.ShowHint("Presiona TAB para abrir las notas.", 3f);
            }

            if (ShouldShowReturnToCarHint())
            {
                TutorialHintOverlay.ShowHint("Ahi viene... debo volver a mi auto ahora!", 4f);
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
        if (TryGetMappedNoteContent(out string generatedPreview, out _))
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
        if (TryGetMappedNoteContent(out _, out string generatedFullText))
        {
            return generatedFullText;
        }

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

    private bool TryGetMappedNoteContent(out string previewText, out string fullText)
    {
        previewText = string.Empty;
        fullText = string.Empty;
        if (!TryGetNoteNumber(out int noteNumber))
        {
            return false;
        }

        switch (noteNumber)
        {
            case 1:
                previewText = "Segui la senal hasta la gasolinera.";
                fullText = "Tome tu linterna y segui la senal hasta la gasolinera vieja. Escuche mi nombre viniendo desde la casa del camino, como si alguien me imitara en la oscuridad. Si lees esto, no me busques por la carretera principal; entra a la casa con cuidado y revisa el cuarto del fondo.";
                return true;
            case 2:
                previewText = "La casa no estaba abandonada.";
                fullText = "La casa tiene marcas frescas en las paredes y un olor a agua podrida. No estoy sola aqui. En una mesa encontre dibujos de una torre, bidones rojos y una flecha apuntando al norte. Voy a seguir ese rastro antes de que anochezca.";
                return true;
            case 3:
                previewText = "La torre revela el camino norte.";
                fullText = "Desde la torre pude verlo por fin: algo enorme rondando el canal, arrastrando sacos hacia una cerca oxidada. Tambien vi huellas recientes junto a varios bidones. Si vas detras de mi, no sigas el barro; busca la reja del taller y manten la luz apagada.";
                return true;
            case 4:
                previewText = "El jeep todavia podria arrancar.";
                fullText = "Los bidones no son para un incendio. Los estan guardando para un jeep militar escondido tras la reja. Escuche a dos hombres decir que moveran 'a la chica' antes del amanecer. Si encuentras suficiente gasolina, ese jeep aun podria sacarte de aqui.";
                return true;
            case 5:
                previewText = "Mencionaron un paradero oculto.";
                fullText = "Oi la palabra 'paradero' varias veces mientras discutian junto al mapa. No se referian a la carretera, sino a un escondite detras del camino hundido. Si sigues buscando, no confies en las voces. Algo aqui aprende rapido a sonar como nosotros.";
                return true;
            case 6:
                previewText = "Bajo el paradero oi tuberias.";
                fullText = "Llegue al paradero y solo encontre libros mojados, sangre seca y una escotilla abierta. Debajo escuche tuberias que regresan hacia la gasolinera. Si lees esto, aun estoy cerca. Sigue el ruido del agua antes de que vuelvan por mi.";
                return true;
            case 7:
                previewText = "Estoy bajo la gasolinera.";
                fullText = "Me movieron al pozo de drenaje bajo la gasolinera. El monstruo vigila la entrada y no creo poder salir sola. Si no puedes bajar por mi, huye en el jeep, regresa armado y no olvides este lugar. Todavia sigo aqui.";
                return true;
            default:
                previewText = $"Fragmento clave del Documento {noteNumber}.";
                fullText = previewText;
                return true;
        }
    }

    private bool ShouldShowReturnToCarHint()
    {
        bool isCubeSeven = gameObject.name.IndexOf("Cube7", StringComparison.OrdinalIgnoreCase) >= 0;
        bool isFinalCollectedNote = InventoryManager.HasInstance
            && InventoryManager.Instance.TotalRegisteredNotes > 0
            && InventoryManager.Instance.CollectedNotesCount >= InventoryManager.Instance.TotalRegisteredNotes;

        if (!isCubeSeven && !isFinalCollectedNote)
        {
            return false;
        }

        return GameplayRunState.TryConsumeReturnToCarHint();
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
