using UnityEngine;

namespace BehaviorTree.Runtime
{
    public class TaskCalculateMovePositionWhenDanger: ActionNode
    {
        public TaskCalculateMovePositionWhenDanger(FootballBlackboard blackboard) : base(blackboard)
        {
        }

        public override NodeState Evaluate()
        {
            var position = GetBestPositionWhenDanger(Blackboard.MatchContext.GetMyGoalPosition(Blackboard.Owner));
            Blackboard.MoveTarget = FootballUtils.GetPositionTowards(Blackboard.Owner.transform.position, position, FootballConstants.DecideMinStep);
            return NodeState.SUCCESS;
        }
        
        private Vector3 GetBestPositionWhenDanger(Vector3 myGoal)
        {
            // 拦截射门
            Vector3 ballHolderPos = Blackboard.MatchContext.Ball.transform.position;
            float distance = (ballHolderPos - myGoal).magnitude;
            Vector3 pos = ballHolderPos + (myGoal - ballHolderPos).normalized * (distance * 0.2f);
            return pos;
        }
    }
}