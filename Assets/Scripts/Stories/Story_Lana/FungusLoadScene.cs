using UnityEngine;
using Fungus;
using UnityEngine.SceneManagement;

namespace Fungus
{
    [CommandInfo("Flow",
                 "Load Scene With History",
                 "Loads a new Unity scene and adds it to the scene history.")]
    [AddComponentMenu("")]
    public class LoadSceneWithHistory : Command
    {
        [Tooltip("Name of the scene to load. The scene must also be added to the build settings.")]
        [SerializeField] protected StringData sceneName = new StringData("");

        public override void OnEnter()
        {
            if (string.IsNullOrEmpty(sceneName.Value))
            {
                Debug.LogError("Scene name is empty!");
                Continue();
                return;
            }

            // Сохраняем текущую сцену в истории
            SceneHistoryManager.AddScene(SceneManager.GetActiveScene().name);

            // Загружаем новую сцену
            SceneManager.LoadScene(sceneName.Value);

            // Продолжаем выполнение Fungus
            Continue();
        }

        public override string GetSummary()
        {
            if (string.IsNullOrEmpty(sceneName.Value))
            {
                return "Error: No scene name selected";
            }

            return "Load Scene: " + sceneName.Value;
        }

        public override Color GetButtonColor()
        {
            return new Color32(235, 191, 217, 255);
        }
    }
}