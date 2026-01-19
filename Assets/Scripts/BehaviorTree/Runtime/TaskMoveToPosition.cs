using UnityEngine;

namespace BehaviorTree.Runtime
{
    public class TaskMoveToPosition : Node
    {
        // 到达判定的误差范围
        private float _stoppingDistance = 0.02f;
        // --- 新增：带球参数 ---
        // 球在脚下的偏移量 (圆柱体前方 0.5米)
        private float _dribbleOffset = 0f;

        public TaskMoveToPosition(FootballBlackboard blackboard) : base(blackboard)
        {
        }

        private Vector3 _targetPos = Vector3.negativeInfinity;
        public override NodeState Evaluate()
        {
            // 1. 获取主角和目标
            GameObject owner = Blackboard.Owner;
            if(Vector3.Distance(_targetPos, Blackboard.MoveTarget) > FootballConstants.DecideMinStep / 2f)
                _targetPos = Blackboard.MoveTarget; // 从黑板读取"要去哪"
            // 防御性检查
            if (owner == null)
            {
                return NodeState.FAILURE;
            }
            // 2. 计算距离
            float distance = Vector3.Distance(owner.transform.position, _targetPos);
            // 3. 判定是否到达
            if (distance < _stoppingDistance)
            {
                // 到了！任务完成
                return NodeState.SUCCESS;
            }
            Vector3 newPos = Vector3.MoveTowards(owner.transform.position, _targetPos,
                Blackboard.Stats.MovementSpeed * Time.deltaTime);

            // // 面朝移动方向（为了让圆柱体看起来自然点）
            if (_targetPos != owner.transform.position)
            {
                Vector3 direction = (_targetPos - owner.transform.position).normalized;
                // 简单的朝向处理，忽略 Y 轴防止圆柱体歪倒
                direction.y = 0; 
                if (direction != Vector3.zero)
                {
                    owner.transform.forward = direction;
                }
            }
            NodeState retNodeState = NodeState.RUNNING;
            Vector3 clampedNewPos = LimitPosToField(newPos);
            if (Vector3.Distance(clampedNewPos, newPos) > _stoppingDistance)
                retNodeState = NodeState.SUCCESS;
            owner.transform.position = clampedNewPos;
            // 2. --- 新增：带球逻辑 (Dribble Logic) ---
            DribbleBall(owner);
            // 5. 还在路上，返回 RUNNING
            Debug.Log($"{Blackboard.Owner.name} From {owner.transform.position} Move to {_targetPos}");
            return retNodeState;
        }
        
        // 核心辅助方法：如果是持球人，就更新球的位置
        private void DribbleBall(GameObject owner)
        {
            // 检查：我是持球人吗？
            // 注意：这里必须用 Context 里的 BallHolder 判断，因为 Context 是权威
            if (Blackboard.MatchContext.BallHolder == owner)
            {
                // 计算球的理想位置：玩家正前方 + 偏移量
                Vector3 ballPos = owner.transform.position + owner.transform.forward * _dribbleOffset;
                ballPos.y = 0f;
                // 同时限制球的位置在边界内
                ballPos = LimitPosToField(ballPos);
                Blackboard.MatchContext.Ball.transform.position = ballPos;
            }
        }

        private Vector3 LimitPosToField(Vector3 pos)
        {
            float leftBorder = Blackboard.MatchContext.GetLeftBorder();
            float rightBorder = Blackboard.MatchContext.GetRightBorder();
            float forwardBorder = Blackboard.MatchContext.GetForwardBorder();
            float backwardBorder = Blackboard.MatchContext.GetBackwardBorder();
                
            pos.x = Mathf.Clamp(pos.x, leftBorder, rightBorder);
            pos.z = Mathf.Clamp(pos.z, backwardBorder, forwardBorder);
            return pos;
        }
    }
}