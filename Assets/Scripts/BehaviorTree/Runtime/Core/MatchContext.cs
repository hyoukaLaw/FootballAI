using System.Collections.Generic;
using UnityEngine;

namespace BehaviorTree.Runtime
{
    public class MatchContext
    {
        // --- 全局共享数据 ---
        public GameObject Ball;
        public BallController BallController;
        public GameObject BallHolder;
        public GameObject GetBallHolder()
        {
            return BallHolder;
        }
        public void SetBallHolder(GameObject holder)
        {
            BallHolder = holder;
        }
        public List<GameObject> TeamRedPlayers;
        public List<GameObject> TeamBluePlayers;
        public Transform RedGoal;
        public Transform BlueGoal;
        public GameObject IncomingPassTarget;
        public GameObject Field;

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
                _passTimer += TimeManager.Instance.GetDeltaTime();

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

        // 我方要进攻的球门位置
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
        

        public float GetLeftBorder()
        {
            return Field.transform.position.x - 10f / 2 * Field.transform.lossyScale.x; // 10f plane默认宽度
        }
        
        public float GetRightBorder()
        {
            return Field.transform.position.x + 10f / 2 * Field.transform.lossyScale.x; // 10f plane默认宽度
        }
        
        public float GetForwardBorder()
        {
            return Field.transform.position.z + 10f / 2 * Field.transform.lossyScale.z; // 10f plane默认高度
        }
        
        public float GetBackwardBorder()
        {
            return Field.transform.position.z - 10f / 2 * Field.transform.lossyScale.z; // 10f plane默认高度
        }

        public bool IsInField(Vector3 pos)
        {
            return pos.x >= GetLeftBorder() && pos.x <= GetRightBorder() && pos.z >= GetBackwardBorder() && pos.z <= GetForwardBorder();
        }

        public float GetFieldWidth()
        {
            return GetRightBorder() - GetLeftBorder();
        }
        
        public float GetFieldLength()
        {
            return GetForwardBorder() - GetBackwardBorder();
        }
        
        public static float MoveSegment = 0.1f;
    }
}
