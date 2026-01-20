using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace BehaviorTree.Runtime
{
    public static class ContextAwareZoneCalculator
    {
        /// <summary>
        /// 计算感知当前态势的跑位得分
        /// </summary>
        /// <param name="position"></param>
        /// <param name="role"></param>
        /// <param name="myGoal"></param>
        /// <param name="enemyGoal"></param>
        /// <param name="ballPosition"></param>
        /// <param name="context"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        public static float CalculateContextAwareScore(Vector3 position, PlayerRole role, Vector3 myGoal,
            Vector3 enemyGoal, Vector3 ballPosition, MatchContext context, GameObject player)
        {
            MatchState currentState = DetermineMatchState(player, context);
            float zoneScore = ZoneProbabilitySystem.CalculateZonePreferenceScore(
                position, role, myGoal, enemyGoal, currentState);

            // 以下，计算与当前比赛实情相关的bonus（进攻或防守阶段）
            float contextBonus = 0f;
            float distToBall = Vector3.Distance(position, ballPosition);
            if (distToBall < 10f)
            {
                contextBonus += (1f - distToBall / 10f) * 20f;
            }
            if (context.BallHolder == null && IsClosestToBall(player, context))
            {
                contextBonus += 50f;
            }
            if (currentState == MatchState.Defending)
            {
                if (IsBetweenBallAndGoal(position, ballPosition, myGoal))
                {
                    contextBonus += 15f;
                }
            }
            if (currentState == MatchState.Attacking)
            {
                Vector3 toEnemyGoal = (enemyGoal - ballPosition).normalized;
                Vector3 toPosition = (position - ballPosition).normalized;
                float alignment = Vector3.Dot(toEnemyGoal, toPosition);
                if (alignment > 0.5f)
                {
                    contextBonus += 10f * alignment;
                }
            }

            return zoneScore + contextBonus;
        }

        public static Vector3 FindBestPosition(PlayerRole role, Vector3 currentPosition, Vector3 myGoal,
            Vector3 enemyGoal, Vector3 ballPosition, MatchContext context, GameObject player,
            List<GameObject> teammates, List<GameObject> enemies)
        {
            List<Vector3> candidatePositions = GenerateCandidatePositions(player, context,
                role, currentPosition, myGoal, enemyGoal, ballPosition);

            Vector3 bestPosition = currentPosition;
            float bestScore = float.MinValue;
            StringBuilder sb = new StringBuilder();
            sb.Append($"{player.name} ");
            foreach (var candidate in candidatePositions)
            {
                float roleScore = CalculateContextAwareScore(candidate, role, myGoal, enemyGoal, ballPosition, context, player);
                float avoidOverlapScore = CalculateAvoidOverlapScore(candidate, player, teammates, enemies);
                float totalScore = roleScore + avoidOverlapScore;
                if (totalScore > bestScore)
                {
                    bestScore = totalScore;
                    bestPosition = candidate;
                }
                sb.Append($"{candidate}-{roleScore}-{avoidOverlapScore} ");
            }
            Debug.Log(sb.ToString());

            return bestPosition;
        }
        /// <summary>
        /// 生成候选跑位
        /// </summary>
        /// <param name="player"></param>
        /// <param name="matchContext"></param>
        /// <param name="role"></param>
        /// <param name="currentPos"></param>
        /// <param name="myGoal"></param>
        /// <param name="enemyGoal"></param>
        /// <param name="ballPosition"></param>
        /// <returns></returns>
        private static List<Vector3> GenerateCandidatePositions(GameObject player, MatchContext matchContext,
            PlayerRole role, Vector3 currentPos, Vector3 myGoal, Vector3 enemyGoal, Vector3 ballPosition)
        {
            List<Vector3> candidates = new List<Vector3>();
            // 先搜索理想点（最高权重区域的中心）周围的位置
            MatchState state = DetermineMatchState(player, matchContext);
            Vector3 idealPos = ZoneProbabilitySystem.CalculateIdealPosition(role, state, myGoal, enemyGoal);
            int layers = 3;
            int pointsPerLayer = 8;
            for (int layer = 1; layer <= layers; layer++)
            {
                float radius = layer * 3f;
                for (int i = 0; i < pointsPerLayer; i++)
                {
                    float angle = (360f / pointsPerLayer) * i;
                    Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * radius;
                    Vector3 candidate = idealPos + offset;
                    candidate.y = currentPos.y;
                    candidates.Add(candidate);
                }
            }
            candidates.Add(currentPos);
            // 搜索靠近球4m或8m的位置
            Vector3 toBall = (ballPosition - idealPos).normalized;
            for (int i = 1; i <= 2; i++)
            {
                Vector3 towardsBall = idealPos + toBall * (i * 4f);
                towardsBall.y = currentPos.y;
                candidates.Add(towardsBall);
            }

            return candidates;
        }

        private static float CalculateAvoidOverlapScore(Vector3 position, GameObject player, 
            List<GameObject> teammates, List<GameObject> enemies)
        {
            float penalty = 0f;
            float minDistance = 2f;

            foreach (var teammate in teammates)
            {
                if (teammate == player) continue;
                float dist = Vector3.Distance(position, teammate.transform.position);
                if (dist < minDistance)
                {
                    penalty += (minDistance - dist) * 100f;
                }
            }

            foreach (var enemy in enemies)
            {
                float dist = Vector3.Distance(position, enemy.transform.position);
                if (dist < minDistance)
                {
                    penalty += (minDistance - dist) * 50f;
                }
            }

            return -penalty;
        }

        private static MatchState DetermineMatchState(GameObject player, MatchContext context)
        {
            if (context == null) return MatchState.ChasingBall;

            if (context.BallHolder == null)
                return MatchState.ChasingBall;

            if (context.GetTeammates(player).Contains(context.BallHolder))
                return MatchState.Attacking;
            else
                return MatchState.Defending;
        }

        private static bool IsClosestToBall(GameObject player, MatchContext context)
        {
            var teammates = context.GetTeammates(player);
            Vector3 ballPos = context.Ball.transform.position;
            float myDist = Vector3.Distance(player.transform.position, ballPos);
            foreach (var teammate in teammates)
            {
                if (teammate == player) continue;
                if (Vector3.Distance(teammate.transform.position, ballPos) < myDist - 0.5f)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsBetweenBallAndGoal(Vector3 position, Vector3 ballPos, Vector3 goalPos)
        {
            Vector3 ballToGoal = (goalPos - ballPos).normalized;
            Vector3 ballToPosition = (position - ballPos).normalized;
            float dot = Vector3.Dot(ballToGoal, ballToPosition);
            return dot > 0.3f && Vector3.Distance(position, ballPos) < Vector3.Distance(goalPos, ballPos);
        }
    }
}
