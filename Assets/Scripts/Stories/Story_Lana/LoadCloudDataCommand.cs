using UnityEngine;
using Fungus;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[CommandInfo("Cloud", "Load Cloud Variable", "Loads a cloud variable from Unity Cloud Save, logs it, and stores it in a Flowchart variable.")]
public class LoadCloudVariableCommand : Command
{
    [Tooltip("The cloud section name, for example 'LANA_UNLOCK_VARIABLES'.")]
    public string cloudSection;

    [Tooltip("The key of the cloud variable to retrieve, for example 'w1'.")]
    public string cloudKey;

    [Tooltip("The Flowchart integer variable where the cloud value will be stored.")]
    [VariableProperty(typeof(IntegerVariable))]
    public IntegerVariable cloudValueVariable;

    public override void OnEnter()
    {
        // Start the async loading process
        LoadAndStoreCloudVariable();
    }

    private async void LoadAndStoreCloudVariable()
    {
        // Initialize Unity Services if not already initialized
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
                Continue(); // Move to the next Fungus command even if there's an error
                return;
            }
        }

        // Load the data from the cloud
        try
        {
            var data = await CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> { cloudSection });
            
            if (data.TryGetValue(cloudSection, out var sectionData))
            {
                Debug.Log($"Raw data from cloud section '{cloudSection}': {sectionData}");

                // Parse the JSON-like structure to get the desired key
                if (sectionData.Contains(cloudKey))
                {
                    // Assume data is in JSON format like { "w1": "1" }
                    string keyData = ParseKeyFromJson(sectionData, cloudKey);
                    Debug.Log($"Value of '{cloudKey}' in section '{cloudSection}' is: {keyData}");

                    // Convert and store the value in the Fungus variable
                    if (int.TryParse(keyData, out int intValue))
                    {
                        cloudValueVariable.Value = intValue;
                    }
                    else
                    {
                        Debug.LogWarning($"Could not parse '{keyData}' as integer for key '{cloudKey}'");
                    }
                }
                else
                {
                    Debug.LogWarning($"Key '{cloudKey}' not found in cloud section '{cloudSection}'");
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

        Continue(); // Continue to the next command in Fungus
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

    private string ParseKeyFromJson(string jsonData, string key)
    {
        // Simplistic parsing, assumes format is { "key": "value" }
        jsonData = jsonData.Trim('{', '}');
        var parts = jsonData.Split(',');
        foreach (var part in parts)
        {
            var keyValue = part.Split(':');
            if (keyValue.Length == 2)
            {
                var parsedKey = keyValue[0].Trim('"').Trim();
                var value = keyValue[1].Trim('"').Trim();
                if (parsedKey == key)
                {
                    return value;
                }
            }
        }
        return null;
    }

    public override string GetSummary()
    {
        return $"Load cloud variable '{cloudKey}' from section '{cloudSection}'";
    }
}
