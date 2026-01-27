using UnityEngine;

namespace BehaviorTree.Runtime
{
    /// <summary>
    /// 前锋进攻评估策略
    /// 优先级：射门 > 带球 > 传球
    /// </summary>
    public class ForwardOffensiveStrategy : BaseOffensiveStrategy
    {
        protected override float GetBasePassScore()
        {
            return FootballConstants.BasePassScoreForward;
        }

        protected override bool ShouldConsiderShoot()
        {
            return true;
        }

        protected override bool ShouldConsiderPass()
        {
            return true;
        }

        protected override bool ShouldConsiderDribble()
        {
            return true;
        }

        protected override bool ShouldConsiderClearance()
        {
            return false;
        }

        public override OffensiveAction Evaluate(FootballBlackboard blackboard)
        {
            OffensiveAction action = OffensiveAction.None;

            var shootEval = EvaluateShoot(blackboard);
            if (shootEval.Score > action.Score)
            {
                action = shootEval.ToAction();
            }

            var dribbleEval = EvaluateDribble(blackboard);
            if (dribbleEval.Score > action.Score)
            {
                action = dribbleEval.ToAction();
            }

            var passEval = EvaluatePass(blackboard);
            if (passEval.Score > action.Score)
            {
                action = passEval.ToAction();
            }

            return action;
        }
    }
}
