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
            return NodeState.FAILURE;
        }

        private void HandleDefenderOptions()
        {
            CalculatePassScoreAndTarget(out float passScore, out GameObject passTarget);
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
        
        private void CalculatePassScoreAndTarget(out float passScore, out GameObject passTarget)
        {
            passScore = 0f;
            passTarget = null;
            var teammates = Blackboard.MatchContext.GetTeammates(Blackboard.Owner);
            List<GameObject> enemyPlayers = Blackboard.MatchContext.GetOpponents(Blackboard.Owner);
            foreach (var candidate in teammates)
            {
                if(FootballUtils.IsPathClear(Blackboard.Owner.transform.position, 
                       candidate.transform.position, enemyPlayers, 
                       FootballConstants.PassBlockThreshold))
                {
                    float mid = (FootballConstants.PassMinDistance + FootballConstants.PassMaxDistance) / 2f;
                    float distance = Vector3.Distance(Blackboard.Owner.transform.position, candidate.transform.position);
                    if(distance >= FootballConstants.PassMinDistance && distance <= FootballConstants.PassMaxDistance)
                    {
                        float score = FootballConstants.BasePassScoreDefender - FootballConstants.PassScoreDistancePenalty * Mathf.Abs(distance - mid);
                        if (score > passScore)
                        {
                            passScore = score;
                            passTarget = candidate;
                        }
                    }
                }
            }
        }
        
        private void CalculateDribbleScoreAndTarget(out float dribbleScore, out Vector3 dribbleTarget)
        {
            dribbleScore = 0f;
            dribbleTarget = Vector3.zero;
            Vector3 enemyGoalPos = Blackboard.MatchContext.GetEnemyGoalPosition(Blackboard.Owner);
            var opponents = Blackboard.MatchContext.GetOpponents(Blackboard.Owner);
            var enemiesInFront = FootballUtils.FindEnemiesInFront(Blackboard.Owner,
                (enemyGoalPos - Blackboard.Owner.transform.position).normalized, opponents,
                FootballConstants.DribbleDetectDistance, FootballConstants.DribbleDetectHalfAngle);
            
            if(enemiesInFront.Count == 0)
                dribbleScore = FootballConstants.BaseDribbleScore + FootballConstants.DribbleClearBonus;
            else
                dribbleScore = Math.Max(FootballConstants.BaseDribbleScore - enemiesInFront.Count * FootballConstants.DribbleEnemyPenalty,0);
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
                Vector3 sidestepDir = Vector3.Cross(Vector3.up, dribbleDirection).normalized;
                // 判断往左还是往右移：选择离球门更近的方向
                Vector3 leftPos = owner.transform.position + sidestepDir;
                Vector3 rightPos = owner.transform.position - sidestepDir;
                float leftDistToEnemy = Vector3.Distance(leftPos, closestBlockingEnemy.transform.position);
                float rightDistToEnemy = Vector3.Distance(rightPos, closestBlockingEnemy.transform.position);
                dribbleTarget = leftDistToEnemy > rightDistToEnemy ? leftPos : rightPos;
                dribbleTarget = owner.transform.position + (dribbleTarget - owner.transform.position).normalized;
            }
            else
            {
                // 前方无阻挡，直接带球
                dribbleTarget = owner.transform.position + dribbleDirection.normalized;
            }
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
    }
}