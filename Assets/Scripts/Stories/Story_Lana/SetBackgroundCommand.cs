using UnityEngine;
using UnityEngine.UI;
using Fungus;

[CommandInfo("Background", 
             "Set Background", 
             "Sets the background image with optional fade effect")]
public class SetBackgroundCommand : Command
{
    [SerializeField] protected Image targetImage;
    [SerializeField] protected Sprite backgroundSprite;
    [SerializeField] protected float fadeDuration = 1f;
    [SerializeField] protected bool useFade = true;

    public override void OnEnter()
    {
        if (targetImage == null)
        {
            Debug.LogError("Target Image is not set in SetBackgroundCommand");
            Continue();
            return;
        }

        if (backgroundSprite == null)
        {
            Debug.LogError("Background Sprite is not set in SetBackgroundCommand");
            Continue();
            return;
        }

        if (useFade)
        {
            LeanTween.value(targetImage.gameObject, 1f, 0f, fadeDuration / 2)
                .setOnUpdate((float value) =>
                {
                    Color c = targetImage.color;
                    c.a = value;
                    targetImage.color = c;
                })
                .setOnComplete(() =>
                {
                    targetImage.sprite = backgroundSprite;
                    LeanTween.value(targetImage.gameObject, 0f, 1f, fadeDuration / 2)
                        .setOnUpdate((float value) =>
                        {
                            Color c = targetImage.color;
                            c.a = value;
                            targetImage.color = c;
                        })
                        .setOnComplete(() =>
                        {
                            Continue();
                        });
                });
        }
        else
        {
            targetImage.sprite = backgroundSprite;
            Continue();
        }
    }

    public override string GetSummary()
    {
        if (targetImage == null)
        {
            return "Error: No target image set";
        }

        if (backgroundSprite == null)
        {
            return "Error: No background sprite set";
        }

        return backgroundSprite.name + (useFade ? " (with fade)" : " (no fade)");
    }

    public override Color GetButtonColor()
    {
        return new Color32(221, 184, 169, 255);
    }
}