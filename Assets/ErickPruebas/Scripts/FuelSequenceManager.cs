using System;
using System.Collections.Generic;
using UnityEngine;

public class FuelSequenceManager : MonoBehaviour
{
    private const string EscapeVehicleName = "Military combat Jeep";
    private const string PlayerIconTemplateName = "PlayerIcon";

    public List<GameObject> fuelIcons;

    [Header("Escape Icon")]
    [SerializeField] private bool revealEscapeVehicleOnFinalNote = true;
    [SerializeField] private Vector3 escapeIconOffset = new Vector3(0f, 80f, 0f);
    [SerializeField] private Color escapeIconColor = new Color(1f, 0.67f, 0.28f, 1f);

    private int currentIndex;
    private GameObject escapeVehicleIcon;

    private void OnEnable()
    {
        InteractableNote.NoteCollected -= HandleNoteCollected;
        InteractableNote.NoteCollected += HandleNoteCollected;
    }

    private void OnDisable()
    {
        InteractableNote.NoteCollected -= HandleNoteCollected;
    }

    private void Start()
    {
        ObjectiveSystem.EnsureInstance();
        ResetIconSequence();
    }

    public void OnFuelCollected()
    {
        if (currentIndex < fuelIcons.Count && fuelIcons[currentIndex] != null)
        {
            fuelIcons[currentIndex].SetActive(false);
        }

        currentIndex++;

        if (currentIndex < fuelIcons.Count && fuelIcons[currentIndex] != null)
        {
            fuelIcons[currentIndex].SetActive(true);
        }
    }

    private void ResetIconSequence()
    {
        currentIndex = 0;

        foreach (GameObject icon in fuelIcons)
        {
            if (icon != null)
            {
                icon.SetActive(false);
            }
        }

        if (fuelIcons.Count > 0 && fuelIcons[0] != null)
        {
            fuelIcons[0].SetActive(true);
        }

        if (escapeVehicleIcon != null)
        {
            Destroy(escapeVehicleIcon);
            escapeVehicleIcon = null;
        }
    }

    private void HandleNoteCollected(InteractableNote collectedNote)
    {
        if (!revealEscapeVehicleOnFinalNote || collectedNote == null || !InventoryManager.HasInstance)
        {
            return;
        }

        InventoryManager inventory = InventoryManager.Instance;
        if (inventory.TotalRegisteredNotes <= 0 || inventory.CollectedNotesCount < inventory.TotalRegisteredNotes)
        {
            return;
        }

        RevealEscapeVehicleIcon();
    }

    private void RevealEscapeVehicleIcon()
    {
        if (escapeVehicleIcon == null)
        {
            escapeVehicleIcon = CreateEscapeVehicleIcon();
        }

        if (escapeVehicleIcon != null)
        {
            escapeVehicleIcon.SetActive(true);
        }
    }

    private GameObject CreateEscapeVehicleIcon()
    {
        Transform jeepTransform = FindTransformByName(EscapeVehicleName);
        if (jeepTransform == null)
        {
            return null;
        }

        GameObject template = GameObject.Find(PlayerIconTemplateName);
        if (template == null && fuelIcons.Count > 0)
        {
            template = fuelIcons[0];
        }

        GameObject iconRoot;
        if (template != null)
        {
            iconRoot = Instantiate(template, jeepTransform.position + escapeIconOffset, template.transform.rotation);
            iconRoot.name = "EscapeVehicleIcon";

            MonoBehaviour[] behaviours = iconRoot.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                Destroy(behaviours[i]);
            }
        }
        else
        {
            iconRoot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            iconRoot.name = "EscapeVehicleIcon";
            Destroy(iconRoot.GetComponent<Collider>());
            iconRoot.transform.position = jeepTransform.position + escapeIconOffset;
            iconRoot.transform.localScale = Vector3.one * 18f;
        }

        ApplyIconColor(iconRoot);
        SetLayerRecursively(iconRoot, LayerMask.NameToLayer("MapIcons"));
        iconRoot.SetActive(false);
        return iconRoot;
    }

    private void ApplyIconColor(GameObject iconRoot)
    {
        if (iconRoot == null)
        {
            return;
        }

        SpriteRenderer[] spriteRenderers = iconRoot.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            spriteRenderers[i].color = escapeIconColor;
        }

        Renderer[] renderers = iconRoot.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].sharedMaterial != null && renderers[i].sharedMaterial.HasProperty("_Color"))
            {
                renderers[i].material.color = escapeIconColor;
            }
        }
    }

    private static Transform FindTransformByName(string targetName)
    {
        if (string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        Transform[] transforms = FindObjectsOfType<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && transforms[i].name.IndexOf(targetName, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return transforms[i];
            }
        }

        return null;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        if (root == null || layer < 0)
        {
            return;
        }

        root.layer = layer;
        Transform transform = root.transform;
        for (int i = 0; i < transform.childCount; i++)
        {
            SetLayerRecursively(transform.GetChild(i).gameObject, layer);
        }
    }
}
