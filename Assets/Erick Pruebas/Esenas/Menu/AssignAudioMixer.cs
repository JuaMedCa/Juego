using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AssignAudioMixer : MonoBehaviour
{
    public AudioMixerGroup targetGroup;

    void Start()
    {
        AudioSource[] sources = FindObjectsOfType<AudioSource>(true);

        foreach (AudioSource source in sources)
        {
            source.outputAudioMixerGroup = targetGroup;
        }

        Debug.Log("Todos los AudioSources fueron asignados al mixer");
    }
}
