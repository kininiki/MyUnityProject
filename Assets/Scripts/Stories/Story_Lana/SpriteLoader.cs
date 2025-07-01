using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using System.Collections;

public class ResourceManager : MonoBehaviour
{
    private Dictionary<string, Object> loadedAssets = new Dictionary<string, Object>();

    // Загрузка ресурса по адресу
    public void LoadAsset<T>(string address, System.Action<T> onLoaded) where T : Object
    {
        if (loadedAssets.ContainsKey(address))
        {
            // Если ресурс уже загружен, используем его
            onLoaded?.Invoke(loadedAssets[address] as T);
            return;
        }

        Addressables.LoadAssetAsync<T>(address).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                loadedAssets[address] = handle.Result; // Кэшируем загруженный ресурс
                onLoaded?.Invoke(handle.Result);
            }
            else
            {
                Debug.LogError($"Failed to load asset at {address}");
            }
        };
    }

    // Выгрузка конкретного ресурса
    public void UnloadAsset(string address)
    {
        if (loadedAssets.ContainsKey(address))
        {
            Addressables.Release(loadedAssets[address]);
            loadedAssets.Remove(address);
            Debug.Log($"Resource {address} unloaded.");
        }
    }

    // Выгрузка всех ресурсов
    public void UnloadAllAssets()
    {
        foreach (var asset in loadedAssets.Values)
        {
            Addressables.Release(asset);
        }
        loadedAssets.Clear();
        Debug.Log("All resources unloaded.");
    }

    // Освобождение неиспользуемых ресурсов из памяти
    public void CleanupUnusedAssets(System.Action onComplete = null)
    {
        StartCoroutine(PerformCleanup(onComplete));
    }

    private IEnumerator PerformCleanup(System.Action onComplete)
    {
        Debug.Log("Starting cleanup of unused assets...");

        // Асинхронно выгружаем неиспользуемые ресурсы
        var asyncOperation = Resources.UnloadUnusedAssets();

        // Ждем завершения без блокировки игры
        while (!asyncOperation.isDone)
        {
            yield return null; 
        }

        Debug.Log("Unused assets cleanup completed.");

        // Вызываем обратный вызов, если он передан
        onComplete?.Invoke();
    }

}

