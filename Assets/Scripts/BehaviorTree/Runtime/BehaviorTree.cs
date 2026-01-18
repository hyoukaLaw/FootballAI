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
            // 1. 安全检查：如果该节点这一帧压根没被 Execute 调用，直接不记录
            // 这能过滤掉 Sequence 中 _currentIndex 之前的那些已经成功的“历史节点”
            if (node == null || node.LastTickFrame != Time.frameCount)
            {
                return;
            }

            path.Add($"{node.GetNodeTypeName()} [{node.GetNodeState()}]");

            // 2. 如果是组合节点，需要遍历子节点
            if (node is CompositeNode composite)
            {
                foreach (var child in composite.ChildrenNodes)
                {
                    // 先递归进去记录（如果子节点没跑，第一行的检查会把它挡住）
                    FindExecutionPath(child, path);

                    // 3. 【核心修正】根据子节点的运行结果，决定是否要"打断"后续打印
                    // 只有当子节点这一帧真正运行过，它的状态才具有决定性
                    if (child.LastTickFrame == Time.frameCount)
                    {
                        var childState = child.GetNodeState();

                        //情况 A: 任何节点返回 RUNNING，组合节点都会在这里停下，后续子节点绝不会执行
                        if (childState == NodeState.RUNNING)
                        {
                            break;
                        }

                        //情况 B: Selector 只要遇到 SUCCESS，就短路，后续不执行
                        if (composite is SelectorNode && childState == NodeState.SUCCESS)
                        {
                            break;
                        }

                        //情况 C: Sequence 只要遇到 FAILURE，就短路，后续不执行
                        if (composite is SequenceNode && childState == NodeState.FAILURE)
                        {
                            break;
                        }
                    }
                }
            }
        }
// ... existing code ...

        public FootballBlackboard GetBlackboard() => _blackboard;
    }
}