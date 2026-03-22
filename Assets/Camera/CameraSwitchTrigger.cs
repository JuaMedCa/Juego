using Cinemachine;
using UnityEngine;

public class CameraSwitchTrigger : MonoBehaviour
{
    public CinemachineVirtualCamera isoCam;
    public CinemachineVirtualCamera fpsCam;
    public PlayerMovemnt playerMovement;

    CinemachinePOV fpsPOV;
    bool isFpsActive = false; // Bandera para alternar entre cámaras

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

        // Alternar entre cámaras al pasar por el trigger
        if (isFpsActive)
        {
            // Cambiar a cámara isométrica
            isoCam.Priority = 20;
            fpsCam.Priority = 0;

            playerMovement.ExitFPS();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // Cambiar a cámara FPS
            ResetFpsLook();
            fpsCam.Priority = 20;
            isoCam.Priority = 0;

            playerMovement.EnterFPS();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Alternar el estado de la cámara
        isFpsActive = !isFpsActive;
    }
}