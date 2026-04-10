using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerMovemnt : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 4f;

    [Header("Isometric Rotation")]
    public float rotationSpeed = 10f;

    [Header("FPS Body Follow Camera")]
    public float bodyTurnSpeed = 12f;
    public float turnDeadZone = 1.5f;

    [Header("Mode")]
    public bool isFPS = false;

    [Tooltip("Asigna aquí el Transform de la Main Camera.")]
    public Transform fpsCamera;

    [Header("Hide Body In FPS")]
    public Renderer[] bodyRenderers;
    public bool keepShadows = false;

    [Header("Transition")]
    public float enterFPSTransitionDuration = 0.8f;
    public float exitFPSTransitionDuration = 0.6f;

    private Animator animator;
    private Rigidbody rb;

    private Vector3 forward;
    private Vector3 right;
    private Vector3 inputDirection;

    private bool isTransitioning = false;
    private Coroutine transitionCoroutine;

    private Quaternion desiredRotation;
    private bool hasDesiredRotation = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        ResolveFpsCamera();
        RecalculateIsoDirections();
        SetBodyVisible(true);
    }

    void Update()
    {
        if (isTransitioning)
        {
            inputDirection = Vector3.zero;

            if (animator != null)
            {
                animator.SetFloat("VelX", 0f);
                animator.SetFloat("VelY", 0f);
            }

            return;
        }

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 direction = isFPS
            ? GetFpsMoveDirection(h, v)
            : GetIsometricMoveDirection(h, v);

        inputDirection = Vector3.ClampMagnitude(direction, 1f);

        if (animator != null)
        {
            animator.SetFloat("VelX", h);
            animator.SetFloat("VelY", v);
        }

        if (!isFPS && inputDirection.sqrMagnitude >= 0.01f)
        {
            desiredRotation = Quaternion.Slerp(
                rb.rotation,
                Quaternion.LookRotation(inputDirection),
                rotationSpeed * Time.deltaTime
            );

            hasDesiredRotation = true;
        }
    }

    void FixedUpdate()
    {
        if (isTransitioning) return;

        if (inputDirection.sqrMagnitude >= 0.01f)
        {
            Vector3 newPosition = rb.position + inputDirection * speed * Time.fixedDeltaTime;
            rb.MovePosition(newPosition);
        }

        if (hasDesiredRotation)
        {
            rb.MoveRotation(desiredRotation);
            hasDesiredRotation = false;
        }
    }

    public void EnterFPS()
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(EnterFPSRoutine());
    }

    public void ExitFPS()
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(ExitFPSRoutine());
    }

    private IEnumerator EnterFPSRoutine()
    {
        Debug.Log("Inicio transición FPS: " + Time.time);

        isTransitioning = true;
        inputDirection = Vector3.zero;

        if (animator != null)
        {
            animator.SetFloat("VelX", 0f);
            animator.SetFloat("VelY", 0f);
        }

        SetBodyVisible(true);

        yield return new WaitForSeconds(2.0f);

        Debug.Log("Ahora sí oculto cuerpo: " + Time.time);

        isFPS = true;
        ResolveFpsCamera();
        SetBodyVisible(false);

        isTransitioning = false;
        transitionCoroutine = null;
    }

    private IEnumerator ExitFPSRoutine()
    {
        isTransitioning = true;
        inputDirection = Vector3.zero;

        if (animator != null)
        {
            animator.SetFloat("VelX", 0f);
            animator.SetFloat("VelY", 0f);
        }

        // Aquí aparece el cuerpo desde el inicio para que se vea en la transición
        SetBodyVisible(true);

        yield return new WaitForSeconds(exitFPSTransitionDuration);

        // Al terminar, vuelve oficialmente a isométrico
        isFPS = false;

        Vector3 euler = rb.rotation.eulerAngles;
        Quaternion flatRotation = Quaternion.Euler(0f, euler.y, 0f);
        rb.MoveRotation(flatRotation);

        RecalculateIsoDirections();

        isTransitioning = false;
        transitionCoroutine = null;
    }

    private void SetBodyVisible(bool visible)
    {
        if (bodyRenderers == null || bodyRenderers.Length == 0) return;

        foreach (Renderer rend in bodyRenderers)
        {
            if (rend == null) continue;

            if (visible)
            {
                rend.enabled = true;
                rend.shadowCastingMode = ShadowCastingMode.On;
            }
            else
            {
                if (keepShadows)
                {
                    rend.enabled = true;
                    rend.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
                else
                {
                    rend.enabled = false;
                }
            }
        }
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
            forward = GetPlanarDirection(transform.forward);

        if (right.sqrMagnitude <= 0.0001f)
            right = GetPlanarDirection(transform.right);
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
            return;

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        float angle = Quaternion.Angle(rb.rotation, targetRotation);

        if (angle <= turnDeadZone)
            return;

        desiredRotation = Quaternion.Slerp(
            rb.rotation,
            targetRotation,
            bodyTurnSpeed * Time.deltaTime
        );

        hasDesiredRotation = true;
    }

    private Transform ResolveGameplayCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
            return mainCamera.transform;

        return ResolveFpsCamera();
    }

    private Transform ResolveFpsCamera()
    {
        if (fpsCamera != null && fpsCamera != transform)
            return fpsCamera;

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
            vector.Normalize();

        return vector;
    }
}