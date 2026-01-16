using UnityEngine;

namespace BehaviorTree.Runtime
{
    public class TaskCalculateSupportSpot : Node
    {
        // 配置参数：理想接应距离
        private float _idealDistance = FootballConstants.IdealSupportDistance;
        // 配置参数：如果我们被挡住了，每次尝试旋转的角度（搜索步长）
        private float _searchAngleStep = FootballConstants.SearchAngleStep;
        // 配置参数：最大搜索次数（防止死循环）
        private int _maxSearchIterations = FootballConstants.MaxSearchIterations;

        public TaskCalculateSupportSpot(FootballBlackboard blackboard) : base(blackboard)
        {
        }

        public override NodeState Evaluate()
        {

            GameObject ball = Blackboard.MatchContext.Ball;
            GameObject owner = Blackboard.Owner;
            // 1 如果当前离持球人太近，横向拉开距离
            if (Vector3.Distance(owner.transform.position, ball.transform.position) < _idealDistance)
            {
                if(owner.transform.position.x < ball.transform.position.x)
                    Blackboard.MoveTarget = owner.transform.position + Vector3.left * FootballConstants.LateralSpreadDistance;
                else
                    Blackboard.MoveTarget = owner.transform.position + Vector3.right * FootballConstants.LateralSpreadDistance;
                return NodeState.SUCCESS;
            }
            // 2 当前在持球人后方，先向前走
            if (FootballUtils.IsBehind(owner, ball))
            {
                Blackboard.MoveTarget = owner.transform.position + FootballUtils.GetForward(owner);
                return NodeState.SUCCESS;
            }
            // 3 最后，直接往球门方向跑
            Blackboard.MoveTarget = Blackboard.MatchContext.GetEnemyGoalPosition(owner);
            return NodeState.FAILURE;
        }
    }
}