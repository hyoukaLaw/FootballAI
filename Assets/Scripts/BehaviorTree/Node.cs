using System.Collections.Generic;

namespace BehaviorTree
{
    public enum NodeState
    {
        NOT_EVALUATED, // 还没运行过
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

        public string Name;
        private bool _isRunning = false; // 标记节点是否正在运行

        // 构造函数传入黑板
        public Node(FootballBlackboard blackboard)
        {
            Blackboard = blackboard;
        }

        // 核心方法：每帧调用，返回当前状态
        public abstract NodeState Evaluate();

        // 执行节点（包装生命周期）
        public virtual NodeState Execute()
        {
            if (!_isRunning)
            {
                OnStart();
                _isRunning = true;
            }

            var status = Evaluate();

            if (status == NodeState.SUCCESS || status == NodeState.FAILURE)
            {
                OnEnd();
                _isRunning = false;
            }

            return status;
        }

        // 任务首次执行时调用（用于初始化）
        public virtual void OnStart() { }

        // 任务返回 Success/Failure 后调用（用于清理）
        public virtual void OnEnd() { }

        // 获取节点类型名称（用于调试）
        public virtual string GetNodeTypeName()
        {
            return $"{GetType().Name}({Name})";
        }

        public virtual void Reset()
        {
            NodeState = NodeState.NOT_EVALUATED;
            _isRunning = false;
        }
    }

    public abstract class CompositeNode : Node
    {
        public List<Node> ChildrenNodes = new();

        protected CompositeNode(FootballBlackboard blackboard) : base(blackboard)
        {
        }

        public override void Reset()
        {
            base.Reset();
            foreach (var child in ChildrenNodes)
            {
                child.Reset();
            }
        }

        public override void OnEnd()
        {
            base.OnEnd();
            // 组合节点结束时，重置所有子节点的运行状态
            foreach (var child in ChildrenNodes)
            {
                child.Reset();
            }
        }
    }
}