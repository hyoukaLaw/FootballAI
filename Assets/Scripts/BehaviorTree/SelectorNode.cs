using System.Collections.Generic;
namespace BehaviorTree
{
    public class SelectorNode : CompositeNode
    {
        public SelectorNode(FootballBlackboard blackboard, List<Node> children) : base(blackboard)
        {
            ChildrenNodes = children;
        }

        public override NodeState Evaluate()
        {
            foreach (var node in ChildrenNodes)
            {
                switch (node.Evaluate())
                {
                    case NodeState.RUNNING:
                        NodeState = NodeState.RUNNING;
                        return NodeState;
                    case NodeState.SUCCESS:
                        NodeState = NodeState.SUCCESS;
                        return NodeState;
                    case NodeState.FAILURE:
                        continue; // 这个失败了？试下一个！
                    default:
                        continue;
                }
            }
            NodeState = NodeState.FAILURE;
            return NodeState;
        }
    }
}