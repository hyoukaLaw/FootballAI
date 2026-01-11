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
        public bool IsInStealCooldown;

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

        public bool IsPassTarget(GameObject player)
        {
            return IncomingPassTarget == player;
        }
    }
}
