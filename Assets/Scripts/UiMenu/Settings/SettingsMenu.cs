using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using System.Globalization;  // Для определения языка системы

public class SettingsMenu : MonoBehaviour
{
    // Переключатель уведомлений
    [SerializeField] private Toggle notificationToggle; 
    [SerializeField] private Image imageOn;       // Ссылка на изображение "On" для уведомлений
    [SerializeField] private Image imageOff;      // Ссылка на изображение "Off" для уведомлений

    // Переключатель звука
    [SerializeField] private Toggle soundToggle;
    [SerializeField] private Image soundImageOn;  // Ссылка на изображение "On" для звука
    [SerializeField] private Image soundImageOff; // Ссылка на изображение "Off" для звука

    // Элементы для выбора языка с пролистыванием
    [SerializeField] private TMP_Text languageLabel;       // Текст текущего языка
    [SerializeField] private Button languageLeftButton;    // Кнопка листания влево
    [SerializeField] private Button languageRightButton;   // Кнопка листания вправо

    // Список поддерживаемых языков
    private readonly string[] languages = { "Русский", "English" };
    private int currentLanguageIndex;

    // Ссылка на GameCloud для сохранения данных
//    [SerializeField] private GameCloud gameCloud;

private void Start()
{
    // Инициализируем переключатель уведомлений и выбор языка на основе загруженных данных
    bool notificationsEnabled = PlayerPrefs.GetInt("NotificationsEnabled", 1) == 1; // По умолчанию "On"
    notificationToggle.isOn = notificationsEnabled;
    UpdateNotificationImages(notificationsEnabled);

    // Инициализируем переключатель звука
    bool soundEnabled = PlayerPrefs.GetInt("SoundEnabled", 1) == 1; // По умолчанию "On"
    soundToggle.isOn = soundEnabled;
    UpdateSoundImages(soundEnabled);
    ApplySoundState(soundEnabled); // Применяем начальное состояние звука

    // Загрузка сохранённого языка или установка языка системы
    string savedLanguage = PlayerPrefs.GetString("SelectedLanguage", "");
    if (string.IsNullOrEmpty(savedLanguage))
    {
        // Если язык не сохранён, получаем язык устройства
        savedLanguage = GetDeviceLanguage();
    }

    // Устанавливаем выбранный язык
    currentLanguageIndex = GetLanguageIndex(savedLanguage);
    languageLabel.text = languages[currentLanguageIndex];

    // Добавляем слушатели изменений для уведомлений и языка
    notificationToggle.onValueChanged.AddListener(OnNotificationToggleChanged);
    soundToggle.onValueChanged.AddListener(OnSoundToggleChanged);
    languageLeftButton.onClick.AddListener(OnLanguageLeft);
    languageRightButton.onClick.AddListener(OnLanguageRight);
    
    // Устанавливаем начальный язык
    ChangeLanguage(savedLanguage);
}


    // Метод для получения языка устройства
    private string GetDeviceLanguage()
    {
        string deviceLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;  // Код языка устройства

        switch (deviceLanguage)
        {
            case "ru":
                return "Русский";
            case "en":
                return "English";
            default:
                return "English"; // По умолчанию — английский, если язык устройства не поддерживается
        }
    }

    // Метод при изменении переключателя уведомлений
    private void OnNotificationToggleChanged(bool isOn)
    {
        UpdateNotificationImages(isOn);

        // Обновляем и сохраняем данные в облако при изменении
        SaveSettingsToCloud(isOn ? 1 : 0, languages[currentLanguageIndex], soundToggle.isOn ? 1 : 0);
    }

    // Метод при изменении языка
    private void OnLanguageChanged(int selectedIndex)
    {
        string selectedLanguage = languages[selectedIndex];

        // Сохраняем выбранный язык и обновляем локализацию
        PlayerPrefs.SetString("SelectedLanguage", selectedLanguage);
        PlayerPrefs.Save();

        ChangeLanguage(selectedLanguage);

        // Обновляем и сохраняем данные в облако при изменении
        SaveSettingsToCloud(notificationToggle.isOn ? 1 : 0, selectedLanguage, notificationToggle.isOn ? 1 : 0);
    }


    private void OnLanguageLeft()
    {
        currentLanguageIndex--;
        if (currentLanguageIndex < 0)
        {
            currentLanguageIndex = languages.Length - 1;
        }
        languageLabel.text = languages[currentLanguageIndex];
        OnLanguageChanged(currentLanguageIndex);
    }

    private void OnLanguageRight()
    {
        currentLanguageIndex++;
        if (currentLanguageIndex >= languages.Length)
        {
            currentLanguageIndex = 0;
        }
        languageLabel.text = languages[currentLanguageIndex];
        OnLanguageChanged(currentLanguageIndex);
    }




// Метод при изменении переключателя звука
private void OnSoundToggleChanged(bool isOn)
{
    UpdateSoundImages(isOn);
    ApplySoundState(isOn); // Применяем состояние звука

    // Сохраняем текущее состояние в локальных настройках
    PlayerPrefs.SetInt("SoundEnabled", isOn ? 1 : 0);
    PlayerPrefs.Save();

    // Обновляем и сохраняем данные в облако при изменении
    SaveSettingsToCloud(
        notificationToggle.isOn ? 1 : 0,
        languages[currentLanguageIndex],
        isOn ? 1 : 0
    );
}


    // Метод для изменения языка в игре
    private async void ChangeLanguage(string language)
    {
        Locale newLocale = null;
        switch (language)
        {
            case "Русский":
                newLocale = LocalizationSettings.AvailableLocales.GetLocale("ru");
                break;
            case "English":
                newLocale = LocalizationSettings.AvailableLocales.GetLocale("en");
                break;
        }

        if (newLocale != null)
        {
            LocalizationSettings.SelectedLocale = newLocale;
            Debug.Log("Language changed to: " + language);

            // Обновляем UI текст на новый язык
            await LocalizationSettings.InitializationOperation.Task;
        }
    }

    // Метод для обновления изображений уведомлений
    private void UpdateNotificationImages(bool isOn)
    {
        imageOn.gameObject.SetActive(isOn);
        imageOff.gameObject.SetActive(!isOn);
    }

    // Метод для обновления изображений звука
    private void UpdateSoundImages(bool isOn)
    {
        soundImageOn.gameObject.SetActive(isOn);
        soundImageOff.gameObject.SetActive(!isOn);
    }

// Метод для применения состояния звука во всей игре
private void ApplySoundState(bool isOn)
{
    if (BackgroundMusic.Instance != null)
    {
        BackgroundMusic.Instance.SetMute(!isOn);
    }
    Debug.Log("Background music is " + (isOn ? "ON" : "OFF"));
}


    // Метод для сохранения настроек в облако
    private void SaveSettingsToCloud(int notificationStatus, string language, int soundStatus)
    {
        // Вызываем метод сохранения данных в облаке
        GameCloud.Instance.SaveSettings(notificationStatus, language, soundStatus);
    }

    // Метод для получения индекса языка в Dropdown (чтобы установить корректный выбор при старте)
    private int GetLanguageIndex(string language)
    {
        for (int i = 0; i < languages.Length; i++)
        {
            if (languages[i] == language)
            {
                return i;
            }
        }
        return 0; // По умолчанию — английский
    }
}



