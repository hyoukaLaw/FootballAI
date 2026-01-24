using System.Collections.Generic;
using UnityEngine;
using UnityEditor; // 添加UnityEditor命名空间引用
using UnityEngine.Events; // 添加UnityEvents命名空间引用

public class MatchManager : MonoBehaviour
{
    // --- 单例模式 (Singleton) ---
    // 方便任何地方都能访问 MatchManager.Instance
    public static MatchManager Instance { get; private set; }

    // --- 全局上下文 ---
    public BehaviorTree.Runtime.MatchContext Context;  // 全局上下文实例

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

    [Header("进球检测")]
    public float GoalDistance = 1.0f; // 球门判定距离
    public bool GamePaused = false; // 游戏是否暂停

    [Header("比分系统")]
    public int RedScore = 0; // 红方得分
    public int BlueScore = 0; // 蓝方得分
    public string NextKickoffTeam = "Red"; // 下一个开球队伍 ("Red" 或 "Blue")

    [Header("传球状态")]
    private float _passTimeout = 3.0f;    // 传球超时时间

    [Header("抢断保护")]
    public float StealCooldownDuration = 0f; // 抢断保护期时长（秒）

    [Header("事件系统")]
    public UnityEvent<int, int> OnScoreChanged; // 红方分数, 蓝方分数

    private void Awake()
    {
        // 初始化单例
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        BallController = new BallController(Ball);

        // 初始化全局上下文
        Context = new BehaviorTree.Runtime.MatchContext();
        Context.Ball = Ball;
        Context.BallController = BallController;
        Context.TeamRedPlayers = TeamRedPlayers;
        Context.TeamBluePlayers = TeamBluePlayers;
        Context.RedGoal = RedGoal;
        Context.BlueGoal = BlueGoal;
        Context.Field = Field;
        ResetBall();
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

    private void Update()
    {
        if (GamePaused)
        {
            return; // 游戏暂停，不执行任何逻辑
        }
        // 更新抢断保护期计时器
        Context.UpdateStealCooldown(Time.deltaTime);

        // 1. 计算物理状态 (谁拿着球？)
        UpdatePossessionState();
        BallController.Update();
        
        // 2. 清理过期的传球状态
        UpdatePassTargetState();

        // 3. 检测进球
        CheckGoal();
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
    /// </summary>
    private void UpdatePossessionState()
    {
        if(BallController.GetIsMoving())
            Context.SetBallHolder(null);
        // 两种情况可以进行争球：1 没有持球人
        if (Context.GetBallHolder() != null) return;
        GameObject closestPlayer = null;
        float minDistance = float.MaxValue;

        // 合并所有球员进行遍历
        List<GameObject> allPlayers = new List<GameObject>();
        allPlayers.AddRange(TeamRedPlayers);
        allPlayers.AddRange(TeamBluePlayers);

        foreach (var player in allPlayers)
        {
            float dist = Vector3.Distance(player.transform.position, Ball.transform.position);
            if (dist < minDistance && dist < PossessionThreshold && 
                player != Context.BallController.GetLastKicker() && !IsStunned(player))
            {
                minDistance = dist;
                closestPlayer = player;
            }
        }
        if(closestPlayer != Context.GetBallHolder()) Debug.Log($"possession {Context.GetBallHolder()?.name}->{closestPlayer?.name}");
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
    /// 恢复游戏（供外部调用）
    /// </summary>
    public void ResumeGame()
    {
        //1. 归位所有红队球员
        foreach (var player in TeamRedPlayers)
        {
            if (player != null)
            {
                var playerAI = player.GetComponent<PlayerAI>();
                if (playerAI != null)
                {
                    playerAI.ResetPosition();
                    playerAI.ResetBlackboard();
                    playerAI.ResetBehaviorTree(); // 重置行为树所有节点状态
                }
            }
        }

        //2. 归位所有蓝队球员
        foreach (var player in TeamBluePlayers)
        {
            if (player != null)
            {
                var playerAI = player.GetComponent<PlayerAI>();
                if (playerAI != null)
                {
                    playerAI.ResetPosition();
                    playerAI.ResetBlackboard();
                    playerAI.ResetBehaviorTree(); // 重置行为树所有节点状态
                }
            }
        }

        //3. 重置球和球权
        ResetBall();

        //5. 恢复游戏
        GamePaused = false;

        Debug.Log($"比赛恢复！{NextKickoffTeam}方开球，所有球员归位，球放回中心，行为树状态已重置。");
    }

    /// <summary>
/// 暂停所有AI逻辑
/// </summary>
private void OnGoalScored(string scoringTeam)
{
    // 更新比分
    if (scoringTeam == "Red")
    {
        RedScore++;
        NextKickoffTeam = "Blue"; // 蓝方开球
    }
    else if (scoringTeam == "Blue")
    {
        BlueScore++;
        NextKickoffTeam = "Red"; // 红方开球
    }

    // 暂停游戏
    GamePaused = true;
    
    if (Context != null)
    {
        Context.IncomingPassTarget = null;
        Context.SetPassTarget(null);
    }

    // 清除抢断保护期
    if (Context != null)
    {
        Context.SetStealCooldown(0f);
    }

    // 通知UI更新比分显示
    UpdateScoreUI();
}

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
    
    public void ResetBall()
    {
        Ball.transform.position = Vector3.zero;
        if (NextKickoffTeam == "Red" && RedStartPlayer != null)
        {
            RedStartPlayer.transform.position = Vector3.zero;
            Debug.Log($"RedStartPlayer position reset to {RedStartPlayer.transform.position}");
        }
        else if (NextKickoffTeam == "Blue" && BlueStartPlayer != null)
        {
            BlueStartPlayer.transform.position = Vector3.zero;
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

    /// <summary>
    /// 更新比分显示UI
    /// </summary>
    private void UpdateScoreUI()
    {
        if (OnScoreChanged != null)
        {
            OnScoreChanged.Invoke(RedScore, BlueScore);
        }
    }

    /// <summary>
    /// 在Scene视图中显示全局信息
    /// </summary>
    void OnGUI()
    {
        // 检查是否在Scene视图中
        if (Event.current.type == EventType.Repaint && Camera.current != null && Camera.current.orthographic)
        {
            // 设置文本样式
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 14;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.UpperRight;
            style.padding = new RectOffset(10, 10, 10, 10);

            // 计算右上角位置
            float width = 200f;
            float height = 30f;
            Rect rect = new Rect(Screen.width - width - 20, 20, width, height);

            // 显示Player当前直径
            string diameterText = "Player当前直径: 1.0";
            GUI.Label(rect, diameterText, style);
        }
    }
}