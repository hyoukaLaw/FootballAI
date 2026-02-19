using UnityEngine;
using System.Collections.Generic;
using System.Text;
using FootballAI.FootballCore;

namespace BehaviorTree.Runtime
{
    public static class RoleBasedPositionScoreCalculator
    {
        #region 分数计算
        public static PositionEvaluation CalculateContextAwareScoreCommon(Vector3 position, PlayerRole role, Vector3 myGoal,
            Vector3 enemyGoal, Vector3 ballPosition, MatchContext context, GameObject player, float distanceFromCurrent)
        {
            float weightZone = 50f;
            float weightBallDist = 0f, weightGoalDist = 0f, weightMarking = 0f, weightSpace = 0f, weightSafety = 0f, weightPressing = 0f, weightSupport = 0f;
            MatchState state = DetermineMatchState(player, context);
            if (state == MatchState.Attacking)
            {
                weightBallDist = role.AttackPositionWeight.WeightBallDist;
                weightGoalDist = role.AttackPositionWeight.WeightGoalDist;
                weightMarking = role.AttackPositionWeight.WeightMarking;
                weightSpace = role.AttackPositionWeight.WeightSpace;
                weightSafety = role.AttackPositionWeight.WeightSafety;
                weightPressing = role.AttackPositionWeight.WeightPressing;
                weightSupport = role.AttackPositionWeight.WeightSupport;
            }
            else if (state == MatchState.Defending || state == MatchState.ChasingBall)
            {
                weightBallDist = role.DefendPositionWeight.WeightBallDist;
                weightGoalDist = role.DefendPositionWeight.WeightGoalDist;
                weightMarking = role.DefendPositionWeight.WeightMarking;
                weightSpace = role.DefendPositionWeight.WeightSpace;
                weightSafety = role.DefendPositionWeight.WeightSafety;
                weightPressing = role.DefendPositionWeight.WeightPressing;
                weightSupport = role.DefendPositionWeight.WeightSupport;
            }
            float zoneScore = ZoneUtils.CalculateNormalizedZoneScore(position, role, myGoal, enemyGoal,
                DetermineMatchState(player, context), context, player) * weightZone;
            float ballScore = CalculateBallScore(position, ballPosition) * weightBallDist;
            float goalScore = CalculateGoalScore(position, enemyGoal) * weightGoalDist;
            float markScore = CalculateMarkScore(position, role, context, myGoal, player) * weightMarking;
            float spaceScore = CalculateSpaceScore(position, context, player) * weightSpace;
            float supportScore = CalculateSupportScore(position, player, context) * weightSupport;
            float safetyScore = CalculateSafetyScore(position, player, context.GetTeammates(player)) * weightSafety;
            float pressingScore = CalculatePressingScore(position, ballPosition, myGoal, player, context.GetBallHolder(), context.GetTeammates(player)) * weightPressing;
            float totalScore = Mathf.Max(0, zoneScore + ballScore + goalScore + markScore + spaceScore + pressingScore+ supportScore - safetyScore);
            return new PositionEvaluation(position, totalScore, zoneScore, ballScore, goalScore, markScore, spaceScore, safetyScore, pressingScore, supportScore, distanceFromCurrent);
        }
        
        public static float CalculateBallScore(Vector3 position, Vector3 ballPosition)
        {
            float ballDistanceBase = 15f;
            float distanceToBall = Vector3.Distance(position, ballPosition);
            return Mathf.Clamp01(1f - Mathf.Pow(distanceToBall / ballDistanceBase,2f));
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

        private static float CalculateSupportScore(Vector3 position, GameObject player, MatchContext context)
        {
            GameObject ballHolder = context.GetBallHolder();
            if (ballHolder == null || ballHolder == player) return 0f;
            float distanceToHolder = Vector3.Distance(position, ballHolder.transform.position);
            if (distanceToHolder > FootballConstants.SupportMaxDistanceToHolder || 
                distanceToHolder < FootballConstants.SupportMinDistanceToHolder)
            {
                return 0f;
            }
            float distanceFromIdeal = Mathf.Abs(distanceToHolder - FootballConstants.IdealSupportDistance);
            float deviationRatio = distanceFromIdeal / ( FootballConstants.SupportMaxDistanceToHolder - 
                                                         FootballConstants.SupportMinDistanceToHolder);
            float distanceScore = Mathf.Clamp01(1f - deviationRatio);
            float laneSafetyScore = CalculateSupportPassLaneSafetyScore(ballHolder.transform.position, position,
                context.GetOpponents(player));
            return distanceScore * laneSafetyScore;
        }

        private static float CalculateSupportPassLaneSafetyScore(Vector3 passerPosition, Vector3 receiverPosition,
            List<GameObject> enemies)
        {
            Vector3 passDirection = (receiverPosition - passerPosition).normalized;
            float safetyScore = 1f;
            for (int i = 0; i < enemies.Count; i++)
            {
                GameObject enemy = enemies[i];
                Vector3 toEnemy = enemy.transform.position - passerPosition;
                // 传球方向反向的敌人不参与截断评分。
                if (Vector3.Dot(passDirection, toEnemy) <= 0f)
                    continue;
                float distanceToSegment = FootballUtils.DistancePointToLineSegment(passerPosition, receiverPosition,
                    enemy.transform.position);
                if (distanceToSegment >= FootballConstants.SupportPassLaneInterceptThreshold)
                    continue;
                float threat = 1f - distanceToSegment / FootballConstants.SupportPassLaneInterceptThreshold;
                safetyScore -= threat * FootballConstants.SupportPassLanePenaltyPerEnemy;
            }
            return Mathf.Clamp01(safetyScore);
        }

        private static float CalculateSafetyScore(Vector3 position, GameObject player, List<GameObject> teammates)
        {
            float minSafeDist = 1.5f; // 降低安全距离到1.5米
            float maxOverlapDegree = 0f;
            foreach (var teammate in teammates)
            {
                if (teammate == player) continue;
                Vector3 estimatedNextPosition = GetEstimatedNextPosition(teammate); // 跑位过程会穿插就没办法了
                float dist = Mathf.Min(Vector3.Distance(position, teammate.transform.position), Vector3.Distance(position, estimatedNextPosition));
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

        private static Vector3 GetEstimatedNextPosition(GameObject player)
        {
            return player.transform.position + player.transform.forward * FootballConstants.DecideMinStep;
        }

        private static float CalculateMarkScore(Vector3 position, PlayerRole role, MatchContext context, Vector3 myGoal,
            GameObject player)
        {
            float scoreNormalized = 0f;
            float enemyDistanceBase = FootballConstants.MarkingEnemyDistanceBase;
            float distanceThreshold = FootballConstants.MarkingStopPassDistanceThreshold;
            float stopPassBonus = FootballConstants.MarkingStopPassBonus;
            float baseBonus = FootballConstants.MarkingBaseBonus;
            List<GameObject> enemies = context.GetOpponents(player);
            List<GameObject> teammates = context.GetTeammates(player);
            GameObject ballHolder = context.GetBallHolder();
            // 危险系数参数
            float fieldLength = MatchManager.Instance.Context.GetFieldLength();
            float minDangerDistance = 5f;
            float maxDangerDistance = fieldLength;
            foreach (var enemy in enemies)
            {
                float distance = Vector3.Distance(position, enemy.transform.position);
                if (distance <= enemyDistanceBase)
                {
                    float enemyToGoalDistance = Vector3.Distance(enemy.transform.position, myGoal);
                    float dangerFactor = 1f - Mathf.Clamp01((enemyToGoalDistance - minDangerDistance) / (maxDangerDistance - minDangerDistance));
                    dangerFactor = dangerFactor * dangerFactor;
                     if (ballHolder != enemy && ballHolder != null && CheckIsStopPass(position,
                                 enemy.transform.position, ballHolder.transform.position, distanceThreshold))
                        scoreNormalized = scoreNormalized + stopPassBonus * (1f + dangerFactor);
                     else
                        scoreNormalized = scoreNormalized + baseBonus;
                }
            }
            float differentiationBonus = CalculatePositionDifferentiation(position, player, enemies, teammates);
            scoreNormalized += differentiationBonus;
            return Mathf.Clamp01(scoreNormalized);
        }

        /// <summary>
        /// 计算基于队友位置的差异化奖励，避免多个后卫选择相同位置
        /// </summary>
        private static float CalculatePositionDifferentiation(Vector3 position, GameObject player, 
            List<GameObject> enemies, List<GameObject> teammates)
        {
            float bonus = 0f;
            foreach (var enemy in enemies)
            {
                // 计算我与这个敌人的距离
                float myDistance = Vector3.Distance(position, enemy.transform.position);
                // 计算队友与这个敌人的距离
                float minTeammateDistance = float.MaxValue;
                foreach (var teammate in teammates)
                {
                    if (teammate == player) continue;
                    float dist = Vector3.Distance(teammate.transform.position, enemy.transform.position);
                    if (dist < minTeammateDistance)
                        minTeammateDistance = dist;
                }
                // 如果队友已经盯防这个敌人更近，我不应该重复盯防
                if (myDistance > minTeammateDistance + 1f)  // 我比队友远1米以上
                {
                    bonus -= 0.1f;  // 惩罚：不要重复盯防
                }
                // 如果我离这个敌人更近，鼓励我盯防
                else if (myDistance < minTeammateDistance)
                {
                    bonus += 0.1f;  // 奖励：我应该盯防这个敌人
                }
            }
            return bonus;
        }

        private static float CalculatePressingScore(Vector3 position, Vector3 ballPosition, 
            Vector3 myGoal, GameObject player, GameObject ballHolder, List<GameObject> teammates)
        {
            if (!FootballUtils.IsClosestTeammateToTarget(ballPosition, player, teammates,0f))  // 只对最近的防守者生效
                return 0f; 
            if (ballHolder == null) return 0f;
            // 计算上抢方向：从持球人到对方球门
            Vector3 holderToGoal = (myGoal - ballHolder.transform.position).normalized;
            Vector3 holderToPosition = (position - ballHolder.transform.position).normalized;
            float alignment = Vector3.Dot(holderToGoal, holderToPosition);// 上抢角度奖励：越靠近"持球人→球门"方向，奖励越高
            if (alignment > 0f) // 只奖励正对持球人的位置（alignment > 0）
            {
                float distanceToHolder = Vector3.Distance(position, ballHolder.transform.position);
                float distanceBonus = 0f;
                if (distanceToHolder < 3f) // 距离奖励：0-3米内的位置获得奖励，保持安全距离
                {
                    distanceBonus = 1f - (distanceToHolder - 0f) / 3f;
                }
                // 综合评分：方向占60%，距离占40%
                return alignment * 0.2f + distanceBonus * 0.8f;
            }
            return 0f;  // 背对持球人，不奖励
        }

        private static bool CheckIsStopPass(Vector3 position, Vector3 enemyPosition, Vector3 ballHolderPosition,
            float distanceThreshold) // 2 检查是否在敌人和持球人的连线上
        {
            if(FootballUtils.DistancePointToLineSegment(ballHolderPosition, enemyPosition, position) < distanceThreshold)
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
            public float PressingScore;
            public float SupportScore;
            public float TotalScore;
            public float DistanceFromCurrent;
            public PositionEvaluation(Vector3 position, float totalScore, float zoneScore, float ballScore, float goalScore,
                float markScore, float spaceScore, float safetyScore, float pressingScore, float supportScore, float distanceFromCurrent)
            {
                Position = position;
                TotalScore = totalScore;
                ZoneScore = zoneScore;
                BallScore = ballScore;
                GoalScore = goalScore;
                MarkScore = markScore;
                SpaceScore = spaceScore;
                SafetyScore = safetyScore;
                PressingScore = pressingScore;
                SupportScore = supportScore;
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
            if (blackboard != null)
            {
                blackboard.DebugHasSelectedPosition = evaluations.Count > 0;
                blackboard.DebugSelectedPosition = bestPosition;
            }
            float bestScore = float.MinValue;
            int bestCandidatesCount = 0;
            for (int i = 0; i < evaluations.Count; i++)
            {
                float score = evaluations[i].TotalScore;
                if (score > bestScore + FootballConstants.FloatEpsilon)
                {
                    bestScore = score;
                    bestCandidatesCount = 1;
                }
                else if (Mathf.Abs(score - bestScore) < FootballConstants.FloatEpsilon)
                {
                    bestCandidatesCount++;
                }
            }
            //LogAndAddDebugInfo(evaluations, player, bestScore, bestCandidatesCount, blackboard);
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

        #endregion

        public static List<Vector3> GenerateCandidatePositionsCommon(GameObject player, MatchContext matchContext,
            PlayerRole role, Vector3 currentPos, Vector3 myGoal, Vector3 enemyGoal, Vector3 ballPosition)
        {
            List<Vector3> candidates = new List<Vector3>();
            MatchState currentState = DetermineMatchState(player, matchContext);
            RolePreferences rolePreferences = currentState == MatchState.Attacking ? role.AttackPreferences : role.DefendPreferences;
            List<Vector3> zoneCandidates = GenerateZoneCandidatesByBudget(matchContext, rolePreferences, currentPos);
            List<Vector3> supportCandidates = GenerateSupportCandidatePositions(player, matchContext, matchContext.GetTeammates(player));
            MarkCandidateBuildResult markBuildResult = GenerateMarkCandidatePositions(player, matchContext, matchContext.GetOpponents(player));
            List<Vector3> markCandidates = markBuildResult.Candidates;
            List<Vector3> aroundBallCandidates = GenerateAroundBallCandidatePositions(player, ballPosition);
            candidates.AddRange(zoneCandidates);
            candidates.AddRange(supportCandidates);
            candidates.AddRange(markCandidates);
            candidates.AddRange(aroundBallCandidates);
            int rawCount = candidates.Count;
            candidates = DeduplicateCandidatePositions(candidates, FootballConstants.CandidateDeduplicateGridSize);
            int dedupCount = candidates.Count;
            candidates = FilterOverlappingPositions(candidates, player, matchContext.GetTeammates(player), FootballConstants.CandidateTeammateSafeDistance);
            int overlapFilteredCount = candidates.Count;
            candidates = FilterInvalidPositions(candidates, matchContext);
            int inFieldCount = candidates.Count;
            MyLog.LogInfoNoSampling($"[RoleBasedPosition] player={player.name} zone={zoneCandidates.Count} support={supportCandidates.Count} mark={markCandidates.Count}(rel={markBuildResult.RelationalCount},fb={markBuildResult.FallbackCount}) aroundBall={aroundBallCandidates.Count} raw={rawCount} dedup={dedupCount} overlap={overlapFilteredCount} inField={inFieldCount}");
            return candidates;
        }

        public static List<Vector3> GenerateZoneCandidatePositions(ZoneUtils.ZoneRange zoneRange, float widthInterval, float lengthInterval)
        {
            List<Vector3> points = PointsGenerator.GeneratePointsInRectangle(zoneRange.LeftBottom, zoneRange.Width, zoneRange.Length, widthInterval, lengthInterval);
            return points;
        }

        private static List<Vector3> GenerateZoneCandidatesByBudget(MatchContext context, RolePreferences preferences,
            Vector3 currentPos)
        {
            List<ZoneSelectionInfo> selectedZones = GetTopWeightedZones(context, preferences,
                FootballConstants.ZoneCandidateTopRegionCount);
            if (selectedZones.Count == 0)
            {
                ZoneUtils.ZoneRange fullRange = ZoneUtils.BuildFullFieldZoneRange(context);
                float interval = Mathf.Clamp(
                    Mathf.Sqrt(fullRange.Width * fullRange.Length / Mathf.Max(1, FootballConstants.ZoneCandidateTotalCap)),
                    FootballConstants.ZoneCandidateMinInterval, FootballConstants.ZoneCandidateMaxInterval);
                List<Vector3> fallbackCandidates = GenerateZoneCandidatePositions(fullRange, interval, interval);
                TrimCandidatesByDistance(fallbackCandidates, currentPos, FootballConstants.ZoneCandidateTotalCap);
                return fallbackCandidates;
            }
            List<Vector3> result = new List<Vector3>();
            float totalWeight = 0f;
            for (int i = 0; i < selectedZones.Count; i++)
                totalWeight += Mathf.Max(0f, selectedZones[i].Weight);
            int usedQuota = 0;
            for (int i = 0; i < selectedZones.Count; i++)
            {
                ZoneSelectionInfo selectedZone = selectedZones[i];
                int remainingRegions = selectedZones.Count - i;
                int remainingQuota = FootballConstants.ZoneCandidateTotalCap - usedQuota;
                int zoneQuota = remainingQuota;
                if (remainingRegions > 1)
                {
                    float ratio = totalWeight > FootballConstants.FloatEpsilon
                        ? Mathf.Max(0f, selectedZone.Weight) / totalWeight
                        : 1f / selectedZones.Count;
                    zoneQuota = Mathf.Max(1, Mathf.RoundToInt(FootballConstants.ZoneCandidateTotalCap * ratio));
                    zoneQuota = Mathf.Min(zoneQuota, remainingQuota - (remainingRegions - 1));
                }
                float interval = Mathf.Clamp(
                    Mathf.Sqrt(selectedZone.Range.Width * selectedZone.Range.Length / Mathf.Max(1, zoneQuota)),
                    FootballConstants.ZoneCandidateMinInterval, FootballConstants.ZoneCandidateMaxInterval);
                List<Vector3> zonePoints = GenerateZoneCandidatePositions(selectedZone.Range, interval, interval);
                TrimCandidatesByDistance(zonePoints, currentPos, zoneQuota);
                result.AddRange(zonePoints);
                usedQuota += zonePoints.Count;
            }
            TrimCandidatesByDistance(result, currentPos, FootballConstants.ZoneCandidateTotalCap);
            return result;
        }

        public static List<Vector3> GenerateSupportCandidatePositions(GameObject player, MatchContext matchContext,
            List<GameObject> teammates)
        {
            List<Vector3> candidates = new List<Vector3>();
            foreach(var teammate in teammates)
                if(teammate != player)
                    candidates.AddRange(PointsGenerator.GeneratePointsAround(teammate.transform.position, 
                        FootballConstants.SupportCandidateLayers, FootballConstants.SupportCandidateLayerWidth, FootballConstants.SupportCandidatePointsPerLayer));
            return candidates;
        }

        private static MarkCandidateBuildResult GenerateMarkCandidatePositions(GameObject player, MatchContext matchContext,
            List<GameObject> enemies)
        {
            List<Vector3> candidates = new List<Vector3>();
            List<GameObject> teammates = matchContext.GetTeammates(player);
            List<GameObject> keyEnemies = SelectKeyMarkEnemies(player, matchContext, enemies, teammates);
            int relationalCount = 0;
            for (int i = 0; i < keyEnemies.Count; i++)
            {
                GameObject enemy = keyEnemies[i];
                int beforeCount = candidates.Count;
                AddRelationalMarkCandidates(candidates, player, matchContext, enemy);
                relationalCount += candidates.Count - beforeCount;
            }
            int fallbackCount = 0;
            if (candidates.Count == 0)
            {
                // 关系候选为空时，用少量环点兜底，避免无候选导致站位异常。
                List<GameObject> fallbackEnemies = FindNearestEnemies(player.transform.position, enemies,
                    FootballConstants.MarkFallbackEnemyCount);
                for (int i = 0; i < fallbackEnemies.Count; i++)
                {
                    GameObject enemy = fallbackEnemies[i];
                    if (enemy == null)
                        continue;
                    List<Vector3> fallbackCandidates = PointsGenerator.GeneratePointsAround(enemy.transform.position,
                        FootballConstants.MarkFallbackLayers, FootballConstants.MarkCandidateLayerWidth,
                        FootballConstants.MarkFallbackPointsPerLayer);
                    fallbackCount += fallbackCandidates.Count;
                    candidates.AddRange(fallbackCandidates);
                }
            }
            return new MarkCandidateBuildResult(candidates, relationalCount, fallbackCount);
        }

        public static List<Vector3> GenerateAroundBallCandidatePositions(GameObject player, Vector3 ballPosition)
        {
            List<Vector3> candidates = new List<Vector3>();
            candidates.Add(ballPosition);
            candidates.AddRange(PointsGenerator.GeneratePointsAround(ballPosition, FootballConstants.AroundBallCandidateLayers,
                FootballConstants.AroundBallCandidateLayerWidth, FootballConstants.AroundBallCandidatePointsPerLayer));
            return candidates;
        }

        # region 通用工具方法
        private static List<Vector3> FilterInvalidPositions(List<Vector3> positions, MatchContext context)
        {
            List<Vector3> filtered = new List<Vector3>(positions.Count);
            for (int i = 0; i < positions.Count; i++)
            {
                Vector3 pos = positions[i];
                if (context.IsInField(pos))
                {
                    filtered.Add(pos);
                }
            }
            return filtered;
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
                    if (teammate == player) continue;
                    Vector3 candidateNextMove = (candidate - player.transform.position).magnitude < FootballConstants.DecideMinStep ?
                        candidate:
                        player.transform.position + (candidate - player.transform.position).normalized * FootballConstants.DecideMinStep;
                    if (FootballUtils.IsOverlappingWithPlayerMovement(candidateNextMove, teammate, safeDistance))
                    {
                        isOverlapping = true;
                        break;
                    }

                    if ((candidateNextMove - FootballUtils.PredictNextPosition(teammate)).magnitude < FootballConstants.CandidatePredictedOverlapThreshold)
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
                List<Vector3> sortedCandidates = new List<Vector3>(candidates);
                sortedCandidates.Sort((a, b) =>
                {
                    float minDistA = CalculateMinDistanceToTeammates(a, teammates, player);
                    float minDistB = CalculateMinDistanceToTeammates(b, teammates, player);
                    return minDistB.CompareTo(minDistA);
                });

                int count = Mathf.Min(5, sortedCandidates.Count);
                safeCandidates = new List<Vector3>(count);
                for (int i = 0; i < count; i++)
                {
                    safeCandidates.Add(sortedCandidates[i]);
                }
            }
            return safeCandidates;
        }

        private static float CalculateMinDistanceToTeammates(Vector3 candidate, List<GameObject> teammates, GameObject player)
        {
            float minDistance = float.MaxValue;
            bool hasValidTeammate = false;
            for (int i = 0; i < teammates.Count; i++)
            {
                GameObject teammate = teammates[i];
                if (teammate == null || teammate == player) continue;

                hasValidTeammate = true;
                float distance = Vector3.Distance(candidate, teammate.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }

            return hasValidTeammate ? minDistance : float.MaxValue;
        }

        private static List<Vector3> DeduplicateCandidatePositions(List<Vector3> candidates, float gridSize)
        {
            if (gridSize <= 0f)
                return candidates;

            Dictionary<Vector2Int, Vector3> unique = new Dictionary<Vector2Int, Vector3>();
            foreach (var candidate in candidates)
            {
                Vector2Int key = new Vector2Int(
                    Mathf.RoundToInt(candidate.x / gridSize),
                    Mathf.RoundToInt(candidate.z / gridSize)
                );
                if (!unique.ContainsKey(key))
                {
                    unique[key] = candidate;
                }
            }

            List<Vector3> deduplicated = new List<Vector3>(unique.Count);
            foreach (var pair in unique)
            {
                deduplicated.Add(pair.Value);
            }

            return deduplicated;
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
        private struct MarkCandidateBuildResult
        {
            public List<Vector3> Candidates;
            public int RelationalCount;
            public int FallbackCount;

            public MarkCandidateBuildResult(List<Vector3> candidates, int relationalCount, int fallbackCount)
            {
                Candidates = candidates;
                RelationalCount = relationalCount;
                FallbackCount = fallbackCount;
            }
        }

        private static void AddRelationalMarkCandidates(List<Vector3> candidates, GameObject player,
            MatchContext matchContext, GameObject enemy)
        {
            Vector3 passerPos = matchContext.GetBallHolder() != null
                ? matchContext.GetBallHolder().transform.position
                : matchContext.Ball.transform.position;
            Vector3 enemyPos = enemy.transform.position;
            Vector3 interceptPos = Vector3.Lerp(passerPos, enemyPos, FootballConstants.MarkRelationalInterceptRatio);
            Vector3 laneDir = enemyPos - passerPos;
            laneDir.y = 0f;
            if (laneDir.sqrMagnitude > FootballConstants.FloatEpsilon)
            {
                laneDir.Normalize();
                Vector3 sideDir = new Vector3(-laneDir.z, 0f, laneDir.x);
                candidates.Add(interceptPos);
                candidates.Add(interceptPos + sideDir * FootballConstants.MarkRelationalLateralOffset);
                candidates.Add(interceptPos - sideDir * FootballConstants.MarkRelationalLateralOffset);
            }
            else
                candidates.Add(interceptPos);
            Vector3 myGoalPos = matchContext.GetMyGoalPosition(player);
            Vector3 goalSideMarkPos = Vector3.Lerp(enemyPos, myGoalPos, FootballConstants.MarkRelationalGoalSideRatio);
            candidates.Add(goalSideMarkPos);
        }

        private static List<GameObject> SelectKeyMarkEnemies(GameObject player, MatchContext context,
            List<GameObject> enemies, List<GameObject> teammates)
        {
            Vector3 myPos = player.transform.position;
            Vector3 ballPos = context.Ball.transform.position;
            int targetCount = FootballConstants.MarkRelationalEnemyCount;
            int preselectCount = Mathf.Min(enemies.Count, targetCount * 2);
            List<GameObject> nearestEnemies = FindNearestEnemies(ballPos, enemies, preselectCount);
            List<GameObject> result = new List<GameObject>(targetCount);
            for (int i = 0; i < nearestEnemies.Count; i++)
            {
                GameObject enemy = nearestEnemies[i];
                if (IsEnemyCoveredByCloserTeammate(enemy, myPos, teammates, player))
                    continue;
                result.Add(enemy);
                if (result.Count >= targetCount)
                    break;
            }
            return result;
        }

        private static bool IsEnemyCoveredByCloserTeammate(GameObject enemy, Vector3 myPos,
            List<GameObject> teammates, GameObject self)
        {
            float myDistance = Vector3.Distance(enemy.transform.position, myPos);
            for (int i = 0; i < teammates.Count; i++)
            {
                GameObject teammate = teammates[i];
                if (teammate == null || teammate == self)
                    continue;
                if (Vector3.Distance(enemy.transform.position, teammate.transform.position) + FootballConstants.ClosestPlayerTolerance < myDistance)
                    return true;
            }
            return false;
        }

        private static List<ZoneSelectionInfo> GetTopWeightedZones(MatchContext context, RolePreferences preferences,
            int topCount)
        {
            List<ZoneSelectionInfo> selected = new List<ZoneSelectionInfo>();
            List<ZoneWeightEntry> sortedWeights = new List<ZoneWeightEntry>(preferences.ZoneWeights);
            sortedWeights.Sort((a, b) => b.Weight.CompareTo(a.Weight));
            for (int i = 0; i < sortedWeights.Count && selected.Count < topCount; i++)
            {
                ZoneWeightEntry entry = sortedWeights[i];
                if (entry == null || string.IsNullOrEmpty(entry.ZoneId))
                    continue;
                ZoneRect zone = FindZoneById(context.FormationLayout.Zones, entry.ZoneId);
                if (zone == null || !zone.IsEnabled)
                    continue;
                ZoneUtils.ZoneRange range = BuildZoneRangeFromRect(zone);
                selected.Add(new ZoneSelectionInfo{Range = range, Weight = entry.Weight});
            }
            return selected;
        }

        private static ZoneRect FindZoneById(List<ZoneRect> zones, string zoneId)
        {
            for (int i = 0; i < zones.Count; i++)
            {
                ZoneRect zone = zones[i];
                if (zone == null || zone.ZoneId != zoneId)
                    continue;
                return zone;
            }
            return null;
        }

        private static ZoneUtils.ZoneRange BuildZoneRangeFromRect(ZoneRect zone)
        {
            float width = Mathf.Max(0.1f, zone.SizeXZ.x);
            float length = Mathf.Max(0.1f, zone.SizeXZ.y);
            Vector3 leftBottom = new Vector3(zone.CenterXZ.x - width * 0.5f, 0f, zone.CenterXZ.y - length * 0.5f);
            return new ZoneUtils.ZoneRange{LeftBottom = leftBottom, Width = width, Length = length};
        }

        private static void TrimCandidatesByDistance(List<Vector3> candidates, Vector3 pivot, int maxCount)
        {
            if (maxCount <= 0 || candidates.Count <= maxCount)
                return;
            candidates.Sort((a, b) => Vector3.SqrMagnitude(a - pivot).CompareTo(Vector3.SqrMagnitude(b - pivot)));
            candidates.RemoveRange(maxCount, candidates.Count - maxCount);
        }

        private struct ZoneSelectionInfo
        {
            public ZoneUtils.ZoneRange Range;
            public float Weight;
        }

        private static List<GameObject> FindNearestEnemies(Vector3 pivot, List<GameObject> enemies, int count)
        {
            List<EnemyDistanceInfo> infos = new List<EnemyDistanceInfo>();
            for (int i = 0; i < enemies.Count; i++)
            {
                GameObject enemy = enemies[i];
                if (enemy == null)
                    continue;
                infos.Add(new EnemyDistanceInfo{Enemy = enemy, Distance = Vector3.Distance(enemy.transform.position, pivot)});
            }
            infos.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            int takeCount = Mathf.Min(count, infos.Count);
            List<GameObject> result = new List<GameObject>(takeCount);
            for (int i = 0; i < takeCount; i++)
                result.Add(infos[i].Enemy);
            return result;
        }

        private struct EnemyDistanceInfo
        {
            public GameObject Enemy;
            public float Distance;
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
            int takeCount = Mathf.Min(cnt, points.Count);
            List<Vector3> result = new List<Vector3>(takeCount);
            for (int i = 0; i < takeCount; i++)
            {
                result.Add(points[i].Position);
            }

            return result;
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
        
        #region log
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
            
            // 找出最佳评估
            PositionEvaluation bestEvaluation = evaluations[0];
            for (int i = 1; i < evaluations.Count; i++)
            {
                if (evaluations[i].TotalScore > bestEvaluation.TotalScore)
                {
                    bestEvaluation = evaluations[i];
                }
            }
            
            // 构建详细的日志输出
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"=== {player.name} 位置选择分析 ===");
            sb.AppendLine($"最佳位置: {bestEvaluation.Position}");
            sb.AppendLine($"总分: {bestEvaluation.TotalScore:F2} | 共{bestCandidatesCount}个最高分候选点");
            sb.AppendLine($"距离当前位置: {bestEvaluation.DistanceFromCurrent:F2}米");
            sb.AppendLine("");
            
            // 输出最佳位置的详细分数
            sb.AppendLine("--- 最佳位置详细分数 ---");
            sb.AppendLine($"区域分: {bestEvaluation.ZoneScore:F2}");
            sb.AppendLine($"球距离分: {bestEvaluation.BallScore:F2}");
            sb.AppendLine($"球门距离分: {bestEvaluation.GoalScore:F2}");
            sb.AppendLine($"盯防分: {bestEvaluation.MarkScore:F2}");
            sb.AppendLine($"空间分: {bestEvaluation.SpaceScore:F2}");
            sb.AppendLine($"支持分: {bestEvaluation.SupportScore:F2}");
            sb.AppendLine($"安全分: {bestEvaluation.SafetyScore:F2}");
            sb.AppendLine($"上抢分: {bestEvaluation.PressingScore:F2}");
            sb.AppendLine($"总分计算: {bestEvaluation.ZoneScore + bestEvaluation.BallScore + bestEvaluation.GoalScore + bestEvaluation.MarkScore + bestEvaluation.SpaceScore + bestEvaluation.SupportScore + bestEvaluation.PressingScore:F2} - {bestEvaluation.SafetyScore:F2} = {bestEvaluation.TotalScore:F2}");
            sb.AppendLine("");
            
            // 安全性警告
            if (bestEvaluation.SafetyScore > 10f)
            {
                sb.AppendLine($"⚠️ 警告: 安全分 {bestEvaluation.SafetyScore:F2} 过高，可能存在队友重叠风险！");
                sb.AppendLine("");
            }

            if (bestEvaluation.PressingScore > 10f)
            {
                sb.AppendLine($"上抢分大于10!");
            }
            
            // 输出所有候选位置的简洁信息
            sb.AppendLine("--- 所有候选位置 (按分数排序) ---");
            List<PositionEvaluation> sortedEvaluations = new List<PositionEvaluation>(evaluations);
            sortedEvaluations.Sort((a, b) => b.TotalScore.CompareTo(a.TotalScore));
            if (sortedEvaluations.Count > 10)
            {
                sortedEvaluations.RemoveRange(10, sortedEvaluations.Count - 10);
            }
            for (int i = 0; i < sortedEvaluations.Count; i++)
            {
                var eval = sortedEvaluations[i];
                string prefix = (i == 0) ? "★" : " ";
                sb.AppendLine($"{prefix} [{i}] 总分:{eval.TotalScore:F2} | 位置:{eval.Position} | 区域:{eval.ZoneScore:F1} | 球距:{eval.BallScore:F1} | 盯防:{eval.MarkScore:F1} | 空间:{eval.SpaceScore:F1} | 支持:{eval.SupportScore:F1} | 上抢:{eval.PressingScore:F1} | 安全:{eval.SafetyScore:F1} | 距离:{eval.DistanceFromCurrent:F1}m");
            }
            
            //MyLog.LogInfo(sb.ToString());
        }
        #endregion
    }
}
