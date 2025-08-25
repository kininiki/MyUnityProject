#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public static class FungusFlowchartSaver
{
    [MenuItem("Tools/Fungus/Save All Flowcharts")]
    public static void SaveAllFlowcharts()
    {
        var dirtyScenes = new HashSet<Scene>();

        foreach (var flow in Object.FindObjectsOfType<Fungus.Flowchart>())
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(flow);
            EditorUtility.SetDirty(flow);
            dirtyScenes.Add(flow.gameObject.scene);
        }

        foreach (var scene in dirtyScenes)
        {
            if (scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("✅ Все Flowchart сохранены");
    }
}
#endif