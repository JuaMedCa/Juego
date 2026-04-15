using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class JeepEscapeInteraction : MonoBehaviour
{
    private const string TargetObjectName = "Military combat Jeep";

    [Header("Interaccion")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactionDistance = 4f;
    [SerializeField] private float collisionPadding = 0.15f;
    [SerializeField] private float interactionPadding = 1.1f;
    [SerializeField] private string readyPrompt = "Presiona E para escapar";
    [SerializeField] private string inspectPrompt = "Presiona E para revisar el jeep";
    [SerializeField] private string missingFuelMessage = "Falta gasolina en el tanque";
    [SerializeField] private float missingFuelMessageDuration = 1.5f;

    private Transform playerTransform;
    private GameObject interactTextObject;
    private TMP_Text interactTextLabel;
    private Bounds interactionBounds;
    private bool hasInteractionBounds;
    private bool ownsInteractPrompt;
    private bool playerInside;
    private BoxCollider solidCollider;
    private BoxCollider interactionTrigger;
    private string temporaryPromptMessage;
    private float temporaryPromptUntil;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallInLoadedScenes()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            InstallOnMatchingChildren(roots[i].transform);
        }
    }

    private static void InstallOnMatchingChildren(Transform current)
    {
        if (current.name.IndexOf(TargetObjectName, StringComparison.OrdinalIgnoreCase) >= 0
            && current.GetComponent<JeepEscapeInteraction>() == null)
        {
            current.gameObject.AddComponent<JeepEscapeInteraction>();
        }

        for (int i = 0; i < current.childCount; i++)
        {
            InstallOnMatchingChildren(current.GetChild(i));
        }
    }

    private void Awake()
    {
        RefreshInteractionBounds();
        EnsureSolidCollider();
        EnsureInteractionTrigger();
        ResolvePlayerTransform();
        ResolveInteractText();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || EscapeEndingManager.IsEndingActive)
        {
            ReleaseInteractPrompt();
            return;
        }

        if (playerTransform == null)
        {
            ResolvePlayerTransform();
        }

        if (playerTransform == null)
        {
            ReleaseInteractPrompt();
            return;
        }

        if (!playerInside && !IsPlayerInRange())
        {
            ReleaseInteractPrompt();
            return;
        }

        bool hasRequiredFuel = ObjectiveSystem.EnsureInstance().HasRequiredFuel;
        string activePrompt = IsTemporaryPromptActive()
            ? temporaryPromptMessage
            : (hasRequiredFuel ? readyPrompt : inspectPrompt);

        ShowInteractPrompt(activePrompt);

        if (Input.GetKeyDown(interactKey))
        {
            TryUseJeep(hasRequiredFuel);
        }
    }

    private void TryUseJeep(bool hasRequiredFuel)
    {
        if (hasRequiredFuel)
        {
            EscapeEndingManager.EnsureInstance().TriggerEscape();
            return;
        }

        if (MessageSystem.instance != null)
        {
            MessageSystem.instance.ShowMessage(missingFuelMessage, missingFuelMessageDuration);
        }

        temporaryPromptMessage = missingFuelMessage;
        temporaryPromptUntil = Time.unscaledTime + Mathf.Max(0.5f, missingFuelMessageDuration);
        ShowInteractPrompt(missingFuelMessage);
    }

    private bool IsPlayerInRange()
    {
        Vector3 playerPosition = playerTransform.position;

        if (hasInteractionBounds)
        {
            float sqrDistance = interactionBounds.SqrDistance(playerPosition);
            return sqrDistance <= interactionDistance * interactionDistance;
        }

        return Vector3.SqrMagnitude(transform.position - playerPosition) <= interactionDistance * interactionDistance;
    }

    private void ResolvePlayerTransform()
    {
        PlayerMovemnt movement = FindObjectOfType<PlayerMovemnt>();
        if (movement != null)
        {
            playerTransform = movement.transform;
            return;
        }

        try
        {
            GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            if (taggedPlayer != null)
            {
                playerTransform = taggedPlayer.transform;
            }
        }
        catch (UnityException)
        {
            playerTransform = null;
        }
    }

    private void ResolveInteractText()
    {
        if (interactTextObject != null && interactTextLabel != null)
        {
            return;
        }

        TMP_Text[] textObjects = FindObjectsOfType<TMP_Text>(true);
        for (int i = 0; i < textObjects.Length; i++)
        {
            if (textObjects[i].name != "InteractText")
            {
                continue;
            }

            interactTextObject = textObjects[i].gameObject;
            interactTextLabel = textObjects[i];
            return;
        }
    }

    private void RefreshInteractionBounds()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].isTrigger)
            {
                continue;
            }

            EncapsulateBounds(colliders[i].bounds);
        }

        if (hasInteractionBounds)
        {
            return;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            EncapsulateBounds(renderers[i].bounds);
        }
    }

    private void EnsureInteractionTrigger()
    {
        if (interactionTrigger == null)
        {
            Collider[] colliders = GetComponents<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] is BoxCollider boxCollider && boxCollider.isTrigger)
                {
                    interactionTrigger = boxCollider;
                    break;
                }
            }

            if (interactionTrigger == null)
            {
                interactionTrigger = gameObject.AddComponent<BoxCollider>();
            }
        }

        interactionTrigger.isTrigger = true;
        ApplyBoundsToCollider(interactionTrigger, interactionPadding);
    }

    private void EnsureSolidCollider()
    {
        if (solidCollider == null)
        {
            Collider[] colliders = GetComponents<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] is BoxCollider boxCollider && !boxCollider.isTrigger)
                {
                    solidCollider = boxCollider;
                    break;
                }
            }

            if (solidCollider == null)
            {
                solidCollider = gameObject.AddComponent<BoxCollider>();
            }
        }

        solidCollider.isTrigger = false;
        ApplyBoundsToCollider(solidCollider, collisionPadding);
    }

    private void EncapsulateBounds(Bounds bounds)
    {
        if (!hasInteractionBounds)
        {
            interactionBounds = bounds;
            hasInteractionBounds = true;
            return;
        }

        interactionBounds.Encapsulate(bounds);
    }

    private void ApplyBoundsToCollider(BoxCollider boxCollider, float padding)
    {
        if (boxCollider == null)
        {
            return;
        }

        if (!hasInteractionBounds)
        {
            boxCollider.center = Vector3.zero;
            boxCollider.size = Vector3.one * Mathf.Max(1.5f, interactionDistance);
            return;
        }

        Bounds localBounds = ConvertWorldBoundsToLocal(interactionBounds);
        boxCollider.center = localBounds.center;

        Vector3 expandedSize = localBounds.size + Vector3.one * Mathf.Max(0f, padding);
        expandedSize.x = Mathf.Max(expandedSize.x, 1.5f);
        expandedSize.y = Mathf.Max(expandedSize.y, 1.5f);
        expandedSize.z = Mathf.Max(expandedSize.z, 1.5f);
        boxCollider.size = expandedSize;
    }

    private Bounds ConvertWorldBoundsToLocal(Bounds worldBounds)
    {
        Vector3 center = transform.InverseTransformPoint(worldBounds.center);
        Vector3 extents = worldBounds.extents;

        Vector3[] corners =
        {
            worldBounds.center + new Vector3( extents.x,  extents.y,  extents.z),
            worldBounds.center + new Vector3( extents.x,  extents.y, -extents.z),
            worldBounds.center + new Vector3( extents.x, -extents.y,  extents.z),
            worldBounds.center + new Vector3( extents.x, -extents.y, -extents.z),
            worldBounds.center + new Vector3(-extents.x,  extents.y,  extents.z),
            worldBounds.center + new Vector3(-extents.x,  extents.y, -extents.z),
            worldBounds.center + new Vector3(-extents.x, -extents.y,  extents.z),
            worldBounds.center + new Vector3(-extents.x, -extents.y, -extents.z)
        };

        Vector3 min = transform.InverseTransformPoint(corners[0]);
        Vector3 max = min;

        for (int i = 1; i < corners.Length; i++)
        {
            Vector3 localCorner = transform.InverseTransformPoint(corners[i]);
            min = Vector3.Min(min, localCorner);
            max = Vector3.Max(max, localCorner);
        }

        Bounds localBounds = new Bounds(center, Vector3.zero);
        localBounds.SetMinMax(min, max);
        return localBounds;
    }

    private void ShowInteractPrompt(string message)
    {
        ResolveInteractText();
        if (interactTextObject == null)
        {
            return;
        }

        interactTextObject.SetActive(true);

        if (interactTextLabel != null)
        {
            interactTextLabel.text = string.IsNullOrWhiteSpace(message) ? inspectPrompt : message;
        }

        ownsInteractPrompt = true;
    }

    private void ReleaseInteractPrompt()
    {
        if (!ownsInteractPrompt)
        {
            return;
        }

        ResolveInteractText();
        if (interactTextObject != null)
        {
            interactTextObject.SetActive(false);
        }

        ownsInteractPrompt = false;
    }

    private bool IsTemporaryPromptActive()
    {
        if (string.IsNullOrWhiteSpace(temporaryPromptMessage))
        {
            return false;
        }

        if (Time.unscaledTime <= temporaryPromptUntil)
        {
            return true;
        }

        temporaryPromptMessage = null;
        temporaryPromptUntil = 0f;
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        playerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        playerInside = false;
        ReleaseInteractPrompt();
    }

    private void OnDisable()
    {
        playerInside = false;
        ReleaseInteractPrompt();
    }
}
