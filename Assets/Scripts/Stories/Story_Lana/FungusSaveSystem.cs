// using UnityEngine;
// using System;
// using Fungus;
// using System.Linq;




// public class FungusSaveSystem : MonoBehaviour
// {
//     [SerializeField] private Flowchart flowchart;

//     private const string BLOCK_NAME_KEY = "SavedBlockName";
//     private const string COMMAND_INDEX_KEY = "SavedCommandIndex";

//     private bool isInitialized = false; // Флаг для предотвращения преждевременного запуска Flowchart

//     private void Awake()
//     {
//         if (!flowchart)
//         {
//             flowchart = FindObjectOfType<Flowchart>();
//         }

//         if (!flowchart)
//         {
//             Debug.LogError("FungusSaveSystem: Flowchart not found!");
//             return;
//         }

//         Debug.Log("FungusSaveSystem Awake. Flowchart found: " + flowchart.name);

//         // Останавливаем Flowchart, чтобы он не начал выполнение
//         flowchart.StopAllBlocks();
//     }

//     private void Start()
//     {
//         if (!flowchart)
//         {
//             Debug.LogError("FungusSaveSystem: No Flowchart assigned.");
//             return;
//         }

//         Debug.Log("FungusSaveSystem Start");

//         // Проверяем, есть ли сохраненные данные
//         if (HasSaveData())
//         {
//             Debug.Log("Save data found. Loading progress...");
//             LoadAndContinue(); // Загружаем сохранения
//         }
//         else
//         {
//             Debug.Log("No save data found. Executing default block.");
//             ExecuteDefaultBlock(); // Запускаем стандартный блок
//         }

//         // Устанавливаем флаг, чтобы разрешить запуск Flowchart
//         isInitialized = true;
//     }

//     private bool HasSaveData()
//     {
//         return PlayerPrefs.HasKey(BLOCK_NAME_KEY) && PlayerPrefs.HasKey(COMMAND_INDEX_KEY);
//     }

//     private void LoadAndContinue()
//     {
//         string blockName = PlayerPrefs.GetString(BLOCK_NAME_KEY);
//         int commandIndex = PlayerPrefs.GetInt(COMMAND_INDEX_KEY);

//         Block targetBlock = flowchart.FindBlock(blockName);

//         if (targetBlock != null)
//         {
//             Debug.Log($"Resuming progress: Block='{blockName}', CommandIndex={commandIndex}");

//             // Проверяем корректность индекса команды
//             if (commandIndex >= 0 && commandIndex < targetBlock.CommandList.Count)
//             {
//                 targetBlock.JumpToCommandIndex = commandIndex;
//             }
//             else
//             {
//                 Debug.LogWarning("Saved command index out of range. Starting block from the beginning.");
//                 targetBlock.JumpToCommandIndex = 0;
//             }

//             // Запускаем сохранённый блок
//             flowchart.ExecuteBlock(targetBlock);
//         }
//         else
//         {
//             Debug.LogError($"Block '{blockName}' not found. Executing default block.");
//             ExecuteDefaultBlock();
//         }
//     }

//     private void ExecuteDefaultBlock()
//     {
//         Block startBlock = flowchart.FindBlock("Start");

//         if (startBlock != null)
//         {
//             Debug.Log("Executing default block 'Start'.");
//             flowchart.ExecuteBlock(startBlock);
//         }
//         else
//         {
//             Debug.LogError("Default block 'Start' not found in Flowchart!");
//         }
//     }

//     public void SaveCurrentPosition(Command command)
//     {
//         if (command != null && command.ParentBlock != null)
//         {
//             Block block = command.ParentBlock;
//             int currentIndex = block.CommandList.IndexOf(command);

//             PlayerPrefs.SetString(BLOCK_NAME_KEY, block.BlockName);
//             PlayerPrefs.SetInt(COMMAND_INDEX_KEY, currentIndex);
//             PlayerPrefs.Save();

//             Debug.Log($"Saved progress: Block={block.BlockName}, CommandIndex={currentIndex}");
//         }
//         else
//         {
//             Debug.LogError("Failed to save progress: Command or ParentBlock is null.");
//         }
//     }

//     public void ResetSave()
//     {
//         PlayerPrefs.DeleteKey(BLOCK_NAME_KEY);
//         PlayerPrefs.DeleteKey(COMMAND_INDEX_KEY);
//         PlayerPrefs.Save();

//         Debug.Log("Saved progress has been reset.");
//     }

//     private void Update()
//     {
//         // Если Flowchart не инициализирован, блокируем его выполнение
//         if (!isInitialized)
//         {
//             flowchart.StopAllBlocks();
//         }
//     }
// }







// [CommandInfo("Flow", 
//     "Save Progress", 
//     "Сохраняет текущую позицию в flowchart")]
// public class SaveCommand : Command
// {
//     public override void OnEnter()
//     {
//         Debug.Log("Save Command executing");
//         var saveSystem = GetComponent<FungusSaveSystem>();
//         if (saveSystem != null)
//         {
//             saveSystem.SaveCurrentPosition(this);
//             Debug.Log("Position saved successfully");
//         }
//         else
//         {
//             Debug.LogError("FungusSaveSystem component not found!");
//         }
//         Continue();
//     }
// }



// [CommandInfo("Flow", 
//              "Reset Saved Progress", 
//              "Resets the saved position in the flowchart (block name and command index).")]
// public class ResetSavedProgressCommand : Command
// {
//     public override void OnEnter()
//     {
//         Debug.Log("ResetSavedProgressCommand executing: clearing save data.");

//         // Удаляем сохранённые данные
//         PlayerPrefs.DeleteKey("SavedBlockName");
//         PlayerPrefs.DeleteKey("SavedCommandIndex");
//         PlayerPrefs.Save();

//         Debug.Log("Saved progress has been reset.");

//         // Продолжаем выполнение следующих команд
//         Continue();
//     }

//     public override string GetSummary()
//     {
//         return "Resets the saved progress (block name and command index).";
//     }

//     public override Color GetButtonColor()
//     {
//         return new Color32(255, 100, 100, 255); // Красный цвет кнопки для выделения
//     }
// }


