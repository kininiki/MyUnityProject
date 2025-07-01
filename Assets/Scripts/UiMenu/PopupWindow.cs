using UnityEngine;
using UnityEngine.UI;

public class PopupWindow : MonoBehaviour
{
    public GameObject scrollView; // объект вашего Scroll View
    public Button closeButton;    // кнопка крестик

    void Start()
    {
        // Скрываем окно при старте игры
        scrollView.SetActive(false);

        // Назначаем функцию закрытия на кнопку крестик
        closeButton.onClick.AddListener(ClosePopup);
    }

    // Функция для открытия окна
    public void OpenPopup()
    {
        scrollView.SetActive(true);
    }

    // Функция для закрытия окна
    void ClosePopup()
    {
        scrollView.SetActive(false);
    }
}
