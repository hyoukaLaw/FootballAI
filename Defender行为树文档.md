# Defender行为树详细文档

## 概述

Defender行为树是一个足球AI后卫角色的行为决策系统，使用行为树架构实现智能决策。该行为树基于xNode可视化编辑器构建，包含多个控制节点、条件节点和动作节点。

## 行为树结构

### 根节点：Selector Node Graph
位置：(-1128, 136)
功能：顶级选择器，按优先级选择执行分支
中止类型：None

子节点（按执行顺序）：
1. StunSeq - 击晕处理序列
2. PassTargetSeq - 接传球序列
3. DefenseRootSelector - 防守根选择器
4. OffenseCheckSeq - 进攻检查序列

---

## 1. 击晕处理分支

### 1.1 StunSeq（序列节点）
位置：(-600, -312)
中止类型：None

**子节点：**
1. Check Is Stunned Graph
2. Task Stun Wait Graph

#### 1.1.1 Check Is Stunned Graph
**类型：** 条件节点
**位置：** (-392, -376)
**脚本：** CheckIsStunned.cs

```csharp
public override NodeState Evaluate()
{
    if (Blackboard.IsStunned)
    {
        return NodeState.SUCCESS;
    }
    else
    {
        return NodeState.FAILURE;
    }
}
```

**功能：** 检查当前球员是否处于击晕状态

#### 1.1.2 Task Stun Wait Graph
**类型：** 动作节点
**位置：** (-376, -264)
**脚本：** TaskStunWait.cs

```csharp
public override NodeState Evaluate()
{
    if (!Blackboard.IsStunned)
    {
        return NodeState.FAILURE;
    }

    // 更新停顿计时器
    Blackboard.StunTimer -= Time.deltaTime;

    if (Blackboard.StunTimer <= 0f)
    {
        // 停顿结束，清除状态
        Blackboard.IsStunned = false;
        Blackboard.StunTimer = 0f;
        return NodeState.SUCCESS;
    }

    // 继续停顿中
    return NodeState.RUNNING;
}
```

**功能：** 等待击晕状态结束，每帧递减计时器

---

## 2. 接传球分支

### 2.1 PassTargetSeq（序列节点）
位置：(-616, -136)
中止类型：None

**子节点：**
1. Check Is Pass Target Graph
2. Task Chase Ball Graph
3. Task Move To Position Graph

#### 2.1.1 Check Is Pass Target Graph
**类型：** 条件节点
**位置：** (-312, -120)
**脚本：** CheckIsPassTarget.cs

```csharp
public override NodeState Evaluate()
{
    if (Blackboard.MatchContext != null &&
        Blackboard.MatchContext.IsPassTarget(Blackboard.Owner))
    {
        return NodeState.SUCCESS;
    }
    return NodeState.FAILURE;
}
```

**功能：** 检查当前球员是否是传球目标，用于锁定接球机制

#### 2.1.2 Task Chase Ball Graph
**类型：** 动作节点
**位置：** (-312, -24)
**脚本：** TaskChaseBall.cs

```csharp
public override NodeState Evaluate()
{
    if (Blackboard.MatchContext == null || Blackboard.MatchContext.Ball == null)
        return NodeState.FAILURE;
    Vector3 ballPos = Blackboard.MatchContext.Ball.transform.position;
    // 核心逻辑：把要去的地方，设为球当前的位置
    // 这样 TaskMoveToPosition 就会让你直接跑向球
    Blackboard.MoveTarget = Blackboard.Owner.transform.position +
                            (ballPos - Blackboard.Owner.transform.position).normalized * FootballConstants.DecideMinStep;
    return NodeState.SUCCESS;
}
```

**功能：** 将移动目标设置为球的当前位置，实现追球行为

#### 2.1.3 Task Move To Position Graph
**类型：** 动作节点
**位置：** (-312, 72)
**脚本：** TaskMoveToPosition.cs

```csharp
public override NodeState Evaluate()
{
    GameObject owner = Blackboard.Owner;
    if (Vector3.Distance(_targetPos, Blackboard.MoveTarget) > FootballConstants.DecideMinStep / 2f)
    {
        _targetPos = Blackboard.MoveTarget;
    }

    float distance = Vector3.Distance(owner.transform.position, _targetPos);

    if (distance < _stoppingDistance)
    {
        return NodeState.SUCCESS;
    }

    Vector3 newPos = Vector3.MoveTowards(owner.transform.position, _targetPos,
        Blackboard.Stats.MovementSpeed * Time.deltaTime);

    if (_targetPos != owner.transform.position)
    {
        Vector3 direction = (_targetPos - owner.transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            owner.transform.forward = direction;
        }
    }

    NodeState retNodeState = NodeState.RUNNING;
    Vector3 clampedNewPos = LimitPosToField(newPos);
    if (Vector3.Distance(clampedNewPos, newPos) > _stoppingDistance)
        retNodeState = NodeState.SUCCESS;
    owner.transform.position = clampedNewPos;

    DribbleBall(owner);
    return retNodeState;
}

private void DribbleBall(GameObject owner)
{
    if (Blackboard.MatchContext.BallHolder == owner)
    {
        Vector3 ballPos = owner.transform.position + owner.transform.forward * _dribbleOffset;
        ballPos.y = 0f;
        ballPos = LimitPosToField(ballPos);
        Blackboard.MatchContext.Ball.transform.position = ballPos;
    }
}
```

**功能：** 执行移动到目标位置的动作，如果持球则带球移动

---

## 3. 防守分支

### 3.1 DefenseRootSelector（选择器节点）
位置：(-792, 792)
中止类型：None

**子节点：**
1. TackleSequence - 抢断序列
2. OrganizedDefenseSeq - 组织防守序列

#### 3.1.1 TackleSequence（序列节点）
位置：(-568, 888)
中止类型：None

**子节点：**
1. Check Can Tackle Graph
2. Task Tackle Graph

##### Check Can Tackle Graph
**类型：** 条件节点
**位置：** (-312, 760)
**脚本：** CheckCanTackle.cs

```csharp
public override NodeState Evaluate()
{
    if (Blackboard.MatchContext.BallHolder == null)
    {
        return NodeState.FAILURE;
    }

    float distance = Vector3.Distance(
        Blackboard.Owner.transform.position,
        Blackboard.MatchContext.BallHolder.transform.position
    );

    if (distance < FootballConstants.TryTackleDistance)
    {
        return NodeState.SUCCESS;
    }
    else
    {
        return NodeState.FAILURE;
    }
}
```

**功能：** 检查是否在抢断范围内

##### Task Tackle Graph
**类型：** 动作节点
**位置：** (-312, 888)
**脚本：** TaskTackle.cs

```csharp
public override NodeState Evaluate()
{
    if (Blackboard.MatchContext != null && Blackboard.MatchContext.IsInStealCooldown)
    {
        return NodeState.FAILURE;
    }

    if (Blackboard.MatchContext == null || Blackboard.MatchContext.BallHolder == null)
        return NodeState.FAILURE;

    GameObject owner = Blackboard.Owner;
    GameObject ballHolder = Blackboard.MatchContext.BallHolder;

    float tackleDistance = ballHolder.GetComponent<PlayerAI>().Stats.TackledDistance;
    float distanceToBallHolder = Vector3.Distance(owner.transform.position, ballHolder.transform.position);

    float tackleChance = CalculateTackleChance(owner, ballHolder);
    float random = Random.Range(0, 1f);

    if(random <= tackleChance)
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

private float CalculateTackleChance(GameObject tackler, GameObject ballHolder)
{
    PlayerAI tacklerAI = tackler.GetComponent<PlayerAI>();
    PlayerAI ballHolderAI = ballHolder.GetComponent<PlayerAI>();

    if (tacklerAI == null || ballHolderAI == null)
        return FootballConstants.DefaultTackleSuccessRate;

    float defensiveFactor = tacklerAI.Stats.DefensiveAwareness;
    float distanceFactor = Mathf.Clamp01(FootballConstants.TackleDistanceFactorBase -
        Vector3.Distance(tackler.transform.position, ballHolder.transform.position));

    float tackleChance = FootballConstants.BaseTackleProbability +
                         defensiveFactor * FootballConstants.DefensiveAttributeBonus +
                         distanceFactor * FootballConstants.DistanceBonusCoefficient;
    return Mathf.Clamp01(tackleChance);
}

private void StealBall(GameObject tackler)
{
    if (Blackboard.MatchContext == null || Blackboard.MatchContext.Ball == null)
        return;

    Vector3 tacklerPosition = tackler.transform.position;
    Blackboard.MatchContext.Ball.transform.position = tacklerPosition;

    GameObject ballHolder = Blackboard.MatchContext.BallHolder;

    if (Blackboard.MatchContext != null)
        Blackboard.MatchContext.BallHolder = tackler;

    MatchManager.Instance.TriggerStealCooldown();

    if (ballHolder != null)
    {
        var ballHolderAI = ballHolder.GetComponent<PlayerAI>();
        if (ballHolderAI != null && ballHolderAI.GetBlackboard() != null)
        {
            var bb = ballHolderAI.GetBlackboard();
            bb.IsStunned = true;
            bb.StunTimer = bb.StunDuration;
        }
    }
}
```

**功能：** 执行抢断动作，计算成功率并执行抢断

#### 3.1.2 OrganizedDefenseSeq（序列节点）
位置：(-568, 1032)
中止类型：None

**子节点：**
1. Task Move To Position Graph
2. Task Calculate Role Based Defense Position Graph

##### Task Move To Position Graph
**类型：** 动作节点
**位置：** (-296, 1096)
**脚本：** TaskMoveToPosition.cs（同2.1.3）

##### Task Calculate Role Based Defense Position Graph
**类型：** 动作节点
**位置：** (-296, 1000)
**脚本：** TaskCalculateRoleBasedDefensePosition.cs

```csharp
public override NodeState Evaluate()
{
    Vector3 curPos = Blackboard.Owner.transform.position;
    GameObject owner = Blackboard.Owner;
    Vector3 bestPos = ContextAwareZoneCalculator.FindBestPosition(Blackboard.Role, curPos,
        Blackboard.MatchContext.GetMyGoalPosition(owner), Blackboard.MatchContext.GetEnemyGoalPosition(owner),
        Blackboard.MatchContext.Ball.transform.position, Blackboard.MatchContext, owner,
        Blackboard.MatchContext.GetTeammates(owner), Blackboard.MatchContext.GetOpponents(owner), Blackboard);
    Blackboard.MoveTarget = bestPos;
    return NodeState.SUCCESS;
}
```

**功能：** 计算基于角色的最佳防守位置

---

## 4. 进攻分支

### 4.1 OffenseCheckSeq（序列节点）
位置：(-760, 264)
中止类型：None

**子节点：**
1. Check Is Team Controlling Ball Graph
2. OffensiveRoot (Selector)

#### 4.1.1 Check Is Team Controlling Ball Graph
**类型：** 条件节点
**位置：** (-312, 216)
**脚本：** CheckIsTeamControllingBallNode.cs

```csharp
public override NodeState Evaluate()
{
    GameObject ballHolder = Blackboard.MatchContext.BallHolder;

    if (ballHolder == null)
    {
        if(Blackboard.MatchContext.GetTeammates(Blackboard.Owner).Contains(Blackboard.MatchContext.IncomingPassTarget))
            return NodeState.SUCCESS;
        return NodeState.FAILURE;
    }

    if (ballHolder == Blackboard.Owner)
    {
        return NodeState.SUCCESS;
    }

    var teammates = Blackboard.MatchContext.GetTeammates(Blackboard.Owner);
    if (teammates != null && teammates.Contains(ballHolder))
    {
        return NodeState.SUCCESS;
    }

    return NodeState.FAILURE;
}
```

**功能：** 检查当前球权是否在己方队伍手中

#### 4.1.2 OffensiveRoot（选择器节点）
位置：(-312, 328)
中止类型：None

**子节点：**
1. HasBall(Sequence) - 持球序列
2. supportSeq - 支持序列

##### 4.1.2.1 HasBall(Sequence)
位置：(-88, 344)
中止类型：Self

**子节点：**
1. Check Has Ball Node Graph
2. Task Evaluate Role Base Offensive Options Graph
3. ActionSelector

###### Check Has Ball Node Graph
**类型：** 条件节点
**位置：** (232, 280)
**脚本：** CheckHasBallNode.cs

```csharp
public override NodeState Evaluate()
{
    if (Blackboard.MatchContext == null ||
        Blackboard.MatchContext.BallHolder == null ||
        Blackboard.Owner == null)
    {
        return NodeState.FAILURE;
    }

    if (Blackboard.MatchContext.BallHolder == Blackboard.Owner)
    {
        return NodeState.SUCCESS;
    }
    else
    {
        return NodeState.FAILURE;
    }
}
```

**功能：** 检查当前球员是否持球

###### Task Evaluate Role Base Offensive Options Graph
**类型：** 动作节点
**位置：** (232, 392)
**脚本：** TaskEvaluateRoleBaseOffensiveOptions.cs

```csharp
public override NodeState Evaluate()
{
    if (Blackboard.Role.RoleType == PlayerRoleType.Defender)
    {
        HandleDefenderOptions();
        return NodeState.SUCCESS;
    }
    return NodeState.FAILURE;
}

private void HandleDefenderOptions()
{
    CalculatePassScoreAndTarget(out float passScore, out GameObject passTarget);
    CalculateDribbleScoreAndTarget(out float dribbleScore, out Vector3 dribbleTarget);
    CalculateClearanceScoreAndTarget(out float clearanceScore, out Vector3 clearanceTarget);

    if (passScore > dribbleScore && passScore > clearanceScore)
    {
        Blackboard.MoveTarget = Vector3.zero;
        Blackboard.BestPassTarget = passTarget;
        Blackboard.ClearanceTarget = Vector3.negativeInfinity;
    }
    else if (dribbleScore > passScore && dribbleScore > clearanceScore)
    {
        Blackboard.MoveTarget = dribbleTarget;
        Blackboard.BestPassTarget = null;
        Blackboard.ClearanceTarget = Vector3.negativeInfinity;
    }
    else
    {
        Blackboard.MoveTarget = Vector3.zero;
        Blackboard.BestPassTarget = null;
        Blackboard.ClearanceTarget = clearanceTarget;
    }
}
```

**功能：** 评估进攻选项（传球、带球、解围），选择最优方案

###### ActionSelector
**类型：** 选择器节点
**位置：** (216, 520)
中止类型：None

**子节点：**
1. PassSequence - 传球序列
2. DribbleSequence - 带球序列
3. ClearanceSequence - 解围序列

##### 4.1.2.2 supportSeq
位置：(-312, 552)
中止类型：None

**子节点：**
1. Task Calculate Role Based Defense Position Graph
2. Task Move To Position Graph

###### Task Calculate Role Based Defense Position Graph
**类型：** 动作节点
**位置：** (-56, 488)
**脚本：** TaskCalculateRoleBasedDefensePosition.cs（同3.1.2.2）

###### Task Move To Position Graph
**类型：** 动作节点
**位置：** (-56, 600)
**脚本：** TaskMoveToPosition.cs（同2.1.3）

---

## 5. 具体动作序列

### 5.1 PassSequence（序列节点）
位置：(472, 456)
中止类型：None

**子节点：**
1. Check Can Pass Graph
2. Task Pass Ball Graph

#### Check Can Pass Graph
**类型：** 条件节点
**位置：** (760, 392)
**脚本：** CheckCanPass.cs

```csharp
public override NodeState Evaluate()
{
    if (Blackboard.BestPassTarget != null)
    {
        return NodeState.SUCCESS;
    }
    else
    {
        return NodeState.FAILURE;
    }
}
```

**功能：** 检查是否有传球目标

#### Task Pass Ball Graph
**类型：** 动作节点
**位置：** (760, 520)
**脚本：** TaskPassBall.cs

```csharp
public override NodeState Evaluate()
{
    if (Blackboard.MatchContext == null || Blackboard.MatchContext.Ball == null)
        return NodeState.FAILURE;

    GameObject target = Blackboard.BestPassTarget;
    GameObject ball = Blackboard.MatchContext.Ball;

    if (target == null) return NodeState.FAILURE;

    BallController ballCtrl = ball.GetComponent<BallController>();

    if (Blackboard.MatchContext != null)
    {
        Blackboard.MatchContext.SetPassTarget(target);
    }

    float distanceToTarget = Vector3.Distance(ball.transform.position, target.transform.position);
    float flightTime = distanceToTarget / Blackboard.Stats.PassingSpeed;

    PlayerAI targetAI = target.GetComponent<PlayerAI>();
    float targetSpeed = targetAI != null ? targetAI.Stats.MovementSpeed : 2.0f;
    Vector3 targetVelocity = target.transform.forward * targetSpeed;

    Vector3 predictedPos = target.transform.position;

    ballCtrl.KickTo(predictedPos, Blackboard.Stats.PassingSpeed);
    Blackboard.IsStunned = true;
    Blackboard.StunTimer = Blackboard.StunDuration;

    return NodeState.SUCCESS;
}
```

**功能：** 执行传球动作，计算提前量并传球

### 5.2 DribbleSequence（序列节点）
位置：(472, 600)
中止类型：None

**子节点：**
1. Check Has Move Target Graph
2. Task Move To Position Graph

#### Check Has Move Target Graph
**类型：** 条件节点
**位置：** (760, 632)
**脚本：** CheckHasMoveTarget.cs

```csharp
public override NodeState Evaluate()
{
    if (Blackboard.MoveTarget != Vector3.zero)
    {
        return NodeState.SUCCESS;
    }
    else
    {
        return NodeState.FAILURE;
    }
}
```

**功能：** 检查是否有移动目标

#### Task Move To Position Graph
**类型：** 动作节点
**位置：** (760, 728)
**脚本：** TaskMoveToPosition.cs（同2.1.3）

### 5.3 ClearanceSequence（序列节点）
位置：(472, 728)
中止类型：None

**子节点：**
1. Check Has Clearance Target Graph
2. Task Clear Graph

#### Check Has Clearance Target Graph
**类型：** 条件节点
**位置：** (760, 824)
**脚本：** CheckHasClearanceTarget.cs

```csharp
public override NodeState Evaluate()
{
    if(Blackboard.ClearanceTarget != Vector3.negativeInfinity)
    {
        return NodeState.SUCCESS;
    }
    return NodeState.FAILURE;
}
```

**功能：** 检查是否有解围目标

#### Task Clear Graph
**类型：** 动作节点
**位置：** (760, 920)
**脚本：** TaskClear.cs

```csharp
public override NodeState Evaluate()
{
    var ballControl = Blackboard.MatchContext.Ball.GetComponent<BallController>();
    ballControl.KickTo(Blackboard.ClearanceTarget, FootballConstants.ClearKickSpeed);
    return NodeState.RUNNING;
}
```

**功能：** 执行解围动作，将球踢向解围目标

---

## 节点类型说明

### SelectorNode（选择器节点）
按顺序执行子节点，遇到第一个SUCCESS即停止并返回SUCCESS
- 如果所有子节点都FAILURE，返回FAILURE
- 支持RUNNING状态（正在执行的节点）

### SequenceNode（序列节点）
按顺序执行所有子节点
- 所有子节点都SUCCESS才返回SUCCESS
- 任一子节点FAILURE则立即返回FAILURE
- 支持RUNNING状态（正在执行的节点）

### ActionNode（动作节点）
执行具体动作，可以返回SUCCESS/FAILURE/RUNNING

### ConditionalNode（条件节点）
检查条件，返回SUCCESS或FAILURE

---

## 后卫角色配置（DefenderRole.asset）

```yaml
RoleType: 0 (Defender)
RoleName: Defender

AttackPreferences:
  OwnDefensiveZoneWeight: 4.5
  OwnOffensiveZoneWeight: 4.5
  EnemyOffensiveZoneWeight: 0.5
  EnemyDefensiveZoneWeight: 0.5
  DistanceDecayRate: 0.1
  MaxZoneDeviation: 10

DefendPreferences:
  OwnDefensiveZoneWeight: 8
  OwnOffensiveZoneWeight: 1
  EnemyOffensiveZoneWeight: 0.5
  EnemyDefensiveZoneWeight: 0.5
  DistanceDecayRate: 0.08
  MaxZoneDeviation: 8

ChaseBallPreferences:
  OwnDefensiveZoneWeight: 5
  OwnOffensiveZoneWeight: 4
  EnemyOffensiveZoneWeight: 2
  EnemyDefensiveZoneWeight: 1
  DistanceDecayRate: 0.12
  MaxZoneDeviation: 12

OffensiveBias: 0.3
DefensiveBias: 0.8
SupportBias: 0.5
HomeZoneRadius: 6
MaximumRoamingDistance: 15
```

---

## 行为树执行流程总结

1. **击晕状态检查** - 如果被击晕，等待击晕结束
2. **接传球检查** - 如果是传球目标，追球接应
3. **防守行为** - 如果对方控球，尝试抢断或组织防守
4. **进攻行为** - 如果己方控球：
   - 如果自己持球：评估传球/带球/解围，选择最优方案
   - 如果队友持球：计算支持位置并移动

---

## 文件清单

### 行为树定义文件
- `Assets/Resources/BTGraph/Defender.asset` - Defender行为树定义
- `Assets/Resources/Roles/DefenderRole.asset` - Defender角色配置

### 运行时节点脚本
- `Assets/Scripts/BehaviorTree/Runtime/Node.cs` - 节点基类
- `Assets/Scripts/BehaviorTree/Runtime/SelectorNode.cs` - 选择器节点
- `Assets/Scripts/BehaviorTree/Runtime/SequenceNode.cs` - 序列节点

### 条件节点
- `CheckIsStunned.cs` - 检查击晕状态
- `CheckIsPassTarget.cs` - 检查是否传球目标
- `CheckCanTackle.cs` - 检查能否抢断
- `CheckIsTeamControllingBallNode.cs` - 检查己方控球
- `CheckHasBallNode.cs` - 检查是否持球
- `CheckCanPass.cs` - 检查能否传球
- `CheckHasMoveTarget.cs` - 检查是否有移动目标
- `CheckHasClearanceTarget.cs` - 检查是否有解围目标

### 动作节点
- `TaskStunWait.cs` - 等待击晕结束
- `TaskChaseBall.cs` - 追球
- `TaskMoveToPosition.cs` - 移动到位置
- `TaskTackle.cs` - 抢断
- `TaskCalculateRoleBasedDefensePosition.cs` - 计算防守位置
- `TaskEvaluateRoleBaseOffensiveOptions.cs` - 评估进攻选项
- `TaskPassBall.cs` - 传球
- `TaskClear.cs` - 解围

---

**文档生成日期：** 2026-01-23
**项目：** FootballAI
