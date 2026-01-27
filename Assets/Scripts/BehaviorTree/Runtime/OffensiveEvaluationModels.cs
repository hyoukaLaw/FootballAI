using UnityEngine;

namespace BehaviorTree.Runtime
{
    /// <summary>
    /// 进攻动作类型枚举
    /// </summary>
    public enum OffensiveActionType
    {
        None,
        Shoot,
        Pass,
        Dribble,
        Clearance
    }

    /// <summary>
    /// 进攻动作评估结果
    /// </summary>
    public struct OffensiveAction
    {
        public OffensiveActionType ActionType;
        public Vector3 MoveTarget;
        public GameObject PassTarget;
        public bool CanShoot;
        public Vector3 ShootTarget;
        public Vector3 ClearanceTarget;
        public float Score;
        public OffensiveEvaluationDetails Details;

        public static OffensiveAction None => new OffensiveAction
        {
            ActionType = OffensiveActionType.None,
            Score = float.MinValue
        };
    }

    /// <summary>
    /// 进攻评估详情
    /// </summary>
    public struct OffensiveEvaluationDetails
    {
        public float ShootScore;
        public float PassScore;
        public float DribbleScore;
        public float ClearanceScore;
        public float LineSafety;
        public float TargetSafety;
        public int EnemiesInFront;
    }

    /// <summary>
    /// 传球评估结果
    /// </summary>
    public struct PassEvaluation
    {
        public float Score;
        public GameObject Target;
        public float LineSafety;
        public float TargetSafety;
        public Vector3 PassDirection;
        public float Distance;

        public OffensiveAction ToAction()
        {
            return new OffensiveAction
            {
                ActionType = OffensiveActionType.Pass,
                PassTarget = Target,
                MoveTarget = Vector3.zero,
                ClearanceTarget = Vector3.negativeInfinity,
                CanShoot = false,
                Score = Score,
                Details = new OffensiveEvaluationDetails
                {
                    PassScore = Score,
                    LineSafety = LineSafety,
                    TargetSafety = TargetSafety
                }
            };
        }
    }

    /// <summary>
    /// 带球评估结果
    /// </summary>
    public struct DribbleEvaluation
    {
        public float Score;
        public Vector3 Target;
        public int EnemiesInFront;

        public OffensiveAction ToAction()
        {
            return new OffensiveAction
            {
                ActionType = OffensiveActionType.Dribble,
                MoveTarget = Target,
                PassTarget = null,
                ClearanceTarget = Vector3.negativeInfinity,
                CanShoot = false,
                Score = Score,
                Details = new OffensiveEvaluationDetails
                {
                    DribbleScore = Score,
                    EnemiesInFront = EnemiesInFront
                }
            };
        }
    }

    /// <summary>
    /// 射门评估结果
    /// </summary>
    public struct ShootEvaluation
    {
        public float Score;
        public Vector3 Target;
        public float Distance;

        public OffensiveAction ToAction()
        {
            return new OffensiveAction
            {
                ActionType = OffensiveActionType.Shoot,
                ShootTarget = Target,
                MoveTarget = Vector3.zero,
                PassTarget = null,
                ClearanceTarget = Vector3.negativeInfinity,
                CanShoot = true,
                Score = Score,
                Details = new OffensiveEvaluationDetails
                {
                    ShootScore = Score
                }
            };
        }
    }

    /// <summary>
    /// 解围评估结果
    /// </summary>
    public struct ClearanceEvaluation
    {
        public float Score;
        public Vector3 Target;
        public int EnemiesNearCount;

        public OffensiveAction ToAction()
        {
            return new OffensiveAction
            {
                ActionType = OffensiveActionType.Clearance,
                ClearanceTarget = Target,
                MoveTarget = Vector3.zero,
                PassTarget = null,
                CanShoot = false,
                Score = Score,
                Details = new OffensiveEvaluationDetails
                {
                    ClearanceScore = Score
                }
            };
        }
    }
}
