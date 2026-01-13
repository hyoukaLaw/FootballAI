using System.Collections.Generic;
using UnityEngine;

public class MatchManager : MonoBehaviour
{
    // --- 单例模式 (Singleton) ---
    // 方便任何地方都能访问 MatchManager.Instance
    public static MatchManager Instance { get; private set; }

    // --- 全局上下文 ---
    public BehaviorTree.MatchContext Context;  // 全局上下文实例

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

    [Header("传球状态")]
    private float _passTimeout = 3.0f;    // 传球超时时间

    [Header("抢断保护")]
    public float StealCooldownDuration = 3f; // 抢断保护期时长（秒）

    private void Awake()
    {
        // 初始化单例
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 初始化全局上下文
        Context = new BehaviorTree.MatchContext();
        Context.Ball = Ball;
        Context.TeamRedPlayers = TeamRedPlayers;
        Context.TeamBluePlayers = TeamBluePlayers;
        Context.RedGoal = RedGoal;
        Context.BlueGoal = BlueGoal;
    }

    private void Update()
    {
        if (GamePaused)
        {
            return; // 游戏暂停，不执行任何逻辑
        }

        // 更新抢断保护期计时器
        if (Context != null)
        {
            Context.UpdateStealCooldown(Time.deltaTime);
        }

        // 1. 计算物理状态 (谁拿着球？)
        UpdatePossessionState();

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
        if (Context != null)
        {
            Context.UpdatePassTarget(_passTimeout, Context.BallHolder);
        }
    }

    /// <summary>
    /// 核心裁判逻辑：遍历所有人，找出离球最近的那个，判断是否获得球权
    /// </summary>
    private void UpdatePossessionState()
    {

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
            if(closestPlayer != Context.BallHolder) Debug.Log($"possession {Context.BallHolder?.name}->{closestPlayer?.name}");
            if (Context != null)
                Context.BallHolder = closestPlayer;
        }
        else
        {
            if (Context != null)
                Context.BallHolder = null;
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

        //4. 恢复游戏
        GamePaused = false;

        Debug.Log("比赛恢复！所有球员归位，球放回中心，行为树状态已重置。");
    }

    /// <summary>
    /// 暂停所有AI逻辑
    /// </summary>
    private void OnGoalScored(string scoringTeam)
    {
        // 暂停游戏
        GamePaused = true;
        
        
        if (Context != null)
        {
            Context.IncomingPassTarget = null;
            Context.SetPassTarget(null);
        }

        // 5. 清除抢断保护期
        if (Context != null)
        {
            Context.SetStealCooldown(0f);
        }
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
    }
}