using UnityEngine;
using BehaviorTree.Runtime;
using Node = BehaviorTree.Runtime.Node;
using FootballAI.FootballCore;

namespace BehaviorTree.Graph
{
    [CreateNodeMenu("Football AI/TaskStunWait")]
    public class TaskStunWaitGraphNode : BTGraphNode
    {
        // 如果Runtime节点有可以在编辑器配置的参数，可以在这里手动添加 public 字段
        // 然后在 CreateRuntimeNode 中传进去
        
        public override Node CreateRuntimeNode(FootballBlackboard blackboard)
        {
            // 构造运行时对象
            return new TaskStunWait(blackboard);
        }
    }
}
