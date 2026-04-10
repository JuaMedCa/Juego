using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupItem : MonoBehaviour
{
    public string itemName = "Tanque de gas";
    public FuelSequenceManager fuelManager;

    [HideInInspector]
    public bool playerInside = false;


    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Entró algo: " + other.name);

        if (other.CompareTag("Player"))
        {
            Debug.Log("Es el jugador");
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
