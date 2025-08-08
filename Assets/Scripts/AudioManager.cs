using UnityEngine.Audio;
using System;
using UnityEngine;

[System.Serializable]
public class Sound : MonoBehaviour
{
    public AudioSource source;

    void Play(AudioClip clip)
    {
        if (source.isPlaying)
            source.Stop();

        source.clip = clip;
        source.Play();
    }
}