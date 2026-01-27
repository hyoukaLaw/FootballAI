# 策略模式使用指南

## 📖 基础使用

策略模式重构后，现有的调用代码无需修改，系统会自动根据角色类型选择对应的策略。

### 自动选择策略（推荐）

```csharp
// 系统会自动根据 PlayerRoleType 选择策略
// 在 TaskEvaluateRoleBaseOffensiveOptions.Evaluate() 中
public override NodeState Evaluate()
{
    var strategy = OffensiveStrategyFactory.GetStrategy(Blackboard.Role.RoleType);
    var action = strategy.Evaluate(Blackboard);
    
    ApplyActionToBlackboard(action);
    
    LogOffensiveEvaluation(action, strategy.StrategyName);
    
    return NodeState.SUCCESS;
}
```

## 🎨 自定义策略

### 1. 创建自定义策略类

```csharp
using UnityEngine;

namespace BehaviorTree.Runtime
{
    /// <summary>
    /// 自定义中场策略
    /// 更加强调传球和组织进攻
    /// </summary>
    public class AggressiveMidfielderStrategy : BaseOffensiveStrategy
    {
        protected override float GetBasePassScore()
        {
            // 中场使用较高的传球基础分
            return 75f;
        }

        protected override bool ShouldConsiderShoot()
        {
            // 中场只在很近时才考虑射门
            return IsCloseToGoal();
        }

        protected override bool ShouldConsiderPass()
        {
            return true; // 中场优先传球
        }

        protected override bool ShouldConsiderDribble()
        {
            return true; // 中场可以带球推进
        }

        protected override bool ShouldConsiderClearance()
        {
            return false; // 中场一般不解围
        }

        // 自定义评估逻辑（可选）
        protected override OffensiveAction Evaluate(FootballBlackboard blackboard)
        {
            // 如果处于危险区域，优先带球逃生
            if (IsInDangerZone(blackboard))
            {
                var dribbleEval = EvaluateDribble(blackboard);
                return dribbleEval.ToAction();
            }

            // 否则使用默认逻辑
            return base.Evaluate(blackboard);
        }

        private bool IsCloseToGoal()
        {
            Vector3 enemyGoalPos = Blackboard.MatchContext.GetEnemyGoalPosition(Blackboard.Owner);
            float distToGoal = Vector3.Distance(Blackboard.Owner.transform.position, enemyGoalPos);
            return distToGoal < 8f;
        }

        private bool IsInDangerZone(FootballBlackboard blackboard)
        {
            var opponents = blackboard.MatchContext.GetOpponents(blackboard.Owner);
            var nearbyEnemies = FootballUtils.FindNearEnemies(
                blackboard.Owner, 
                opponents, 
                3f
            );
            return nearbyEnemies.Count >= 2;
        }
    }
}
```

### 2. 注册自定义策略

```csharp
// 在游戏初始化或设置时注册策略
OffensiveStrategyFactory.RegisterStrategy(
    PlayerRoleType.Midfielder, 
    new AggressiveMidfielderStrategy()
);

// 或者替换默认策略
OffensiveStrategyFactory.RegisterStrategy(
    PlayerRoleType.Forward, 
    new CustomForwardStrategy()
);
```

### 3. 运行时切换策略

```csharp
// 根据比赛状态动态切换策略
public void UpdateTactics(MatchState currentState)
{
    IOffensiveEvaluationStrategy strategy;
    
    switch (currentState)
    {
        case MatchState.Attacking:
            strategy = new AggressiveMidfielderStrategy();
            break;
        case MatchState.Defending:
            strategy = new DefensiveMidfielderStrategy();
            break;
        default:
            strategy = new StandardMidfielderStrategy();
            break;
    }
    
    OffensiveStrategyFactory.RegisterStrategy(
        PlayerRoleType.Midfielder, 
        strategy
    );
}
```

## 🔍 策略测试

### 单元测试示例

```csharp
using UnityEngine;
using NUnit.Framework;

public class OffensiveStrategyTests
{
    private FootballBlackboard CreateTestBlackboard(PlayerRoleType roleType)
    {
        var blackboard = new GameObject().AddComponent<FootballBlackboard>();
        blackboard.Role = ScriptableObject.CreateInstance<PlayerRole>();
        blackboard.Role.RoleType = roleType;
        return blackboard;
    }

    [Test]
    public void DefenderStrategy_ShouldConsiderPass()
    {
        var blackboard = CreateTestBlackboard(PlayerRoleType.Defender);
        var strategy = new DefenderOffensiveStrategy();
        
        var action = strategy.Evaluate(blackboard);
        
        Assert.AreEqual(OffensiveActionType.Pass, action.ActionType);
    }

    [Test]
    public void ForwardStrategy_ShouldConsiderShoot_WhenCloseToGoal()
    {
        var blackboard = CreateTestBlackboard(PlayerRoleType.Forward);
        // 设置位置靠近球门...
        var strategy = new ForwardOffensiveStrategy();
        
        var action = strategy.Evaluate(blackboard);
        
        Assert.AreEqual(OffensiveActionType.Shoot, action.ActionType);
    }
}
```

## 📊 策略对比

### DefenderOffensiveStrategy
- **优先级**: 传球 > 带球 > 解围
- **传球基础分**: 80
- **射门**: 不考虑
- **解围**: 考虑

### ForwardOffensiveStrategy
- **优先级**: 射门 > 带球 > 传球
- **传球基础分**: 60
- **射门**: 考虑
- **解围**: 不考虑

### MidfielderOffensiveStrategy
- **优先级**: 传球 > 射门 > 带球
- **传球基础分**: 60
- **射门**: 考虑
- **解围**: 不考虑

## 🎯 最佳实践

### 1. 遵循开闭原则
```csharp
// ✅ 好的做法：创建新策略
public class NewRoleStrategy : BaseOffensiveStrategy
{
    // 实现...
}

// ❌ 不好的做法：修改现有策略
public class ForwardOffensiveStrategy : BaseOffensiveStrategy
{
    // 不应该修改优先级，应该创建新策略
}
```

### 2. 重用基类方法
```csharp
// ✅ 好的做法：重用基类方法
protected override OffensiveAction Evaluate(FootballBlackboard blackboard)
{
    // 自定义前置逻辑
    if (ShouldUseCustomLogic())
    {
        return CustomEvaluation(blackboard);
    }
    
    // 重用默认逻辑
    return base.Evaluate(blackboard);
}

// ❌ 不好的做法：重复实现
protected override OffensiveAction Evaluate(FootballBlackboard blackboard)
{
    // 重复实现基类已有的逻辑...
}
```

### 3. 使用日志调试
```csharp
// 查看日志输出，了解策略选择
========== 进攻选择评估 ==========
球员: Player_Forward_1 | 角色: Forward | 策略: ForwardOffensiveStrategy
----------------------------------------
【评分详情】
射门分: 85.32
传球分: 65.00 | 目标: Player_Midfielder_2
带球分: 50.00 | 目标: (12.3, 0.0, 25.6)
----------------------------------------
【最终选择】 射门 | 得分: 85.32
======================================
```

## 🔧 调试技巧

### 1. 检查当前策略
```csharp
var strategy = OffensiveStrategyFactory.GetStrategy(PlayerRoleType.Forward);
Debug.Log($"当前策略: {strategy.StrategyName}");
```

### 2. 检查策略是否已注册
```csharp
if (OffensiveStrategyFactory.HasStrategy(PlayerRoleType.Midfielder))
{
    Debug.Log("中场策略已注册");
}
else
{
    Debug.LogWarning("中场策略未注册，将使用默认策略");
}
```

### 3. 策略性能监控
```csharp
using System.Diagnostics;

var stopwatch = Stopwatch.StartNew();
var action = strategy.Evaluate(blackboard);
stopwatch.Stop();

Debug.Log($"策略评估耗时: {stopwatch.ElapsedMilliseconds}ms");
```

## 🚀 高级用法

### 1. 策略组合
```csharp
public class CompositeOffensiveStrategy : IOffensiveEvaluationStrategy
{
    private List<IOffensiveEvaluationStrategy> _strategies;
    private Func<OffensiveAction, bool> _selectionCriteria;
    
    public CompositeOffensiveStrategy(
        List<IOffensiveEvaluationStrategy> strategies,
        Func<OffensiveAction, bool> selectionCriteria
    )
    {
        _strategies = strategies;
        _selectionCriteria = selectionCriteria;
    }
    
    public string StrategyName => "Composite";
    
    public OffensiveAction Evaluate(FootballBlackboard blackboard)
    {
        foreach (var strategy in _strategies)
        {
            var action = strategy.Evaluate(blackboard);
            if (_selectionCriteria(action))
            {
                return action;
            }
        }
        return OffensiveAction.None;
    }
}

// 使用组合策略
var strategies = new List<IOffensiveEvaluationStrategy>
{
    new ForwardOffensiveStrategy(),
    new MidfielderOffensiveStrategy()
};

var composite = new CompositeOffensiveStrategy(
    strategies,
    action => action.Score > 70f
);
```

### 2. 策略装饰器
```csharp
public class LoggingStrategyDecorator : IOffensiveEvaluationStrategy
{
    private IOffensiveEvaluationStrategy _innerStrategy;
    
    public LoggingStrategyDecorator(IOffensiveEvaluationStrategy innerStrategy)
    {
        _innerStrategy = innerStrategy;
    }
    
    public string StrategyName => $"{_innerStrategy.StrategyName} (Logged)";
    
    public OffensiveAction Evaluate(FootballBlackboard blackboard)
    {
        Debug.Log($"开始评估: {_innerStrategy.StrategyName}");
        var action = _innerStrategy.Evaluate(blackboard);
        Debug.Log($"评估完成: {action.ActionType}, 得分: {action.Score}");
        return action;
    }
}

// 使用装饰器
var strategy = new LoggingStrategyDecorator(
    new ForwardOffensiveStrategy()
);
```

## 📚 总结

策略模式重构后：
- ✅ 代码更清晰、更易维护
- ✅ 新增角色无需修改现有代码
- ✅ 支持运行时策略切换
- ✅ 易于测试和调试
- ✅ 支持自定义和扩展

通过合理使用策略模式，可以让AI决策系统更加灵活和强大！
