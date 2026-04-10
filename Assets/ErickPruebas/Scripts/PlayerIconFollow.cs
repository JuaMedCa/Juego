using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIconFollow : MonoBehaviour
{
    public Transform player;
    public float height = 2f;

    void Update()
    {
        if (player == null) return;

        Vector3 pos = player.position;
        pos.y += height;

        transform.position = pos;

        // Rotación
        transform.rotation = Quaternion.Euler(90f, player.eulerAngles.y, 0f);
    }
}
