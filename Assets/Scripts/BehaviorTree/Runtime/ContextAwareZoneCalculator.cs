using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BehaviorTree.Runtime
{
    public static class ContextAwareZoneCalculator
    {
        /// <summary>
        /// 计算感知当前态势的跑位得分
        /// </summary>
        public static float CalculateContextAwareScore(Vector3 position, PlayerRole role, Vector3 myGoal,
            Vector3 enemyGoal, Vector3 ballPosition, MatchContext context, GameObject player)
        {
            MatchState currentState = DetermineMatchState(player, context);
            float zoneWeight = 50f, ballWeight = 25f, markWeight = 25f;
            float zoneScore = ZoneProbabilitySystem.CalculateNormalizedZoneScore(position, role, myGoal, enemyGoal, currentState) * zoneWeight;
            if (role.RoleType == PlayerRoleType.Defender)
            {

                if (currentState == MatchState.Defending)
                {
                    float ballDistanceBase = 10f;
                    float ballScore = Mathf.Clamp01((ballDistanceBase - Vector3.Distance(position, ballPosition)) / ballDistanceBase) * ballWeight;
                    float markScore = CalculateNormalizedMarkScore(position, role, context, myGoal, player) * markWeight;
                    return zoneScore + ballScore + markScore;
                }
                else
                {
                    float ballDistanceBase = 40f;
                    float ballScore = Mathf.Clamp01((ballDistanceBase - Vector3.Distance(position, ballPosition)) / ballDistanceBase) * ballWeight;
                    float markScore = CalculateNormalizedMarkScore(position, role, context, myGoal, player) * markWeight;
                    return zoneScore + ballScore + markScore;
                }
            }
            else if (role.RoleType == PlayerRoleType.Forward)
            {
                if (currentState == MatchState.Attacking)
                {
                    zoneWeight = 50f;float goalWeight = 25f, avoidEnemyWeight = 25f;
                    zoneScore = ZoneProbabilitySystem.CalculateNormalizedZoneScore(position, role, myGoal, enemyGoal, currentState) * zoneWeight;
                    float goalScore = CalculateNormalizedGoalScore(position, role, context, enemyGoal, player) * goalWeight;
                    float avoidEnemyScore = CalculateNormalizedAvoidEnemyScore(position, role, context, player) * avoidEnemyWeight;
                    return zoneScore + goalScore + avoidEnemyScore;
                }
                else if (currentState == MatchState.ChasingBall || currentState == MatchState.Defending)
                {
                    float ballDistanceBase = 40f;
                    float ballScore = Mathf.Clamp01((ballDistanceBase - Vector3.Distance(position, ballPosition)) / ballDistanceBase) * ballWeight;
                    float markScore = CalculateNormalizedMarkScore(position, role, context, myGoal, player) * markWeight;
                    return zoneScore + ballScore + markScore;
                }
            }

            // 以下，计算与当前比赛实情相关的bonus（进攻或防守阶段）
            float contextBonus = 0f;
            float distToBall = Vector3.Distance(position, ballPosition);
            if (distToBall < 10f)
            {
                contextBonus += (1f - distToBall / 10f) * 20f; // 接近球的bonus
            }
            if (context.GetBallHolder() == null && IsClosestToBall(player, context))
            {
                contextBonus += 50f; // 最接近无主球的额外bonus
            }
            if (currentState == MatchState.Defending)
            {
                if (IsBetweenBallAndGoal(position, ballPosition, myGoal))
                {
                    contextBonus += 15f; // 防守时，在球和门之间的额外bonus
                }
            }
            if (currentState == MatchState.Attacking)
            {
                Vector3 toEnemyGoal = (enemyGoal - ballPosition).normalized; // 球指向对方球门和球指向待评估位置的方向向量，夹角小于60度
                Vector3 toPosition = (position - ballPosition).normalized;
                float alignment = Vector3.Dot(toEnemyGoal, toPosition);
                if (alignment > 0.5f)
                {
                    contextBonus += 10f * alignment;
                }
            }

            return zoneScore + contextBonus;
        }

        private static float CalculateNormalizedMarkScore(Vector3 position, PlayerRole role, MatchContext context, Vector3 myGoal,
            GameObject player)
        {
            float enemyDistacneBase = 5f;
            List<GameObject> enemies = context.GetOpponents(player);
            List<EnemyInfo> enemiesWithDist = new();
            float normalizedMarkScore = 0f;
            int concernCount = 3;
            foreach (var enemy in enemies)
            {
                enemiesWithDist.Add(new EnemyInfo()
                {
                    Position = enemy.transform.position,
                    Distance = Vector3.Distance(myGoal, enemy.transform.position)
                });
            }
            enemiesWithDist.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            for (int i = 0; i < Mathf.Min(concernCount, enemiesWithDist.Count); i++)
            {
                normalizedMarkScore += Mathf.Clamp01(1f - Vector3.Distance(position, enemiesWithDist[i].Position) / enemyDistacneBase);
            }
            normalizedMarkScore /= concernCount;
            return normalizedMarkScore;
        }

        public static Vector3 FindBestPosition(PlayerRole role, Vector3 currentPosition, Vector3 myGoal,
            Vector3 enemyGoal, Vector3 ballPosition, MatchContext context, GameObject player,
            List<GameObject> teammates, List<GameObject> enemies, FootballBlackboard blackboard = null)
        {
            List<Vector3> candidatePositions = GenerateCandidatePositions(player, context, 
                role, currentPosition, myGoal, enemyGoal, ballPosition);
            List<Vector3> bestCandidates = new List<Vector3>();
            float bestScore = float.MinValue;
            if (blackboard.DebugShowCandidates)
            {
                blackboard.DebugCandidatePositions = new List<CandidatePosition>();
            }
            StringBuilder sb = new StringBuilder();
            sb.Append($"{player.name} candidate\n");
            foreach (var candidate in candidatePositions)
            {
                float roleScore = CalculateContextAwareScore(candidate, role, myGoal, enemyGoal, ballPosition, context, player);
                float avoidOverlapScore = 0f;
                float totalScore = roleScore + avoidOverlapScore;
                if (blackboard.DebugShowCandidates)
                {
                    blackboard.DebugCandidatePositions.Add(new CandidatePosition(candidate, roleScore, avoidOverlapScore));
                }
                if (totalScore > bestScore)
                {
                    bestScore = totalScore;
                    bestCandidates.Clear();
                    bestCandidates.Add(candidate);
                }
                else if (Mathf.Abs(totalScore - bestScore) < FootballConstants.FloatEpsilon)
                {
                    bestCandidates.Add(candidate);
                }
                sb.Append($"{candidate}-{totalScore}-{avoidOverlapScore}\n");
            }
            Vector3 bestPosition;
            if (bestCandidates.Count == 0) // 如果只有一个最佳点位，则选择它
            {
                bestPosition = currentPosition;
            }
            else // 如果有多个最佳点位，则选择最近的一个
            {
                bestPosition = currentPosition;
                float minDistance = float.MaxValue;
                foreach (var candidate in bestCandidates)
                {
                    float dist = Vector3.Distance(currentPosition, candidate);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        bestPosition = candidate;
                    }
                }
            }
            Debug.Log($"{bestPosition}-{bestScore} (共{bestCandidates.Count}个最高分候选点)\n{sb}");
            return FootballUtils.GetPositionTowards(currentPosition, bestPosition, FootballConstants.DecideMinStep);
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
            if (role.RoleType == PlayerRoleType.Defender)
            {
                return GenerateCandidatePositionsDefender(player, matchContext, role, currentPos, myGoal, enemyGoal, ballPosition);
            }
            if (role.RoleType == PlayerRoleType.Forward)
            {
                return GenerateCandidatePositionsForward(player, matchContext, role, currentPos, myGoal, enemyGoal, ballPosition);
            }
            List<Vector3> candidates = new List<Vector3>();
            // 1 先搜索理想点（最高权重区域的中心）周围的位置
            MatchState state = DetermineMatchState(player, matchContext);
            Vector3 idealPos = ZoneProbabilitySystem.CalculateIdealPosition(role, state, myGoal, enemyGoal);
            List<Vector3> points = GeneratePositionAround(idealPos, 3, 3f, 8);
            candidates.AddRange(points);
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

            if (context.GetBallHolder() == null)
                return MatchState.ChasingBall;

            if (context.GetTeammates(player).Contains(context.GetBallHolder()))
                return MatchState.Attacking;
            else
                return MatchState.Defending;
        }

        public static List<Vector3> GenerateCandidatePositionsDefender(GameObject player, MatchContext matchContext, PlayerRole role,
            Vector3 currentPos, Vector3 myGoal, Vector3 enemyGoal, Vector3 ballPosition)
        {
            MatchState state = DetermineMatchState(player, matchContext);
            List<Vector3> candidates = new List<Vector3>();
            
            if (state == MatchState.Defending || state == MatchState.ChasingBall)// 先考虑防守
            {
                candidates.Add(ballPosition); // 1 考虑向球跑
                candidates.AddRange(GenerateTwoPointsBetweenPoints(ballPosition, myGoal, 3)); // 2 封堵射门角度
                Vector3 idealPos = ZoneProbabilitySystem.CalculateIdealPosition(role, state, myGoal, enemyGoal); // 区域中心位置
                candidates.AddRange(GeneratePositionAround(idealPos, 4, 3f, 8)); // 3 考虑向理想位置或周围跑
                candidates.Add(idealPos);
                candidates.AddRange(FindNearEnemies(idealPos, matchContext.GetOpponents(player), 3));
            }
            else if (state == MatchState.Attacking)
            {
                candidates.Add(ballPosition); //  1 考虑向球跑
                Vector3 idealPos = ZoneProbabilitySystem.CalculateIdealPosition(role, state, myGoal, enemyGoal); // 区域中心位置
                candidates.AddRange(GeneratePositionAround(idealPos, 2, 6f, 8)); // 3 考虑向理想位置或周围跑
                candidates.Add(idealPos);
                List<Vector3> otherZonePos = ZoneProbabilitySystem.CalculateOtherZonePositions(role, state, myGoal, enemyGoal, idealPos);
                candidates.AddRange(otherZonePos); // 4 进攻时考虑其他区域
            }
            return candidates;
        }

        /// <summary>
        /// 生成前锋的候选跑位
        /// </summary>
        public static List<Vector3> GenerateCandidatePositionsForward(GameObject player, MatchContext matchContext, PlayerRole role,
            Vector3 currentPos, Vector3 myGoal, Vector3 enemyGoal, Vector3 ballPosition)
        {
            MatchState state = DetermineMatchState(player, matchContext);
            List<Vector3> candidates = new List<Vector3>();

            if (state == MatchState.Attacking)
            {
                candidates.Add(CalculatePenaltyAreaFront(enemyGoal)); // 1 禁区前沿（寻找射门机会）
                candidates.AddRange(GeneratePositionAround(CalculatePenaltyAreaFront(enemyGoal), 2, 2f, 6)); // 禁区周围

                GameObject ballHolder = matchContext.GetBallHolder();
                if (ballHolder != null && ballHolder != player)
                {
                    candidates.Add(CalculateSupportingPosition(ballHolder.transform.position, enemyGoal)); // 2 接应持球人
                    candidates.Add(CalculateSpacePosition(currentPos, matchContext.GetOpponents(player))); // 3 拉开空间
                }

                Vector3 idealPos = ZoneProbabilitySystem.CalculateIdealPosition(role, state, myGoal, enemyGoal);
                candidates.AddRange(GeneratePositionAround(idealPos, 2, 4f, 8)); // 4 理想区域周围
                candidates.Add(idealPos);
            }
            else if (state == MatchState.Defending || state == MatchState.ChasingBall)
            {
                candidates.Add(ballPosition); // 1 向球跑

                GameObject ballHolder = matchContext.GetBallHolder();
                if (ballHolder != null)
                {
                    candidates.Add(CalculatePressingPosition(ballHolder.transform.position, myGoal)); // 2 适度前压
                }

                Vector3 idealPos = ZoneProbabilitySystem.CalculateIdealPosition(role, state, myGoal, enemyGoal);
                candidates.AddRange(GeneratePositionAround(idealPos, 2, 3f, 8)); // 3 理想区域周围
                candidates.Add(idealPos);
            }
            return candidates;
        }

        private static float CalculateNormalizedGoalScore(Vector3 position, PlayerRole role, MatchContext context, Vector3 enemyGoal, GameObject player)
        {
            // < 8m
            float distance = Vector3.Distance(position, enemyGoal);
            if (distance > 8f) return 0f;
            float normalizedScore = 1f - distance / 8f;
            return normalizedScore;
        }
        
        private static float CalculateNormalizedAvoidEnemyScore(Vector3 position, PlayerRole role, MatchContext context, GameObject player)
        {
            float normalizedAvoidScore = 1f;
            foreach (GameObject enemy in context.GetOpponents(player))
            {
                float distance = Vector3.Distance(position, enemy.transform.position);
                if (distance < 3f)
                {
                    normalizedAvoidScore += 1f - distance / 3f;
                }
            }
            return Mathf.Max(0, normalizedAvoidScore);
        }

        private static List<Vector3> GeneratePositionAround(Vector3 position, int layers, float layerWidth, int pointsPerLayer)
        {
            List<Vector3> points = new List<Vector3>();
            for (int layer = 1; layer <= layers; layer++)
            {
                float radius = layer * layerWidth;
                for (int i = 0; i < pointsPerLayer; i++)
                {
                    float angle = (360f / pointsPerLayer) * i;
                    Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * radius;
                    Vector3 candidate = position + offset;
                    points.Add(candidate);
                }
            }
            return points;
        }
        // 两个点之间的点作为候选点
        private static List<Vector3> GenerateTwoPointsBetweenPoints(Vector3 p1, Vector3 p2, int numPoints)
        {
            List<Vector3> points = new List<Vector3>();
            Vector3 direction = (p2 - p1).normalized;
            float distance = Vector3.Distance(p1, p2);
            for (int i = 1; i <= numPoints; i++)
            {
                Vector3 point = p1 + direction * (i/(float)numPoints * distance);
                points.Add(point);
            }
            return points;
        }

        // === 前锋跑位辅助方法 ===

        /// <summary>
        /// 计算禁区前沿位置（距离球门8米，用于寻找射门机会）
        /// </summary>
        private static Vector3 CalculatePenaltyAreaFront(Vector3 enemyGoal)
        {
            Vector3 goalToCenter = Vector3.zero - enemyGoal;
            goalToCenter.y = 0;
            return enemyGoal + goalToCenter.normalized * 8f;
        }

        /// <summary>
        /// 计算接应位置（在持球人前方6米，用于接应传球）
        /// </summary>
        private static Vector3 CalculateSupportingPosition(Vector3 holderPos, Vector3 enemyGoal)
        {
            Vector3 holderToGoal = (enemyGoal - holderPos).normalized;
            holderToGoal.y = 0;
            return holderPos + holderToGoal * 6f;
        }

        /// <summary>
        /// 计算拉开空间位置（远离最近防守者4米，用于创造跑动空间）
        /// </summary>
        private static Vector3 CalculateSpacePosition(Vector3 currentPos, List<GameObject> opponents)
        {
            GameObject nearestOpponent = null;
            float minDistance = float.MaxValue;

            foreach (var opp in opponents)
            {
                float dist = Vector3.Distance(currentPos, opp.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearestOpponent = opp;
                }
            }

            if (nearestOpponent != null)
            {
                Vector3 awayFromOpponent = (currentPos - nearestOpponent.transform.position).normalized;
                awayFromOpponent.y = 0;
                return currentPos + awayFromOpponent * 4f;
            }

            return currentPos;
        }

        /// <summary>
        /// 计算前压位置（中场附近，距持球人8米，用于适度前压拦截）
        /// </summary>
        private static Vector3 CalculatePressingPosition(Vector3 holderPos, Vector3 myGoal)
        {
            Vector3 holderToMyGoal = (myGoal - holderPos).normalized;
            holderToMyGoal.y = 0;
            return holderPos + holderToMyGoal * 8f;
        }

        struct EnemyInfo
        {
            public float Distance;
            public Vector3 Position;
        }
        private static List<Vector3> FindNearEnemies(Vector3 curPos, List<GameObject> enemies, int cnt)
        {
            List<EnemyInfo> points = new List<EnemyInfo>();
            foreach (var enemy in enemies)
            {
                points.Add(new EnemyInfo
                {
                    Distance = Vector3.Distance(curPos, enemy.transform.position),
                    Position = enemy.transform.position
                });
            }
            points.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            return points.Take(cnt).Select(x => x.Position).ToList();
        }

        # region 通用工具方法
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
        #endregion
    }
}
