namespace BehaviorTree.Runtime
{
    public class TaskDribbleToPosition : ActionNode
    {
        public TaskDribbleToPosition(FootballBlackboard blackboard) : base(blackboard)
        {
        }

        public override NodeState Evaluate()
        {
            if( Blackboard.MatchContext.BallHolder != Blackboard.Owner)
                return NodeState.FAILURE;
            
            return NodeState.RUNNING;
        }
    }
}