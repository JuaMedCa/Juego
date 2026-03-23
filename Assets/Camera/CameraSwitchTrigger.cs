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

    public static CameraMode CurrentMode => currentMode;
    public static bool IsFirstPersonActive => isFpsApplied;

    void Awake()
    {
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
        if (!HasRequiredReferences())
        {
            return;
        }

        if (activeController == null || activeController == this || !activeController.HasRequiredReferences())
        {
            activeController = this;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        insideTriggerCount++;
        RefreshGlobalCameraState();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        insideTriggerCount = Mathf.Max(0, insideTriggerCount - 1);
        RefreshGlobalCameraState();
    }

    private void RefreshCameraState()
    {
        if (!HasRequiredReferences())
        {
            ApplyCursorState();
            return;
        }

        bool shouldUseFps = currentMode == CameraMode.PrimeraPersona || insideTriggerCount > 0;

        if (shouldUseFps != isFpsApplied)
        {
            bool changed = shouldUseFps ? ActivateFps() : ActivateIsometric();
            if (changed)
            {
                isFpsApplied = shouldUseFps;
            }
        }

        ApplyCursorState();
    }

    private bool ActivateFps()
    {
        if (!HasRequiredReferences())
        {
            return false;
        }

        fpsCam.Priority = 20;
        isoCam.Priority = 0;
        playerMovement.EnterFPS();
        return true;
    }

    private bool ActivateIsometric()
    {
        if (!HasRequiredReferences())
        {
            return false;
        }

        isoCam.Priority = 20;
        fpsCam.Priority = 0;
        playerMovement.ExitFPS();
        return true;
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

    private bool HasRequiredReferences()
    {
        return isoCam != null && fpsCam != null && playerMovement != null;
    }
}
