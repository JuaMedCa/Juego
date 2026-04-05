using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapCameraFollow : MonoBehaviour
{
    public Transform player;

    void LateUpdate()
    {
        Vector3 pos = transform.position;
        pos.x = player.position.x;
        pos.z = player.position.z;
        transform.position = pos;
    }
}
