using UnityEngine;

namespace BehaviorTree
{
    public class CheckIsClosestToLooseBall : Node
    {
        public CheckIsClosestToLooseBall(FootballBlackboard bb) : base(bb) { }

        public override NodeState Evaluate()
        {
            // 防御性编程：检查上下文
            if (Blackboard.MatchContext == null || Blackboard.MatchContext.Ball == null)
                return NodeState.FAILURE;

            // 1. 如果球有人拿，那就不算 Loose Ball，我不去抢
            if (Blackboard.MatchContext.BallHolder != null)
            {
                return NodeState.FAILURE;
            }
            // 2. 遍历所有队友（包括我自己），看谁离球最近
            GameObject ball = Blackboard.MatchContext.Ball;
            if (ball == null) return NodeState.FAILURE;
            float myDist = Vector3.Distance(Blackboard.Owner.transform.position, ball.transform.position);
            // 假设我是最近的，尝试被推翻
            bool amIClosest = true;

            var teammates = Blackboard.MatchContext.GetTeammates(Blackboard.Owner);
            if (teammates == null) return NodeState.FAILURE;

            foreach (var mate in teammates)
            {
                if (mate == Blackboard.Owner) continue;

                float mateDist = Vector3.Distance(mate.transform.position, ball.transform.position);

                // 如果有个队友比我还近 (加个 0.5m 的容错，防止抖动)
                if (mateDist < myDist - FootballConstants.ClosestPlayerTolerance)
                {
                    amIClosest = false;
                    break;
                }
            }

            // 3. 只有我是最近的时候，我才去接球
            if (amIClosest)
            {
                return NodeState.SUCCESS;
            }

            return NodeState.FAILURE;
        }
    }
}