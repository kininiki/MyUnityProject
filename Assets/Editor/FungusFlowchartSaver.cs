#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class FungusFlowchartSaver {
    [MenuItem("Tools/Fungus/Save All Flowcharts")]
    public static void SaveAllFlowcharts() {
        foreach (var flow in Object.FindObjectsOfType<Fungus.Flowchart>()) {
            EditorUtility.SetDirty(flow);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("✅ Все Flowchart сохранены");
    }
}
#endif