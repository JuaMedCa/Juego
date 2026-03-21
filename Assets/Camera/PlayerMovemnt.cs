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

    private Vector3 forward, right;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        RecalculateIsoDirections();
    }

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 direction = Vector3.zero;

        if (isFPS)
        {
            // 1) Hacer que el cuerpo siga el yaw (Y) de la cámara, suave y controlado
            if (fpsCamera != null)
            {
                Vector3 lookDir = fpsCamera.forward;
                lookDir.y = 0f;

                if (lookDir.sqrMagnitude > 0.0001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookDir);

                    float angle = Quaternion.Angle(transform.rotation, targetRot);
                    if (angle > turnDeadZone)
                    {
                        transform.rotation = Quaternion.Slerp(
                            transform.rotation,
                            targetRot,
                            bodyTurnSpeed * Time.deltaTime
                        );
                    }
                }
            }

            // 2) Movimiento relativo al cuerpo (ya alineado con la cámara)
            Vector3 f = transform.forward;
            Vector3 r = transform.right;

            f.y = 0f;
            r.y = 0f;

            f.Normalize(); 
            r.Normalize();

            direction = f * v + r * h;
        }
        else
        {
            RecalculateIsoDirections();   // <- fuerza recalcular al volver a iso
            direction = right * h + forward * v;
        }


        // Animator
        if (animator != null)
        {
            animator.SetFloat("VelX", h);
            animator.SetFloat("VelY", v);
        }

        // Movimiento
        if (direction.magnitude >= 0.1f)
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
        // Direcciones isométricas basadas en la cámara principal actual
        forward = Camera.main.transform.forward;
        right = Camera.main.transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();
    }
}
