using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyChase : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform player;
    [SerializeField] private float sightOriginHeight = 1.6f;
    [SerializeField] private float playerSightHeight = 1.1f;

    [Header("Deteccion")]
    [SerializeField] private float baseDetectionRange = 10f;
    [SerializeField] private float runDetectionMultiplier = 1.5f;
    [SerializeField] private float crouchDetectionMultiplier = 0.5f;
    [SerializeField, Range(0f, 180f)] private float fieldOfView = 90f;

    [Header("Persecucion")]
    [SerializeField] private float loseDistance = 20f;
    [SerializeField] private float loseSightDelay = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float catchUpSpeed = 7.5f;
    [SerializeField] private float catchUpDistance = 16f;
    [SerializeField] private float catchUpReleaseDistance = 9f;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseRefreshInterval = 0.15f;

    [Header("Recuperacion")]
    [SerializeField] private float stuckDetectionDelay = 0.85f;
    [SerializeField] private float stuckMovementThreshold = 0.12f;
    [SerializeField] private float stuckRemainingDistanceThreshold = 0.75f;
    [SerializeField] private float recoveryWarpRadius = 1.25f;
    [SerializeField] private float recoveryDestinationSampleRadius = 3.5f;

    [Header("Animacion")]
    [SerializeField] private string animatorSpeedParameter = "Speed";
    [SerializeField] private float walkAnimationSpeed = 0.4f;
    [SerializeField] private float runAnimationSpeed = 4f;
    [SerializeField] private float animationDampTime = 0.1f;
    [SerializeField] private float movementAnimationThreshold = 0.05f;

    [Header("Audio")]
    [SerializeField] private AudioClip chaseAudioClip;
    [SerializeField] private AudioClip[] patrolAudioClips = new AudioClip[2];
    [SerializeField, Range(0f, 1f)] private float audioVolume = 0.5f;
    [SerializeField] private float audioMinDistance = 2f;
    [SerializeField] private float audioMaxDistance = 22f;
    [SerializeField] private AudioRolloffMode audioRolloffMode = AudioRolloffMode.Logarithmic;

    private PlayerMovemnt playerScript;
    private NavMeshAgent agent;
    private Animator animator;
    private AudioSource enemyAudioSource;
    private float timeWithoutSight;
    private float chaseRefreshTimer;
    private bool isChasing;
    private int animatorSpeedParameterHash;
    private bool hasAnimatorSpeedParameter;
    private int nextPatrolClipIndex;
    private EnemyAudioMode currentAudioMode;
    private Vector3 lastNavigationSamplePosition;
    private float stuckTimer;
    private Vector3 currentNavigationTarget;
    private bool hasNavigationTarget;
    private bool catchUpSpeedActive;

    public bool IsChasing => isChasing;
    public float PatrolSpeed => patrolSpeed;

    private enum EnemyAudioMode
    {
        None,
        Patrol,
        Chase
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        enemyAudioSource = GetComponent<AudioSource>();
        if (enemyAudioSource == null)
        {
            enemyAudioSource = gameObject.AddComponent<AudioSource>();
        }

        CacheAnimatorParameter();
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        ConfigureAudioSource();
        ResolvePlayerReference();
        ResetNavigationRecovery();
    }

    private void OnEnable()
    {
        ResetNavigationRecovery();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
        {
            return;
        }

        if (ResolvePlayerReference())
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            float detectionRange = GetDetectionRange();
            bool canSeePlayer = CanSeePlayer(distanceToPlayer, detectionRange);

            if (canSeePlayer)
            {
                StartChase();
                timeWithoutSight = 0f;
            }
            else if (isChasing)
            {
                timeWithoutSight += Time.deltaTime;
            }

            if (isChasing)
            {
                UpdateChaseSpeed(distanceToPlayer);
                chaseRefreshTimer += Time.deltaTime;
                if (chaseRefreshTimer >= chaseRefreshInterval || !agent.hasPath)
                {
                    chaseRefreshTimer = 0f;
                    SetDestination(player.position);
                }

                if (distanceToPlayer > GetEffectiveLoseDistance() || timeWithoutSight >= loseSightDelay)
                {
                    StopChase();
                }
            }
        }

        UpdateNavigationRecovery();
        UpdateAnimationState();
        UpdateAudioState();
    }

    private bool ResolvePlayerReference()
    {
        if (player == null)
        {
            PlayerMovemnt movement = FindObjectOfType<PlayerMovemnt>();
            if (movement != null)
            {
                player = movement.transform;
            }
        }

        if (player == null)
        {
            try
            {
                GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
                if (taggedPlayer != null)
                {
                    player = taggedPlayer.transform;
                }
            }
            catch (UnityException)
            {
                player = null;
            }
        }

        if (player == null)
        {
            playerScript = null;
            return false;
        }

        if (playerScript == null || playerScript.transform != player)
        {
            playerScript = player.GetComponent<PlayerMovemnt>();
        }

        return playerScript != null;
    }

    private void CacheAnimatorParameter()
    {
        hasAnimatorSpeedParameter = false;
        animatorSpeedParameterHash = 0;

        if (animator == null || string.IsNullOrWhiteSpace(animatorSpeedParameter))
        {
            return;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == AnimatorControllerParameterType.Float
                && parameters[i].name == animatorSpeedParameter)
            {
                animatorSpeedParameterHash = Animator.StringToHash(animatorSpeedParameter);
                hasAnimatorSpeedParameter = true;
                return;
            }
        }
    }

    private void UpdateAnimationState()
    {
        if (animator == null || !hasAnimatorSpeedParameter)
        {
            return;
        }

        float targetAnimationSpeed = 0f;

        if (isChasing)
        {
            targetAnimationSpeed = runAnimationSpeed;
        }
        else if (ShouldPlayWalkAnimation())
        {
            targetAnimationSpeed = walkAnimationSpeed;
        }

        animator.SetFloat(animatorSpeedParameterHash, targetAnimationSpeed, animationDampTime, Time.deltaTime);
    }

    private bool ShouldPlayWalkAnimation()
    {
        if (agent == null)
        {
            return false;
        }

        float thresholdSqr = movementAnimationThreshold * movementAnimationThreshold;
        if (agent.velocity.sqrMagnitude > thresholdSqr || agent.desiredVelocity.sqrMagnitude > thresholdSqr)
        {
            return true;
        }

        return agent.hasPath && agent.remainingDistance > agent.stoppingDistance + 0.05f;
    }

    private float GetDetectionRange()
    {
        float detectionRange = baseDetectionRange;

        if (playerScript.IsRunning)
        {
            detectionRange *= runDetectionMultiplier;
        }
        else if (playerScript.IsCrouching)
        {
            detectionRange *= crouchDetectionMultiplier;
        }

        return detectionRange;
    }

    private bool CanSeePlayer(float distanceToPlayer, float detectionRange)
    {
        if (player == null || distanceToPlayer > detectionRange)
        {
            return false;
        }

        Vector3 origin = transform.position + Vector3.up * sightOriginHeight;
        Vector3 target = player.position + Vector3.up * playerSightHeight;
        Vector3 direction = target - origin;

        if (direction.sqrMagnitude <= 0.001f)
        {
            return true;
        }

        float angle = Vector3.Angle(transform.forward, direction);
        if (angle > fieldOfView)
        {
            return false;
        }

        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, direction.magnitude))
        {
            return hit.transform == player || hit.transform.IsChildOf(player);
        }

        return false;
    }

    private void SetDestination(Vector3 targetPosition)
    {
        if (agent == null || !agent.isOnNavMesh)
        {
            return;
        }

        currentNavigationTarget = targetPosition;
        hasNavigationTarget = true;

        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 4f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            return;
        }

        agent.SetDestination(targetPosition);
    }

    private void StartChase()
    {
        if (isChasing)
        {
            return;
        }

        isChasing = true;
        catchUpSpeedActive = false;
        agent.speed = chaseSpeed;
        timeWithoutSight = 0f;
        chaseRefreshTimer = chaseRefreshInterval;

        if (GameplayRunState.TryConsumeRunHint())
        {
            TutorialHintOverlay.ShowHint("Presiona SHIFT para correr.", 3f);
        }

        Debug.Log("Persiguiendo");
    }

    private void StopChase()
    {
        isChasing = false;
        catchUpSpeedActive = false;
        agent.speed = patrolSpeed;
        timeWithoutSight = 0f;
        chaseRefreshTimer = 0f;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }

        hasNavigationTarget = false;
        ResetNavigationRecovery();

        Debug.Log("Patrullando");
    }

    private void UpdateNavigationRecovery()
    {
        if (agent == null || !agent.isOnNavMesh)
        {
            ResetNavigationRecovery();
            return;
        }

        if (!agent.hasPath || agent.pathPending)
        {
            ResetNavigationRecovery();
            return;
        }

        if (agent.remainingDistance <= agent.stoppingDistance + stuckRemainingDistanceThreshold)
        {
            ResetNavigationRecovery();
            return;
        }

        bool wantsToMove = agent.desiredVelocity.sqrMagnitude > 0.01f;
        float movedDistance = Vector3.Distance(transform.position, lastNavigationSamplePosition);
        if (!wantsToMove || movedDistance >= stuckMovementThreshold)
        {
            lastNavigationSamplePosition = transform.position;
            stuckTimer = 0f;
            return;
        }

        stuckTimer += Time.deltaTime;
        if (stuckTimer < stuckDetectionDelay)
        {
            return;
        }

        AttemptNavigationRecovery();
        lastNavigationSamplePosition = transform.position;
        stuckTimer = 0f;
    }

    private void UpdateChaseSpeed(float distanceToPlayer)
    {
        if (!isChasing || agent == null)
        {
            return;
        }

        float effectiveCatchUpDistance = GetEffectiveCatchUpDistance();
        float effectiveReleaseDistance = Mathf.Min(catchUpReleaseDistance, Mathf.Max(2f, effectiveCatchUpDistance - 3f));

        if (!catchUpSpeedActive && distanceToPlayer >= effectiveCatchUpDistance)
        {
            catchUpSpeedActive = true;
        }
        else if (catchUpSpeedActive && distanceToPlayer <= effectiveReleaseDistance)
        {
            catchUpSpeedActive = false;
        }

        agent.speed = catchUpSpeedActive ? Mathf.Max(chaseSpeed, catchUpSpeed) : chaseSpeed;
    }

    private float GetEffectiveCatchUpDistance()
    {
        return Mathf.Min(Mathf.Max(8f, catchUpDistance), Mathf.Max(8f, loseDistance - 4f));
    }

    private float GetEffectiveLoseDistance()
    {
        return Mathf.Max(loseDistance, GetEffectiveCatchUpDistance() + 8f);
    }

    private void AttemptNavigationRecovery()
    {
        if (agent == null || !agent.isOnNavMesh)
        {
            return;
        }

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit currentHit, recoveryWarpRadius, agent.areaMask))
        {
            agent.Warp(currentHit.position);
        }

        Vector3 desiredTarget = hasNavigationTarget ? currentNavigationTarget : agent.destination;
        agent.ResetPath();

        if (TryResolveReachableDestination(desiredTarget, out Vector3 resolvedDestination))
        {
            agent.SetDestination(resolvedDestination);
            return;
        }

        agent.SetDestination(desiredTarget);
    }

    private bool TryResolveReachableDestination(Vector3 desiredTarget, out Vector3 resolvedDestination)
    {
        if (TrySampleReachablePosition(desiredTarget, recoveryDestinationSampleRadius, out resolvedDestination))
        {
            return true;
        }

        float ringRadius = Mathf.Max(1f, recoveryDestinationSampleRadius * 0.55f);
        for (int i = 0; i < 8; i++)
        {
            float angle = (Mathf.PI * 2f * i) / 8f;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ringRadius;
            if (TrySampleReachablePosition(desiredTarget + offset, recoveryDestinationSampleRadius * 0.5f, out resolvedDestination))
            {
                return true;
            }
        }

        resolvedDestination = Vector3.zero;
        return false;
    }

    private bool TrySampleReachablePosition(Vector3 sampleCenter, float sampleRadius, out Vector3 resolvedDestination)
    {
        if (agent != null &&
            NavMesh.SamplePosition(sampleCenter, out NavMeshHit hit, sampleRadius, agent.areaMask) &&
            CanReach(hit.position))
        {
            resolvedDestination = hit.position;
            return true;
        }

        resolvedDestination = Vector3.zero;
        return false;
    }

    private bool CanReach(Vector3 destination)
    {
        if (agent == null || !agent.isOnNavMesh)
        {
            return false;
        }

        NavMeshPath path = new NavMeshPath();
        if (!agent.CalculatePath(destination, path))
        {
            return false;
        }

        return path.status == NavMeshPathStatus.PathComplete;
    }

    private void ResetNavigationRecovery()
    {
        lastNavigationSamplePosition = transform.position;
        stuckTimer = 0f;
    }

    private void ConfigureAudioSource()
    {
        if (enemyAudioSource == null)
        {
            return;
        }

        enemyAudioSource.playOnAwake = false;
        enemyAudioSource.loop = false;
        enemyAudioSource.spatialBlend = 1f;
        enemyAudioSource.spread = 360f;
        enemyAudioSource.rolloffMode = audioRolloffMode;
        enemyAudioSource.minDistance = Mathf.Max(0.1f, audioMinDistance);
        enemyAudioSource.maxDistance = Mathf.Max(enemyAudioSource.minDistance + 0.1f, audioMaxDistance);
        enemyAudioSource.volume = Mathf.Clamp01(audioVolume);
        enemyAudioSource.dopplerLevel = 0f;
    }

    private void UpdateAudioState()
    {
        if (enemyAudioSource == null)
        {
            return;
        }

        ConfigureAudioSource();

        EnemyAudioMode desiredMode = DetermineAudioMode();
        if (desiredMode != currentAudioMode)
        {
            SwitchAudioMode(desiredMode);
        }

        if (currentAudioMode == EnemyAudioMode.Patrol && !enemyAudioSource.isPlaying)
        {
            PlayNextPatrolClip();
        }
        else if (currentAudioMode == EnemyAudioMode.Chase && !enemyAudioSource.isPlaying && chaseAudioClip != null)
        {
            PlayClip(chaseAudioClip, true);
        }
    }

    private EnemyAudioMode DetermineAudioMode()
    {
        if (isChasing && chaseAudioClip != null)
        {
            return EnemyAudioMode.Chase;
        }

        return HasPatrolAudio() ? EnemyAudioMode.Patrol : EnemyAudioMode.None;
    }

    private void SwitchAudioMode(EnemyAudioMode newMode)
    {
        currentAudioMode = newMode;

        if (newMode == EnemyAudioMode.None)
        {
            enemyAudioSource.Stop();
            enemyAudioSource.clip = null;
            return;
        }

        if (newMode == EnemyAudioMode.Chase)
        {
            PlayClip(chaseAudioClip, true);
            return;
        }

        PlayNextPatrolClip();
    }

    private void PlayNextPatrolClip()
    {
        if (!TryGetNextPatrolClip(out AudioClip clip))
        {
            enemyAudioSource.Stop();
            enemyAudioSource.clip = null;
            return;
        }

        PlayClip(clip, false);
    }

    private bool TryGetNextPatrolClip(out AudioClip clip)
    {
        clip = null;
        if (patrolAudioClips == null || patrolAudioClips.Length == 0)
        {
            return false;
        }

        for (int attempt = 0; attempt < patrolAudioClips.Length; attempt++)
        {
            int index = (nextPatrolClipIndex + attempt) % patrolAudioClips.Length;
            AudioClip candidate = patrolAudioClips[index];
            if (candidate == null)
            {
                continue;
            }

            nextPatrolClipIndex = (index + 1) % patrolAudioClips.Length;
            clip = candidate;
            return true;
        }

        return false;
    }

    private bool HasPatrolAudio()
    {
        if (patrolAudioClips == null)
        {
            return false;
        }

        for (int i = 0; i < patrolAudioClips.Length; i++)
        {
            if (patrolAudioClips[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private void PlayClip(AudioClip clip, bool loop)
    {
        if (enemyAudioSource == null || clip == null)
        {
            return;
        }

        if (enemyAudioSource.isPlaying)
        {
            enemyAudioSource.Stop();
        }

        enemyAudioSource.clip = clip;
        enemyAudioSource.loop = loop;
        enemyAudioSource.Play();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (GameOverManager.instance != null)
        {
            GameOverManager.instance.TriggerGameOver();
        }
    }

    private void OnDisable()
    {
        if (enemyAudioSource != null)
        {
            enemyAudioSource.Stop();
        }

        ResetNavigationRecovery();
    }
}
