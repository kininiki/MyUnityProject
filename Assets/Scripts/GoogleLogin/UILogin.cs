using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UILogin : MonoBehaviour
{
    [SerializeField] private Button loginButton;

    [SerializeField] private TMP_Text userIdText;
    [SerializeField] private TMP_Text userNameText;

    [SerializeField] private Transform loginPanel, userPanel;

    [SerializeField] private LoginController loginController;

    private PlayerProfile playerProfile;

    private void OnEnable()
    {
        loginButton.onClick.AddListener(LoginButtonPressed);
        loginController.OnSignedIn += LoginController_OnSignedIn;
        loginController.OnAvatarUpdate += LoginController_OnAvatarUpdate;
    }

    private void OnDisable()
    {
        loginButton.onClick.RemoveListener(LoginButtonPressed);
        loginController.OnSignedIn -= LoginController_OnSignedIn;
        loginController.OnAvatarUpdate -= LoginController_OnAvatarUpdate;
    }

    private async void LoginButtonPressed()
    {
        await loginController.InitSignIn();
    }

    private void LoginController_OnSignedIn(PlayerProfile profile)
    {
        playerProfile = profile;
        loginPanel.gameObject.SetActive(false);
        userPanel.gameObject.SetActive(true);
       
        userIdText.text = $"id_{playerProfile.playerInfo.Id}";
        userNameText.text = profile.Name;

        // Переход на сцену "mainMenu" (можно также здесь переключить сцену)
        SceneManager.LoadScene("mainMenu");
    }
    private void LoginController_OnAvatarUpdate(PlayerProfile profile)
    {
        playerProfile = profile;
    }

   
}