using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace BehaviorTree.Runtime
{
    public static class ZoneProbabilitySystem
    {
        private static readonly float[] ZoneProgresses = new float[]
        {
            0.125f,
            0.375f,
            0.625f,
            0.875f
        };

        private static float _penaltyAreaProgresses = 0.2f;
        private static float _penaltyAreaWidthNormalized = 0.7f;
        
        private static readonly float[] ZoneStart = new float[]{0f, 0.25f, 0.5f, 0.75f};

        // 根据位置计算所在的区域
        public static FieldZone GetFieldZone(Vector3 position, Vector3 myGoal, Vector3 enemyGoal)
        {
            float distToMyGoal = Vector3.Distance(position, myGoal);
            float distToEnemyGoal = Vector3.Distance(position, enemyGoal);
            float totalDist = distToMyGoal + distToEnemyGoal;

            if (totalDist == 0) return FieldZone.OwnOffensiveZone;
            float normalizedDist = distToMyGoal / totalDist;
            if (normalizedDist < 0.25f) return FieldZone.OwnDefensiveZone;
            else if (normalizedDist < 0.5f) return FieldZone.OwnOffensiveZone;
            else if (normalizedDist < 0.75f) return FieldZone.EnemyOffensiveZone;
            else return FieldZone.EnemyDefensiveZone;
        }
        
        public static float CalculateZonePreferenceScoreV2(Vector3 position, PlayerRole role, Vector3 myGoal, Vector3 enemyGoal, MatchState currentState)
        {
            RolePreferences preferences = GetPreferencesForState(role, currentState);
            FieldZone zone = GetFieldZone(position, myGoal, enemyGoal);
            float baseWeight = GetZoneWeight(zone, preferences);
            return baseWeight;
        }

        public static float CalculateNormalizedZoneScore(Vector3 position, PlayerRole role, Vector3 myGoal,
            Vector3 enemyGoal, MatchState currentState)
        {
            RolePreferences preferences = GetPreferencesForState(role, currentState);
            return CalculateZonePreferenceScoreV2(position, role, myGoal, enemyGoal, currentState)/FindHighestWeightZoneAndWeight(preferences).weight;
        }

        public static (FieldZone zone, float weight) FindHighestWeightZoneAndWeight(RolePreferences preferences)
        {
            float[] weights = new float[]
            {
                preferences.OwnDefensiveZoneWeight,
                preferences.OwnOffensiveZoneWeight,
                preferences.EnemyOffensiveZoneWeight,
                preferences.EnemyDefensiveZoneWeight
            };
            float maxWeight = 0f;
            int maxIndex = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] > maxWeight)
                {
                    maxWeight = weights[i];
                    maxIndex = i;
                }
            }
            return (zone: (FieldZone)maxIndex, weight: maxWeight);
        }

        private static Vector3 CalculateZoneCenter(FieldZone zone, Vector3 myGoal, Vector3 enemyGoal)
        {
            Vector3 direction = (enemyGoal - myGoal).normalized;
            float fieldLength = Vector3.Distance(myGoal, enemyGoal);
            float progress = GetZoneProgress(zone);
            Vector3 center = myGoal + direction * (fieldLength * progress);
            center.y = myGoal.y;
            return center;
        }

        private static float GetZoneProgress(FieldZone zone)
        {
            int index = (int)zone;
            if (index >= 0 && index < ZoneProgresses.Length)
            {
                return ZoneProgresses[index];
            }
            return 0.5f;
        }

        private static float GetZoneWeight(FieldZone zone, RolePreferences preferences)
        {
            switch (zone)
            {
                case FieldZone.OwnDefensiveZone: return preferences.OwnDefensiveZoneWeight;
                case FieldZone.OwnOffensiveZone: return preferences.OwnOffensiveZoneWeight;
                case FieldZone.EnemyOffensiveZone: return preferences.EnemyOffensiveZoneWeight;
                case FieldZone.EnemyDefensiveZone: return preferences.EnemyDefensiveZoneWeight;
                default: return 0f;
            }
        }
        
        private static RolePreferences GetPreferencesForState(PlayerRole role, MatchState state)
        {
            switch (state)
            {
                case MatchState.Attacking: return role.AttackPreferences;
                case MatchState.Defending: return role.DefendPreferences;
                case MatchState.ChasingBall: return role.ChaseBallPreferences;
                default: return role.AttackPreferences;
            }
        }
        
        public class ZoneRange
        {
            public Vector3 LeftBottom;
            public float Width;
            public float Length;
        }

        public static ZoneRange GetZoneRange(FieldZone zone, Vector3 enemyGoal, Vector3 myGoal)
        {
            float start = ZoneStart[(int)zone];
            float next = (int)zone + 1 < ZoneStart.Length? ZoneStart[(int)zone + 1]: 1f;

            Vector3 leftBottom = new Vector3(MatchManager.Instance.Context.GetLeftBorder(), 0f,
                MatchManager.Instance.Context.GetBackwardBorder());
            float width = MatchManager.Instance.Context.GetFieldWidth();
            float Length = MatchManager.Instance.Context.GetFieldLength() * (next - start);
            return new ZoneRange{LeftBottom = leftBottom, Width = width, Length = Length};
        }
        
        public static bool IsInZone(Vector3 position, FieldZone zone, Vector3 enemyGoal, Vector3 myGoal)
        {
            ZoneRange range = GetZoneRange(zone, enemyGoal, myGoal);
            if(position.x >= range.LeftBottom.x && position.x <= range.LeftBottom.x + range.Width && 
                position.z >= range.LeftBottom.z && position.z <= range.LeftBottom.z + range.Length) 
                return true;
            return false;
        }
        
        public static bool IsInPenaltyArea(Vector3 position)
        {
            float length = MatchManager.Instance.Context.GetFieldLength() * _penaltyAreaProgresses;
            float width = MatchManager.Instance.Context.GetFieldWidth() * _penaltyAreaWidthNormalized;
            Vector3 leftBottom = new Vector3(MatchManager.Instance.Context.GetLeftBorder(), 0f,
                MatchManager.Instance.Context.GetBackwardBorder()); // 球场的左下角
            leftBottom += Vector3.right * (MatchManager.Instance.Context.GetFieldWidth()-width)/2f;
            Debug.Log($"penalty area: ({leftBottom}, {width}, {length})");
            if(position.x >= leftBottom.x && position.x <= leftBottom.x + width && 
                position.z >= leftBottom.z && position.z <= leftBottom.z + length) 
                return true;
            return false;
        }
    }
}
