using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Fungus;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using System.Collections;

[System.Serializable]
public class ButtonStyle
{
    public string buttonKey;
    public Button button;
    public Sprite buttonSprite;
    public TMP_FontAsset font;
    public int fontSize = 14;
    public Color textColor = Color.white;
    public FontStyles fontStyle = FontStyles.Normal;
    
    public TextMeshProUGUI textComponent;


    // Второй TextMeshPro для текста из инспектора
    public TextMeshProUGUI additionalTextComponent;
}

public class ButtonManager : MonoBehaviour
{
    [Header("Button Settings")]
    public List<ButtonStyle> buttonStyles;

    public void ApplyStyle(string buttonKey, string buttonText, string additionalText = "")
    {
        ButtonStyle style = buttonStyles.Find(b => b.buttonKey == buttonKey);

        if (style == null)
        {
            Debug.LogError($"Button with key '{buttonKey}' not found in ButtonManager.");
            return;
        }

        if (style.button != null)
        {
            if (style.buttonSprite != null)
            {
                style.button.image.sprite = style.buttonSprite;
            }

            if (style.textComponent != null)
            {
                style.textComponent.font = style.font;
                style.textComponent.fontSize = style.fontSize;
                style.textComponent.color = style.textColor;
                style.textComponent.fontStyle = style.fontStyle;
                style.textComponent.text = buttonText;
            }
            else
            {
                Debug.LogWarning($"TextMeshProUGUI component is not assigned for button '{buttonKey}'");
            }

            // Обновление второго текстового компонента
            if (style.additionalTextComponent != null)
            {
                style.additionalTextComponent.text = additionalText;
            }
            else
            {
                Debug.LogWarning($"Additional TextMeshProUGUI component is not assigned for button '{buttonKey}'");
            }

            style.button.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError($"Button object not assigned for key '{buttonKey}' in ButtonManager.");
        }
    }

    public void HideAllButtons(params string[] keepKeys)
    {
        HashSet<string> keepSet = (keepKeys != null) ? new HashSet<string>(keepKeys) : null;
        foreach (var style in buttonStyles)
        {
            if (keepSet != null && keepSet.Contains(style.buttonKey))
            {
                continue;
            }
            
            if (style.button != null)
            {
                style.button.onClick.RemoveAllListeners();
                style.button.gameObject.SetActive(false);
            }

            if (style.textComponent != null)
            {
                style.textComponent.text = "";
            }
            if (style.additionalTextComponent != null)
            {
                style.additionalTextComponent.text = "";
            }
        }
    }
}

[CommandInfo("UI", "Show Button", "Displays multiple buttons and hides them all when one is clicked.")]
public class ShowButtonCommand : Command
{
    [Header("Button Settings")]
    public List<string> buttonKeys; // Список ключей кнопок

    [Tooltip("Localized text for each button")]
    public List<LocalizedString> buttonTexts; // Локализованный текст для каждой кнопки

    [Tooltip("List of Flowchart variable names to set when each button is clicked")]
    public List<string> flowchartVariableNames; // Имена переменных для каждой кнопки

    [Header("Additional Text Settings")]
    [Tooltip("Text for additional TextMeshPro components")]
    public List<string> additionalTexts; // Текст для второго TextMeshPro

    private Flowchart flowchart;
    private ButtonManager buttonManager;

    public override void OnEnter()
    {
        flowchart = GetFlowchart();

        if (flowchart == null)
        {
            Debug.LogError("Flowchart not found.");
            Continue();
            return;
        }

        buttonManager = FindObjectOfType<ButtonManager>();

        if (buttonManager == null)
        {
            Debug.LogError("ButtonManager not found in the scene.");
            Continue();
            return;
        }

        buttonManager.HideAllButtons(PriceCommand.GetActiveButtonKey());
        StartCoroutine(ShowLocalizedButtons());
    }

    private IEnumerator ShowLocalizedButtons()
    {
        yield return LocalizationSettings.InitializationOperation;

        for (int i = 0; i < buttonKeys.Count; i++)
        {
            string buttonKey = buttonKeys[i];
            LocalizedString localizedText = buttonTexts[i];
            string variableName = flowchartVariableNames[i];

            string additionalText = (i < additionalTexts.Count) ? additionalTexts[i] : "";

            var textOperation = localizedText.GetLocalizedStringAsync();
            yield return textOperation;

            if (textOperation.IsDone)
            {
                buttonManager.ApplyStyle(buttonKey, textOperation.Result, additionalText);

                ButtonStyle buttonStyle = buttonManager.buttonStyles.Find(b => b.buttonKey == buttonKey);
                if (buttonStyle != null && buttonStyle.button != null)
                {
                    buttonStyle.button.onClick.RemoveAllListeners();
                    buttonStyle.button.onClick.AddListener(() => OnButtonClicked(variableName));
                }
            }
        }
    }

    private void OnButtonClicked(string variableName)
    {
        if (!string.IsNullOrEmpty(variableName) && flowchart.HasVariable(variableName))
        {
            flowchart.SetIntegerVariable(variableName, 1);
        }

        buttonManager.HideAllButtons(PriceCommand.GetActiveButtonKey());
        Continue();
    }

    public override string GetSummary()
    {
        return $"Show multiple buttons and set respective Flowchart variables on click";
    }
}
