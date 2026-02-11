using System;
using System.Collections.Generic;
using UnityEngine;
using BehaviorTree.Runtime;

public class MatchFlowSystem
{
    public void ResumeGame(List<GameObject> teamRedPlayers, List<GameObject> teamBluePlayers, MatchContext context,
        GameObject ball, BallController ballController, string nextKickoffTeam, GameObject redStartPlayer,
        GameObject blueStartPlayer, ref bool gamePaused)
    {
        ResetPlayers(teamRedPlayers, teamBluePlayers);
        ResetContext(context);
        ResetBall(ball, ballController, nextKickoffTeam, redStartPlayer, blueStartPlayer, context);
        gamePaused = false;
    }

    public void StartNewMatch(Action resetScoreAndKickoff, Action resetMatchState, ref bool gamePaused)
    {
        resetScoreAndKickoff();
        resetMatchState();
        gamePaused = false;
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

    public void ResetContext(MatchContext context)
    {
        context.IncomingPassTarget = null;
        context.SetPassTarget(null);
        context.SetBallHolder(null);
        context.SetStealCooldown(0f);
    }

    public void ResetPlayers(List<GameObject> teamRedPlayers, List<GameObject> teamBluePlayers)
    {
        ResetPlayersInternal(teamRedPlayers);
        ResetPlayersInternal(teamBluePlayers);
    }

    public void ResetBall(GameObject ball, BallController ballController, string nextKickoffTeam, GameObject redStartPlayer,
        GameObject blueStartPlayer, MatchContext context)
    {
        ball.transform.position = Vector3.zero;
        ballController.ResetMotionState();
        GameObject kickoffPlayer = null;
        if (nextKickoffTeam == "Red" && redStartPlayer != null)
        {
            redStartPlayer.transform.position = Vector3.zero;
            kickoffPlayer = redStartPlayer;
        }
        else if (nextKickoffTeam == "Blue" && blueStartPlayer != null)
        {
            blueStartPlayer.transform.position = Vector3.zero;
            kickoffPlayer = blueStartPlayer;
        }
        context.SetBallHolder(kickoffPlayer);
    }

    private static void ResetPlayersInternal(List<GameObject> players)
    {
        for (int i = 0; i < players.Count; i++)
        {
            GameObject player = players[i];
            PlayerAI playerAI = player != null ? player.GetComponent<PlayerAI>() : null;
            if (playerAI == null)
                continue;
            playerAI.ResetPosition();
            playerAI.ResetBlackboard();
            playerAI.ResetBehaviorTree();
        }
    }
}
