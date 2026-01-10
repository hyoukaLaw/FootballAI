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
        private float _dribbleDistance = 5.0f; // 盘带前进距离
        private float _detectRange = 3.5f; // 前方检测距离
        private float _detectAngle = 90f; // 检测角度（半角）
        private float _sidestepDistance = 3.0f; // 侧移距离

        public TaskEvaluateOffensiveOptions(FootballBlackboard blackboard) : base(blackboard)
        {
        }

        public override NodeState Evaluate()
        {
            // 清理上一帧的数据
            Blackboard.BestPassTarget = null;
            Blackboard.MoveTarget = Vector3.zero;

            GameObject owner = Blackboard.Owner;
            Vector3 goalPos = Blackboard.EnemyGoalPosition;

            // --- 1. 评估传球选项 (寻找最佳队友) ---
            GameObject bestMate = null;
            float highestScore = -1f;

            foreach (var mate in Blackboard.Teammates)
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
                Debug.Log($"传球分 {highestScore}");
                Debug.Log($"{owner.gameObject.name} forwardgain:>60 {owner.transform.position} {bestMate.transform.position} {goalPos}");
                return NodeState;
            }

            // --- 2. 评估盘带选项 (如果传球不好，计算向前带球点) ---

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
                potentialDribblePos = sidestepPos + dribbleDirection * _dribbleDistance;

                Debug.DrawLine(owner.transform.position, potentialDribblePos, Color.yellow, 1.0f);
                Debug.Log($"前方阻挡，侧向盘带: {blockingEnemy.name}");
            }
            else
            {
                // 前方无阻挡，直接带球
                potentialDribblePos = owner.transform.position + dribbleDirection * _dribbleDistance;
                Debug.DrawLine(owner.transform.position, potentialDribblePos, Color.green, 1.0f);
            }

            Blackboard.MoveTarget = potentialDribblePos;
            Blackboard.BestPassTarget = null; // 明确表示不传球

            // 决策完成：建议盘带
            NodeState = NodeState.SUCCESS;
            return NodeState;
        }

        // --- 辅助：给传球目标打分 ---
        private float CalculatePassScore(GameObject me, GameObject mate, Vector3 goal)
        {
            float score = _basePassScore;
            float distToMate = Vector3.Distance(me.transform.position, mate.transform.position);

            // A. 距离评分：太远容易失误，太近没意义
            if (distToMate < 3f) score -= 40f; // 太近
            if (distToMate > 10f) score -= 30f; // 太远

            // B. 进攻性评分：队友比我更接近球门吗？
            // 计算 "球门距离差"
            float myDistToGoal = Vector3.Distance(me.transform.position, goal);
            float mateDistToGoal = Vector3.Distance(mate.transform.position, goal);
            float forwardGain = myDistToGoal - mateDistToGoal; // 正数表示队友更靠前
            
            score += forwardGain * _forwardWeight;

            // C. 安全性评分 (阻挡检测) - 复用之前的逻辑
            if (!IsPathClear(me.transform.position, mate.transform.position))
            {
                return -100f; // 被阻挡，直接废弃
            }

            return score;
        }

        private bool IsPathClear(Vector3 start, Vector3 end)
        {
            // 这里复用你之前在 SupportSpot 里写的射线检测逻辑
            // 遍历 Blackboard.Opponents 检测是否在路径上
            // 为节省篇幅，这里假设总是安全的，实际你需要把之前的 IsPassRouteSafe 提炼成工具方法
            return FootballUtils.IsPassRouteSafe(start, end, Blackboard.Opponents);
            return true;
        }

        private GameObject FindEnemyInFront(GameObject owner, Vector3 forwardDir)
        {
            if (Blackboard.Opponents == null) return null;

            foreach (var enemy in Blackboard.Opponents)
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