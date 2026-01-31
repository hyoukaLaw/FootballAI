using UnityEngine;

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
            Vector3 myGoal = Blackboard.MatchContext.GetMyGoalPosition(Blackboard.Owner);
            Vector3 enemyGoal = Blackboard.MatchContext.GetEnemyGoalPosition(Blackboard.Owner);
            FieldZone preferZone = ZoneUtils.FindHighestWeightZoneAndWeight(Blackboard.Role.DefendPreferences).zone;
            Vector3 ballPos = Blackboard.MatchContext.Ball.transform.position;
            var teammates = Blackboard.MatchContext.GetTeammates(Blackboard.Owner);
            if (ZoneUtils.IsInZone(ballPos, preferZone, enemyGoal, myGoal) && 
                (IsLastDefensePlayer() || FootballUtils.IsClosestTeammateToTarget(ballPos, Blackboard.Owner, teammates)) && 
                !IsBallHolderMark(Blackboard.Owner.transform.position))
            {
                Debug.Log($"{Blackboard.Owner.name} is in danger");
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