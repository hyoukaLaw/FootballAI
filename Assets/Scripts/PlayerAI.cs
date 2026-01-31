// PlayerAI.cs (挂在圆柱体上)

using System;
using System.Collections.Generic;
using BehaviorTree.Graph;
using UnityEngine;
using BehaviorTree.Runtime;
using Unity.VisualScripting; // 引用命名空间

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
        ExecutionPath = _tree.ExecutionPath;
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
    
    #region Debug
    // Debug
    // 调试用：在 Scene 窗口画出他想去哪
    void OnDrawGizmos()
    {
        if (_blackboard != null)
        {
            // // === 持球范围可视化 ===
            // if (_blackboard.MatchContext != null && 
            //     _blackboard.MatchContext.BallHolder == _blackboard.Owner)
            // {
            //     // 持球范围（淡黄色，1.0m）
            //     Gizmos.color = new Color(1f, 1f, 0.5f, 0.5f);
            //     Gizmos.DrawWireSphere(transform.position, 1.0f);
            //
            //     // 抢断范围（红色，1.6m）
            //     Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.5f);
            //     Gizmos.DrawWireSphere(transform.position, Stats.TackledDistance);
            // }

            // 画出移动目标
            // if (_blackboard.MoveTarget != Vector3.zero)
            // {
            //     Gizmos.color = Color.yellow;
            //     Gizmos.DrawLine(transform.position,
            //         transform.position + (_blackboard.MoveTarget - transform.position).normalized);
            //     Gizmos.DrawWireSphere(transform.position + (_blackboard.MoveTarget - transform.position).normalized, 0.3f);
            // }
            // // 画出传球目标连线
            // if (_blackboard.BestPassTarget != null)
            // {
            //     Gizmos.color = Color.green;
            //     Gizmos.DrawLine(transform.position, _blackboard.BestPassTarget.transform.position);
            //     Gizmos.DrawIcon(transform.position + Vector3.up * 2, "Pass");
            // }

            DrawCandidatePositions();
        }
     }

    private void DrawCandidatePositions()
    {
        if (!_blackboard.DebugShowCandidates || _blackboard.DebugCandidatePositions == null)
            return;

        if (_blackboard.DebugCandidatePositions.Count == 0)
            return;

        float minScore = float.MaxValue;
        float maxScore = float.MinValue;

        foreach (var candidate in _blackboard.DebugCandidatePositions)
        {
            if (candidate.Score < minScore)
                minScore = candidate.Score;
            if (candidate.Score > maxScore)
                maxScore = candidate.Score;
        }

        float scoreRange = maxScore - minScore;
        if (scoreRange < 0.0001f)
            scoreRange = 1f;

        foreach (var candidate in _blackboard.DebugCandidatePositions)
        {
            float normalizedScore = (candidate.Score - minScore) / scoreRange;
            float radius = 0.1f + normalizedScore * 0.4f;

            Color color;
            if (normalizedScore > 0.7f)
            {
                color = Color.green * new Vector4(1,1,1,0.5f);
            }
            else if (normalizedScore > 0.3f)
            {
                color = Color.yellow * new Vector4(1,1,1,0.5f);
            }
            else
            {
                color = Color.red * new Vector4(1,1,1,0.5f);
            }

            Gizmos.color = color;
            Gizmos.DrawWireSphere(candidate.Position, 0.5f);
        }
    }
    #endregion
}