using Fusion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class SpaceSetup
{
  [MenuItem("Double Trouble/Gerar executavel Windows")]
  public static void Build()
  {
    EditorSceneManager.SaveOpenScenes();
    AssetDatabase.SaveAssets();
    Fusion.Editor.NetworkProjectConfigUtilities.RebuildPrefabTable();
    NetworkProjectConfig.UnloadGlobal();
    System.IO.Directory.CreateDirectory("Build");
    BuildPlayerOptions settings = new BuildPlayerOptions();
    settings.scenes = new string[] { "Assets/Scenes/Game.unity" };
    settings.locationPathName = "Build/DoubleTroubleSpace.exe";
    settings.target = BuildTarget.StandaloneWindows64;
    UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(settings);
    if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
    {
      Debug.Log("DOUBLE TROUBLE: executavel criado em Build/DoubleTroubleSpace.exe");
    }
    else
    {
      throw new System.Exception("Build failed: " + report.summary.result);
    }
  }
}
