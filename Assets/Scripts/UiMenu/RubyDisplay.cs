using UnityEngine;
using TMPro;

public class RubyDisplay : MonoBehaviour
{
    public TMP_Text rubyText;

    private void Start()
    {
        InvokeRepeating(nameof(UpdateRuby), 0, 4f);
    }

    private void UpdateRuby()
    {
        if (GameCloud.Instance != null)
        {
            int ruby = GameCloud.Instance.GetCurrencyAmount("PLAYER_RUBY");
            rubyText.text = ruby.ToString();
        }
    }
}