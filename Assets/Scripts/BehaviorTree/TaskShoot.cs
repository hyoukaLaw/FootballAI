using UnityEngine;

namespace BehaviorTree
{
    /// <summary>
    /// 射门动作节点
    /// 将球踢向对方球门
    /// </summary>
    public class TaskShoot : Node
    {
        private float _shootPower = 20f; // 射门力度
        private float _shootAngle = 15f; // 射门角度（地面球或半高球）

        public TaskShoot(FootballBlackboard bb) : base(bb) { }

        public override NodeState Evaluate()
        {
            if (Blackboard.MatchContext == null || Blackboard.MatchContext.Ball == null)
                return NodeState.FAILURE;

            GameObject ball = Blackboard.MatchContext.Ball;
            Vector3 goalPos = Blackboard.MatchContext.GetEnemyGoalPosition(Blackboard.Owner);

            if (ball == null) return NodeState.FAILURE;

            BallController ballCtrl = ball.GetComponent<BallController>();

            // 计算射门目标点（稍微偏离球门中心，模拟射门精度）
            Vector3 shootTarget = CalculateShootTarget(goalPos);

            // 踢球
            ballCtrl.KickTo(shootTarget, _shootPower);

            return NodeState.SUCCESS;
        }

        /// <summary>
        /// 计算射门目标点，考虑射门精度
        /// </summary>
        private Vector3 CalculateShootTarget(Vector3 goalPos)
        {
            return goalPos; // 暂时不考虑射到别的地方
            // 基础目标：球门中心
            Vector3 target = goalPos;

            // 根据射门准确率添加随机偏移
            float accuracy = Blackboard.Stats.ShootingAccuracy;

            // 偏移范围（球门假设为3x2米的范围）
            float xOffset = (1.0f - accuracy) * Random.Range(-1.5f, 1.5f);
            float yOffset = (1.0f - accuracy) * Random.Range(-1.0f, 1.0f);

            target.x += xOffset;
            target.y += yOffset;

            return target;
        }
    }
}
