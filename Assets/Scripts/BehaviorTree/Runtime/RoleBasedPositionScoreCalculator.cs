using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BehaviorTree.Runtime
{
    public static class RoleBasedPositionScoreCalculator
    {
        #region 分数计算
        public static PositionEvaluation CalculateContextAwareScoreCommon(Vector3 position, PlayerRole role, Vector3 myGoal,
            Vector3 enemyGoal, Vector3 ballPosition, MatchContext context, GameObject player, float distanceFromCurrent)
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
            float zoneScore = ZoneUtils.CalculateNormalizedZoneScore(position, role, myGoal, enemyGoal, DetermineMatchState(player, context)) * weightZone;
            float ballScore = CalculateBallScore(position, ballPosition) * weightBallDist;
            float goalScore = CalculateGoalScore(position, enemyGoal) * weightGoalDist;
            float markScore = CalculateMarkScore(position, role, context, myGoal, player) * weightMarking;
            float spaceScore = CalculateSpaceScore(position, context, player) * weightSpace;
            float safetyScore = CalculateSafetyScore(position, context.GetTeammates(player)) * weightSafety;
            float totalScore = Mathf.Max(0, zoneScore + ballScore + goalScore + markScore + spaceScore - safetyScore);
            return new PositionEvaluation(position, totalScore, zoneScore, ballScore, goalScore, markScore, spaceScore, safetyScore, distanceFromCurrent);
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
        
        private static float CalculateSafetyScore(Vector3 position, List<GameObject> teammates)
        {
            float minSafeDist = 1.5f; // 降低安全距离到1.5米
            float maxOverlapDegree = 0f;
            foreach (var teammate in teammates)
            {
                float dist = Vector3.Distance(position, teammate.transform.position);
                // 只有在安全距离内才计算惩罚
                if (dist < minSafeDist)
                {
                    // 使用指数惩罚：越近惩罚越重
                    float normalizedDist = dist / minSafeDist;
                    float degree = 1f - Mathf.Pow(normalizedDist, 3); // 使用立方加剧惩罚
                    if (degree > maxOverlapDegree)// 取最严重的那个重叠作为当前点的重叠分
                    {
                        maxOverlapDegree = degree;
                    }
                }
            }
            return maxOverlapDegree;
        }

        private static float CalculateMarkScore(Vector3 position, PlayerRole role, MatchContext context, Vector3 myGoal,
            GameObject player)
        {
            float scoreNormalized = 0f;
            // 只要在协防范围内，每个敌人+0.1分，如果是在持球人和球门的连线 +0.7分，或者敌人和持球人的连线，则+0.2分
            float enemyDistanceBase = 3f, distanceThreshold = 1f;
            float stopGoalBonus = 0.7f;
            float stopPassBonus = 0.2f;
            float baseBonus = 0.1f;
            List<GameObject> enemies = context.GetOpponents(player);
            GameObject ballHolder = context.GetBallHolder();
            foreach (var enemy in enemies)
            {
                float distance = Vector3.Distance(position, enemy.transform.position);
                if (distance <= enemyDistanceBase)
                {
                    if (ballHolder == enemy && CheckIsStopGoal(position, enemy.transform.position, myGoal, distanceThreshold))
                        scoreNormalized = scoreNormalized + stopGoalBonus;
                    else if (ballHolder != enemy && ballHolder != null && CheckIsStopPass(position,
                                 enemy.transform.position, ballHolder.transform.position, distanceThreshold))
                        scoreNormalized = scoreNormalized + stopPassBonus;
                    else
                        scoreNormalized = scoreNormalized + baseBonus;
                }
            }
            return Mathf.Clamp01(scoreNormalized); 
        }

        private static bool CheckIsStopGoal(Vector3 position, Vector3 ballHolderPosition, Vector3 myGoal,
            float distanceThreshold)
        {
            if(FootballUtils.DistancePointToLineSegment(ballHolderPosition, myGoal, position) < distanceThreshold)
                return true;
            return false;
        }

        private static bool CheckIsStopPass(Vector3 position, Vector3 enemyPosition, Vector3 ballHolderPosition,
            float distanceThreshold)
        {
            if(FootballUtils.DistancePointToLineSegment(ballHolderPosition, enemyPosition, position) < distanceThreshold)
                return true;
            return false;
        }

        private static bool CheckIsStrongMark(Vector3 position, Vector3 enemyPosition, Vector3 ballHolderPosition,
            Vector3 myGoal, float distanceThreshold)
        {
            // 1 检查是否在敌人和球门连线上
            if(FootballUtils.DistancePointToLineSegment(enemyPosition, myGoal, position) < distanceThreshold)
                return true;
            // 2 检查是否在敌人和持球人的连线
            if(FootballUtils.DistancePointToLineSegment(enemyPosition, ballHolderPosition, position) < distanceThreshold)
                return true;
            return false;
        }
        #endregion
        
        #region 确认最好的跑位位置
        public struct PositionEvaluation
        {
            public Vector3 Position;
            public float ZoneScore;
            public float BallScore;
            public float GoalScore;
            public float MarkScore;
            public float SpaceScore;
            public float SafetyScore;
            public float TotalScore;
            public float DistanceFromCurrent;
            public PositionEvaluation(Vector3 position, float totalScore, float zoneScore, float ballScore, float goalScore, 
                float markScore, float spaceScore, float safetyScore, float distanceFromCurrent)
            {
                Position = position;
                TotalScore = totalScore;
                ZoneScore = zoneScore;
                BallScore = ballScore;
                GoalScore = goalScore;
                MarkScore = markScore;
                SpaceScore = spaceScore;
                SafetyScore = safetyScore;
                DistanceFromCurrent = distanceFromCurrent;
            }
        }
        
        public static Vector3 FindBestPosition(PlayerRole role, Vector3 currentPosition, Vector3 myGoal,
            Vector3 enemyGoal, Vector3 ballPosition, MatchContext context, GameObject player,
            List<GameObject> teammates, List<GameObject> enemies, FootballBlackboard blackboard = null)
        {
            List<Vector3> candidatePositions = GenerateCandidatePositionsCommon(player, context, 
                role, currentPosition, myGoal, enemyGoal, ballPosition);
            List<PositionEvaluation> evaluations = EvaluatePositions(candidatePositions, currentPosition, role,
                myGoal, enemyGoal, ballPosition, context, player);
            Vector3 bestPosition = SelectBestPosition(evaluations, currentPosition);
            float bestScore = evaluations.Count > 0 ? evaluations.Max(e => e.TotalScore) : float.MinValue;
            int bestCandidatesCount = evaluations.Count(e => Mathf.Abs(e.TotalScore - bestScore) < FootballConstants.FloatEpsilon);
            LogAndAddDebugInfo(evaluations, player, bestScore, bestCandidatesCount, blackboard);
            return FootballUtils.GetPositionTowards(currentPosition, bestPosition, FootballConstants.DecideMinStep);
        }
        
        private static List<PositionEvaluation> EvaluatePositions(List<Vector3> positions, Vector3 currentPosition, PlayerRole role,
            Vector3 myGoal, Vector3 enemyGoal, Vector3 ballPosition, MatchContext context, GameObject player)
        {
            List<PositionEvaluation> evaluations = new List<PositionEvaluation>();
            foreach (var position in positions)
            {
                float distanceFromCurrent = Vector3.Distance(currentPosition, position);
                PositionEvaluation evaluation = CalculateContextAwareScoreCommon(position, role, myGoal, enemyGoal, ballPosition, context, player, distanceFromCurrent);
                
                evaluations.Add(evaluation);
            }
            return evaluations;
        }

        private static Vector3 SelectBestPosition(List<PositionEvaluation> evaluations, Vector3 currentPosition)
        {
            float bestScore = float.MinValue;
            List<Vector3> bestCandidates = new List<Vector3>();
            foreach (var evaluation in evaluations)
            {
                if (evaluation.TotalScore > bestScore)
                {
                    bestScore = evaluation.TotalScore;
                    bestCandidates.Clear();
                    bestCandidates.Add(evaluation.Position);
                }
                else if (Mathf.Abs(evaluation.TotalScore - bestScore) < FootballConstants.FloatEpsilon)
                {
                    bestCandidates.Add(evaluation.Position);
                }
            }
            if (bestCandidates.Count == 0)
            {
                return currentPosition;
            }
            else
            {
                Vector3 bestPosition = currentPosition;
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
                return bestPosition;
            }
        }

        private static void LogAndAddDebugInfo(List<PositionEvaluation> evaluations, GameObject player, 
            float bestScore, int bestCandidatesCount, FootballBlackboard blackboard)
        {
            if (blackboard != null && blackboard.DebugShowCandidates)
            {
                blackboard.DebugCandidatePositions = new List<CandidatePosition>();
                foreach (var evaluation in evaluations)
                {
                    blackboard.DebugCandidatePositions.Add(new CandidatePosition(evaluation.Position, evaluation.TotalScore));
                }
            }
            StringBuilder sb = new StringBuilder();
            sb.Append($"{player.name} candidate\n");
            foreach (var evaluation in evaluations)
            {
                sb.Append($"{evaluation.Position}-totalScore:{evaluation.TotalScore}-" +
                          $"ballScore:{evaluation.BallScore}-markScore{evaluation.MarkScore}-safetyScore:{evaluation.SafetyScore}\n");
            }
            PositionEvaluation bestEvaluation = evaluations.OrderByDescending(e => e.TotalScore).First();
            if(bestEvaluation.SafetyScore > 10f)
                Debug.Log($"{bestEvaluation.Position}-totalScore:{bestEvaluation.TotalScore}-" +
                      $"ballScore:{bestEvaluation.BallScore}-markScore{bestEvaluation.MarkScore}-safetyScore:{bestEvaluation.SafetyScore}\n"+
                      $" (共{bestCandidatesCount}个最高分候选点)\n{sb}");
        }
        #endregion

        public static List<Vector3> GenerateCandidatePositionsCommon(GameObject player, MatchContext matchContext,
            PlayerRole role, Vector3 currentPos, Vector3 myGoal, Vector3 enemyGoal, Vector3 ballPosition)
        {
            List<Vector3> candidates = new List<Vector3>();
            MatchState currentState = DetermineMatchState(player, matchContext);
            RolePreferences rolePreferences = currentState == MatchState.Attacking ? role.AttackPreferences : role.DefendPreferences;
            FieldZone zone = ZoneUtils.FindHighestWeightZoneAndWeight(rolePreferences).zone;
            candidates.AddRange(GenerateZoneCandidatePositions(ZoneUtils.GetZoneRange(zone, enemyGoal, myGoal), 1f, 1f));
            candidates.AddRange(GenerateSupportCandidatePositions(player, matchContext, matchContext.GetTeammates(player)));
            candidates.AddRange(GenerateMarkCandidatePositions(player, matchContext, matchContext.GetOpponents(player)));
            candidates.AddRange(GenerateAroundBallCandidatePositions(player, ballPosition));
            candidates = FilterOverlappingPositions(candidates, player, matchContext.GetTeammates(player), 1.2f);
            candidates = FilterInvalidPositions(candidates, matchContext);
            return candidates;
        }

        public static List<Vector3> GenerateZoneCandidatePositions(ZoneUtils.ZoneRange zoneRange, float widthInterval, float lengthInterval)
        {
            List<Vector3> points = PointsGenerator.GeneratePointsInRectangle(zoneRange.LeftBottom, zoneRange.Width, zoneRange.Length, widthInterval, lengthInterval);
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
        private static List<Vector3> FilterInvalidPositions(List<Vector3> positions, MatchContext context)
        {
            return positions.Where(pos => context.IsInField(pos)).ToList();
        }

        private static List<Vector3> FilterOverlappingPositions(List<Vector3> candidates, 
            GameObject player, List<GameObject> teammates, float safeDistance)
        {
            List<Vector3> safeCandidates = new List<Vector3>();
            
            foreach (var candidate in candidates)
            {
                bool isOverlapping = false;
                foreach (var teammate in teammates)
                {
                    if (Vector3.Distance(candidate, teammate.transform.position) < safeDistance)
                    {
                        isOverlapping = true;
                        break;
                    }
                }
                
                if (!isOverlapping)
                {
                    safeCandidates.Add(candidate);
                }
            }
            
            // 如果所有候选点都重叠，保留重叠最轻的5个
            if (safeCandidates.Count == 0)
            {
                safeCandidates = candidates
                    .OrderBy(c => teammates.Min(t => Vector3.Distance(c, t.transform.position)))
                    .Take(5)
                    .ToList();
            }
            
            return safeCandidates;
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

        #region 几何点生成
        public static class PointsGenerator
        {
            public static List<Vector3> GeneratePointsInRectangle(Vector3 leftBottom, float width, float length, float widthInterval, float lengthInterval)
            {
                List<Vector3> points = new List<Vector3>();
                for (float z = leftBottom.z; z <= leftBottom.z + length; z += lengthInterval)
                {
                    for (float x = leftBottom.x; x <= leftBottom.x + width; x += widthInterval)
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
        #endregion
    }
}
