using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ButtonSlide : MonoBehaviour
{
    public RectTransform buttonGroup; // Группа всех кнопок, включая кнопку ButtonUp
    public Button buttonUp;           // Кнопка для скрытия кнопок
    public Button buttonDown;         // Кнопка для показа кнопок
    public float slideDuration = 0.5f; // Длительность анимации
    public float slideDistance = 200f; // Расстояние для перемещения кнопок вверх

    private Vector2 shownPosition;    // Позиция, когда панель видима
    private Vector2 hiddenPosition;   // Позиция, когда панель скрыта

    private bool isSliding = false; // Флаг для предотвращения повторных нажатий

    void Start()
    {
        // Привязка событий нажатия на кнопки
        buttonUp.onClick.AddListener(HideButtons);
        buttonDown.onClick.AddListener(ShowButtons);

        shownPosition = buttonGroup.anchoredPosition;
        hiddenPosition = shownPosition + Vector2.up * slideDistance;

        // Панель и кнопка Hide изначально скрыты, отображается только кнопка Show
        buttonGroup.anchoredPosition = hiddenPosition;
        buttonGroup.gameObject.SetActive(false);
        buttonUp.gameObject.SetActive(false);
        buttonDown.gameObject.SetActive(true);
    }

    public void HideButtons()
    {
        if (isSliding) return; // Если уже идет анимация, пропускаем
        isSliding = true;

        // Начинаем анимацию сдвига кнопок вверх
        StartCoroutine(Slide(buttonGroup, Vector3.up * slideDistance, slideDuration, () =>
        {
            // Скрываем панель и показываем кнопку для ее вызова
            buttonGroup.gameObject.SetActive(false);
            buttonUp.gameObject.SetActive(false);
            buttonDown.gameObject.SetActive(true);
            isSliding = false;
        }));
    }

    public void ShowButtons()
    {
        if (isSliding) return; // Если уже идет анимация, пропускаем
        isSliding = true;

        // Перед анимацией делаем панель видимой
        buttonGroup.gameObject.SetActive(true);

        // Начинаем анимацию сдвига кнопок вниз
        StartCoroutine(Slide(buttonGroup, Vector3.down * slideDistance, slideDuration, () =>
        {
            // Показываем кнопку ButtonUp и скрываем кнопку ButtonDown
            buttonUp.gameObject.SetActive(true);
            buttonDown.gameObject.SetActive(false);
            isSliding = false;
        }));
    }

    // Анимация перемещения RectTransform
    private IEnumerator Slide(RectTransform target, Vector3 direction, float duration, System.Action onComplete)
    {
        Vector3 startPosition = target.anchoredPosition;
        Vector3 endPosition = startPosition + direction;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            target.anchoredPosition = Vector3.Lerp(startPosition, endPosition, elapsedTime / duration);
            yield return null;
        }

        target.anchoredPosition = endPosition;
        onComplete?.Invoke();
    }
}

     
