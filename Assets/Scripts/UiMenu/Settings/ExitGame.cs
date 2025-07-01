using UnityEngine;

public class ExitGame : MonoBehaviour
{
    public void QuitGame()
    {
        Debug.Log("Выход из игры"); // Для проверки в редакторе
        Application.Quit(); // Закрывает приложение

        // В редакторе Unity выход не работает, поэтому можно добавить:
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}