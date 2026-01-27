using System.Collections.Generic;

namespace BehaviorTree.Runtime
{
    /// <summary>
    /// 进攻评估策略工厂
    /// 负责创建和管理策略实例
    /// </summary>
    public static class OffensiveStrategyFactory
    {
        private static Dictionary<PlayerRoleType, IOffensiveEvaluationStrategy> _strategies;

        static OffensiveStrategyFactory()
        {
            _strategies = new Dictionary<PlayerRoleType, IOffensiveEvaluationStrategy>
            {
                { PlayerRoleType.Defender, new DefenderOffensiveStrategy() },
                { PlayerRoleType.Forward, new ForwardOffensiveStrategy() },
                { PlayerRoleType.Midfielder, new MidfielderOffensiveStrategy() }
            };
        }

        /// <summary>
        /// 获取指定角色的进攻评估策略
        /// </summary>
        public static IOffensiveEvaluationStrategy GetStrategy(PlayerRoleType roleType)
        {
            if (_strategies.TryGetValue(roleType, out var strategy))
            {
                return strategy;
            }

            return _strategies[PlayerRoleType.Forward];
        }

        /// <summary>
        /// 注册自定义策略
        /// </summary>
        public static void RegisterStrategy(PlayerRoleType roleType, IOffensiveEvaluationStrategy strategy)
        {
            _strategies[roleType] = strategy;
        }

        /// <summary>
        /// 检查是否已注册指定角色的策略
        /// </summary>
        public static bool HasStrategy(PlayerRoleType roleType)
        {
            return _strategies.ContainsKey(roleType);
        }
    }
}
