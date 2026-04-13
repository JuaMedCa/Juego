using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyChase : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    private PlayerMovemnt playerScript;
    private NavMeshAgent agent;

    [Header("Detección")]
    public float baseDetectionRange = 10f;
    public float runDetectionMultiplier = 1.5f;
    public float crouchDetectionMultiplier = 0.5f;
    public float fieldOfView = 90f;

    [Header("Persecución")]
    public float loseDistance = 20f;
    public float chaseSpeed = 4f;
    public float patrolSpeed = 2f;

    private bool isChasing = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (player != null)
            playerScript = player.GetComponent<PlayerMovemnt>();
    }

    void Update()
    {
        if (player == null || playerScript == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        float detectionRange = baseDetectionRange;

        if (playerScript.IsRunning)
            detectionRange *= runDetectionMultiplier;
        else if (playerScript.IsCrouching)
            detectionRange *= crouchDetectionMultiplier;

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dirToPlayer);

        bool canSeePlayer = false;

        if (Physics.Raycast(transform.position + Vector3.up, dirToPlayer, out RaycastHit hit, detectionRange))
        {
            if (hit.transform == player)
                canSeePlayer = true;
        }

        if (distance <= detectionRange && angle <= fieldOfView && canSeePlayer)
        {
            StartChase();
        }

        if (isChasing)
        {
            agent.SetDestination(player.position);

            if (distance > loseDistance)
            {
                StopChase();
            }
        }
    }

    void StartChase()
    {
        if (isChasing) return;

        isChasing = true;
        agent.speed = chaseSpeed;

        Debug.Log("Persiguiendo");
    }

    void StopChase()
    {
        isChasing = false;
        agent.speed = patrolSpeed;

        Debug.Log("Patrullando");
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameOverManager.instance.TriggerGameOver();
        }
    }
}