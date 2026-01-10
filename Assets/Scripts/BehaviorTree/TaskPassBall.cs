using UnityEngine;

namespace BehaviorTree
{
    public class TaskPassBall : Node
    {
        // 假设队友的跑动速度 (需要和 TaskMoveToPosition 里的保持一致)
        private float _teammateSpeed = 5.0f;
        // 球的飞行速度 (需要和 BallController 里的保持一致)
        private float _ballSpeed = 30f;

        public TaskPassBall(FootballBlackboard blackboard) : base(blackboard)
        {
        }

        public override NodeState Evaluate()
        {
            GameObject target = Blackboard.BestPassTarget;
            GameObject ball = Blackboard.Ball;

            if (target == null) return NodeState.FAILURE;

            BallController ballCtrl = ball.GetComponent<BallController>();
            
            // --- 核心修改：计算提前量 (Prediction) ---

            // 1. 计算当前的距离
            float distanceToTarget = Vector3.Distance(ball.transform.position, target.transform.position);

            // 2. 估算球飞行时间 (t = s / v)
            float flightTime = distanceToTarget / _ballSpeed;

            // 3. 获取目标的移动方向
            // 在简单的圆柱体实现中，transform.forward 就是他的移动方向
            // (如果用了 NavMeshAgent，应该用 agent.velocity)
            Vector3 targetVelocity = target.transform.forward * _teammateSpeed;

            // 4. 计算预测点：他现在的位置 + (速度 * 时间)
            // 也就是：球到了之后，他大概会在哪
            Vector3 predictedPos = target.transform.position + (targetVelocity * flightTime);

            // --- 5. 传球给预测点，而不是当前点 ---
            ballCtrl.KickTo(predictedPos, _ballSpeed);

            // 画线调试：看看预测点准不准
            Debug.DrawLine(ball.transform.position, predictedPos, Color.red, 2.0f);
            Debug.Log($"传球预测：目标当前在 {target.transform.position}，预测会跑去 {predictedPos}");

            return NodeState.SUCCESS;
        }
    }
}