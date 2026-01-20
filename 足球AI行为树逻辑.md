# 足球AI行为树逻辑文档

## 概述

本文档详细描述了当前足球AI的行为树逻辑，该逻辑实现于`Assets/Scenes/New Football BT.asset`文件中。整个行为树采用Unity的行为树系统实现，控制足球AI球员的决策和行为。

---

## 行为树整体结构

### 根节点
- **类型**: Selector Node (选择器)
- **功能**: 根节点负责从多个高层策略中选择一个执行

---

## 一、根选择器（Root Selector）

根选择器包含4个主要分支，按优先级从高到低依次执行：

```
Root Selector
├── StunSeq (眩晕状态处理)
├── PassTargetSeq (作为传球目标)
├── LooseBallSeq (自由球处理 - 防守根)
└── offensiveBranch (进攻分支)
```

---

## 二、眩晕状态处理（StunSeq）

**位置**: (-312, -232)

**功能**: 当球员处于眩晕状态时，停止所有行为进行恢复

**执行流程**:
1. **Check Is Stunned Graph** - 检查球员是否处于眩晕状态
   - 如果是，进入下一步
   - 如果否，跳过此序列

2. **Task Stun Wait Graph** - 等待眩晕效果结束
   - 执行等待动作，不进行其他行为

---

## 三、传球目标序列（PassTargetSeq）

**位置**: (-360, -104)

**功能**: 当被队友选为传球目标时，跑位接球

**执行流程**:
1. **Check Is Pass Target Graph** - 检查是否被标记为传球目标
   - 如果是，进入下一步
   - 如果否，跳过此序列

2. **Task Chase Ball Graph** - 追球动作
   - 向传球方向移动以准备接球

3. **Task Move To Position Graph** - 移动到指定位置
   - 移动到最佳接球点

---

## 四、自由球处理（LooseBallSeq - 防守根）

**位置**: (-136, 504)

**功能**: 当球不在任何球员控制范围内时，争夺球权

**执行流程**:
1. **Check Is Closest To Loose Ball Graph** - 检查是否是离自由球最近的球员
   - 如果是，进入下一步
   - 如果否，跳过此序列

2. **Task Chase Ball Graph** - 追球动作
   - 快速跑向球的位置

3. **Task Move To Position Graph** - 移动到位置
   - 精确移动到球的位置进行控球

---

## 五、防守根节点（DefensiveRoot）

**位置**: (-584, 520)

**功能**: 在防守阶段的总体策略选择

**子分支**:

### 5.1 组织防守序列（organizedDefenseSeq）

**位置**: (-344, 664)

**执行流程**:
1. **Task Evaluate Defensive State Graph** - 评估防守状态
   - 分析场上形势、对手位置、球的位置等
   - 确定防守策略

2. **DefenseActionSelector** - 防守动作选择器
   根据评估结果选择以下防守动作之一：

#### 5.1.1 抢断序列（TackleSeq）

**位置**: (488, 760)

- **Check Can Tackle Graph** - 检查是否可以进行抢断
  - 判断距离、角度、时机是否合适
- **Task Tackle Graph** - 执行抢断动作
  - 尝试从对手脚下抢回球

#### 5.1.2 追球防守序列（chaseBallDefenseSeq）

**位置**: (488, 872)

- **Check Has No Marked Player Graph** - 检查是否没有需要盯防的对手
  - 如果没有直接盯防对象，则追球
- **Task Chase Ball Graph** - 追球动作
  - 跟随球移动

#### 5.1.3 位置防守移动

**位置**: (488, 1096)

- **Task Move To Position Graph** - 移动到防守位置
  - 回到预定的防守区域

### 5.2 兜底守门/追击（Sequence Node Graph）

**位置**: (-296, 1176)

**执行流程**:
1. **Task Chase Ball(兜底)** - 底层追球动作
   - 作为最后的防守手段，无论如何都尝试靠近球

2. **Task Move To Position Graph** - 移动到球的位置
   - 确保防守覆盖

---

## 六、进攻分支（offensiveBranch）

**位置**: (-424, 40)

**功能**: 在进攻阶段的总体策略

**执行流程**:
1. **Check Is Team Controlling Ball Graph** - 检查团队是否控球
   - 如果是，进入进攻策略
   - 如果否，跳过此分支

2. **OffensiveRoot(Selector)** - 进攻根选择器
   在以下策略中选择一个：

### 6.1 持球序列（HasBall）

**位置**: (-72, 152)

**功能**: 当球员持球时的决策

**执行流程**:
1. **Check Has Ball Node Graph** - 确认是否持球
2. **Task Evaluate Offensive Options Graph** - 评估进攻选项
   - 分析射门、传球、带球的可能性

3. **ActionSelector** - 动作选择器
   根据评估结果选择进攻动作：

#### 6.1.1 射门序列（ShootSequence）

**位置**: (664, 152)

- **Check Can Shoot Graph** - 检查是否可以射门
  - 判断射门角度、距离、是否被阻挡
- **Task Shoot Graph** - 执行射门动作
  - 向球门方向射门

#### 6.1.2 传球序列（PassSequence）

**位置**: (664, 264)

- **Check Can Pass Graph** - 检查是否可以传球
  - 查找可传球队友，判断传球路线是否安全
- **Task Pass Ball Graph** - 执行传球动作
  - 将球传给选定的队友

#### 6.1.3 带球序列（DribbleSequence）

**位置**: (616, 424)

- **Check Has Move Target Graph** - 检查是否有移动目标
  - 确定带球方向
- **Task Move To Position Graph** - 带球移动
  - 控球向目标位置移动

### 6.2 支持序列（supportSeq）

**位置**: (-296, 360)

**功能**: 当球队控球但自己不持球时，提供支持

**执行流程**:
1. **Task Calculate Support Spot Graph** - 计算支持位置
   - 分析场上形势，找到最佳的支援位置
   - 考虑队友位置、对手位置、传球路线等因素

2. **Task Move To Position Graph** - 移动到支持位置
   - 跑位到计算出的支持点
   - 为持球队友提供传球选择或策应

---

## 节点类型说明

### 控制节点

1. **Selector (选择器)**
   - 依次执行子节点
   - 当子节点成功时停止并返回成功
   - 当所有子节点都失败时返回失败

2. **Sequence (序列)**
   - 依次执行子节点
   - 当所有子节点都成功时返回成功
   - 当任一子节点失败时停止并返回失败

### 条件节点

- **Check Is Stunned Graph** - 检查眩晕状态
- **Check Is Pass Target Graph** - 检查是否为传球目标
- **Check Is Closest To Loose Ball Graph** - 检查是否最近
- **Check Is Team Controlling Ball Graph** - 检查团队控球
- **Check Has Ball Node Graph** - 检查是否持球
- **Check Can Shoot Graph** - 检查射门条件
- **Check Can Pass Graph** - 检查传球条件
- **Check Has Move Target Graph** - 检查移动目标
- **Check Can Tackle Graph** - 检查抢断条件
- **Check Has No Marked Player Graph** - 检查无盯防对象

### 动作节点

- **Task Stun Wait Graph** - 眩晕等待
- **Task Chase Ball Graph** - 追球
- **Task Move To Position Graph** - 移动到位置
- **Task Evaluate Offensive Options Graph** - 评估进攻选项
- **Task Shoot Graph** - 射门
- **Task Pass Ball Graph** - 传球
- **Task Calculate Support Spot Graph** - 计算支持位置
- **Task Evaluate Defensive State Graph** - 评估防守状态
- **Task Tackle Graph** - 抢断

---

## 决策优先级

整个AI的决策优先级如下：

1. **最高优先级**: 处理眩晕状态
   - 确保被击晕的球员不会执行任何动作

2. **次高优先级**: 作为传球目标
   - 当队友准备传球时，优先准备接球

3. **中等优先级**: 争夺自由球
   - 主动去抢夺无人控制的球

4. **防守优先级**: 组织防守
   - 评估防守状态，选择最合适的防守动作（抢断、追球、位置防守）

5. **最低优先级**: 进攻支持
   - 当球队控球时，根据是否持球选择射门、传球、带球或跑位支持

---

## 状态流转图

```
开始
  │
  ├─> 眩晕检查
  │     ├─ 是 -> 等待恢复 -> 结束
  │     └─ 否 ──┘
  │
  ├─> 传球目标检查
  │     ├─ 是 -> 追球接球 -> 结束
  │     └─ 否 ──┘
  │
  ├─> 自由球检查
  │     ├─ 是 -> 追球 -> 结束
  │     └─ 否 ──┘
  │
  ├─> 防守阶段
  │     ├─ 评估防守状态
  │     ├─ 抢断检查 -> 可抢断 -> 抢断 -> 结束
  │     ├─ 盯防检查 -> 无盯防 -> 追球 -> 结束
  │     └─ 位置防守 -> 移动到位置 -> 结束
  │
  └─> 进攻阶段
        ├─> 持球
        │     ├─ 评估进攻选项
        │     ├─ 可射门 -> 射门 -> 结束
        │     ├─ 可传球 -> 传球 -> 结束
        │     └─ 有目标 -> 带球 -> 结束
        │
        └─> 不持球
              ├─ 计算支持位置
              └─ 跑位支援 -> 结束
```

---

## 关键逻辑说明

### 1. 状态优先级
行为树通过优先级设计确保AI不会产生冲突行为。例如，当球员处于眩晕状态时，无论其他条件如何，都会先执行等待动作。

### 2. 防守策略
防守采用分层策略：
- 第一层：尝试直接抢断（如果条件允许）
- 第二层：追球防守（当没有直接盯防对象时）
- 第三层：区域防守（回到预定的防守位置）

### 3. 进攻决策
进攻决策基于实时评估：
- 优先考虑射门（最高收益）
- 其次考虑传球（保持控球）
- 最后选择带球或跑位支持（创造机会）

### 4. 团队协作
通过"传球目标"和"支持位置"计算，实现了队友之间的配合：
- 被标记为传球目标的球员会主动跑位
- 不持球的球员会计算最佳支持位置，提供传球选择

---

## 总结

该足球AI行为树实现了一个完整的足球比赛决策系统，涵盖了：

- **状态管理**: 眩晕等特殊状态的处理
- **角色分配**: 根据场上形势自动分配抢球、盯防、跑位等角色
- **进攻策略**: 射门、传球、带球的智能选择
- **防守策略**: 抢断、追球、区域防守的灵活切换
- **团队配合**: 通过传球目标和支持位置实现队友协作

整个系统通过优先级选择器确保决策的合理性，通过条件判断确保动作执行的时机正确，通过动作节点实现具体的足球技术动作。
