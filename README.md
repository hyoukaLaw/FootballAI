# FootballAI

一个基于 Unity 的足球 AI 对抗 Demo（当前为 5v5 场景）。

## 项目简介
- 使用行为树驱动球员决策，覆盖持球与无球两类核心行为。
- 无球跑位基于角色权重与位置评分（阵型、盯防、压迫、与球关系等）。
- 持球决策支持传球、带球、解围、射门等动作评估，通过效用评分确定具体执行哪一项动作。
- 比赛主流程已拆分为编排器 + 领域系统，便于调试、扩展与重构。
- OpenCode + GLM4.7/GPT5.3 CodeX 辅助

## 我具体做了什么
- 设计并实现足球 AI 决策主干：行为树运行时（Selector/Sequence/条件/任务）与 XNode 图编辑映射。
- 设计并实现位置评分与角色化策略：后卫/中场/前锋在攻防状态下使用不同权重与动作评估。
- 重构：将比赛流程、球权裁判、统计逻辑从 `MatchManager` 下沉为独立系统。
- 持续进行问题定位与性能治理：排查抢断-眩晕中断链路、减少热路径分配、统一调试开关与日志策略。
- 搭建可复盘的调参闭环：通过多场对局结果观察策略变更的真实收益。

## 代码结构（当前）
- `Assets/Scripts/FootballCore/Bootstrap/MatchManager.cs`：比赛编排入口（Orchestrator）。
- `Assets/Scripts/FootballCore/MatchLogic/Systems/MatchFlowSystem.cs`：比赛流程（暂停/恢复/开新局）。
- `Assets/Scripts/FootballCore/MatchLogic/Systems/MatchStatsSystem.cs`：比分与统计。
- `Assets/Scripts/FootballCore/MatchLogic/Systems/PossessionRefereeSystem.cs`：球权与裁判逻辑。
- `Assets/Scripts/BehaviorTree`：行为树运行时、图节点与编辑工具。

## 运行方式
1. 使用 Unity 打开项目。
2. 打开 `Assets/Scenes/SampleScene.unity`。
3. 进入 Play 模式观察 5v5 对抗。

## 说明
- AI 工具用于辅助重构与方案推演，最终代码设计、实现与调参由本人完成。



