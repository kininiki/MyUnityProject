using UnityEngine;
using TMPro;

public class FoodDisplay : MonoBehaviour
{
    public TMP_Text foodText;

    private void OnEnable()
    {
        UpdateFood();
    }

    private void Start()
    {
        InvokeRepeating(nameof(UpdateFood), 0, 4f);
    }

    private void UpdateFood()
    {
        if (GameCloud.Instance != null)
        {
            int food = GameCloud.Instance.GetCurrencyAmount("PLAYER_FOOD");
            foodText.text = food.ToString();
        }
    }
}