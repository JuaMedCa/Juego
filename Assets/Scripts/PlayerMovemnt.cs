using UnityEngine;

public class PlayerMovemnt : MonoBehaviour
{
    public float speed = 4;
    public float rotationSpeed = 10;
    public float mouseSensitivity = 300f;

    public bool isFPS = false;
    public Transform fpsCamera;   // Main Camera

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
    float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
    transform.Rotate(Vector3.up * mouseX);

            forward = fpsCamera.forward;
            right = fpsCamera.right;

            forward.y = 0;
            right.y = 0;

            forward.Normalize();
            right.Normalize();

            direction = forward * v + right * h;
        }
        else
        {
            // Movimiento isométrico
            direction = right * h + forward * v;
        }

        // Animator
        animator.SetFloat("VelX", h);
        animator.SetFloat("VelY", v);

        if (direction.magnitude >= 0.1f)
        {
            transform.position += direction * speed * Time.deltaTime;

            // Rotación solo en isométrico
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

        // Resetear rotación solo en Y (evita perspectiva rara)
        Vector3 euler = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(0, euler.y, 0);

        RecalculateIsoDirections();
    }


    private void RecalculateIsoDirections()
    {
        forward = Camera.main.transform.forward;
        right = Camera.main.transform.right;

        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();
    }
}
