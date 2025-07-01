// using UnityEngine;
// using UnityEngine.UI;
// using System;
// using System.Collections.Generic;
// using Fungus;

// public enum CharacterEmotion
// {
//     ОднаБровь,
//     ВсторонуВверх,
//     ВсторонуНаправо,
//     Грусть,
//     Задумч,
//     ЗакатГлаз,
//     Злость,
//     Краснеет,
//     Нахмур,
//     ОтведНаправоВзгляд,
//     ПоднБрови,
//     Слёзы,
//     Улыбка,
//     Ухмылка,
//     ШирУлыбка
// }

// [Serializable]
// public class CharacterCategory
// {
//     public string categoryName;
//     public Sprite[] sprites;
// }

// [Serializable]
// public class CharacterType
// {
//     public Sprite baseSprite;
//     public Sprite[] emotionSprites;
//     [SerializeField]
//     private CharacterEmotion[] availableEmotions;

//     public Sprite GetEmotionSprite(CharacterEmotion emotion)
//     {
//         int index = Array.IndexOf(availableEmotions, emotion);
//         return index >= 0 && index < emotionSprites.Length ? emotionSprites[index] : null;
//     }
// }

// public class CharacterManager : MonoBehaviour
// {
//     [SerializeField]
//     private Flowchart flowchart; // ссылка на Flowchart для доступа к переменным

//     [SerializeField]
//     private RawImage[] characterPositions = new RawImage[8];
    
//     [SerializeField]
//     private CharacterType[] characterTypes = new CharacterType[3];
    
//     [SerializeField]
//     private CharacterCategory makeupCategory;
//     [SerializeField]
//     private CharacterCategory hairCategory;
//     [SerializeField]
//     private CharacterCategory dressCategory;
//     [SerializeField]
//     private CharacterCategory ukrashenieCategory;
//     [SerializeField]
//     private CharacterCategory accessoriseCategory;

//     private static CharacterManager instance;
//     public static CharacterManager Instance => instance;

//     private async void Awake()
//     {
//         // Проверка, назначен ли flowchart
//         if (flowchart == null)
//         {
//             Debug.LogError("Flowchart не назначен в инспекторе!");
//         }
//         else
//         {
//             Debug.Log("Flowchart назначен успешно.");
//         }
        
//         Debug.Log("Awake in CharacterManager started.");
        
//         if (instance == null)
//         {
//             instance = this;
//             DontDestroyOnLoad(gameObject);

//             // Проверка массива characterPositions
//             Debug.Log("Starting CharacterManager initialization...");

//             for (int i = 0; i < characterPositions.Length; i++)
//             {
//                 if (characterPositions[i] == null)
//                 {
//                     Debug.LogError($"Character position {i} is NOT assigned in the inspector.");
//                 }
//                 else
//                 {
//                     Debug.Log($"Character position {i} is assigned: {characterPositions[i].name}");
//                 }
//             }
//         }
//         else
//         {
//             Debug.LogWarning("Duplicate CharacterManager instance detected. Destroying the object.");
//             Destroy(gameObject);
//         }
//     }


//     public bool SetCharacterSprites(int positionIndex, CharacterEmotion emotion)
//     {
//         // Проверка корректности позиции
//         if (positionIndex < 0 || positionIndex >= characterPositions.Length)
//         {
//             Debug.LogError($"Invalid position index: {positionIndex}");
//             return false;
//         }

//         // Проверка RawImage
//         if (characterPositions[positionIndex] == null)
//         {
//             Debug.LogError($"Character position {positionIndex} is not assigned in the inspector");
//             return false;
//         }

//         // Получение значений переменных из Flowchart
//         int typeIndex = flowchart.GetIntegerVariable("Type");
//         int makeupIndex = flowchart.GetIntegerVariable("Makeup");
//         int hairIndex = flowchart.GetIntegerVariable("Hair");
//         int dressIndex = flowchart.GetIntegerVariable("Dress");
//         int ukrashenieIndex = flowchart.GetIntegerVariable("Ukrashenie");
//         int accessoriseIndex = flowchart.GetIntegerVariable("Accessorise");

//         // Создаем и очищаем RenderTexture
//         var resultTexture = new RenderTexture(1024, 1024, 0);
//         Graphics.SetRenderTarget(resultTexture);
//         GL.Clear(true, true, Color.clear);

//         // Отрисовка базового спрайта и эмоций для указанного типа персонажа
//         if (typeIndex >= 0 && typeIndex < characterTypes.Length && characterTypes[typeIndex] != null)
//         {
//             if (characterTypes[typeIndex].baseSprite != null)
//             {
//                 DrawSpriteToTexture(characterTypes[typeIndex].baseSprite);
//             }

//             Sprite emotionSprite = characterTypes[typeIndex].GetEmotionSprite(emotion);
//             if (emotionSprite != null)
//             {
//                 DrawSpriteToTexture(emotionSprite);
//             }
//         }
//         else
//         {
//             Debug.LogError($"Invalid character type index: {typeIndex}");
//         }

//         // Отрисовка категорий (слои накладываются последовательно)
//         DrawCategorySprite(makeupCategory, makeupIndex);
//         DrawCategorySprite(hairCategory, hairIndex);
//         DrawCategorySprite(dressCategory, dressIndex);
//         DrawCategorySprite(ukrashenieCategory, ukrashenieIndex);
//         DrawCategorySprite(accessoriseCategory, accessoriseIndex);

//         // Назначаем текстуру на RawImage в нужной позиции
//         characterPositions[positionIndex].texture = resultTexture;
//         Debug.Log($"Successfully set character sprites at position {positionIndex}");
//         return true;
//     }

//     private void DrawCategorySprite(CharacterCategory category, int index)
//     {
//         if (category == null || category.sprites == null || index < 0 || index >= category.sprites.Length)
//         {
//             Debug.LogWarning($"Invalid index or missing sprites for category: {category?.categoryName}");
//             return;
//         }

//         var sprite = category.sprites[index];
//         if (sprite != null)
//         {
//             DrawSpriteToTexture(sprite);
//         }
//         else
//         {
//             Debug.LogWarning($"Sprite at index {index} is null in category {category.categoryName}");
//         }
//     }

//     private void DrawSpriteToTexture(Sprite sprite)
//     {
//         if (sprite == null) return;

//         var material = new Material(Shader.Find("Sprites/Default"));
//         material.mainTexture = sprite.texture;

//         Graphics.DrawMesh(
//             GetQuadMesh(),
//             Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one),
//             material,
//             0
//         );
//     }

//     private Mesh GetQuadMesh()
//     {
//         var mesh = new Mesh();
//         mesh.vertices = new Vector3[]
//         {
//             new Vector3(-1, -1, 0),
//             new Vector3(-1, 1, 0),
//             new Vector3(1, 1, 0),
//             new Vector3(1, -1, 0)
//         };
//         mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
//         mesh.uv = new Vector2[]
//         {
//             new Vector2(0, 0),
//             new Vector2(0, 1),
//             new Vector2(1, 1),
//             new Vector2(1, 0)
//         };
//         return mesh;
//     }
// }




// [CommandInfo("Character", 
//     "Set Character Position", 
//     "Sets character sprites at specified position with selected emotion")]
// public class SetCharacterPosition : Command
// {
//     public enum CharacterPositionType
//     {
//         LeftFar = 0,
//         LeftMid = 1,
//         LeftClose = 2,
//         CenterLeft = 3,
//         Center = 4,
//         CenterRight = 5,
//         RightClose = 6,
//         RightMid = 7
//     }

//     [Tooltip("Position on the screen where the character should appear")]
//     [SerializeField]
//     protected CharacterPositionType characterPosition;

//     [Tooltip("Character emotion to display")]
//     [SerializeField]
//     protected CharacterEmotion emotion;

//     public override void OnEnter()
//     {
//         if (CharacterManager.Instance != null)
//         {
//             int positionIndex = (int)characterPosition;
//             CharacterManager.Instance.SetCharacterSprites(positionIndex, emotion);
//         }
//         else
//         {
//             Debug.LogError("CharacterManager instance not found!");
//         }

//         Continue();
//     }

//     public override string GetSummary()
//     {
//         return $"Set character at {characterPosition} with {emotion} emotion";
//     }

//     public override Color GetButtonColor()
//     {
//         return new Color32(235, 191, 217, 255);
//     }
// }
