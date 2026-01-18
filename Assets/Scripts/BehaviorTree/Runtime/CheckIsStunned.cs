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
                return NodeState.SUCCESS;
            }
            else
            {
                return NodeState.FAILURE;
            }
        }
    }
}
