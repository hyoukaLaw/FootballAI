namespace BehaviorTree.Runtime
{
    public class CheckIsTestOutsideKick: ConditionalNode
    {
        public CheckIsTestOutsideKick(FootballBlackboard blackboard) : base(blackboard)
        {
        }
        
        public override NodeState Evaluate()
        {
            if (Blackboard.IsTestKickOutside)
            {
                Blackboard.IsTestKickOutside = false;
                return NodeState.SUCCESS;
            }
                
            return NodeState.FAILURE;
        }
    }
}