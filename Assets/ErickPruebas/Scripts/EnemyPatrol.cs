using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyPatrol : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] private float walkRadius = 10f;
    [SerializeField] private float waitTime = 2f;
    [SerializeField] private int destinationAttempts = 8;

    [Header("NavMesh")]
    [SerializeField] private bool autoCreateAgentIfMissing = true;
    [SerializeField] private float navMeshSnapDistance = 10f;

    [Header("Default Agent Setup")]
    [SerializeField] private float agentSpeed = 2.5f;
    [SerializeField] private float agentAngularSpeed = 180f;
    [SerializeField] private float agentAcceleration = 8f;
    [SerializeField] private float agentStoppingDistance = 0.15f;
    [SerializeField] private float agentRadius = 0.35f;
    [SerializeField] private float agentHeight = 2f;

    private NavMeshAgent agent;
    private float timer;
    private bool agentWasAutoCreated;

    private void Awake()
    {
        ResolveAgent();
    }

    private void Start()
    {
        if (!EnsureAgentReady())
        {
            enabled = false;
            return;
        }

        SetNewDestination();
    }

    private void Update()
    {
        if (agent == null || !agent.isOnNavMesh)
        {
            return;
        }

        if (agent.pathPending)
        {
            return;
        }

        bool reachedDestination = agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, 0.05f);
        bool finishedMoving = !agent.hasPath || agent.velocity.sqrMagnitude <= 0.01f;

        if (reachedDestination && finishedMoving)
        {
            timer += Time.deltaTime;

            if (timer >= waitTime)
            {
                SetNewDestination();
                timer = 0f;
            }
        }
    }

    private bool EnsureAgentReady()
    {
        if (!ResolveAgent())
        {
            Debug.LogError($"EnemyPatrol: no se encontro ni se pudo crear un NavMeshAgent para '{name}'.", this);
            return false;
        }

        if (agentWasAutoCreated)
        {
            ConfigureAutoCreatedAgent();
        }

        if (agent.isOnNavMesh)
        {
            return true;
        }

        if (NavMesh.SamplePosition(agent.transform.position, out NavMeshHit hit, navMeshSnapDistance, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            return true;
        }

        Debug.LogWarning(
            $"EnemyPatrol: '{name}' tiene NavMeshAgent, pero no esta sobre un NavMesh cercano. Revisa el bake del NavMesh o la posicion inicial del enemigo.",
            this);
        return false;
    }

    private bool ResolveAgent()
    {
        if (agent != null)
        {
            return true;
        }

        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            return true;
        }

        agent = GetComponentInChildren<NavMeshAgent>();
        if (agent != null)
        {
            return true;
        }

        agent = GetComponentInParent<NavMeshAgent>();
        if (agent != null)
        {
            return true;
        }

        if (!autoCreateAgentIfMissing)
        {
            return false;
        }

        agent = gameObject.AddComponent<NavMeshAgent>();
        agentWasAutoCreated = agent != null;
        return agent != null;
    }

    private void ConfigureAutoCreatedAgent()
    {
        agent.speed = agentSpeed;
        agent.angularSpeed = agentAngularSpeed;
        agent.acceleration = agentAcceleration;
        agent.stoppingDistance = agentStoppingDistance;
        agent.radius = agentRadius;
        agent.height = agentHeight;
    }

    private void SetNewDestination()
    {
        if (agent == null || !agent.isOnNavMesh)
        {
            return;
        }

        Vector3 origin = agent.transform.position;

        for (int attempt = 0; attempt < Mathf.Max(1, destinationAttempts); attempt++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * walkRadius;
            Vector3 candidate = origin + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, walkRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }

        timer = 0f;
    }
}
