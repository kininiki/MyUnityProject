using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

[System.Serializable]
public class PaymentData
{
    public string amount;
    public Metadata metadata;
}

[System.Serializable]
public class Metadata
{
    public string itemId;
}

public class PaymentManager : MonoBehaviour
{
    [SerializeField] private string serverUrl = "http://127.0.0.1:5000"; // URL вашего сервера

    public delegate void PaymentSuccessful(int amount);
    public static event PaymentSuccessful OnPaymentSuccessful;

    public void MakePayment(float amount, string itemId)
    {
        StartCoroutine(SendPaymentRequest(amount, itemId));
    }

    private IEnumerator SendPaymentRequest(float amount, string itemId)
    {
        UnityWebRequest request = null;
        string json = "";

        try
        {
            // Создаем экземпляр PaymentData
            var paymentData = new PaymentData
            {
                amount = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture), // Форматируем сумму с десятичной точкой
                metadata = new Metadata
                {
                    itemId = itemId
                }
            };

            // Сериализуем объект в JSON
            json = JsonUtility.ToJson(paymentData);

            request = new UnityWebRequest(serverUrl, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Ошибка создания запроса: " + e.Message);
            yield break;
        }

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Ошибка при отправке запроса: " + request.error);
        }
        else
        {
            try
            {
                var jsonResponse = JObject.Parse(request.downloadHandler.text);
                string confirmationUrl = jsonResponse["confirmationUrl"].ToString();

                Application.OpenURL(confirmationUrl);

                OnPaymentSuccessful?.Invoke((int)amount);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Ошибка парсинга JSON: " + e.Message);
                Debug.LogError("Текст ответа: " + request.downloadHandler.text);
            }
        }
    }

}



