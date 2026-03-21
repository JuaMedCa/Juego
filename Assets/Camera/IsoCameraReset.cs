using UnityEngine;
using Cinemachine;

public class IsoCameraReset : MonoBehaviour
{
    private CinemachineVirtualCamera vcam;
    private CinemachineFramingTransposer framing;

    private float screenX;
    private float screenY;
    private float softW;
    private float softH;
    private float deadW;
    private float deadH;
    private float distance;
    private float fieldOfView;   // 🔥 NUEVO

    void Awake()
    {
        vcam = GetComponent<CinemachineVirtualCamera>();
        framing = vcam.GetCinemachineComponent<CinemachineFramingTransposer>();

        // Guardar estado ISO
        screenX = framing.m_ScreenX;
        screenY = framing.m_ScreenY;
        softW = framing.m_SoftZoneWidth;
        softH = framing.m_SoftZoneHeight;
        deadW = framing.m_DeadZoneWidth;
        deadH = framing.m_DeadZoneHeight;
        distance = framing.m_CameraDistance;

        fieldOfView = vcam.m_Lens.FieldOfView; // 🔥
    }

    public void Restore()
    {
        framing.m_ScreenX = screenX;
        framing.m_ScreenY = screenY;
        framing.m_SoftZoneWidth = softW;
        framing.m_SoftZoneHeight = softH;
        framing.m_DeadZoneWidth = deadW;
        framing.m_DeadZoneHeight = deadH;
        framing.m_CameraDistance = distance;

        vcam.m_Lens.FieldOfView = fieldOfView; // 🔥
    }
}
