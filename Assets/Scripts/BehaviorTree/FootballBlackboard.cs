using System.Collections.Generic;
using UnityEngine;

namespace BehaviorTree
{
    public class FootballBlackboard
    {
        // --- 1. 引用数据 (所有球员共享的感知) ---
        public GameObject Ball;
        public GameObject BallHolder; // 当前谁拿着球
        public GameObject Owner; // 黑板属于哪个球员
        public List<GameObject> Teammates = new List<GameObject>();
        public List<GameObject> Opponents = new List<GameObject>();

        // --- 2. 个人决策数据 (每个球员独有的状态) ---
        public Vector3 MoveTarget; // 当前准备跑向的目标点
        public GameObject PassTarget;      // 评估后选出的最佳传球目标
        
        // --- 新增：进攻决策数据 ---
        public Vector3 EnemyGoalPosition; // 对方球门位置 (由 MatchManager 设置)
        public GameObject BestPassTarget; // 计算出的最佳接球人
        public bool CanShoot;             // 是否满足射门条件

        // --- 新增：防守决策数据 ---
        public GameObject MarkedPlayer;   // 我当前负责盯防的敌人
        public Vector3 DefensePosition;   // 计算出的防守站位点

        // --- 3. 球员属性数据 (来自 PlayerAI) ---
        public PlayerStats Stats;
    }
}