using System.Collections.Generic;
using BehaviorTree.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events; // 添加UnityEvents命名空间引用

namespace FootballAI.FootballCore
{
public partial class MatchManager : MonoBehaviour
{
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
    public FormationLayout FormationLayout;

    [Header("Game Settings")]
    // 距离球多少米以内算"持球"
    public float PossessionThreshold = 0.5f;
    public float GoalDistance = 1.0f; // 球门判定距离
    public float StealCooldownDuration = 0f; // 抢断保护期时长（秒）
    public string NextKickoffTeam = "Red"; // 下一个开球队伍 ("Red" 或 "Blue")
    public float AutoResumeInterval = 5f;
    public float ThrowInResumeInterval = 2f; // 界外球恢复延迟时间（秒）
    public float PassTimeout = 3.0f;    // 传球超时时间
    
    [Header("比分系统")]
    private int _redScore = 0; // 红方得分
    private int _blueScore = 0; // 蓝方得分
    
    [Header("比赛统计系统")]
    public bool AutoGame = false; // 是否自动比赛
    public int CurrentMatchNumber = 0; // 当前比赛场次
    public int TotalMatches = 20; // 总比赛场次
    [ShowInInspector] private MatchRuntimeState _runtimeState = new MatchRuntimeState();
    public List<MatchResult> MatchHistory = new List<MatchResult>(); // 比赛历史记录
    private List<string> _currentMatchScoreChanges = new List<string>(); // 当前比赛比分变动记录
    
    public UnityEvent<int, int> OnScoreChanged; // 红方分数, 蓝方分数
    
    private MatchStatsSystem _matchStatsSystem;
    private MatchFlowSystem _matchFlowSystem;
    private PossessionRefereeSystem _possessionRefereeSystem;
    private MatchDiagnosticsSystem _matchDiagnosticsSystem;
    
    #region Unity 生命周期
    private void Awake()
    {
        // 初始化单例
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        BallController = new BallController(Ball);
        _matchStatsSystem = new MatchStatsSystem(OnScoreChanged);
        _matchFlowSystem = new MatchFlowSystem();
        _possessionRefereeSystem = new PossessionRefereeSystem();
        _matchDiagnosticsSystem = new MatchDiagnosticsSystem();
        InitContext();
        ResetBall();
        InitScoreEvent();
        _matchDiagnosticsSystem.LogZoneInfo(Context);
    }


    private void FixedUpdate()
    {
        if (_runtimeState.GamePaused)
        {
            // 自动比赛模式下，倒计时5秒后自动恢复
            if (AutoGame  && _runtimeState.CurrentGameState == MatchGameState.Goal)
            {
                HandleAutoGame();
            }
            else if (_runtimeState.CurrentGameState == MatchGameState.OutOfBounds)
            {
                HandleAutoResumeThrowIn();
            }
            return; // 游戏暂停，不执行任何逻辑
        }
        UpdatePlayerAI();
        _matchDiagnosticsSystem.UpdateBlueOverlapDiagnostics(TeamBluePlayers);
        BallController.Update(); // UpdateBall
        UpdatePossessionState();// 1. 计算物理状态 (谁拿着球？)
        UpdatePassTargetState();// 2. 清理过期的传球状态
        CheckGoal();// 3. 检测进球
        CheckBallOutOfBounds();// 4. 检测球出界
    }
    #endregion
    
    #region 比赛生命周期
    /// <summary>
    /// 恢复游戏（供外部调用，用于进球后的恢复）
    /// </summary>
    public void ResumeGame()
    {
        _matchFlowSystem.ResumeGame(TeamRedPlayers, TeamBluePlayers, Context, Ball, BallController, NextKickoffTeam,
            RedStartPlayer, BlueStartPlayer, ref _runtimeState.GamePaused);
    }

    /// <summary>
    /// 暂停所有AI逻辑
    /// </summary>
    private void OnGoalScored(string scoringTeam)
    {
        _runtimeState.CurrentGameState = MatchGameState.Goal;
        _matchStatsSystem.AddGoal(scoringTeam, ref _redScore, ref _blueScore, _currentMatchScoreChanges);
        if ((_redScore + _blueScore) % 2 == 0) NextKickoffTeam = "Red";
        else NextKickoffTeam = "Blue";
        if (_redScore >= 20 || _blueScore >= 20)
        {
            EndMatch();
            return;
        }
        _matchFlowSystem.BeginGoalPause(ref _runtimeState.GamePaused, ref _runtimeState.AutoResumeTimer);
        _matchStatsSystem.UpdateScoreUI(_redScore, _blueScore);
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
            PauseEditorPlayMode();
            #endif
        }
        else
        {
            _matchFlowSystem.BeginMatchEndPause(ref _runtimeState.GamePaused, ref _runtimeState.AutoResumeTimer, ref _runtimeState.IsMatchEndPause);
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
        _matchFlowSystem.StartNewMatch(ResetScoreForNewMatch, ResetMatchState, ref _runtimeState.GamePaused);
        _matchStatsSystem.UpdateScoreUI(_redScore, _blueScore);
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
        _matchFlowSystem.ResetContext(Context);
        _matchFlowSystem.ResetPlayers(TeamRedPlayers, TeamBluePlayers);
        ResetBall();
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public bool GetIsKickoffCountDownActive()
    {
        return _runtimeState.GamePaused && _runtimeState.CurrentGameState == MatchGameState.Goal && AutoGame;
    }

    public float GetKickoffCountDownRemainingSeconds()
    {
        float remaining = AutoResumeInterval - _runtimeState.AutoResumeTimer;
        return Mathf.Max(0f, remaining);
    }
    #endregion

    #region 比赛中的操作
    /// <summary>
    /// 执行抢断动作：统一管理球权转移和相关状态更新
    /// </summary>
    /// <param name="tackler">抢断者</param>
    /// <param name="currentHolder">当前持球人（被抢断者）</param>
    public void StealBall(GameObject tackler, GameObject currentHolder)
    {
        _possessionRefereeSystem.StealBall(Context, Ball, tackler, currentHolder, StealCooldownDuration);
    }
    #endregion

    #region 初始化
    private void InitContext()
    {
        // 初始化全局上下文
        Context = new MatchContext();
        if (FormationLayout == null)
            FormationLayout = Resources.Load<FormationLayout>("Config/FormationLayout");
        Context.Ball = Ball;
        Context.BallController = BallController;
        Context.TeamRedPlayers = TeamRedPlayers;
        Context.TeamBluePlayers = TeamBluePlayers;
        Context.RedGoal = RedGoal;
        Context.BlueGoal = BlueGoal;
        Context.Field = Field;
        Context.FormationLayout = FormationLayout;
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
        _matchFlowSystem.ResetBall(Ball, BallController, NextKickoffTeam, RedStartPlayer, BlueStartPlayer, Context);
    }

    private void InitScoreEvent()
    {
        _matchStatsSystem.InitScoreEvent(_redScore, _blueScore);
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

    private void HandleAutoGame()
    {
        _matchFlowSystem.HandleAutoGame(TimeManager.Instance.GetDeltaTime(), AutoResumeInterval, ref _runtimeState.AutoResumeTimer,
            ref _runtimeState.IsMatchEndPause, StartNewMatch, ResumeGame);
    }

    /// <summary>
    /// 清理过期的传球状态
    /// 当传球超时或球被拦截/接住时，清除传球目标锁定
    /// </summary>
    private void UpdatePassTargetState()
    {
        _possessionRefereeSystem.UpdatePassTargetState(Context, PassTimeout);
    }

    /// <summary>
    /// 核心裁判逻辑：遍历所有人，找出离球最近的那个，判断是否获得球权
    /// 修复：在距离相等时随机选择，避免红队因为遍历顺序优势而获得不公平的球权
    /// </summary>
    private void UpdatePossessionState()
    {
        _possessionRefereeSystem.UpdatePossessionState(Context, Ball, PossessionThreshold, IsStunned);
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
        _possessionRefereeSystem.CheckGoal(Ball, RedGoal, BlueGoal, GoalDistance, OnGoalScored);
    }
    
    #endregion
    
    #region 处理出界
    
    /// <summary>
    /// 检测球是否出界
    /// </summary>
    private void CheckBallOutOfBounds()
    {
        if (!_possessionRefereeSystem.TryHandleBallOutOfBounds(Context, Ball, TeamRedPlayers, TeamBluePlayers,
                out _runtimeState.OutOfBoundsPosition, out RestartType restartType, out string restartTeam, out _runtimeState.RestartPlayer,
                out Vector3 restartPosition))
            return;
        _runtimeState.CurrentGameState = MatchGameState.OutOfBounds;
        _runtimeState.CurrentRestartType = restartType;
        _matchFlowSystem.ResetContext(Context);
        _matchFlowSystem.ResetPlayers(TeamRedPlayers, TeamBluePlayers);
        _possessionRefereeSystem.SetupRestartPositions(Context, Ball, TeamRedPlayers, TeamBluePlayers,
            _runtimeState.RestartPlayer, restartPosition, restartType, restartTeam);
        _matchFlowSystem.BeginGoalPause(ref _runtimeState.GamePaused, ref _runtimeState.AutoResumeTimer);
    }
    
    /// <summary>
    /// 处理界外球自动恢复
    /// </summary>
    private void HandleAutoResumeThrowIn()
    {
        _matchFlowSystem.HandleAutoResumeThrowIn(TimeManager.Instance.GetDeltaTime(), ThrowInResumeInterval,
            ref _runtimeState.AutoResumeTimer, ResumeFromThrowIn);
    }

    private void ResumeFromThrowIn()
    {
        _runtimeState.CurrentGameState = MatchGameState.Playing;
        _runtimeState.RestartPlayer = null;
        _runtimeState.GamePaused = false;
    }
    #endregion
    
#if !UNITY_EDITOR
    private static void PauseEditorPlayMode()
    {
        // Runtime no-op. Editor implementation lives in MatchManager.Editor.cs.
    }
#endif

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
}
