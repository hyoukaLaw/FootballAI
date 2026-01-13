using UnityEngine;

namespace BehaviorTree
{
    public class TaskTackle : Node
    {
        public TaskTackle(FootballBlackboard bb) : base(bb) { }

        public override NodeState Evaluate()
        {
            Debug.Log($"Tackle {Time.realtimeSinceStartup} {Time.frameCount} {Blackboard.Owner.name}");
            // 检查抢断保护期：如果在保护期内，不允许任何抢断
            if (Blackboard.MatchContext != null && Blackboard.MatchContext.IsInStealCooldown)
            {
                return NodeState.FAILURE; // 保护期内不允许抢断
            }

            // 检查必要条件
            if (Blackboard.MatchContext == null || Blackboard.MatchContext.BallHolder == null)
                return NodeState.FAILURE; // 没有持球人，无法抢断

            GameObject owner = Blackboard.Owner;
            GameObject ballHolder = Blackboard.MatchContext.BallHolder;

            // 检查是否在抢断范围内
            float tackleDistance =  ballHolder.GetComponent<PlayerAI>().Stats.TackledDistance; // 抢断有效距离
            float distanceToBallHolder = Vector3.Distance(owner.transform.position, ballHolder.transform.position);

            // 尝试抢断
            float tackleChance = CalculateTackleChance(owner, ballHolder);
            float random = Random.Range(0f, 1f);

            if(random <= tackleChance)
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
            return Mathf.Clamp01(tackleChance); // 确保在0-1范围内
        }
        
        // 执行抢断动作
        private void StealBall(GameObject tackler)
        {
            // 检查上下文
            if (Blackboard.MatchContext == null || Blackboard.MatchContext.Ball == null)
                return;

            // 将球移动到抢断球员的位置
            Vector3 tacklerPosition = tackler.transform.position;
            Blackboard.MatchContext.Ball.transform.position = tacklerPosition;

            // 被抢断者（记录在抢断前）
            GameObject ballHolder = Blackboard.MatchContext.BallHolder;

            // 将球权转移给抢断球员（MatchManager 会同步到 Context）
            if (Blackboard.MatchContext != null)
                Blackboard.MatchContext.BallHolder = tackler;

            // 触发抢断保护期，防止立即被反抢
            MatchManager.Instance.TriggerStealCooldown();

            // 让被抢断者停顿一下（符合现实，避免反复互相抢断）
            if (ballHolder != null)
            {
                var ballHolderAI = ballHolder.GetComponent<PlayerAI>();
                if (ballHolderAI != null && ballHolderAI.GetBlackboard() != null)
                {
                    var bb = ballHolderAI.GetBlackboard();
                    bb.IsStunned = true;
                    bb.StunTimer = bb.StunDuration; // 使用配置的停顿时长
                }
            }
        }
    }
}