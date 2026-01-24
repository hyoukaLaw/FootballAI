using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BehaviorTree.Runtime
{
    public static class ContextAwareZoneCalculator
    {
        public static float CalculateContextAwareScoreCommon(Vector3 position, PlayerRole role, Vector3 myGoal,
            Vector3 enemyGoal, Vector3 ballPosition, MatchContext context, GameObject player)
        {
            float weightZone = 50f;
            float weightBallDist = 0f, weightGoalDist = 0f, weightMarking = 0f, weightSpace = 0f, weightSafety = 0f;
            MatchState state = DetermineMatchState(player, context);
            if (state == MatchState.Attacking)
            {
                weightBallDist = role.AttackPositionWeight.WeightBallDist; // 球距离
                weightGoalDist = role.AttackPositionWeight.WeightGoalDist;
                weightMarking = role.AttackPositionWeight.WeightMarking;
                weightSpace = role.AttackPositionWeight.WeightSpace;
                weightSafety = role.AttackPositionWeight.WeightSafety;
            }
            else if (state == MatchState.Defending)
            {
                weightBallDist = role.DefendPositionWeight.WeightBallDist;
                weightGoalDist = role.DefendPositionWeight.WeightGoalDist;
                weightMarking = role.DefendPositionWeight.WeightMarking;
                weightSpace = role.DefendPositionWeight.WeightSpace;
                weightSafety = role.DefendPositionWeight.WeightSafety;
            }
            float zoneScore = ZoneProbabilitySystem.CalculateNormalizedZoneScore(position, role, myGoal, enemyGoal, DetermineMatchState(player, context)) * weightZone;
            float ballScore = CalculateBallScore(position, ballPosition) * weightBallDist;
            float goalScore = CalculateGoalScore(position, enemyGoal) * weightGoalDist;
            float markScore = CalculateMarkScore(position, role, context, myGoal, player) * weightMarking;
            float spaceScore = CalculateSpaceScore(position, context, player) * weightSpace;
            float safetyScore = CalculateSafetyScore(position, player, context.GetTeammates(player)) * weightSafety;
            return Mathf.Max(0, zoneScore + ballScore + goalScore + markScore + spaceScore - safetyScore);
        }
        
        public static float CalculateBallScore(Vector3 position, Vector3 ballPosition)
        {
            float ballDistanceBase = 30f;
            float distanceToBall = Vector3.Distance(position, ballPosition);
            return Mathf.Clamp01(1f - distanceToBall / ballDistanceBase);
        }

        public static float CalculateGoalScore(Vector3 position, Vector3 goalPosition)
        {
            float goalDistanceBase = 10f;
            float distanceToGoal = Vector3.Distance(position, goalPosition);
            return Mathf.Clamp01(1f - distanceToGoal / goalDistanceBase);
        }
        
        // 新增：计算空间分 (离最近敌人越远越好)
        private static float CalculateSpaceScore(Vector3 position, MatchContext context, GameObject player)
        {
            float closestDist = float.MaxValue;
            foreach(var enemy in context.GetOpponents(player))
            {
                float d = Vector3.Distance(position, enemy.transform.position);
                if(d < closestDist) closestDist = d;
            }
            // 假设超过 5米 就算很空了
            return Mathf.Clamp01(closestDist / 5f);
        }
        
        private static float CalculateSafetyScore(Vector3 position, GameObject player, 
            List<GameObject> teammates)
        {
            float minSafeDist = 2.0f; // 扩大一点安全距离，比如2米
            float maxOverlapDegree = 0f;
            foreach (var teammate in teammates)
            {
                float dist = Vector3.Distance(position, teammate.transform.position);
                // 只有在安全距离内才计算惩罚
                if (dist < minSafeDist)
                {
                    // 距离越近，重叠程度越高
                    float degree = 1f - (dist / minSafeDist);
                    // 取最严重的那个重叠作为当前点的重叠分
                    if (degree > maxOverlapDegree)
                    {
                        maxOverlapDegree = degree;
                    }
                }
            }
            return maxOverlapDegree;
        }
        /// <summary>
        /// 计算感知当前态势的跑位得分，暂时弃用
        /// </summary>
        public static float CalculateContextScore(Vector3 position, PlayerRole role, Vector3 myGoal,
            Vector3 enemyGoal, Vector3 ballPosition, MatchContext context, GameObject player)
        {
            var currentState = DetermineMatchState(player, context);
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
            return contextBonus;
        }

        private static float CalculateMarkScore(Vector3 position, PlayerRole role, MatchContext context, Vector3 myGoal,
            GameObject player)
        {
            float scoreNormalized = 0f;
            // 只要在协防范围内，每个敌人+0.3分，如果是在敌人和球门的连线 +0.5分，或者敌人和持球人的连线，则+0.5分
            float enemyDistanceBase = 3f, distanceThreshold = 1f;
            float enemyMarkBonus = 0.3f;
            float enemyStrongMarkBonus = 0.5f;
            List<GameObject> enemies = context.GetOpponents(player);
            GameObject ballHolder = context.GetBallHolder();
            foreach (var enemy in enemies)
            {
                if (enemy == ballHolder) continue;
                float distance = Vector3.Distance(position, enemy.transform.position);
                if (distance <= enemyDistanceBase)
                {
                    if (ballHolder != null && CheckIsStrongMark(position, enemy.transform.position, ballHolder.transform.position, myGoal, distanceThreshold))
                        scoreNormalized = scoreNormalized + enemyStrongMarkBonus;
                    else
                        scoreNormalized = scoreNormalized + enemyMarkBonus;
                }
            }
            return Mathf.Clamp01(scoreNormalized); 
            
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
                normalizedMarkScore += Mathf.Clamp01(1f - Vector3.Distance(position, enemiesWithDist[i].Position) / enemyDistanceBase);
            }
            normalizedMarkScore /= concernCount;
            return normalizedMarkScore;
        }

        private static bool CheckIsStrongMark(Vector3 position, Vector3 enemyPosition, Vector3 ballHolderPosition,
            Vector3 myGoal, float distanceThreshold)
        {
            // 1 检查是否在敌人和球门连线上
            if(FootballUtils.DistancePointToLineSegment(enemyPosition, myGoal, ballHolderPosition) < distanceThreshold)
                return true;
            // 2 检查是否在敌人和持球人的连线
            if(FootballUtils.DistancePointToLineSegment(enemyPosition, ballHolderPosition, position) < distanceThreshold)
                return true;
            return false;
        }
        public static Vector3 FindBestPosition(PlayerRole role, Vector3 currentPosition, Vector3 myGoal,
            Vector3 enemyGoal, Vector3 ballPosition, MatchContext context, GameObject player,
            List<GameObject> teammates, List<GameObject> enemies, FootballBlackboard blackboard = null)
        {
            List<Vector3> candidatePositions = GenerateCandidatePositionsCommon(player, context, 
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
                float roleScore = CalculateContextAwareScoreCommon(candidate, role, myGoal, enemyGoal, ballPosition, context, player);
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
                FieldZone zone = ZoneProbabilitySystem.FindHighestWeightZoneAndWeight(role.DefendPreferences).zone;
                candidates.AddRange(GenerateZoneCandidatePositions(ZoneProbabilitySystem.GetZoneRange(zone, enemyGoal, myGoal),1f,1f)); // 3 考虑向理想位置或周围跑
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

        public static List<Vector3> GenerateCandidatePositionsCommon(GameObject player, MatchContext matchContext,
            PlayerRole role, Vector3 currentPos, Vector3 myGoal, Vector3 enemyGoal, Vector3 ballPosition)
        {
            List<Vector3> candidates = new List<Vector3>();
            MatchState currentState = DetermineMatchState(player, matchContext);
            RolePreferences rolePreferences = currentState == MatchState.Attacking ? role.AttackPreferences : role.DefendPreferences;
            FieldZone zone = ZoneProbabilitySystem.FindHighestWeightZoneAndWeight(rolePreferences).zone;
            candidates.AddRange(GenerateZoneCandidatePositions(ZoneProbabilitySystem.GetZoneRange(zone, enemyGoal, myGoal), 1f, 1f));
            candidates.AddRange(GenerateSupportCandidatePositions(player, matchContext, matchContext.GetTeammates(player)));
            candidates.AddRange(GenerateMarkCandidatePositions(player, matchContext, matchContext.GetOpponents(player)));
            candidates.AddRange(GenerateAroundBallCandidatePositions(player, ballPosition));
            return candidates;
        }

        public static List<Vector3> GenerateZoneCandidatePositions(ZoneProbabilitySystem.ZoneRange zoneRange, float widthInterval, float lengthInterval)
        {
            List<Vector3> points = new();
            for (float z = zoneRange.LeftBottom.z; z <= zoneRange.LeftBottom.z + zoneRange.Length; z += lengthInterval)
            {
                for (float x = zoneRange.LeftBottom.x; x <= zoneRange.LeftBottom.x + zoneRange.Width; x += widthInterval)
                {
                    points.Add(new Vector3(x, 0f, z));
                }
            }
            StringBuilder sb = new StringBuilder();
            foreach (var point in points)
            {
                sb.Append($"({point.x}, {point.z}), ");
            }
            Debug.Log($"Zone candidate points({points.Count} {zoneRange.LeftBottom}, {zoneRange.Width}, {zoneRange.Length}): {sb} ");
            return points;
        }

        public static List<Vector3> GenerateSupportCandidatePositions(GameObject player, MatchContext matchContext,
            List<GameObject> teammates)
        {
            List<Vector3> candidates = new List<Vector3>();
            foreach(var teammate in teammates)
                if(teammate != player)
                    candidates.AddRange(PointsGenerator.GeneratePointsAround(teammate.transform.position, 
                        1, 5f, 8));
            return candidates;
        }

        public static List<Vector3> GenerateMarkCandidatePositions(GameObject player, MatchContext matchContext,
            List<GameObject> enemies)
        {
            List<Vector3> candidates = new List<Vector3>();
            foreach(var enemy in enemies)
                candidates.AddRange(PointsGenerator.GeneratePointsAround(enemy.transform.position, 
                    3, 1f, 8));
            return candidates;
        }

        public static List<Vector3> GenerateAroundBallCandidatePositions(GameObject player, Vector3 ballPosition)
        {
            List<Vector3> candidates = new List<Vector3>();
            candidates.AddRange(PointsGenerator.GeneratePointsAround(ballPosition, 2, 1f, 8));
            return candidates;
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

        public static class PointsGenerator
        {
            public static List<Vector3> GeneratePointsInRectangle(Vector3 leftBottom, float width, float length, float interval)
            {
                List<Vector3> points = new List<Vector3>();
                for (float z = leftBottom.z; z <= leftBottom.z + length; z += interval)
                {
                    for (float x = leftBottom.x; x <= leftBottom.x + width; x += interval)
                    {
                        points.Add(new Vector3(x, 0f, z));
                    }
                }
                return points;
            }
            
            // 圆形生成
            public static List<Vector3> GeneratePointsAround(Vector3 position, int layers, 
                float layerWidth, int pointsPerLayer)
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
        }
    }
}
