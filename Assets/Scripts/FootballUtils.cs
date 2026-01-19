using System.Collections.Generic;
using UnityEngine;
using BehaviorTree.Runtime;

public static class FootballUtils
{
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

    public static float DistancePointToLineSegment(Vector3 a, Vector3 b, Vector3 p)
    {
        Vector3 ab = b - a;
        Vector3 ap = p - a;
        float magOfab2 = ab.sqrMagnitude;
        if (magOfab2 == 0) return (p - a).magnitude;
        float t = Vector3.Dot(ap, ab) / magOfab2;
        if (t < 0)
            return (p - a).magnitude;
        else if (t > 1)
            return (p - b).magnitude;
        Vector3 closestPoint = a + ab * t;
        return (p - closestPoint).magnitude;
    }

    public static bool IsPathClear(Vector3 start, Vector3 end, List<GameObject> obstacles, float blockThreshold)
    {
        if (obstacles == null) return true;

        foreach (var obstacle in obstacles)
        {
            if (obstacle == null) continue;
            if (Vector3.Dot(end - start, obstacle.transform.position - start) < 0)
                continue;

            float distToLine = DistancePointToLineSegment(start, end, obstacle.transform.position);
            if (distToLine < blockThreshold)
            {
                return false;
            }
        }

        return true;
    }

    public static List<GameObject> FindEnemiesInFront(GameObject owner, Vector3 forwardDir, List<GameObject> enemies, 
        float detectDistance, float detectHalfAngle)
    {
        List<GameObject> enemiesInFront = new List<GameObject>();
        foreach (var enemy in enemies)
        {
            Vector3 toEnemy = enemy.transform.position - owner.transform.position;
            float distance = toEnemy.magnitude;
            float angle = Vector3.Angle(forwardDir, toEnemy.normalized);
            if (distance <= detectDistance && angle <= detectHalfAngle)
            {
                enemiesInFront.Add(enemy);
            }
        }

        return enemiesInFront;
    }

    public static bool IsClosestTeammateToTarget(Vector3 targetPos, GameObject owner, List<GameObject> teammates, float tolerance = 0.5f)
    {
        float myDist = Vector3.Distance(owner.transform.position, targetPos);

        foreach (var mate in teammates)
        {
            if (mate == owner) continue;
            if (Vector3.Distance(mate.transform.position, targetPos) < myDist - tolerance)
            {
                return false;
            }
        }
        return true;
    }

    public static GameObject FindClosestEnemy(GameObject owner, List<GameObject> enemies, GameObject excludeEnemy = null)
    {
        GameObject closestEnemy = null;
        float closestDist = float.MaxValue;
        foreach (var enemy in enemies)
        {
            if (enemy == excludeEnemy) continue;
            float d = Vector3.Distance(owner.transform.position, enemy.transform.position);
            if (d < closestDist)
            {
                closestDist = d;
                closestEnemy = enemy;
            }
        }
        return closestEnemy;
    }
}