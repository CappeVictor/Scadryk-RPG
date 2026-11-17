#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ScadrykSceneBuilder
{
    [MenuItem("Scadryk/Criar cena jogável")]
    public static void CreateDemoScene()
    {
        const string folder = "Assets/Scenes";
        const string path = folder + "/ScadrykDemo.unity";
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7.3f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.025f, 0.035f, 0.045f);
        cameraObject.AddComponent<AudioListener>();

        var director = new GameObject("Scadryk Game");
        director.AddComponent<ScadrykGameDirector>();

        EditorSceneManager.SaveScene(scene, path);
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool alreadyIncluded = false;
        foreach (var buildScene in scenes) if (buildScene.path == path) alreadyIncluded = true;
        if (!alreadyIncluded) { scenes.Add(new EditorBuildSettingsScene(path, true)); EditorBuildSettings.scenes = scenes.ToArray(); }
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Scadryk", "Cena pronta! Abra Assets/Scenes/ScadrykDemo e pressione Play.", "OK");
    }
}
#endif
