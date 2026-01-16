using UnityEngine;

namespace BehaviorTree.Runtime
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

        // 获取根节点（用于重置状态）
        public Node GetRootNode()
        {
            return _rootNode;
        }

        // 每帧由外部控制器调用
        public void Tick()
        {
            if (_rootNode != null)
            {
                _rootNode.Execute();
                // 更新执行路径
                var pathList = new System.Collections.Generic.List<string>();
                FindExecutionPath(_rootNode, pathList);
                ExecutionPath = pathList.Count > 0 ? string.Join(" → ", pathList) : "None";
            }
        }

        // 查找执行路径（追踪当前执行的节点）
        private void FindExecutionPath(Node node, System.Collections.Generic.List<string> path)
        {
            // 如果是叶子节点
            if (!(node is CompositeNode))
            {
                if (node.GetNodeState() != NodeState.NOT_EVALUATED)
                {
                    path.Add(node.GetNodeTypeName() + " " + node.GetNodeState().ToString());
                }
                return;
            }

            // 如果是组合节点，根据类型不同处理
            var composite = node as CompositeNode;

            // 组合节点本身也要显示状态
            if (composite.GetNodeState() != NodeState.NOT_EVALUATED)
            {
                path.Add(composite.GetNodeTypeName() + " " + composite.GetNodeState().ToString());
            }
            else
            {
                return;
            }

            // SelectorNode：只遍历到第一个 SUCCESS/RUNNING 的子节点
            if (composite is SelectorNode)
            {
                foreach (var child in composite.ChildrenNodes)
                {
                    var childPath = new System.Collections.Generic.List<string>();
                    FindExecutionPath(child, childPath);
                    path.AddRange(childPath);
                    if (child.GetNodeState() == NodeState.RUNNING || child.GetNodeState() == NodeState.SUCCESS)
                    {
                        break; // Selector 只选择第一个成功分支
                    }
                }
            }
            // SequenceNode：遍历所有子节点，直到遇到 FAILURE
            else if (composite is SequenceNode)
            {
                foreach (var child in composite.ChildrenNodes)
                {
                    var childPath = new System.Collections.Generic.List<string>();
                    FindExecutionPath(child, childPath);

                    // 如果子节点被评估过（NOT_EVALUATED 除外），就加入路径
                    if (child.GetNodeState() != NodeState.NOT_EVALUATED)
                    {
                        path.AddRange(childPath);

                        // 如果遇到 FAILURE，停止遍历后续子节点
                        if (child.GetNodeState() == NodeState.FAILURE || child.GetNodeState() == NodeState.RUNNING)
                        {
                            break;
                        }
                    }
                }
            }
        }

        public FootballBlackboard GetBlackboard() => _blackboard;
    }
}