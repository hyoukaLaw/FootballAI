using System.Collections.Generic;

namespace BehaviorTree
{
    public enum NodeState
    {
        RUNNING, // 正在做（比如正在跑位，还没到）
        SUCCESS, // 做完了，且成功了（比如找到人了）
        FAILURE  // 做完了，但在失败了（比如被阻挡了）
    }


    [System.Serializable]
    public abstract class Node
    {
        protected NodeState NodeState;
        public NodeState GetNodeState() => NodeState;
        protected FootballBlackboard Blackboard; // 核心：引用数据源
        // 构造函数传入黑板
        public Node(FootballBlackboard blackboard)
        {
            Blackboard = blackboard;
        }

        // 核心方法：每帧调用，返回当前状态
        public abstract NodeState Evaluate();
    }

    public abstract class CompositeNode : Node
    {
        protected List<Node> ChildrenNodes = new();

        protected CompositeNode(FootballBlackboard blackboard) : base(blackboard)
        {
        }
    }
}