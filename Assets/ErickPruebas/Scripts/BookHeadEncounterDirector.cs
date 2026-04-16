using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class BookHeadEncounterDirector : MonoBehaviour
{
    private const string RuntimeObjectName = "BookHeadEncounterDirector";
    private const string TargetNoteObjectName = "Cube2";
    private const string TargetEnemyObjectName = "BookHeadMonster";

    private static BookHeadEncounterDirector instance;

    private InteractableNote targetNote;
    private GameObject enemyRoot;
    private Vector3 enemySpawnPosition;
    private Quaternion enemySpawnRotation = Quaternion.identity;
    private bool hasEnemySpawnState;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (instance != null)
        {
            return;
        }

        GameObject existing = GameObject.Find(RuntimeObjectName);
        if (existing != null)
        {
            instance = existing.GetComponent<BookHeadEncounterDirector>();
            return;
        }

        GameObject directorObject = new GameObject(RuntimeObjectName);
        instance = directorObject.AddComponent<BookHeadEncounterDirector>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;

        InteractableNote.NoteCollected -= HandleNoteCollected;
        InteractableNote.NoteCollected += HandleNoteCollected;

        RefreshSceneBindings(SceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            InteractableNote.NoteCollected -= HandleNoteCollected;
            instance = null;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshSceneBindings(scene);
    }

    private void RefreshSceneBindings(Scene scene)
    {
        targetNote = null;
        enemyRoot = null;
        hasEnemySpawnState = false;

        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        ResolveSceneTargets(scene);
        CacheEnemySpawnState();
        SyncEncounterState();
    }

    private void ResolveSceneTargets(Scene scene)
    {
        InteractableNote[] notes = FindObjectsOfType<InteractableNote>(true);
        for (int i = 0; i < notes.Length; i++)
        {
            if (notes[i] == null || notes[i].gameObject.scene != scene)
            {
                continue;
            }

            if (string.Equals(notes[i].gameObject.name, TargetNoteObjectName, StringComparison.OrdinalIgnoreCase))
            {
                targetNote = notes[i];
                break;
            }
        }

        EnemyChase[] enemies = FindObjectsOfType<EnemyChase>(true);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null || enemies[i].gameObject.scene != scene)
            {
                continue;
            }

            if (string.Equals(enemies[i].gameObject.name, TargetEnemyObjectName, StringComparison.OrdinalIgnoreCase))
            {
                enemyRoot = enemies[i].gameObject;
                break;
            }
        }
    }

    private void CacheEnemySpawnState()
    {
        if (enemyRoot == null)
        {
            return;
        }

        enemySpawnPosition = enemyRoot.transform.position;
        enemySpawnRotation = enemyRoot.transform.rotation;
        hasEnemySpawnState = true;
    }

    private void SyncEncounterState()
    {
        if (enemyRoot == null)
        {
            return;
        }

        bool noteAlreadyCollected = InventoryManager.HasInstance
            && targetNote != null
            && InventoryManager.Instance.IsNoteCollected(targetNote.NoteId);

        if (noteAlreadyCollected)
        {
            ActivateEnemy();
            return;
        }

        DeactivateEnemy();
    }

    private void HandleNoteCollected(InteractableNote collectedNote)
    {
        if (collectedNote == null || targetNote == null || enemyRoot == null)
        {
            return;
        }

        if (collectedNote == targetNote ||
            string.Equals(collectedNote.gameObject.name, TargetNoteObjectName, StringComparison.OrdinalIgnoreCase))
        {
            ActivateEnemy();
        }
    }

    private void ActivateEnemy()
    {
        if (enemyRoot == null)
        {
            return;
        }

        ResetEnemyToSpawnState();
        enemyRoot.SetActive(true);

        NavMeshAgent agent = enemyRoot.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            WarpAgentToSpawn(agent);
        }
    }

    private void DeactivateEnemy()
    {
        if (enemyRoot == null)
        {
            return;
        }

        NavMeshAgent agent = enemyRoot.GetComponent<NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }

        ResetEnemyToSpawnState();
        enemyRoot.SetActive(false);
    }

    private void ResetEnemyToSpawnState()
    {
        if (enemyRoot == null || !hasEnemySpawnState)
        {
            return;
        }

        enemyRoot.transform.SetPositionAndRotation(enemySpawnPosition, enemySpawnRotation);
    }

    private void WarpAgentToSpawn(NavMeshAgent agent)
    {
        if (agent == null)
        {
            return;
        }

        Vector3 spawnPosition = hasEnemySpawnState ? enemySpawnPosition : agent.transform.position;
        if (NavMesh.SamplePosition(spawnPosition, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            return;
        }

        agent.Warp(spawnPosition);
    }
}
