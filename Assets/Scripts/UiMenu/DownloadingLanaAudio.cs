using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pauses every other <see cref="AudioSource"/> in the scene and plays the
/// configured source when this object becomes active. When deactivated, previously
/// playing sources resume and the configured source stops.
/// </summary>
public class DownloadLanaAudio : MonoBehaviour
{
    [SerializeField] private AudioSource activeSource;
    private readonly List<AudioSource> pausedSources = new List<AudioSource>();

    private void OnEnable()
    {
        if (activeSource == null)
        {
            activeSource = GetComponent<AudioSource>();
        }

        AudioSource[] allSources = FindObjectsOfType<AudioSource>();
        foreach (var source in allSources)
        {
            if (source == activeSource) continue;
            if (source.isPlaying)
            {
                pausedSources.Add(source);
            }
            source.Pause();
        }

        activeSource?.Play();
    }

    private void OnDisable()
    {
        foreach (var source in pausedSources)
        {
            if (source != null)
            {
                source.UnPause();
            }
        }
        pausedSources.Clear();
        activeSource?.Stop();
    }
}