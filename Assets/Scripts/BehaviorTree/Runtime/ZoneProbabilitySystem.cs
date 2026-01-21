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

        // 对某个位置，计算其区域得分。先找到区域中心，计算该位置与区域中心的距离（越小分数越高）
        public static float CalculateZonePreferenceScore(Vector3 position, PlayerRole role, Vector3 myGoal,
            Vector3 enemyGoal, MatchState currentState)
        {
            RolePreferences preferences = GetPreferencesForState(role, currentState);
            FieldZone zone = GetFieldZone(position, myGoal, enemyGoal);
            float baseWeight = GetZoneWeight(zone, preferences);
            Vector3 idealPosition = CalculateIdealPosition(role, currentState, myGoal, enemyGoal);
            float distanceToIdeal = Vector3.Distance(position, idealPosition);
            float distanceDecay = Mathf.Exp(-distanceToIdeal * preferences.DistanceDecayRate);// e^(-distance * DecayRate) = distanceDecay
            Debug.Log($"idealPosition For {role} in {zone} is {idealPosition}, distanceToIdeal is {distanceToIdeal}, distanceDecay is {distanceDecay}");
            if (distanceToIdeal > preferences.MaxZoneDeviation)
            {
                distanceDecay *= 0.2f;
            }
            return baseWeight * distanceDecay;
        }

        public static Vector3 CalculateIdealPosition(PlayerRole role, MatchState state,
            Vector3 myGoal, Vector3 enemyGoal)
        {
            RolePreferences preferences = GetPreferencesForState(role, state);

            FieldZone targetZone = FindHighestWeightZone(preferences);

            return CalculateZoneCenter(targetZone, myGoal, enemyGoal);
        }

        private static FieldZone FindHighestWeightZone(RolePreferences preferences)
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
            return (FieldZone)maxIndex;
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
    }
}
