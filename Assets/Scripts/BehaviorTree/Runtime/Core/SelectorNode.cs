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
            if (AbortType == AbortTypeEnum.Self && NodeState == NodeState.RUNNING)
            {
                for (int i = 0; i < _currentIndex; i++) // Gemini:前面哪个条件节点突然行了，要执行那个节点（还未验证）
                {
                    // 偷偷运行排在前面的高优先级节点
                    if (ChildrenNodes[i] is CompositeNode || ChildrenNodes[i] is ConditionalNode)
                    {
                        var status = ChildrenNodes[i].Execute();
                        // 【核心修改点】：只要是 SUCCESS 或者 RUNNING，都要中断！
                        // RUNNING 代表：StunSeq 虽然没把整套动作做完，但它已经在运行了（比如正在晕），所以必须马上切过去。
                        if (status == NodeState.SUCCESS || status == NodeState.RUNNING)
                        {
                            Debug.Log($"{Blackboard.Owner.name} Abort {ChildrenNodes[_currentIndex].GetNodeTypeName()}");
                            // 1. 杀掉旧的（当前正在运行的低优先级节点）
                            ChildrenNodes[_currentIndex].OnEnd();
                            // 2. 处理新的状态
                            if (status == NodeState.SUCCESS)
                            {
                                // 如果高优先级直接成功了（Selector只要有一个成功就全成功）
                                _currentIndex = 0; // 重置
                                NodeState = NodeState.SUCCESS;
                                return NodeState.SUCCESS; // 【重要】直接返回，别往下跑了
                            }
                            else // status == NodeState.RUNNING
                            {
                                // 如果高优先级节点正在运行（比如 StunSeq 正在 wait）
                                _currentIndex = i; // 更新索引为当前高优先级节点
                                NodeState = NodeState.RUNNING;
                                return NodeState.RUNNING; // 【重要】直接返回
                            }
                        }
                    }
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