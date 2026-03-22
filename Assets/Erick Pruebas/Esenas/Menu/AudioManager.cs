using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    public AudioSource audioSource;

    void Awake()
    {
        if (instance == null)
            instance = this;
    }

    void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat("volume", 1f);
        audioSource.volume = savedVolume;
    }

    public void SetVolume(float volume)
    {
        audioSource.volume = volume;
        PlayerPrefs.SetFloat("volume", volume);
    }
}
