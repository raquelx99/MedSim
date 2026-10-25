using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource source;
    public int maxSimultaneousSounds = 4;

    private readonly List<AudioSource> pool = new();

    void Awake()
    {
        if (source != null)
            pool.Add(source);
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
        return oldest;
    }

    private AudioSource CreatePooledSource()
    {
        AudioSource created = gameObject.AddComponent<AudioSource>();
        if (source != null)
        {
            created.outputAudioMixerGroup = source.outputAudioMixerGroup;
            created.spatialBlend = source.spatialBlend;
            created.volume = source.volume;
        }
        pool.Add(created);
        return created;
    }
}
