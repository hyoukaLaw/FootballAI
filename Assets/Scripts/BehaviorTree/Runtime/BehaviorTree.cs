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
// ... existing code ...
        private void FindExecutionPath(Node node, System.Collections.Generic.List<string> path)
        {
            // 2. 记录当前节点状态
            path.Add($"{node.GetNodeTypeName()} [{node.GetNodeState()}]");
            // 3. 如果是叶子节点，递归结束
            if (!(node is CompositeNode composite))
            {
                return;
            }

            // 4. 处理组合节点的特殊短路逻辑
            foreach (var child in composite.ChildrenNodes)
            {

                if (composite is SequenceNode && child.GetNodeState() == NodeState.FAILURE)
                {
                    break;
                }
                // 递归记录子节点路径
                FindExecutionPath(child, path);
                if (composite is SelectorNode && child.GetNodeState() == NodeState.SUCCESS)
                {
                    break;
                }
            }
        }
// ... existing code ...

        public FootballBlackboard GetBlackboard() => _blackboard;
    }
}