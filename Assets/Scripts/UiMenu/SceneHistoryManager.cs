using System.Collections.Generic;
using UnityEngine;

public static class SceneHistoryManager
{
    private static Stack<string> sceneHistory = new Stack<string>();

    // Добавляем сцену в историю
    public static void AddScene(string sceneName)
    {
        sceneHistory.Push(sceneName);
        SaveHistory();
    }

    // Получаем последнюю сцену из истории
    public static string GetPreviousScene()
    {
        if (sceneHistory.Count > 0)
        {
            return sceneHistory.Pop();
        }
        return "";
    }

    // Сохраняем историю в PlayerPrefs
    private static void SaveHistory()
    {
        var historyList = new List<string>(sceneHistory);
        string historyJson = JsonUtility.ToJson(new SceneHistoryWrapper { History = historyList });
        PlayerPrefs.SetString("SceneHistory", historyJson);
        PlayerPrefs.Save();
    }

    // Загружаем историю из PlayerPrefs
    public static void LoadHistory()
    {
        string historyJson = PlayerPrefs.GetString("SceneHistory", "");
        if (!string.IsNullOrEmpty(historyJson))
        {
            var wrapper = JsonUtility.FromJson<SceneHistoryWrapper>(historyJson);
            sceneHistory = new Stack<string>(wrapper.History);
        }
    }

    // Вспомогательный класс для сериализации стека
    [System.Serializable]
    private class SceneHistoryWrapper
    {
        public List<string> History;
    }
}