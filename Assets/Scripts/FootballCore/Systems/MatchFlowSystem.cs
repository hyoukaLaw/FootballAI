using System;

public class MatchFlowSystem
{
    public void ResumeGame(Action resetPlayers, Action resetContext, Action resetBall, ref bool gamePaused)
    {
        resetPlayers();
        resetContext();
        resetBall();
        gamePaused = false;
    }

    public void StartNewMatch(Action resetScoreAndKickoff, Action resetMatchState, Action updateScoreUI, ref bool gamePaused)
    {
        resetScoreAndKickoff();
        resetMatchState();
        gamePaused = false;
        updateScoreUI();
    }

    public void BeginGoalPause(ref bool gamePaused, ref float autoResumeTimer)
    {
        gamePaused = true;
        autoResumeTimer = 0f;
    }

    public void BeginMatchEndPause(ref bool gamePaused, ref float autoResumeTimer, ref bool isMatchEndPause)
    {
        gamePaused = true;
        autoResumeTimer = 0f;
        isMatchEndPause = true;
    }

    public void HandleAutoGame(float deltaTime, float autoResumeInterval, ref float autoResumeTimer, ref bool isMatchEndPause, Action startNewMatch, Action resumeGame)
    {
        autoResumeTimer += deltaTime;
        if (autoResumeTimer < autoResumeInterval)
            return;
        if (isMatchEndPause)
        {
            startNewMatch();
            isMatchEndPause = false;
        }
        else
        {
            resumeGame();
        }
        autoResumeTimer = 0f;
    }

    public void HandleAutoResumeThrowIn(float deltaTime, float resumeInterval, ref float autoResumeTimer, Action resumeThrowIn)
    {
        autoResumeTimer += deltaTime;
        if (autoResumeTimer < resumeInterval)
            return;
        autoResumeTimer = 0f;
        resumeThrowIn();
    }
}
