using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class BackButtonController : MonoBehaviour
{
    private Button backButton;

    void Start()
    {
        // Получаем компонент Button
        backButton = GetComponent<Button>();
        
        // Добавляем слушатель на кнопку
        backButton.onClick.AddListener(GoToPreviousScene);
    }

    void GoToPreviousScene()
    {
        string previousScene = SceneHistoryManager.GetPreviousScene();
        if (!string.IsNullOrEmpty(previousScene))
        {
            SceneManager.LoadScene(previousScene);
        }
        else
        {
            Debug.LogWarning("Предыдущая сцена не найдена!");
            SceneManager.LoadScene("mainMenu");
        }
    }

    void OnDestroy()
    {
        // Очищаем слушатель при уничтожении объекта
        backButton.onClick.RemoveListener(GoToPreviousScene);
    }
}