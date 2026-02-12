using UnityEngine;
using FootballAI.FootballCore;

namespace BehaviorTree.Runtime
{
    /// <summary>
    /// 中场进攻评估策略
    /// 优先级：传球 > 射门 > 带球
    /// </summary>
    public class MidfielderOffensiveStrategy : BaseOffensiveStrategy
    {
        protected override float GetBasePassScore()
        {
            return FootballConstants.BasePassScoreMidfielder;
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

            var passEval = EvaluatePass(blackboard);
            if (passEval.Score > action.Score)
            {
                action = passEval.ToAction();
            }

            var dribbleEval = EvaluateDribble(blackboard);
            if (dribbleEval.Score > action.Score)
            {
                action = dribbleEval.ToAction();
            }

            return action;
        }
    }
}
