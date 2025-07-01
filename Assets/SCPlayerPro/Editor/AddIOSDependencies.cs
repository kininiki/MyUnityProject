using UnityEditor;
using UnityEditor.Callbacks;
#if UNITY_EDITOR_OSX
using UnityEditor.iOS.Xcode;
using System.IO;

public class AddIOSDependencies
{
    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget buildTarget, string path)
    {
        if (buildTarget == BuildTarget.iOS)
        {
          
            string projPath = PBXProject.GetPBXProjectPath(path);
            PBXProject proj = new PBXProject();
            proj.ReadFromFile(projPath);

            
            string targetGuid = proj.GetUnityFrameworkTargetGuid();

            proj.AddFrameworkToProject(targetGuid, "VideoToolbox.framework", false);

            proj.AddFrameworkToProject(targetGuid, "libz.tbd", false);
            proj.AddFrameworkToProject(targetGuid, "liblzma.tbd", false);
            proj.AddFrameworkToProject(targetGuid, "libbz2.tbd", false);
            proj.AddFrameworkToProject(targetGuid, "libiconv.tbd", false);

            proj.WriteToFile(projPath);
        }
    }
}
#endif