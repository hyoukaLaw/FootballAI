using Unity.VisualScripting;
using UnityEngine;

namespace BehaviorTree.Runtime
{
    public class TaskCalculateRoleBasedDefensePosition : ActionNode
    {
        public TaskCalculateRoleBasedDefensePosition(FootballBlackboard blackboard) : base(blackboard)
        {
        }

        public override NodeState Evaluate()
        {
            Vector3 curPos = Blackboard.Owner.transform.position;
            GameObject owner = Blackboard.Owner;
            Vector3 bestPos = ContextAwareZoneCalculator.FindBestPosition(Blackboard.Role, curPos,
                Blackboard.MatchContext.GetMyGoalPosition(owner), Blackboard.MatchContext.GetEnemyGoalPosition(owner),
                Blackboard.MatchContext.Ball.transform.position, Blackboard.MatchContext, owner,
                Blackboard.MatchContext.GetTeammates(owner), Blackboard.MatchContext.GetOpponents(owner), Blackboard);
            Blackboard.MoveTarget = bestPos;
            return NodeState.SUCCESS;
        }
    }
}