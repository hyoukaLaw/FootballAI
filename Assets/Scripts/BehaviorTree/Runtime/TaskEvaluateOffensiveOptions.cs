using System;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviorTree.Runtime
{
    public class TaskEvaluateOffensiveOptions : Node
    {
        // 评分权重配置
        private float _basePassScore = FootballConstants.BasePassScore;
        private float _forwardWeight = FootballConstants.ForwardWeight; // 越靠前的队友分越高
        private float _distanceWeight = FootballConstants.DistanceWeight; // 距离适中的队友分高

        // 盘带配置
        private float _dribbleDistance = FootballConstants.DribbleForwardDistance; // 盘带前进距离
        private float _detectRange = FootballConstants.ForwardDetectionDistance; // 前方检测距离
        private float _detectAngle = FootballConstants.DetectionAngleHalf; // 检测角度（半角）
        private float _sidestepDistance = FootballConstants.SidestepDistance; // 侧移距离

        // 射门配置
        private float _shootDistance = FootballConstants.ShootDistance; // 射门距离
        private float _shootAngleThreshold = FootballConstants.ShootAngleThreshold; // 射门角度阈值

        public TaskEvaluateOffensiveOptions(FootballBlackboard blackboard) : base(blackboard)
        {
        }

        public override NodeState Evaluate()
        {
            Vector3 currentPos = Blackboard.Owner.transform.position;
            // 优先射门
            Vector3 enemyGoalPosition = Blackboard.MatchContext.GetEnemyGoalPosition(Blackboard.Owner);
            float distToGoal = Vector3.Distance(Blackboard.Owner.transform.position, enemyGoalPosition);
            float shootProb = Math.Max(FootballConstants.ShootDistanceBase - distToGoal, 0) / FootballConstants.ShootDistanceBase;
            float shootBlockFactor = IsPathClear(currentPos, enemyGoalPosition) ? 1f : FootballConstants.ShootBlockPenaltyFactor;
            float shootScore = shootProb * FootballConstants.BaseShootScore * shootBlockFactor;
            
            
            // 接下来是传球
            float bestPassScore = 0f;
            GameObject bestPassTarget = null;
            foreach (var mate in Blackboard.MatchContext.GetTeammates(Blackboard.Owner))
            {
                if(mate == Blackboard.Owner) continue;
                float passScore = CalculatePassScore(Blackboard.Owner, mate, enemyGoalPosition);
                if (passScore > bestPassScore)
                {
                    bestPassScore = passScore;
                    bestPassTarget = mate;
                }
            }
            
            // 最后是带球
            float dribbleScore = CalculateDribbleScore(out List<GameObject> enemiesInFront);
            Vector3 dribbleTarget = GetDribbleTarget(enemiesInFront, enemyGoalPosition);
            Debug.Log($"TaskEvaluateOffensiveOptions({Blackboard.Owner.name}): " +
                      $"shootScore={shootScore}(shootBlockFactor={shootBlockFactor}), bestPassScore={bestPassScore}, dribbleScore={dribbleScore}" +
                      $"bestPassTarget={bestPassTarget?.name}, dribbleTarget={dribbleTarget}");
            
            if(shootScore > bestPassScore && shootScore > dribbleScore)
            {
                DecideToShoot();
            }
            else if(bestPassScore > dribbleScore)
            {
                DecideToPass(bestPassTarget);
            }
            else
            {
                DecideToDribble(dribbleTarget);
            }
            
            return NodeState.SUCCESS;
        }

        public void DecideToShoot()
        {
            Blackboard.CanShoot = true;
            Blackboard.BestPassTarget = null;
            Blackboard.MoveTarget = Vector3.zero;
        }
        
        public void DecideToPass(GameObject bestPassTarget)
        {
            Blackboard.CanShoot = false;
            Blackboard.BestPassTarget = bestPassTarget;
            Blackboard.MoveTarget = Vector3.zero;
        }
        
        public void DecideToDribble(Vector3 dribbleTarget)
        {
            Blackboard.CanShoot = false;
            Blackboard.BestPassTarget = null;
            Blackboard.MoveTarget = dribbleTarget;
        }

        // === 辅助：判断是否可以射门 ===
        private bool CanShoot(GameObject shooter, Vector3 goalPos)
        {
            // 1. 距离判断：必须在射门范围内
            float distToGoal = Vector3.Distance(shooter.transform.position, goalPos);
            if (distToGoal > _shootDistance)
            {
                return false;
            }

            // 2. 角度判断：必须大致面向球门
            Vector3 toGoal = (goalPos - shooter.transform.position).normalized;
            toGoal.y = 0;
            float angle = Vector3.Angle(shooter.transform.forward, toGoal);

            if (angle > _shootAngleThreshold)
            {
                return false;
            }

            // 3. 安全性判断：前方是否有阻挡
            if (!IsPathClear(shooter.transform.position, goalPos))
            {
                return false;
            }

            return true;
        }

        // === 辅助：给传球目标打分 ===
        private float CalculatePassScore(GameObject me, GameObject mate, Vector3 goalPos)
        {
            float myDist = Vector3.Distance(me.transform.position, goalPos);
            float mateDist = Vector3.Distance(mate.transform.position, goalPos);
            float improvement = (myDist - mateDist) * FootballConstants.PassForwardWeight;
            float passProb = IsPathClear(me.transform.position, mate.transform.position) ? 1f:FootballConstants.PassBlockPenaltyFactor;
            return (FootballConstants.BasePassScore + improvement * FootballConstants.PassForwardWeight) * passProb;
        }

        private float CalculateDribbleScore(out List<GameObject> enemiesInFront)
        {
            Vector3 enemyGoalPos = Blackboard.MatchContext.GetEnemyGoalPosition(Blackboard.Owner);
            enemiesInFront = FindEnemiesInFront(Blackboard.Owner,
                (enemyGoalPos - Blackboard.Owner.transform.position).normalized);
            if(enemiesInFront.Count == 0)
                return FootballConstants.BaseDribbleScore + FootballConstants.DribbleClearBonus;
            return FootballConstants.BaseDribbleScore - enemiesInFront.Count * FootballConstants.DribbleEnemyPenalty;
        }

        public Vector3 GetDribbleTarget(List<GameObject> enemiesInFront, Vector3 goalPos)
        {
            GameObject closestBlockingEnemy = null;
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
            Vector3 potentialDribblePos;
            GameObject owner = Blackboard.Owner;
            if (closestBlockingEnemy != null)
            {
                // 前方有阻挡，侧向移动绕过
                Vector3 sidestepDir = Vector3.Cross(Vector3.up, dribbleDirection);

                // 判断往左还是往右移：选择离球门更近的方向
                Vector3 leftPos = owner.transform.position + sidestepDir * _sidestepDistance;
                Vector3 rightPos = owner.transform.position - sidestepDir * _sidestepDistance;
                float leftDistToGoal = Vector3.Distance(leftPos, goalPos);
                float rightDistToGoal = Vector3.Distance(rightPos, goalPos);
                float leftDistToEnemy = Vector3.Distance(leftPos, closestBlockingEnemy.transform.position);
                float rightDistToEnemy = Vector3.Distance(rightPos, closestBlockingEnemy.transform.position);

                Vector3 sidestepPos = leftDistToGoal < rightDistToGoal ? leftPos : rightPos;
                potentialDribblePos = sidestepPos + dribbleDirection;

                potentialDribblePos = owner.transform.position +
                                      (potentialDribblePos - owner.transform.position).normalized;
                Debug.Log($"dribble: {(potentialDribblePos - owner.transform.position).normalized} {owner.transform.position} {potentialDribblePos}");
            }
            else
            {
                // 前方无阻挡，直接带球
                potentialDribblePos = owner.transform.position + dribbleDirection.normalized;
            }
            return potentialDribblePos;
        }

        // === 辅助：检查路径是否安全 ===
        private bool IsPathClear(Vector3 start, Vector3 end)
        {
            if (Blackboard.MatchContext == null) return true;

            // 遍历 Context.Opponents 检测是否在路径上
            var owner = Blackboard.Owner;
            var opponents = Blackboard.MatchContext.GetOpponents(owner);
            if (opponents == null) return true;

            foreach (var enemy in opponents)
            {
                if (enemy == null) continue;
                if (Vector3.Dot(end - start, enemy.transform.position - start) < 0)
                    continue;

                // 计算点到线段的距离
                float distToLine = DistancePointToLineSegment(start, end, enemy.transform.position);

                // 如果敌人距离传球路线小于敌人阻挡距离阈值，认为会被阻挡
                if (distToLine < FootballConstants.EnemyBlockDistanceThreshold)
                {
                    return false; // 被阻挡
                }
            }

            return true; // 安全
        }

        // === 辅助：计算点到线段的距离 ===
        private float DistancePointToLineSegment(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 ab = b - a;
            Vector3 ap = p - a;
            float magOfab2 = ab.sqrMagnitude;
            if (magOfab2 == 0) return (p - a).magnitude;
            float t = Vector3.Dot(ap, ab) / magOfab2;
            if (t < 0)
                return (p - a).magnitude;
            else if (t > 1)
                return (p - b).magnitude;
            Vector3 closestPoint = a + ab * t;
            return (p - closestPoint).magnitude;
        }
        

        // === 辅助：查找前方阻挡的敌人 ===
        private List<GameObject> FindEnemiesInFront(GameObject owner, Vector3 forwardDir)
        {
            var opponents = Blackboard.MatchContext.GetOpponents(owner);
            List<GameObject> enemiesInFront = new List<GameObject>();
            foreach (var enemy in opponents)
            {
                if (enemy == null) continue;

                Vector3 toEnemy = enemy.transform.position - owner.transform.position;
                float distance = toEnemy.magnitude;
                float angle = Vector3.Angle(forwardDir, toEnemy.normalized);
                // 检查距离和角度
                if (distance <= FootballConstants.DribbleDetectDistance && angle <= FootballConstants.DribbleDetectHalfAngle)
                {
                    enemiesInFront.Add(enemy);
                }
            }

            return enemiesInFront;
        }
    }
}