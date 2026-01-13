using UnityEngine;

namespace BehaviorTree
{
    public class BehaviorTree
    {
        // 根节点
        private Node _rootNode;
        // 该树关联的黑板
        private FootballBlackboard _blackboard;
        // 执行路径（用于调试）
        public string ExecutionPath = "None";

        public BehaviorTree(FootballBlackboard blackboard)
        {
            _blackboard = blackboard;
        }

        // 设置根节点
        public void SetRoot(Node root)
        {
            _rootNode = root;
        }

        // 每帧由外部控制器调用
        public void Tick()
        {
            _rootNode.Reset();
            if (_rootNode != null)
            {
                _rootNode.Evaluate();
                // 更新执行路径
                var pathList = new System.Collections.Generic.List<string>();
                FindExecutionPath(_rootNode, pathList);
                ExecutionPath = pathList.Count > 0 ? string.Join(" → ", pathList) : "None";
            }
        }

        // 查找执行路径（收集所有返回 SUCCESS 或 RUNNING 的节点）
        private void FindExecutionPath(Node node, System.Collections.Generic.List<string> path)
        {
            // 如果是叶子节点
            if (!(node is CompositeNode))
            {
                if (node.GetNodeState() != NodeState.NOT_EVALUATED)
                {
                    path.Add(node.GetNodeTypeName()+" "+node.GetNodeState().ToString());
                }
                return;
            }

            // 如果是组合节点，根据类型不同处理
            var composite = node as CompositeNode;

            if (composite is SelectorNode)
            {
                if (node.GetNodeState() != NodeState.NOT_EVALUATED)
                {
                    path.Add(node.GetNodeTypeName()+" "+node.GetNodeState().ToString());
                }
                else
                {
                    return;
                }
                // SelectorNode：只找到第一个 SUCCESS/RUNNING 子节点
                foreach (var child in composite.ChildrenNodes)
                {
                    var childPath = new System.Collections.Generic.List<string>();
                    FindExecutionPath(child, childPath);

                    if (childPath.Count > 0)
                    {
                        path.AddRange(childPath);
                    }
                }
            }
            else if (composite is SequenceNode)
            {
                if (node.GetNodeState() != NodeState.NOT_EVALUATED)
                {
                    path.Add(node.GetNodeTypeName()+" "+node.GetNodeState().ToString());
                }
                else
                {
                    return;
                }
                foreach (var child in composite.ChildrenNodes)
                {
                    var childPath = new System.Collections.Generic.List<string>();
                    FindExecutionPath(child, childPath);

                    if (childPath.Count > 0)
                    {
                        path.AddRange(childPath);
                    }

                    // // 如果这个子节点返回 FAILURE，SequenceNode 会停止
                    // if (child.GetNodeState() == NodeState.FAILURE)
                    // {
                    //     break;
                    // }
                }
            }
        }

        public FootballBlackboard GetBlackboard() => _blackboard;
    }
}