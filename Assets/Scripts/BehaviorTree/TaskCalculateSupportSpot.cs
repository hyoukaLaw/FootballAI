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
            GameObject ball = Blackboard.Ball;
            GameObject owner = Blackboard.Owner;
            if (ball == null || owner == null)
            {
                NodeState = NodeState.FAILURE;
                return NodeState;
            }
            // 2. 确定圆心：接应是围绕着“球”或“持球人”进行的
            // 这里简单起见，直接围绕球
            Vector3 centerPos = ball.transform.position;
            Vector3 myPos = owner.transform.position;

            // 3. 计算初始方向：从球指向我
            // 这意味着 AI 倾向于保持当前的相对角度，而不是乱跑
            Vector3 directionToMe = (myPos - centerPos).normalized;
            if (directionToMe == Vector3.zero) directionToMe = owner.transform.forward; // 防止重叠

            // 4. 开始寻找最佳点 (扫描算法)
            Vector3 bestSpot = Vector3.zero;
            bool foundSpot = false;

            // 我们尝试向左偏和向右偏搜索
            // i=0(当前方向), i=1(+15度), i=2(-15度), i=3(+30度)...
            for (int i = 0; i < _maxSearchIterations; i++)
            {
                // 计算当前的测试角度偏移
                // 序列生成：0, 15, -15, 30, -30...
                // ReSharper disable once PossibleLossOfFraction
                float angle = (i % 2 == 0 ? 1 : -1) * ((i+1) / 2) * _searchAngleStep;
                
                // 旋转向量 (绕 Y 轴)
                Vector3 testDir = Quaternion.Euler(0, angle, 0) * directionToMe;
                Vector3 testPos = centerPos + testDir * _idealDistance;

                // 5. 核心判断：这个点安全吗？(传球路线上有敌人吗？)
                if (FootballUtils.IsPassRouteSafe(centerPos, testPos, Blackboard.Opponents))
                {
                    bestSpot = testPos;
                    foundSpot = true;
                    break; // 找到了！停止搜索
                }
            }

            // 6. 决策写入黑板
            if (foundSpot)
            {
                Blackboard.MoveTarget = bestSpot;
                
                // 可选：在这里可以画线调试，方便你看 AI 想去哪
                Debug.DrawLine(centerPos, bestSpot, Color.green); 
                
                NodeState = NodeState.SUCCESS;
                return NodeState;
            }
            else
            {
                // 实在找不到空档（被包围了），就原地不动或者保持原定距离
                Blackboard.MoveTarget = centerPos + directionToMe * _idealDistance;
                NodeState = NodeState.SUCCESS; // 或者 SUCCESS，取决于你希望树怎么处理
                return NodeState.SUCCESS; // 这种情况下通常返回 SUCCESS 让他至少动起来
            }
        }
        
    }
}