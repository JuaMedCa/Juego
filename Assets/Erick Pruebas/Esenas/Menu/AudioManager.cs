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

    public void SetVolume(float volume)
    {
        audioSource.volume = volume;
    }
}
