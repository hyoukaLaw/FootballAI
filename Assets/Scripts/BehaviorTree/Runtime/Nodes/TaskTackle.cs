using UnityEngine;

namespace BehaviorTree.Runtime
{
    public class TaskTackle : ActionNode
    {
        public TaskTackle(FootballBlackboard bb) : base(bb) { }

        public override NodeState Evaluate()
        {
            GameObject owner = Blackboard.Owner;
            GameObject ballHolder = Blackboard.MatchContext.GetBallHolder();

            float tackleChance = CalculateTackleChance(owner, ballHolder);
            
            if(tackleChance >= 0.5f)
            {
                StealBall(owner);
                return NodeState.SUCCESS;
            }
            else
            {
                BlackboardUtils.StartStun(Blackboard, 0.5f);
                return NodeState.FAILURE;
            }
        }
        
        // 计算抢断成功率
        private float CalculateTackleChance(GameObject tackler, GameObject ballHolder)
        {
            PlayerAI tacklerAI = tackler.GetComponent<PlayerAI>();
            PlayerAI ballHolderAI = ballHolder.GetComponent<PlayerAI>();
            
            if (tacklerAI == null || ballHolderAI == null)
                return FootballConstants.DefaultTackleSuccessRate; // 默认50%成功率
            
            // 基于球员属性计算抢断成功率
            float defensiveFactor = tacklerAI.Stats.DefensiveAwareness;
            float distanceFactor = Mathf.Clamp01(FootballConstants.TackleDistanceFactorBase - Vector3.Distance(tackler.transform.position, ballHolder.transform.position));
            
            // 基础抢断概率 + 防守属性加成 + 距离加成
            float tackleChance = FootballConstants.BaseTackleProbability + 
                                 defensiveFactor * FootballConstants.DefensiveAttributeBonus + 
                                 distanceFactor * FootballConstants.DistanceBonusCoefficient;
            return Mathf.Clamp01(tackleChance); // 确保在0-1范围内
        }
        
        // 执行抢断动作
        private void StealBall(GameObject tackler)
        {
            GameObject currentHolder = Blackboard.MatchContext.GetBallHolder();
            MatchManager.Instance.StealBall(tackler, currentHolder);
        }
    }
}
