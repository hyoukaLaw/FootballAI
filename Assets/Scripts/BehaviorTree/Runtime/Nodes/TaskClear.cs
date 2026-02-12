using FootballAI.FootballCore;

namespace BehaviorTree.Runtime
{
    public class TaskClear:ActionNode
    {
        public TaskClear(FootballBlackboard blackboard) : base(blackboard)
        {
        }

        public override NodeState Evaluate()
        {
            var ballControl = MatchManager.Instance.BallController;
            ballControl.KickTo(Blackboard.Owner, Blackboard.ClearanceTarget, FootballConstants.ClearKickSpeed);
            return NodeState.SUCCESS;
        }
    }
}
