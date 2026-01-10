using UnityEngine;

namespace BehaviorTree
{
    public class CheckIsClosestToLooseBall : Node
    {
        public CheckIsClosestToLooseBall(FootballBlackboard bb) : base(bb) { }

        public override NodeState Evaluate()
        {
            // 1. 如果球有人拿，那就不算 Loose Ball，我不去抢
            if (Blackboard.BallHolder != null)
            {
                return NodeState.FAILURE;
            }
            // 2. 遍历所有队友（包括我自己），看谁离球最近
            GameObject ball = Blackboard.Ball;
            if (ball == null) return NodeState.FAILURE;
            float myDist = Vector3.Distance(Blackboard.Owner.transform.position, ball.transform.position);
            // 假设我是最近的，尝试被推翻
            bool amIClosest = true;

            foreach (var mate in Blackboard.Teammates)
            {
                if (mate == Blackboard.Owner) continue;

                float mateDist = Vector3.Distance(mate.transform.position, ball.transform.position);
                
                // 如果有个队友比我还近 (加个 0.5m 的容错，防止抖动)
                if (mateDist < myDist - 0.5f)
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