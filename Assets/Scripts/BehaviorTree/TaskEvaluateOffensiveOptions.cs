using UnityEngine;

namespace BehaviorTree
{
    public class TaskEvaluateOffensiveOptions : Node
    {
        // 评分权重配置
        private float _basePassScore = 50f;
        private float _forwardWeight = 2.0f; // 越靠前的队友分越高
        private float _distanceWeight = 1.0f; // 距离适中的队友分高

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
                return NodeState;
            }

            // --- 2. 评估盘带选项 (如果传球不好，计算向前带球点) ---
            
            // 简单逻辑：向球门方向计算一个 5米远 的点
            Vector3 dribbleDirection = (goalPos - owner.transform.position).normalized;
            Vector3 potentialDribblePos = owner.transform.position + dribbleDirection * 5.0f;

            // 这里应该加一个射线检测：如果前方有人挡，就必须侧向盘带
            // (为了简化，暂定前方无阻挡)
            
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
            if (distToMate > 20f) score -= 30f; // 太远

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
            return true; 
        }
    }
}