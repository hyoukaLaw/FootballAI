using UnityEngine;
using System.Linq;

namespace BehaviorTree.Runtime
{
    /// <summary>
    /// 条件节点：检查当前球权是否在对手手中
    /// </summary>
    public class CheckIsEnemyControllingBall : ConditionalNode
    {
        public CheckIsEnemyControllingBall(FootballBlackboard blackboard) : base(blackboard)
        {
            Name = "CheckIsEnemyControllingBall";
        }

        public override NodeState Evaluate()
        {
            // 1. 获取当前持球人
            GameObject ballHolder = Blackboard.MatchContext.GetBallHolder();
            var opponents = Blackboard.MatchContext.GetOpponents(Blackboard.Owner);
            if (ballHolder == null)
            {
                // 检查是否有对手的接球目标
                if (opponents.Contains(Blackboard.MatchContext.IncomingPassTarget))
                    return NodeState.SUCCESS;
                return NodeState.FAILURE;
            }
            if (opponents.Contains(ballHolder))
            {
                return NodeState.SUCCESS;
            }
            return NodeState.FAILURE;
        }
    }
}
