using System.Collections.Generic;
using UnityEngine;

namespace BehaviorTree.Runtime
{
    public class SelectorNode : CompositeNode
    {
        private int _currentIndex = 0; // 记忆执行到第几个子节点

        public SelectorNode(FootballBlackboard blackboard, List<Node> children) : base(blackboard)
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
                if (status == NodeState.SUCCESS)
                {
                    _currentIndex = 0; // 成功，重置
                    NodeState = NodeState.SUCCESS;
                    return NodeState.SUCCESS;
                }
                // FAILURE 则继续下一个
            }

            _currentIndex = 0; // 全部失败，重置
            NodeState = NodeState.FAILURE;
            OnEnd();
            return NodeState.FAILURE;
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
            _currentIndex = 0; // 选择器结束时重置索引
        }
    }
}