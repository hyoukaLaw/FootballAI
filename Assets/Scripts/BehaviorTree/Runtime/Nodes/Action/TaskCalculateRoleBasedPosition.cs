using Unity.VisualScripting;
using UnityEngine;
using FootballAI.FootballCore;

namespace BehaviorTree.Runtime
{
    public class TaskCalculateRoleBasedPosition : ActionNode
    {
        public TaskCalculateRoleBasedPosition(FootballBlackboard blackboard) : base(blackboard)
        {
        }
        
        public override NodeState Evaluate()
        {
            Vector3 curPos = Blackboard.Owner.transform.position;
            GameObject owner = Blackboard.Owner;
            Vector3 bestPos = RoleBasedPositionScoreCalculator.FindBestPosition(Blackboard.Role, curPos,
                Blackboard.MatchContext.GetMyGoalPosition(owner), Blackboard.MatchContext.GetEnemyGoalPosition(owner),
                Blackboard.MatchContext.Ball.transform.position, Blackboard.MatchContext, owner,
                Blackboard.MatchContext.GetTeammates(owner), Blackboard.MatchContext.GetOpponents(owner), Blackboard);
            Blackboard.MoveTarget = FootballUtils.GetPositionTowards(curPos, bestPos, FootballConstants.DecideMinStep);
            return NodeState.SUCCESS;
        }


    }
}
