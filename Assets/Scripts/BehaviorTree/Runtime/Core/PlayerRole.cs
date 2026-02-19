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
        [Tooltip("进攻状态权重。参与总分公式：Total=zone+ball+goal+mark+space+pressing+support-safety（见 RoleBasedPositionScoreCalculator.CalculateContextAwareScoreCommon）")]
        public PositionWeight AttackPositionWeight;
        [Tooltip("防守/追球状态权重。参与总分公式：Total=zone+ball+goal+mark+space+pressing+support-safety（见 RoleBasedPositionScoreCalculator.CalculateContextAwareScoreCommon）")]
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
            [Tooltip("对应 BallScore。算法：Clamp01(1-(distanceToBall/15)^2)，最终贡献=BallScore*WeightBallDist")]
            public float WeightBallDist = 10f;
            [Tooltip("对应 GoalScore。算法：Clamp01(1-distanceToGoal/10)，最终贡献=GoalScore*WeightGoalDist")]
            public float WeightGoalDist = 10f;
            [Tooltip("对应 MarkScore。基于敌我距离、危险系数与封堵传球线等综合计算，最终贡献=MarkScore*WeightMarking")]
            public float WeightMarking = 10f;
            [Tooltip("对应 SpaceScore。算法：取最近敌人距离并做 Clamp01(closestDist/5)，最终贡献=SpaceScore*WeightSpace")]
            public float WeightSpace = 10f;
            [Tooltip("对应 SafetyScore。算法：与队友距离过近产生惩罚，注意在总分中是减项：-SafetyScore*WeightSafety")]
            public float WeightSafety = 10f;
            [Tooltip("对应 PressingScore。仅最近防守者生效，结合压迫角度与距离计算，最终贡献=PressingScore*WeightPressing")]
            public float WeightPressing = 10f;
            [Tooltip("对应 SupportScore。以持球人为参考，偏好 3~10m 且接近理想距离 6m 的接应点，最终贡献=SupportScore*WeightSupport")]
            public float WeightSupport = 10f;
        }
    }


}
