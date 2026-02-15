using System;
using System.Collections.Generic;
using UnityEngine;
using BehaviorTree.Runtime;

namespace FootballAI.FootballCore
{
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
        bool isBallMoving = context.BallController.GetIsMoving();
        if (isBallMoving)
            context.SetBallHolder(null);
        if (context.GetBallHolder() != null)
            return;
        _closestPlayersBuffer.Clear();
        float minDistance = float.MaxValue;
        float distanceTolerance = 0.001f;
        Vector3 ballFlightDirection = isBallMoving ? context.BallController.GetFlightDirection() : Vector3.zero;
        AddClosestPlayers(context.TeamRedPlayers, context, ball, possessionThreshold, isStunned, _closestPlayersBuffer, ref minDistance, distanceTolerance, isBallMoving, ballFlightDirection);
        AddClosestPlayers(context.TeamBluePlayers, context, ball, possessionThreshold, isStunned, _closestPlayersBuffer, ref minDistance, distanceTolerance, isBallMoving, ballFlightDirection);
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

    public bool TryHandleBallOutOfBounds(MatchContext context, GameObject ball, List<GameObject> teamRedPlayers, List<GameObject> teamBluePlayers,
        out Vector3 outOfBoundsPosition, out RestartType restartType, out string restartTeam, out GameObject restartPlayer, out Vector3 restartPosition)
    {
        outOfBoundsPosition = Vector3.zero;
        restartType = RestartType.ThrowIn;
        restartTeam = "Red";
        restartPlayer = null;
        restartPosition = Vector3.zero;
        if (context.IsInField(ball.transform.position))
            return false;
        outOfBoundsPosition = ball.transform.position;
        GameObject lastKicker = context.BallController.GetLastKicker();
        string lastKickerTeam = GetTeamByPlayer(lastKicker, teamRedPlayers, teamBluePlayers);
        float left = context.GetLeftBorder();
        float right = context.GetRightBorder();
        float forward = context.GetForwardBorder();
        float backward = context.GetBackwardBorder();
        bool outOnSideLine = outOfBoundsPosition.x <= left || outOfBoundsPosition.x >= right;
        if (outOnSideLine)
        {
            restartType = RestartType.ThrowIn;
            restartTeam = GetOppositeTeam(lastKickerTeam);
            restartPosition = GetThrowInRestartPosition(context, outOfBoundsPosition);
        }
        else
        {
            string defendingTeam = DetermineDefendingTeamAtGoalLine(context, outOfBoundsPosition, forward, backward);
            if (lastKickerTeam == defendingTeam)
            {
                restartType = RestartType.CornerKick;
                restartTeam = GetOppositeTeam(defendingTeam);
                restartPosition = GetCornerKickPosition(context, outOfBoundsPosition, defendingTeam, left, right, forward, backward);
            }
            else
            {
                restartType = RestartType.GoalKick;
                restartTeam = defendingTeam;
                restartPosition = GetGoalKickPosition(context, defendingTeam, forward, backward);
            }
        }
        List<GameObject> restartTeamPlayers = restartTeam == "Red" ? teamRedPlayers : teamBluePlayers;
        restartPlayer = FindNearestPlayer(restartTeamPlayers, restartPosition, null);
        return true;
    }

    public void SetupRestartPositions(MatchContext context, GameObject ball, List<GameObject> teamRedPlayers, List<GameObject> teamBluePlayers,
        GameObject restartPlayer, Vector3 restartPosition, RestartType restartType, string restartTeam)
    {
        List<GameObject> teammates = restartTeam == "Red" ? teamRedPlayers : teamBluePlayers;
        List<GameObject> opponents = restartTeam == "Red" ? teamBluePlayers : teamRedPlayers;
        float attackSign = GetAttackSign(context, restartTeam, teammates);
        float inFieldSign = restartType == RestartType.CornerKick ? -attackSign : attackSign;
        Vector3 clampedRestartPosition = FootballUtils.ClampToField(context, restartPosition, out bool _);
        if (restartPlayer != null)
            restartPlayer.transform.position = clampedRestartPosition;
        ball.transform.position = clampedRestartPosition;
        context.SetBallHolder(restartPlayer);
        if (restartPlayer != null)
        {
            PlayerAI playerAI = restartPlayer.GetComponent<PlayerAI>();
            FootballBlackboard blackboard = playerAI != null ? playerAI.GetBlackboard() : null;
            if (blackboard != null)
                blackboard.IsPassingOutsideBall = true;
        }
        SetupTeammateRestartPositions(context, teammates, restartPlayer, ball.transform.position, restartType, inFieldSign);
        SetupOpponentRestartPositions(context, opponents, ball.transform.position, restartType, inFieldSign);
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

    private static void AddClosestPlayers(List<GameObject> players, MatchContext context, GameObject ball, float possessionThreshold, Func<GameObject, bool> isStunned, List<GameObject> closestPlayers, ref float minDistance, float distanceTolerance, bool isBallMoving, Vector3 ballFlightDirection)
    {
        for (int i = 0; i < players.Count; i++)
        {
            GameObject player = players[i];
            if (player == null)
                continue;
            float distance = Vector3.Distance(player.transform.position, ball.transform.position);
            if (distance >= possessionThreshold || player == context.BallController.GetRecentKicker() || isStunned(player))
                continue;
            if (isBallMoving && !IsInBallFlightForwardDirection(ball.transform.position, player.transform.position, ballFlightDirection))
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

    private static bool IsInBallFlightForwardDirection(Vector3 ballPosition, Vector3 playerPosition, Vector3 flightDirection)
    {
        if (flightDirection.sqrMagnitude <= FootballConstants.FloatEpsilon)
            return true;
        Vector3 toPlayer = playerPosition - ballPosition;
        toPlayer.y = 0f;
        Vector3 planarFlightDirection = flightDirection;
        planarFlightDirection.y = 0f;
        if (toPlayer.sqrMagnitude <= FootballConstants.FloatEpsilon || planarFlightDirection.sqrMagnitude <= FootballConstants.FloatEpsilon)
            return true;
        return Vector3.Dot(planarFlightDirection, toPlayer) > 0f;
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

    private static string GetTeamByPlayer(GameObject player, List<GameObject> teamRedPlayers, List<GameObject> teamBluePlayers)
    {
        if (player == null)
            return "Red";
        if (teamRedPlayers.Contains(player))
            return "Red";
        if (teamBluePlayers.Contains(player))
            return "Blue";
        return "Red";
    }

    private static string GetOppositeTeam(string team)
    {
        return team == "Red" ? "Blue" : "Red";
    }

    private static string DetermineDefendingTeamAtGoalLine(MatchContext context, Vector3 outOfBoundsPosition, float forward, float backward)
    {
        if (Mathf.Abs(outOfBoundsPosition.z - forward) < Mathf.Abs(outOfBoundsPosition.z - backward))
            return context.RedGoal.position.z > context.BlueGoal.position.z ? "Red" : "Blue";
        return context.RedGoal.position.z < context.BlueGoal.position.z ? "Red" : "Blue";
    }

    private static Vector3 GetThrowInRestartPosition(MatchContext context, Vector3 outOfBoundsPosition)
    {
        float x = Mathf.Clamp(outOfBoundsPosition.x, context.GetLeftBorder(), context.GetRightBorder());
        float z = Mathf.Clamp(outOfBoundsPosition.z, context.GetBackwardBorder(), context.GetForwardBorder());
        return new Vector3(x, outOfBoundsPosition.y, z);
    }

    private static Vector3 GetCornerKickPosition(MatchContext context, Vector3 outOfBoundsPosition, string defendingTeam, float left, float right, float forward, float backward)
    {
        bool useRightCorner = Mathf.Abs(outOfBoundsPosition.x - right) < Mathf.Abs(outOfBoundsPosition.x - left);
        float cornerX = useRightCorner ? right - FootballConstants.CornerKickInFieldOffset : left + FootballConstants.CornerKickInFieldOffset;
        float defendingGoalZ = defendingTeam == "Red" ? context.RedGoal.position.z : context.BlueGoal.position.z;
        float goalLineZ = defendingGoalZ > 0f ? forward : backward;
        float inFieldZ = goalLineZ - Mathf.Sign(goalLineZ) * FootballConstants.CornerKickInFieldOffset;
        return new Vector3(cornerX, outOfBoundsPosition.y, inFieldZ);
    }

    private static Vector3 GetGoalKickPosition(MatchContext context, string defendingTeam, float forward, float backward)
    {
        float defendingGoalZ = defendingTeam == "Red" ? context.RedGoal.position.z : context.BlueGoal.position.z;
        float goalLineZ = defendingGoalZ > 0f ? forward : backward;
        float inFieldZ = goalLineZ - Mathf.Sign(goalLineZ) * FootballConstants.GoalKickInFieldOffset;
        return new Vector3(0f, 0f, inFieldZ);
    }

    private static float GetAttackSign(MatchContext context, string team, List<GameObject> teammates)
    {
        if (teammates != null && teammates.Count > 0 && teammates[0] != null)
        {
            float enemyGoalZ = context.GetEnemyGoalPosition(teammates[0]).z;
            return Mathf.Sign(enemyGoalZ);
        }
        float z = team == "Red" ? context.BlueGoal.position.z : context.RedGoal.position.z;
        return Mathf.Sign(z);
    }

    private static void SetupTeammateRestartPositions(MatchContext context, List<GameObject> teammates, GameObject restartPlayer,
        Vector3 restartPosition, RestartType restartType, float inFieldSign)
    {
        List<GameObject> nearest = FindNearestPlayers(teammates, restartPosition, restartPlayer, 3);
        float sideSign = restartPosition.x >= 0f ? -1f : 1f;
        for (int i = 0; i < nearest.Count; i++)
        {
            GameObject player = nearest[i];
            Vector3 target;
            if (restartType == RestartType.GoalKick)
            {
                float lateralIndex = i - 1f;
                float xOffset = lateralIndex * FootballConstants.RestartLateralOffset * 1.5f;
                float zOffset = inFieldSign * FootballConstants.RestartSupportNearDistance;
                target = restartPosition + new Vector3(xOffset, 0f, zOffset);
            }
            else if (restartType == RestartType.CornerKick)
            {
                Vector3 cornerDirection = GetCornerDirection(restartPosition, inFieldSign);
                Vector3 lineDirection = GetPerpendicularDirection(cornerDirection);
                float lateralIndex = i - (nearest.Count - 1) * 0.5f;
                Vector3 lineCenter = restartPosition + cornerDirection * FootballConstants.RestartSupportNearDistance;
                target = lineCenter + lineDirection * (lateralIndex * FootballConstants.RestartLateralOffset);
            }
            else
            {
                float xDistance = FootballConstants.RestartSupportNearDistance;
                target = restartPosition + new Vector3(sideSign * xDistance, 0f, i * inFieldSign * FootballConstants.RestartLateralOffset);
            }
            player.transform.position = FootballUtils.ClampToField(context, target, out bool _);
        }
    }

    private static void SetupOpponentRestartPositions(MatchContext context, List<GameObject> opponents, Vector3 restartPosition,
        RestartType restartType, float inFieldSign)
    {
        List<GameObject> nearest = FindNearestPlayers(opponents, restartPosition, null, 3);
        float sideSign = restartPosition.x >= 0f ? -1f : 1f;
        for (int i = 0; i < nearest.Count; i++)
        {
            GameObject player = nearest[i];
            Vector3 target;
            if (restartType == RestartType.GoalKick)
            {
                float lateralIndex = i - 1f;
                float xOffset = lateralIndex * FootballConstants.RestartLateralOffset * 1.5f;
                float zOffset =  inFieldSign * FootballConstants.RestartSupportNearDistance * 2;
                target = restartPosition + new Vector3(xOffset, 0f, zOffset);
            }
            else if (restartType == RestartType.CornerKick)
            {
                Vector3 cornerDirection = GetCornerDirection(restartPosition, inFieldSign);
                Vector3 lineDirection = GetPerpendicularDirection(cornerDirection);
                float lateralIndex = i - (nearest.Count - 1) * 0.5f;
                Vector3 lineCenter = restartPosition + cornerDirection * FootballConstants.RestartSupportFarDistance;
                target = lineCenter + lineDirection * (lateralIndex * FootballConstants.RestartLateralOffset);
            }
            else
            {
                float xDistance = FootballConstants.RestartSupportNearDistance * 2f;
                target = restartPosition + new Vector3(sideSign * xDistance, 0f, i * inFieldSign * FootballConstants.RestartLateralOffset);
            }
            player.transform.position = FootballUtils.ClampToField(context, target, out bool _);
        }
    }

    private static Vector3 GetCornerDirection(Vector3 restartPosition, float inFieldSign)
    {
        float inwardXSign = restartPosition.x >= 0f ? -1f : 1f;
        return new Vector3(inwardXSign, 0f, inFieldSign).normalized;
    }

    private static Vector3 GetPerpendicularDirection(Vector3 direction)
    {
        return new Vector3(-direction.z, 0f, direction.x).normalized;
    }

    private static List<GameObject> FindNearestPlayers(List<GameObject> players, Vector3 pivot, GameObject excludePlayer, int count)
    {
        List<GameObject> sorted = new List<GameObject>();
        for (int i = 0; i < players.Count; i++)
        {
            GameObject player = players[i];
            if (player == null || player == excludePlayer)
                continue;
            sorted.Add(player);
        }
        sorted.Sort((a, b) => Vector3.Distance(a.transform.position, pivot).CompareTo(Vector3.Distance(b.transform.position, pivot)));
        if (sorted.Count > count)
            sorted.RemoveRange(count, sorted.Count - count);
        return sorted;
    }

    private static GameObject FindNearestPlayer(List<GameObject> players, Vector3 pivot, GameObject excludePlayer)
    {
        GameObject nearest = null;
        float minDistance = float.MaxValue;
        for (int i = 0; i < players.Count; i++)
        {
            GameObject player = players[i];
            if (player == null || player == excludePlayer)
                continue;
            float distance = Vector3.Distance(player.transform.position, pivot);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = player;
            }
        }
        return nearest;
    }
}
}
