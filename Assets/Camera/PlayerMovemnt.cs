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

    [Header("Legacy Isometric")]
    public float rotationSpeed = 10f;

    [Header("FPS Body Follow Camera")]
    public float bodyTurnSpeed = 12f;
    public float turnDeadZone = 1.5f;

    [Header("Mode")]
    public bool isFPS = true;

    [Tooltip("Asigna aqui el Transform de la Main Camera.")]
    public Transform fpsCamera;

    [Header("Hide Body In FPS")]
    public Renderer[] bodyRenderers;
    public bool keepShadows = false;

    [Header("Legacy Transition")]
    public float enterFPSTransitionDuration = 0f;
    public float exitFPSTransitionDuration = 0f;

    [Header("Footsteps")]
    [SerializeField] private string walkFootstepsResourceFolder = "Footsteps/Walk";
    [SerializeField] private string runFootstepsResourceFolder = "Footsteps/Run";
    [SerializeField] private float walkFootstepInterval = 0.46f;
    [SerializeField] private float runFootstepInterval = 0.29f;
    [SerializeField] private float crouchFootstepInterval = 0.62f;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.26f;
    [SerializeField, Range(0f, 0.3f)] private float footstepPitchVariance = 0.04f;

    private Animator animator;
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private Transform resolvedCrouchViewTarget;
    private AudioSource footstepAudioSource;
    private AudioClip[] walkFootstepClips = System.Array.Empty<AudioClip>();
    private AudioClip[] runFootstepClips = System.Array.Empty<AudioClip>();
    private Vector3 inputDirection;
    private Quaternion desiredRotation;
    private bool hasDesiredRotation;
    private bool isRunning;
    private bool isCrouching;
    private float standingColliderHeight;
    private Vector3 standingColliderCenter;
    private float standingColliderBottom;
    private Vector3 standingViewTargetLocalPosition;
    private float footstepTimer;

    public bool IsRunning => isRunning;
    public bool IsCrouching => isCrouching;
    public float CurrentMoveSpeed => speed * GetMovementSpeedMultiplier();

    private void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        ResolveFpsCamera();
        ResolveCrouchViewTarget();
        CacheStandingGeometry();
        EnsureFootstepAudioSource();
        LoadFootstepClips();
        ApplyFirstPersonStateImmediate();
    }

    private void OnEnable()
    {
        ApplyFirstPersonStateImmediate();
        footstepTimer = 0f;
    }

    private void OnDisable()
    {
        if (footstepAudioSource != null)
        {
            footstepAudioSource.Stop();
        }
    }

    private void Update()
    {
        ResolveFpsCamera();
        HandleCrouchInput();

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        inputDirection = Vector3.ClampMagnitude(GetFpsMoveDirection(horizontal, vertical), 1f);
        isRunning = inputDirection.sqrMagnitude > 0.01f && ShouldRun(horizontal, vertical);

        if (animator != null)
        {
            animator.SetFloat("VelX", horizontal);
            animator.SetFloat("VelY", vertical);
        }

        UpdateCrouchState(Time.deltaTime);
        UpdateFootsteps(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

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
        ApplyFirstPersonStateImmediate();
    }

    public void ExitFPS()
    {
        ApplyFirstPersonStateImmediate();
    }

    private void ApplyFirstPersonStateImmediate()
    {
        isFPS = true;
        ResolveFpsCamera();
        SetBodyVisible(false);
        ApplyCrouchStateImmediate();
    }

    private void SetBodyVisible(bool visible)
    {
        if (bodyRenderers == null || bodyRenderers.Length == 0)
        {
            return;
        }

        foreach (Renderer rend in bodyRenderers)
        {
            if (rend == null)
            {
                continue;
            }

            if (visible)
            {
                rend.enabled = true;
                rend.shadowCastingMode = ShadowCastingMode.On;
            }
            else if (keepShadows)
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

    private Vector3 GetFpsMoveDirection(float horizontal, float vertical)
    {
        Transform viewTransform = ResolveFpsCamera();

        Vector3 lookForward = viewTransform != null
            ? GetPlanarDirection(viewTransform.forward)
            : GetPlanarDirection(transform.forward);

        Vector3 lookRight = viewTransform != null
            ? GetPlanarDirection(viewTransform.right)
            : GetPlanarDirection(transform.right);

        RotateBodyTowards(lookForward);
        return lookForward * vertical + lookRight * horizontal;
    }

    private void RotateBodyTowards(Vector3 lookDirection)
    {
        if (lookDirection.sqrMagnitude <= 0.0001f || rb == null)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        float angle = Quaternion.Angle(rb.rotation, targetRotation);
        if (angle <= turnDeadZone)
        {
            return;
        }

        desiredRotation = Quaternion.Slerp(
            rb.rotation,
            targetRotation,
            bodyTurnSpeed * Time.deltaTime);
        hasDesiredRotation = true;
    }

    private Transform ResolveGameplayCamera()
    {
        return ResolveFpsCamera();
    }

    private Transform ResolveFpsCamera()
    {
        if (fpsCamera != null && fpsCamera != transform)
        {
            if (fpsCamera.GetComponent<Camera>() != null || fpsCamera.IsChildOf(transform))
            {
                return fpsCamera;
            }
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

    private bool ShouldRun(float horizontal, float vertical)
    {
        if (isCrouching || !Input.GetKey(runKey))
        {
            return false;
        }

        return new Vector2(horizontal, vertical).sqrMagnitude > 0.01f;
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

    private void EnsureFootstepAudioSource()
    {
        if (footstepAudioSource != null)
        {
            return;
        }

        footstepAudioSource = gameObject.GetComponent<AudioSource>();
        if (footstepAudioSource == null)
        {
            footstepAudioSource = gameObject.AddComponent<AudioSource>();
        }

        footstepAudioSource.playOnAwake = false;
        footstepAudioSource.loop = false;
        footstepAudioSource.spatialBlend = 0f;
        footstepAudioSource.dopplerLevel = 0f;
        footstepAudioSource.volume = footstepVolume;
    }

    private void LoadFootstepClips()
    {
        if (walkFootstepClips.Length == 0 && !string.IsNullOrWhiteSpace(walkFootstepsResourceFolder))
        {
            walkFootstepClips = Resources.LoadAll<AudioClip>(walkFootstepsResourceFolder.Trim());
        }

        if (runFootstepClips.Length == 0 && !string.IsNullOrWhiteSpace(runFootstepsResourceFolder))
        {
            runFootstepClips = Resources.LoadAll<AudioClip>(runFootstepsResourceFolder.Trim());
        }
    }

    private void UpdateFootsteps(float deltaTime)
    {
        if (!CanPlayFootsteps())
        {
            footstepTimer = 0f;
            return;
        }

        footstepTimer -= deltaTime;
        if (footstepTimer > 0f)
        {
            return;
        }

        PlayFootstep();
        footstepTimer = GetFootstepInterval();
    }

    private bool CanPlayFootsteps()
    {
        return Time.timeScale > 0f
            && footstepAudioSource != null
            && inputDirection.sqrMagnitude > 0.01f
            && (walkFootstepClips.Length > 0 || runFootstepClips.Length > 0);
    }

    private float GetFootstepInterval()
    {
        if (isCrouching)
        {
            return Mathf.Max(0.14f, crouchFootstepInterval);
        }

        if (isRunning)
        {
            return Mathf.Max(0.1f, runFootstepInterval);
        }

        return Mathf.Max(0.14f, walkFootstepInterval);
    }

    private void PlayFootstep()
    {
        if (footstepAudioSource == null)
        {
            return;
        }

        AudioClip clip = GetRandomFootstepClip();
        if (clip == null)
        {
            return;
        }

        footstepAudioSource.volume = footstepVolume;
        footstepAudioSource.pitch = 1f + Random.Range(-footstepPitchVariance, footstepPitchVariance);
        footstepAudioSource.PlayOneShot(clip);
    }

    private AudioClip GetRandomFootstepClip()
    {
        AudioClip[] source = isRunning && runFootstepClips.Length > 0
            ? runFootstepClips
            : walkFootstepClips;

        if (source == null || source.Length == 0)
        {
            return null;
        }

        return source[Random.Range(0, source.Length)];
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
        {
            vector.Normalize();
        }

        return vector;
    }
}
