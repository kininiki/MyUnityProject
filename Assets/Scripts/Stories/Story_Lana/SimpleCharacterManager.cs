using UnityEngine;
using UnityEngine.UI;
using Fungus;
using System;
using System.Collections;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine.Scripting;

public enum CharacterEmotion
{
    Нейтральное,
    ПоднятаяБровь,
    Задумчивость,
    Ухмылка,
    Покраснение,
    Злость,
    Грусть,
    ГрустьВзглядВСтор,
    НахмурБрови,
    ВСторНаправо,
    ВСторНаверхБровь,
    ОтведВзглядНаправо,
    ПоднятБрови,
    Улыбка,
    ШирУлыбка,
    ЗакрытГлаза,
    ЗакрГлазаНахмБрови,
    ЗакрГлазаУлыбка,
    УхмылкаСлёзы,
    Удивление,
    Испуг,
    НахмурСлёзы,
    ИспугСлёзы,
    УдивлПоднБрови,
    Прищур,
    НахмурВСтор,
    ВзглядВСтор
}

[Serializable]
public class CharacterType
{
    public AssetReferenceSprite baseSprite; // Базовый спрайт персонажа
    public AssetReferenceSprite[] emotionSprites; // Спрайты эмоций
}

[Preserve]
public class SimpleCharacterManager : MonoBehaviour
{
    [SerializeField]
    private RawImage[] characterPositions = new RawImage[8]; // Слоты для отображения персонажей на экране

    [SerializeField]
    private RawImage[] emotionLayerImages = new RawImage[8]; // Слои для эмоций (8 позиций)

    [SerializeField]
    private RawImage[] makeupLayerImages = new RawImage[8]; // Слои для макияжа (8 позиций)

    [SerializeField]
    private RawImage[] hairLayerImages = new RawImage[8]; // Слои для волос (8 позиций)

    [SerializeField]
    private RawImage[] dressLayerImages = new RawImage[8]; // Слои для одежды (8 позиций)

    [SerializeField]
    private RawImage[] ukrashenieLayerImages = new RawImage[8]; // Слои для украшений (8 позиций)

    [SerializeField]
    private RawImage[] accessoriseLayerImages = new RawImage[8]; // Слои для аксессуаров (8 позиций)

    [SerializeField]
    private CharacterType[] characterTypes; // Типы персонажей

    [SerializeField]
    private AssetReferenceSprite[] hairSprites; // Спрайты для волос

    [SerializeField]
    private AssetReferenceSprite[] makeupSprites; // Спрайты для макияжа

    [SerializeField]
    public AssetReferenceSprite[] dressSprites; // Спрайты для одежды

    [SerializeField]
    private AssetReferenceSprite[] ukrashenieSprites; // Спрайты для украшений

    [SerializeField]
    private AssetReferenceSprite[] accessoriseSprites; // Спрайты для аксессуаров

    // Кэш для загруженных спрайтов
    private Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    public RawImage GetCharacterPosition(int index) => index >= 0 && index < characterPositions.Length ? characterPositions[index] : null;
    public RawImage GetEmotionLayer(int index) => index >= 0 && index < emotionLayerImages.Length ? emotionLayerImages[index] : null;
    public RawImage GetMakeupLayer(int index) => index >= 0 && index < makeupLayerImages.Length ? makeupLayerImages[index] : null;
    public RawImage GetHairLayer(int index) => index >= 0 && index < hairLayerImages.Length ? hairLayerImages[index] : null;
    public RawImage GetDressLayer(int index) => index >= 0 && index < dressLayerImages.Length ? dressLayerImages[index] : null;
    public RawImage GetUkrashenieLayer(int index) => index >= 0 && index < ukrashenieLayerImages.Length ? ukrashenieLayerImages[index] : null;
    public RawImage GetAccessoriseLayer(int index) => index >= 0 && index < accessoriseLayerImages.Length ? accessoriseLayerImages[index] : null;

    private IEnumerator LoadSpriteAsync(AssetReferenceSprite assetReference, Action<Sprite> onLoaded)
    {
        if (assetReference == null)
        {
            Debug.LogError("AssetReference is null!");
            yield break;
        }

        // Проверяем, есть ли спрайт в кэше
        if (spriteCache.TryGetValue(assetReference.AssetGUID, out Sprite cachedSprite))
        {
            onLoaded?.Invoke(cachedSprite);
            yield break;
        }

        // Загружаем спрайт асинхронно
        var handle = assetReference.LoadAssetAsync<Sprite>();
        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            // Сохраняем спрайт в кэше
            spriteCache[assetReference.AssetGUID] = handle.Result;
            onLoaded?.Invoke(handle.Result);
        }
        else
        {
            Debug.LogError($"Failed to load sprite: {assetReference.AssetGUID}");
            onLoaded?.Invoke(null);
        }
    }

    // Метод для очистки кэша
    public void ClearSpriteCache()
    {
        foreach (var sprite in spriteCache.Values)
        {
            Addressables.Release(sprite);
        }
        spriteCache.Clear();
        Debug.Log("Кэш спрайтов очищен.");
    }

    // Очистка кэша при уничтожении объекта
    private void OnDestroy()
    {
        ClearSpriteCache();
    }




    private Dictionary<int, AsyncOperationHandle<Sprite>> dressHandles = new Dictionary<int, AsyncOperationHandle<Sprite>>();
    public void ForceUpdateDress(int positionIndex, int dressIndex)
    {
        if (dressIndex < 0 || dressIndex >= dressSprites.Length)
        {
            Debug.LogError($"Invalid dress index: {dressIndex}");
            return;
        }

        // Освобождаем предыдущий хендл
        if (dressHandles.ContainsKey(positionIndex))
        {
            Addressables.Release(dressHandles[positionIndex]);
            dressHandles.Remove(positionIndex);
        }

        // Загружаем новый спрайт
        var handle = Addressables.LoadAssetAsync<Sprite>(dressSprites[dressIndex]);
        dressHandles[positionIndex] = handle;

        handle.Completed += operation =>
        {
            if (operation.Status == AsyncOperationStatus.Succeeded)
            {
                dressLayerImages[positionIndex].texture = operation.Result.texture;
                Debug.Log($"Dress updated: {dressIndex} at {positionIndex}");
            }
        };
    }


    public void SetCharacterSprite(int positionIndex, int characterTypeIndex, CharacterEmotion emotion, int hairIndex, int makeupIndex, int dressIndex, int ukrashenieIndex, int accessoriseIndex)
    {
        if (positionIndex < 0 || positionIndex >= characterPositions.Length || characterPositions[positionIndex] == null)
        {
            Debug.LogError("Неверный индекс позиции или RawImage не назначен.");
            return;
        }

        if (characterTypeIndex < 0 || characterTypeIndex >= characterTypes.Length)
        {
            Debug.LogError("Неверный индекс типа персонажа.");
            return;
        }

        // Устанавливаем альфа-канал для всех слоев в 1.0 перед установкой спрайтов
        RawImage[] layers = {
            characterPositions[positionIndex],
            emotionLayerImages[positionIndex],
            makeupLayerImages[positionIndex],
            hairLayerImages[positionIndex],
            dressLayerImages[positionIndex],
            ukrashenieLayerImages[positionIndex],
            accessoriseLayerImages[positionIndex]
        };

        foreach (RawImage layer in layers)
        {
            if (layer != null)
            {
                Color color = layer.color;
                color.a = 1.0f;
                layer.color = color;
            }
        }

        var characterType = characterTypes[characterTypeIndex];
        if (characterType == null)
        {
            Debug.LogError("Тип персонажа не назначен.");
            return;
        }

        // Загружаем базовый спрайт
        StartCoroutine(LoadSpriteAsync(characterType.baseSprite, (sprite) =>
        {
            if (sprite != null)
            {
                characterPositions[positionIndex].texture = sprite.texture;
            }
        }));

        // Загружаем спрайт эмоции
        if (emotion >= 0 && (int)emotion < characterType.emotionSprites.Length)
        {
            StartCoroutine(LoadSpriteAsync(characterType.emotionSprites[(int)emotion], (sprite) =>
            {
                if (sprite != null && emotionLayerImages != null && positionIndex < emotionLayerImages.Length)
                {
                    emotionLayerImages[positionIndex].texture = sprite.texture;
                }
            }));
        }

        // Загружаем спрайт для макияжа
        if (makeupIndex >= 0 && makeupIndex < makeupSprites.Length)
        {
            StartCoroutine(LoadSpriteAsync(makeupSprites[makeupIndex], (sprite) =>
            {
                if (sprite != null && makeupLayerImages != null && positionIndex < makeupLayerImages.Length)
                {
                    makeupLayerImages[positionIndex].texture = sprite.texture;
                }
            }));
        }

        // Загружаем спрайт для волос
        if (hairIndex >= 0 && hairIndex < hairSprites.Length)
        {
            StartCoroutine(LoadSpriteAsync(hairSprites[hairIndex], (sprite) =>
            {
                if (sprite != null && hairLayerImages != null && positionIndex < hairLayerImages.Length)
                {
                    hairLayerImages[positionIndex].texture = sprite.texture;
                }
            }));
        }

        // Загружаем спрайт для одежды
        if (dressIndex >= 0 && dressIndex < dressSprites.Length)
        {
            StartCoroutine(LoadSpriteAsync(dressSprites[dressIndex], (sprite) =>
            {
                if (sprite != null && dressLayerImages != null && positionIndex < dressLayerImages.Length)
                {
                    dressLayerImages[positionIndex].texture = sprite.texture;
                }
            }));
        }

        // Загружаем спрайт для украшений
        if (ukrashenieIndex >= 0 && ukrashenieIndex < ukrashenieSprites.Length)
        {
            StartCoroutine(LoadSpriteAsync(ukrashenieSprites[ukrashenieIndex], (sprite) =>
            {
                if (sprite != null && ukrashenieLayerImages != null && positionIndex < ukrashenieLayerImages.Length)
                {
                    ukrashenieLayerImages[positionIndex].texture = sprite.texture;
                }
            }));
        }

        // Загружаем спрайт для аксессуаров
        if (accessoriseIndex >= 0 && accessoriseIndex < accessoriseSprites.Length)
        {
            StartCoroutine(LoadSpriteAsync(accessoriseSprites[accessoriseIndex], (sprite) =>
            {
                if (sprite != null && accessoriseLayerImages != null && positionIndex < accessoriseLayerImages.Length)
                {
                    accessoriseLayerImages[positionIndex].texture = sprite.texture;
                }
            }));
        }

        Debug.Log($"Отображен персонаж типа {characterTypeIndex} с эмоцией {emotion}, макияжем, волосами, одеждой, украшениями и аксессуарами на позиции {positionIndex}.");
    }
}





[CommandInfo("Character", 
    "Set Character Sprite", 
    "Sets the character sprite at a specified position with a specified emotion, hair, makeup, dress, ukrashenie and accessorise")]
[Preserve]
public class SetCharacterSpriteCommand : Command
{
    [Tooltip("Position on the screen where the character should appear")]
    [SerializeField]
    private int positionIndex;

    [Tooltip("Character emotion")]
    [SerializeField]
    private CharacterEmotion emotion;

    private Flowchart flowchart;

    public override void OnEnter()
    {
        StartCoroutine(SetCharacterSpriteWithDelay());
    }

    private IEnumerator SetCharacterSpriteWithDelay()
    {
        yield return null;

        flowchart = GetFlowchart();

        if (flowchart == null)
        {
            Debug.LogError("Flowchart не найден!");
            Continue();
            yield break;
        }

        IntegerVariable typeVariable = flowchart.GetVariable<IntegerVariable>("Type");
        IntegerVariable hairVariable = flowchart.GetVariable<IntegerVariable>("Hair");
        IntegerVariable makeupVariable = flowchart.GetVariable<IntegerVariable>("Makeup");
        IntegerVariable dressVariable = flowchart.GetVariable<IntegerVariable>("Dress");
        IntegerVariable ukrashenieVariable = flowchart.GetVariable<IntegerVariable>("Ukrashenie");
        IntegerVariable accessoriseVariable = flowchart.GetVariable<IntegerVariable>("Accessorise");

        if (typeVariable == null || hairVariable == null || makeupVariable == null || dressVariable == null || ukrashenieVariable == null || accessoriseVariable == null)
        {
            Debug.LogError("Одна или несколько переменных не найдены в Flowchart!");
            Continue();
            yield break;
        }

        int characterTypeIndex = typeVariable.Value;
        int hairIndex = hairVariable.Value;
        int makeupIndex = makeupVariable.Value;
        int dressIndex = dressVariable.Value;
        int ukrashenieIndex = ukrashenieVariable.Value;
        int accessoriseIndex = accessoriseVariable.Value;

        SimpleCharacterManager characterManager = FindObjectOfType<SimpleCharacterManager>();

        if (characterManager != null)
        {
            characterManager.SetCharacterSprite(positionIndex, characterTypeIndex, emotion, hairIndex, makeupIndex, dressIndex, ukrashenieIndex, accessoriseIndex);
            Debug.Log($"Character sprite set at position {positionIndex}, type {characterTypeIndex}, emotion {emotion}, hair {hairIndex}, makeup {makeupIndex}, dress {dressIndex}, ukrashenie {ukrashenieIndex}, accessorise {accessoriseIndex}");
        }
        else
        {
            Debug.LogError("SimpleCharacterManager не найден!");
        }

        Continue();
    }

    public override string GetSummary()
    {
        return $"Set character sprite at position {positionIndex} with emotion {emotion}, hair, makeup, dress, ukrashenie and accessorise.";
    }

    public override Color GetButtonColor()
    {
        return new Color32(255, 223, 186, 255);
    }
}



[CommandInfo("Character", 
    "Fade Out Character", 
    "Fades out the character at a specified position over a set duration, including all layers.")]
public class FadeOutCharacterCommand : Command
{
    [Tooltip("Position on the screen where the character is displayed")]
    [SerializeField]
    private int positionIndex;

    [Tooltip("Duration of the fade-out effect")]
    [SerializeField]
    private float fadeDuration = 0.03f;

    public override void OnEnter()
    {
        // Получаем ссылку на SimpleCharacterManager
        SimpleCharacterManager characterManager = FindObjectOfType<SimpleCharacterManager>();

        if (characterManager != null)
        {
            // Начинаем корутину для плавного исчезновения
            characterManager.StartCoroutine(FadeOutCharacter(positionIndex, fadeDuration));
        }
        else
        {
            Debug.LogError("SimpleCharacterManager не найден!");
        }

        // Завершаем команду
        Continue();
    }

    private IEnumerator FadeOutCharacter(int positionIndex, float duration)
    {
        SimpleCharacterManager characterManager = FindObjectOfType<SimpleCharacterManager>();

        if (characterManager == null)
        {
            Debug.LogError("SimpleCharacterManager не найден!");
            yield break;
        }

        RawImage[] layers = {
            characterManager.GetCharacterPosition(positionIndex),
            characterManager.GetEmotionLayer(positionIndex),
            characterManager.GetMakeupLayer(positionIndex),
            characterManager.GetHairLayer(positionIndex),
            characterManager.GetDressLayer(positionIndex),
            characterManager.GetUkrashenieLayer(positionIndex),
            characterManager.GetAccessoriseLayer(positionIndex)
        };

        float startAlpha = 1.0f;
        float endAlpha = 0.0f;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);

            foreach (RawImage layer in layers)
            {
                if (layer != null)
                {
                    Color color = layer.color;
                    color.a = alpha;
                    layer.color = color;
                }
            }

            yield return null;
        }

        // Устанавливаем альфа-канал на 0 для полного исчезновения всех слоев
        foreach (RawImage layer in layers)
        {
            if (layer != null)
            {
                Color color = layer.color;
                color.a = endAlpha;
                layer.color = color;
            }
        }
    }


    public override string GetSummary()
    {
        return $"Fades out character at position {positionIndex} over {fadeDuration} seconds.";
    }

    public override Color GetButtonColor()
    {
        return new Color32(186, 223, 255, 255);
    }
}





// [CommandInfo("Flowchart", 
//     "Preload Flowchart Variables", 
//     "Ensures that the Flowchart and its variables are loaded before proceeding.")]
// public class PreloadFlowchartVariablesCommand : Command
// {
//     [Tooltip("Names of the variables to preload")]
//     [SerializeField]
//     private string[] variableNames = { "Type", "Hair", "Makeup", "Dress", "Ukrashenie", "Accessorise" };

//     public override void OnEnter()
//     {
//         // Запускаем корутину для подгрузки переменных
//         StartCoroutine(PreloadVariables());
//     }

//     private IEnumerator PreloadVariables()
//     {
//         Flowchart flowchart = FindObjectOfType<Flowchart>();

//         if (flowchart == null)
//         {
//             Debug.LogError("Flowchart не найден!");
//             Continue();
//             yield break;
//         }

//         // Логируем все переменные в Flowchart
//         foreach (var variable in flowchart.Variables)
//         {
//             Debug.Log($"Найдена переменная: {variable.Key} ");
//         }

//         int attempts = 10; // Количество попыток
//         float delay = 0.1f; // Задержка между попытками

//         bool allVariablesLoaded = false;

//         for (int i = 0; i < attempts; i++)
//         {
//             allVariablesLoaded = true;

//             // Проверяем каждую переменную
//             foreach (string variableName in variableNames)
//             {
//                 IntegerVariable variable = flowchart.GetVariable<IntegerVariable>(variableName);
//                 if (variable == null)
//                 {
//                     allVariablesLoaded = false;
//                     Debug.LogWarning($"Переменная '{variableName}' не найдена.");
//                     break;
//                 }
//             }

//             if (allVariablesLoaded)
//             {
//                 break; // Все переменные найдены, выходим из цикла
//             }

//             Debug.LogWarning($"Попытка {i + 1}: не все переменные загружены. Повторная попытка через {delay} секунд...");
//             yield return new WaitForSeconds(delay); // Ждем перед следующей попыткой
//         }

//         if (!allVariablesLoaded)
//         {
//             Debug.LogError("Не удалось загрузить все переменные!");
//         }
//         else
//         {
//             Debug.Log("Все переменные успешно загружены.");
//         }

//         // Завершаем команду
//         Continue();
//     }

//     public override string GetSummary()
//     {
//         return "Preloads Flowchart variables to ensure they are available.";
//     }

//     public override Color GetButtonColor()
//     {
//         return new Color32(200, 200, 255, 255);
//     }
// }



[CommandInfo("Flowchart", 
    "Preload Flowchart Variables", 
    "Ensures that the Flowchart and its variables are loaded before proceeding.")]
public class PreloadFlowchartVariablesCommand : Command
{
    [Tooltip("Names of the variables to preload")]
    [SerializeField]
    private string[] variableNames = { "Type", "Hair", "Makeup", "Dress", "Ukrashenie", "Accessorise" };

    private const string UNLOCK_KEY = "LANA_WARDROBE_DATA"; // Ключ для данных в облаке

    public override void OnEnter()
    {
        // Запускаем корутину для подгрузки переменных
        StartCoroutine(PreloadVariables());
    }

    private IEnumerator PreloadVariables()
    {
        // Инициализируем Unity Services
        yield return InitializeUnityServices();

        // Загружаем данные из облака
        yield return LoadUnlockVariables();

        // Получаем Flowchart
        Flowchart flowchart = GetFlowchart();

        if (flowchart == null)
        {
            Debug.LogError("Flowchart не найден!");
            Continue();
            yield break;
        }

        // Устанавливаем значения переменных в Flowchart
        SetFlowchartVariables(flowchart);

        // Завершаем команду
        Continue();
    }

    private IEnumerator InitializeUnityServices()
    {
        // Инициализация Unity Services
        var initialization = UnityServices.InitializeAsync();
        yield return new WaitUntil(() => initialization.IsCompleted);


        // Анонимный вход
        var signIn = AuthenticationService.Instance.SignInAnonymouslyAsync();
        yield return new WaitUntil(() => signIn.IsCompleted);

        Debug.Log("Unity Services инициализированы успешно.");
    }

    private IEnumerator LoadUnlockVariables()
    {
        // Загрузка данных из облака
        var loadOperation = CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> { UNLOCK_KEY });
        yield return new WaitUntil(() => loadOperation.IsCompleted);


        var data = loadOperation.Result;

        if (data.TryGetValue(UNLOCK_KEY, out var savedData))
        {
            Debug.Log($"Данные из облака: {savedData}");

            try
            {
                // Десериализуем JSON
                var wardrobeData = JsonConvert.DeserializeObject<WardrobeData>(savedData);

                if (wardrobeData != null)
                {
                    Debug.Log("Данные успешно загружены и десериализованы.");
                    // Сохраняем данные для использования
                    PlayerPrefs.SetString(UNLOCK_KEY, savedData);
                    PlayerPrefs.Save();
                }
                else
                {
                    Debug.LogError("Ошибка десериализации данных из облака.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Ошибка парсинга JSON: {ex.Message}");
            }
        }
        else
        {
            Debug.Log("Данные не найдены в облаке.");
        }
    }

    private void SetFlowchartVariables(Flowchart flowchart)
    {
        // Получаем сохраненные данные из PlayerPrefs
        string savedData = PlayerPrefs.GetString(UNLOCK_KEY, string.Empty);

        if (string.IsNullOrEmpty(savedData))
        {
            Debug.LogError("Нет данных для установки переменных.");
            return;
        }

        try
        {
            // Десериализуем JSON
            var wardrobeData = JsonConvert.DeserializeObject<WardrobeData>(savedData);

            if (wardrobeData == null)
            {
                Debug.LogError("Ошибка десериализации данных.");
                return;
            }

            // Устанавливаем значения переменных в Flowchart
            foreach (var category in wardrobeData.categories)
            {
                IntegerVariable variable = flowchart.GetVariable<IntegerVariable>(category.categoryName);
                if (variable != null)
                {
                    variable.Value = category.index;
                    Debug.Log($"Установлено значение переменной {category.categoryName} = {category.index}");
                }
                else
                {
                    Debug.LogWarning($"Переменная {category.categoryName} не найдена в Flowchart.");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Ошибка при установке переменных: {ex.Message}");
        }
    }

    [System.Serializable]
    private class WardrobeCategory
    {
        public string categoryName;
        public int index;
    }

    [System.Serializable]
    private class WardrobeData
    {
        public WardrobeCategory[] categories;
    }

    public override string GetSummary()
    {
        return "Preloads Flowchart variables to ensure they are available.";
    }

    public override Color GetButtonColor()
    {
        return new Color32(200, 200, 255, 255);
    }
}