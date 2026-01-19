using UnityEngine;
using System.Collections.Generic;

namespace BehaviorTree.Runtime
{
    public static class TeamPositionUtils
    {
        // 检查目标位置是否被队友占据
        public static bool IsPositionOccupiedByTeammates(GameObject owner, Vector3 targetPos,  List<GameObject> teammates, 
            List<GameObject> enemies, float minDistance = 1f)
        {
            foreach (var teammate in teammates)
            {
                if (teammate == owner) continue;

                float dist = Vector3.Distance(teammate.transform.position, targetPos);
                if (dist < minDistance)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsPositionOccupiedByEnemy(GameObject owner, Vector3 targetPos,
            List<GameObject> enemies, float minDistance = 0.5f)
        {
            foreach(var enemy in enemies)
            {
                float dist = Vector3.Distance(enemy.transform.position, targetPos);
                if (dist < minDistance)
                {
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="desiredPosition"></param>
        /// <param name="teammates"></param>
        /// <param name="enemies"></param>
        /// <param name="minDistance">判断位置被占据的最小距离</param>
        /// <returns></returns>
        // 找到不重叠的最佳位置
        public static Vector3 FindUnoccupiedPosition(GameObject owner, Vector3 desiredPosition,
            List<GameObject> teammates, List<GameObject> enemies, float minDistance = 1f)
        {
            var searchRadius = FootballConstants.OccupiedSearchRadius;
            // 1. 先检查理想位置是否可用
            bool occupied = IsPositionOccupiedByTeammates(owner, desiredPosition, teammates, enemies,  minDistance) || 
                            IsPositionOccupiedByEnemy(owner, desiredPosition, enemies, minDistance);

            if (!occupied)
            {
                return desiredPosition;
            }

            // 2. 理想位置不可用，搜索周围可用位置
            Vector3 bestPosition = desiredPosition;
            float bestScore = float.MinValue;

            // 12个方向搜索（每30度）
            for (int i = 0; i < 12; i++)
            {
                float angle = i * 30f;
                Vector3 searchDir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                Vector3 testPos = desiredPosition + searchDir * searchRadius;

                // 评估这个位置
                float score = EvaluatePosition(owner, testPos, desiredPosition, teammates, enemies, minDistance);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPosition = testPos;
                }
            }
            Debug.Log($"desiredPosition: {desiredPosition}, bestPosition: {bestPosition}, bestScore: {bestScore}");
            return bestPosition;
        }

        // 评估位置得分
        private static float EvaluatePosition(GameObject owner, Vector3 testPos, Vector3 desiredPosition,
            List<GameObject> teammates, List<GameObject> enemies, float minDistance)
        {
            // 因素1：与理想位置的接近程度（越近越好）
            float distanceToIdeal = Vector3.Distance(testPos, desiredPosition);
            float proximityScore = Mathf.Max(0, 10f - distanceToIdeal);

            // 因素2：与队友的重叠惩罚（重叠越少越好）
            float overlapPenalty = 0f;

            // 检查是否被队友占据
            foreach (var teammate in teammates)
            {
                if (teammate == owner) continue;
                float dist = Vector3.Distance(teammate.transform.position, testPos);
                if (dist < minDistance)
                {
                    overlapPenalty += (minDistance - dist) * 100f;
                }
            }
            
            // 检查是否被敌人占据，但惩罚要轻一些（支持和敌人卡位）
            foreach (var enemy in enemies)
            {
                float dist = Vector3.Distance(enemy.transform.position, testPos);
                if (dist < minDistance)
                {
                    overlapPenalty += (minDistance - dist) * 50f;
                }
            }
            
            return proximityScore - overlapPenalty;
        }
    }
}
