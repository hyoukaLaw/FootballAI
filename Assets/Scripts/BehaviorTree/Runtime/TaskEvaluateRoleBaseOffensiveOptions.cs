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

        // 百分制计分
        // 后卫：优先传球，其次向前带球，压迫大就解围
        // 传球目标：找距离合适且没压迫的人作为传球目标，3m-8m，多和少都扣分
        public override NodeState Evaluate()
        {
            if (Blackboard.Role.RoleType == PlayerRoleType.Defender)
            {
                HandleDefenderOptions();
                return NodeState.SUCCESS;
            }
            else if (Blackboard.Role.RoleType == PlayerRoleType.Forward)
            {
                HandleForwardOptions();
                return NodeState.SUCCESS;   
            }
            return NodeState.FAILURE;
        }

        private void HandleDefenderOptions()
        {
            OffensiveActionCalculator.CalculatePassScoreAndTarget(out float passScore, out GameObject passTarget, Blackboard, FootballConstants.BasePassScoreDefender);
            CalculateDribbleScoreAndTarget(out float dribbleScore, out Vector3 dribbleTarget);
            CalculateClearanceScoreAndTarget(out float clearanceScore, out Vector3 clearanceTarget);
            Debug.Log($"{Blackboard.Owner.name} TaskEvaluateRoleBaseOffensiveOptions " +
                      $"PassScore:{passScore}, DribbleScore:{dribbleScore}, ClearanceScore:{clearanceScore} ");
            if (passScore > dribbleScore && passScore > clearanceScore)
            {
                Blackboard.MoveTarget = Vector3.zero;
                Blackboard.BestPassTarget = passTarget;
                Blackboard.ClearanceTarget = Vector3.negativeInfinity;
            }
            else if (dribbleScore > passScore && dribbleScore > clearanceScore)
            {
                Blackboard.MoveTarget = dribbleTarget;
                Blackboard.BestPassTarget = null;
                Blackboard.ClearanceTarget = Vector3.negativeInfinity;
            }
            else
            {
                Blackboard.MoveTarget = Vector3.zero;
                Blackboard.BestPassTarget = null;
                Blackboard.ClearanceTarget = clearanceTarget;
            }
        }
        
        #region Defender
        private void CalculateDribbleScoreAndTarget(out float dribbleScore, out Vector3 dribbleTarget)
        {
            List<GameObject> enemiesInFront = FindEnemiesInFront();
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

        public float CalculateDribbleScore(List<GameObject> enemiesInFront)
        {
            if(enemiesInFront.Count == 0)
                return FootballConstants.BaseDribbleScore + FootballConstants.DribbleClearBonus;
            else
                return Math.Max(FootballConstants.BaseDribbleScore - enemiesInFront.Count * FootballConstants.DribbleEnemyPenalty,0);
        }
        
        private List<GameObject> FindEnemiesInFront()
        {
            Vector3 enemyGoalPos = Blackboard.MatchContext.GetEnemyGoalPosition(Blackboard.Owner);
            var opponents = Blackboard.MatchContext.GetOpponents(Blackboard.Owner);
            return FootballUtils.FindEnemiesInFront(Blackboard.Owner,
                (enemyGoalPos - Blackboard.Owner.transform.position).normalized, opponents,
                FootballConstants.DribbleDetectDistance, FootballConstants.DribbleDetectHalfAngle);
        }
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
        private void CalculateClearanceScoreAndTarget(out float clearanceScore, out Vector3 clearanceTarget)
        {
            var opponents = Blackboard.MatchContext.GetOpponents(Blackboard.Owner);
            Vector3 forwardDir = FootballUtils.GetForward(Blackboard.Owner);

            float[] angles = { 0f, 45f, -45f };
            float bestScore = float.MinValue;
            Vector3 bestTarget = Vector3.zero;

            foreach (float angle in angles)
            {
                float rad = angle * Mathf.Deg2Rad;
                Vector3 clearanceDir = Quaternion.Euler(0, rad, 0) * forwardDir;
                Vector3 candidateTarget = Blackboard.Owner.transform.position + clearanceDir * FootballConstants.ClearanceDistance;

                float score = CalculateDirectionClearanceScore(clearanceDir, candidateTarget, opponents);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = candidateTarget;
                }
            }

            clearanceScore = bestScore;
            clearanceTarget = bestTarget;
        }

        private float CalculateDirectionClearanceScore(Vector3 clearanceDir, Vector3 targetPos, List<GameObject> opponents)
        {
            Vector3 startPos = Blackboard.Owner.transform.position;
            float score = FootballConstants.BaseClearanceScore;

            if (!FootballUtils.IsPathClear(startPos, targetPos, opponents, FootballConstants.ClearanceBlockThreshold))
            {
                score -= FootballConstants.ClearanceScoreDistancePenalty;
            }

            var enemiesNear = FootballUtils.FindNearEnemies(Blackboard.Owner, opponents, FootballConstants.ClearanceDetectDistance);
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

            return score;
        }
        #endregion
        
        #region Forward

        private void HandleForwardOptions()
        {
            CalculateShootScoreAndTarget(out float shootScore, out Vector3 shootTarget);
            CalculateDribbleScoreAndTarget(out float dribbleScore, out Vector3 dribbleTarget);
            OffensiveActionCalculator.CalculatePassScoreAndTarget(out float passScore, out GameObject passTarget, Blackboard, FootballConstants.BasePassScoreForward);
            Debug.Log($"{Blackboard.Owner.name} TaskEvaluateRoleBaseOffensiveOptions " +
                      $"ShootScore:{shootScore}, DribbleScore:{dribbleScore}, PassScore:{passScore} ");
            if (shootScore > dribbleScore && shootScore > passScore)
            {
                Blackboard.CanShoot = true;
                Blackboard.MoveTarget = Vector3.zero;
                Blackboard.BestPassTarget = null;
            }
            else if (dribbleScore > shootScore && dribbleScore > passScore)
            {
                Blackboard.CanShoot = false;
                Blackboard.MoveTarget = dribbleTarget;
                Blackboard.BestPassTarget = null;
            }
            else
            {
                Blackboard.CanShoot = false;
                Blackboard.MoveTarget = Vector3.zero;
                Blackboard.BestPassTarget = passTarget;
            }
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
            public static void CalculatePassScoreAndTarget(out float passScore, out GameObject passTarget, FootballBlackboard blackboard, float basePassScore)
            {
                passScore = 0f;
                passTarget = null;
                var teammates = blackboard.MatchContext.GetTeammates(blackboard.Owner);
                List<GameObject> enemyPlayers = blackboard.MatchContext.GetOpponents(blackboard.Owner);
                Vector3 enemyGoalPos = blackboard.MatchContext.GetEnemyGoalPosition(blackboard.Owner);
                foreach (var candidate in teammates)
                {
                    if(FootballUtils.IsPathClear(blackboard.Owner.transform.position, 
                           candidate.transform.position, enemyPlayers, 
                           FootballConstants.PassBlockThreshold))
                    {
                        float mid = (FootballConstants.PassMinDistance + FootballConstants.PassMaxDistance) / 2f;
                        float distance = Vector3.Distance(blackboard.Owner.transform.position, candidate.transform.position);
                        if(distance >= FootballConstants.PassMinDistance && distance <= FootballConstants.PassMaxDistance)
                        {
                            float safetyFactor = CalculatePassLineSafety(blackboard.Owner.transform.position,
                                candidate.transform.position, enemyPlayers) * CalculatePassTargetSafety(candidate, enemyPlayers, enemyGoalPos);
                            float score = basePassScore * safetyFactor - FootballConstants.PassScoreDistancePenalty * Mathf.Abs(distance - mid);
                            if (score > passScore)
                            {
                                passScore = score;
                                passTarget = candidate;
                            }
                        }
                    }
                }
            }
            
            public static float CalculatePassLineSafety(Vector3 start, Vector3 end, List<GameObject> enemies, float interceptThreshold = 1.5f)
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
                    safetyScore = safetyScore - threatLevel * (isEnemyInFront ? 1 : 0.5f);
                }
                return Mathf.Clamp01(safetyScore);
            }
        }
    }
}