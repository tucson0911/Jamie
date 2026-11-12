using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public static class FlashlightPlayModeToggle
{
    private const string Keyword = "_IN_PLAY_MODE";

    static FlashlightPlayModeToggle()
    {
#if UNITY_EDITOR
        // Ensure keyword is disabled when scripts recompile in Edit Mode
        Shader.DisableKeyword(Keyword);
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnableInBuildOrPlay()
    {
        Shader.EnableKeyword(Keyword);
    }

#if UNITY_EDITOR
    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Shader.EnableKeyword(Keyword);
        }
        else if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
        {
            Shader.DisableKeyword(Keyword);
        }
    }
#endif
}