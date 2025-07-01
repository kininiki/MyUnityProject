using UnityEngine;
using Fungus;
using System.Collections;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Audio;

[CommandInfo("Audio", "Play Music New", "Plays a music track with optional looping, fade-in, and volume control.")]
public class PlayMusicCommand : Command
{
    [Tooltip("Audio source to play music from (leave empty to auto-create one).")]
    public AudioSource audioSource;

    [Tooltip("Addressable Audio Clip to play as music.")]
    public AssetReference musicClipReference;  // Заменили на AssetReference для загрузки через Addressables

    [Tooltip("Volume of the music (0 to 1).")]
    [Range(0f, 1f)]
    public float volume = 1.0f;

    [Tooltip("Loop the music.")]
    public bool loop = true;

    [Tooltip("Duration of the fade-in (in seconds).")]
    public float fadeInDuration = 2.0f;

    private AsyncOperationHandle<AudioClip> musicClipHandle;  // Для отслеживания загрузки клипа

    public override void OnEnter()
    {
        // Если AudioSource не указан, создаём его
        if (audioSource == null)
        {
            GameObject audioObject = new GameObject("MusicPlayer");
            audioSource = audioObject.AddComponent<AudioSource>();
            DontDestroyOnLoad(audioObject);
        }

        if (musicClipReference != null)
        {
            // Загружаем музыку через Addressables
            LoadMusicClipAsync();
        }
        else
        {
            Debug.LogWarning("No music clip assigned to PlayMusicCommand.");
            Continue();
        }
    }

    // Асинхронная загрузка музыкального клипа через Addressables
    private void LoadMusicClipAsync()
    {
        // Запускаем загрузку через Addressables
        musicClipHandle = musicClipReference.LoadAssetAsync<AudioClip>();

        // Подписываемся на завершение загрузки
        musicClipHandle.Completed += OnMusicClipLoaded;
    }

    // Обработчик завершения загрузки музыкального клипа
    private void OnMusicClipLoaded(AsyncOperationHandle<AudioClip> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            AudioClip musicClip = handle.Result;  // Присваиваем загруженный клип

            // Настроим и проиграем музыку
            audioSource.clip = musicClip;
            audioSource.volume = 0f;  // Начинаем с 0 громкости
            audioSource.loop = loop;

            audioSource.Play();
            StartCoroutine(FadeInMusic(audioSource, volume, fadeInDuration));
        }
        else
        {
            Debug.LogError("Failed to load music clip.");
        }

        Continue();  // Продолжаем выполнение команды, когда музыка загружена
    }

    private IEnumerator FadeInMusic(AudioSource source, float targetVolume, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, targetVolume, time / duration);
            yield return null;
        }
        source.volume = targetVolume; // Убедимся, что громкость достигла целевого значения
    }

    public override string GetSummary()
    {
        if (musicClipReference == null)
        {
            return "Error: No music clip reference set!";
        }

        return $"Play music from Addressables with fade-in (Loop: {loop})";
    }

    public override Color GetButtonColor()
    {
        return new Color32(255, 210, 125, 255); // Светло-оранжевый цвет
    }

    private void OnDestroy()
    {
        // Освобождаем ресурсы, если они больше не нужны
        if (musicClipHandle.IsValid())
        {
            Addressables.Release(musicClipHandle);
        }
    }
}







[CommandInfo("Audio", "Stop Music New", "Stops the currently playing music with a fade-out effect.")]
public class StopMusicCommand : Command
{
    [Tooltip("Audio source to stop music from.")]
    public AudioSource audioSource;

    [Tooltip("Duration of the fade-out (in seconds).")]
    public float fadeOutDuration = 2.0f;

    public override void OnEnter()
    {
        if (audioSource == null)
        {
            Debug.LogWarning("No AudioSource specified in StopMusicCommand.");
            Continue();
            return;
        }

        if (audioSource.isPlaying)
        {
            StartCoroutine(FadeOutMusic(audioSource, fadeOutDuration));
        }
        else
        {
            Debug.LogWarning("AudioSource is not playing any music.");
            Continue();
        }
    }

    private IEnumerator FadeOutMusic(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, time / duration);
            yield return null;
        }

        source.volume = 0f; // Убедимся, что громкость опустилась до 0
        source.Stop();
        source.clip = null; // Освобождаем память
        Continue();
    }

    public override string GetSummary()
    {
        return "Stop music with fade-out";
    }

    public override Color GetButtonColor()
    {
        return new Color32(255, 125, 125, 255); // Светло-красный цвет
    }
}



[CommandInfo("Audio", "Play OneShot Sound", "Plays a one-shot sound effect with a specified volume and pitch.")]
public class PlayOneShotSoundCommand : Command
{
    [Tooltip("Audio clip to play as a one-shot sound.")]
    public AudioClip soundClip;

    [Tooltip("Volume of the sound (0 to 1).")]
    [Range(0f, 1f)]
    public float volume = 1.0f;

    [Tooltip("Pitch of the sound (1 = normal, <1 = lower, >1 = higher).")]
    [Range(0.5f, 2.0f)]
    public float pitch = 1.0f;

    [Tooltip("Audio source to play the sound from (leave empty to create a temporary one).")]
    public AudioSource audioSource;

    public override void OnEnter()
    {
        if (soundClip == null)
        {
            Debug.LogWarning("No sound clip assigned to PlayOneShotSoundCommand.");
            Continue();
            return;
        }

        // Если AudioSource не указан, создаём временный AudioSource
        if (audioSource == null)
        {
            GameObject tempAudioObject = new GameObject("TempAudioSource");
            audioSource = tempAudioObject.AddComponent<AudioSource>();

            // Настраиваем временный AudioSource
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D звук
            audioSource.volume = volume;
            audioSource.pitch = pitch;

            // Воспроизводим звук
            audioSource.PlayOneShot(soundClip, volume);

            // Уничтожаем объект после окончания звука
            Destroy(tempAudioObject, soundClip.length / pitch);
        }
        else
        {
            // Настраиваем указанный AudioSource
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(soundClip, volume);
        }

        Continue();
    }

    public override string GetSummary()
    {
        if (soundClip == null)
        {
            return "Error: No sound clip selected!";
        }
        return $"Play sound '{soundClip.name}' at volume {volume}";
    }

    public override Color GetButtonColor()
    {
        return new Color32(125, 255, 125, 255); // Светло-зелёный цвет
    }
}