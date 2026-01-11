using UnityEngine;

namespace BehaviorTree
{
    public class TaskPassBall : Node
    {
        public TaskPassBall(FootballBlackboard blackboard) : base(blackboard)
        {
        }

        public override NodeState Evaluate()
        {
            GameObject target = Blackboard.BestPassTarget;
            GameObject ball = Blackboard.Ball;

            if (target == null) return NodeState.FAILURE;

            BallController ballCtrl = ball.GetComponent<BallController>();

            // 设置传球目标锁定
            MatchManager.Instance.IncomingPassTarget = target;

            // --- 核心修改：计算提前量 (Prediction) ---

            // 1. 计算当前的距离
            float distanceToTarget = Vector3.Distance(ball.transform.position, target.transform.position);

            // 2. 估算球飞行时间 (t = s / v)
            float flightTime = distanceToTarget / Blackboard.Stats.PassingSpeed;

            // 3. 获取目标的移动方向和速度
            // 需要从目标的 PlayerAI 获取其速度属性
            PlayerAI targetAI = target.GetComponent<PlayerAI>();
            float targetSpeed = targetAI != null ? targetAI.Stats.MovementSpeed : 2.0f;
            Vector3 targetVelocity = target.transform.forward * targetSpeed;

            // 4. 计算预测点：他现在的位置 + (速度 * 时间)
            // 也就是：球到了之后，他大概会在哪
            Vector3 predictedPos = target.transform.position + (targetVelocity * flightTime);

            // --- 5. 传球给预测点，而不是当前点 ---
            ballCtrl.KickTo(predictedPos, Blackboard.Stats.PassingSpeed);

            Debug.DrawLine(ball.transform.position, predictedPos, Color.red, 2.0f);

            return NodeState.SUCCESS;
        }
    }
}