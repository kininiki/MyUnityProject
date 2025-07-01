// using UnityEngine;
// using TMPro;
// using Unity.Services.Authentication;  // Для работы с Unity Authentication
// using Unity.Services.Core;  // Для инициализации Unity Services
// using UnityEngine.UI;  // Для работы с кнопками
// using System.Threading.Tasks;
// using System.Collections;

// public class UserProfileDisplay : MonoBehaviour
// {
//     [SerializeField] private TMP_Text userIdText;  // Поле для отображения UserID
//     [SerializeField] private Button copyButton;    // Кнопка для копирования UID
//     [SerializeField] private TMP_Text copyNotificationText; // Поле для уведомления о копировании (добавьте это в Canvas)
    
//     private string userID;                         // Переменная для хранения UID

//     private async void Start()
//     {
//         // Инициализация Unity Services
//         await UnityServices.InitializeAsync();

//         // Проверяем, есть ли активная сессия пользователя
//         if (AuthenticationService.Instance.IsSignedIn)
//         {
//             // Получаем UID из AuthenticationService
//             userID = AuthenticationService.Instance.PlayerId;
//             userIdText.text = $"UID: {userID}";
//         }
//         else
//         {
//             // Если пользователь не авторизован, выводим сообщение
//             userIdText.text = "User is not signed in!";
//         }

//         // Добавляем слушатель события для копирования
//         copyButton.onClick.AddListener(CopyUIDToClipboard);
        
//         // Скрываем уведомление о копировании по умолчанию
//         copyNotificationText.gameObject.SetActive(false);
//     }

//     // Метод для копирования UID в буфер обмена
//     private void CopyUIDToClipboard()
//     {
//         if (!string.IsNullOrEmpty(userID))
//         {
//             // Сохраняем UID в буфер обмена
//             GUIUtility.systemCopyBuffer = userID;
//             Debug.Log($"Copied UserID: {userID} to clipboard.");
            
//             // Показываем уведомление о копировании
//             StartCoroutine(ShowCopyNotification());
//         }
//     }

//     // Корутина для показа уведомления о копировании на 2 секунды
//     private IEnumerator ShowCopyNotification()
//     {
//         copyNotificationText.gameObject.SetActive(true);  // Показываем уведомление
//         yield return new WaitForSeconds(2);               // Ждем 2 секунды
//         copyNotificationText.gameObject.SetActive(false); // Скрываем уведомление
//     }

//     private void OnDestroy()
//     {
//         // Удаляем слушатель событий, чтобы избежать ошибок
//         copyButton.onClick.RemoveListener(CopyUIDToClipboard);
//     }
// }


using UnityEngine;
using TMPro;
using Unity.Services.Authentication;  // Для работы с Unity Authentication
using Unity.Services.Core;  // Для инициализации Unity Services
using UnityEngine.UI;  // Для работы с кнопками
using System.Threading.Tasks;
using System.Collections;

public class UserProfileDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text userIdText;  // Поле для отображения UserID
    [SerializeField] private Button copyButton;    // Кнопка для копирования UID
    [SerializeField] private TMP_Text copyNotificationText; // Поле для уведомления о копировании (добавьте это в Canvas)
    
    private string userID;                         // Переменная для хранения UID

    private async void Start()
    {
        // Инициализация Unity Services
        await UnityServices.InitializeAsync();

        // Проверяем, есть ли активная сессия пользователя
        if (AuthenticationService.Instance.IsSignedIn)
        {
            // Получаем UID из AuthenticationService
            userID = AuthenticationService.Instance.PlayerId;
            userIdText.text = $"UID: {userID}";
        }
        else
        {
            // Если пользователь не авторизован, выводим сообщение
            userIdText.text = "User is not signed in!";
        }

        // Добавляем слушатель события для копирования
        copyButton.onClick.AddListener(CopyUIDToClipboard);
        
        // Скрываем уведомление о копировании по умолчанию
        copyNotificationText.gameObject.SetActive(false);
    }

    // Метод для копирования UID в буфер обмена
    private void CopyUIDToClipboard()
    {
        if (!string.IsNullOrEmpty(userID))
        {
            // Сохраняем UID в буфер обмена
            GUIUtility.systemCopyBuffer = userID;
            Debug.Log($"Copied UserID: {userID} to clipboard.");
            
            // Показываем уведомление о копировании
            StartCoroutine(ShowCopyNotification());
        }
    }

    // Корутина для показа уведомления о копировании на 2 секунды
    private IEnumerator ShowCopyNotification()
    {
        copyNotificationText.gameObject.SetActive(true);  // Показываем уведомление
        yield return new WaitForSeconds(2);               // Ждем 2 секунды
        copyNotificationText.gameObject.SetActive(false); // Скрываем уведомление
    }

    private void OnDestroy()
    {
        // Удаляем слушатель событий, чтобы избежать ошибок
        copyButton.onClick.RemoveListener(CopyUIDToClipboard);
    }
}
