using UnityEngine;

namespace FootballAI.FootballCore
{
public static class FootballConstants
{
    #region CoreTuning
    public static float FloatEpsilon = 0.0001f;
    public static float SamePositionDistance = 0.05f;
    public static float ClosestPlayerTolerance = 0.5f;
    public static float DecideMinStep = 0.8f;
    public static float OccupiedSearchRadius = 1f;
    #endregion

    #region TackleTuning
    public static float TryTackleDistance = 1f;
    public static float DefaultTackleSuccessRate = 0.5f;
    public static float TackleDistanceFactorBase = 1.5f;
    public static float BaseTackleProbability = 0.3f;
    public static float DefensiveAttributeBonus = 0.2f;
    public static float DistanceBonusCoefficient = 0.3f;
    #endregion

    #region OffensiveGeneralTuning
    public static float ForwardWeight = 1.0f;
    public static float DistanceWeight = 1.0f;
    public static float DribbleForwardDistance = 0.1f;
    public static float ForwardDetectionDistance = 3.5f;
    public static float DetectionAngleHalf = 90f;
    public static float SidestepDistance = 2.0f;
    public static float ShootDistance = 12f;
    public static float ShootAngleThreshold = 30f;
    #endregion

    #region ShootTuning
    public static float EnemyBlockDistanceThreshold = 1.5f;
    public static float ShootDistanceBase = 10f;
    public static float BaseShootScore = 100f;
    public static float ShootBlockPenaltyFactor = 0.1f;
    public static float ShootNoBlockFactor = 1.1f;
    public static float ShootForce = 20f;
    public static float ShootAccuracyBase = 1.0f;
    public static float ShootXOffsetRange = 1.5f;
    public static float ShootYOffsetRange = 1.0f;
    public static float BaseScoreShootScore = 80f;
    #endregion

    #region PassTuning
    public static float BasePassScore = 60f;
    public static float PassForwardWeight = 0.2f;
    public static float PassBlockPenaltyFactor = 0.1f;
    public static float PassMinDistance = 2f;
    public static float PassMaxDistance = 12f;
    public static float PassBlockThreshold = 1.0f;
    public static float PassTargetMinEnemyDistance = 1.4f;
    public static float PassScoreDistancePenalty = 3.5f;
    public static float BasePassScoreDefender = 62f;
    public static float BasePassScoreForward = 55f;
    public static float BasePassScoreMidfielder = 64f;
    public static float PassForwardDirectionBonus = 10f;
    #endregion

    #region DribbleTuning
    public static float DribbleDetectDistance = 3f;
    public static float DribbleDetectHalfAngle = 90f;
    public static float DribbleEnemyPenalty = 10f;
    public static float BaseDribbleScore = 40f;
    public static float DribbleClearBonus = 24f;
    public static float DribbleDistancePenalty = 20f;
    #endregion

    #region ClearanceTuning
    public static float BaseClearanceScore = 45f;
    public static float ClearanceScorePerEnemy = 10f;
    public static float ClearanceBlockThreshold = 1.5f;
    public static float ClearanceScoreDistancePenalty = 50f;
    public static float ClearanceDistance = 15f;
    public static float ClearanceDetectDistance = 3f;
    public static float ClearKickSpeed = 20f;
    #endregion

    #region SupportTuning
    public static float IdealSupportDistance = 4.0f;
    public static float LateralSpreadDistance = 3f;
    #endregion

    #region RestartTuning
    public static float CornerKickInFieldOffset = 0.2f;
    public static float GoalKickInFieldOffset = 2f;
    public static float RestartSupportNearDistance = 4f;
    public static float RestartSupportFarDistance = 8f;
    public static float RestartLateralOffset = 2f;
    #endregion

    #region PositionTuning
    public static float CandidateTeammateSafeDistance = 1.0f;
    public static float CandidatePredictedOverlapThreshold = 0.3f;
    public static float CandidateDeduplicateGridSize = 0.2f;
    #endregion

    #region MarkingTuning
    public static float MarkingEnemyDistanceBase = 3.5f;
    public static float MarkingStopPassDistanceThreshold = 0.9f;
    public static float MarkingStopPassBonus = 0.6f;
    public static float MarkingBaseBonus = 0.05f;
    #endregion

    #region DefenderDangerTuning
    public static float DefenderDangerGoalDistance = 12f;
    public static float DefenderDangerSidelineDistance = 2.2f;
    public static float DefenderDangerEnemyRadius = 3f;
    public static float DefenderDangerEnemyCountForMax = 2f;
    public static float DefenderDribbleDangerPenalty = 35f;
    public static float DefenderClearanceDangerBonus = 40f;
    #endregion

    #region LegacyUnusedTuning
    public const float DefaultPassRouteSafeDistance = 3f;
    public static float ZeroDistanceBoundary = 0;
    public static float PassThreshold = 60f;
    public static float TooClosePassDistance = 3f;
    public static float TooFarPassDistance = 10f;
    public static float SearchAngleStep = 15.0f;
    public static int MaxSearchIterations = 12;
    public static float DefaultTargetSpeed = 2.0f;
    public static float CommonBlockThreshold = 1f;
    #endregion
}
}
