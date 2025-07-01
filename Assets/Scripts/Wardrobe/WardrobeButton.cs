using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class WardrobeButton : MonoBehaviour
{
    [SerializeField] private Image buttonIcon; // Основная иконка кнопки
    
    // Настройки свечения
    [SerializeField] private float glowSize = 100f; // Размер свечения
    [SerializeField] private float glowIntensity = 0.5f; // Максимальная яркость
    [SerializeField] private float glowSpeed = 2f; // Скорость пульсации
    
    private Button button;
    private Image glowImage;
    private Coroutine glowCoroutine;
    public static WardrobeButton CurrentSelected { get; private set; }

    private void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
        
        // Создаём объект для свечения
        CreateGlowEffect();

        // Если это первая кнопка - делаем её выбранной при старте
        if (CurrentSelected == null && transform.GetSiblingIndex() == 0)
        {
            SetSelected(true);
        }
    }

    private void CreateGlowEffect()
    {
        // Создаём новый GameObject для свечения
        GameObject glowObject = new GameObject("Glow");
        glowObject.transform.SetParent(transform);
        
        // Настраиваем RectTransform
        RectTransform glowRect = glowObject.AddComponent<RectTransform>();
        glowRect.anchoredPosition = Vector2.zero;
        glowRect.sizeDelta = new Vector2(glowSize, glowSize);
        glowRect.anchorMin = new Vector2(0.5f, 0.5f);
        glowRect.anchorMax = new Vector2(0.5f, 0.5f);
        glowRect.pivot = new Vector2(0.5f, 0.5f);
        
        // Добавляем и настраиваем Image
        glowImage = glowObject.AddComponent<Image>();
        glowImage.sprite = CreateGlowSprite();
        glowImage.color = new Color(1, 1, 1, 0); // Начально прозрачный
        glowImage.raycastTarget = false; // Чтобы не мешать кликам
        
        // Помещаем свечение под кнопку в иерархии
        glowObject.transform.SetSiblingIndex(0);
    }

    private Sprite CreateGlowSprite()
    {
        // Создаём текстуру с радиальным градиентом
        int textureSize = 256;
        Texture2D texture = new Texture2D(textureSize, textureSize);
        
        float radius = textureSize / 2f;
        Vector2 center = new Vector2(radius, radius);
        
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float normalizedDistance = distance / radius;
                
                // Создаём плавное затухание от центра к краям
                float alpha = Mathf.Clamp01(1 - normalizedDistance);
                // Используем кривую для более мягкого свечения
                alpha = Mathf.Pow(alpha, 2f);
                
                texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }
        
        texture.Apply();
        
        // Создаём спрайт из текстуры
        return Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize), new Vector2(0.5f, 0.5f));
    }

    private void OnClick()
    {
        if (CurrentSelected != null && CurrentSelected != this)
        {
            CurrentSelected.SetSelected(false);
        }
        SetSelected(true);
    }

    private void SetSelected(bool selected)
    {
        if (selected)
        {
            CurrentSelected = this;
            if (glowCoroutine != null)
                StopCoroutine(glowCoroutine);
            glowCoroutine = StartCoroutine(GlowAnimation());
        }
        else
        {
            if (glowCoroutine != null)
                StopCoroutine(glowCoroutine);
            glowImage.color = new Color(1, 1, 1, 0);
        }
    }

    private IEnumerator GlowAnimation()
    {
        float t = 0;
        while (true)
        {
            t += Time.deltaTime * glowSpeed;
            float alpha = (Mathf.Sin(t) + 1) * 0.5f * glowIntensity;
            glowImage.color = new Color(1, 1, 1, alpha);
            yield return null;
        }
    }

    private void OnDestroy()
    {
        if (glowCoroutine != null)
            StopCoroutine(glowCoroutine);
    }
}