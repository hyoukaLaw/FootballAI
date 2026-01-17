using UnityEngine;
using System.Linq;

namespace BehaviorTree.Runtime
{
    /// <summary>
    /// 条件节点：检查当前球权是否在己方队伍手中
    /// </summary>
    public class CheckIsTeamControllingBall : Node
    {
        public CheckIsTeamControllingBall(FootballBlackboard blackboard) : base(blackboard)
        {
            Name = "CheckIsTeamControllingBall";
        }

        public override NodeState Evaluate()
        {
            // 1. 获取当前持球人
            GameObject ballHolder = Blackboard.MatchContext.BallHolder;

            // 2. 如果没人控球（无主球），则认为本队不处于控球状态
            if (ballHolder == null)
            {
                NodeState = NodeState.FAILURE;
                return NodeState;
            }

            // 3. 如果持球人是 AI 自己，返回 SUCCESS
            if (ballHolder == Blackboard.Owner)
            {
                NodeState = NodeState.SUCCESS;
                return NodeState;
            }

            // 4. 检查持球人是否是队友
            var teammates = Blackboard.MatchContext.GetTeammates(Blackboard.Owner);
            if (teammates != null && teammates.Contains(ballHolder))
            {
                NodeState = NodeState.SUCCESS;
                return NodeState;
            }

            // 5. 否则（对手控球），返回 FAILURE
            NodeState = NodeState.FAILURE;
            return NodeState;
        }
    }
}
