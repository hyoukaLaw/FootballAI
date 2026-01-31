using UnityEngine;

namespace BehaviorTree.Runtime
{
    /// <summary>
    /// 射门动作节点
    /// 将球踢向对方球门
    /// </summary>
    public class TaskShoot : ActionNode
    {
        private float _shootPower = FootballConstants.ShootForce; // 射门力度

        public TaskShoot(FootballBlackboard bb) : base(bb) { }

        public override NodeState Evaluate()
        {
            Vector3 goalPos = Blackboard.MatchContext.GetEnemyGoalPosition(Blackboard.Owner);
            BallController ballCtrl = MatchManager.Instance.BallController;
            // 计算射门目标点（稍微偏离球门中心，模拟射门精度）
            Vector3 shootTarget = CalculateShootTarget(goalPos);

            // 踢球
            ballCtrl.KickTo(Blackboard.Owner, shootTarget, _shootPower);
            Debug.Log($"{Blackboard.Owner.name} Shoot {Time.realtimeSinceStartup} {Time.frameCount} {shootTarget}");
            Blackboard.IsStunned = true;
            Blackboard.StunTimer = Blackboard.StunDuration;
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
            float xOffset = (FootballConstants.ShootAccuracyBase - accuracy) * Random.Range(-FootballConstants.ShootXOffsetRange, FootballConstants.ShootXOffsetRange);
            float yOffset = (FootballConstants.ShootAccuracyBase - accuracy) * Random.Range(-FootballConstants.ShootYOffsetRange, FootballConstants.ShootYOffsetRange);

            target.x += xOffset;
            target.y += yOffset;

            return target;
        }
    }
}