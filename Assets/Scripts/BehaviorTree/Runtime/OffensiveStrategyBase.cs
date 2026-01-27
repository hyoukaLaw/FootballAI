using System.Collections.Generic;
using UnityEngine;

namespace BehaviorTree.Runtime
{
    /// <summary>
    /// 进攻评估策略接口
    /// </summary>
    public interface IOffensiveEvaluationStrategy
    {
        /// <summary>
        /// 评估并返回最佳进攻动作
        /// </summary>
        OffensiveAction Evaluate(FootballBlackboard blackboard);

        /// <summary>
        /// 获取策略名称（用于调试）
        /// </summary>
        string StrategyName { get; }
    }

    /// <summary>
    /// 进攻评估策略抽象基类
    /// 提供通用的评估流程和共享方法
    /// </summary>
    public abstract class BaseOffensiveStrategy : IOffensiveEvaluationStrategy
    {
        public string StrategyName => GetType().Name;

        /// <summary>
        /// 模板方法：定义评估流程
        /// </summary>
        public virtual OffensiveAction Evaluate(FootballBlackboard blackboard)
        {
            OffensiveAction action = OffensiveAction.None;

            if (ShouldConsiderShoot())
            {
                var shootEval = EvaluateShoot(blackboard);
                if (shootEval.Score > action.Score)
                {
                    action = shootEval.ToAction();
                }
            }

            if (ShouldConsiderPass())
            {
                var passEval = EvaluatePass(blackboard);
                if (passEval.Score > action.Score)
                {
                    action = passEval.ToAction();
                }
            }

            if (ShouldConsiderDribble())
            {
                var dribbleEval = EvaluateDribble(blackboard);
                if (dribbleEval.Score > action.Score)
                {
                    action = dribbleEval.ToAction();
                }
            }

            if (ShouldConsiderClearance())
            {
                var clearanceEval = EvaluateClearance(blackboard);
                if (clearanceEval.Score > action.Score)
                {
                    action = clearanceEval.ToAction();
                }
            }

            return action;
        }

        protected abstract float GetBasePassScore();
        protected abstract bool ShouldConsiderShoot();
        protected abstract bool ShouldConsiderPass();
        protected abstract bool ShouldConsiderDribble();
        protected abstract bool ShouldConsiderClearance();

        protected virtual ShootEvaluation EvaluateShoot(FootballBlackboard blackboard)
        {
            Vector3 enemyGoalPos = blackboard.MatchContext.GetEnemyGoalPosition(blackboard.Owner);
            List<GameObject> opponents = blackboard.MatchContext.GetOpponents(blackboard.Owner);
            
            float distToGoal = Vector3.Distance(blackboard.Owner.transform.position, enemyGoalPos);
            float distanceScore = Mathf.Max((1 - Mathf.Max(distToGoal - 5, 0f) / 5f), 0) * FootballConstants.BaseScoreShootScore;
            
            bool isPathClear = FootballUtils.IsPathClear(
                blackboard.Owner.transform.position,
                enemyGoalPos,
                opponents,
                FootballConstants.ClearanceBlockThreshold
            );
            
            float shootBlockFactor = isPathClear ? FootballConstants.ShootNoBlockFactor : FootballConstants.ShootBlockPenaltyFactor;
            float score = distanceScore * shootBlockFactor;
            
            return new ShootEvaluation
            {
                Score = score,
                Target = enemyGoalPos,
                Distance = distToGoal
            };
        }

        protected virtual PassEvaluation EvaluatePass(FootballBlackboard blackboard)
        {
            return TaskEvaluateRoleBaseOffensiveOptions.OffensiveActionCalculator.CalculatePassScoreAndTarget(
                blackboard,
                GetBasePassScore()
            );
        }

        protected virtual DribbleEvaluation EvaluateDribble(FootballBlackboard blackboard)
        {
            return TaskEvaluateRoleBaseOffensiveOptions.OffensiveActionCalculator.CalculateDribbleScoreAndTarget(
                blackboard
            );
        }

        protected virtual ClearanceEvaluation EvaluateClearance(FootballBlackboard blackboard)
        {
            return TaskEvaluateRoleBaseOffensiveOptions.OffensiveActionCalculator.CalculateClearanceScoreAndTarget(
                blackboard
            );
        }
    }
}
