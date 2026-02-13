namespace BehaviorTree.Runtime
{
    public class CheckCanPass : ConditionalNode
    {
        public CheckCanPass(FootballBlackboard blackboard) : base(blackboard)
        {
        }

        public override NodeState Evaluate()
        {
            // 直接读取黑板中的布尔状态
            if (Blackboard.BestPassTarget != null)
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