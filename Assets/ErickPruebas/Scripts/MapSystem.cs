using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MapSystem : MonoBehaviour
{
    public GameObject mapPanel;
    public GameObject interactText;
    [Header("Feedback")]
    [SerializeField] private string mapItemId = "mapa";
    [SerializeField] private string mapDisplayName = "Mapa";
    [SerializeField] private int mapPoints = 0;
    [SerializeField] private float pickupMessageDuration = 1.5f;

    private MapPickup currentMap;
    private bool hasMap = false;
    private bool mapOpen = false;
    private Camera mapCamera;

    void Awake()
    {
        ResolveMapCamera();
        ApplyMapCameraState();
    }

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
        InventoryManager.EnsureInstance().AddItem(mapItemId, mapDisplayName, 1, mapPoints);
        ObjectiveSystem.Instance.RegisterMapPickup();

        if (MessageSystem.instance != null)
        {
            MessageSystem.instance.ShowMessage(mapDisplayName + " obtenido", pickupMessageDuration);
        }

        Destroy(currentMap.gameObject);
        currentMap = null;

        interactText.SetActive(false);
        ApplyMapCameraState();
    }

    void ToggleMap()
    {
        mapOpen = !mapOpen;
        mapPanel.SetActive(mapOpen);
        ApplyMapCameraState();

        Cursor.lockState = mapOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = mapOpen;
    }

    private void ResolveMapCamera()
    {
        if (mapCamera != null)
        {
            return;
        }

        Camera[] cameras = FindObjectsOfType<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null && cameras[i].targetTexture != null && cameras[i].name == "MapCamera")
            {
                mapCamera = cameras[i];
                break;
            }
        }
    }

    private void ApplyMapCameraState()
    {
        ResolveMapCamera();

        if (mapCamera == null)
        {
            return;
        }

        // Keep the map camera off unless the fullscreen map is actually open.
        mapCamera.enabled = hasMap && mapOpen;

        AudioListener listener = mapCamera.GetComponent<AudioListener>();
        if (listener != null)
        {
            listener.enabled = false;
        }
    }
}
