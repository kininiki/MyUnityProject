using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;

public class PlayerPrefsTools
{
    [MenuItem("Tools/Clear PlayerPrefs")]
    public static void ClearPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("PlayerPrefs cleared!");
    }
}
#endif
