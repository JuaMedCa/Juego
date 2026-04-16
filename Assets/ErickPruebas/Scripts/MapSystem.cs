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
    [SerializeField] private float tutorialMessageDuration = 3f;
    [SerializeField] private bool freezeGameWhenMapOpen = true;
    [SerializeField] private bool showCursorWhenMapOpen = false;

    private MapPickup currentMap;
    private bool hasMap = false;
    private bool mapOpen = false;
    private Camera mapCamera;
    private int mapIconsLayer = -1;

    void Awake()
    {
        ResolveMapCamera();
        ResolveMapIconsLayer();
        ApplyMapIconCameraFiltering();
        ApplyMapCameraState();
    }

    void LateUpdate()
    {
        ApplyMapIconCameraFiltering();
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

        if (GameplayRunState.TryConsumeMapPickupHint())
        {
            TutorialHintOverlay.ShowHint("Presiona M para abrir el mapa.", tutorialMessageDuration);
        }
        else if (MessageSystem.instance != null)
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

        if (freezeGameWhenMapOpen)
        {
            Time.timeScale = mapOpen ? 0f : 1f;
        }

        Cursor.lockState = showCursorWhenMapOpen
            ? (mapOpen ? CursorLockMode.None : CursorLockMode.Locked)
            : CursorLockMode.Locked;
        Cursor.visible = showCursorWhenMapOpen && mapOpen;

        if (mapOpen)
        {
            GameplayRunState.RegisterFirstMapOpen();
            return;
        }

        if (GameplayRunState.TryConsumeFirstMapCloseInsight())
        {
            TutorialHintOverlay.ShowHint("Los puntos rosas del mapa parecen marcar los otros bidones.", 3.4f);
        }
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

    private void ResolveMapIconsLayer()
    {
        if (mapIconsLayer >= 0)
        {
            return;
        }

        mapIconsLayer = LayerMask.NameToLayer("MapIcons");
    }

    private void ApplyMapIconCameraFiltering()
    {
        ResolveMapCamera();
        ResolveMapIconsLayer();

        if (mapIconsLayer < 0)
        {
            return;
        }

        int mapIconsMask = 1 << mapIconsLayer;
        Camera[] cameras = FindObjectsOfType<Camera>(true);

        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];
            if (camera == null)
            {
                continue;
            }

            bool isDedicatedMapCamera = camera == mapCamera
                || (camera.targetTexture != null && camera.name == "MapCamera");

            if (isDedicatedMapCamera)
            {
                camera.cullingMask |= mapIconsMask;
            }
            else
            {
                camera.cullingMask &= ~mapIconsMask;
            }
        }
    }

    private void ApplyMapCameraState()
    {
        ResolveMapCamera();
        ApplyMapIconCameraFiltering();

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
