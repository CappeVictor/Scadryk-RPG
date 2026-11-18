#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ScadrykBuildMenu
{
    [MenuItem("Scadryk/Build Windows demo")]
    public static void BuildWindowsDemo()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string outputDirectory = Path.Combine(projectRoot, "Build");
        Directory.CreateDirectory(outputDirectory);

        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/ScadrykDemo.unity" },
            locationPathName = Path.Combine(outputDirectory, "ScadrykDemo.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            throw new System.Exception("Falha ao gerar o executável Windows do Scadryk: " + report.summary.result);
        if (!Application.isBatchMode) EditorUtility.DisplayDialog("Scadryk", "Executável criado em Build/ScadrykDemo.exe", "OK");
    }
}
#endif
