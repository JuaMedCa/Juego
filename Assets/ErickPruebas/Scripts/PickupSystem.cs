using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PickupSystem : MonoBehaviour
{
    public static PickupSystem Instance { get; private set; }

    public static PickupSystem EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        PickupSystem existing = FindObjectOfType<PickupSystem>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject managerObject = new GameObject("PickupManager");
        Instance = managerObject.AddComponent<PickupSystem>();
        return Instance;
    }

    [Header("UI")]
    public GameObject interactText;
    [SerializeField] private TMP_Text interactLabel;

    [Header("Configuracion")]
    public KeyCode pickupKey = KeyCode.E;
    public bool useMessageSystem = true;
    public float pickupMessageDuration = 1.5f;

    private readonly List<PickupItem> nearbyItems = new List<PickupItem>();
    private PickupItem currentItem;

    private void Awake()
    {
        Instance = this;
        InventoryManager.EnsureInstance();
        ObjectiveSystem.EnsureInstance();

        if (interactText == null)
        {
            interactText = FindInteractTextObject();
        }

        if (interactLabel == null && interactText != null)
        {
            interactLabel = interactText.GetComponent<TMP_Text>();
        }

        HideInteractText();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private GameObject FindInteractTextObject()
    {
        TMP_Text[] textObjects = FindObjectsOfType<TMP_Text>(true);
        for (int i = 0; i < textObjects.Length; i++)
        {
            if (textObjects[i].name == "InteractText")
            {
                return textObjects[i].gameObject;
            }
        }

        return null;
    }

    private void Update()
    {
        if (currentItem == null)
        {
            return;
        }

        ShowInteractText(currentItem.PickupPrompt);

        if (Input.GetKeyDown(pickupKey))
        {
            Pickup(currentItem);
        }
    }

    public void SetCurrentItem(PickupItem item)
    {
        if (item == null)
        {
            return;
        }

        if (!nearbyItems.Contains(item))
        {
            nearbyItems.Add(item);
        }

        currentItem = item;
        ShowInteractText(item.PickupPrompt);
    }

    public void ClearCurrentItem(PickupItem item)
    {
        if (item == null)
        {
            return;
        }

        nearbyItems.Remove(item);

        if (currentItem == item)
        {
            currentItem = nearbyItems.Count > 0 ? nearbyItems[nearbyItems.Count - 1] : null;
        }

        if (currentItem != null)
        {
            ShowInteractText(currentItem.PickupPrompt);
        }
        else
        {
            HideInteractText();
        }
    }

    public void TryPickup(PickupItem item)
    {
        if (item == null)
        {
            return;
        }

        Pickup(item);
    }

    private void Pickup(PickupItem item)
    {
        if (item == null)
        {
            return;
        }

        InventoryManager.Instance.AddItem(item.ItemId, item.DisplayName, item.amount, item.points);

        if (item.fuelManager != null)
        {
            item.fuelManager.OnFuelCollected();
        }

        if (item.IsFuelPickup)
        {
            ObjectiveSystem.Instance.RegisterFuelPickup(item.ItemId);
        }

        if (useMessageSystem && MessageSystem.instance != null)
        {
            int newCount = InventoryManager.Instance.GetItemCount(item.ItemId);
            MessageSystem.instance.ShowMessage($"{item.DisplayName} x{newCount}", pickupMessageDuration);
        }

        nearbyItems.Remove(item);
        currentItem = nearbyItems.Count > 0 ? nearbyItems[nearbyItems.Count - 1] : null;

        if (currentItem != null)
        {
            ShowInteractText(currentItem.PickupPrompt);
        }
        else
        {
            HideInteractText();
        }

        Destroy(item.gameObject);
    }

    private void ShowInteractText(string message)
    {
        if (interactText == null)
        {
            return;
        }

        interactText.SetActive(true);

        if (interactLabel != null)
        {
            interactLabel.text = string.IsNullOrWhiteSpace(message)
                ? "Presiona E para recoger"
                : message;
        }
    }

    private void HideInteractText()
    {
        if (interactText != null)
        {
            interactText.SetActive(false);
        }
    }
}
