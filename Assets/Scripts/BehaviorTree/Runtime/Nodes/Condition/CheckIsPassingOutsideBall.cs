using FootballAI.FootballCore;

namespace BehaviorTree.Runtime
{
    public class CheckIsPassingOutsideBall:ConditionalNode
    {
        public CheckIsPassingOutsideBall(FootballBlackboard bb) : base(bb) { }
        public override NodeState Evaluate()
        {
            if (Blackboard.IsPassingOutsideBall)
            {
                Blackboard.IsPassingOutsideBall = false;
                Blackboard.BestPassTarget = FootballUtils.FindClosestTeammate(Blackboard.Owner, Blackboard.MatchContext.GetTeammates(Blackboard.Owner));
                return NodeState.SUCCESS;
            }
            return NodeState.FAILURE;
        }
    }
}
