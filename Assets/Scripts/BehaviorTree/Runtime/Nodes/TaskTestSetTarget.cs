using UnityEngine;
using FootballAI.FootballCore;

namespace BehaviorTree.Runtime
{
    public class TaskTestSetTarget: ActionNode
    {
        public TaskTestSetTarget(FootballBlackboard blackboard) : base(blackboard)
        {
        }

        public override NodeState Evaluate()
        {
            Blackboard.MoveTarget = Blackboard.Owner.transform.position + Vector3.forward;
            return NodeState.SUCCESS;
        }
    }
}
