using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FuelSequenceManager : MonoBehaviour
{
    public List<GameObject> fuelIcons;

    private int currentIndex = 0;

    void Start()
    {
        foreach (GameObject icon in fuelIcons)
        {
            icon.SetActive(false);
        }

        if (fuelIcons.Count > 0)
        {
            fuelIcons[0].SetActive(true);
        }
    }

    public void OnFuelCollected()
    {
        if (currentIndex < fuelIcons.Count)
        {
            fuelIcons[currentIndex].SetActive(false);
        }

        currentIndex++;

        if (currentIndex < fuelIcons.Count)
        {
            fuelIcons[currentIndex].SetActive(true);
        }
    }
}
