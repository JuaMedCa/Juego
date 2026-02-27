using UnityEngine;
using Cinemachine;

public class CameraSwitchTrigger : MonoBehaviour
{
    public CinemachineVirtualCamera isoCam;
    public CinemachineVirtualCamera fpsCam;
    public PlayerMovemnt playerMovement;

    private IsoCameraReset isoReset;

    void Awake()
    {
        isoReset = isoCam.GetComponent<IsoCameraReset>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

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

        if (isoReset != null)
            isoReset.Restore();

        playerMovement.ExitFPS();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

}
