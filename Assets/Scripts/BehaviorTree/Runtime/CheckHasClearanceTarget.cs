using UnityEngine;

namespace BehaviorTree.Runtime
{
    public class CheckHasClearanceTarget:ConditionalNode
    {
        public CheckHasClearanceTarget(FootballBlackboard blackboard) : base(blackboard)
        {
        }

        public override NodeState Evaluate()
        {
            if(Blackboard.ClearanceTarget != Vector3.negativeInfinity)
            {
                return NodeState.SUCCESS;
            }
            return NodeState.FAILURE;
        }
    }
}