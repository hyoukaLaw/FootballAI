using System;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviorTree.Runtime
{
    public class TaskEvaluateRoleBaseOffensiveOptions: ActionNode
    {
        public TaskEvaluateRoleBaseOffensiveOptions(FootballBlackboard blackboard) : base(blackboard)
        {
        }

        public override NodeState Evaluate()
        {
            var strategy = OffensiveStrategyFactory.GetStrategy(Blackboard.Role.RoleType);
            var action = strategy.Evaluate(Blackboard);
            
            ApplyActionToBlackboard(action);
            
            LogOffensiveEvaluation(action, strategy.StrategyName);
            
            return NodeState.SUCCESS;
        }
        
        private void ApplyActionToBlackboard(OffensiveAction action)
        {
            switch (action.ActionType)
            {
                case OffensiveActionType.Shoot:
                    Blackboard.CanShoot = true;
                    Blackboard.MoveTarget = Vector3.zero;
                    Blackboard.BestPassTarget = null;
                    Blackboard.ClearanceTarget = Vector3.negativeInfinity;
                    break;
                    
                case OffensiveActionType.Pass:
                    Blackboard.CanShoot = false;
                    Blackboard.MoveTarget = Vector3.zero;
                    Blackboard.BestPassTarget = action.PassTarget;
                    Blackboard.ClearanceTarget = Vector3.negativeInfinity;
                    break;
                    
                case OffensiveActionType.Dribble:
                    Blackboard.CanShoot = false;
                    Blackboard.MoveTarget = action.MoveTarget;
                    Blackboard.BestPassTarget = null;
                    Blackboard.ClearanceTarget = Vector3.negativeInfinity;
                    break;
                    
                case OffensiveActionType.Clearance:
                    Blackboard.CanShoot = false;
                    Blackboard.MoveTarget = Vector3.zero;
                    Blackboard.BestPassTarget = null;
                    Blackboard.ClearanceTarget = action.ClearanceTarget;
                    break;
                    
                case OffensiveActionType.None:
                    Blackboard.CanShoot = false;
                    Blackboard.MoveTarget = Vector3.zero;
                    Blackboard.BestPassTarget = null;
                    Blackboard.ClearanceTarget = Vector3.negativeInfinity;
                    break;
            }
        }

        private void HandleDefenderOptions()
        {
            OffensiveActionCalculator.CalculatePassScoreAndTarget(out float passScore, out GameObject passTarget, out float lineSafety, out float targetSafety, Blackboard, FootballConstants.BasePassScoreDefender);
            CalculateDribbleScoreAndTarget(out float dribbleScore, out Vector3 dribbleTarget, out int enemiesInFront);
            CalculateClearanceScoreAndTarget(out float clearanceScore, out Vector3 clearanceTarget, out int enemiesNearClearance);

            var log = new OffensiveEvaluationLog(
                Blackboard.Owner.name, Blackboard.Role.RoleType,
                0f, Vector3.zero,
                passScore, passTarget,
                dribbleScore, dribbleTarget,
                clearanceScore, clearanceTarget,
                enemiesInFront, enemiesNearClearance,
                lineSafety, targetSafety
            );
            if (passScore > dribbleScore && passScore > clearanceScore)
            {
                Blackboard.MoveTarget = Vector3.zero;
                Blackboard.BestPassTarget = passTarget;
                Blackboard.ClearanceTarget = Vector3.negativeInfinity;
                log.SetPassAction(passTarget, Blackboard.Owner.transform.position);
            }
            else if (dribbleScore > passScore && dribbleScore > clearanceScore)
            {
                Blackboard.MoveTarget = dribbleTarget;
                Blackboard.BestPassTarget = null;
                Blackboard.ClearanceTarget = Vector3.negativeInfinity;
                log.SetDribbleAction(dribbleTarget);
            }
            else
            {
                Blackboard.MoveTarget = Vector3.zero;
                Blackboard.BestPassTarget = null;
                Blackboard.ClearanceTarget = clearanceTarget;
                log.SetClearanceAction(clearanceTarget);
            }
            LogOffensiveEvaluation(log);
        }
        
        #region Common
        private void CalculateDribbleScoreAndTarget(out float dribbleScore, out Vector3 dribbleTarget, out int enemiesInFrontCount)
        {
            List<GameObject> enemiesInFront = FindEnemiesInFront();
            enemiesInFrontCount = enemiesInFront.Count;
            dribbleScore = CalculateDribbleScore(enemiesInFront);
            dribbleTarget = Vector3.zero;
            GameObject closestBlockingEnemy = null;
            var goalPos = Blackboard.MatchContext.GetEnemyGoalPosition(Blackboard.Owner);
            float closestDistance = float.MaxValue;
            foreach (var enemy in enemiesInFront)
            {
                float distToEnemy = Vector3.Distance(Blackboard.Owner.transform.position, enemy.transform.position);
                if (distToEnemy < closestDistance)
                {
                    closestDistance = distToEnemy;
                    closestBlockingEnemy = enemy;
                }
            }
            Vector3 dribbleDirection = (goalPos - Blackboard.Owner.transform.position).normalized;
            dribbleDirection.y = 0;
            GameObject owner = Blackboard.Owner;
            if (closestBlockingEnemy != null)
            {
                // 前方有阻挡，侧向移动绕过
                dribbleTarget = GetSideStepTarget(dribbleDirection, owner, closestBlockingEnemy);
            }
            else
            {
                // 前方无阻挡，直接带球
                dribbleTarget = FootballUtils.GetPositionTowards(owner.transform.position, owner.transform.position + dribbleDirection.normalized, FootballConstants.DecideMinStep);
            }
        }
        
        
        // 计算到敌人的平均距离
        private float CalculateSpacePenalty(Vector3 ownerPos, List<GameObject> nearbyEnemies)
        {
            if (nearbyEnemies.Count == 0) return 0;
            float totalDistance = 0f;
            foreach (var enemy in nearbyEnemies)
            {
                totalDistance += Vector3.Distance(ownerPos, enemy.transform.position);
            }
            float avgDistance = totalDistance / nearbyEnemies.Count;
            float spacePenalty = avgDistance * Mathf.Clamp01((avgDistance - 2f)/ 3f) * FootballConstants.DribbleDistancePenalty; // 2米以下很危险
            return spacePenalty;
        }
        
        public float CalculateDribbleScore(List<GameObject> enemiesInFront)
        {
            if(enemiesInFront.Count == 0)
                return FootballConstants.BaseDribbleScore + FootballConstants.DribbleClearBonus;
            else
            {
                float enemyCountPenalty = enemiesInFront.Count * FootballConstants.DribbleEnemyPenalty;
                float enemyDistancePenalty = CalculateSpacePenalty(Blackboard.Owner.transform.position, enemiesInFront);
                float score = Math.Max(FootballConstants.BaseDribbleScore - enemyCountPenalty - enemyDistancePenalty, 0);
                return score;
            }
        }
        
        private List<GameObject> FindEnemiesInFront()
        {
            Vector3 enemyGoalPos = Blackboard.MatchContext.GetEnemyGoalPosition(Blackboard.Owner);
            var opponents = Blackboard.MatchContext.GetOpponents(Blackboard.Owner);
            return FootballUtils.FindEnemiesInFront(Blackboard.Owner,
                (enemyGoalPos - Blackboard.Owner.transform.position).normalized, opponents,
                FootballConstants.DribbleDetectDistance, FootballConstants.DribbleDetectHalfAngle);
        }
        #endregion
        
        #region Defender

        private Vector3 GetSideStepTarget(Vector3 dribbleDirection, GameObject owner, GameObject closestBlockingEnemy)
        {
            // 前方有阻挡，侧向移动绕过
            Vector3 sidestepDir = Vector3.Cross(Vector3.up, dribbleDirection);
            Vector3 leftPos = owner.transform.position + sidestepDir * FootballConstants.SidestepDistance;
            Vector3 rightPos = owner.transform.position - sidestepDir * FootballConstants.SidestepDistance;
            float leftDistToEnemy = Vector3.Distance(leftPos, closestBlockingEnemy.transform.position);
            float rightDistToEnemy = Vector3.Distance(rightPos, closestBlockingEnemy.transform.position);
            Vector3 sidestepPos;
            if(Blackboard.MatchContext.IsInField(leftPos) && Blackboard.MatchContext.IsInField(rightPos))
                sidestepPos = leftDistToEnemy > rightDistToEnemy ? leftPos : rightPos;
            else if(Blackboard.MatchContext.IsInField(leftPos)) 
                sidestepPos = leftPos;
            else 
                sidestepPos = rightPos;
            return FootballUtils.GetPositionTowards(owner.transform.position, sidestepPos, FootballConstants.DecideMinStep);
        }
        private void CalculateClearanceScoreAndTarget(out float clearanceScore, out Vector3 clearanceTarget, out int enemiesNearClearance)
        {
            var opponents = Blackboard.MatchContext.GetOpponents(Blackboard.Owner);
            Vector3 forwardDir = FootballUtils.GetForward(Blackboard.Owner);

            float[] angles = { 0f, 45f, -45f };
            float bestScore = float.MinValue;
            Vector3 bestTarget = Vector3.zero;
            int totalEnemiesNear = 0;

            foreach (float angle in angles)
            {
                float rad = angle * Mathf.Deg2Rad;
                Vector3 clearanceDir = Quaternion.Euler(0, rad, 0) * forwardDir;
                Vector3 candidateTarget = Blackboard.Owner.transform.position + clearanceDir * FootballConstants.ClearanceDistance;
                CalculateDirectionClearanceScore(clearanceDir, candidateTarget, opponents, out float score, out int currentEnemiesNear);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = candidateTarget;
                    totalEnemiesNear = currentEnemiesNear;
                }
            }
            clearanceScore = bestScore;
            clearanceTarget = bestTarget;
            enemiesNearClearance = totalEnemiesNear;
        }

        private void CalculateDirectionClearanceScore(Vector3 clearanceDir, Vector3 targetPos, List<GameObject> opponents, out float score, out int enemiesNearCount)
        {
            Vector3 startPos = Blackboard.Owner.transform.position;
            score = FootballConstants.BaseClearanceScore;

            if (!FootballUtils.IsPathClear(startPos, targetPos, opponents, FootballConstants.ClearanceBlockThreshold))
            {
                score -= FootballConstants.ClearanceScoreDistancePenalty;
            }

            var enemiesNear = FootballUtils.FindNearEnemies(Blackboard.Owner, opponents, FootballConstants.ClearanceDetectDistance);
            enemiesNearCount = enemiesNear.Count;

            foreach (var enemyNear in enemiesNear)
            {
                Vector3 meToEnemy = (enemyNear.transform.position - startPos).normalized;
                float angleToEnemy = Vector3.Angle(clearanceDir, meToEnemy);

                if (angleToEnemy < 30f)
                {
                    score -= FootballConstants.ClearanceScorePerEnemy * 2;
                }
                else if (angleToEnemy < 60f)
                {
                    score -= FootballConstants.ClearanceScorePerEnemy;
                }
            }
        }
        #endregion
        
        #region Forward

        private void HandleForwardOptions()
        {
            CalculateShootScoreAndTarget(out float shootScore, out Vector3 shootTarget);
            CalculateDribbleScoreAndTarget(out float dribbleScore, out Vector3 dribbleTarget, out int enemiesInFront);
            OffensiveActionCalculator.CalculatePassScoreAndTarget(out float passScore, out GameObject passTarget, out float lineSafety, out float targetSafety, Blackboard, FootballConstants.BasePassScoreForward);

            var log = new OffensiveEvaluationLog(
                Blackboard.Owner.name, Blackboard.Role.RoleType,
                shootScore, shootTarget,
                passScore, passTarget,
                dribbleScore, dribbleTarget,
                0f, Vector3.zero,
                enemiesInFront, 0,
                lineSafety, targetSafety
            );

            if (shootScore > dribbleScore && shootScore > passScore)
            {
                Blackboard.CanShoot = true;
                Blackboard.MoveTarget = Vector3.zero;
                Blackboard.BestPassTarget = null;
                log.SetShootAction(shootTarget, Blackboard.Owner.transform.position);
            }
            else if (dribbleScore > shootScore && dribbleScore > passScore)
            {
                Blackboard.CanShoot = false;
                Blackboard.MoveTarget = dribbleTarget;
                Blackboard.BestPassTarget = null;
                log.SetDribbleAction(dribbleTarget);
            }
            else
            {
                Blackboard.CanShoot = false;
                Blackboard.MoveTarget = Vector3.zero;
                Blackboard.BestPassTarget = passTarget;
                log.SetPassAction(passTarget, Blackboard.Owner.transform.position);
            }

            LogOffensiveEvaluation(log);
        }
        
        private void CalculateShootScoreAndTarget(out float shootScore, out Vector3 shootTarget)
        {
            shootTarget = Vector3.zero;
            // 优先射门,[10m可以射门，5m以内满分，5m-10m取系数]
            Vector3 enemyGoalPosition = Blackboard.MatchContext.GetEnemyGoalPosition(Blackboard.Owner);
            List<GameObject> opponents = Blackboard.MatchContext.GetOpponents(Blackboard.Owner);
            float distToGoal = Vector3.Distance(Blackboard.Owner.transform.position, enemyGoalPosition);
            shootScore = Mathf.Max((1 - Mathf.Max(distToGoal - 5, 0f) / 5f),0) * FootballConstants.BaseScoreShootScore;
            // 考虑射门是否被阻挡
            float shootBlockFactor = FootballUtils.IsPathClear(Blackboard.Owner.transform.position, 
                enemyGoalPosition, opponents, FootballConstants.ClearanceBlockThreshold) ? FootballConstants.ShootNoBlockFactor : FootballConstants.ShootBlockPenaltyFactor;
            shootScore = shootScore * shootBlockFactor;
            shootTarget = enemyGoalPosition;
        }
        
        #endregion

        public static class OffensiveActionCalculator
        {
            public static PassEvaluation CalculatePassScoreAndTarget(FootballBlackboard blackboard, float basePassScore)
            {
                CalculatePassScoreAndTarget(out float passScore, out GameObject passTarget, out float lineSafety, 
                    out float targetSafety, blackboard, basePassScore);
                
                return new PassEvaluation
                {
                    Score = passScore,
                    Target = passTarget,
                    LineSafety = lineSafety,
                    TargetSafety = targetSafety
                };
            }
            
            public static DribbleEvaluation CalculateDribbleScoreAndTarget(FootballBlackboard blackboard)
            {
                float dribbleScore;
                Vector3 dribbleTarget;
                int enemiesInFront;
                
                List<GameObject> enemiesInFrontList = FindEnemiesInFront(blackboard);
                enemiesInFront = enemiesInFrontList.Count;
                dribbleScore = CalculateDribbleScore(enemiesInFrontList, blackboard);
                dribbleTarget = Vector3.zero;
                GameObject closestBlockingEnemy = null;
                var goalPos = blackboard.MatchContext.GetEnemyGoalPosition(blackboard.Owner);
                float closestDistance = float.MaxValue;
                foreach (var enemy in enemiesInFrontList)
                {
                    float distToEnemy = Vector3.Distance(blackboard.Owner.transform.position, enemy.transform.position);
                    if (distToEnemy < closestDistance)
                    {
                        closestDistance = distToEnemy;
                        closestBlockingEnemy = enemy;
                    }
                }
                Vector3 dribbleDirection = (goalPos - blackboard.Owner.transform.position).normalized;
                dribbleDirection.y = 0;
                if (closestBlockingEnemy != null)
                {
                    dribbleTarget = GetSideStepTarget(dribbleDirection, blackboard.Owner, closestBlockingEnemy, blackboard);
                }
                else
                {
                    dribbleTarget = FootballUtils.GetPositionTowards(blackboard.Owner.transform.position, blackboard.Owner.transform.position + dribbleDirection.normalized, FootballConstants.DecideMinStep);
                }
                
                return new DribbleEvaluation
                {
                    Score = dribbleScore,
                    Target = dribbleTarget,
                    EnemiesInFront = enemiesInFront
                };
            }
            
            public static ClearanceEvaluation CalculateClearanceScoreAndTarget(FootballBlackboard blackboard)
            {
                float clearanceScore;
                Vector3 clearanceTarget;
                int enemiesNearClearance;
                
                var opponents = blackboard.MatchContext.GetOpponents(blackboard.Owner);
                Vector3 forwardDir = FootballUtils.GetForward(blackboard.Owner);

                float[] angles = { 0f, 45f, -45f };
                float bestScore = float.MinValue;
                Vector3 bestTarget = Vector3.zero;
                int totalEnemiesNear = 0;

                foreach (float angle in angles)
                {
                    float rad = angle * Mathf.Deg2Rad;
                    Vector3 clearanceDir = Quaternion.Euler(0, rad, 0) * forwardDir;
                    Vector3 candidateTarget = blackboard.Owner.transform.position + clearanceDir * FootballConstants.ClearanceDistance;
                    CalculateDirectionClearanceScore(clearanceDir, candidateTarget, opponents, blackboard, out float score, out int currentEnemiesNear);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestTarget = candidateTarget;
                        totalEnemiesNear = currentEnemiesNear;
                    }
                }
                clearanceScore = bestScore;
                if (blackboard.Role.RoleType == PlayerRoleType.Defender)
                {
                    float danger = CalculateDefenderDanger(blackboard);
                    clearanceScore += danger * FootballConstants.DefenderClearanceDangerBonus;
                }
                clearanceTarget = bestTarget;
                enemiesNearClearance = totalEnemiesNear;
                
                return new ClearanceEvaluation
                {
                    Score = clearanceScore,
                    Target = clearanceTarget,
                    EnemiesNearCount = enemiesNearClearance
                };
            }
            
            private static List<GameObject> FindEnemiesInFront(FootballBlackboard blackboard)
            {
                Vector3 enemyGoalPos = blackboard.MatchContext.GetEnemyGoalPosition(blackboard.Owner);
                var opponents = blackboard.MatchContext.GetOpponents(blackboard.Owner);
                return FootballUtils.FindEnemiesInFront(blackboard.Owner,
                    (enemyGoalPos - blackboard.Owner.transform.position).normalized, opponents,
                    FootballConstants.DribbleDetectDistance, FootballConstants.DribbleDetectHalfAngle);
            }
            
            private static float CalculateDribbleScore(List<GameObject> enemiesInFront, FootballBlackboard blackboard)
            {
                float score;
                if (enemiesInFront.Count == 0)
                {
                    score = FootballConstants.BaseDribbleScore + FootballConstants.DribbleClearBonus;
                }
                else
                {
                    float enemyCountPenalty = enemiesInFront.Count * FootballConstants.DribbleEnemyPenalty;
                    float enemyDistancePenalty = CalculateSpacePenalty(blackboard.Owner.transform.position, enemiesInFront);
                    score = Math.Max(FootballConstants.BaseDribbleScore - enemyCountPenalty - enemyDistancePenalty, 0);
                }

                if (blackboard.Role.RoleType == PlayerRoleType.Defender)
                {
                    float danger = CalculateDefenderDanger(blackboard);
                    score -= danger * FootballConstants.DefenderDribbleDangerPenalty;
                }

                return Mathf.Max(score, 0f);
            }

            private static float CalculateDefenderDanger(FootballBlackboard blackboard)
            {
                Vector3 ownerPos = blackboard.Owner.transform.position;
                float distanceToMyGoal = Vector3.Distance(ownerPos, blackboard.MatchContext.GetMyGoalPosition(blackboard.Owner));
                float goalDanger = 1f - Mathf.Clamp01(distanceToMyGoal / FootballConstants.DefenderDangerGoalDistance);

                float distanceToSideline = Mathf.Min(
                    Mathf.Abs(ownerPos.x - blackboard.MatchContext.GetLeftBorder()),
                    Mathf.Abs(blackboard.MatchContext.GetRightBorder() - ownerPos.x)
                );
                float sidelineDanger = 1f - Mathf.Clamp01(distanceToSideline / FootballConstants.DefenderDangerSidelineDistance);

                var opponents = blackboard.MatchContext.GetOpponents(blackboard.Owner);
                int nearbyEnemyCount = FootballUtils.FindNearEnemies(
                    blackboard.Owner,
                    opponents,
                    FootballConstants.DefenderDangerEnemyRadius
                ).Count;
                float pressureDanger = Mathf.Clamp01(nearbyEnemyCount / FootballConstants.DefenderDangerEnemyCountForMax);

                return Mathf.Clamp01(Mathf.Max(goalDanger, Mathf.Max(sidelineDanger, pressureDanger)));
            }
            
            private static float CalculateSpacePenalty(Vector3 ownerPos, List<GameObject> nearbyEnemies)
            {
                if (nearbyEnemies.Count == 0) return 0;
                float totalDistance = 0f;
                foreach (var enemy in nearbyEnemies)
                {
                    totalDistance += Vector3.Distance(ownerPos, enemy.transform.position);
                }
                float avgDistance = totalDistance / nearbyEnemies.Count;
                float spacePenalty = avgDistance * Mathf.Clamp01((avgDistance - 2f)/ 3f) * 10f; 
                return spacePenalty;
            }
            
            private static Vector3 GetSideStepTarget(Vector3 dribbleDirection, GameObject owner, GameObject closestBlockingEnemy, FootballBlackboard blackboard)
            {
                Vector3 sidestepDir = Vector3.Cross(Vector3.up, dribbleDirection);
                Vector3 leftPos = owner.transform.position + sidestepDir * FootballConstants.SidestepDistance;
                Vector3 rightPos = owner.transform.position - sidestepDir * FootballConstants.SidestepDistance;
                float leftDistToEnemy = Vector3.Distance(leftPos, closestBlockingEnemy.transform.position);
                float rightDistToEnemy = Vector3.Distance(rightPos, closestBlockingEnemy.transform.position);
                Vector3 sidestepPos;
                if(blackboard.MatchContext.IsInField(leftPos) && blackboard.MatchContext.IsInField(rightPos))
                    sidestepPos = leftDistToEnemy > rightDistToEnemy ? leftPos : rightPos;
                else if(blackboard.MatchContext.IsInField(leftPos)) 
                    sidestepPos = leftPos;
                else 
                    sidestepPos = rightPos;
                return FootballUtils.GetPositionTowards(owner.transform.position, sidestepPos, FootballConstants.DecideMinStep);
            }
            
            private static void CalculateDirectionClearanceScore(Vector3 clearanceDir, Vector3 targetPos, List<GameObject> opponents, FootballBlackboard blackboard, out float score, out int enemiesNearCount)
            {
                Vector3 startPos = blackboard.Owner.transform.position;
                score = FootballConstants.BaseClearanceScore;

                if (!FootballUtils.IsPathClear(startPos, targetPos, opponents, FootballConstants.ClearanceBlockThreshold))
                {
                    score -= FootballConstants.ClearanceScoreDistancePenalty;
                }

                var enemiesNear = FootballUtils.FindNearEnemies(blackboard.Owner, opponents, FootballConstants.ClearanceDetectDistance);
                enemiesNearCount = enemiesNear.Count;

                foreach (var enemyNear in enemiesNear)
                {
                    Vector3 meToEnemy = (enemyNear.transform.position - startPos).normalized;
                    float angleToEnemy = Vector3.Angle(clearanceDir, meToEnemy);

                    if (angleToEnemy < 30f)
                    {
                        score -= FootballConstants.ClearanceScorePerEnemy * 2;
                    }
                    else if (angleToEnemy < 60f)
                    {
                        score -= FootballConstants.ClearanceScorePerEnemy;
                    }
                }
            }

            public static void CalculatePassScoreAndTarget(out float passScore, out GameObject passTarget, out float lineSafety, 
                out float targetSafety, FootballBlackboard blackboard, float basePassScore)
            {
                passScore = 0f;
                passTarget = null;
                lineSafety = 0f;
                targetSafety = 0f;
                var teammates = blackboard.MatchContext.GetTeammates(blackboard.Owner);
                List<GameObject> enemyPlayers = blackboard.MatchContext.GetOpponents(blackboard.Owner);
                Vector3 enemyGoalPos = blackboard.MatchContext.GetEnemyGoalPosition(blackboard.Owner);
                float mid = (FootballConstants.PassMinDistance + FootballConstants.PassMaxDistance) / 2f;
                Vector3 ownerPosition = blackboard.Owner.transform.position;
                foreach (var candidate in teammates)
                {
                    Vector3 candidatePos = candidate.transform.position;
                    if(FootballUtils.IsPathClear(ownerPosition, candidatePos, enemyPlayers, FootballConstants.PassBlockThreshold))
                    {
                        float distance = Vector3.Distance(ownerPosition, candidatePos);
                        if(distance >= FootballConstants.PassMinDistance && distance <= FootballConstants.PassMaxDistance)
                        { 
                            float currentLineSafety = CalculatePassLineSafety(ownerPosition, candidatePos, enemyPlayers);
                            float currentTargetSafety = CalculatePassTargetSafety(candidate, enemyPlayers, enemyGoalPos);
                            float safetyFactor = currentLineSafety * currentTargetSafety;
                            float directionScore = CalculatePassDirectionScore(blackboard, candidate, enemyGoalPos);
                            float score = basePassScore * safetyFactor - 
                                FootballConstants.PassScoreDistancePenalty * Mathf.Abs(distance - mid) + 
                                directionScore;
                            if (score > passScore)
                            {
                                passScore = score;
                                passTarget = candidate;
                                lineSafety = currentLineSafety;
                                targetSafety = currentTargetSafety;
                            }
                        }
                    }
                }
            }

            private static float CalculatePassDirectionScore(FootballBlackboard blackboard, GameObject candidate, Vector3 enemyGoalPos)
            {
                Vector3 passDirection = (candidate.transform.position - blackboard.Owner.transform.position).normalized;
                Vector3 forwardDir = (enemyGoalPos - blackboard.Owner.transform.position).normalized;
                forwardDir.y = 0;
                float forwardAlignment = Vector3.Dot(passDirection, forwardDir);
                float directionBonus = forwardAlignment * FootballConstants.PassForwardDirectionBonus;
                return directionBonus;
            }
            
            public static float CalculatePassLineSafety(Vector3 start, Vector3 end, List<GameObject> enemies, float interceptThreshold = 3f)
            {
                float safetyScore = 1f; // 1.0 = 完全安全，0.0 = 极度危险
                foreach(var enemy in enemies)
                {
                    // 1. 检查是否在传球方向上
                    Vector3 passDirection = (end - start).normalized;
                    Vector3 enemyToStart = (enemy.transform.position - start).normalized;
                    // 如果敌人在传球反方向，跳过
                    if(Vector3.Dot(passDirection, enemyToStart) < 0) continue;
                    // 2. 计算到传球线段的距离
                    float distanceToLine = FootballUtils.DistancePointToLineSegment(start, end, enemy.transform.position);
                    // 3. 如果在线段威胁范围内，降低安全性
                    if(distanceToLine < interceptThreshold)
                    {
                        // 距离越近，威胁越大
                        float threatLevel = 1f - (distanceToLine / interceptThreshold);
                        safetyScore -= threatLevel * 0.5f; // 单个威胁最高降低50%安全性
                    }
                }
                return Mathf.Clamp01(safetyScore);
            }

            private static float CalculatePassTargetSafety(GameObject passTarget, List<GameObject> enemies, Vector3 enemyGoalPos)
            {
                float safetyScore = 1f; // 1.0 = 完全安全，0.0 = 极度危险
                float baseDistance = 2f; // 多少米以内算危险
                foreach (var enemy in enemies)
                {
                    float enemyDistance = Vector3.Distance(enemy.transform.position, passTarget.transform.position);
                    float threatLevel = Mathf.Max(0, 1 - enemyDistance / baseDistance) * 0.5f;// 单个威胁最高降低50%安全性
                    Vector3 targetToEnemyGoal = enemyGoalPos - passTarget.transform.position;
                    Vector3 targetToEnemy = enemy.transform.position - passTarget.transform.position;
                    bool isEnemyInFront = Vector3.Dot(targetToEnemy, targetToEnemyGoal) > 0;
                    safetyScore = safetyScore - threatLevel * (isEnemyInFront ? 1 : 0.75f);
                }
                return Mathf.Clamp01(safetyScore);
            }
        }

        #region 日志相关
        
        #region 日志数据结构
        public struct OffensiveEvaluationLog
        {
            public string PlayerName;
            public PlayerRoleType RoleType;

            public float ShootScore;
            public Vector3 ShootTarget;
            public float PassScore;
            public GameObject PassTarget;
            public float DribbleScore;
            public Vector3 DribbleTarget;
            public float ClearanceScore;
            public Vector3 ClearanceTarget;

            public int EnemiesInFront;
            public int EnemiesNearClearance;
            public float LineSafety;
            public float TargetSafety;

            public string SelectedAction;
            public string TargetDescription;

            public OffensiveEvaluationLog(string playerName, PlayerRoleType roleType,
                float shootScore, Vector3 shootTarget,
                float passScore, GameObject passTarget,
                float dribbleScore, Vector3 dribbleTarget,
                float clearanceScore, Vector3 clearanceTarget,
                int enemiesInFront, int enemiesNearClearance,
                float lineSafety, float targetSafety)
            {
                PlayerName = playerName;
                RoleType = roleType;
                ShootScore = shootScore;
                ShootTarget = shootTarget;
                PassScore = passScore;
                PassTarget = passTarget;
                DribbleScore = dribbleScore;
                DribbleTarget = dribbleTarget;
                ClearanceScore = clearanceScore;
                ClearanceTarget = clearanceTarget;
                EnemiesInFront = enemiesInFront;
                EnemiesNearClearance = enemiesNearClearance;
                LineSafety = lineSafety;
                TargetSafety = targetSafety;
                SelectedAction = "";
                TargetDescription = "";
            }

            public void SetPassAction(GameObject passTarget, Vector3 ownerPosition)
            {
                SelectedAction = "传球";
                TargetDescription = passTarget != null
                    ? $"目标: {passTarget.name} (距离: {Vector3.Distance(ownerPosition, passTarget.transform.position):F2}m)"
                    : "无目标";
            }

            public void SetDribbleAction(Vector3 dribbleTarget)
            {
                SelectedAction = "带球";
                TargetDescription = $"目标: {dribbleTarget}";
            }

            public void SetShootAction(Vector3 shootTarget, Vector3 ownerPosition)
            {
                SelectedAction = "射门";
                TargetDescription = $"目标: {shootTarget} (距离: {Vector3.Distance(ownerPosition, shootTarget):F2}m)";
            }

            public void SetClearanceAction(Vector3 clearanceTarget)
            {
                SelectedAction = "解围";
                TargetDescription = $"目标: {clearanceTarget}";
            }
        }
        #endregion
        
        private void LogOffensiveEvaluation(OffensiveAction action, string strategyName)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("========== 进攻选择评估 ==========");
            sb.AppendLine($"球员: {Blackboard.Owner.name} | 角色: {Blackboard.Role.RoleType} | 策略: {strategyName}");
            sb.AppendLine("----------------------------------------");
            
            var details = action.Details;
            
            sb.AppendLine("【评分详情】");
            if (details.ShootScore > 0)
                sb.AppendLine($"射门分: {details.ShootScore:F2}");
            if (details.PassScore > 0)
                sb.AppendLine($"传球分: {details.PassScore:F2} | 目标: {(action.PassTarget != null ? action.PassTarget.name : "无")}");
            if (details.DribbleScore > 0)
                sb.AppendLine($"带球分: {details.DribbleScore:F2} | 目标: {action.MoveTarget}");
            if (details.ClearanceScore > 0)
                sb.AppendLine($"解围分: {details.ClearanceScore:F2} | 目标: {action.ClearanceTarget}");
            
            if (Blackboard.Role.RoleType == PlayerRoleType.Defender)
            {
                sb.AppendLine("\n【环境分析】");
                sb.AppendLine($"前方敌人数量: {details.EnemiesInFront}");
                sb.AppendLine($"线路安全性: {details.LineSafety:F2}");
                sb.AppendLine($"目标安全性: {details.TargetSafety:F2}");
            }
            else if (Blackboard.Role.RoleType == PlayerRoleType.Forward)
            {
                sb.AppendLine("\n【传球细节】");
                sb.AppendLine($"线路安全性: {details.LineSafety:F2}");
                sb.AppendLine($"目标安全性: {details.TargetSafety:F2}");
                sb.AppendLine($"前方敌人数量: {details.EnemiesInFront}");
            }
            
            sb.AppendLine("----------------------------------------");
            sb.AppendLine($"【最终选择】 {GetActionTypeName(action.ActionType)} | 得分: {action.Score:F2}");
            sb.AppendLine("======================================");
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            //Debug.Log(sb.ToString());
            #endif
        }
        
        private string GetActionTypeName(OffensiveActionType actionType)
        {
            switch (actionType)
            {
                case OffensiveActionType.Shoot: return "射门";
                case OffensiveActionType.Pass: return "传球";
                case OffensiveActionType.Dribble: return "带球";
                case OffensiveActionType.Clearance: return "解围";
                default: return "无";
            }
        }
        
        private void LogOffensiveEvaluation(OffensiveEvaluationLog log)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("========== 进攻选择评估 ==========");
            sb.AppendLine($"球员: {log.PlayerName} | 角色: {log.RoleType}");
            sb.AppendLine("----------------------------------------");

            if (log.RoleType == PlayerRoleType.Defender)
            {
                sb.AppendLine("【评分详情】");
                sb.AppendLine($"传球分: {log.PassScore:F2} | 目标: {(log.PassTarget != null ? log.PassTarget.name : "无")}");
                sb.AppendLine($"带球分: {log.DribbleScore:F2} | 目标: {log.DribbleTarget}");
                sb.AppendLine($"解围分: {log.ClearanceScore:F2} | 目标: {log.ClearanceTarget}");

                sb.AppendLine("\n【环境分析】");
                sb.AppendLine($"前方敌人数量: {log.EnemiesInFront}");
                sb.AppendLine($"近距离威胁敌人数量: {log.EnemiesNearClearance}");
            }
            else if (log.RoleType == PlayerRoleType.Forward)
            {
                sb.AppendLine("【评分详情】");
                sb.AppendLine($"射门分: {log.ShootScore:F2} | 目标: {log.ShootTarget}");
                sb.AppendLine($"传球分: {log.PassScore:F2} | 目标: {(log.PassTarget != null ? log.PassTarget.name : "无")}");
                sb.AppendLine($"带球分: {log.DribbleScore:F2} | 目标: {log.DribbleTarget}");

                sb.AppendLine("\n【传球细节】");
                sb.AppendLine($"线路安全性: {log.LineSafety:F2}");
                sb.AppendLine($"目标安全性: {log.TargetSafety:F2}");
                sb.AppendLine($"前方敌人数量: {log.EnemiesInFront}");
            }
            sb.AppendLine("----------------------------------------");
            sb.AppendLine($"【最终选择】 {log.SelectedAction}");
            if (!string.IsNullOrEmpty(log.TargetDescription))
            {
                sb.AppendLine($"目标详情: {log.TargetDescription}");
            }
            sb.AppendLine("======================================");
            //MyLog.LogInfo(sb.ToString());
        }
        #endregion
    }
}
