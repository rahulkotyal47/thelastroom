using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TheLastRoomBuilder
{
    const string ScenePath = "Assets/Scenes/TheLastRoom.unity";

    [MenuItem("Tools/The Last Room/Build Game")]
    public static void BuildGame() { BuildScene(); }

    [MenuItem("Tools/The Last Room/Build Game and Play")]
    public static void BuildAndPlay()
    {
        if (BuildScene()) EditorApplication.isPlaying = true;
    }

    [MenuItem("Tools/The Last Room/Build Windows EXE")]
    public static void BuildExe()
    {
        if (!BuildScene()) return;
        string path = "Builds/TheLastRoom/TheLastRoom.exe";
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var opts = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = path,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("THE LAST ROOM: build succeeded at " + path);
            EditorUtility.RevealInFinder(path);
        }
        else Debug.LogError("THE LAST ROOM: build failed. See the Console for the first error.");
    }

    static bool BuildScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("THE LAST ROOM: leave Play Mode first.");
            return false;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;

        EnsureFolders();
        Material[] keepers = CreateKeepers();
        AddShader("Standard");
        AddShader("Universal Render Pipeline/Lit");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var go = new GameObject("GameBootstrap");
        var boot = go.AddComponent<GameBootstrap>();
        boot.variantKeepers = keepers;
        GameBootstrap.ApplyAtmosphere();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            Debug.LogError("THE LAST ROOM: could not save " + ScenePath);
            return false;
        }

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.productName = "THE LAST ROOM";
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        AssetDatabase.SaveAssets();

        bool ok = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null
                  && EditorBuildSettings.scenes.Length == 1
                  && GameObject.Find("GameBootstrap") != null;
        Debug.Log(ok ? "THE LAST ROOM: scene built and added to Build Settings. Press Play."
                     : "THE LAST ROOM: validation failed.");
        return ok;
    }

    static void EnsureFolders()
    {
        foreach (var n in new[] { "Scenes", "Materials", "Prefabs", "Audio", "UI", "Resources" })
            if (!AssetDatabase.IsValidFolder("Assets/" + n)) AssetDatabase.CreateFolder("Assets", n);
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Audio")) AssetDatabase.CreateFolder("Assets/Resources", "Audio");
    }

    static Material[] CreateKeepers()
    {
        string a = "Assets/Materials/KeeperEmissive.mat", b = "Assets/Materials/KeeperPlain.mat";
        AssetDatabase.DeleteAsset(a); AssetDatabase.DeleteAsset(b);
        var m1 = Mats.Make(Color.gray, 0.3f, Color.white);
        var m2 = Mats.Make(Color.gray, 0.3f);
        AssetDatabase.CreateAsset(m1, a);
        AssetDatabase.CreateAsset(m2, b);
        return new[] { m1, m2 };
    }

    static void AddShader(string name)
    {
        var sh = Shader.Find(name);
        if (sh == null) return;
        var gs = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
        if (gs == null) return;
        var so = new SerializedObject(gs);
        var arr = so.FindProperty("m_AlwaysIncludedShaders");
        if (arr == null) return;
        for (int i = 0; i < arr.arraySize; i++)
            if (arr.GetArrayElementAtIndex(i).objectReferenceValue == sh) return;
        arr.arraySize++;
        arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh;
        so.ApplyModifiedProperties();
    }
}
