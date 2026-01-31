using UnityEngine;

namespace BehaviorTree.Runtime
{
    public class TaskCalculateSupportSpot : ActionNode
    {
        public TaskCalculateSupportSpot(FootballBlackboard blackboard) : base(blackboard)
        {
        }

        public override NodeState Evaluate()
        {
            GameObject ball = Blackboard.MatchContext.Ball;
            GameObject owner = Blackboard.Owner;
            Vector3 potentialTarget;
            if (Vector3.Distance(owner.transform.position, ball.transform.position) < FootballConstants.IdealSupportDistance)// 1 如果当前离持球人太近，横向拉开距离
            {
                potentialTarget = CalculateLateralSpreadPosition(owner, ball);
            }
            else if (FootballUtils.IsBehind(owner, ball))// 2 当前在持球人后方，先向前走
            {
                potentialTarget = CalculateForwardPosition(owner);
            }
            else // 3 否则，直接往球门方向跑
            {
                potentialTarget = Blackboard.MatchContext.GetEnemyGoalPosition(owner);
            }
            potentialTarget = Vector3.MoveTowards(Blackboard.Owner.transform.position,
                potentialTarget, FootballConstants.DecideMinStep);
            // 最终边界检查和位置优化
            Vector3 finalTarget = Blackboard.MatchContext.IsInField(potentialTarget) ? 
                potentialTarget : owner.transform.position;

            Blackboard.MoveTarget = TeamPositionUtils.FindUnoccupiedPosition(owner, finalTarget,
                Blackboard.MatchContext.GetTeammates(owner), Blackboard.MatchContext.GetOpponents(owner));
            return NodeState.SUCCESS;
        }

        /// <summary>
        /// 计算横向拉开接应位置
        /// </summary>
        private Vector3 CalculateLateralSpreadPosition(GameObject owner, GameObject ball)
        {
            // 根据进攻方向计算横向（垂直于前进方向的左右）
            Vector3 forwardDir = FootballUtils.GetForward(owner);
            Vector3 lateralDir = Vector3.Cross(Vector3.up, forwardDir); // 计算横向方向

            // 判断向左还是向右：选择离球更远的一侧
            Vector3 leftPos = owner.transform.position + lateralDir * FootballConstants.LateralSpreadDistance;
            Vector3 rightPos = owner.transform.position - lateralDir * FootballConstants.LateralSpreadDistance;

            float leftDistToBall = Vector3.Distance(leftPos, ball.transform.position);
            float rightDistToBall = Vector3.Distance(rightPos, ball.transform.position);

            Vector3 potentialTarget = leftDistToBall > rightDistToBall ? leftPos : rightPos;
            return potentialTarget;
        }

        /// <summary>
        /// 计算向前接应位置
        /// </summary>
        private Vector3 CalculateForwardPosition(GameObject owner)
        {
            Vector3 potentialTarget = owner.transform.position + FootballUtils.GetForward(owner);
            return potentialTarget;
        }
    }
}