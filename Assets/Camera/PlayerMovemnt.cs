using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerMovemnt : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 4f;
    [SerializeField] private float runMultiplier = 1.65f;
    [SerializeField] private KeyCode runKey = KeyCode.LeftShift;

    [Header("Crouch")]
    [SerializeField] private float crouchSpeedMultiplier = 0.55f;
    [SerializeField] private float crouchHeight = 1.15f;
    [SerializeField] private float crouchTransitionSpeed = 8f;
    [SerializeField] private float crouchCameraOffset = 0.45f;
    [SerializeField] private KeyCode crouchKey = KeyCode.C;
    [SerializeField] private KeyCode alternateCrouchKey = KeyCode.LeftControl;
    [SerializeField] private Transform crouchViewTarget;

    [Header("Isometric Rotation")]
    public float rotationSpeed = 10f;

    [Header("FPS Body Follow Camera")]
    public float bodyTurnSpeed = 12f;
    public float turnDeadZone = 1.5f;

    [Header("Mode")]
    public bool isFPS = false;

    [Tooltip("Asigna aqui el Transform de la Main Camera.")]
    public Transform fpsCamera;

    [Header("Hide Body In FPS")]
    public Renderer[] bodyRenderers;
    public bool keepShadows = false;

    [Header("Transition")]
    public float enterFPSTransitionDuration = 0.8f;
    public float exitFPSTransitionDuration = 0.6f;

    private Animator animator;
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private Transform resolvedCrouchViewTarget;

    private Vector3 forward;
    private Vector3 right;
    private Vector3 inputDirection;

    private bool isTransitioning = false;
    private Coroutine transitionCoroutine;

    private Quaternion desiredRotation;
    private bool hasDesiredRotation = false;
    private bool isRunning = false;
    private bool isCrouching = false;
    private float standingColliderHeight;
    private Vector3 standingColliderCenter;
    private float standingColliderBottom;
    private Vector3 standingViewTargetLocalPosition;

    public bool IsRunning => isRunning;
    public bool IsCrouching => isCrouching;
    public float CurrentMoveSpeed => speed * GetMovementSpeedMultiplier();

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        ResolveFpsCamera();
        ResolveCrouchViewTarget();
        CacheStandingGeometry();
        RecalculateIsoDirections();
        SetBodyVisible(true);
        ApplyCrouchStateImmediate();
    }

    void Update()
    {
        if (isTransitioning)
        {
            inputDirection = Vector3.zero;
            isRunning = false;

            if (animator != null)
            {
                animator.SetFloat("VelX", 0f);
                animator.SetFloat("VelY", 0f);
            }

            UpdateCrouchState(Time.deltaTime);
            return;
        }

        HandleCrouchInput();

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 direction = isFPS
            ? GetFpsMoveDirection(h, v)
            : GetIsometricMoveDirection(h, v);

        inputDirection = Vector3.ClampMagnitude(direction, 1f);
        isRunning = ShouldRun(h, v);

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

        UpdateCrouchState(Time.deltaTime);
    }

    void FixedUpdate()
    {
        if (isTransitioning) return;

        if (inputDirection.sqrMagnitude >= 0.01f)
        {
            Vector3 newPosition = rb.position + inputDirection * CurrentMoveSpeed * Time.fixedDeltaTime;
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
        isTransitioning = true;
        inputDirection = Vector3.zero;
        isRunning = false;

        if (animator != null)
        {
            animator.SetFloat("VelX", 0f);
            animator.SetFloat("VelY", 0f);
        }

        SetBodyVisible(true);

        yield return new WaitForSeconds(enterFPSTransitionDuration);

        isFPS = true;
        ResolveFpsCamera();
        SetBodyVisible(false);
        ApplyCrouchStateImmediate();

        isTransitioning = false;
        transitionCoroutine = null;
    }

    private IEnumerator ExitFPSRoutine()
    {
        isTransitioning = true;
        inputDirection = Vector3.zero;
        isRunning = false;

        if (animator != null)
        {
            animator.SetFloat("VelX", 0f);
            animator.SetFloat("VelY", 0f);
        }

        SetBodyVisible(true);

        yield return new WaitForSeconds(exitFPSTransitionDuration);

        isFPS = false;

        Vector3 euler = rb.rotation.eulerAngles;
        Quaternion flatRotation = Quaternion.Euler(0f, euler.y, 0f);
        rb.MoveRotation(flatRotation);

        RecalculateIsoDirections();
        ApplyCrouchStateImmediate();

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
        {
            if (fpsCamera.GetComponent<Camera>() != null)
                return fpsCamera;

            Camera fallbackCamera = Camera.main;
            if (fallbackCamera == null)
                return fpsCamera;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            fpsCamera = mainCamera.transform;
            return fpsCamera;
        }

        return fpsCamera != transform ? fpsCamera : null;
    }

    private void HandleCrouchInput()
    {
        bool crouchPressed = Input.GetKeyDown(crouchKey) || Input.GetKeyDown(alternateCrouchKey);
        if (!crouchPressed)
        {
            return;
        }

        if (isCrouching)
        {
            TryStandUp();
            return;
        }

        isCrouching = true;
        isRunning = false;
    }

    private bool ShouldRun(float h, float v)
    {
        if (isCrouching || !Input.GetKey(runKey))
        {
            return false;
        }

        return new Vector2(h, v).sqrMagnitude > 0.01f;
    }

    private float GetMovementSpeedMultiplier()
    {
        if (isCrouching)
        {
            return Mathf.Max(0.1f, crouchSpeedMultiplier);
        }

        if (isRunning)
        {
            return Mathf.Max(1f, runMultiplier);
        }

        return 1f;
    }

    private void ResolveCrouchViewTarget()
    {
        if (crouchViewTarget != null)
        {
            resolvedCrouchViewTarget = crouchViewTarget;
            return;
        }

        resolvedCrouchViewTarget = FindDescendantByName(transform, "FPSCameraTarget");
        if (resolvedCrouchViewTarget != null)
        {
            crouchViewTarget = resolvedCrouchViewTarget;
            return;
        }

        if (fpsCamera != null && fpsCamera != transform && fpsCamera.IsChildOf(transform))
        {
            resolvedCrouchViewTarget = fpsCamera;
            crouchViewTarget = fpsCamera;
        }
    }

    private void CacheStandingGeometry()
    {
        if (capsuleCollider != null)
        {
            standingColliderHeight = capsuleCollider.height;
            standingColliderCenter = capsuleCollider.center;
            standingColliderBottom = standingColliderCenter.y - (standingColliderHeight * 0.5f);
        }

        if (resolvedCrouchViewTarget != null)
        {
            standingViewTargetLocalPosition = resolvedCrouchViewTarget.localPosition;
        }
    }

    private void UpdateCrouchState(float deltaTime)
    {
        if (capsuleCollider != null)
        {
            float minimumHeight = Mathf.Max(capsuleCollider.radius * 2f, 0.2f);
            float targetHeight = isCrouching
                ? Mathf.Clamp(crouchHeight, minimumHeight, standingColliderHeight)
                : standingColliderHeight;

            float nextHeight = Mathf.MoveTowards(capsuleCollider.height, targetHeight, crouchTransitionSpeed * deltaTime);
            capsuleCollider.height = nextHeight;
            capsuleCollider.center = new Vector3(
                standingColliderCenter.x,
                standingColliderBottom + (nextHeight * 0.5f),
                standingColliderCenter.z);
        }

        if (resolvedCrouchViewTarget != null)
        {
            Vector3 crouchedViewPosition = standingViewTargetLocalPosition + Vector3.down * Mathf.Max(0f, crouchCameraOffset);
            Vector3 targetPosition = isCrouching ? crouchedViewPosition : standingViewTargetLocalPosition;
            resolvedCrouchViewTarget.localPosition = Vector3.MoveTowards(
                resolvedCrouchViewTarget.localPosition,
                targetPosition,
                Mathf.Max(0.01f, crouchTransitionSpeed * deltaTime));
        }
    }

    private void ApplyCrouchStateImmediate()
    {
        if (capsuleCollider != null)
        {
            float minimumHeight = Mathf.Max(capsuleCollider.radius * 2f, 0.2f);
            float targetHeight = isCrouching
                ? Mathf.Clamp(crouchHeight, minimumHeight, standingColliderHeight)
                : standingColliderHeight;

            capsuleCollider.height = targetHeight;
            capsuleCollider.center = new Vector3(
                standingColliderCenter.x,
                standingColliderBottom + (targetHeight * 0.5f),
                standingColliderCenter.z);
        }

        if (resolvedCrouchViewTarget != null)
        {
            Vector3 crouchedViewPosition = standingViewTargetLocalPosition + Vector3.down * Mathf.Max(0f, crouchCameraOffset);
            resolvedCrouchViewTarget.localPosition = isCrouching ? crouchedViewPosition : standingViewTargetLocalPosition;
        }
    }

    private void TryStandUp()
    {
        if (!CanStandUp())
        {
            return;
        }

        isCrouching = false;
    }

    private bool CanStandUp()
    {
        if (capsuleCollider == null)
        {
            return true;
        }

        float radiusScale = Mathf.Max(
            Mathf.Abs(transform.lossyScale.x),
            Mathf.Abs(transform.lossyScale.z));
        float heightScale = Mathf.Abs(transform.lossyScale.y);
        float worldRadius = Mathf.Max(0.01f, (capsuleCollider.radius * radiusScale) - 0.02f);
        float worldHeight = standingColliderHeight * heightScale;
        float halfDistance = Mathf.Max(0f, (worldHeight * 0.5f) - worldRadius);
        Vector3 targetCenter = new Vector3(
            standingColliderCenter.x,
            standingColliderBottom + (standingColliderHeight * 0.5f),
            standingColliderCenter.z);
        Vector3 worldCenter = transform.TransformPoint(targetCenter);
        Vector3 pointTop = worldCenter + (transform.up * halfDistance);
        Vector3 pointBottom = worldCenter - (transform.up * halfDistance);

        bool previousState = capsuleCollider.enabled;
        capsuleCollider.enabled = false;
        bool blocked = Physics.CheckCapsule(
            pointTop,
            pointBottom,
            worldRadius,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore);
        capsuleCollider.enabled = previousState;

        return !blocked;
    }

    private static Transform FindDescendantByName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == targetName)
            {
                return child;
            }

            Transform nestedMatch = FindDescendantByName(child, targetName);
            if (nestedMatch != null)
            {
                return nestedMatch;
            }
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
