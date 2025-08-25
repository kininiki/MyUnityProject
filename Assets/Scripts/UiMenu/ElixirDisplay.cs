using UnityEngine;
using TMPro;

public class ElixirDisplay : MonoBehaviour
{
    public TMP_Text elixirText;

    private void OnEnable()
    {
        UpdateElixir();
    }

    private void Start()
    {
        InvokeRepeating(nameof(UpdateElixir), 0, 4f);
    }

    private void UpdateElixir()
    {
        if (GameCloud.Instance != null)
        {
            int elixir = GameCloud.Instance.GetCurrencyAmount("PLAYER_ELIXIR");
            elixirText.text = elixir.ToString();
        }
    }
}