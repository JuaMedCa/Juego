using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    private static InventoryManager instance;

    public static bool HasInstance => instance != null;

    public static InventoryManager Instance
    {
        get
        {
            if (instance == null)
            {
                EnsureInstance();
            }

            return instance;
        }
    }

    public static InventoryManager EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindObjectOfType<InventoryManager>();

        if (instance == null)
        {
            GameObject managerObject = new GameObject("InventoryManager");
            instance = managerObject.AddComponent<InventoryManager>();
        }

        return instance;
    }

    public event Action InventoryChanged;

    public int TotalScore { get; private set; }
    public int TotalItemCount { get; private set; }
    public int UniqueItemCount => itemOrder.Count;
    public int TotalRegisteredNotes => noteOrder.Count;
    public int CollectedNotesCount { get; private set; }
    public int PendingNotesCount => Mathf.Max(0, TotalRegisteredNotes - CollectedNotesCount);

    private readonly Dictionary<string, InventoryEntry> inventory = new Dictionary<string, InventoryEntry>();
    private readonly List<string> itemOrder = new List<string>();
    private readonly Dictionary<string, NoteEntry> notes = new Dictionary<string, NoteEntry>();
    private readonly List<string> noteOrder = new List<string>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddItem(string itemId, string displayName, int amount, int pointsPerUnit)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            itemId = "Item";
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = itemId;
        }

        amount = Mathf.Max(1, amount);
        pointsPerUnit = Mathf.Max(0, pointsPerUnit);

        if (!inventory.TryGetValue(itemId, out InventoryEntry entry))
        {
            entry = new InventoryEntry(itemId, displayName);
            inventory.Add(itemId, entry);
            itemOrder.Add(itemId);
        }

        entry.DisplayName = displayName;
        entry.Quantity += amount;
        entry.TotalPoints += amount * pointsPerUnit;

        TotalItemCount += amount;
        TotalScore += amount * pointsPerUnit;

        InventoryChanged?.Invoke();
    }

    public int GetItemCount(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return 0;
        }

        return inventory.TryGetValue(itemId, out InventoryEntry entry) ? entry.Quantity : 0;
    }

    public string GetCompactSummary(string emptyMessage = "Inventario vacio")
    {
        if (inventory.Count == 0)
        {
            return emptyMessage;
        }

        return $"Objetos: {TotalItemCount} | Tipos: {UniqueItemCount} | Puntos: {TotalScore}";
    }

    public string GetDetailedSummary(string emptyMessage = "Inventario vacio")
    {
        if (inventory.Count == 0)
        {
            return emptyMessage;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine(GetCompactSummary(emptyMessage));

        for (int i = 0; i < itemOrder.Count; i++)
        {
            InventoryEntry entry = inventory[itemOrder[i]];
            builder.Append("- ");
            builder.Append(entry.DisplayName);
            builder.Append(" x");
            builder.Append(entry.Quantity);

            if (i < itemOrder.Count - 1)
            {
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    public void RegisterNote(string noteId, string displayName, string previewText = "", string fullText = "")
    {
        if (string.IsNullOrWhiteSpace(noteId))
        {
            return;
        }

        bool changed = false;
        if (!notes.TryGetValue(noteId, out NoteEntry entry))
        {
            entry = new NoteEntry(noteId, displayName, previewText, fullText);
            notes.Add(noteId, entry);
            noteOrder.Add(noteId);
            changed = true;
        }

        string resolvedName = string.IsNullOrWhiteSpace(displayName) ? entry.DisplayName : displayName.Trim();
        string resolvedPreview = string.IsNullOrWhiteSpace(previewText) ? entry.PreviewText : previewText.Trim();
        string resolvedFullText = string.IsNullOrWhiteSpace(fullText) ? entry.FullText : fullText.Trim();

        if (entry.DisplayName != resolvedName)
        {
            entry.DisplayName = resolvedName;
            changed = true;
        }

        if (entry.PreviewText != resolvedPreview)
        {
            entry.PreviewText = resolvedPreview;
            changed = true;
        }

        if (entry.FullText != resolvedFullText)
        {
            entry.FullText = resolvedFullText;
            changed = true;
        }

        if (changed)
        {
            InventoryChanged?.Invoke();
        }
    }

    public bool DiscoverNote(string noteId, string displayName, string previewText = "", string fullText = "")
    {
        if (string.IsNullOrWhiteSpace(noteId))
        {
            return false;
        }

        RegisterNote(noteId, displayName, previewText, fullText);

        NoteEntry entry = notes[noteId];
        if (entry.Collected)
        {
            return false;
        }

        entry.Collected = true;
        CollectedNotesCount++;
        InventoryChanged?.Invoke();
        return true;
    }

    public bool IsNoteCollected(string noteId)
    {
        return !string.IsNullOrWhiteSpace(noteId)
            && notes.TryGetValue(noteId, out NoteEntry entry)
            && entry.Collected;
    }

    public string GetNotesCounterSummary(string emptyMessage = "Sin documentos")
    {
        if (TotalRegisteredNotes == 0)
        {
            return emptyMessage;
        }

        return $"Documentos {CollectedNotesCount}/{TotalRegisteredNotes}";
    }

    public string GetNotesDetailedSummary(string emptyMessage = "Sin documentos")
    {
        if (TotalRegisteredNotes == 0)
        {
            return emptyMessage;
        }

        return $"{CollectedNotesCount} archivados  |  {PendingNotesCount} pendientes";
    }

    public List<NoteSnapshot> GetOrderedNotes()
    {
        List<NoteSnapshot> orderedNotes = new List<NoteSnapshot>(noteOrder.Count);

        for (int i = 0; i < noteOrder.Count; i++)
        {
            NoteEntry entry = notes[noteOrder[i]];
            orderedNotes.Add(new NoteSnapshot(
                entry.NoteId,
                entry.DisplayName,
                entry.PreviewText,
                entry.FullText,
                entry.Collected,
                i + 1));
        }

        orderedNotes.Sort((left, right) =>
        {
            int orderComparison = ExtractSortOrder(left).CompareTo(ExtractSortOrder(right));
            if (orderComparison != 0)
            {
                return orderComparison;
            }

            return string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
        });

        return orderedNotes;
    }

    [Serializable]
    private class InventoryEntry
    {
        public string ItemId;
        public string DisplayName;
        public int Quantity;
        public int TotalPoints;

        public InventoryEntry(string itemId, string displayName)
        {
            ItemId = itemId;
            DisplayName = displayName;
            Quantity = 0;
            TotalPoints = 0;
        }
    }

    private class NoteEntry
    {
        public string NoteId;
        public string DisplayName;
        public string PreviewText;
        public string FullText;
        public bool Collected;

        public NoteEntry(string noteId, string displayName, string previewText, string fullText)
        {
            NoteId = noteId;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Documento" : displayName.Trim();
            PreviewText = string.IsNullOrWhiteSpace(previewText) ? "Pendiente por inspeccionar" : previewText.Trim();
            FullText = string.IsNullOrWhiteSpace(fullText) ? PreviewText : fullText.Trim();
            Collected = false;
        }
    }

    public struct NoteSnapshot
    {
        public readonly string NoteId;
        public readonly string DisplayName;
        public readonly string PreviewText;
        public readonly string FullText;
        public readonly bool Collected;
        public readonly int Order;

        public NoteSnapshot(string noteId, string displayName, string previewText, string fullText, bool collected, int order)
        {
            NoteId = noteId;
            DisplayName = displayName;
            PreviewText = previewText;
            FullText = fullText;
            Collected = collected;
            Order = order;
        }
    }

    private static int ExtractSortOrder(NoteSnapshot snapshot)
    {
        string[] candidates =
        {
            snapshot.DisplayName,
            snapshot.NoteId
        };

        for (int candidateIndex = 0; candidateIndex < candidates.Length; candidateIndex++)
        {
            string candidate = candidates[candidateIndex];
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            StringBuilder digits = new StringBuilder();
            for (int i = 0; i < candidate.Length; i++)
            {
                if (char.IsDigit(candidate[i]))
                {
                    digits.Append(candidate[i]);
                }
            }

            if (digits.Length > 0 && int.TryParse(digits.ToString(), out int parsed))
            {
                return parsed;
            }
        }

        return snapshot.Order;
    }
}
