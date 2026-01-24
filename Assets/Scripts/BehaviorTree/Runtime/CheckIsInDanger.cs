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
            FieldZone preferZone = ZoneProbabilitySystem.FindHighestWeightZoneAndWeight(Blackboard.Role.DefendPreferences).zone;
            if (IsLastDefensePlayer() && ZoneProbabilitySystem.IsInZone(Blackboard.MatchContext.Ball.transform.position, preferZone, enemyGoal, myGoal) || 
                ZoneProbabilitySystem.IsInPenaltyArea(Blackboard.Owner.transform.position))
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
            float goalPosX = Blackboard.MatchContext.GetMyGoalPosition(Blackboard.Owner).x;
            foreach (var mate in teammates)
            {
                if (mate == Blackboard.Owner) continue;
                if (Mathf.Abs(mate.transform.position.x - goalPosX) < 
                    Mathf.Abs(Blackboard.Owner.transform.position.x - goalPosX))
                {
                    isLastDefense = false;
                    break;
                }
            }
            return isLastDefense ;
        }
    }
}