using UnityEngine;

namespace BehaviorTree.Runtime
{
    public enum PlayerRoleType
    {
        Defender,
        Midfielder,
        Forward
    }

    public enum FieldZone
    {
        OwnDefensiveZone,
        OwnOffensiveZone,
        EnemyOffensiveZone,
        EnemyDefensiveZone
    }

    public enum MatchState
    {
        Attacking,
        Defending,
        ChasingBall
    }

    [System.Serializable]
    public class RolePreferences
    {
        [Header("区域权重")]
        [Tooltip("己方防守区权重（0.0-0.25）")]
        public float OwnDefensiveZoneWeight = 0.0f;

        [Tooltip("己方进攻区权重（0.25-0.5）")]
        public float OwnOffensiveZoneWeight = 0.0f;

        [Tooltip("敌方进攻区权重（0.5-0.75）")]
        public float EnemyOffensiveZoneWeight = 0.0f;

        [Tooltip("敌方防守区权重（0.75-1.0）")]
        public float EnemyDefensiveZoneWeight = 0.0f;

        [Header("距离衰减参数")]
        [Tooltip("距离理想区域越远，权重衰减越快")]
        public float DistanceDecayRate = 0.1f;

        [Tooltip("允许偏离理想区域的最大距离")]
        public float MaxZoneDeviation = 8f;
    }

    [CreateAssetMenu(fileName = "New Player Role", menuName = "Football/Player Role")]
    public class PlayerRole : ScriptableObject
    {
        [Header("角色信息")]
        public PlayerRoleType RoleType;
        public string RoleName;

        [Header("区域偏好")]
        public RolePreferences AttackPreferences;
        public RolePreferences DefendPreferences;
        public RolePreferences ChaseBallPreferences;

        [Header("行为倾向")]
        [Range(0f, 1f)]
        [Tooltip("进攻倾向")]
        public float OffensiveBias = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("防守倾向")]
        public float DefensiveBias = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("支持倾向")]
        public float SupportBias = 0.5f;

        [Header("活动范围")]
        [Tooltip("主要活动区域半径")]
        public float HomeZoneRadius = 5f;

        [Tooltip("最大漫游距离")]
        public float MaximumRoamingDistance = 15f;

        private void OnEnable()
        {
            if (AttackPreferences == null)
            {
                AttackPreferences = new RolePreferences();
            }

            if (DefendPreferences == null)
            {
                DefendPreferences = new RolePreferences();
            }

            if (ChaseBallPreferences == null)
            {
                ChaseBallPreferences = new RolePreferences();
            }
        }
    }
}
