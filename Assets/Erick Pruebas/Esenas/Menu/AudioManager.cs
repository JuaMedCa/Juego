using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;


public class AudioManager : MonoBehaviour
{
    public AudioMixer mixer;
    public Slider volumeSlider;

    void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat("volume", 1f);

        if (savedVolume <= 0.0001f)
            savedVolume = 1f;

        volumeSlider.minValue = 0.0001f;
        volumeSlider.maxValue = 1f;
        volumeSlider.wholeNumbers = false;

        volumeSlider.value = savedVolume;

        SetVolume(savedVolume);
    }

    public void SetVolume(float value)
    {
        float volume;

        if (value <= 0.0001f)
            volume = -80f;
        else
            volume = Mathf.Lerp(-40f, 0f, value);

        mixer.SetFloat("SFXVolume", volume); 

        PlayerPrefs.SetFloat("volume", value);
        PlayerPrefs.Save();
    }
}
