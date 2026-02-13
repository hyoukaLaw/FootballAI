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
            if(float.IsNegativeInfinity(Blackboard.ClearanceTarget.x) || 
               float.IsNegativeInfinity(Blackboard.ClearanceTarget.y) || 
               float.IsNegativeInfinity(Blackboard.ClearanceTarget.z))
            {
                return NodeState.FAILURE;
            }
            return NodeState.SUCCESS;
        }
    }
}