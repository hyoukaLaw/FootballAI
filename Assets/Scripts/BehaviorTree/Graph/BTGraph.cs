using System.Collections.Generic;
using UnityEngine;
using XNode;
using BehaviorTree.Runtime;
using Node = BehaviorTree.Runtime.Node; // 引用运行时命名空间
namespace BehaviorTree.Graph
{
    [CreateAssetMenu(fileName = "New Football BT", menuName = "AI/Football BT Graph")]
    public class BTGraph : NodeGraph 
    { 
        // 不需要写逻辑，只是一个容器
    }


    public abstract class BTGraphNode : XNode.Node
    {
        // 输入口：父节点连进来 (仅仅为了画线)
        [Input] public bool Entry;
    
        // 输出口：连向子节点
        [Output] public bool Exit;

        // --- 核心工厂方法 ---
        // 所有的 xNode 节点必须实现这个方法：
        // "拿着这张图纸，给我造一个能在游戏里跑的 C# 对象出来"
        public abstract Node CreateRuntimeNode(FootballBlackboard blackboard);

        // --- 核心排序逻辑 ---
        // 获取连接在 "Exit" 端口的子节点，并按 Y 轴从上到下排序
        public List<BTGraphNode> GetSortedGraphChildren()
        {
            List<BTGraphNode> children = new List<BTGraphNode>();
            NodePort port = GetOutputPort("Exit");
        
            if (port == null || !port.IsConnected) return children;

            // 1. 获取所有连接
            List<NodePort> connections = port.GetConnections();

            // 2. 按 Y 轴坐标排序 (核心！)
            // a.node.position.y 小的在上面（注意：xNode坐标系里 Y 越小越靠上还是靠下取决于设置，通常排序即可）
            connections.Sort((a, b) => a.node.position.y.CompareTo(b.node.position.y));

            // 3. 转换回 BTGraphNode
            foreach (var p in connections)
            {
                children.Add(p.node as BTGraphNode);
            }

            return children;
        }
    }
    
    [CreateNodeMenu("Composites/Sequence")]
    public class SequenceGraphNode : BTGraphNode
    {
        public override BehaviorTree.Runtime.Node CreateRuntimeNode(FootballBlackboard blackboard)
        {
            // 1. 先找到图纸上的子节点
            List<BTGraphNode> graphChildren = GetSortedGraphChildren();
        
            // 2. 创建运行时的子节点列表
            List<BehaviorTree.Runtime.Node> runtimeChildren = new List<BehaviorTree.Runtime.Node>();

            // 3. 递归生产 (这就就把整棵树 new 出来了)
            foreach (var childGraphNode in graphChildren)
            {
                runtimeChildren.Add(childGraphNode.CreateRuntimeNode(blackboard));
            }

            // 4. 返回包含所有子节点的运行时 Sequence
            return new BehaviorTree.Runtime.SequenceNode(blackboard, runtimeChildren);
        }
    }
    
    [CreateNodeMenu("Actions/TaskPassBall")]
    public class TaskPassBallGraphNode : BTGraphNode
    {
        // --- 这里是给策划填的参数 ---
        public float BallSpeed = 15.0f;

        public override BehaviorTree.Runtime.Node CreateRuntimeNode(FootballBlackboard blackboard)
        {
            // --- 这里把参数传给运行时 ---
            // 每次运行到这里，都会 new 一个全新的 TaskPassBall
            // 所以运行时节点可以尽情地存状态，互不干扰！
            return new BehaviorTree.Runtime.TaskPassBall(blackboard);
        }
    }
}