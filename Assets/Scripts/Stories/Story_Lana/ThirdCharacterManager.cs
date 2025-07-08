using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Fungus;

[Serializable]
public class ThirdCharacterType
{
    public AssetReferenceSprite baseSprite;

    [Serializable]
    public class EmotionSprite
    {
        public string emotionKey;
        public AssetReferenceSprite emotionSprite;
    }

    public List<EmotionSprite> emotionSprites = new List<EmotionSprite>();
}

[Serializable]
public class ThirdCharacter
{
    public string characterName;
    public ThirdCharacterType[] characterTypes;
    public AssetReferenceSprite[] dressSprites;
}

public class ThirdCharacterManager : MonoBehaviour
{
    [SerializeField] public RawImage[] characterPositions = new RawImage[8];
    [SerializeField] public RawImage[] emotionLayers = new RawImage[8];
    [SerializeField] public RawImage[] dressLayers = new RawImage[8];
    [SerializeField] private List<ThirdCharacter> characters;

    private Dictionary<AssetReferenceSprite, Sprite> spriteCache = new Dictionary<AssetReferenceSprite, Sprite>();

    public ThirdCharacter GetCharacterByName(string name) => characters.Find(c => c.characterName == name);


    public void SetCharacterSprite(string characterName, int positionIndex, int characterTypeIndex, string emotionKey, int dressIndex)
    {
        if (positionIndex < 0 || positionIndex >= characterPositions.Length || characterPositions[positionIndex] == null)
            return;

        var character = GetCharacterByName(characterName);
        if (character == null) return;

        if (characterTypeIndex < 0 || characterTypeIndex >= character.characterTypes.Length)
            return;

        var type = character.characterTypes[characterTypeIndex];
        LoadAndSetSprite(type.baseSprite, characterPositions[positionIndex]);

        if (!string.IsNullOrEmpty(emotionKey))
        {
            var emo = type.emotionSprites.Find(e => e.emotionKey == emotionKey);
            if (emo != null)
            {
                LoadAndSetSprite(emo.emotionSprite, emotionLayers[positionIndex]);
            }
        }

        if (dressIndex >= 0 && dressIndex < character.dressSprites.Length)
        {
            LoadAndSetSprite(character.dressSprites[dressIndex], dressLayers[positionIndex]);
        }
    }

    private void LoadAndSetSprite(AssetReferenceSprite reference, RawImage image)
    {
        if (reference == null || image == null) return;
        if (spriteCache.TryGetValue(reference, out var cached))
        {
            image.texture = cached.texture;
            return;
        }
        reference.LoadAssetAsync<Sprite>().Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                spriteCache[reference] = handle.Result;
                image.texture = handle.Result.texture;
            }
        };
    }

    private void ClearSpriteCache()
    {
        foreach (var key in spriteCache.Keys) Addressables.Release(key);
        spriteCache.Clear();
    }

    private void OnDestroy() => ClearSpriteCache();
}

[CommandInfo("Character", "Fade Out Character 3", "Fades out the third-type character at the specified position")]
public class FadeOutCharacterCommand3 : Command
{
    [SerializeField] private int positionIndex;
    [SerializeField] private float duration = 1f;

    public override void OnEnter()
    {
        ThirdCharacterManager manager = FindObjectOfType<ThirdCharacterManager>();
        if (manager != null)
        {
            manager.StartCoroutine(FadeOut(positionIndex, duration));
        }
        Continue();
    }

    private IEnumerator FadeOut(int index, float time)
    {
        ThirdCharacterManager manager = FindObjectOfType<ThirdCharacterManager>();
        if (manager == null) yield break;
        if (index < 0 || index >= manager.characterPositions.Length) yield break;
        RawImage baseLayer = manager.characterPositions[index];
        RawImage emotionLayer = manager.emotionLayers[index];
        RawImage dressLayer = manager.dressLayers[index];
        float t = 0f;
        Color bc = baseLayer.color;
        Color ec = emotionLayer.color;
        Color dc = dressLayer.color;
        while (t < time)
        {
            float a = Mathf.Lerp(1f, 0f, t / time);
            baseLayer.color = new Color(bc.r, bc.g, bc.b, a);
            emotionLayer.color = new Color(ec.r, ec.g, ec.b, a);
            dressLayer.color = new Color(dc.r, dc.g, dc.b, a);
            t += Time.deltaTime;
            yield return null;
        }

        baseLayer.color = new Color(bc.r, bc.g, bc.b, 0f);
        emotionLayer.color = new Color(ec.r, ec.g, ec.b, 0f);
        dressLayer.color = new Color(dc.r, dc.g, dc.b, 0f);

        // Clear textures to ensure the character fully disappears
        baseLayer.texture = null;
        emotionLayer.texture = null;
        dressLayer.texture = null;
    }

    public override string GetSummary() => $"Fade out character at position {positionIndex} in {duration}s";
}