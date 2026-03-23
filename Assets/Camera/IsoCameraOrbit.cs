using Cinemachine;
using UnityEngine;

[DisallowMultipleComponent]
public class IsoCameraOrbit : MonoBehaviour
{
    [Header("Orbit")]
    [SerializeField] private float mouseSensitivity = 6f;
    [SerializeField] private bool requireRightMouseButton = true;
    [SerializeField] private bool invertX = false;

    private CinemachineVirtualCamera virtualCamera;
    private float pitch;
    private float yaw;
    private float roll;

    public float MouseSensitivity
    {
        get => mouseSensitivity;
        set => mouseSensitivity = Mathf.Clamp(value, 1f, 20f);
    }

    void Awake()
    {
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        CacheAngles();
    }

    void OnEnable()
    {
        CacheAngles();
    }

    void LateUpdate()
    {
        if (virtualCamera != null && virtualCamera.Priority <= 0)
        {
            return;
        }

        if (requireRightMouseButton && !Input.GetMouseButton(1))
        {
            return;
        }

        float mouseX = Input.GetAxis("Mouse X");
        if (Mathf.Abs(mouseX) < 0.0001f)
        {
            return;
        }

        float direction = invertX ? -1f : 1f;
        yaw = Mathf.Repeat(yaw + (mouseX * MouseSensitivity * direction), 360f);
        transform.rotation = Quaternion.Euler(pitch, yaw, roll);
    }

    private void CacheAngles()
    {
        Vector3 euler = transform.eulerAngles;
        pitch = euler.x;
        yaw = euler.y;
        roll = euler.z;
    }
}
