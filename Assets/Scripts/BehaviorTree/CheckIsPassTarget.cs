using UnityEngine;

namespace BehaviorTree
{
    /// <summary>
    /// 检查当前球员是否是传球目标
    /// 用于方案A：传球目标锁定机制
    /// 当该节点返回 SUCCESS 时，球员必须去接球（忽略"是否最近"判断）
    /// </summary>
    public class CheckIsPassTarget : Node
    {
        public CheckIsPassTarget(FootballBlackboard bb) : base(bb) { }

        public override NodeState Evaluate()
        {
            // 检查黑板上标记的传球目标状态
            if (Blackboard.IsPassTarget)
            {
                return NodeState.SUCCESS;
            }
            return NodeState.FAILURE;
        }
    }
}
