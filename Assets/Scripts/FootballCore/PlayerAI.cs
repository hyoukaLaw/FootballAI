// PlayerAI.cs (挂在圆柱体上)

using System;
using System.Collections.Generic;
using BehaviorTree.Graph;
using UnityEngine;
using BehaviorTree.Runtime;

namespace FootballAI.FootballCore
{
[System.Serializable]
public class PlayerStats
{
    [Header("移动属性")]
    [Range(2f, 10f)]
    public float MovementSpeed = 2.0f;
    public float SprintMultiplier = 1.2f;
    [Header("带球属性")]
    public float TackledDistance = 1.6f;
    [Header("传球属性")]
    public float PassingSpeed = 10f;
    [Range(0.3f, 1.0f)]
    public float ShootingAccuracy = 0.7f;
    [Header("防守属性")]
    public float DefensiveAwareness = 1.0f;
}

public class PlayerAI : MonoBehaviour
{
    [Header("AI Configuration")]
    public BTGraph AIBehaviorGraph; // 拖入 xNode 图表资源
    private FootballBlackboard _blackboard;
    private BehaviorTree.Runtime.BehaviorTree _tree; // 指明是由于我们自定义的类
    public Node CurrentNode;

    [Header("调试信息")]
    [TextArea(3, 10)]
    public string ExecutionPath = "None"; // 当前执行路径（返回 SUCCESS 或 RUNNING 的所有节点）

    [Header("初始位置")]
    public Vector3 InitialPosition; // 初始位置

    [Header("球员属性配置")]
    public PlayerStats Stats = new PlayerStats();

    [Header("角色配置")]
    public PlayerRole PlayerRole; // 球员角色（前锋、中场、后卫）

    private void OnValidate()
    {
        InitialPosition = transform.position;
    }

    void Awake()
    {
        // 1. 【源头】创建唯一的黑板实例
        _blackboard = new FootballBlackboard();
        _blackboard.Owner = this.gameObject; // 记录自己是谁
        _blackboard.Stats = Stats; // 传入球员属性
        _blackboard.Role = PlayerRole; // 传入角色配置
        _blackboard.DebugShowCandidates = RuntimeDebugSettings.EnableCandidateVisualization;
        if (AIBehaviorGraph != null)
        {
            BTGraphNode rootGraphNode = FindRootNode(AIBehaviorGraph);//没有 Input 连接的节点就是 Root
            if (rootGraphNode != null)
            {
                var runtimeRoot = rootGraphNode.CreateRuntimeNode(_blackboard);
                _tree = new BehaviorTree.Runtime.BehaviorTree(_blackboard);
                _tree.SetRoot(runtimeRoot);
            }
        }
    }
    // 简单的找根节点工具方法
    private BTGraphNode FindRootNode(BTGraph graph)
    {
        foreach (var node in graph.nodes)
        {
            var btNode = node as BTGraphNode;
            // 如果 Entry 端口没连线，它就是根
            if (btNode != null && !btNode.GetInputPort("Entry").IsConnected)
                return btNode;
        }
        return null;
    }

    /// <summary>
    /// 手动Tick方法，由MatchManager交错调用
    /// </summary>
    public void ManualTick()
    {
        _tree.Tick();
        UpdateExecutionPathDebugText();
    }

    private void UpdateExecutionPathDebugText()
    {
        ExecutionPath = RuntimeDebugSettings.ShouldTraceExecutionPath() ? _tree.ExecutionPath : "Disabled";
    }
    
    // === 新增：归位方法 ===
    public void ResetPosition()
    {
        transform.position = InitialPosition;
    }

    public void ResetBlackboard()
    {
        _blackboard.MoveTarget = Vector3.zero;
        _blackboard.BestPassTarget = null;
        _blackboard.CanShoot = false;
        _blackboard.MarkedPlayer = null;
        _blackboard.DefensePosition = Vector3.zero;
        _blackboard.IsStunned = false;
        _blackboard.StunTimer = 0f;
        _blackboard.DebugHasSelectedPosition = false;
        _blackboard.DebugSelectedPosition = Vector3.zero;
    }

    public void ResetBehaviorTree()
    {
        if (_tree == null) return;

        // 获取根节点并重置所有节点状态
        Node root = _tree.GetRootNode();
        if (root != null)
        {
            root.Reset();
        }

        // 清空执行路径
        ExecutionPath = "None";
    }
    
    // === 新增：给 MatchManager 用的接口 ===
    public FootballBlackboard GetBlackboard()
    {
        return _blackboard;
    }
    
#if UNITY_EDITOR
    #region Debug
    void OnDrawGizmos()
    {
        if (_blackboard == null)
            return;
        DrawCandidatePositions();
    }

    private void DrawCandidatePositions()
    {
        if (!_blackboard.DebugShowCandidates)
            return;
        if (!_blackboard.DebugHasSelectedPosition)
            return;
        Gizmos.color = Color.green * new Vector4(1, 1, 1, 0.5f);
        Gizmos.DrawWireSphere(_blackboard.DebugSelectedPosition, 0.5f);
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawSphere(_blackboard.DebugSelectedPosition, 0.3f);
    }
    #endregion
#endif
}
}
