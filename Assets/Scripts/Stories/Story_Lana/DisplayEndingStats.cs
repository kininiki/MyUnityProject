using UnityEngine;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

public class DisplayEndingStats : MonoBehaviour
{
    [Header("Text components for stats")]
    public TMP_Text trickText;
    public TMP_Text charmText;
    public TMP_Text lightText;
    public TMP_Text darknessText;
    public TMP_Text rubyText;

    private const string SectionKey = "LANA_UNLOCK_VARIABLES";
    private const string RubyKey = "PLAYER_RUBY";

    private async void Start()
    {
        await InitializeServices();
        var stats = await LoadStats();
        var ruby = await LoadRuby();
        UpdateUI(stats, ruby);
    }

    private async Task InitializeServices()
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            await UnityServices.InitializeAsync();
        }
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    private async Task<Dictionary<string, int>> LoadStats()
    {
        try
        {
            var data = await CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> { SectionKey });
            if (data.TryGetValue(SectionKey, out var json) && !string.IsNullOrEmpty(json))
            {
                return JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to load stats: {e.Message}");
        }
        return new Dictionary<string, int>();
    }

    private async Task<int> LoadRuby()
    {
        try
        {
            var data = await CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> { RubyKey });
            if (data.TryGetValue(RubyKey, out var json) && !string.IsNullOrEmpty(json))
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
                if (dict != null && dict.TryGetValue("ruby", out var ruby))
                {
                    return ruby;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to load ruby: {e.Message}");
        }
        return 0;
    }

    private void UpdateUI(Dictionary<string, int> stats, int ruby)
    {
        trickText.text = stats.TryGetValue("trick", out var trick) ? trick.ToString() : "0";
        charmText.text = stats.TryGetValue("charm", out var charm) ? charm.ToString() : "0";
        lightText.text = stats.TryGetValue("light", out var light) ? light.ToString() : "0";
        darknessText.text = stats.TryGetValue("darkness", out var darkness) ? darkness.ToString() : "0";
        if (rubyText != null)
        {
            rubyText.text = ruby.ToString();
        }
    }
}