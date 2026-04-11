using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MapSystem : MonoBehaviour
{
    public GameObject mapPanel;
    public GameObject interactText;

    private MapPickup currentMap;
    private bool hasMap = false;
    private bool mapOpen = false;

    void Update()
    {
        ObjectiveSystem.EnsureInstance();
        DetectMap();

        // Recoger mapa
        if (currentMap != null && currentMap.playerInside)
        {
            interactText.SetActive(true);

            if (Input.GetKeyDown(KeyCode.E))
            {
                PickupMap();
            }
        }
        else
        {
            interactText.SetActive(false);
        }

        // Abrir / cerrar mapa
        if (hasMap && Input.GetKeyDown(KeyCode.M))
        {
            ToggleMap();
        }
    }

    void DetectMap()
    {
        MapPickup[] maps = FindObjectsOfType<MapPickup>();

        currentMap = null;

        foreach (var map in maps)
        {
            if (map.playerInside)
            {
                currentMap = map;
                break;
            }
        }
    }

    void PickupMap()
    {
        hasMap = true;
        ObjectiveSystem.Instance.RegisterMapPickup();

        Destroy(currentMap.gameObject);

        interactText.SetActive(false);
    }

    void ToggleMap()
    {
        mapOpen = !mapOpen;
        mapPanel.SetActive(mapOpen);

        Cursor.lockState = mapOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = mapOpen;
    }
}
