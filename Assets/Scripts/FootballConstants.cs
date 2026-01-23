using UnityEngine;

public static class FootballConstants
{
    // PlayerAI Constants
    public static float TryTackleDistance = 1f;         // 抢断尝试范围，也就是1.5m以内才有可能抢断成功
    
    // FootballUtils Constants
    public const float DefaultPassRouteSafeDistance = 3f; // 传球路线最小安全距离默认值
    public static float ZeroDistanceBoundary = 0;         // 距离计算中的边界条件（当线段长度为0时）
    
    // CheckIsClosestToLooseBall Constants
    public static float ClosestPlayerTolerance = 0.5f;    // 判断最近球员的容错值
    
    // TaskTackle Constants
    public static float DefaultTackleSuccessRate = 0.5f;  // 默认抢断成功率
    public static float TackleDistanceFactorBase = 1.5f;    // 距离因子计算的基础值
    public static float BaseTackleProbability = 0.3f;     // 基础抢断概率
    public static float DefensiveAttributeBonus = 0.2f;   // 防守属性加成系数
    public static float DistanceBonusCoefficient = 0.3f;  // 距离加成系数
    
    // TaskEvaluateOffensiveOptions Constants
    // public static float BasePassScore = 30f;              // 基础传球评分
    public static float ForwardWeight = 1.0f;             // 前方权重
    public static float DistanceWeight = 1.0f;            // 距离权重
    public static float DribbleForwardDistance = 0.1f;    // 盘带前进距离
    public static float ForwardDetectionDistance = 3.5f;  // 前方检测距离
    public static float DetectionAngleHalf = 90f;         // 检测角度（半角）
    public static float SidestepDistance = 3.0f;          // 侧移距离
    public static float ShootDistance = 12f;              // 射门距离
    public static float ShootAngleThreshold = 30f;        // 射门角度阈值
    public static float PassThreshold = 60f;              // 传球阈值
    public static float TooClosePassDistance = 3f;        // 太近传球距离阈值
    public static float TooFarPassDistance = 10f;         // 太远传球距离阈值
    
    // 进攻选择评估（new）
    public static float EnemyBlockDistanceThreshold = 1.5f; // 敌人阻挡距离阈值
    public static float ShootDistanceBase = 10f; // 最远射门距离
    public static float BaseShootScore = 100f;
    public static float ShootBlockPenaltyFactor = 0.1f;
    public static float ShootNoBlockFactor = 1.1f;
    public static float BasePassScore = 60f;
    public static float PassForwardWeight = 0.2f;
    public static float PassBlockPenaltyFactor = 0.1f;
    public static float DribbleDetectDistance = 3f;
    public static float DribbleDetectHalfAngle = 90f;
    public static float DribbleEnemyPenalty = 10f;
    public static float BaseDribbleScore = 40f;
    public static float DribbleClearBonus = 25f;
    
    // TaskCalculateSupportSpot Constants
    public static float IdealSupportDistance = 4.0f;      // 理想接应距离
    public static float SearchAngleStep = 15.0f;          // 搜索角度步长
    public static int MaxSearchIterations = 12;           // 最大搜索迭代次数
    public static float LateralSpreadDistance = 3f;       // 横向拉开距离
    
    // TaskPassBall Constants
    public static float DefaultTargetSpeed = 2.0f;        // 默认目标速度（当无法获取目标速度时使用）
    
    // TaskShoot Constants
    public static float ShootForce = 20f;                 // 射门力度
    public static float ShootAccuracyBase = 1.0f;         // 射门精度计算基础值
    public static float ShootXOffsetRange = 1.5f;         // 射门X轴偏移范围
    public static float ShootYOffsetRange = 1.0f;         // 射门Y轴偏移范围

    public static float DecideMinStep = 0.25f; // 每一步位置移动决策的长度
    public static float OccupiedSearchRadius = 0.25f; // 距离多远开始算是被占据
    
    // TaskEvaluateRolePreferences Constants
    public static float PassMinDistance = 2f;
    public static float PassMaxDistance = 10f;
    public static float PassBlockThreshold = 1f;
    public static float BasePassScoreDefender = 80f;
    public static float PassScoreDistancePenalty = 5f;
    public static float BaseClearanceScore = 30f;
    public static float ClearanceScorePerEnemy = 10f;
    public static float ClearanceBlockThreshold = 1.5f;
    public static float ClearanceScoreDistancePenalty = 50f;
    public static float ClearanceDistance = 15f; // 向前踢出多远
    public static float ClearanceDetectDistance = 3f; // 解围探测敌方的阈值
    public static float ClearKickSpeed = 20f;

    public static float BaseScoreShootScore = 80f;
    public static float BasePassScoreForward = 60f;
    
    // 通用常量
    public static float FloatEpsilon = 0.0001f;
    public static float SamePositionDistance = 0.05f;
    public static float CommonBlockThreshold = 1f;
}