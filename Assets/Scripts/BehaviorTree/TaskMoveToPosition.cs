using UnityEngine;

namespace BehaviorTree
{
    public class TaskMoveToPosition : Node
    {
        // 移动速度，暂时硬编码，后期可以放进黑板的 stats 里
        private float _speed = 5.0f;
        // 到达判定的误差范围
        private float _stoppingDistance = 0.1f;
        // --- 新增：带球参数 ---
        // 球在脚下的偏移量 (圆柱体前方 0.5米)
        private float _dribbleOffset = 0f;

        public TaskMoveToPosition(FootballBlackboard blackboard) : base(blackboard)
        {
        }

        public override NodeState Evaluate()
        {
            // 1. 获取主角和目标
            GameObject owner = Blackboard.Owner;
            Vector3 targetPos = Blackboard.MoveTarget; // 从黑板读取“要去哪”
            // 防御性检查
            if (owner == null)
            {
                NodeState = NodeState.FAILURE;
                return NodeState;
            }
            // 2. 计算距离
            float distance = Vector3.Distance(owner.transform.position, targetPos);
            // 3. 判定是否到达
            if (distance < _stoppingDistance)
            {
                // 到了！任务完成
                NodeState = NodeState.SUCCESS;
                return NodeState;
            }
            // 4. 如果没到，执行移动逻辑 (简单的匀速移动)
            // 注意：这里直接修改 Transform，不依赖 NavMesh，符合你的白盒测试需求
            Vector3 newPos = Vector3.MoveTowards(
                owner.transform.position, 
                targetPos, 
                _speed * Time.deltaTime
            );
            //
            // // 面朝移动方向（为了让圆柱体看起来自然点）
            if (targetPos != owner.transform.position)
            {
                Vector3 direction = (targetPos - owner.transform.position).normalized;
                // 简单的朝向处理，忽略 Y 轴防止圆柱体歪倒
                direction.y = 0; 
                if (direction != Vector3.zero)
                {
                    owner.transform.forward = direction;
                }
            }
            owner.transform.position = newPos;
            // 2. --- 新增：带球逻辑 (Dribble Logic) ---
            DribbleBall(owner);
            // 5. 还在路上，返回 RUNNING
            NodeState = NodeState.RUNNING;
            return NodeState;
        }
        
        // 核心辅助方法：如果是持球人，就更新球的位置
        private void DribbleBall(GameObject owner)
        {
            // 检查：我是持球人吗？
            // 注意：这里必须用 Blackboard 里的 BallHolder 判断，因为 MatchManager 是权威
            if (Blackboard.BallHolder == owner && Blackboard.Ball != null)
            {
                // 计算球的理想位置：玩家正前方 + 偏移量
                Vector3 ballPos = owner.transform.position + owner.transform.forward * _dribbleOffset;
                ballPos.y = 0f; 

                Blackboard.Ball.transform.position = ballPos;
            }
        }
    }
}