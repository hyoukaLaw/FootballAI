using UnityEngine;
using System.Collections.Generic;

namespace BehaviorTree
{
    public static class TeamPositionUtils
    {
        // 检查目标位置是否被队友占据
        public static bool IsPositionOccupiedByTeammate(
            GameObject owner,
            Vector3 targetPos,
            List<GameObject> teammates,
            float minDistance = 3f)
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

        // 检查目标位置是否与队友的目标冲突
        public static bool IsTargetPositionConflicted(
            GameObject owner,
            Vector3 targetPos,
            List<GameObject> teammates,
            float minDistance =3f)
        {
            foreach (var teammate in teammates)
            {
                if (teammate == owner) continue;

                var teammateAI = teammate.GetComponent<PlayerAI>();
                if (teammateAI != null && teammateAI.GetBlackboard() != null)
                {
                    Vector3 teammateTarget = teammateAI.GetBlackboard().MoveTarget;
                    if (teammateTarget != Vector3.zero)
                    {
                        float dist = Vector3.Distance(teammateTarget, targetPos);
                        if (dist < minDistance)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        // 找到不重叠的最佳位置
        public static Vector3 FindUnoccupiedPosition(
            GameObject owner,
            Vector3 desiredPosition,
            List<GameObject> teammates,
            float searchRadius = 3f,
            float minDistance = 1.5f)
        {
            // 1. 先检查理想位置是否可用
            bool occupiedByTeammate = IsPositionOccupiedByTeammate(owner, desiredPosition, teammates, minDistance);
            bool conflictedWithTarget = IsTargetPositionConflicted(owner, desiredPosition, teammates, minDistance);

            if (!occupiedByTeammate && !conflictedWithTarget)
            {
                return desiredPosition;
            }

            // 2. 理想位置不可用，搜索周围可用位置
            Vector3 bestPosition = desiredPosition;
            float bestScore = float.MinValue;

            // 8个方向搜索（每45度）
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;
                Vector3 searchDir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                Vector3 testPos = desiredPosition + searchDir * searchRadius;

                // 评估这个位置
                float score = EvaluatePosition(
                    owner,
                    testPos,
                    desiredPosition,
                    teammates,
                    minDistance
                );

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPosition = testPos;
                }
            }

            return bestPosition;
        }

        // 评估位置得分
        private static float EvaluatePosition(
            GameObject owner,
            Vector3 testPos,
            Vector3 desiredPosition,
            List<GameObject> teammates,
            float minDistance)
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

            // 检查是否与队友的目标冲突
            foreach (var teammate in teammates)
            {
                if (teammate == owner) continue;
                var teammateAI = teammate.GetComponent<PlayerAI>();
                if (teammateAI != null && teammateAI.GetBlackboard() != null)
                {
                    Vector3 teammateTarget = teammateAI.GetBlackboard().MoveTarget;
                    if (teammateTarget != Vector3.zero)
                    {
                        float dist = Vector3.Distance(teammateTarget, testPos);
                        if (dist < minDistance)
                        {
                            overlapPenalty += (minDistance - dist) * 50f;
                        }
                    }
                }
            }
            
            return proximityScore - overlapPenalty;
        }
    }
}
