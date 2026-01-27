using UnityEngine;

namespace BehaviorTree.Runtime
{
    /// <summary>
    /// 后卫进攻评估策略
    /// 优先级：传球 > 带球 > 解围
    /// </summary>
    public class DefenderOffensiveStrategy : BaseOffensiveStrategy
    {
        protected override float GetBasePassScore()
        {
            return FootballConstants.BasePassScoreDefender;
        }

        protected override bool ShouldConsiderShoot()
        {
            return false;
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
            return true;
        }

        public override OffensiveAction Evaluate(FootballBlackboard blackboard)
        {
            OffensiveAction action = OffensiveAction.None;

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

            var clearanceEval = EvaluateClearance(blackboard);
            if (clearanceEval.Score > action.Score)
            {
                action = clearanceEval.ToAction();
            }

            return action;
        }
    }
}
