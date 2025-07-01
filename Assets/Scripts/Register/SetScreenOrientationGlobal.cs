using UnityEngine;

public class SetScreenOrientationGlobal : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SetPortraitOrientation();
    }

    private void SetPortraitOrientation()
    {
#if UNITY_ANDROID
        Screen.orientation = ScreenOrientation.Portrait;
#endif
    }
}