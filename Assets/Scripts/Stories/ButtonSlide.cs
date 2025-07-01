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

    private bool isSliding = false; // Флаг для предотвращения повторных нажатий

    void Start()
    {
        // Привязка событий нажатия на кнопки
        buttonUp.onClick.AddListener(HideButtons);
        buttonDown.onClick.AddListener(ShowButtons);

        // Изначально кнопка ButtonDown отключена
        buttonDown.gameObject.SetActive(false);
    }

    public void HideButtons()
    {
        if (isSliding) return; // Если уже идет анимация, пропускаем
        isSliding = true;

        // Начинаем анимацию сдвига кнопок вверх
        StartCoroutine(Slide(buttonGroup, Vector3.up * slideDistance, slideDuration, () =>
        {
            // Скрываем кнопку ButtonUp и активируем кнопку ButtonDown
            buttonUp.gameObject.SetActive(false);
            buttonDown.gameObject.SetActive(true);
            isSliding = false;
        }));
    }

    public void ShowButtons()
    {
        if (isSliding) return; // Если уже идет анимация, пропускаем
        isSliding = true;

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

     
