using UnityEngine;
using UnityEngine.UI;
using Fungus;
using System.Collections.Generic;
using System.Linq;

public class CharacterSpriteManager : MonoBehaviour
{
    [System.Serializable]
    public class CharacterLayer
    {
        public string layerName;
        public List<Sprite> sprites;
    }

    public List<CharacterLayer> characterLayers;
    public Image leftCharacterImage;
    public Image rightCharacterImage;
    public Image leftOffscreenCharacterImage;
    public Image rightOffscreenCharacterImage;

    private Dictionary<string, Image> positionToImageMap;

    void Awake()
    {
        positionToImageMap = new Dictionary<string, Image>
        {
            {"left", leftCharacterImage},
            {"right", rightCharacterImage},
            {"offscreen left", leftOffscreenCharacterImage},
            {"offscreen right", rightOffscreenCharacterImage}
        };
    }

    [CommandInfo("Character", "Show Layered Character", "Shows a layered character sprite in a specific position")]
    public class ShowLayeredCharacterCommand : Command
    {
        [Tooltip("Position to show the character (left, right, offscreen left, offscreen right)")]
        [SerializeField] protected string position;

        [Tooltip("Comma-separated list of sprite names for each layer. Use quotes for names with spaces.")]
        [SerializeField] protected string spriteNames;

        public override void OnEnter()
        {
            CharacterSpriteManager manager = FindObjectOfType<CharacterSpriteManager>();
            if (manager != null)
            {
                string[] spriteNameArray = ParseSpriteNames(spriteNames);
                manager.ShowLayeredCharacter(position, spriteNameArray);
            }
            else
            {
                Debug.LogError("CharacterSpriteManager not found in the scene.");
            }
            
            Continue();
        }

        private string[] ParseSpriteNames(string input)
        {
            List<string> result = new List<string>();
            bool inQuotes = false;
            string currentName = "";

            foreach (char c in input)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentName.Trim());
                    currentName = "";
                }
                else
                {
                    currentName += c;
                }
            }

            if (!string.IsNullOrEmpty(currentName))
            {
                result.Add(currentName.Trim());
            }

            return result.ToArray();
        }
    }

    public void ShowLayeredCharacter(string position, string[] spriteNames)
    {
        if (!positionToImageMap.TryGetValue(position.ToLower(), out Image targetImage))
        {
            Debug.LogWarning($"Invalid position: {position}");
            return;
        }

        Texture2D combinedTexture = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);
        bool hasValidSprite = false;

        for (int i = 0; i < characterLayers.Count; i++)
        {
            string spriteName = i < spriteNames.Length ? spriteNames[i] : "";
            if (string.IsNullOrEmpty(spriteName)) continue;

            Sprite layerSprite = characterLayers[i].sprites.Find(s => s.name == spriteName.Trim());
            if (layerSprite != null)
            {
                try
                {
                    CombineSprites(combinedTexture, layerSprite);
                    hasValidSprite = true;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error processing sprite '{spriteName}': {e.Message}. Make sure the sprite's texture is set to 'Read/Write Enabled' in the import settings.");
                }
            }
            else
            {
                Debug.LogWarning($"Sprite not found: '{spriteName}' in layer {characterLayers[i].layerName}");
            }
        }

        if (hasValidSprite)
        {
            combinedTexture.Apply();
            targetImage.sprite = Sprite.Create(combinedTexture, new Rect(0, 0, combinedTexture.width, combinedTexture.height), new Vector2(0.5f, 0.5f));
            targetImage.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("No valid sprites found to display");
            targetImage.gameObject.SetActive(false);
        }
    }

    private void CombineSprites(Texture2D targetTexture, Sprite sprite)
    {
        if (!sprite.texture.isReadable)
        {
            throw new System.Exception($"Texture for sprite '{sprite.name}' is not readable. Enable 'Read/Write' in import settings.");
        }

        Texture2D spriteTexture = sprite.texture;
        var pixels = spriteTexture.GetPixels((int)sprite.rect.x, (int)sprite.rect.y, (int)sprite.rect.width, (int)sprite.rect.height);
        
        for (int y = 0; y < sprite.rect.height; y++)
        {
            for (int x = 0; x < sprite.rect.width; x++)
            {
                Color pixelColor = pixels[y * (int)sprite.rect.width + x];
                if (pixelColor.a > 0)
                {
                    targetTexture.SetPixel(x, y, Color.Lerp(targetTexture.GetPixel(x, y), pixelColor, pixelColor.a));
                }
            }
        }
    }


    public void HideCharacter(string position)
    {
        if (position.ToLower() == "all")
        {
            foreach (var image in positionToImageMap.Values)
            {
                image.gameObject.SetActive(false);
            }
        }
        else if (positionToImageMap.TryGetValue(position.ToLower(), out Image targetImage))
        {
            targetImage.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning($"Invalid position: {position}");
        }
    }

    [ContextMenu("Check Setup")]
    private void CheckSetup()
    {
        Debug.Log("Checking CharacterSpriteManager setup:");
        Debug.Log($"Character Layers Count: {characterLayers.Count}");
        foreach (var layer in characterLayers)
        {
            Debug.Log($"  Layer '{layer.layerName}' Sprites Count: {layer.sprites.Count}");
            Debug.Log($"  Sprites in this layer: {string.Join(", ", layer.sprites.Select(s => s.name))}");
        }
        Debug.Log($"Left Character Image: {(leftCharacterImage != null ? "Set" : "Not set")}");
        Debug.Log($"Right Character Image: {(rightCharacterImage != null ? "Set" : "Not set")}");
        Debug.Log($"Offscreen Left Character Image: {(leftOffscreenCharacterImage != null ? "Set" : "Not set")}");
        Debug.Log($"Offscreen Right Character Image: {(rightOffscreenCharacterImage != null ? "Set" : "Not set")}");
    }
}