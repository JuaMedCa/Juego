using UnityEngine;

public class PickupItem : MonoBehaviour
{
    [Header("Datos del objeto")]
    public string itemId;
    public string displayName;
    [Min(1)] public int amount = 1;
    [Min(0)] public int points = 1;
    public string pickupPrompt = "Presiona E para recoger";
    public FuelSequenceManager fuelManager;

    [HideInInspector]
    public bool playerInside = false;

    public string ItemId => ResolveName(itemId);
    public string DisplayName => ResolveName(displayName);
    public string PickupPrompt => string.IsNullOrWhiteSpace(pickupPrompt)
        ? $"Presiona E para recoger {DisplayName}"
        : pickupPrompt;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        playerInside = true;

        PickupSystem pickupSystem = other.GetComponentInParent<PickupSystem>();
        if (pickupSystem != null)
        {
            pickupSystem.SetCurrentItem(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        playerInside = false;

        PickupSystem pickupSystem = other.GetComponentInParent<PickupSystem>();
        if (pickupSystem != null)
        {
            pickupSystem.ClearCurrentItem(this);
        }
    }

    private string ResolveName(string configuredValue)
    {
        if (!string.IsNullOrWhiteSpace(configuredValue))
        {
            return configuredValue.Trim();
        }

        string fallbackName = gameObject.name.Replace("(Clone)", string.Empty).Trim();
        return string.IsNullOrWhiteSpace(fallbackName) ? "Item" : fallbackName;
    }
}
