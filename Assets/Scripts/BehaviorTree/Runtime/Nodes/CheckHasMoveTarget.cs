using UnityEngine;

namespace BehaviorTree.Runtime
{
    public class CheckHasMoveTarget:ConditionalNode
    {
        public CheckHasMoveTarget(FootballBlackboard blackboard) : base(blackboard)
        {
        }

        public override NodeState Evaluate()
        {
            // 直接读取黑板中的布尔状态
            if (Blackboard.MoveTarget != Vector3.zero)
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