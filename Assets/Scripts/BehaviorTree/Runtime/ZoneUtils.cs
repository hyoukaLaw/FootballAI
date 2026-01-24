using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace BehaviorTree.Runtime
{
    public static class ZoneUtils
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
        
        // ZoneStart 数组已被移除，改用 GetZoneStartProgress 和 GetZoneEndProgress 方法
        // 这样可以避免硬编码数组，提供更灵活的区域计算

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
            // 计算从己方球门到敌方球门的方向和长度
            Vector3 fieldDirection = (enemyGoal - myGoal).normalized;
            float fieldLength = MatchManager.Instance.Context.GetFieldLength();
            // 获取区域的起始和结束进度
            float startProgress = GetZoneStartProgress(zone);
            float endProgress = GetZoneEndProgress(zone);
            // 计算区域的起始和结束位置
            Vector3 startPos = myGoal + (fieldDirection.z > 0 ? fieldDirection * (fieldLength * startProgress) : fieldDirection * (fieldLength * endProgress));
            Vector3 endPos = myGoal + (fieldDirection.z > 0 ? fieldDirection * (fieldLength * endProgress) : fieldDirection * (fieldLength * startProgress));
            float halfWidth = MatchManager.Instance.Context.GetFieldWidth() / 2f;
            // 计算区域的左下角位置
            Vector3 leftBottom = startPos - Vector3.right * halfWidth;
            float width = MatchManager.Instance.Context.GetFieldWidth();
            float length = Vector3.Distance(startPos, endPos);
            return new ZoneRange{LeftBottom = leftBottom, Width = width, Length = length};
        }

        /// <summary>
        /// 获取区域的起始进度
        /// </summary>
        private static float GetZoneStartProgress(FieldZone zone)
        {
            switch(zone)
            {
                case FieldZone.OwnDefensiveZone: return 0f;
                case FieldZone.OwnOffensiveZone: return 0.25f;
                case FieldZone.EnemyOffensiveZone: return 0.5f;
                case FieldZone.EnemyDefensiveZone: return 0.75f;
                default: return 0f;
            }
        }

        /// <summary>
        /// 获取区域的结束进度
        /// </summary>
        private static float GetZoneEndProgress(FieldZone zone)
        {
            switch(zone)
            {
                case FieldZone.OwnDefensiveZone: return 0.25f;
                case FieldZone.OwnOffensiveZone: return 0.5f;
                case FieldZone.EnemyOffensiveZone: return 0.75f;
                case FieldZone.EnemyDefensiveZone: return 1f;
                default: return 0.25f;
            }
        }
        
        public static bool IsInZone(Vector3 position, FieldZone zone, Vector3 enemyGoal, Vector3 myGoal)
        {
            ZoneRange range = GetZoneRange(zone, enemyGoal, myGoal);
            if(position.x >= range.LeftBottom.x && position.x <= range.LeftBottom.x + range.Width && 
                position.z >= range.LeftBottom.z && position.z <= range.LeftBottom.z + range.Length) 
                return true;
            return false;
        }
        
        /// <summary>
        /// 检查位置是否在指定球门的罚区内
        /// </summary>
        /// <param name="position">要检查的位置</param>
        /// <param name="goalPosition">球门位置（用于确定是哪个罚区）</param>
        /// <returns>是否在罚区内</returns>
        public static bool IsInPenaltyArea(Vector3 position, Vector3 goalPosition)
        {
            float penaltyAreaDepth = MatchManager.Instance.Context.GetFieldLength() * _penaltyAreaProgresses;
            float penaltyAreaWidth = MatchManager.Instance.Context.GetFieldWidth() * _penaltyAreaWidthNormalized;
            if (goalPosition.z < 0)
            {
                Vector3 leftBottom = new Vector3(goalPosition.x - penaltyAreaWidth / 2f, 0,
                    MatchManager.Instance.Context.GetBackwardBorder());
                ZoneRange zoneRange = new ZoneRange(){LeftBottom = leftBottom, Width = penaltyAreaWidth, Length = penaltyAreaDepth};
                return position.x >= zoneRange.LeftBottom.x && position.x <= zoneRange.LeftBottom.x + zoneRange.Width &&
                    position.z >= zoneRange.LeftBottom.z && position.z <= zoneRange.LeftBottom.z + zoneRange.Length;
            }
            else
            {
                Vector3 leftBottom = new Vector3(goalPosition.x - penaltyAreaWidth / 2f, 0,
                    MatchManager.Instance.Context.GetForwardBorder() - penaltyAreaDepth);
                ZoneRange zoneRange = new ZoneRange(){LeftBottom = leftBottom, Width = penaltyAreaWidth, Length = penaltyAreaDepth};
                return position.x >= zoneRange.LeftBottom.x && position.x <= zoneRange.LeftBottom.x + zoneRange.Width &&
                       position.z >= zoneRange.LeftBottom.z && position.z <= zoneRange.LeftBottom.z + zoneRange.Length;
            }
        }
    }
}
