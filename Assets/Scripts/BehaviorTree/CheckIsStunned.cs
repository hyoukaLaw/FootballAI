using UnityEngine;

namespace BehaviorTree.Runtime
{
    public class CheckIsStunned : Node
    {
        public CheckIsStunned(FootballBlackboard bb) : base(bb) { }

        public override NodeState Evaluate()
        {
            if (Blackboard.IsStunned)
            {
                NodeState = NodeState.SUCCESS;
                return NodeState;
            }
            else
            {
                NodeState = NodeState.FAILURE;
                return NodeState;
            }
        }
    }
}
