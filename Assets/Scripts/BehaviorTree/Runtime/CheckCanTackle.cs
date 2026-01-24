using UnityEngine;

namespace BehaviorTree.Runtime
{
    /// <summary>
    /// 条件节点：检查当前球员是否在抢断范围内
    /// </summary>
    public class CheckCanTackle : ConditionalNode
    {
        public CheckCanTackle(FootballBlackboard blackboard) : base(blackboard)
        {
        }

        public override NodeState Evaluate()
        {
            // 1. 如果球没有持球人，无法进行针对性的抢断动作（通常由争抢无主球逻辑处理）
            if (Blackboard.MatchContext.GetBallHolder() == null)
            {
                return NodeState.FAILURE;
            }

            // 3. 计算与持球人的距离
            float distance = Vector3.Distance(Blackboard.Owner.transform.position, Blackboard.MatchContext.GetBallHolder().transform.position);

            // 4. 判断是否在常量定义的抢断范围内
            if (distance < FootballConstants.TryTackleDistance)
            {
                return NodeState.SUCCESS;
            }
            else
            {
                return NodeState.FAILURE;
            }
        }
    }
}
