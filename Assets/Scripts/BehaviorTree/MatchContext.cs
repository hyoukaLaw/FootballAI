using System.Collections.Generic;
using UnityEngine;

namespace BehaviorTree
{
    public class MatchContext
    {
        // --- 全局共享数据 ---
        public GameObject Ball;
        public GameObject BallHolder;
        public List<GameObject> TeamRedPlayers;
        public List<GameObject> TeamBluePlayers;
        public Transform RedGoal;
        public Transform BlueGoal;
        public GameObject IncomingPassTarget;

        // --- 传球状态管理 ---
        private float _passTimer = 0f; // 传球计时器

        // --- 抢断保护期状态 ---
        private float _stealCooldownTimer = 0f; // 抢断保护期计时器
        public bool IsInStealCooldown { get { return _stealCooldownTimer > 0f; } }

        // --- 内部方法：用于 MatchManager 更新保护期 ---
        public void SetStealCooldown(float duration)
        {
            _stealCooldownTimer = duration;
        }

        public void UpdateStealCooldown(float deltaTime)
        {
            if (_stealCooldownTimer > 0f)
                _stealCooldownTimer -= deltaTime;
        }

        // --- 内部方法：用于 MatchManager 更新传球状态 ---
        public void SetPassTarget(GameObject target)
        {
            IncomingPassTarget = target;
            _passTimer = 0f; // 重置计时器
        }

        public void UpdatePassTarget(float passTimeout, GameObject ballHolder)
        {
            if (IncomingPassTarget != null)
            {
                _passTimer += Time.deltaTime;

                // 超时或球已被接住，清除传球目标
                if (_passTimer > passTimeout || ballHolder != null)
                {
                    IncomingPassTarget = null;
                    _passTimer = 0f;
                }
            }
        }

        // --- 辅助方法：根据球员身份获取上下文数据 ---
        public List<GameObject> GetTeammates(GameObject player)
        {
            if (TeamRedPlayers.Contains(player))
                return TeamRedPlayers;
            else if (TeamBluePlayers.Contains(player))
                return TeamBluePlayers;
            return null;
        }

        public List<GameObject> GetOpponents(GameObject player)
        {
            if (TeamRedPlayers.Contains(player))
                return TeamBluePlayers;
            else if (TeamBluePlayers.Contains(player))
                return TeamRedPlayers;
            return null;
        }

        public Vector3 GetEnemyGoalPosition(GameObject player)
        {
            if (TeamRedPlayers.Contains(player))
                return BlueGoal.position;
            else
                return RedGoal.position;
        }

        public Vector3 GetMyGoalPosition(GameObject player)
        {
            if (TeamRedPlayers.Contains(player))
                return RedGoal.position;
            else
                return BlueGoal.position;
        }

        public bool IsPassTarget(GameObject player)
        {
            return IncomingPassTarget == player;
        }

        public void Reset()
        {
            IncomingPassTarget = null;
            _stealCooldownTimer = 0f;
            BallHolder = null;
            _passTimer = 0f;
        }
        
        public static float MoveSegment = 0.1f;
    }
}
