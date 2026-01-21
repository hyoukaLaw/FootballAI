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
            // 1 如果当前离持球人太近，横向拉开距离
            if (Vector3.Distance(owner.transform.position, ball.transform.position) < FootballConstants.IdealSupportDistance)
            {
                if(owner.transform.position.x < ball.transform.position.x)
                    Blackboard.MoveTarget = owner.transform.position + Vector3.left * FootballConstants.LateralSpreadDistance;
                else
                    Blackboard.MoveTarget = owner.transform.position + Vector3.right * FootballConstants.LateralSpreadDistance;
            }
            // 2 当前在持球人后方，先向前走
            else if (FootballUtils.IsBehind(owner, ball))
            {
                Blackboard.MoveTarget = owner.transform.position + FootballUtils.GetForward(owner);
            }
            else
            {
                // 3 否则，直接往球门方向跑
                Blackboard.MoveTarget = Vector3.MoveTowards(Blackboard.Owner.transform.position,
                    Blackboard.MatchContext.GetEnemyGoalPosition(owner), FootballConstants.DecideMinStep);
            }

            Blackboard.MoveTarget = TeamPositionUtils.FindUnoccupiedPosition(owner, Blackboard.MoveTarget,
                Blackboard.MatchContext.GetTeammates(owner), Blackboard.MatchContext.GetOpponents(owner));
            return NodeState.SUCCESS;
        }
    }
}