using UnityEngine;
using Fungus;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

[CommandInfo("Cloud", "Load Cloud Variable 2", "Loads multiple category indexes from Unity Cloud Save, logs them, and stores them in Flowchart variables.")]
public class LoadCloudVariable2Command : Command
{
    [Tooltip("The cloud section name, for example 'LANA_WARDROBE_DATA'.")]
    public string cloudSection;

    // Пары ключ-значение для каждой категории и переменной Flowchart
    [Tooltip("The category 'Type' key and associated Flowchart variable.")]
    public string typeCategoryKey;
    [VariableProperty(typeof(IntegerVariable))]
    public IntegerVariable typeCategoryValue;

    [Tooltip("The category 'Makeup' key and associated Flowchart variable.")]
    public string makeupCategoryKey;
    [VariableProperty(typeof(IntegerVariable))]
    public IntegerVariable makeupCategoryValue;

    [Tooltip("The category 'Hair' key and associated Flowchart variable.")]
    public string hairCategoryKey;
    [VariableProperty(typeof(IntegerVariable))]
    public IntegerVariable hairCategoryValue;

    [Tooltip("The category 'Dress' key and associated Flowchart variable.")]
    public string dressCategoryKey;
    [VariableProperty(typeof(IntegerVariable))]
    public IntegerVariable dressCategoryValue;

    [Tooltip("The category 'Ukrashenie' key and associated Flowchart variable.")]
    public string ukrashenieCategoryKey;
    [VariableProperty(typeof(IntegerVariable))]
    public IntegerVariable ukrashenieCategoryValue;

    [Tooltip("The category 'Accessorise' key and associated Flowchart variable.")]
    public string accessoriseCategoryKey;
    [VariableProperty(typeof(IntegerVariable))]
    public IntegerVariable accessoriseCategoryValue;

    public override void OnEnter()
    {
        // Запуск асинхронного процесса загрузки
        LoadAndParseCloudData();
    }

    private async void LoadAndParseCloudData()
    {
        // Инициализация Unity Services, если еще не инициализировано
        if (!UnityServices.State.Equals(ServicesInitializationState.Initialized))
        {
            try
            {
                await UnityServices.InitializeAsync();
                await SignInAnonymously();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to initialize Unity Services: {e.Message}");
                Continue();
                return;
            }
        }

        // Загрузка данных из облака
        try
        {
            var data = await CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> { cloudSection });
            
            if (data.TryGetValue(cloudSection, out var sectionData))
            {
                Debug.Log($"Raw data from cloud section '{cloudSection}': {sectionData}");
                
                // Парсинг JSON и извлечение индексов
                JObject jsonData = JObject.Parse(sectionData);
                JArray categories = (JArray)jsonData["categories"];

                // Извлечение и логирование индексов каждой категории
                foreach (JObject category in categories)
                {
                    string categoryName = category["categoryName"]?.ToString();
                    int index = category["index"]?.ToObject<int>() ?? -1;

                    if (categoryName == typeCategoryKey && typeCategoryValue != null)
                    {
                        typeCategoryValue.Value = index;
                        Debug.Log($"Value of '{typeCategoryKey}' in section '{cloudSection}' is: {index}");
                    }
                    else if (categoryName == makeupCategoryKey && makeupCategoryValue != null)
                    {
                        makeupCategoryValue.Value = index;
                        Debug.Log($"Value of '{makeupCategoryKey}' in section '{cloudSection}' is: {index}");
                    }
                    else if (categoryName == hairCategoryKey && hairCategoryValue != null)
                    {
                        hairCategoryValue.Value = index;
                        Debug.Log($"Value of '{hairCategoryKey}' in section '{cloudSection}' is: {index}");
                    }
                    else if (categoryName == dressCategoryKey && dressCategoryValue != null)
                    {
                        dressCategoryValue.Value = index;
                        Debug.Log($"Value of '{dressCategoryKey}' in section '{cloudSection}' is: {index}");
                    }
                    else if (categoryName == ukrashenieCategoryKey && ukrashenieCategoryValue != null)
                    {
                        ukrashenieCategoryValue.Value = index;
                        Debug.Log($"Value of '{ukrashenieCategoryKey}' in section '{cloudSection}' is: {index}");
                    }
                    else if (categoryName == accessoriseCategoryKey && accessoriseCategoryValue != null)
                    {
                        accessoriseCategoryValue.Value = index;
                        Debug.Log($"Value of '{accessoriseCategoryKey}' in section '{cloudSection}' is: {index}");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"Cloud section '{cloudSection}' not found.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading cloud variable: {e.Message}");
        }

        Continue();
    }

    private async System.Threading.Tasks.Task SignInAnonymously()
    {
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Sign-in successful");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to sign in: {e.Message}");
        }
    }

    public override string GetSummary()
    {
        return $"Load multiple category indexes from '{cloudSection}'";
    }
}
