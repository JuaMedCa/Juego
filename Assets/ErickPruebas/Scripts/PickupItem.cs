using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupItem : MonoBehaviour
{
    public string itemName = "Tanque de gas";
    public GameObject mapIconPrefab;
    private GameObject iconInstance;

    void Start()
    {
        if (mapIconPrefab != null)
        {
            iconInstance = Instantiate(mapIconPrefab);

            GasIconFollow follow = iconInstance.AddComponent<GasIconFollow>();
            follow.target = this.transform;
        }

        if (iconInstance != null)
        {
            Destroy(iconInstance);
        }
    }

    [HideInInspector]
    public bool playerInside = false;

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

}
