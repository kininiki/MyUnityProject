using UnityEngine;
using UnityEngine.UI;

public class ShopPageManager : MonoBehaviour
{
    [Header("Buttons for Tabs")]
    [SerializeField] private Button rubyButton;
    [SerializeField] private Button elixirButton;
    [SerializeField] private Button catmoneyButton;

    [Header("Scroll Views for Pages")]
    [SerializeField] private GameObject rubyScrollView;
    [SerializeField] private GameObject elixirScrollView;
    [SerializeField] private GameObject catmoneyScrollView;

    [Header("Button Colors")]
    [SerializeField] private Color activeButtonColor; // Цвет активной кнопки
    [SerializeField] private Color inactiveButtonColor; // Цвет неактивных кнопок

    private void Start()
    {
        // Назначаем события кнопкам
        rubyButton.onClick.AddListener(ShowRubyPage);
        elixirButton.onClick.AddListener(ShowElixirPage);
        catmoneyButton.onClick.AddListener(ShowCatmoneyPage);

        // Показываем вкладку на основе выбранной в SceneLoaderShop
        switch (SceneLoaderShop.selectedScrollView)
        {
            case "Elixir":
                ShowElixirPage();
                break;
            case "Catmoney":
                ShowCatmoneyPage();
                break;
            default:
                ShowRubyPage(); // По умолчанию Ruby
                break;
        }
    }

    private void ShowRubyPage()
    {
        // Активируем только Scroll View для рубинов
        SetActiveScrollView(rubyScrollView, rubyButton);
    }

    private void ShowElixirPage()
    {
        // Активируем только Scroll View для эликсиров
        SetActiveScrollView(elixirScrollView, elixirButton);
    }

    private void ShowCatmoneyPage()
    {
        // Активируем только Scroll View для catmoney
        SetActiveScrollView(catmoneyScrollView, catmoneyButton);
    }

    private void SetActiveScrollView(GameObject activeScrollView, Button activeButton)
    {
        // Деактивируем все Scroll View
        rubyScrollView.SetActive(false);
        elixirScrollView.SetActive(false);
        catmoneyScrollView.SetActive(false);

        // Активируем нужный Scroll View
        activeScrollView.SetActive(true);

        // Сбрасываем цвета всех кнопок
        ResetButtonColors();

        // Подсвечиваем активную кнопку
        activeButton.GetComponent<Image>().color = activeButtonColor;
    }

    private void ResetButtonColors()
    {
        rubyButton.GetComponent<Image>().color = inactiveButtonColor;
        elixirButton.GetComponent<Image>().color = inactiveButtonColor;
        catmoneyButton.GetComponent<Image>().color = inactiveButtonColor;
    }
}
