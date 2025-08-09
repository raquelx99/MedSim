using UnityEngine.Audio;
using System;
using UnityEngine;

[System.Serializable]
public class AudioManager : MonoBehaviour
{
    public AudioSource source;

    public void Play(AudioClip clip)
    {
        if (source.isPlaying)
            source.Stop();

        source.clip = clip;
        source.Play();
    }

    public void Stop(AudioClip clip)
    {
        if (source.clip == clip && source.isPlaying)
        {
            source.Stop();
        }
    }
}