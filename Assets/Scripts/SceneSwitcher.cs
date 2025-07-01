using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    // Метод для перехода на следующую сцену
    public void GoToMainMenu()
    {
        ClearOldIAP();
        SceneManager.LoadScene("mainMenu"); // Название вашей сцены
    }



    void ClearOldIAP()
    {
        var oldListeners = FindObjectsOfType<UnityEngine.Purchasing.IAPListener>();
        foreach (var listener in oldListeners)
        {
            Destroy(listener.gameObject);
        }

        GameObject[] dontDestroyObjects = FindObjectsOfType<GameObject>();
        foreach (var obj in dontDestroyObjects)
        {
            if (obj.name == "DontDestroyOnLoad")
            {
                Destroy(obj);
            }
        }
    }
}
