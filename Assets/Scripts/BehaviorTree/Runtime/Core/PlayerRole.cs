using System;
using UnityEngine;
using FootballAI.FootballCore;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace BehaviorTree.Runtime
{
    public enum PlayerRoleType
    {
        Defender,
        Midfielder,
        Forward
    }

    public enum MatchState
    {
        Attacking,
        Defending,
        ChasingBall
    }

    [System.Serializable]
    public class ZoneWeightEntry
    {
        [ValueDropdown(nameof(GetZoneIdOptions))]
        [ValidateInput(nameof(IsZoneIdValid), "ZoneId not found in Resources/Config/FormationLayout", InfoMessageType.Warning)]
        public string ZoneId = "zone_01";
        public float Weight = 0f;

        private static IEnumerable<ValueDropdownItem<string>> GetZoneIdOptions()
        {
            FormationLayout layout = Resources.Load<FormationLayout>("Config/FormationLayout");
            if (layout != null && layout.Zones != null && layout.Zones.Count > 0)
            {
                for (int i = 0; i < layout.Zones.Count; i++)
                {
                    FormationZoneRect zone = layout.Zones[i];
                    if (zone == null || string.IsNullOrEmpty(zone.ZoneId))
                        continue;
                    string label = string.IsNullOrEmpty(zone.DisplayName) ? zone.ZoneId : $"{zone.DisplayName} ({zone.ZoneId})";
                    yield return new ValueDropdownItem<string>(label, zone.ZoneId);
                }
            }
        }

        private static bool IsZoneIdValid(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId))
                return false;
            FormationLayout layout = Resources.Load<FormationLayout>("Config/FormationLayout");
            if (layout == null || layout.Zones == null || layout.Zones.Count == 0)
                return true;
            for (int i = 0; i < layout.Zones.Count; i++)
            {
                FormationZoneRect zone = layout.Zones[i];
                if (zone == null)
                    continue;
                if (zone.ZoneId == zoneId)
                    return true;
            }
            return false;
        }
    }

    [System.Serializable]
    public class RolePreferences
    {
        [Header("区域权重")]
        public List<ZoneWeightEntry> ZoneWeights = new List<ZoneWeightEntry>();

        public float GetZoneWeightById(string zoneId)
        {
            for (int i = 0; i < ZoneWeights.Count; i++)
            {
                ZoneWeightEntry entry = ZoneWeights[i];
                if (entry.ZoneId == zoneId)
                    return entry.Weight;
            }
            return 0f;
        }

        public (string zoneId, float weight) GetHighestZoneWeight()
        {
            string bestZoneId = string.Empty;
            float bestWeight = float.MinValue;
            for (int i = 0; i < ZoneWeights.Count; i++)
            {
                ZoneWeightEntry entry = ZoneWeights[i];
                if (entry.Weight > bestWeight)
                {
                    bestWeight = entry.Weight;
                    bestZoneId = entry.ZoneId;
                }
            }
            if (Math.Abs(bestWeight - float.MinValue) < FootballConstants.FloatEpsilon)
                return (string.Empty, 0f);
            return (bestZoneId, bestWeight);
        }
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
        
        [Header("位置计算权重")]
        public PositionWeight AttackPositionWeight;
        public PositionWeight DefendPositionWeight;

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
        [System.Serializable]
        public class PositionWeight
        {
            public float WeightBallDist = 10f;
            public float WeightGoalDist = 10f;
            public float WeightMarking = 10f;
            public float WeightSpace = 10f;
            public float WeightSafety = 10f;
            public float WeightPressing = 10f;
            public float WeightSupport = 10f;
        }
    }


}
