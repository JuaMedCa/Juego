using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyPatrol : MonoBehaviour
{
    [Header("Patrulla Aleatoria")]
    public float walkRadius = 15f;
    public float waitTime = 2f;

    [Header("Acecho")]
    [SerializeField] private bool followPlayerDuringPatrol = true;
    [SerializeField] private Transform player;
    [SerializeField] private float playerRefreshInterval = 0.75f;
    [SerializeField] private float playerSampleRadius = 2f;

    private NavMeshAgent agent;
    private EnemyChase enemyChase;
    private float timer;
    private float playerRefreshTimer;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        enemyChase = GetComponent<EnemyChase>();
        ResolvePlayerReference();
    }

    private void Start()
    {
        SetNewDestination();
    }

    private void Update()
    {
        if (agent == null || !agent.isOnNavMesh)
        {
            return;
        }

        if (enemyChase != null)
        {
            if (enemyChase.IsChasing)
            {
                timer = 0f;
                playerRefreshTimer = 0f;
                return;
            }
        }

        if (followPlayerDuringPatrol && ResolvePlayerReference())
        {
            playerRefreshTimer += Time.deltaTime;

            if (playerRefreshTimer >= playerRefreshInterval || !agent.hasPath || agent.remainingDistance <= agent.stoppingDistance + 0.5f)
            {
                SetPlayerDestination();
                playerRefreshTimer = 0f;
            }

            return;
        }

        if (agent.pathPending)
        {
            return;
        }

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            timer += Time.deltaTime;

            if (timer >= waitTime)
            {
                SetRandomDestination();
                timer = 0f;
            }
        }
    }

    private bool ResolvePlayerReference()
    {
        if (player != null)
        {
            return true;
        }

        PlayerMovemnt movement = FindObjectOfType<PlayerMovemnt>();
        if (movement != null)
        {
            player = movement.transform;
            return true;
        }

        try
        {
            GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            if (taggedPlayer != null)
            {
                player = taggedPlayer.transform;
                return true;
            }
        }
        catch (UnityException)
        {
            player = null;
        }

        return false;
    }

    private void SetNewDestination()
    {
        if (followPlayerDuringPatrol && ResolvePlayerReference())
        {
            SetPlayerDestination();
            return;
        }

        SetRandomDestination();
    }

    private void SetPlayerDestination()
    {
        if (player == null)
        {
            return;
        }

        Vector3 desiredPosition = player.position;
        Vector2 randomOffset = Random.insideUnitCircle * playerSampleRadius;
        desiredPosition += new Vector3(randomOffset.x, 0f, randomOffset.y);

        float sampleDistance = Mathf.Max(playerSampleRadius + 1f, walkRadius);
        if (NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, sampleDistance, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            return;
        }

        agent.SetDestination(player.position);
    }

    private void SetRandomDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * walkRadius;
        randomDirection += transform.position;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, walkRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
}
