using System.Collections.Generic;
using UnityEngine;

public class MatchManager : MonoBehaviour
{
    // --- 单例模式 (Singleton) ---
    // 方便任何地方都能访问 MatchManager.Instance
    public static MatchManager Instance { get; private set; }

    [Header("Scene References")]
    public GameObject Ball;
    // --- 新增：球门引用 ---
    public Transform RedGoal;  // 红方防守的球门（蓝方攻击目标）
    public Transform BlueGoal; // 蓝方防守的球门（红方攻击目标）
    
    // 在 Inspector 中把红队和蓝队的圆柱体分别拖进去
    public List<GameObject> TeamRedPlayers = new List<GameObject>();
    public List<GameObject> TeamBluePlayers = new List<GameObject>();

    [Header("Game Settings")]
    // 距离球多少米以内算"持球"
    public float PossessionThreshold = 0.8f;

    [Header("进球检测")]
    public float GoalDistance = 1.0f; // 球门判定距离
    public bool GamePaused = false; // 游戏是否暂停

    [Header("Debug Info (Read Only)")]
    // 当前持球者 (如果没有人持球则为 null)
    public GameObject CurrentBallHolder;

    [Header("传球状态")]
    public GameObject IncomingPassTarget; // 当前应该接球的队友
    private float _passTimeout = 3.0f;    // 传球超时时间
    private float _passTimer = 0f;        // 计时器

    [Header("抢断保护")]
    private float _stealCooldownTimer = 0f; // 抢断保护期计时器
    public float StealCooldownDuration = 3f; // 抢断保护期时长（秒）
    public bool IsInStealCooldown { get { return _stealCooldownTimer > 0f; } } // 是否在保护期内

    private void Awake()
    {
        // 初始化单例
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (GamePaused)
        {
            return; // 游戏暂停，不执行任何逻辑
        }

        // 更新抢断保护期计时器
        if (_stealCooldownTimer > 0f)
        {
            _stealCooldownTimer -= Time.deltaTime;
        }

        // 1. 计算物理状态 (谁拿着球？)
        UpdatePossessionState();

        // 2. 清理过期的传球状态
        UpdatePassTargetState();

        // 3. 检测进球
        CheckGoal();

        // 4. 同步数据 (把计算结果塞给所有人的黑板)
        SyncAllBlackboards();
    }

    /// <summary>
    /// 清理过期的传球状态
    /// 当传球超时或球被拦截/接住时，清除传球目标锁定
    /// </summary>
    private void UpdatePassTargetState()
    {
        if (IncomingPassTarget != null)
        {
            _passTimer += Time.deltaTime;

            // 超时或球已被接住，清除传球目标
            if (_passTimer > _passTimeout || CurrentBallHolder != null)
            {
                IncomingPassTarget = null;
                _passTimer = 0f;
            }
        }
    }

    /// <summary>
    /// 核心裁判逻辑：遍历所有人，找出离球最近的那个，判断是否获得球权
    /// </summary>
    private void UpdatePossessionState()
    {
        // 如果在抢断保护期内，跳过重新计算球权
        if (_stealCooldownTimer > 0f)
        {
            return;
        }

        GameObject closestPlayer = null;
        float minDistance = float.MaxValue;

        // 合并所有球员进行遍历
        List<GameObject> allPlayers = new List<GameObject>();
        allPlayers.AddRange(TeamRedPlayers);
        allPlayers.AddRange(TeamBluePlayers);

        foreach (var player in allPlayers)
        {
            if (player == null) continue;

            float dist = Vector3.Distance(player.transform.position, Ball.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestPlayer = player;
            }
        }

        // 只有距离小于阈值，才算真正持球
        // 否则球处于"无人控制"状态 (Loose Ball)
        if (minDistance <= PossessionThreshold)
        {
            CurrentBallHolder = closestPlayer;
        }
        else
        {
            CurrentBallHolder = null;
        }
    }

    /// <summary>
    /// 数据分发：将全局信息写入每个球员的 FootballBlackboard
    /// </summary>
    public void SyncAllBlackboards()
    {
        // 为红队同步 (队友是红队，敌人是蓝队)
        SyncTeamData(TeamRedPlayers, TeamRedPlayers, TeamBluePlayers, BlueGoal.position);

        // 为蓝队同步 (队友是蓝队，敌人是红队)
        SyncTeamData(TeamBluePlayers, TeamBluePlayers, TeamRedPlayers, RedGoal.position);
    }

    /// <summary>
    /// 通用同步方法
    /// </summary>
    private void SyncTeamData(List<GameObject> playersToSync,
        List<GameObject> allies, List<GameObject> enemies,
        Vector3 enemyGoalPos)
    {
        foreach (var playerObj in playersToSync)
        {
            if (playerObj == null) continue;

            // 获取球员身上的 PlayerAI 组件 (我们之前定义的那个入口类)
            var aiController = playerObj.GetComponent<PlayerAI>();
            
            // 防御性编程：确保该物体真的有 AI
            if (aiController != null && aiController.GetBlackboard() != null)
            {
                var bb = aiController.GetBlackboard();

                // --- 填充黑板数据 ---
                bb.Ball = Ball;
                bb.BallHolder = CurrentBallHolder; // 告诉他现在球在谁脚下
                bb.Teammates = allies;             // 告诉他谁是队友
                bb.Opponents = enemies;            // 告诉他谁是敌人

                bb.EnemyGoalPosition = enemyGoalPos;

                // 同步传球目标状态
                bb.IsPassTarget = (playerObj == IncomingPassTarget);
                // 计算一些常用的个人数据，免得他在节点里重复算
                // bb.DistanceToBall = Vector3.Distance(playerObj.transform.position, Ball.transform.position);
            }
        }
    }

    /// <summary>
    /// 检测进球
    /// 检查球是否到达任意球门位置
    /// </summary>
    private void CheckGoal()
    {
        if (Ball == null) return;

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
    /// 进球后的处理
    /// 暂停所有AI逻辑
    /// </summary>
    private void OnGoalScored(string scoringTeam)
    {
        // 暂停游戏
        GamePaused = true;

        // 可选：在这里重置球的位置、统计分数等
        // ResetBall();
    }

    /// <summary>
    /// 恢复游戏（供外部调用）
    /// </summary>
    public void ResumeGame()
    {
        GamePaused = false;
    }

    /// <summary>
    /// 重置球的位置（可选）
    /// </summary>
    private void ResetBall()
    {
        // 将球放回中心
        Ball.transform.position = Vector3.zero;

        // 重置球的速度
        BallController ballCtrl = Ball.GetComponent<BallController>();
        // 如果 BallController 有重置方法可以调用
    }

    /// <summary>
    /// 触发抢断保护期，防止抢断后立即被反抢
    /// </summary>
    public void TriggerStealCooldown()
    {
        _stealCooldownTimer = StealCooldownDuration;
    }
}