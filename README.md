# FootballAI

一个基于 Unity 的足球 AI 对抗 Demo（当前为 5v5场景）。

## 项目简介
- 使用行为树驱动球员决策，覆盖持球与无球两类核心行为。行为树核心模块手写，编辑部分基于XNode实现。
- 无球跑位基于角色权重与位置评分（阵型、盯防、压迫、与球关系等）。
- 持球决策支持传球、带球、解围、射门等动作评估。
- 已将比赛主流程拆分为多个系统模块，便于调试与持续重构。
- 利用OpenCode+GLM4.7/GPT 5.3 辅助完成

## 核心模块
- `Assets/Scripts/FootballCore/MatchManager.cs`：比赛编排入口（Orchestrator）。
- `Assets/Scripts/FootballCore/Systems/MatchFlowSystem.cs`：比赛流程（暂停/恢复/开新局）。
- `Assets/Scripts/FootballCore/Systems/MatchStatsSystem.cs`：比分与统计。
- `Assets/Scripts/FootballCore/Systems/PossessionRefereeSystem.cs`：球权与裁判逻辑。
- `Assets/Scripts/BehaviorTree`：行为树运行时与图资源。



