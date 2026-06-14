using UnityEditor;

[InitializeOnLoad]
public static class PlayModeStartSceneSetup
{
    private const string StartScenePath = "Assets/Scenes/TapToPlay.unity";

    static PlayModeStartSceneSetup()
    {
        EditorApplication.delayCall += SetStartScene;
    }

    private static void SetStartScene()
    {
        SceneAsset startScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(StartScenePath);
        if (startScene == null)
        {
            UnityEngine.Debug.LogWarning($"Could not find Play Mode start scene at {StartScenePath}.");
            return;
        }

        UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene = startScene;
    }
}