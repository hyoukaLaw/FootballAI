using UnityEngine;

namespace BehaviorTree
{
    public class TaskMoveToPosition : Node
    {
        // 移动速度，暂时硬编码，后期可以放进黑板的 stats 里
        private float _speed = 5.0f;
        // 到达判定的误差范围
        private float _stoppingDistance = 0.1f;

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
            // if (targetPos != owner.transform.position)
            // {
            //     Vector3 direction = (targetPos - owner.transform.position).normalized;
            //     // 简单的朝向处理，忽略 Y 轴防止圆柱体歪倒
            //     direction.y = 0; 
            //     if (direction != Vector3.zero)
            //     {
            //         owner.transform.forward = direction;
            //     }
            // }
            owner.transform.position = newPos;
            // 5. 还在路上，返回 RUNNING
            NodeState = NodeState.RUNNING;
            return NodeState;
        }
    }
}