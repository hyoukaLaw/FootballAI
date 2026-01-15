using System.Collections.Generic;
using UnityEngine;


    public static class FootballUtils
    {
        public static bool IsPassRouteSafe(Vector3 from, Vector3 to, List<GameObject> enemies, float minDist = 3f)
        {
            // 遍历黑板里的敌人列表
            if (enemies == null) return true;
            // 简单的“管道检测”：
            // 如果任何一个敌人距离“传球线段”太近，就认为是不安全的
            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                
                // 计算点到线段的距离 (这是几何数学，网上有很多现成公式)
                float distToLine = DistancePointToLineSegment(from, to, enemy.transform.position);
                
                // 如果敌人距离传球路线小于 1.5米，认为会被拦截
                if (distToLine < minDist)
                {
                    return false; // 不安全
                }
            }
            return true; // 安全
        }
        
        private static float DistancePointToLineSegment(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 ab = b - a;
            Vector3 ap = p - a;
            float magOfab2 = ab.sqrMagnitude;
            if (magOfab2 == 0) return (p - a).magnitude;
            float t = Vector3.Dot(ap, ab) / magOfab2;
            // 限制 t 在线段范围内 [0, 1]
            if (t < 0) 
                return (p - a).magnitude;
            else if (t > 1) 
                return (p - b).magnitude;
            Vector3 closestPoint = a + ab * t;
            return (p - closestPoint).magnitude;
        }

        public static bool IsBehind(GameObject me, GameObject target)
        {
            float enemyGoalPosZ = MatchManager.Instance.Context.GetEnemyGoalPosition(me).z;
            if (enemyGoalPosZ > 0)
            {
                return me.transform.position.z < target.transform.position.z;
            }
            else
            {
                return me.transform.position.z > target.transform.position.z;
            }
        }

        public static Vector3 GetForward(GameObject me)
        {
            float enemyGoalPosZ = MatchManager.Instance.Context.GetEnemyGoalPosition(me).z;
            if (enemyGoalPosZ > 0)
            {
                return Vector3.forward;
            }
            else
            {
                return -Vector3.forward;
            }
        }
    }
