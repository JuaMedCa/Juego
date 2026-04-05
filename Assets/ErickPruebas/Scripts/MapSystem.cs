using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapSystem : MonoBehaviour
{
    public GameObject mapPanel;

    private bool hasMap = false;
    private bool isMapOpen = false;

    void Update()
    {
        if (!hasMap) return;

        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleMap();
        }
    }

    public void UnlockMap()
    {
        hasMap = true;
    }

    void ToggleMap()
    {
        isMapOpen = !isMapOpen;
        mapPanel.SetActive(isMapOpen);

        if (isMapOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
