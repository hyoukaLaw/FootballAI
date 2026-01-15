using UnityEngine;

namespace BehaviorTree
{
    public class TaskEvaluateOffensiveOptions : Node
    {
        // 评分权重配置
        private float _basePassScore = 50f;
        private float _forwardWeight = 2.0f; // 越靠前的队友分越高
        private float _distanceWeight = 1.0f; // 距离适中的队友分高

        // 盘带配置
        private float _dribbleDistance = 0.1f; // 盘带前进距离
        private float _detectRange = 3.5f; // 前方检测距离
        private float _detectAngle = 90f; // 检测角度（半角）
        private float _sidestepDistance = 3.0f; // 侧移距离

        // 射门配置
        private float _shootDistance = 12f; // 射门距离
        private float _shootAngleThreshold = 30f; // 射门角度阈值

        public TaskEvaluateOffensiveOptions(FootballBlackboard blackboard) : base(blackboard)
        {
        }

        public override NodeState Evaluate()
        {
            // 防御性编程：检查上下文
            if (Blackboard.MatchContext == null)
                return NodeState.FAILURE;

            // 清理上一帧的数据
            Blackboard.BestPassTarget = null;
            Blackboard.MoveTarget = Vector3.zero;
            Blackboard.CanShoot = false;

            GameObject owner = Blackboard.Owner;
            Vector3 goalPos = Blackboard.MatchContext.GetEnemyGoalPosition(Blackboard.Owner);

            // === 0. 评估射门条件 (最高优先级) ===
            if (CanShoot(owner, goalPos))
            {
                Blackboard.CanShoot = true;
                Blackboard.BestPassTarget = null; // 明确不传球
                Blackboard.MoveTarget = Vector3.zero; // 明确不移动
                return NodeState.SUCCESS;
            }

            // === 1. 评估传球选项 (寻找最佳队友) ===
            GameObject bestMate = null;
            float highestScore = -1f;

            var teammates = Blackboard.MatchContext.GetTeammates(owner);
            if (teammates == null) return NodeState.FAILURE;

            foreach (var mate in teammates)
            {
                if (mate == owner) continue; // 排除自己

                float score = CalculatePassScore(owner, mate, goalPos);

                if (score > highestScore)
                {
                    highestScore = score;
                    bestMate = mate;
                }
            }

            // 设定传球阈值：如果最高分都低于 60 分，说明没有好机会，不如自己带球
            if (highestScore > 60f)
            {
                Blackboard.BestPassTarget = bestMate;
                // 决策完成：建议传球
                NodeState = NodeState.SUCCESS;
                return NodeState;
            }

            // === 2. 评估盘带选项 (如果传球不好，计算向前带球点) ===

            Vector3 dribbleDirection = (goalPos - owner.transform.position).normalized;
            dribbleDirection.y = 0;

            // 检测前方扇形区域内是否有敌人
            GameObject blockingEnemy = FindEnemyInFront(owner, dribbleDirection);

            Vector3 potentialDribblePos;

            if (blockingEnemy != null)
            {
                // 前方有阻挡，侧向移动绕过
                Vector3 sidestepDir = Vector3.Cross(Vector3.up, dribbleDirection);

                // 判断往左还是往右移：选择离球门更近的方向
                Vector3 leftPos = owner.transform.position + sidestepDir * _sidestepDistance;
                Vector3 rightPos = owner.transform.position - sidestepDir * _sidestepDistance;
                float leftDistToGoal = Vector3.Distance(leftPos, goalPos);
                float rightDistToGoal = Vector3.Distance(rightPos, goalPos);

                Vector3 sidestepPos = leftDistToGoal < rightDistToGoal ? leftPos : rightPos;
                potentialDribblePos = sidestepPos + dribbleDirection;

                potentialDribblePos = owner.transform.position +
                                      (potentialDribblePos - owner.transform.position).normalized *
                                      MatchContext.MoveSegment;
                Debug.Log($"dribble: {(potentialDribblePos - owner.transform.position).normalized} {owner.transform.position} {potentialDribblePos}");
            }
            else
            {
                // 前方无阻挡，直接带球
                potentialDribblePos = owner.transform.position + dribbleDirection * MatchContext.MoveSegment;
            }

            Blackboard.MoveTarget = potentialDribblePos;
            Blackboard.BestPassTarget = null; // 明确表示不传球

            // 决策完成：建议盘带
            NodeState = NodeState.SUCCESS;
            return NodeState;
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
        private float CalculatePassScore(GameObject me, GameObject mate, Vector3 goal)
        {
            float score = _basePassScore;
            float distToMate = Vector3.Distance(me.transform.position, mate.transform.position);

            // A. 距离评分：太远容易失误，太近没意义
            if (distToMate < 3f) score -= 40f; // 太近
            if (distToMate > 10f) score -= 30f; // 太远

            // B. 进攻性评分：队友比我更接近球门吗？
            float myDistToGoal = Vector3.Distance(me.transform.position, goal);
            float mateDistToGoal = Vector3.Distance(mate.transform.position, goal);
            float forwardGain = myDistToGoal - mateDistToGoal; // 正数表示队友更靠前

            score += forwardGain * _forwardWeight;

            // C. 安全性评分 (阻挡检测)
            if (!IsPathClear(me.transform.position, mate.transform.position))
            {
                return -100f; // 被阻挡，直接废弃
            }

            return score;
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

                // 计算点到线段的距离
                float distToLine = DistancePointToLineSegment(start, end, enemy.transform.position);

                // 如果敌人距离传球路线小于 1.5米，认为会被阻挡
                if (distToLine < 1.5f)
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
        private GameObject FindEnemyInFront(GameObject owner, Vector3 forwardDir)
        {
            if (Blackboard.MatchContext == null) return null;

            var opponents = Blackboard.MatchContext.GetOpponents(owner);
            if (opponents == null) return null;

            foreach (var enemy in opponents)
            {
                if (enemy == null) continue;

                Vector3 toEnemy = enemy.transform.position - owner.transform.position;
                toEnemy.y = 0;
                float distance = toEnemy.magnitude;

                // 检查距离和角度
                if (distance <= _detectRange)
                {
                    float angle = Vector3.Angle(forwardDir, toEnemy.normalized);
                    if (angle <= _detectAngle)
                    {
                        return enemy;
                    }
                }
            }

            return null;
        }
    }
}
