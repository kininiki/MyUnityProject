using UnityEngine;
using Fungus;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Newtonsoft.Json;

[CommandInfo("Cloud", "Save Data to Cloud", "Save key-value pairs to Unity Cloud Save in a specific section.")]
public class CloudSaveCommand : Command
{
    [Tooltip("Cloud section (key) where the data will be stored")]
    [SerializeField] private string cloudSection = "LANA_UNLOCK_VARIABLES";
    
    [Tooltip("Key for the value you want to store (e.g., 'w1')")]
    [SerializeField] private string dataKey;
    
    [Tooltip("Value for the key (e.g., '1')")]
    [SerializeField] private string dataValue;

    public override async void OnEnter()
    {
        Debug.Log("CloudSaveCommand.OnEnter called.");
        Debug.Log($"Starting save process for Section: {cloudSection}, Key: {dataKey}, Value: {dataValue}");

        // Инициализация Unity Services
        await InitializeUnityServices();

        // Проверка успешной инициализации
        if (!UnityServices.State.Equals(ServicesInitializationState.Initialized))
        {
            Debug.LogError("Unity Services failed to initialize. Stopping cloud save.");
            Continue();
            return;
        }

        // Сохранение данных
        await SaveDataToCloud(cloudSection, dataKey, dataValue);

        Debug.Log("Cloud save process completed.");
        Continue();
    }

    private async System.Threading.Tasks.Task InitializeUnityServices()
    {
        Debug.Log("Initializing Unity Services...");
        if (UnityServices.State == ServicesInitializationState.Initialized)
        {
            Debug.Log("Unity Services already initialized.");
            return;
        }

        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("Successfully signed in to Unity Services.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize Unity Services: {e.Message}");
        }
    }



private async System.Threading.Tasks.Task<Dictionary<string, string>> LoadDataFromCloud(string section)
{
    Debug.Log($"Attempting to load data from cloud for section: {section}");

    try
    {
        // Загружаем данные из облака
        var loadedData = await CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> { section });

        // Логируем загруженные данные
        Debug.Log($"Loaded raw data from cloud: {JsonConvert.SerializeObject(loadedData)}");

        // Проверяем, есть ли данные в указанной секции
        if (loadedData.TryGetValue(section, out var jsonData))
        {
            Debug.Log($"Raw JSON data for section '{section}': {jsonData}");

            // Проверяем, является ли загруженное значение корректным JSON
            if (!string.IsNullOrEmpty(jsonData) && IsValidJson(jsonData))
            {
                try
                {
                    // Десериализуем строку JSON в словарь
                    var deserializedData = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData);
                    if (deserializedData != null)
                    {
                        Debug.Log($"Deserialized data: {JsonConvert.SerializeObject(deserializedData)}");
                        return deserializedData;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to deserialize JSON data: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"Data in section '{section}' is not valid JSON or is empty. Returning empty dictionary.");
            }
        }
        else
        {
            Debug.Log($"No data found for section '{section}'. Returning empty dictionary.");
        }
    }
    catch (System.Exception e)
    {
        Debug.LogError($"Error loading data from cloud: {e.Message}");
    }

    // Если ничего не загрузилось или произошла ошибка, возвращаем пустой словарь
    return new Dictionary<string, string>();
}



private async System.Threading.Tasks.Task SaveDataToCloud(string section, string key, string value)
{
    Debug.Log($"Preparing to save data to cloud. Section: {section}, Key: {key}, Value: {value}");

    // Загружаем текущие данные из облака
    Dictionary<string, string> existingData = await LoadDataFromCloud(section);

    // Если словарь не загружен (null), инициализируем новый
    if (existingData == null)
    {
        Debug.LogWarning("No existing data found. Creating a new dictionary.");
        existingData = new Dictionary<string, string>();
    }

    Debug.Log($"Existing data before update: {JsonConvert.SerializeObject(existingData)}");

    // Проверяем, существует ли ключ
    if (existingData.ContainsKey(key))
    {
        Debug.Log($"Key '{key}' already exists with value '{existingData[key]}'. Updating to new value '{value}'.");
        existingData[key] = value; // Обновляем значение
    }
    else
    {
        Debug.Log($"Key '{key}' does not exist. Adding it with value '{value}'.");
        existingData.Add(key, value); // Добавляем новый ключ и значение
    }

    Debug.Log($"Data after update: {JsonConvert.SerializeObject(existingData)}");

    // Сохраняем обновлённый словарь обратно в облако
    try
    {
        string jsonData = JsonConvert.SerializeObject(existingData);
        Debug.Log($"Saving JSON data to cloud: {jsonData}");

        await CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object>
        {
            { section, jsonData }
        });

        Debug.Log("Data successfully saved to cloud.");
    }
    catch (System.Exception e)
    {
        Debug.LogError($"Failed to save data to cloud: {e.Message}");
    }
}


    private bool IsValidJson(string jsonData)
    {
        try
        {
            JsonConvert.DeserializeObject(jsonData);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public override string GetSummary()
    {
        return $"Save {dataKey}={dataValue} to {cloudSection}";
    }
}





[CommandInfo("Cloud", "Save Flowchart Variable to Cloud", "Save an integer Flowchart variable to Unity Cloud Save under a specific section and key.")]
public class CloudSaveFlowchartVariableCommand : Command
{
    [Tooltip("Cloud section (key) where the data will be stored")]
    [SerializeField] private string cloudSection = "PLAYER_RUBY";

    [Tooltip("Key for the value you want to store (e.g., 'ruby')")]
    [SerializeField] private string dataKey;

    [Tooltip("Flowchart variable name to save")]
    [SerializeField] private string flowchartVariableName;

    private Flowchart flowchart;

    public override async void OnEnter()
    {
        Debug.Log("CloudSaveFlowchartVariableCommand.OnEnter called.");
        Debug.Log($"Starting save process for Section: {cloudSection}, Key: {dataKey}, Variable: {flowchartVariableName}");

        // Получение Flowchart
        flowchart = GetFlowchart();

        if (flowchart == null)
        {
            Debug.LogError("Flowchart not found. Aborting cloud save.");
            Continue();
            return;
        }

        // Получение значения переменной Flowchart
        if (!flowchart.HasVariable(flowchartVariableName))
        {
            Debug.LogError($"Flowchart does not have a variable named '{flowchartVariableName}'. Aborting cloud save.");
            Continue();
            return;
        }

        int variableValue = flowchart.GetIntegerVariable(flowchartVariableName);

        // Инициализация Unity Services
        await InitializeUnityServices();

        if (!UnityServices.State.Equals(ServicesInitializationState.Initialized))
        {
            Debug.LogError("Unity Services failed to initialize. Stopping cloud save.");
            Continue();
            return;
        }

        // Сохранение данных
        await SaveDataToCloud(cloudSection, dataKey, variableValue);

        Debug.Log("Cloud save process completed.");
        Continue();
    }


    private async System.Threading.Tasks.Task InitializeUnityServices()
    {
        Debug.Log("Initializing Unity Services...");
        if (UnityServices.State == ServicesInitializationState.Initialized)
        {
            Debug.Log("Unity Services already initialized.");
            return;
        }

        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("Successfully signed in to Unity Services.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize Unity Services: {e.Message}");
        }
    }

    private async System.Threading.Tasks.Task SaveDataToCloud(string section, string key, int value)
    {
        Debug.Log($"Preparing to save data to cloud. Section: {section}, Key: {key}, Value: {value}");

        // Загружаем текущие данные из облака
        Dictionary<string, int> existingData = await LoadDataFromCloud(section);

        if (existingData == null)
        {
            Debug.LogWarning("No existing data found. Creating a new dictionary.");
            existingData = new Dictionary<string, int>();
        }

        Debug.Log($"Existing data before update: {JsonConvert.SerializeObject(existingData)}");

        // Обновляем или добавляем значение
        if (existingData.ContainsKey(key))
        {
            Debug.Log($"Key '{key}' already exists with value '{existingData[key]}'. Updating to new value '{value}'.");
            existingData[key] = value;
        }
        else
        {
            Debug.Log($"Key '{key}' does not exist. Adding it with value '{value}'.");
            existingData.Add(key, value);
        }

        Debug.Log($"Data after update: {JsonConvert.SerializeObject(existingData)}");

        // Сохраняем обновлённый словарь обратно в облако
        try
        {
            string jsonData = JsonConvert.SerializeObject(existingData);
            Debug.Log($"Saving JSON data to cloud: {jsonData}");

            await CloudSaveService.Instance.Data.ForceSaveAsync(new Dictionary<string, object>
            {
                { section, jsonData }
            });

            Debug.Log("Data successfully saved to cloud.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save data to cloud: {e.Message}");
        }
    }

    private async Task<Dictionary<string, int>> LoadDataFromCloud(string section)
    {
        Debug.Log($"Attempting to load data from cloud for section: {section}");

        try
        {
            var loadedData = await CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> { section });

            if (loadedData.TryGetValue(section, out var jsonData))
            {
                Debug.Log($"Raw data loaded from cloud for section '{section}': {jsonData}");

                string jsonString = jsonData.ToString();

                if (IsValidJson(jsonString))
                {
                    var deserializedData = JsonConvert.DeserializeObject<Dictionary<string, int>>(jsonString);
                    Debug.Log($"Deserialized data: {JsonConvert.SerializeObject(deserializedData)}");
                    return deserializedData;
                }
                else
                {
                    Debug.LogWarning($"Data in section '{section}' is not valid JSON. Returning empty dictionary.");
                    return new Dictionary<string, int>();
                }
            }
            else
            {
                Debug.Log($"No data found in cloud for section '{section}'. Returning empty dictionary.");
                return new Dictionary<string, int>();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading data from cloud: {e.Message}");
            return new Dictionary<string, int>();
        }
    }

    private bool IsValidJson(string jsonData)
    {
        try
        {
            JsonConvert.DeserializeObject(jsonData);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public override string GetSummary()
    {
        return $"Save Flowchart variable '{flowchartVariableName}' to {cloudSection} under key '{dataKey}'";
    }
}