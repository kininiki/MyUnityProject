// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;
// using System.Collections;

// public class PetNameController : MonoBehaviour
// {
//     [Header("Ссылки")]
//     [SerializeField] private TMP_InputField inputField;
//     [SerializeField] private TMP_Text petNameText;
    
//     [Header("Настройки")]
//     [SerializeField] private int characterLimit = 15;
//     [SerializeField] private string defaultName = "Antony";
    

//     private string petName;
//     private const string PetNameKey = "PetName";
//     private bool isInitialized = false;

//     private IEnumerator Start()
//     {
//         // Загрузка данных
//         LoadPetName();
//     }

//     private void LoadPetName()
//     {
//         petName = PlayerPrefs.GetString(PetNameKey, defaultName);
//         petNameText.text = petName;
//     }



//     public void OnPetNameClicked()
//     {
//         if (!isInitialized) return;
        
//         petNameText.gameObject.SetActive(false);
//         inputField.gameObject.SetActive(true);
//         inputField.text = petName;
//         inputField.ActivateInputField();
//     }

//     private void UpdatePetName(string newName)
//     {
//         if (!string.IsNullOrWhiteSpace(newName))
//         {
//             petName = newName;
//             PlayerPrefs.SetString(PetNameKey, petName);
//             PlayerPrefs.Save();
//             petNameText.text = petName;
//         }

//         inputField.gameObject.SetActive(false);
//         petNameText.gameObject.SetActive(true);
//     }


// }