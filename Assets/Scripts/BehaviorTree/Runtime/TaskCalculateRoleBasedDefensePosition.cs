using Unity.VisualScripting;
using UnityEngine;

namespace BehaviorTree.Runtime
{
    public class TaskCalculateRoleBasedDefensePosition : ActionNode
    {
        public TaskCalculateRoleBasedDefensePosition(FootballBlackboard blackboard) : base(blackboard)
        {
        }

        // 0 危险情况：敌人已经进入禁区；敌人周围无人跟防
        // 1 评分情况：
        // 取点：默认区域是矩形，修改原先的取点为矩形内等间隔取点。
        // 后卫：球的位置 球与本方球门的连线 球与对方其他球员的连线
        public override NodeState Evaluate()
        {
            Vector3 curPos = Blackboard.Owner.transform.position;
            GameObject owner = Blackboard.Owner;
            Vector3 myGoalPos = Blackboard.MatchContext.GetMyGoalPosition(owner);
            Vector3 bestPos = ContextAwareZoneCalculator.FindBestPosition(Blackboard.Role, curPos,
                Blackboard.MatchContext.GetMyGoalPosition(owner), Blackboard.MatchContext.GetEnemyGoalPosition(owner),
                Blackboard.MatchContext.Ball.transform.position, Blackboard.MatchContext, owner,
                Blackboard.MatchContext.GetTeammates(owner), Blackboard.MatchContext.GetOpponents(owner), Blackboard);
            Blackboard.MoveTarget = FootballUtils.GetPositionTowards(curPos, bestPos, FootballConstants.DecideMinStep);
            return NodeState.SUCCESS;
        }


    }
}