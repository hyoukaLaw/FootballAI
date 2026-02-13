using UnityEngine;
using FootballAI.FootballCore;

namespace BehaviorTree.Runtime
{
    /// <summary>
    /// 条件节点：检查黑板上是否标记为可以射门
    /// 通常由之前的评估节点（如 TaskEvaluateOffensiveOptions）计算得出
    /// </summary>
    public class CheckCanShoot : ConditionalNode
    {
        public CheckCanShoot(FootballBlackboard blackboard) : base(blackboard)
        {
        }

        public override NodeState Evaluate()
        {
            // 直接读取黑板中的布尔状态
            if (Blackboard.CanShoot)
            {
                return NodeState.SUCCESS;
            }
            else
            {
                return NodeState.FAILURE;
            }
        }
    }
}
