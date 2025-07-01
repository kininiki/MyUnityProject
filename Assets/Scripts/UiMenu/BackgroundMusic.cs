using UnityEngine;
using UnityEngine.SceneManagement;

public class BackgroundMusic : MonoBehaviour
{
    private static BackgroundMusic instance;
    [SerializeField] private string[] allowedScenes = { "mainMenu", "Pet", "add", "shopRubin", "settings" };
    private AudioSource audioSource;

    public static BackgroundMusic Instance => instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        audioSource = GetComponent<AudioSource>();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        CheckScene(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CheckScene(scene.name);
    }

    private void CheckScene(string sceneName)
    {
        bool shouldPlay = System.Array.IndexOf(allowedScenes, sceneName) >= 0;
        if (audioSource == null) return;

        if (shouldPlay)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
        else
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }

    public void SetMute(bool mute)
    {
        if (audioSource != null)
        {
            audioSource.mute = mute;
        }
    }

    /// <summary>
    /// Pause the background music without resetting its playback position.
    /// </summary>
    public void PauseMusic()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause();
        }
    }

    /// <summary>
    /// Resume background music if it was previously paused.
    /// </summary>
    public void ResumeMusic()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.UnPause();
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}