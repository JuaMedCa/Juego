using Cinemachine;
using UnityEngine;

public class CameraSwitchTrigger : MonoBehaviour
{
    public CinemachineVirtualCamera isoCam;
    public CinemachineVirtualCamera fpsCam;
    public PlayerMovemnt playerMovement;

    CinemachinePOV fpsPOV;

    void Awake()
    {
        fpsPOV = fpsCam.GetCinemachineComponent<CinemachinePOV>();
    }

    void ResetFpsLook()
    {
        if (fpsPOV == null) return;

        fpsPOV.m_HorizontalAxis.Value = 0f; // yaw
        fpsPOV.m_VerticalAxis.Value = 0f;   // pitch
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        ResetFpsLook(); // <-- clave

        fpsCam.Priority = 20;
        isoCam.Priority = 0;

        playerMovement.EnterFPS();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        isoCam.Priority = 20;
        fpsCam.Priority = 0;

        playerMovement.ExitFPS();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
