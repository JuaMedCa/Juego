using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GasIconFollow : MonoBehaviour
{
    public Transform target;

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 pos = target.position;
        pos.y += 2f; // Esta es la altura del icono

        transform.position = pos;
    }
}
