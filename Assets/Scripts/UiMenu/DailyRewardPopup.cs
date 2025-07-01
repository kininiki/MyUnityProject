using UnityEngine;
using TMPro; // Подключаем TextMeshPro
using System;

public class DailyRewardPopup : MonoBehaviour
{
    public GameObject viewport; // Окно с текстом
    public TMP_Text dailyRewardText; // Ссылка на TextMeshPro для ежедневного текста
    public TMP_Text welcomeText; // Ссылка на TextMeshPro для приветственного текста (для первого входа)

    private void Start()
    {
        CheckDailyReward();
    }

    private void CheckDailyReward()
    {
        // Получаем дату последнего входа (если нет - ставим "пусто")
        string lastLoginDate = PlayerPrefs.GetString("LastLoginDate", "");

        // Получаем текущую дату
        string currentDate = DateTime.Now.ToString("yyyy-MM-dd");

        // Если последний вход НЕ сегодня - показываем окно
        if (lastLoginDate != currentDate)
        {
            viewport.SetActive(true); // Показываем окошко

            // Если человек впервые заходит в игру
            if (string.IsNullOrEmpty(lastLoginDate))
            {
                // Прячем ежедневное сообщение и показываем приветственное
                dailyRewardText.gameObject.SetActive(false);
                welcomeText.gameObject.SetActive(true);
            }
            else
            {
                // Показываем стандартный текст для ежедневного награждения
                dailyRewardText.gameObject.SetActive(true);
                welcomeText.gameObject.SetActive(false);
            }

            // Обновляем дату последнего входа
            PlayerPrefs.SetString("LastLoginDate", currentDate);
            PlayerPrefs.Save();
        }
        else
        {
            viewport.SetActive(false); // Прячем окошко, если уже заходили сегодня
        }
    }
}