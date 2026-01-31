using UnityEngine;

namespace BehaviorTree.Runtime
{
    public class TaskKickOutside:ActionNode
    {
        public TaskKickOutside(FootballBlackboard blackboard) : base(blackboard)
        {
        }

        public override NodeState Evaluate()
        {
            Blackboard.MatchContext.BallController.KickTo(Blackboard.Owner, new Vector3(-100,0,100), 10f);
            return NodeState.SUCCESS;
        }
    }
}