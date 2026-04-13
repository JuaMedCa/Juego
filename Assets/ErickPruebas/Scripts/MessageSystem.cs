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
        if (messageText == null)
        {
            yield break;
        }

        messageText.text = msg;
        messageText.maxVisibleCharacters = int.MaxValue;
        messageText.gameObject.SetActive(true);

        yield return new WaitForSeconds(time);

        messageText.gameObject.SetActive(false);
    }

    private IEnumerator ShowTypewriter(string msg, float time, float characterDelay)
    {
        if (messageText == null)
        {
            yield break;
        }

        messageText.text = msg;
        messageText.maxVisibleCharacters = 0;
        messageText.gameObject.SetActive(true);
        messageText.ForceMeshUpdate();

        int totalCharacters = messageText.textInfo.characterCount;
        for (int visibleCharacters = 1; visibleCharacters <= totalCharacters; visibleCharacters++)
        {
            messageText.maxVisibleCharacters = visibleCharacters;
            yield return new WaitForSeconds(characterDelay);
        }

        yield return new WaitForSeconds(time);

        messageText.maxVisibleCharacters = int.MaxValue;
        messageText.gameObject.SetActive(false);
    }
}
