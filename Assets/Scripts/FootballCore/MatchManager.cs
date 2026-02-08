using System.Collections.Generic;
using System.Linq;
using BehaviorTree.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEditor; // 添加UnityEditor命名空间引用
using UnityEngine.Events; // 添加UnityEvents命名空间引用

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
    
    private void Awake()
    {
        // 初始化单例
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        BallController = new BallController(Ball);
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
        ResetPlayers();
        ResetContext();
        ResetBall();// 重置球和球权
        _gamePaused = false;// 恢复游戏
    }

    /// <summary>
    /// 暂停所有AI逻辑
    /// </summary>
    private void OnGoalScored(string scoringTeam)
    {
        _currentGameState = GameState.Goal;
        // 更新比分
        if (scoringTeam == "Red")
        {
            _redScore++;
        }
        else if (scoringTeam == "Blue")
        {
            _blueScore++;
        }
        // 记录比分变动
        _currentMatchScoreChanges.Add($"{_redScore}:{_blueScore}");
        if((_redScore + _blueScore) % 2 == 0) NextKickoffTeam = "Red";
        else NextKickoffTeam = "Blue";
        // 检查是否有队伍达到20分，如果是则结束比赛
        if (_redScore >= 20 || _blueScore >= 20)
        {
            EndMatch();
            return;
        }
        _gamePaused = true;// 暂停游戏
        _autoResumeTimer = 0f;// 重置自动恢复倒计时
        // 通知UI更新比分显示
        UpdateScoreUI();
    }

    /// <summary>
    /// 结束当前比赛
    /// </summary>
    private void EndMatch()
    {
        CurrentMatchNumber++;
        // 记录本场比赛结果
        MatchResult result = new MatchResult
        {
            MatchNumber = CurrentMatchNumber,
            RedFinalScore = _redScore,
            BlueFinalScore = _blueScore,
            ScoreChanges = new List<string>(_currentMatchScoreChanges)
        };
        MatchHistory.Add(result);
        LogOneMatchResult(result);

        // 检查是否达到20场比赛
        if (CurrentMatchNumber >= TotalMatches)
        {
            // 输出最终统计
            OutputMatchStatistics();
            // 暂停游戏
            EditorApplication.isPaused = true;
        }
        else
        {
            // 暂停5秒后再开始下一场比赛
            _gamePaused = true;
            _autoResumeTimer = 0f;
            _isMatchEndPause = true;
        }
    }

    private void LogOneMatchResult(MatchResult result)
    {
        string log = $"第{result.MatchNumber}场比赛: Red {result.RedFinalScore} - Blue {result.BlueFinalScore}";
        if (result.ScoreChanges.Count > 0)
        {
            foreach (var scoreChange in result.ScoreChanges)
            {
                log += $"\n{scoreChange}";
            }
        }
        MyLog.LogInfo(log);
        OutputMatchStatistics();
    }

    /// <summary>
    /// 开始新的比赛
    /// </summary>
    private void StartNewMatch()
    {
        // 重置比分
        _redScore = 0;
        _blueScore = 0;
        NextKickoffTeam = "Red";
        _currentMatchScoreChanges.Clear();
        // 重置比赛状态（包括球员归位、球重置等）
        ResetMatchState();
        // 恢复游戏（只需设置GamePaused为false）
        _gamePaused = false;
        // 更新UI
        UpdateScoreUI();
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
        if (NextKickoffTeam == "Red" && RedStartPlayer != null)
        {
            RedStartPlayer.transform.position = Vector3.zero;
        }
        else if (NextKickoffTeam == "Blue" && BlueStartPlayer != null)
        {
            BlueStartPlayer.transform.position = Vector3.zero;
        }
    }

    private void ResetContext()
    {
        Context.IncomingPassTarget = null;
        Context.SetPassTarget(null);
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
        OnScoreChanged?.Invoke(0, 0);
    }
    
    private void LogZoneInfo()
    {
        foreach (var fieldZone in typeof(FieldZone).GetEnumValues())
        {
            ZoneUtils.ZoneRange zoneRange = ZoneUtils.GetZoneRange((FieldZone)fieldZone,
                Context.GetEnemyGoalPosition(TeamRedPlayers[0]), Context.GetMyGoalPosition(TeamRedPlayers[0]));
            MyLog.LogInfo($"fieldZone: {fieldZone.ToString()} {zoneRange.LeftBottom} {zoneRange.Width} {zoneRange.Length}");
        }
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
        _autoResumeTimer += TimeManager.Instance.GetDeltaTime();
        if (_autoResumeTimer >= AutoResumeInterval)
    {
            if (_isMatchEndPause)
            {
                // 比赛结束的暂停：开始下一场比赛
                StartNewMatch();
                _isMatchEndPause = false;
            }
            else
            {
                // 普通进球的暂停：恢复比赛
                ResumeGame();
            }
            _autoResumeTimer = 0f;
        }
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

        // 合并所有球员进行遍历
        List<GameObject> allPlayers = new List<GameObject>();
        allPlayers.AddRange(TeamRedPlayers);
        allPlayers.AddRange(TeamBluePlayers);
        foreach (var player in allPlayers)
        {
            float dist = Vector3.Distance(player.transform.position, Ball.transform.position);
            if (dist < PossessionThreshold && player != Context.BallController.GetRecentKicker() && !IsStunned(player))
            {
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
        GameObject closestPlayer = null;
        if (closestPlayers.Count > 0)
        {
            // 移除随机性，使用确定性选择规则
            closestPlayer = closestPlayers.OrderBy(p => p.name).FirstOrDefault();
        }
        if(closestPlayer != Context.GetBallHolder()) MyLog.LogInfo($"possession {Context.GetBallHolder()?.name}->{closestPlayer?.name}");
        Context.SetBallHolder(closestPlayer);
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
        if (OnScoreChanged != null)
        {
            OnScoreChanged.Invoke(_redScore, _blueScore);
        }
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
        _autoResumeTimer += TimeManager.Instance.GetDeltaTime();
        if (_autoResumeTimer >= _throwInResumeInterval)
    {
            _currentGameState = GameState.Playing;
            _autoResumeTimer = 0f;
            _throwInPlayer = null;
            _gamePaused = false;
        }
    }
    #endregion
    
    #region 日志
    
    /// <summary>
    /// 输出比赛统计信息
    /// </summary>
    private void OutputMatchStatistics()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        sb.AppendLine("========================================");
        sb.AppendLine("       比赛统计报告");
        sb.AppendLine("========================================");
        sb.AppendLine();
        
        // 输出每场比赛的比分
        sb.AppendLine("【每场比赛比分】");
        foreach(var result in MatchHistory)
        {
            sb.AppendLine($"第{result.MatchNumber}场: 红方 {result.RedFinalScore} - 蓝方 {result.BlueFinalScore}");
        }
        
        sb.AppendLine();
        
        // 计算统计信息
        float redTotal = 0;
        float blueTotal = 0;
        int redWins = 0;
        int blueWins = 0;
        int draws = 0;
        
        foreach(var result in MatchHistory)
        {
            redTotal += result.RedFinalScore;
            blueTotal += result.BlueFinalScore;
            
            if(result.RedFinalScore > result.BlueFinalScore)
                redWins++;
            else if(result.BlueFinalScore > result.RedFinalScore)
                blueWins++;
            else
                draws++;
        }
        
        float redAverage = redTotal / MatchHistory.Count;
        float blueAverage = blueTotal / MatchHistory.Count;
        
        // 输出统计信息
        sb.AppendLine("【统计汇总】");
        sb.AppendLine($"比赛场次: {MatchHistory.Count}");
        sb.AppendLine();
        sb.AppendLine($"红方平均得分: {redAverage:F2}");
        sb.AppendLine($"蓝方平均得分: {blueAverage:F2}");
        sb.AppendLine($"红方总进球数: {redTotal}");
        sb.AppendLine($"蓝方总进球数: {blueTotal}");
        sb.AppendLine();
        sb.AppendLine($"红方胜场: {redWins}");
        sb.AppendLine($"蓝方胜场: {blueWins}");
        sb.AppendLine($"平局数: {draws}");
        sb.AppendLine();
        
        // 计算胜负关系
        sb.AppendLine("【胜负分析】");
        if(redWins > blueWins)
        {
            sb.AppendLine($"红方表现更优，领先 {redWins - blueWins} 场");
        }
        else if(blueWins > redWins)
        {
            sb.AppendLine($"蓝方表现更优，领先 {blueWins - redWins} 场");
        }
        else
        {
            sb.AppendLine("双方平分秋色");
        }
        
        sb.AppendLine("========================================");
        
        MyLog.LogInfo(sb.ToString());
    }
    #endregion
}
