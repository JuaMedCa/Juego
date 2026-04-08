using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MessageSystem : MonoBehaviour
{
    public static MessageSystem instance;

    public TMP_Text messageText;

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

    IEnumerator Show(string msg, float time)
    {
        messageText.text = msg;
        messageText.gameObject.SetActive(true);

        yield return new WaitForSeconds(time);

        messageText.gameObject.SetActive(false);
    }
}
