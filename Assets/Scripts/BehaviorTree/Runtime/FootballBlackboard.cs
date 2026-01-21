using System.Collections.Generic;
using UnityEngine;

namespace BehaviorTree.Runtime
{
    [System.Serializable]
    public class CandidatePosition
    {
        public Vector3 Position;
        public float RoleScore;
        public float AvoidOverlapScore;
        public float TotalScore;

        public CandidatePosition(Vector3 position, float roleScore, float avoidOverlapScore)
        {
            Position = position;
            RoleScore = roleScore;
            AvoidOverlapScore = avoidOverlapScore;
            TotalScore = roleScore + avoidOverlapScore;
        }
    }

    public class FootballBlackboard
    {
        // --- 全局上下文引用 ---
        public MatchContext MatchContext;

        // --- 个人决策数据 (每个球员独有的状态) ---
        public GameObject Owner; // 黑板属于哪个球员
        public Vector3 MoveTarget; // 当前准备跑向的目标点

        // --- 进攻决策数据 ---
        public GameObject BestPassTarget; // 计算出的最佳接球人
        public bool CanShoot;             // 是否满足射门条件

        // --- 防守决策数据 ---
        public GameObject MarkedPlayer;   // 我当前负责盯防的敌人
        public Vector3 DefensePosition;   // 计算出的防守站位点

        // --- 球员属性数据 (来自 PlayerAI) ---
        public PlayerStats Stats;

        // --- 角色系统 ---
        public PlayerRole Role;  // 球员角色配置

        // --- 状态效果 ---
        public bool IsStunned;        // 是否处于眩晕/停顿状态
        public float StunTimer = 0f;  // 停顿计时器
        public float StunDuration = 1f; // 停顿时长（秒）

        // --- 调试数据 ---
        public List<CandidatePosition> DebugCandidatePositions;
        public bool DebugShowCandidates = true;

    }

    public static class BlackboardUtils
    {
        public static void StartStun(FootballBlackboard bb, float stunDuration = 1f)
        {
            bb.IsStunned = true;
            bb.StunTimer = stunDuration;
        }
    }
}