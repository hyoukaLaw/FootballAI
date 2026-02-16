using UnityEngine;
using FootballAI.FootballCore;

namespace BehaviorTree.Runtime
{
    public class CheckIsInDanger: ConditionalNode
    {
        private ConditionalNode _conditionalNodeImplementation;

        public CheckIsInDanger(FootballBlackboard blackboard) : base(blackboard)
        {
        }

        public override NodeState Evaluate()
        {
            return IsInDanger() ? NodeState.SUCCESS : NodeState.FAILURE;
        }
        private bool IsInDanger()
        {
            bool isBallInPreferredZone;
            if (ZoneUtils.TryGetPreferredZoneRangeFromFormation(Blackboard.MatchContext, Blackboard.Owner, Blackboard.Role.DefendPreferences,
                    out ZoneUtils.ZoneRange preferredRange))
                isBallInPreferredZone = ZoneUtils.IsPositionInRange(Blackboard.MatchContext.Ball.transform.position, preferredRange);
            else
                isBallInPreferredZone = false;
            Vector3 ballPos = Blackboard.MatchContext.Ball.transform.position;
            var teammates = Blackboard.MatchContext.GetTeammates(Blackboard.Owner);
            if (isBallInPreferredZone && 
                (IsLastDefensePlayer() || FootballUtils.IsClosestTeammateToTarget(ballPos, Blackboard.Owner, teammates)) && 
                !IsBallHolderMark(Blackboard.Owner.transform.position))
            {
                return true;
            }
            return false;
        }
        
        // 是防线上的最后一人
        private bool IsLastDefensePlayer()
        {
            bool isLastDefense = true;
            var teammates = Blackboard.MatchContext.GetTeammates(Blackboard.Owner);
            float goalPosZ = Blackboard.MatchContext.GetMyGoalPosition(Blackboard.Owner).z;
            foreach (var mate in teammates)
            {
                if (mate == Blackboard.Owner) continue;
                if (Mathf.Abs(mate.transform.position.z - goalPosZ) < 
                    Mathf.Abs(Blackboard.Owner.transform.position.z - goalPosZ))
                {
                    isLastDefense = false;
                    break;
                }
            }
            return isLastDefense ;
        }
        
        // 是否已经有球员在持球人与球门之间
        private bool IsBallHolderMark(Vector3 myPos)
        {
            var teammates = Blackboard.MatchContext.GetTeammates(Blackboard.Owner);
            var ballPos = Blackboard.MatchContext.Ball.transform.position;
            foreach (var mate in teammates)
            {
                if (mate == Blackboard.Owner) continue;
                if (FootballUtils.DistancePointToLineSegment(ballPos, myPos, mate.transform.position) <= 1f)
                    return true;
            }

            return false;
        }
    }
}
