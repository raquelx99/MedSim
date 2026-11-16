using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class AudioManager : MonoBehaviour
{
    public AudioSource voiceSource;
    public int maxSimultaneousSounds = 4;

    private readonly List<AudioSource> pool = new();

    public void PlayVoice(AudioClip clip)
    {
        if (clip == null) return;

        if (voiceSource == null)
        {
            Debug.LogWarning("AudioManager: voiceSource não configurado, tocando a fala como efeito sonoro.", this);
            Play(clip);
            return;
        }

        voiceSource.Stop();
        voiceSource.clip = clip;
        voiceSource.Play();
    }

    public void StopVoice()
    {
        if (voiceSource != null)
            voiceSource.Stop();
    }

    public void Play(AudioClip clip)
    {
        if (clip == null) return;

        AudioSource availableSource = GetAvailableSource();
        availableSource.clip = clip;
        availableSource.Play();
    }

    public void Stop(AudioClip clip)
    {
        foreach (AudioSource pooledSource in pool)
        {
            if (pooledSource.clip == clip && pooledSource.isPlaying)
                pooledSource.Stop();
        }
    }

    private AudioSource GetAvailableSource()
    {
        foreach (AudioSource pooledSource in pool)
        {
            if (!pooledSource.isPlaying)
                return pooledSource;
        }

        if (pool.Count < Mathf.Max(1, maxSimultaneousSounds))
            return CreatePooledSource();

        AudioSource oldest = pool[0];
        oldest.Stop();
        pool.RemoveAt(0);
        pool.Add(oldest);
        return oldest;
    }

    private AudioSource CreatePooledSource()
    {
        AudioSource created = gameObject.AddComponent<AudioSource>();
        created.playOnAwake = false;
        if (voiceSource != null)
        {
            created.outputAudioMixerGroup = voiceSource.outputAudioMixerGroup;
            created.spatialBlend = voiceSource.spatialBlend;
            created.volume = voiceSource.volume;
        }
        pool.Add(created);
        return created;
    }
}
