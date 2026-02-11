using System;
using System.Collections.Generic;
using UnityEngine;
using BehaviorTree.Runtime;

public class PossessionRefereeSystem
{
    private readonly List<GameObject> _closestPlayersBuffer = new List<GameObject>();

    public void UpdatePassTargetState(MatchContext context, float passTimeout)
    {
        context.UpdatePassTarget(passTimeout, context.GetBallHolder());
    }

    public void UpdatePossessionState(MatchContext context, GameObject ball, float possessionThreshold, Func<GameObject, bool> isStunned, Action<GameObject, GameObject> logPossessionChange)
    {
        context.UpdateStealCooldown(TimeManager.Instance.GetDeltaTime());
        if (context.BallController.GetIsMoving())
            context.SetBallHolder(null);
        if (context.GetBallHolder() != null)
            return;
        _closestPlayersBuffer.Clear();
        float minDistance = float.MaxValue;
        float distanceTolerance = 0.001f;
        AddClosestPlayers(context.TeamRedPlayers, context, ball, possessionThreshold, isStunned, _closestPlayersBuffer, ref minDistance, distanceTolerance);
        AddClosestPlayers(context.TeamBluePlayers, context, ball, possessionThreshold, isStunned, _closestPlayersBuffer, ref minDistance, distanceTolerance);
        GameObject previousHolder = context.GetBallHolder();
        GameObject closestPlayer = SelectClosestPlayer(_closestPlayersBuffer);
        logPossessionChange(previousHolder, closestPlayer);
        context.SetBallHolder(closestPlayer);
    }

    public void CheckGoal(GameObject ball, Transform redGoal, Transform blueGoal, float goalDistance, Action<string> onGoalScored)
    {
        Vector3 ballPosition = ball.transform.position;
        if (Vector3.Distance(ballPosition, redGoal.position) < goalDistance)
        {
            onGoalScored("Blue");
            return;
        }
        if (Vector3.Distance(ballPosition, blueGoal.position) < goalDistance)
        {
            onGoalScored("Red");
            return;
        }
    }

    public bool TryHandleBallOutOfBounds(MatchContext context, GameObject ball, List<GameObject> teamRedPlayers, List<GameObject> teamBluePlayers, GameObject redStartPlayer, GameObject blueStartPlayer, out Vector3 outOfBoundsPosition, out string throwingTeam, out GameObject throwInPlayer)
    {
        outOfBoundsPosition = Vector3.zero;
        throwingTeam = "Red";
        throwInPlayer = null;
        if (context.IsInField(ball.transform.position))
            return false;
        outOfBoundsPosition = ball.transform.position;
        GameObject lastKicker = context.BallController.GetLastKicker();
        if (lastKicker != null)
        {
            if (teamRedPlayers.Contains(lastKicker))
                throwingTeam = "Blue";
            else if (teamBluePlayers.Contains(lastKicker))
                throwingTeam = "Red";
        }
        if (throwingTeam == "Red" && redStartPlayer != null)
            throwInPlayer = redStartPlayer;
        else if (throwingTeam == "Blue" && blueStartPlayer != null)
            throwInPlayer = blueStartPlayer;
        return true;
    }

    public void SetupThrowInPositions(MatchContext context, GameObject ball, GameObject throwInPlayer, Vector3 outOfBoundsPosition)
    {
        if (throwInPlayer != null)
            throwInPlayer.transform.position = FootballUtils.ClampToField(context, outOfBoundsPosition, out bool wasClamped);
        ball.transform.position = FootballUtils.ClampToField(context, outOfBoundsPosition, out bool clamped);
        context.SetBallHolder(throwInPlayer);
        if (throwInPlayer != null)
            throwInPlayer.GetComponent<PlayerAI>().GetBlackboard().IsPassingOutsideBall = true;
    }

    public void StealBall(MatchContext context, GameObject ball, GameObject tackler, GameObject currentHolder, float stealCooldownDuration)
    {
        ball.transform.position = tackler.transform.position;
        context.SetBallHolder(tackler);
        context.SetStealCooldown(stealCooldownDuration);
        if (currentHolder == null)
            return;
        PlayerAI holderAI = currentHolder.GetComponent<PlayerAI>();
        if (holderAI == null)
            return;
        FootballBlackboard blackboard = holderAI.GetBlackboard();
        if (blackboard == null)
            return;
        blackboard.IsStunned = true;
        blackboard.StunTimer = blackboard.StunDuration;
    }

    private static void AddClosestPlayers(List<GameObject> players, MatchContext context, GameObject ball, float possessionThreshold, Func<GameObject, bool> isStunned, List<GameObject> closestPlayers, ref float minDistance, float distanceTolerance)
    {
        for (int i = 0; i < players.Count; i++)
        {
            GameObject player = players[i];
            if (player == null)
                continue;
            float distance = Vector3.Distance(player.transform.position, ball.transform.position);
            if (distance >= possessionThreshold || player == context.BallController.GetRecentKicker() || isStunned(player))
                continue;
            if (distance < minDistance - distanceTolerance)
            {
                minDistance = distance;
                closestPlayers.Clear();
                closestPlayers.Add(player);
            }
            else if (distance <= minDistance + distanceTolerance)
            {
                closestPlayers.Add(player);
            }
        }
    }

    private static GameObject SelectClosestPlayer(List<GameObject> closestPlayers)
    {
        if (closestPlayers.Count == 0)
            return null;
        GameObject closestPlayer = closestPlayers[0];
        for (int i = 1; i < closestPlayers.Count; i++)
        {
            GameObject candidate = closestPlayers[i];
            if (string.CompareOrdinal(candidate.name, closestPlayer.name) < 0)
                closestPlayer = candidate;
        }
        return closestPlayer;
    }
}
