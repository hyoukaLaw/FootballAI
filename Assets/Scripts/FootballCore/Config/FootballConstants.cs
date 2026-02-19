
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
    public static float ShootMaxDistance = 5f;
    public static float BaseShootScore = 100f;
    public static float ShootBlockPenaltyFactor = 0.1f;
    public static float ShootNoBlockFactor = 1.1f;
    public static float ShootForce = 20f;
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
    public const float PassLineSafetyDefaultInterceptThreshold = 3f; // 传球线路默认拦截阈值
    public static float PassLineSafetyThreatPenaltyScale = 0.5f; // 单个拦截威胁惩罚系数
    public static float PassTargetSafetyBaseDistance = 2f; // 接应点危险距离
    public static float PassTargetSafetyThreatScale = 0.5f; // 接应点威胁缩放
    public static float PassTargetSafetyBehindFactor = 0.75f; // 身后敌人威胁折减
    #endregion

    #region DribbleTuning
    public static float DribbleDetectDistance = 3f;
    public static float DribbleDetectHalfAngle = 90f;
    public static float DribbleEnemyPenalty = 10f;
    public static float BaseDribbleScore = 40f;
    public static float DribbleClearBonus = 24f;
    public static float DribbleDistancePenalty = 20f;
    public static float DribbleSpaceDangerDistance = 2f; // 空间惩罚起始距离
    public static float DribbleSpaceDangerRange = 3f; // 空间惩罚过渡范围
    public static float DribbleLegacySpacePenaltyMultiplier = 10f; // 兼容旧评估分支
    #endregion

    #region ClearanceTuning
    public static float BaseClearanceScore = 45f;
    public static float ClearanceScorePerEnemy = 10f;
    public static float ClearanceBlockThreshold = 1.5f;
    public static float ClearanceScoreDistancePenalty = 50f;
    public static float ClearanceDistance = 15f;
    public static float ClearanceDetectDistance = 3f;
    public static float ClearKickSpeed = 20f;
    public static float ClearanceAngleNarrowThreshold = 30f; // 正前方高威胁角
    public static float ClearanceAngleWideThreshold = 60f; // 侧前方中威胁角
    public static readonly float[] ClearanceDirectionAngles = { 0f, 45f, -45f }; // 解围候选方向
    #endregion

    #region SupportTuning
    public static float IdealSupportDistance = 6.0f; // was 4.0f
    public static float LateralSpreadDistance = 3f;
    public static float SupportMinDistanceToHolder = 3f;
    public static float SupportMaxDistanceToHolder = 10f;
    public static float SupportPassLaneInterceptThreshold = 0.9f; // was 1.2f
    public static float SupportPassLanePenaltyPerEnemy = 0.15f; // was 0.35f
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
    public static float ZoneCandidateWidthInterval = 1.5f; // was 1f
    public static float ZoneCandidateLengthInterval = 1.5f; // was 1f
    public static int SupportCandidateLayers = 1;
    public static float SupportCandidateLayerWidth = 5f;
    public static int SupportCandidatePointsPerLayer = 8;
    public static int MarkCandidateLayers = 2; // was 3
    public static float MarkCandidateLayerWidth = 1f;
    public static int MarkCandidatePointsPerLayer = 8; // was 8
    public static int MarkRelationalEnemyCount = 2;
    public static float MarkRelationalInterceptRatio = 0.7f;
    public static float MarkRelationalLateralOffset = 1.2f;
    public static float MarkRelationalGoalSideRatio = 0.35f;
    public static int MarkFallbackEnemyCount = 2;
    public static int MarkFallbackLayers = 1;
    public static int MarkFallbackPointsPerLayer = 6;
    public static int AroundBallCandidateLayers = 2;
    public static float AroundBallCandidateLayerWidth = 1f;
    public static int AroundBallCandidatePointsPerLayer = 8;
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
    public static float DefaultPassRouteSafeDistance = 3f;
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
