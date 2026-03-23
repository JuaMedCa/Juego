using UnityEngine;

public class PlayerMovemnt : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 4f;

    [Header("Isometric Rotation")]
    public float rotationSpeed = 10f;

    [Header("FPS Body Follow Camera")]
    public float bodyTurnSpeed = 12f;   // qué tan rápido el cuerpo sigue la cámara
    public float turnDeadZone = 1.5f;   // grados para evitar que gire por micro movimientos

    public bool isFPS = false;

    [Tooltip("Asigna aquí el Transform de la Main Camera (la cámara real que mueve Cinemachine).")]
    public Transform fpsCamera;

    private Animator animator;
    private Vector3 forward;
    private Vector3 right;

    void Start()
    {
        animator = GetComponent<Animator>();
        ResolveFpsCamera();
        RecalculateIsoDirections();
    }

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 direction = isFPS
            ? GetFpsMoveDirection(h, v)
            : GetIsometricMoveDirection(h, v);

        direction = Vector3.ClampMagnitude(direction, 1f);

        // Animator
        if (animator != null)
        {
            animator.SetFloat("VelX", h);
            animator.SetFloat("VelY", v);
        }

        // Movimiento
        if (direction.sqrMagnitude >= 0.01f)
        {
            transform.position += direction * speed * Time.deltaTime;

            // Rotación SOLO en isométrico (en FPS la controla el bloque de "seguir cámara")
            if (!isFPS)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }
    }

    // ======== MÉTODOS CLAVE ========

    public void EnterFPS()
    {
        isFPS = true;
        ResolveFpsCamera();
    }

    public void ExitFPS()
    {
        isFPS = false;

        // Resetear rotación solo en Y (evita inclinaciones raras)
        Vector3 euler = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(0, euler.y, 0);

        RecalculateIsoDirections();
    }

    private void RecalculateIsoDirections()
    {
        Transform viewTransform = ResolveGameplayCamera();

        if (viewTransform == null)
        {
            forward = GetPlanarDirection(transform.forward);
            right = GetPlanarDirection(transform.right);
            return;
        }

        forward = GetPlanarDirection(viewTransform.forward);
        right = GetPlanarDirection(viewTransform.right);

        if (forward.sqrMagnitude <= 0.0001f)
        {
            forward = GetPlanarDirection(transform.forward);
        }

        if (right.sqrMagnitude <= 0.0001f)
        {
            right = GetPlanarDirection(transform.right);
        }
    }

    private Vector3 GetFpsMoveDirection(float h, float v)
    {
        Transform viewTransform = ResolveFpsCamera();
        Vector3 lookForward = viewTransform != null
            ? GetPlanarDirection(viewTransform.forward)
            : GetPlanarDirection(transform.forward);
        Vector3 lookRight = viewTransform != null
            ? GetPlanarDirection(viewTransform.right)
            : GetPlanarDirection(transform.right);

        RotateBodyTowards(lookForward);
        return lookForward * v + lookRight * h;
    }

    private Vector3 GetIsometricMoveDirection(float h, float v)
    {
        RecalculateIsoDirections();
        return right * h + forward * v;
    }

    private void RotateBodyTowards(Vector3 lookDirection)
    {
        if (lookDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        float angle = Quaternion.Angle(transform.rotation, targetRotation);

        if (angle <= turnDeadZone)
        {
            return;
        }

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            bodyTurnSpeed * Time.deltaTime
        );
    }

    private Transform ResolveGameplayCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            return mainCamera.transform;
        }

        return ResolveFpsCamera();
    }

    private Transform ResolveFpsCamera()
    {
        if (fpsCamera != null && fpsCamera != transform)
        {
            return fpsCamera;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            fpsCamera = mainCamera.transform;
            return fpsCamera;
        }

        return null;
    }

    private static Vector3 GetPlanarDirection(Vector3 vector)
    {
        vector.y = 0f;

        if (vector.sqrMagnitude > 0.0001f)
        {
            vector.Normalize();
        }

        return vector;
    }
}
