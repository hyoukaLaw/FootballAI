namespace BehaviorTree
{
    public class SimpleCondition : Node {
        System.Func<FootballBlackboard, bool> _check;
        public SimpleCondition(FootballBlackboard bb, System.Func<FootballBlackboard, bool> c) : base(bb) { _check = c; }
        public override NodeState Evaluate() => _check(Blackboard) ? NodeState.SUCCESS : NodeState.FAILURE;
    }
}