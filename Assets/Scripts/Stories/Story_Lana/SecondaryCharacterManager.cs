using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using Fungus;
using System.Collections;
using UnityEditor;
using System.Linq;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[Serializable]
public class SecondaryCharacter
{
    public string characterName; // Уникальное имя персонажа
    public List<AssetReferenceSprite> baseSprites; // Список базовых спрайтов персонажа (теперь через Addressables)

    [Serializable]
    public class EmotionSprite
    {
        public string emotionKey;  // Ключ эмоции (название эмоции)
        public AssetReferenceSprite emotionSprite; // Спрайт эмоции (теперь через Addressables)
    }

    public List<EmotionSprite> emotionSprites; // Список эмоций персонажа

    [Serializable]
    public class DressSprite
    {
        public string dressKey;  // Ключ наряда (название наряда)
        public AssetReferenceSprite dressSprite; // Спрайт наряда (теперь через Addressables)
    }

    public List<DressSprite> dressSprites; // Список нарядов персонажа
}


public class SecondaryCharacterManager : MonoBehaviour
{
    [SerializeField]
    public RawImage[] characterPositions = new RawImage[8]; // 8 позиций для отображения персонажей

    [SerializeField]
    public RawImage[] emotionLayers = new RawImage[8]; // 8 слоев для отображения эмоций

    [SerializeField]
    public RawImage[] dressLayers = new RawImage[8]; // 8 слоев для отображения нарядов

    [SerializeField]
    private List<SecondaryCharacter> characters; // Список всех второстепенных персонажей

    // Кэш для загруженных спрайтов
    private Dictionary<AssetReferenceSprite, Sprite> spriteCache = new Dictionary<AssetReferenceSprite, Sprite>();

    // Метод для получения списка имен персонажей
    public string[] GetCharacterNames()
    {
        return characters.Select(c => c.characterName).ToArray();
    }

    // Метод для получения персонажа по имени
    public SecondaryCharacter GetCharacterByName(string name)
    {
        return characters.Find(c => c.characterName == name);
    }

    // Метод для отображения персонажа на экране
    public void SetCharacterSprite(string characterName, int positionIndex, int baseSpriteIndex, string emotionKey, string dressKey)
    {
        if (positionIndex < 0 || positionIndex >= characterPositions.Length || characterPositions[positionIndex] == null)
        {
            Debug.LogError("Неверный индекс позиции или RawImage не назначен.");
            return;
        }

        // Находим персонажа по имени
        SecondaryCharacter character = characters.Find(c => c.characterName == characterName);
        if (character == null)
        {
            Debug.LogError($"Персонаж с именем '{characterName}' не найден.");
            return;
        }

        // Проверяем, что индекс базового спрайта в пределах доступных спрайтов
        if (baseSpriteIndex < 0 || baseSpriteIndex >= character.baseSprites.Count)
        {
            Debug.LogError($"Индекс базового спрайта {baseSpriteIndex} выходит за пределы доступных спрайтов для персонажа '{characterName}'.");
            return;
        }

        // Загружаем и устанавливаем базовый спрайт
        LoadAndSetSprite(character.baseSprites[baseSpriteIndex], characterPositions[positionIndex], $"Отображен персонаж '{characterName}' на позиции {positionIndex} с базовым спрайтом {baseSpriteIndex}.");

        // Загружаем и устанавливаем спрайт эмоции, если задан ключ эмоции
        if (!string.IsNullOrEmpty(emotionKey))
        {
            var emotionSprite = character.emotionSprites.Find(e => e.emotionKey == emotionKey)?.emotionSprite;
            if (emotionSprite != null)
            {
                LoadAndSetSprite(emotionSprite, emotionLayers[positionIndex], $"Эмоция '{emotionKey}' для персонажа '{characterName}' отображена на позиции {positionIndex}.");
            }
            else
            {
                Debug.LogWarning($"Эмоция '{emotionKey}' не найдена для персонажа '{characterName}'.");
            }
        }

        // Загружаем и устанавливаем спрайт наряда, если задан ключ наряда
        if (!string.IsNullOrEmpty(dressKey))
        {
            var dressSprite = character.dressSprites.Find(d => d.dressKey == dressKey)?.dressSprite;
            if (dressSprite != null)
            {
                LoadAndSetSprite(dressSprite, dressLayers[positionIndex], $"Наряд '{dressKey}' для персонажа '{characterName}' отображен на позиции {positionIndex}.");
            }
            else
            {
                Debug.LogWarning($"Наряд '{dressKey}' не найден для персонажа '{characterName}'.");
            }
        }
    }

    // Вспомогательный метод для загрузки и установки спрайта
    private void LoadAndSetSprite(AssetReferenceSprite assetReference, RawImage targetImage, string logMessage)
    {
        if (assetReference == null || targetImage == null)
        {
            Debug.LogError("AssetReference или RawImage не назначены.");
            return;
        }

        // Проверяем, есть ли спрайт в кэше
        if (spriteCache.TryGetValue(assetReference, out Sprite cachedSprite))
        {
            targetImage.texture = cachedSprite.texture;
            Debug.Log(logMessage);
            return;
        }

        // Загружаем спрайт асинхронно
        assetReference.LoadAssetAsync<Sprite>().Completed += (handle) =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                // Сохраняем спрайт в кэше
                spriteCache[assetReference] = handle.Result;
                targetImage.texture = handle.Result.texture;
                Debug.Log(logMessage);
            }
            else
            {
                Debug.LogError($"Не удалось загрузить спрайт: {assetReference.AssetGUID}");
            }
        };
    }

    // Метод для очистки кэша
    private void ClearSpriteCache()
    {
        foreach (var assetReference in spriteCache.Keys)
        {
            Addressables.Release(assetReference);
        }
        spriteCache.Clear();
    }

    // Очистка кэша при уничтожении объекта
    private void OnDestroy()
    {
        ClearSpriteCache();
    }
}







[CommandInfo("Character",
    "Set Character Sprite 2",
    "Sets the character sprite at a specified position with a specified emotion, base sprite, and dress from the secondary character manager")]
public class SetCharacterSpriteCommand2 : Command
{
    [Tooltip("Name of the character to display")]
    [SerializeField] private string characterName;

    [Tooltip("Position on the screen where the character should appear")]
    [SerializeField] private int positionIndex;

    [Tooltip("Index of the base sprite to display")]
    [SerializeField] private int baseSpriteIndex;

    [Tooltip("Emotion to display (key of emotion)")]
    [SerializeField] private string emotionKey;

    [Tooltip("Dress to display (key of dress)")]
    [SerializeField] private string dressKey;

    public override void OnEnter()
    {
        // Получаем ссылку на SecondaryCharacterManager
        SecondaryCharacterManager characterManager = FindObjectOfType<SecondaryCharacterManager>();

        if (characterManager != null)
        {
            // Восстанавливаем альфа-каналы всех слоев для отображения
            characterManager.characterPositions[positionIndex].color = new Color(1, 1, 1, 1);
            characterManager.emotionLayers[positionIndex].color = new Color(1, 1, 1, 1);
            characterManager.dressLayers[positionIndex].color = new Color(1, 1, 1, 1);

            // Устанавливаем спрайт персонажа с базовым слоем, эмоцией и нарядом
            characterManager.SetCharacterSprite(characterName, positionIndex, baseSpriteIndex, emotionKey, dressKey);
            Debug.Log($"Character '{characterName}' sprite set at position {positionIndex} with base sprite {baseSpriteIndex}, emotion '{emotionKey}', and dress '{dressKey}'.");
        }
        else
        {
            Debug.LogError("SecondaryCharacterManager не найден!");
        }

        // Завершаем команду
        Continue();
    }

    public override string GetSummary()
    {
        return $"Set character '{characterName}' at position {positionIndex} with base sprite {baseSpriteIndex}, emotion '{emotionKey}', and dress '{dressKey}'";
    }

    public override Color GetButtonColor()
    {
        return new Color32(200, 255, 200, 255);
    }
}











[CommandInfo("Character",
    "Fade Out Character 2",
    "Fades out the character at the specified position by gradually setting the alpha channels of the base, emotion, and dress layers to 0 over a specified duration.")]
public class FadeOutCharacterCommand2 : Command
{
    [Tooltip("Position on the screen where the character should fade out")]
    [SerializeField] private int positionIndex;

    [Tooltip("Duration of the fade out effect in seconds")]
    [SerializeField] private float duration = 1f;

    public override void OnEnter()
    {
        // Получаем ссылку на SecondaryCharacterManager
        SecondaryCharacterManager characterManager = FindObjectOfType<SecondaryCharacterManager>();

        if (characterManager != null)
        {
            // Запускаем корутину для плавного исчезновения персонажа
            characterManager.StartCoroutine(FadeOutCharacter(positionIndex, duration));
            Debug.Log($"Fading out character at position {positionIndex} over {duration} seconds.");
        }
        else
        {
            Debug.LogError("SecondaryCharacterManager не найден!");
        }

        // Завершаем команду
        Continue();
    }

    private IEnumerator FadeOutCharacter(int positionIndex, float duration)
    {
        SecondaryCharacterManager characterManager = FindObjectOfType<SecondaryCharacterManager>();
        if (characterManager == null) yield break;

        // Получаем RawImage для каждого слоя: базовый, эмоции и наряд
        RawImage baseLayer = characterManager.characterPositions[positionIndex];
        RawImage emotionLayer = characterManager.emotionLayers[positionIndex];
        RawImage dressLayer = characterManager.dressLayers[positionIndex];

        float elapsedTime = 0f;

        // Начальные альфа значения
        Color baseColor = baseLayer.color;
        Color emotionColor = emotionLayer.color;
        Color dressColor = dressLayer.color;

        while (elapsedTime < duration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);
            
            // Обновляем альфа для всех слоев
            baseLayer.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            emotionLayer.color = new Color(emotionColor.r, emotionColor.g, emotionColor.b, alpha);
            dressLayer.color = new Color(dressColor.r, dressColor.g, dressColor.b, alpha);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Устанавливаем альфа в 0, чтобы полностью исчезли
        baseLayer.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
        emotionLayer.color = new Color(emotionColor.r, emotionColor.g, emotionColor.b, 0f);
        dressLayer.color = new Color(dressColor.r, dressColor.g, dressColor.b, 0f);


    }

    public override string GetSummary()
    {
        return $"Fade out character at position {positionIndex} over {duration} seconds.";
    }

    public override Color GetButtonColor()
    {
        return new Color32(255, 150, 150, 255);
    }
}
