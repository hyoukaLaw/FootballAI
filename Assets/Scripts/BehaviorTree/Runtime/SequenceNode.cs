using System.Collections.Generic;
using UnityEngine;

namespace BehaviorTree.Runtime
{
    public class SequenceNode : CompositeNode
    {
        private int _currentIndex = 0; // 记忆执行到第几个子节点

        public SequenceNode(FootballBlackboard blackboard,List<Node> children) : base(blackboard)
        {
            ChildrenNodes = children;
        }

        public override NodeState Execute()
        {
            LastTickFrame = Time.frameCount;
            OnStart();
            for (int i = _currentIndex; i < ChildrenNodes.Count; i++)
            {
                var status = ChildrenNodes[i].Execute();

                if (status == NodeState.RUNNING)
                {
                    _currentIndex = i; // 记住当前执行位置
                    NodeState = NodeState.RUNNING;
                    return NodeState.RUNNING;
                }
                if (status == NodeState.FAILURE)
                {
                    _currentIndex = 0; // 失败，重置
                    NodeState = NodeState.FAILURE;
                    return NodeState.FAILURE;
                }
                // SUCCESS 则继续下一个
            }

            _currentIndex = 0; // 全部成功，重置
            NodeState = NodeState.SUCCESS;
            OnEnd();
            return NodeState.SUCCESS;
        }

        public override NodeState Evaluate()
        {
            return Execute();
        }

        public override void Reset()
        {
            base.Reset();
            _currentIndex = 0;
        }

        public override void OnEnd()
        {
            base.OnEnd();
            _currentIndex = 0; // 序列结束时重置索引
        }
    }
}