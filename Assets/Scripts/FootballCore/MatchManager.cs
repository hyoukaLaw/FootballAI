using System.Collections.Generic;
using BehaviorTree.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events; // 添加UnityEvents命名空间引用
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MatchManager : MonoBehaviour
{
    // 比赛结果数据结构
    public class MatchResult
    {
        public int MatchNumber;
        public int RedFinalScore;
        public int BlueFinalScore;
        public List<string> ScoreChanges = new List<string>();
    }
    // --- 单例模式 (Singleton) ---
    // 方便任何地方都能访问 MatchManager.Instance
    public static MatchManager Instance { get; private set; }
    // --- 全局上下文 ---
    public MatchContext Context;  // 全局上下文实例

    [Header("Scene References")]
    public GameObject Ball;
    public BallController BallController;
    // --- 新增：球门引用 ---
    public Transform RedGoal;  // 红方防守的球门（蓝方攻击目标）
    public Transform BlueGoal; // 蓝方防守的球门（红方攻击目标）
    // 在 Inspector 中把红队和蓝队的圆柱体分别拖进去
    public List<GameObject> TeamRedPlayers = new List<GameObject>();
    public List<GameObject> TeamBluePlayers = new List<GameObject>();
    public GameObject RedStartPlayer;// 红方开球人
    public GameObject BlueStartPlayer;// 蓝方开球人
    public GameObject Field; // 球场模型

    [Header("Game Settings")]
    // 距离球多少米以内算"持球"
    public float PossessionThreshold = 0.5f;
    public float GoalDistance = 1.0f; // 球门判定距离
    public float StealCooldownDuration = 0f; // 抢断保护期时长（秒）
    public string NextKickoffTeam = "Red"; // 下一个开球队伍 ("Red" 或 "Blue")
    public float AutoResumeInterval = 5f;
    
    [Header("比分系统")]
    private int _redScore = 0; // 红方得分
    private int _blueScore = 0; // 蓝方得分
    
    [Header("比赛统计系统")]
    [ShowInInspector] private bool _gamePaused = false; // 游戏是否暂停
    public bool AutoGame = false; // 是否自动比赛
    public int CurrentMatchNumber = 0; // 当前比赛场次
    public int TotalMatches = 20; // 总比赛场次
    public List<MatchResult> MatchHistory = new List<MatchResult>(); // 比赛历史记录
    private List<string> _currentMatchScoreChanges = new List<string>(); // 当前比赛比分变动记录

    
    public UnityEvent<int, int> OnScoreChanged; // 红方分数, 蓝方分数

    private float _passTimeout = 3.0f;    // 传球超时时间
    private float _autoResumeTimer = 0f; // 自动恢复倒计时
    private bool _isMatchEndPause = false; // 标记是否是比赛结束的暂停
    
    private enum GameState { Playing, Goal, OutOfBounds }
    private GameState _currentGameState = GameState.Playing;
    private Vector3 _outOfBoundsPosition;
    private GameObject _throwInPlayer; // 指定的发球球员
    private float _throwInResumeInterval = 2f; // 界外球恢复延迟时间（秒）
    private float _blueOverlapDiagnosticsTimer = 0f;
    private MatchStatsSystem _matchStatsSystem;
    private MatchFlowSystem _matchFlowSystem;
    
    private void Awake()
    {
        // 初始化单例
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        BallController = new BallController(Ball);
        _matchStatsSystem = new MatchStatsSystem(OnScoreChanged);
        _matchFlowSystem = new MatchFlowSystem();
        InitContext();
        ResetBall();
        InitScoreEvent();
        LogZoneInfo();
    }


    private void FixedUpdate()
    {
        if (_gamePaused)
        {
            // 自动比赛模式下，倒计时5秒后自动恢复
            if (AutoGame  && _currentGameState == GameState.Goal)
            {
                HandleAutoGame();
            }
            else if (_currentGameState == GameState.OutOfBounds)
            {
                HandleAutoResumeThrowIn();
            }
            return; // 游戏暂停，不执行任何逻辑
        }
        UpdatePlayerAI();
        UpdateBlueOverlapDiagnostics();
        UpdatePossessionState();// 1. 计算物理状态 (谁拿着球？)
        BallController.Update(); // UpdateBall
        UpdatePassTargetState();// 2. 清理过期的传球状态
        CheckGoal();// 3. 检测进球
        CheckBallOutOfBounds();// 4. 检测球出界
    }
    
    #region 比赛生命周期
    /// <summary>
    /// 恢复游戏（供外部调用，用于进球后的恢复）
    /// </summary>
    public void ResumeGame()
    {
        _matchFlowSystem.ResumeGame(ResetPlayers, ResetContext, ResetBall, ref _gamePaused);
    }

    /// <summary>
    /// 暂停所有AI逻辑
    /// </summary>
    private void OnGoalScored(string scoringTeam)
    {
        _currentGameState = GameState.Goal;
        _matchStatsSystem.AddGoal(scoringTeam, ref _redScore, ref _blueScore, _currentMatchScoreChanges);
        if ((_redScore + _blueScore) % 2 == 0) NextKickoffTeam = "Red";
        else NextKickoffTeam = "Blue";
        if (_redScore >= 20 || _blueScore >= 20)
        {
            EndMatch();
            return;
        }
        _matchFlowSystem.BeginGoalPause(ref _gamePaused, ref _autoResumeTimer);
        UpdateScoreUI();
    }

    /// <summary>
    /// 结束当前比赛
    /// </summary>
    private void EndMatch()
    {
        MatchResult result = _matchStatsSystem.BuildMatchResultAndTrack(ref CurrentMatchNumber, _redScore, _blueScore,
            _currentMatchScoreChanges, MatchHistory);
        LogOneMatchResult(result);
        if (CurrentMatchNumber >= TotalMatches)
        {
            OutputMatchStatistics();
#if UNITY_EDITOR
            EditorApplication.isPaused = true;
#endif
        }
        else
        {
            _matchFlowSystem.BeginMatchEndPause(ref _gamePaused, ref _autoResumeTimer, ref _isMatchEndPause);
        }
    }

    private void LogOneMatchResult(MatchResult result)
    {
        MyLog.LogInfo(_matchStatsSystem.BuildOneMatchResultLog(result));
        OutputMatchStatistics();
    }

    /// <summary>
    /// 开始新的比赛
    /// </summary>
    private void StartNewMatch()
    {
        _matchFlowSystem.StartNewMatch(ResetScoreForNewMatch, ResetMatchState, UpdateScoreUI, ref _gamePaused);
    }

    private void ResetScoreForNewMatch()
    {
        _matchStatsSystem.ResetForNewMatch(ref _redScore, ref _blueScore, _currentMatchScoreChanges);
        NextKickoffTeam = "Red";
    }

    /// <summary>
    /// 重置比赛状态
    /// </summary>
    private void ResetMatchState()
    {
        ResetContext();
        ResetPlayers();
        // 重置球的位置
        ResetBall();
    }
    #endregion

    #region 比赛中的操作
    /// <summary>
    /// 触发抢断保护期，防止抢断后立即被反抢
    /// </summary>
    public void TriggerStealCooldown()
    {
        if (Context != null)
        {
            Context.SetStealCooldown(StealCooldownDuration);
        }
    }

    /// <summary>
    /// 执行抢断动作：统一管理球权转移和相关状态更新
    /// </summary>
    /// <param name="tackler">抢断者</param>
    /// <param name="currentHolder">当前持球人（被抢断者）</param>
    public void StealBall(GameObject tackler, GameObject currentHolder)
    {
        // 1. 移动球到抢断者位置
        Ball.transform.position = tackler.transform.position;
        // 2. 更新球权
        Context.SetBallHolder(tackler);
        // 3. 触发抢断保护期
        TriggerStealCooldown();
        // 4. 让被抢断者停顿
        if (currentHolder != null)
        {
            var holderAI = currentHolder.GetComponent<PlayerAI>();
            if (holderAI?.GetBlackboard() != null)
            {
                var bb = holderAI.GetBlackboard();
                bb.IsStunned = true;
                bb.StunTimer = bb.StunDuration;
            }
        }
    }
    #endregion

    #region 初始化
    private void InitContext()
    {
        // 初始化全局上下文
        Context = new MatchContext();
        Context.Ball = Ball;
        Context.BallController = BallController;
        Context.TeamRedPlayers = TeamRedPlayers;
        Context.TeamBluePlayers = TeamBluePlayers;
        Context.RedGoal = RedGoal;
        Context.BlueGoal = BlueGoal;
        Context.Field = Field;
        foreach(var player in TeamRedPlayers)
        {
            var playerAI = player.GetComponent<PlayerAI>();
            playerAI.GetBlackboard().MatchContext = Context;
        }
        foreach (var player in TeamBluePlayers)
        {
            var playerAI = player.GetComponent<PlayerAI>();
            playerAI.GetBlackboard().MatchContext = Context;
        }
    }
    
    public void ResetBall()
    {
        Ball.transform.position = Vector3.zero;
        BallController.ResetMotionState();

        GameObject kickoffPlayer = null;
        if (NextKickoffTeam == "Red" && RedStartPlayer != null)
        {
            RedStartPlayer.transform.position = Vector3.zero;
            kickoffPlayer = RedStartPlayer;
        }
        else if (NextKickoffTeam == "Blue" && BlueStartPlayer != null)
        {
            BlueStartPlayer.transform.position = Vector3.zero;
            kickoffPlayer = BlueStartPlayer;
        }

        Context.SetBallHolder(kickoffPlayer);
    }

    private void ResetContext()
    {
        Context.IncomingPassTarget = null;
        Context.SetPassTarget(null);
        Context.SetBallHolder(null);
        Context.SetStealCooldown(0f);
    }

    private void ResetPlayers()
    {
        // 重置所有球员状态
        foreach(var player in TeamRedPlayers)
        {
            var playerAI = player.GetComponent<PlayerAI>();
            if(playerAI != null)
            {
                playerAI.ResetPosition();
                playerAI.ResetBlackboard();
                playerAI.ResetBehaviorTree();
            }
        }
        foreach(var player in TeamBluePlayers)
        {
            var playerAI = player.GetComponent<PlayerAI>();
            if(playerAI != null)
            {
                playerAI.ResetPosition();
                playerAI.ResetBlackboard();
                playerAI.ResetBehaviorTree();
            }
        }
    }

    private void InitScoreEvent()
    {
        _matchStatsSystem.InitScoreEvent(_redScore, _blueScore);
    }
    
    private void LogZoneInfo()
    {
        foreach (var fieldZone in typeof(FieldZone).GetEnumValues())
        {
            ZoneUtils.ZoneRange zoneRange = ZoneUtils.GetZoneRange((FieldZone)fieldZone,
                Context.GetEnemyGoalPosition(TeamRedPlayers[0]), Context.GetMyGoalPosition(TeamRedPlayers[0]));
            LogFieldZoneInfo((FieldZone)fieldZone, zoneRange);
        }
    }

    private void LogFieldZoneInfo(FieldZone zone, ZoneUtils.ZoneRange zoneRange)
    {
        MyLog.LogInfo($"fieldZone: {zone} {zoneRange.LeftBottom} {zoneRange.Width} {zoneRange.Length}");
    }

    private void LogPossessionChange(GameObject previousHolder, GameObject newHolder)
    {
        if (previousHolder == newHolder)
            return;

        MyLog.LogInfo($"possession {previousHolder?.name}->{newHolder?.name}");
    }

    private void LogBlueOverlapError(GameObject playerA, GameObject playerB, float distance, float minDistance)
    {
        MyLog.LogError($"[BlueOverlap] {playerA.name} and {playerB.name} distance={distance:F3} (< {minDistance:F1})");
    }
    #endregion
    
    #region 更新过程
    private void UpdatePlayerAI()
    {
        int maxPlayers = Mathf.Max(TeamRedPlayers.Count, TeamBluePlayers.Count);
        for (int i = 0; i < maxPlayers; i++)// 确保红队和蓝队AI交替执行，消除执行顺序导致的系统性偏见
        {
            // 红队AI先执行
            if (i < TeamRedPlayers.Count)
            {
                var redPlayerAI = TeamRedPlayers[i].GetComponent<PlayerAI>();
                if (redPlayerAI != null)
                    redPlayerAI.ManualTick();
            }

            // 蓝队AI后执行
            if (i < TeamBluePlayers.Count)
            {
                var bluePlayerAI = TeamBluePlayers[i].GetComponent<PlayerAI>();
                if (bluePlayerAI != null)
                    bluePlayerAI.ManualTick();
            }
        }
    }

    private void UpdateBlueOverlapDiagnostics()
    {
        if (!RuntimeDebugSettings.EnableBlueOverlapDiagnostics)
            return;

        _blueOverlapDiagnosticsTimer += TimeManager.Instance.GetDeltaTime();
        if (_blueOverlapDiagnosticsTimer < RuntimeDebugSettings.BlueOverlapDiagnosticInterval)
            return;

        _blueOverlapDiagnosticsTimer = 0f;
        LogBlueTeammateOverlapErrors();
    }

    private void LogBlueTeammateOverlapErrors()
    {
        const float minDistance = 0.5f;
        for (int i = 0; i < TeamBluePlayers.Count; i++)
        {
            GameObject playerA = TeamBluePlayers[i];
            if (playerA == null) continue;
            for (int j = i + 1; j < TeamBluePlayers.Count; j++)
            {
                GameObject playerB = TeamBluePlayers[j];
                if (playerB == null) continue;
                float distance = Vector3.Distance(playerA.transform.position, playerB.transform.position);
                if (distance < minDistance)
                {
                    LogBlueOverlapError(playerA, playerB, distance, minDistance);
                }
            }
        }
    }

    private void HandleAutoGame()
    {
        _matchFlowSystem.HandleAutoGame(TimeManager.Instance.GetDeltaTime(), AutoResumeInterval, ref _autoResumeTimer,
            ref _isMatchEndPause, StartNewMatch, ResumeGame);
    }

    /// <summary>
    /// 清理过期的传球状态
    /// 当传球超时或球被拦截/接住时，清除传球目标锁定
    /// </summary>
    private void UpdatePassTargetState()
    {
        Context.UpdatePassTarget(_passTimeout, Context.GetBallHolder());
    }

    /// <summary>
    /// 核心裁判逻辑：遍历所有人，找出离球最近的那个，判断是否获得球权
    /// 修复：在距离相等时随机选择，避免红队因为遍历顺序优势而获得不公平的球权
    /// </summary>
    private void UpdatePossessionState()
    {
        // 更新抢断保护期计时器
        Context.UpdateStealCooldown(TimeManager.Instance.GetDeltaTime());
        if(BallController.GetIsMoving())
            Context.SetBallHolder(null);
        // 两种情况可以进行争球：1 没有持球人
        if (Context.GetBallHolder() != null) return;
        
        List<GameObject> closestPlayers = new List<GameObject>();
        float minDistance = float.MaxValue;
        float distanceTolerance = 0.001f; // 距离相等容差值

        AddClosestPlayers(TeamRedPlayers, closestPlayers, ref minDistance, distanceTolerance);
        AddClosestPlayers(TeamBluePlayers, closestPlayers, ref minDistance, distanceTolerance);

        GameObject closestPlayer = null;
        if (closestPlayers.Count > 0)
        {
            closestPlayer = closestPlayers[0];
            for (int i = 1; i < closestPlayers.Count; i++)
            {
                GameObject candidate = closestPlayers[i];
                if (string.CompareOrdinal(candidate.name, closestPlayer.name) < 0)
                {
                    closestPlayer = candidate;
                }
            }
        }
        LogPossessionChange(Context.GetBallHolder(), closestPlayer);
        Context.SetBallHolder(closestPlayer);
    }

    private void AddClosestPlayers(List<GameObject> players, List<GameObject> closestPlayers, ref float minDistance, float distanceTolerance)
    {
        for (int i = 0; i < players.Count; i++)
        {
            GameObject player = players[i];
            if (player == null) continue;

            float dist = Vector3.Distance(player.transform.position, Ball.transform.position);
            if (dist >= PossessionThreshold || player == Context.BallController.GetRecentKicker() || IsStunned(player))
                continue;

            if (dist < minDistance - distanceTolerance)
            {
                minDistance = dist;
                closestPlayers.Clear();
                closestPlayers.Add(player);
            }
            else if (dist <= minDistance + distanceTolerance)
            {
                closestPlayers.Add(player);
            }
        }
    }

    private bool IsStunned(GameObject player)
    {
        return player.GetComponent<PlayerAI>().GetBlackboard().IsStunned;
    }

    /// <summary>
    /// 检测进球
    /// 检查球是否到达任意球门位置
    /// </summary>
    private void CheckGoal()
    {
        Vector3 ballPos = Ball.transform.position;
        // 检测红方球门（蓝方进攻）
        float distToRedGoal = Vector3.Distance(ballPos, RedGoal.position);
        if (distToRedGoal < GoalDistance)
        {
            OnGoalScored("Blue"); // 蓝方进球
            return;
        }
        // 检测蓝方球门（红方进攻）
        float distToBlueGoal = Vector3.Distance(ballPos, BlueGoal.position);
        if (distToBlueGoal < GoalDistance)
        {
            OnGoalScored("Red"); // 红方进球
            return;
        }
    }
    
    /// <summary>
    /// 更新比分显示UI
    /// </summary>
    private void UpdateScoreUI()
    {
        _matchStatsSystem.UpdateScoreUI(_redScore, _blueScore);
    }
    #endregion
    
    #region 处理出界
    
    /// <summary>
    /// 检测球是否出界
    /// </summary>
    private void CheckBallOutOfBounds()
    {
        
        if (Context.IsInField(Ball.transform.position))
        {
            return;
        }
        
        // 球出界了
        _currentGameState = GameState.OutOfBounds;
        _outOfBoundsPosition = Ball.transform.position;
        
        // 确定发球方（最后踢球人的对方）
        GameObject lastKicker = BallController.GetLastKicker();
        string throwingTeam = "Red";
        
        if (lastKicker != null)
        {
            if (TeamRedPlayers.Contains(lastKicker))
            {
                throwingTeam = "Blue";
            }
            else if (TeamBluePlayers.Contains(lastKicker))
            {
                throwingTeam = "Red";
            }
        }
        
        // 设置发球球员
        if (throwingTeam == "Red" && RedStartPlayer != null)
        {
            _throwInPlayer = RedStartPlayer;
        }
        else if (throwingTeam == "Blue" && BlueStartPlayer != null)
        {
            _throwInPlayer = BlueStartPlayer;
        }
        
        // 设置球员位置
        SetupThrowInPositions(throwingTeam);
        
        // 重置自动恢复计时器
        _autoResumeTimer = 0f;
        _gamePaused = true;
    }
    
    /// <summary>
    /// 设置界外球时的球员位置
    /// </summary>
    /// <param name="throwingTeam">发球队伍 ("Red" 或 "Blue")</param>
    private void SetupThrowInPositions(string throwingTeam)
    {
        // 清理传球等状态
        ResetContext();
        
        // 其他球员都回初始位置
        foreach (var player in TeamRedPlayers)
        {
            var playerAI = player.GetComponent<PlayerAI>();
            if (playerAI != null)
            {
                playerAI.ResetPosition();
                playerAI.ResetBlackboard();
                playerAI.ResetBehaviorTree();
            }
            
        }
        
        foreach (var player in TeamBluePlayers)
        {
            var playerAI = player.GetComponent<PlayerAI>();
            if (playerAI != null)
            {
                playerAI.ResetPosition();
                playerAI.ResetBlackboard();
                playerAI.ResetBehaviorTree();
            }
            
        }
                
        // 将发球球员移动到出界位置
        if (_throwInPlayer != null)
        {
            _throwInPlayer.transform.position = FootballUtils.ClampToField(Context, _outOfBoundsPosition, out bool wasClamped);
        }
        // 将球放在出界位置（由发球球员持有）
        Ball.transform.position = FootballUtils.ClampToField(Context, _outOfBoundsPosition, out bool _);
        Context.SetBallHolder(_throwInPlayer);
        _throwInPlayer.GetComponent<PlayerAI>().GetBlackboard().IsPassingOutsideBall = true;
    }
    
    /// <summary>
    /// 处理界外球自动恢复
    /// </summary>
    private void HandleAutoResumeThrowIn()
    {
        _matchFlowSystem.HandleAutoResumeThrowIn(TimeManager.Instance.GetDeltaTime(), _throwInResumeInterval,
            ref _autoResumeTimer, ResumeFromThrowIn);
    }

    private void ResumeFromThrowIn()
    {
        _currentGameState = GameState.Playing;
        _throwInPlayer = null;
        _gamePaused = false;
    }
    #endregion
    
    #region 日志
    
    /// <summary>
    /// 输出比赛统计信息
    /// </summary>
    private void OutputMatchStatistics()
    {
        if (MatchHistory.Count == 0)
        {
            MyLog.LogInfo("比赛统计报告: 暂无比赛数据");
            return;
        }
        MyLog.LogInfo(_matchStatsSystem.BuildMatchStatisticsReport(MatchHistory));
    }
    #endregion
}
