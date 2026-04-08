using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PickupSystem : MonoBehaviour
{
    public GameObject interactText;

    private PickupItem currentItem;

    void Update()
    {
        PickupItem[] items = FindObjectsOfType<PickupItem>();
        currentItem = null;

        foreach (var item in items)
        {
            if (item.playerInside == true)
            {
                currentItem = item;
                break;
            }
        }

        if (currentItem != null)
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

    void Pickup(PickupItem item)
    {
        if (item.fuelManager != null)
        {
            item.fuelManager.OnFuelCollected();
        }

        Destroy(item.gameObject);
    }
}
