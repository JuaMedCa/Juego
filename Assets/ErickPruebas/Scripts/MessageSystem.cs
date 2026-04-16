using System.Collections;
using TMPro;
using UnityEngine;

public class MessageSystem : MonoBehaviour
{
    public static MessageSystem instance;

    public TMP_Text messageText;
    [SerializeField] private float defaultTypewriterCharacterDelay = 0.03f;

    private Coroutine currentMessage;

    void Awake()
    {
        instance = this;
        ResolveMessageText();
    }

    private void OnEnable()
    {
        ResolveMessageText();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public void ShowMessage(string message, float duration)
    {
        if (currentMessage != null)
        {
            StopCoroutine(currentMessage);
        }

        currentMessage = StartCoroutine(Show(message, duration));
    }

    public void ShowTypewriterMessage(string message, float duration, float characterDelay = -1f)
    {
        if (currentMessage != null)
        {
            StopCoroutine(currentMessage);
        }

        float resolvedDelay = characterDelay > 0f ? characterDelay : defaultTypewriterCharacterDelay;
        currentMessage = StartCoroutine(ShowTypewriter(message, duration, resolvedDelay));
    }

    private IEnumerator Show(string msg, float time)
    {
        if (!TryGetMessageText(out TMP_Text target))
        {
            yield break;
        }

        target.text = msg ?? string.Empty;
        target.maxVisibleCharacters = int.MaxValue;
        target.gameObject.SetActive(true);

        yield return new WaitForSeconds(time);

        if (target != null)
        {
            target.gameObject.SetActive(false);
        }

        currentMessage = null;
    }

    private IEnumerator ShowTypewriter(string msg, float time, float characterDelay)
    {
        if (!TryGetMessageText(out TMP_Text target))
        {
            yield break;
        }

        target.text = msg ?? string.Empty;
        target.maxVisibleCharacters = 0;
        target.gameObject.SetActive(true);

        // Let TMP initialize after enabling the object before we read textInfo.
        yield return null;

        if (target == null)
        {
            currentMessage = null;
            yield break;
        }

        target.ForceMeshUpdate();

        if (target.textInfo == null)
        {
            currentMessage = StartCoroutine(Show(msg, time));
            yield break;
        }

        int totalCharacters = target.textInfo.characterCount;
        for (int visibleCharacters = 1; visibleCharacters <= totalCharacters; visibleCharacters++)
        {
            if (target == null)
            {
                currentMessage = null;
                yield break;
            }

            target.maxVisibleCharacters = visibleCharacters;
            yield return new WaitForSeconds(characterDelay);
        }

        yield return new WaitForSeconds(time);

        if (target != null)
        {
            target.maxVisibleCharacters = int.MaxValue;
            target.gameObject.SetActive(false);
        }

        currentMessage = null;
    }

    private bool TryGetMessageText(out TMP_Text target)
    {
        target = ResolveMessageText();
        return target != null;
    }

    private TMP_Text ResolveMessageText()
    {
        if (messageText != null)
        {
            return messageText;
        }

        TMP_Text[] textObjects = FindObjectsOfType<TMP_Text>(true);
        for (int i = 0; i < textObjects.Length; i++)
        {
            if (textObjects[i] != null && textObjects[i].name == "MessageText")
            {
                messageText = textObjects[i];
                break;
            }
        }

        return messageText;
    }
}
