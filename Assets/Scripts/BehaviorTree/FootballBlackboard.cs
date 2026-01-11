using UnityEngine;

namespace BehaviorTree
{
    public class FootballBlackboard
    {
        // --- 全局上下文引用 ---
        public MatchContext MatchContext;

        // --- 个人决策数据 (每个球员独有的状态) ---
        public GameObject Owner; // 黑板属于哪个球员
        public Vector3 MoveTarget; // 当前准备跑向的目标点
        public GameObject PassTarget;      // 评估后选出的最佳传球目标

        // --- 进攻决策数据 ---
        public GameObject BestPassTarget; // 计算出的最佳接球人
        public bool CanShoot;             // 是否满足射门条件

        // --- 防守决策数据 ---
        public GameObject MarkedPlayer;   // 我当前负责盯防的敌人
        public Vector3 DefensePosition;   // 计算出的防守站位点

        // --- 球员属性数据 (来自 PlayerAI) ---
        public PlayerStats Stats;

        // --- 状态效果 ---
        public bool IsStunned;        // 是否处于眩晕/停顿状态
        public float StunTimer = 0f;  // 停顿计时器
        public float StunDuration = 0.5f; // 停顿时长（秒）
    }
}