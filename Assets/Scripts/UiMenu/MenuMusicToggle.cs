using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Plays a temporary music track when a specific button is pressed. The normal
/// background music pauses while this special track is playing. Additional
/// buttons can stop the special track to resume the main music.
/// </summary>
public class MenuMusicToggle : MonoBehaviour
{
    [SerializeField] private AudioSource specialMusic;
    [SerializeField] private AudioSource mainMusic;
    [SerializeField] private Button playButton;     // Button that starts the special track
    [SerializeField] private Button[] stopButtons;  // Buttons that stop the special track

    private bool playing;

    private void OnEnable()
    {
        // Ensure the special track doesn't auto-play when the viewport is enabled
        playing = false;
        if (specialMusic != null)
        {
            specialMusic.Stop();
        }

        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayButtonPressed);
        }

        if (stopButtons != null)
        {
            foreach (var btn in stopButtons)
            {
                if (btn != null && btn != playButton)
                {
                    btn.onClick.AddListener(StopMusic);
                }
            }
        }
    }

    private void OnDisable()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(OnPlayButtonPressed);
        }

        if (stopButtons != null)
        {
            foreach (var btn in stopButtons)
            {
                if (btn != null && btn != playButton)
                {
                    btn.onClick.RemoveListener(StopMusic);
                }
            }
        }

        StopMusic();
    }

    private void OnPlayButtonPressed()
    {
        if (playing)
        {
            StopMusic();
        }
        else
        {
            PlayMusic();
        }
    }

    private void PlayMusic()
    {
        if (specialMusic == null)
            return;

        playing = true;
        specialMusic.Play();

        if (mainMusic != null)
        {
            if (mainMusic.isPlaying)
                mainMusic.Pause();
        }
        else if (BackgroundMusic.Instance != null)
        {
            BackgroundMusic.Instance.PauseMusic();
        }
    }

    public void StopMusic()
    {
        if (specialMusic != null)
        {
            specialMusic.Stop();
        }
        playing = false;

        if (mainMusic != null)
        {
            mainMusic.UnPause();
        }
        else if (BackgroundMusic.Instance != null)
        {
            BackgroundMusic.Instance.ResumeMusic();
        }
    }
}