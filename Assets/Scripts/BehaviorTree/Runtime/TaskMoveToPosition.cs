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

        private Vector3 targetPos = Vector3.negativeInfinity;
        public override NodeState Evaluate()
        {
            // 1. 获取主角和目标
            GameObject owner = Blackboard.Owner;
            if(Vector3.Distance(targetPos, Blackboard.MoveTarget) > FootballConstants.DecideMinStep / 2f)
                targetPos = Blackboard.MoveTarget; // 从黑板读取"要去哪"
            // 防御性检查
            if (owner == null)
            {
                return NodeState.FAILURE;
            }
            // 2. 计算距离
            float distance = Vector3.Distance(owner.transform.position, targetPos);
            // 3. 判定是否到达
            if (distance < _stoppingDistance)
            {
                // 到了！任务完成
                return NodeState.SUCCESS;
            }
            // if(TeamPositionUtils.IsPositionOccupiedByTeammates(owner, targetPos, Blackboard.MatchContext.GetTeammates(owner), 
            //        Blackboard.MatchContext.GetOpponents(owner)) || 
            //         TeamPositionUtils.IsPositionOccupiedByEnemy(owner, targetPos, Blackboard.MatchContext.GetOpponents(owner)))
            //     targetPos = TeamPositionUtils.FindUnoccupiedPosition(owner, targetPos, Blackboard.MatchContext.GetTeammates(owner), 
            //         Blackboard.MatchContext.GetOpponents(owner));
            // 4. 如果没到，执行移动逻辑 (简单的匀速移动)
            // 注意：这里直接修改 Transform，不依赖 NavMesh，符合你的白盒测试需求
            Vector3 newPos = Vector3.MoveTowards(owner.transform.position, targetPos,
                Blackboard.Stats.MovementSpeed * Time.deltaTime);

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
            
            newPos = LimitPosToField(newPos);
            
            owner.transform.position = newPos;
            // 2. --- 新增：带球逻辑 (Dribble Logic) ---
            DribbleBall(owner);
            // 5. 还在路上，返回 RUNNING
            Debug.Log($"{Blackboard.Owner.name} From {owner.transform.position} Move to {targetPos}");
            return NodeState.RUNNING;
        }
        
        // 核心辅助方法：如果是持球人，就更新球的位置
        private void DribbleBall(GameObject owner)
        {
            // 检查：我是持球人吗？
            // 注意：这里必须用 Context 里的 BallHolder 判断，因为 Context 是权威
            if (Blackboard.MatchContext != null &&
                Blackboard.MatchContext.BallHolder == owner &&
                Blackboard.MatchContext.Ball != null)
            {
                // 计算球的理想位置：玩家正前方 + 偏移量
                Vector3 ballPos = owner.transform.position + owner.transform.forward * _dribbleOffset;
                ballPos.y = 0f;
                
                // 同时限制球的位置在边界内
                ballPos = LimitPosToField(ballPos);

                Blackboard.MatchContext.Ball.transform.position = ballPos;
                Debug.Log($"{Blackboard.Owner.name}Dribble ball to {ballPos}");
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