using Cinemachine;
using UnityEngine;

public class CameraSwitchTrigger : MonoBehaviour
{
    public enum CameraMode
    {
        Aerea,
        PrimeraPersona
    }

    public CinemachineVirtualCamera isoCam;
    public CinemachineVirtualCamera fpsCam;
    public PlayerMovemnt playerMovement;

    private static CameraMode currentMode = CameraMode.Aerea;
    private static int insideTriggerCount = 0;
    private static bool isFpsApplied = false;
    private static bool isMenuOpen = true;
    private static CameraSwitchTrigger activeController;

    private CinemachinePOV fpsPOV;

    public static CameraMode CurrentMode => currentMode;
    public static bool IsFirstPersonActive => isFpsApplied;

    void Awake()
    {
        if (fpsCam != null)
        {
            fpsPOV = fpsCam.GetCinemachineComponent<CinemachinePOV>();
        }

        RegisterAsActiveController();
    }

    void OnEnable()
    {
        RegisterAsActiveController();
        RefreshGlobalCameraState();
    }

    void OnDisable()
    {
        if (activeController == this)
        {
            activeController = null;
        }
    }

    public static void SetCameraMode(CameraMode mode)
    {
        currentMode = mode;
        RefreshGlobalCameraState();
    }

    public static void SetMenuOpen(bool menuOpen)
    {
        isMenuOpen = menuOpen;

        if (activeController != null)
        {
            activeController.ApplyCursorState();
        }
    }

    public static void RefreshGlobalCameraState()
    {
        if (activeController != null)
        {
            activeController.RefreshCameraState();
        }
    }

    private void RegisterAsActiveController()
    {
        if (activeController == null && isoCam != null && fpsCam != null && playerMovement != null)
        {
            activeController = this;
        }
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

        insideTriggerCount++;
        RefreshCameraState();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        insideTriggerCount = Mathf.Max(0, insideTriggerCount - 1);
        RefreshCameraState();
    }

    private void RefreshCameraState()
    {
        bool shouldUseFps = currentMode == CameraMode.PrimeraPersona || insideTriggerCount > 0;

        if (shouldUseFps != isFpsApplied)
        {
            if (shouldUseFps)
            {
                ActivateFps();
            }
            else
            {
                ActivateIsometric();
            }

            isFpsApplied = shouldUseFps;
        }

        ApplyCursorState();
    }

    private void ActivateFps()
    {
        if (fpsCam == null || isoCam == null || playerMovement == null)
        {
            return;
        }

        ResetFpsLook();
        fpsCam.Priority = 20;
        isoCam.Priority = 0;
        playerMovement.EnterFPS();
    }

    private void ActivateIsometric()
    {
        if (fpsCam == null || isoCam == null || playerMovement == null)
        {
            return;
        }

        isoCam.Priority = 20;
        fpsCam.Priority = 0;
        playerMovement.ExitFPS();
    }

    private void ApplyCursorState()
    {
        if (isMenuOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (isFpsApplied)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
