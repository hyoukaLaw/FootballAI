using UnityEngine;

namespace BehaviorTree.Runtime
{
    /// <summary>
    /// 条件节点：检查黑板中是否【没有】指定的盯人目标。
    /// 常用于防守逻辑中：如果没有盯人目标，则去追球。
    /// </summary>
    public class CheckHasNoMarkedPlayer : Node
    {
        public CheckHasNoMarkedPlayer(FootballBlackboard blackboard) : base(blackboard)
        {
            Name = "CheckHasNoMarkedPlayer";
        }

        public override NodeState Evaluate()
        {
            // 检查黑板中的 MarkedPlayer 是否为空
            if (Blackboard.MarkedPlayer == null)
            {
                // 没有盯人目标，返回 SUCCESS
                return NodeState.SUCCESS;
            }
            else
            {
                // 已有盯人目标，返回 FAILURE
                return NodeState.FAILURE;
            }
        }
    }
}
