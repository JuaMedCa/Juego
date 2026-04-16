using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class EscapeEndingManager : MonoBehaviour
{
    private static EscapeEndingManager instance;

    private readonly List<AudioSource> pausedAudioSources = new List<AudioSource>();
    private bool isEndingActive;

    public static bool IsEndingActive => instance != null && instance.isEndingActive;

    public static EscapeEndingManager EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindObjectOfType<EscapeEndingManager>();
        if (instance == null)
        {
            GameObject managerObject = new GameObject("EscapeEndingManager");
            instance = managerObject.AddComponent<EscapeEndingManager>();
        }

        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public void TriggerEscape()
    {
        if (isEndingActive)
        {
            return;
        }

        isEndingActive = true;
        Time.timeScale = 0f;
        AudioListener.pause = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        PauseWorldAudio();
        DisableGameplayUi();

        FinishEndingSequenceController endingController = FinishEndingSequenceController.GetOrCreate();
        if (endingController != null && endingController.TryPlay(RestartScene))
        {
            return;
        }

        RestartScene();
    }

    private void PauseWorldAudio()
    {
        pausedAudioSources.Clear();

        AudioSource[] audioSources = FindObjectsOfType<AudioSource>(true);
        for (int i = 0; i < audioSources.Length; i++)
        {
            AudioSource source = audioSources[i];
            if (source == null || !source.isPlaying)
            {
                continue;
            }

            pausedAudioSources.Add(source);
            source.Pause();
        }
    }

    private void DisableGameplayUi()
    {
        MainMenuController menuController = FindObjectOfType<MainMenuController>();
        if (menuController != null)
        {
            menuController.enabled = false;
        }

        PlayerMovemnt movement = FindObjectOfType<PlayerMovemnt>();
        if (movement != null)
        {
            movement.enabled = false;
        }

        IsoCameraOrbit isoCameraOrbit = FindObjectOfType<IsoCameraOrbit>();
        if (isoCameraOrbit != null)
        {
            isoCameraOrbit.enabled = false;
        }

        GameObject interactText = GameObject.Find("InteractText");
        if (interactText != null)
        {
            interactText.SetActive(false);
        }

        GameObject notesCounter = GameObject.Find("Txt_NotasContador");
        if (notesCounter != null)
        {
            notesCounter.SetActive(false);
        }

        ObjectiveSystem.EnsureInstance().SetHudVisible(false);
    }

    private void RestartScene()
    {
        isEndingActive = false;
        AudioListener.pause = false;
        Time.timeScale = 1f;

        Scene currentScene = SceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(currentScene.name))
        {
            SceneManager.LoadScene(currentScene.name);
        }
    }
}
