using System.Collections.Generic;
using UnityEngine;


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
    }