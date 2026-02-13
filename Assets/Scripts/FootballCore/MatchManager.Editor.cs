#if UNITY_EDITOR
using UnityEditor;

namespace FootballAI.FootballCore
{
public partial class MatchManager
{
    private static void PauseEditorPlayMode()
    {
        EditorApplication.isPaused = true;
    }
}
}
#endif
