using System.Collections.Generic;
using UnityEngine;

namespace BehaviorTree.Runtime
{
    public class SelectorNode : CompositeNode
    {
        private int _currentIndex = 0; // 记忆执行到第几个子节点

        public SelectorNode(FootballBlackboard blackboard, List<Node> children, AbortTypeEnum abortType) : base(blackboard)
        {
            ChildrenNodes = children;
            AbortType = abortType;
        }

        public override NodeState Execute()
        {
            LastTickFrame = Time.frameCount;
            OnStart();
            for (int i = 0; i < _currentIndex; i++) // Gemini:前面哪个条件节点突然行了，要执行那个节点（还未验证）
            {
                // 偷偷运行排在前面的高优先级节点
                var status = ChildrenNodes[i].Execute();

                // 关键区别：Sequence 是怕前面由 Success 变 Failure
                // Selector 是看前面有没有由 Failure 变 Success
                if (status == NodeState.SUCCESS)
                {
                    // 【中止触发】！
                    // 1. 既然前面的节点成功了，现在正在运行的这个低级任务（比如远程射击）就没用了，杀掉它。
                    ChildrenNodes[_currentIndex].OnEnd();
                    // 2. 指针指回那个成功的节点（以便下一帧直接从它开始，或者这一帧直接算它赢）
                    _currentIndex = i+1;
                    break;
                }
            }
            
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
            _currentIndex = 0; // 选择器结束时重置索引
        }
    }
}