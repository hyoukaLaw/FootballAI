using System.Collections.Generic;
using UnityEngine;
using BehaviorTree.Runtime;
using Node = BehaviorTree.Runtime.Node;

namespace BehaviorTree.Graph
{
    [CreateNodeMenu("Football AI/Composite/SelectorNode")]
    public class SelectorNodeGraphNode : BTGraphNode
    {
        public override Node CreateRuntimeNode(FootballBlackboard blackboard)
        {
            // 1. 获取图表中连接的子节点（已按Y轴排序）
            var graphChildren = GetSortedGraphChildren();
            
            // 2. 将它们转换为运行时节点
            var runtimeChildren = new List<Node>();
            foreach (var childGraphNode in graphChildren)
            {
                // 递归创建子节点
                if (childGraphNode != null)
                {
                    runtimeChildren.Add(childGraphNode.CreateRuntimeNode(blackboard));
                }
            }
            SelectorNode selectorNode = new SelectorNode(blackboard, runtimeChildren);
            selectorNode.Name = name;

            // 3. 构造运行时组合节点，传入子节点列表
            return selectorNode;
        }
    }
}