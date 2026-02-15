using UnityEngine;

namespace FootballAI.FootballCore
{
public enum MatchGameState
{
    Playing,
    Goal,
    OutOfBounds
}

public enum RestartType
{
    ThrowIn,
    CornerKick,
    GoalKick
}

public class MatchRuntimeState
{
    #region Bool State
    public bool GamePaused = false;
    public bool IsMatchEndPause = false;
    #endregion

    #region Timer State
    public float AutoResumeTimer = 0f;
    #endregion

    #region Phase State
    public MatchGameState CurrentGameState = MatchGameState.Playing;
    #endregion

    #region Restart State
    public Vector3 OutOfBoundsPosition = Vector3.zero;
    public GameObject RestartPlayer;
    public RestartType CurrentRestartType = RestartType.ThrowIn;
    #endregion
}
}
