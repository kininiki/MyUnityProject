// using UnityEngine;
// using UnityEngine.UI;
// using Fungus;

// public class ResetProgressButton : MonoBehaviour
// {
//     [SerializeField] private Button resetButton;
//     [SerializeField] private Flowchart flowchart;
//     private FungusSaveSystem saveSystem;

//     private void Awake()
//     {
//         Debug.Log("ResetProgressButton: Awake started");
//         InitializeComponents();
//     }

//     private void InitializeComponents()
//     {
//         // Поиск FungusSaveSystem в сцене
//         saveSystem = FindObjectOfType<FungusSaveSystem>();
//         Debug.Log($"ResetProgressButton: FungusSaveSystem found: {saveSystem != null}");

//         // Получение компонента Button
//         if (!resetButton)
//         {
//             resetButton = GetComponent<Button>();
//             Debug.Log($"ResetProgressButton: Button component found: {resetButton != null}");
//         }

//         // Поиск Flowchart в сцене
//         if (!flowchart)
//         {
//             flowchart = FindObjectOfType<Flowchart>();
//             Debug.Log($"ResetProgressButton: Flowchart found: {flowchart != null}");
//         }

//         // Настройка обработчика кнопки
//         if (resetButton != null)
//         {
//             resetButton.onClick.AddListener(ResetProgress);
//             Debug.Log("ResetProgressButton: Click listener added");
//         }
//         else
//         {
//             Debug.LogError("ResetProgressButton: Button component is still null after initialization!");
//         }
//     }

//     public void ResetProgress()
//     {
//         Debug.Log("ResetProgressButton: ResetProgress called");

//         if (saveSystem == null)
//         {
//             Debug.LogWarning("ResetProgressButton: FungusSaveSystem is null, trying to find it in scene");
//             saveSystem = FindObjectOfType<FungusSaveSystem>();
//         }

//         if (saveSystem != null)
//         {
//             Debug.Log("ResetProgressButton: Attempting to reset save");
//             try
//             {
//                 saveSystem.ResetSave();
//                 Debug.Log("ResetProgressButton: Save reset successful");
                
//                 // Перезагрузка сцены после сброса
//                 UnityEngine.SceneManagement.SceneManager.LoadScene(
//                     UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
//                 );
//             }
//             catch (System.Exception e)
//             {
//                 Debug.LogError($"ResetProgressButton: Error resetting save: {e}");
//             }
//         }
//         else
//         {
//             Debug.LogError("ResetProgressButton: FungusSaveSystem not found in scene!");
//         }
//     }

//     private void OnDestroy()
//     {
//         if (resetButton != null)
//         {
//             resetButton.onClick.RemoveListener(ResetProgress);
//             Debug.Log("ResetProgressButton: Click listener removed");
//         }
//     }
// }