using UnityEngine;
using TMPro;

public class CatMoneyDisplay : MonoBehaviour
{
    public TMP_Text catMoneyText;

    private void Start()
    {
        InvokeRepeating(nameof(UpdateCatMoney), 0, 4f);
    }

    private void UpdateCatMoney()
    {
        if (GameCloud.Instance != null)
        {
            int catMoney = GameCloud.Instance.GetCurrencyAmount("CATMONEY_ELIXIR");
            catMoneyText.text = catMoney.ToString();
        }
    }
}
