using System;
using UnityEngine;

public class OpenTelegram : MonoBehaviour
{
    private string telegramUrl = "https://t.me/verybadstories"; // Заменить на нашу ссылку
    private string openTermsUrl = "https://drive.google.com/file/d/1mrd31eio-3q9jHJ-wyE8LVIubaPxRlBo/view?usp=sharing"; // пользовательское соглашение
    private string confidenceUrl = "https://www.termsfeed.com/live/b5cec0ef-b615-437d-a337-af1ee094542f";
    private string helpEmail = "verybadstories.manager@gmail.com";
    private const string helpSubject = "Письмо в поддержку \"Very Bad Stories\"";
    private const string helpBody = "Опишите Вашу проблему здесь.";

    public void OpenTelega()
    {
        Application.OpenURL(telegramUrl);
    }

    public void OpenTerms()
    {
        Application.OpenURL(openTermsUrl); 
    }

    public void OpenConf()
    {
        Application.OpenURL(confidenceUrl);
    }


    public void OpenHelp()
    {
        string url = "https://mail.google.com/mail/?view=cm&fs=1&to=" + helpEmail +
                     "&su=" + Uri.EscapeDataString(helpSubject) +
                     "&body=" + Uri.EscapeDataString(helpBody);
        Application.OpenURL(url);
    }
}
