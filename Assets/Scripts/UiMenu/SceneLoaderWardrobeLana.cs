using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneLoaderWardrobeLana : MonoBehaviour
{
    private void Start()
    {
        Button button = GetComponent<Button>();
        button.onClick.AddListener(LoadWardrobeScene);
    }

    private void LoadWardrobeScene()
    {
        // Сохраняем имя текущей сцены перед переходом
        PlayerPrefs.SetString("PreviousScene", SceneManager.GetActiveScene().name);
        SceneManager.LoadScene("Wardrobe_Lana");
    }
}
