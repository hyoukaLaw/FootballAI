using UnityEngine;

namespace BehaviorTree
{
    public class TaskTackle : Node
    {
        public TaskTackle(FootballBlackboard bb) : base(bb) { }

        public override NodeState Evaluate()
        {
            // 检查抢断保护期：如果在保护期内，不允许任何抢断
            if (MatchManager.Instance != null && MatchManager.Instance.IsInStealCooldown)
            {
                return NodeState.FAILURE; // 保护期内不允许抢断
            }

            // 检查必要条件
            if (Blackboard.BallHolder == null)
                return NodeState.FAILURE; // 没有持球人，无法抢断
            
            GameObject owner = Blackboard.Owner;
            GameObject ballHolder = Blackboard.BallHolder;
            
            // 检查是否在抢断范围内
            float tackleDistance = 1.6f; // 抢断有效距离
            float distanceToBallHolder = Vector3.Distance(owner.transform.position, ballHolder.transform.position);
            
            if (distanceToBallHolder > tackleDistance)
            {
                // 不在抢断范围内，继续移动接近
                Blackboard.MoveTarget = ballHolder.transform.position;
                Debug.Log($"抢球接近");
                return NodeState.RUNNING;
            }
            
            // 尝试抢断
            float tackleChance = CalculateTackleChance(owner, ballHolder);
            float random = Random.Range(0f, 1f);
            
            if (random <= tackleChance)
            {
                // 抢断成功！
                StealBall(owner);
                return NodeState.SUCCESS;
            }
            else
            {
                // 抢断失败
                return NodeState.FAILURE;
            }
        }
        
        // 计算抢断成功率
        private float CalculateTackleChance(GameObject tackler, GameObject ballHolder)
        {
            PlayerAI tacklerAI = tackler.GetComponent<PlayerAI>();
            PlayerAI ballHolderAI = ballHolder.GetComponent<PlayerAI>();
            
            if (tacklerAI == null || ballHolderAI == null)
                return 0.5f; // 默认50%成功率
            
            // 基于球员属性计算抢断成功率
            float defensiveFactor = tacklerAI.Stats.DefensiveAwareness;
            float distanceFactor = Mathf.Clamp01(2f - Vector3.Distance(tackler.transform.position, ballHolder.transform.position));
            
            // 基础抢断概率 + 防守属性加成 + 距离加成
            float tackleChance = 0.3f + defensiveFactor * 0.2f + distanceFactor * 0.3f;
            Debug.Log($"抢断机率 {tackleChance}");
            return Mathf.Clamp01(tackleChance); // 确保在0-1范围内
        }
        
        // 执行抢断动作
        private void StealBall(GameObject tackler)
        {
            // 将球移动到抢断球员的位置
            Vector3 tacklerPosition = tackler.transform.position;
            MatchManager.Instance.Ball.transform.position = tacklerPosition;

            // 将球权转移给抢断球员
            MatchManager.Instance.CurrentBallHolder = tackler;

            // 立即同步所有黑板数据，确保被抢断者下一帧能正确识别丢球状态
            MatchManager.Instance.SyncAllBlackboards();

            // 触发抢断保护期，防止立即被反抢
            MatchManager.Instance.TriggerStealCooldown();

            Debug.Log($"{tackler.name} 抢断成功！获得球权！");
        }
    }
}