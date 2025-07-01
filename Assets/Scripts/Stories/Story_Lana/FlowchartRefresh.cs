using UnityEngine;
using Fungus;
using System.Collections;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Video;

[CommandInfo("Flowchart", "Flowchart Refresh", "Очищает все ресурсы Addressables во Flowchart без трогания переменных.")]
public class FlowchartRefresh : Command
{
    [Tooltip("Имя блока, с которого нужно запустить Flowchart после очистки.")]
    public string restartBlockName = "Check"; // Тут сразу можно задать блок по умолчанию

    public override void OnEnter()
    {
        Flowchart flowchart = GetFlowchart();
        if (flowchart == null)
        {
            Debug.LogError("Flowchart не найден!");
            Continue();
            return;
        }

        Debug.Log("Flowchart очищается...");

        StartCoroutine(RefreshFlowchart(flowchart));
    }

    private IEnumerator RefreshFlowchart(Flowchart flowchart)
    {
        // 1. Останавливаем все блоки
        StopAllBlocks(flowchart);

        // 2. Очищаем ресурсы Addressables
        yield return ClearAddressables();

        Debug.Log("Flowchart очищен.");

        yield return null; // Ждём кадр, чтобы движок успел освободить ресурсы

        // 3. Перезапуск блока с restartBlockName
        Block restartBlock = flowchart.FindBlock(restartBlockName);
        if (restartBlock != null)
        {
            Debug.Log($"Перезапуск блока {restartBlockName}");
            flowchart.ExecuteBlock(restartBlock);
        }
        else
        {
            Debug.LogError($"Блок {restartBlockName} не найден!");
        }
    }

    private void StopAllBlocks(Flowchart flowchart)
    {
        flowchart.StopAllCoroutines();
        foreach (Block block in flowchart.GetComponents<Block>())
        {
            if (block.IsExecuting())
            {
                block.Stop();
            }
        }
    }

    private IEnumerator ClearAddressables()
    {
        // Очищаем видео
        foreach (VideoPlayer player in FindObjectsOfType<VideoPlayer>())
        {
            if (player.clip != null)
            {
                Debug.Log($"Видео {player.clip.name} освобождено.");
                Addressables.Release(player.clip);
                player.Stop();
                player.targetTexture?.Release();
                player.targetTexture = null;
                yield return null;
            }
        }

        // Очищаем музыку
        foreach (PlayMusicCommand music in FindObjectsOfType<PlayMusicCommand>())
        {
            if (music.musicClipReference != null && music.musicClipReference.OperationHandle.IsValid())
            {
                Debug.Log($"Музыка {music.musicClipReference.AssetGUID} освобождена.");
                Addressables.Release(music.musicClipReference.OperationHandle);
                music.audioSource?.Stop();
                yield return null;
            }
        }

        Debug.Log("Все ресурсы Addressables очищены.");
    }

    public override string GetSummary()
    {
        return $"Очистка ресурсов и перезапуск блока '{restartBlockName}'";
    }

    public override Color GetButtonColor()
    {
        return new Color32(180, 120, 255, 255);
    }
}
