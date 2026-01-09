using System.Collections.Generic;

namespace BehaviorTree
{
    public class SequenceNode : CompositeNode
    {
        public SequenceNode(FootballBlackboard blackboard,List<Node> children) : base(blackboard)
        {
            ChildrenNodes = children;
        }

        public override NodeState Evaluate()
        {
            bool isAnyChildRunning = false;

            foreach (var node in ChildrenNodes)
            {
                switch (node.Evaluate())
                {
                    case NodeState.FAILURE:
                        NodeState = NodeState.FAILURE;
                        return NodeState; // 有一步卡住了，整个序列失败
                    case NodeState.SUCCESS:
                        continue; // 这一步成了，继续下一步
                    case NodeState.RUNNING:
                        isAnyChildRunning = true;
                        continue; // 继续维持运行状态
                    default:
                        NodeState = NodeState.SUCCESS;
                        return NodeState;
                }
            }
            // 如果有节点还在Running，那我也Running，否则就是全Success
            NodeState = isAnyChildRunning ? NodeState.RUNNING : NodeState.SUCCESS;
            return NodeState;
        }
        
    }
}