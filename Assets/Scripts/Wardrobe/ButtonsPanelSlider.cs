using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ButtonsPanelSlider : MonoBehaviour
{
    [SerializeField] private Button buttonsUpButton;      // Кнопка для подъёма панели
    [SerializeField] private Button buttonsDownButton1;   // Первая кнопка для опускания панели
    [SerializeField] private Button buttonsDownButton2;   // Вторая кнопка для опускания панели
    [SerializeField] private RectTransform panelRect;     // Панель, которую нужно двигать
    
    [Header("Animation Settings")]
    [SerializeField] private float slideDuration = 0.5f;      // Время анимации
    [SerializeField] private float slideDistance = 500f;      // Дистанция перемещения панели
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);  // Кривая анимации
    [SerializeField] private bool fadeButtons = true;         // Нужно ли скрывать кнопки
    [SerializeField] private float fadeDelay = 0.1f;          // Задержка начала затухания кнопок
    
    [Header("Optional Effects")]
    [SerializeField] private bool useScale = true;        // Масштабирование при скрытии
    [SerializeField] private bool useRotation = false;    // Поворот при движении
    
    private bool isPanelVisible = true;   // Панель видна или скрыта
    private bool isButtonsDownButton2Clicked = false; // Отслеживаем, была ли нажата вторая кнопка
    private Coroutine slideCoroutine;     // Для остановки текущей анимации
    private CanvasGroup panelCanvasGroup; // Для управления прозрачностью панели

    private void Start()
    {
        // Добавляем обработчики нажатий на кнопки
        buttonsUpButton.onClick.AddListener(() => OnButtonsUpButtonClicked()); // Поднять панель
        buttonsDownButton1.onClick.AddListener(() => OnButtonsDownButton1Clicked()); // Опустить панель (первая кнопка)
        buttonsDownButton2.onClick.AddListener(() => OnButtonsDownButton2Clicked()); // Опустить панель (вторая кнопка)
        
        // Скрываем кнопку "Вверх" изначально
        buttonsUpButton.gameObject.SetActive(false);
        
        // Добавляем CanvasGroup если нужно затухание
        if (fadeButtons && panelRect.GetComponent<CanvasGroup>() == null)
        {
            panelCanvasGroup = panelRect.gameObject.AddComponent<CanvasGroup>();
        }
    }

    // Метод, срабатывающий при нажатии на первую кнопку (ButtonsDownButton1)
    private void OnButtonsDownButton1Clicked()
    {
        // Перемещаем панель вниз
        SlidePanel(false);
    }

    // Метод, срабатывающий при нажатии на вторую кнопку (ButtonsDownButton2)
    private void OnButtonsDownButton2Clicked()
    {
        // Скрываем первую кнопку сразу, как только нажата вторая кнопка
        buttonsDownButton1.gameObject.SetActive(false);

        // Запоминаем, что была нажата вторая кнопка
        isButtonsDownButton2Clicked = true;

        // Перемещаем панель вниз
        SlidePanel(false);
    }

    // Метод, срабатывающий при нажатии на кнопку Up
    private void OnButtonsUpButtonClicked()
    {
        // Если была нажата вторая кнопка, то не восстанавливаем первую кнопку
        if (!isButtonsDownButton2Clicked)
        {
            buttonsDownButton1.gameObject.SetActive(true);  // Восстанавливаем первую кнопку, если не была нажата вторая кнопка
        }

        // Восстанавливаем кнопку "Up"
        buttonsUpButton.gameObject.SetActive(true);

        // Перемещаем панель вверх
        SlidePanel(true);
    }

    // Метод для управления панелью
    private void SlidePanel(bool slideUp)
    {
        // Прежде чем двигать панель, скрываем первую кнопку в случае, если была нажата вторая кнопка
        if (isButtonsDownButton2Clicked)
        {
            buttonsDownButton1.gameObject.SetActive(false);  // Скрываем первую кнопку
        }

        // Включаем нужные кнопки
        buttonsUpButton.gameObject.SetActive(!slideUp);  // Если панель должна подняться, скрываем кнопку "Вверх"
        buttonsDownButton2.gameObject.SetActive(slideUp); // И наоборот для кнопки "Вниз" (вторая)

        // Останавливаем текущую анимацию, если она есть
        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
        }

        // Запускаем анимацию для подъёма или опускания панели
        slideCoroutine = StartCoroutine(SlidePanelCoroutine(slideUp));
    }

    // Корутин для анимации движения панели
    private IEnumerator SlidePanelCoroutine(bool slideUp)
    {
        float elapsed = 0f;  // Время анимации
        Vector2 startPos = panelRect.anchoredPosition;  // Начальная позиция
        Vector2 targetPos = startPos;  // Целевая позиция

        // Начальные значения для дополнительных эффектов
        Vector3 startScale = panelRect.localScale;
        Vector3 targetScale = slideUp ? Vector3.one : new Vector3(0.8f, 0.8f, 1f);
        
        Quaternion startRotation = panelRect.localRotation;
        Quaternion targetRotation = slideUp ? 
            Quaternion.Euler(0, 0, 0) : 
            Quaternion.Euler(0, 0, -5f);

        // Определяем целевую позицию для панели
        targetPos.y = startPos.y + (slideUp ? slideDistance : -slideDistance);

        // Начальная и конечная прозрачность
        float startAlpha = panelCanvasGroup ? panelCanvasGroup.alpha : 1f;
        float targetAlpha = slideUp ? 1f : 0f;

        // Анимация
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / slideDuration;
            float curveValue = slideCurve.Evaluate(progress);

            // Позиция панели
            panelRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, curveValue);

            // Масштабирование панели
            if (useScale)
            {
                panelRect.localScale = Vector3.Lerp(startScale, targetScale, curveValue);
            }

            // Поворот панели
            if (useRotation)
            {
                panelRect.localRotation = Quaternion.Lerp(startRotation, targetRotation, curveValue);
            }

            // Прозрачность панели
            if (fadeButtons && panelCanvasGroup != null && elapsed > fadeDelay)
            {
                float fadeProgress = (elapsed - fadeDelay) / (slideDuration - fadeDelay);
                panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, fadeProgress);
            }

            yield return null;
        }

        // Финальные значения
        panelRect.anchoredPosition = targetPos;
        if (useScale) panelRect.localScale = targetScale;
        if (useRotation) panelRect.localRotation = targetRotation;
        if (fadeButtons && panelCanvasGroup != null) panelCanvasGroup.alpha = targetAlpha;

        // Обновление состояния панели
        isPanelVisible = slideUp;
    }
}

