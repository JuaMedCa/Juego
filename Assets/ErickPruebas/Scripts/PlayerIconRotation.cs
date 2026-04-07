using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIconRotation : MonoBehaviour
{
    public Transform player;

    void Update()
    {
        if (player == null) return;

        Vector3 rot = transform.eulerAngles;
        rot.y = player.eulerAngles.y;
        transform.eulerAngles = rot;
    }
}
