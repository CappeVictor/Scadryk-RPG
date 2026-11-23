#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ScadrykBuildMenu
{
    public static void BuildWindowsAndCampaignSmoke()
    {
        BuildWindowsDemo();
        BuildCampaignSmokeTest();
    }

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
        WriteBuildInfo(projectRoot, outputDirectory);
        if (!Application.isBatchMode) EditorUtility.DisplayDialog("Scadryk", "Executável criado em Build/ScadrykDemo.exe", "OK");
    }

    private static void WriteBuildInfo(string projectRoot, string outputDirectory)
    {
        string commit = "desconhecido";
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                Arguments = "-C \"" + projectRoot.Replace("\"", "\\\"") + "\" rev-parse --short=12 HEAD",
                WorkingDirectory = projectRoot,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(startInfo))
            {
                if (process != null)
                {
                    string output = process.StandardOutput.ReadToEnd().Trim();
                    process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    if (process.ExitCode == 0 && !string.IsNullOrEmpty(output)) commit = output;
                }
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning("Scadryk: commit não identificado para a build: " + exception.Message);
        }

        string editorVersion = Application.unityVersion;
        string buildUtc = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture);
        string[] lines =
        {
            "Scadryk — UnityPort Windows build",
            "Commit: " + commit,
            "Unity Editor: " + editorVersion,
            "Build UTC: " + buildUtc
        };
        File.WriteAllLines(Path.Combine(outputDirectory, "SCADRYK_BUILD_INFO.txt"), lines, new System.Text.UTF8Encoding(false));
    }

    [MenuItem("Scadryk/Build campaign smoke test")]
    public static void BuildCampaignSmokeTest()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string outputDirectory = Path.Combine(projectRoot, "BuildSmoke");
        Directory.CreateDirectory(outputDirectory);
        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/ScadrykDemo.unity" },
            locationPathName = Path.Combine(outputDirectory, "ScadrykSmoke.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            throw new System.Exception("Falha no build de teste da campanha: " + report.summary.result);
    }
}
#endif
