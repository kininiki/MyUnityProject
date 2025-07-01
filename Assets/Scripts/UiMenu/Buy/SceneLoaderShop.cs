using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoaderShop : MonoBehaviour
{
    
    // Статическая переменная для хранения информации о выбранной вкладке
    public static string selectedScrollView = "Ruby";

    // Метод для загрузки сцены shopRubin с включением rubyScrollView
    public void LoadShopSceneWithRuby()
    {
        SaveCurrentScene();
        ClearOldIAP();
        SceneHistoryManager.AddScene(SceneManager.GetActiveScene().name);
        selectedScrollView = "Ruby"; // Устанавливаем Ruby как активную вкладку
        SceneManager.LoadScene("shopRubin");
        
    }

    // Метод для загрузки сцены shopRubin с включением elixirScrollView
    public void LoadShopSceneWithElixir()
    {
        SaveCurrentScene();
        ClearOldIAP();
        SceneHistoryManager.AddScene(SceneManager.GetActiveScene().name);
        selectedScrollView = "Elixir"; // Устанавливаем Elixir как активную вкладку
        SceneManager.LoadScene("shopRubin");
        
    }

    // Метод для загрузки сцены shopRubin с включением catmoneyScrollView
    public void LoadShopSceneWithCatmoney()
    {
        SaveCurrentScene();
        ClearOldIAP();
        SceneHistoryManager.AddScene(SceneManager.GetActiveScene().name);
        selectedScrollView = "Catmoney"; // Устанавливаем Catmoney как активную вкладку
        SceneManager.LoadScene("shopRubin");
        
    }

    // Метод для загрузки питомца
    public void LoadPetScene()
    {
        SaveCurrentScene();
        ClearOldIAP();
        SceneHistoryManager.AddScene(SceneManager.GetActiveScene().name);
        SceneManager.LoadScene("Pet");
        
    }

    // Метод для загрузки рекламы
    public void LoadAds()
    {
        SaveCurrentScene();
        ClearOldIAP();
        SceneHistoryManager.AddScene(SceneManager.GetActiveScene().name);
        SceneManager.LoadScene("add");

    }

    // Метод для загрузки настроек
    public void LoadSettings()
    {
        SaveCurrentScene();
        ClearOldIAP();
        SceneHistoryManager.AddScene(SceneManager.GetActiveScene().name);
        SceneManager.LoadScene("settings");

    }

    // Метод для загрузки главного меню
    public void LoadMainMenuScene()
    {
        ClearOldIAP();
        SceneHistoryManager.AddScene(SceneManager.GetActiveScene().name);
        SceneManager.LoadScene("mainMenu");

    }

    // Метод для загрузки сцены Wardrobe_Lana
    public void GoToWardrobeLana()
    {
        SaveCurrentScene();
        ClearOldIAP();
        SceneHistoryManager.AddScene(SceneManager.GetActiveScene().name);
        SceneManager.LoadScene("Wardrobe_Lana");

    }

    // Метод для загрузки сцены LanaCollection
    public void GoToLanaCollection()
    {
        SaveCurrentScene();
        ClearOldIAP();
        SceneHistoryManager.AddScene(SceneManager.GetActiveScene().name);
        SceneManager.LoadScene("LanaCollection");

    }




    // Сохраняем текущую сцену
    private void SaveCurrentScene()
    {
        PlayerPrefs.SetString("PreviousScene", SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();
    }

    void ClearOldIAP()
    {
        var oldListeners = FindObjectsOfType<UnityEngine.Purchasing.IAPListener>();
        
        if (oldListeners.Length > 1)
        {
            for (int i = 1; i < oldListeners.Length; i++) // Оставляем один, удаляем остальные
            {
                Debug.Log($"❌ Удаляем дубликат IAPListener: {oldListeners[i].gameObject.name}");
                Destroy(oldListeners[i].gameObject);
            }
        }
    }
}