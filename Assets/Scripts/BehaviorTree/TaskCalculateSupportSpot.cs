using UnityEngine;

namespace BehaviorTree
{
    public class TaskCalculateSupportSpot : Node
    {
        // 配置参数：理想接应距离
        private float _idealDistance = 8.0f;
        // 配置参数：如果我们被挡住了，每次尝试旋转的角度（搜索步长）
        private float _searchAngleStep = 15.0f;
        // 配置参数：最大搜索次数（防止死循环）
        private int _maxSearchIterations = 12;

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
                    Blackboard.MoveTarget = owner.transform.position + Vector3.left * 3f;
                else
                    Blackboard.MoveTarget = owner.transform.position + Vector3.right * 3f;
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


            // //2. 确定圆心：接应是围绕着"球"或"持球人"进行的
            // // 这里简单起见，直接围绕球
            // Vector3 centerPos = ball.transform.position;
            // Vector3 myPos = owner.transform.position;
            // Vector3 enemyGoalPos = Blackboard.MatchContext.GetEnemyGoalPosition(owner);
            // //3. 计算初始方向：从球指向我
            // // 这意味着 AI 倾向于保持当前的相对角度，而不是乱跑
            // Vector3 attackDirection = (enemyGoalPos - centerPos).normalized;
            // if (attackDirection == Vector3.zero) attackDirection = owner.transform.forward; // 防止重叠
            //
            // //4. 开始寻找最佳点 (扫描算法)
            // Vector3 bestSpot = Vector3.zero;
            // bool foundSpot = false;
            //
            // var opponents = Blackboard.MatchContext.GetOpponents(owner);
            // if (opponents == null) return NodeState.FAILURE;
            //
            // // 我们尝试向左偏和向右偏搜索
            //
            // // i=0(当前方向), i=1(+15度), i=2(-15度), i=3(+30度)...
            // for (int i = 0; i < _maxSearchIterations; i++)
            // {
            //     // 计算当前的测试角度偏移
            //     // 序列生成：0, 15, -15, 30, -30...
            //     // ReSharper disable once PossibleLossOfFraction
            //     float angle = (i % 2 == 0 ? 1 : -1) * ((i+1) / 2) * _searchAngleStep;
            //
            //     // 旋转向量 (绕 Y 轴)
            //     Vector3 testDir = Quaternion.Euler(0, angle, 0) * attackDirection;
            //     Vector3 testPos = centerPos + testDir * _idealDistance;
            //
            //     // 5. 核心判断：这个点安全吗？(传球路线上有敌人吗？)
            //     if (FootballUtils.IsPassRouteSafe(centerPos, testPos, opponents))
            //     {
            //         bestSpot = testPos;
            //         foundSpot = true;
            //         break; // 找到了！停止搜索
            //     }
            // }
            //
            // if (true)
            // {
            //     Blackboard.MoveTarget = Blackboard.Owner.transform.position + (Blackboard.MatchContext.GetEnemyGoalPosition(owner).x < 0?Vector3.left:Vector3.right); 
            // }
            //
            // //6. 决策写入黑板
            // if (foundSpot)
            // {
            //     Vector3 finalSpot = bestSpot;
            //     finalSpot = Blackboard.Owner.transform.position + (finalSpot - Blackboard.Owner.transform.position).normalized * MatchContext.MoveSegment;
            //     Blackboard.MoveTarget = Blackboard.Owner.transform.position + (finalSpot - Blackboard.Owner.transform.position).normalized * MatchContext.MoveSegment;
            //     NodeState = NodeState.SUCCESS;
            //     return NodeState;
            // }
            // else
            // {
            //     // 实在找不到空档（被包围了），就向球门方向移动
            //     Vector3 fallbackSpot = centerPos + 
            //                            (Blackboard.MatchContext.GetEnemyGoalPosition(Blackboard.Owner) - centerPos).normalized
            //                            * _idealDistance; 
            //     Blackboard.MoveTarget = Blackboard.Owner.transform.position + (fallbackSpot - Blackboard.Owner.transform.position).normalized * MatchContext.MoveSegment;
            //     NodeState = NodeState.SUCCESS;
            //     return NodeState;
            // }
        }
    }
}
