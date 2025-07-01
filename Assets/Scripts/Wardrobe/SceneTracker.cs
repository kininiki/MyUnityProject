using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTracker : MonoBehaviour
{
    void Start()
    {
        // Сохраняем имя текущей сцены
        PlayerPrefs.SetString("PreviousScene", SceneManager.GetActiveScene().name);
    }
}