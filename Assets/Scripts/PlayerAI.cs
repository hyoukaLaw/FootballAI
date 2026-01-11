// PlayerAI.cs (挂在圆柱体上)

using System.Collections.Generic;
using UnityEngine;
using BehaviorTree;
using Unity.VisualScripting; // 引用命名空间

[System.Serializable]
public class PlayerStats
{
    [Header("移动属性")]
    public float MovementSpeed = 2.0f;
    public float SprintMultiplier = 1.2f;
    
    [Header("传球属性")]
    public float PassingSpeed = 10f;
    [Range(0.5f, 1.0f)]
    public float PassingAccuracy = 0.9f;

    [Header("射门属性")]
    public float ShootingPower = 20f;
    [Range(0.3f, 1.0f)]
    public float ShootingAccuracy = 0.7f;

    [Header("防守属性")]
    public float DefensiveAwareness = 1.0f;
}

public class PlayerAI : MonoBehaviour
{
    private FootballBlackboard _blackboard;
    private BehaviorTree.BehaviorTree _tree; // 指明是由于我们自定义的类
    public Node CurrentNode;

    [Header("球员属性配置")]
    public PlayerStats Stats = new PlayerStats();

    void Awake()
    {
        // 1. 【源头】创建唯一的黑板实例
        _blackboard = new FootballBlackboard();
        _blackboard.Owner = this.gameObject; // 记录自己是谁
        _blackboard.Stats = Stats; // 传入球员属性

        // 2. 注入全局上下文
        if (MatchManager.Instance != null && MatchManager.Instance.Context != null)
        {
            _blackboard.MatchContext = MatchManager.Instance.Context;
        }
        else
        {
            Debug.LogError("MatchManager.Instance or Context is null!");
        }

        // 3. 创建树，把黑板传进去
        _tree = new BehaviorTree.BehaviorTree(_blackboard);

        // 4. 构建行为树结构 (这里是关键的引用传递！)
        _tree.SetRoot(BuildMainTree());
    }

    void Update()
    {
        if (MatchManager.Instance.GamePaused)
            return;
        // 每帧运行行为树
        _tree.Tick();
    }
    
    // === 新增：构建完整主树 ===
    private Node BuildMainTree()
    {
        // === 最高优先级：眩晕状态检查 ===
        Node checkStunned = new CheckIsStunned(_blackboard);
        Node stunWait = new TaskStunWait(_blackboard);
        SequenceNode stunnedSeq = new SequenceNode(_blackboard, new List<Node>
        {
            checkStunned,
            stunWait
        });

        // === 次高优先级：我是传球目标 → 接球 ===
        Node isPassTarget = new CheckIsPassTarget(_blackboard);
        Node chaseBallForPass = new TaskChaseBall(_blackboard);
        Node runToPass = new TaskMoveToPosition(_blackboard);
        SequenceNode passReceiveSeq = new SequenceNode(_blackboard, new List<Node>
        {
            isPassTarget,
            chaseBallForPass,
            runToPass
        });

        // === 次要优先级：攻防逻辑 ===
        // 1. 构建 进攻子树 (你原来写的那个)
        Node offensiveTree = BuildOffensiveTree();

        // 2. 构建 防守子树 (新写的)
        Node defensiveTree = BuildDefensiveTree();

        // 3. 根选择器：决定是进攻还是防守
        // 逻辑：如果我们队拿球 -> 进攻；否则 -> 防守
        // 注意：这里需要一个条件节点来判断球权归属

        Node isTeamInControl = new SimpleCondition(_blackboard, IsTeamControllingBall);

        // 如果条件满足(本队控球)，执行进攻树；否则执行防守树
        // 这种结构可以用 "If-Else" 风格的选择器，或者简单的 Selector 配合取反条件
        // 咱们用一个标准的 Selector 结构：
        //   - 尝试执行"进攻分支" (前提是本队控球)
        //   - 否则执行"防守分支"

        SequenceNode offensiveBranch = new SequenceNode(_blackboard, new List<Node>
        {
            isTeamInControl,
            offensiveTree
        });

        // === 根节点：眩晕 > 接球 > 进攻 > 防守 ===
        SelectorNode root = new SelectorNode(_blackboard, new List<Node>
        {
            stunnedSeq,          // 最高：处于眩晕状态
            passReceiveSeq,      // 次高：我是传球目标
            offensiveBranch,     // 次要：本队控球→进攻
            defensiveTree        // 最后：防守
        });

        return root;
    }
    
    // 辅助条件：我方是否控球？
    private bool IsTeamControllingBall(FootballBlackboard bb)
    {
        // 如果球没人拿，或者球在队友脚下，或者是自己脚下
        if (bb.MatchContext.BallHolder == null) return false; // 无主球不算控球，通常进入争抢逻辑(防守端处理)
        
        if (bb.MatchContext.BallHolder == bb.Owner) return true;
        if (bb.MatchContext.GetTeammates(this.gameObject).Contains(bb.MatchContext.BallHolder)) return true;
        
        return false;
    }
    
    // === 新增：组装防守行为树 ===
    private Node BuildDefensiveTree()
    {
        // 分支 A: 争抢无主球 (Loose Ball)
        Node checkLoose = new CheckIsClosestToLooseBall(_blackboard);
        Node chaseBall = new TaskChaseBall(_blackboard);
        Node moveAction = new TaskMoveToPosition(_blackboard);
        
        SequenceNode looseBallSeq = new SequenceNode(_blackboard, new List<Node>
        {
            checkLoose, chaseBall, moveAction
        });
    
        // 分支 B: 组织防守 (Organized Defense)
        // 1. 思考节点：评估我是去抢持球人还是盯人
        Node evalDefense = new TaskEvaluateDefensiveState(_blackboard);
    
        // 2. 抢断分支
        // 条件：是否在抢断范围内且有明确的目标
        Node checkCanTackle = new SimpleCondition(_blackboard, bb => 
        {
            if (bb.MatchContext.BallHolder == null) return false;
            float distance = Vector3.Distance(bb.Owner.transform.position, bb.MatchContext.BallHolder.transform.position);
            return distance < 1.5f; // 抢断尝试范围
        });
        Node tackleAction = new TaskTackle(_blackboard);
        SequenceNode tackleSeq = new SequenceNode(_blackboard, new List<Node>
        {
            checkCanTackle, tackleAction
        });
    
        // 3. 移动分支：如果不能抢断，就移动接近
        Node moveToPosition = new TaskMoveToPosition(_blackboard);
    
        // 4. 防守行动选择器：优先尝试抢断，否则移动
        SelectorNode defenseActionSelector = new SelectorNode(_blackboard, new List<Node>
        {
            tackleSeq,
            moveToPosition
        });
    
        // 5. 防守序列：先评估防守状态，再执行行动
        SequenceNode organizedDefenseSeq = new SequenceNode(_blackboard, new List<Node>
        {
            evalDefense,
            defenseActionSelector
        });
    
        // 防守根：优先抢无主球，否则进行组织防守
        return new SelectorNode(_blackboard, new List<Node>
        {
            looseBallSeq,
            organizedDefenseSeq
        });
    }

// === 核心：组装进攻方行为树 ===
    private Node BuildOffensiveTree()
    {
        // ---------------------------------------------
        // 分支 A: 如果我有球 (Ball Carrier Logic)
        // ---------------------------------------------
        Node checkHasBall = new CheckHasBallNode(_blackboard);
        
        // 1. 思考节点：评估我是射门、传还是带？
        // 这个节点总是返回 SUCCESS，因为它只是计算并更新黑板
        Node evaluateOptions = new TaskEvaluateOffensiveOptions(_blackboard);

        // 2. 射门分支 (最高优先级)
        Node checkCanShoot = new SimpleCondition(_blackboard, bb => bb.CanShoot);
        Node shootAction = new TaskShoot(_blackboard);
        SequenceNode shootSequence = new SequenceNode(_blackboard, new List<Node> { checkCanShoot, shootAction });

        // 3. 传球分支
        // 条件：黑板里有传球目标吗？
        Node checkPassTarget = new SimpleCondition(_blackboard, bb => bb.BestPassTarget != null);
        Node passAction = new TaskPassBall(_blackboard);
        SequenceNode passSequence = new SequenceNode(_blackboard, new List<Node> { checkPassTarget, passAction });

        // 4. 盘带分支 (兜底)
        // 条件：黑板里有移动目标吗？
        Node checkDribbleTarget = new SimpleCondition(_blackboard, bb => bb.MoveTarget != Vector3.zero);
        Node dribbleAction = new TaskMoveToPosition(_blackboard); // 复用之前的移动节点！
        SequenceNode dribbleSequence = new SequenceNode(_blackboard, new List<Node> { checkDribbleTarget, dribbleAction });

        // 5. 行动选择器：优先射门 > 传球 > 盘带
        SelectorNode actionSelector = new SelectorNode(_blackboard, new List<Node>
        {
            shootSequence,
            passSequence,
            dribbleSequence
        });

        // 5. 总序列：先思考，再行动
        SequenceNode hasBallSequence = new SequenceNode(_blackboard, new List<Node> 
        { 
            checkHasBall, // 确认有球
            evaluateOptions,                   // 脑子：想
            actionSelector                     // 身体：动
        });


// ==========================================
        // 分支 B: 无球逻辑
        // ==========================================

        // --- 子分支 B-0 (接应传球) ---
        // 如果我是传球目标，去接球（但在主树中已有更高优先级的处理）
        Node isPassTargetOff = new CheckIsPassTarget(_blackboard);
        Node chaseBallForPassOff = new TaskChaseBall(_blackboard);
        Node runToPassOff = new TaskMoveToPosition(_blackboard);
        SequenceNode passReceiveSeqOff = new SequenceNode(_blackboard, new List<Node>
        {
            isPassTargetOff,
            chaseBallForPassOff,
            runToPassOff
        });

        // --- 子分支 B-1 (抢无主球) ---
        // 逻辑：如果球是无主的，且我是最近的 -> 追球
        Node checkLoose = new CheckIsClosestToLooseBall(_blackboard);
        Node setBallTarget = new TaskChaseBall(_blackboard); // 把目标设为球
        Node runToBall = new TaskMoveToPosition(_blackboard); // 复用移动节点

        SequenceNode interceptSeq = new SequenceNode(_blackboard, new List<Node>
        {
            checkLoose,
            setBallTarget,
            runToBall
        });


        // --- 子分支 B-2 (战术跑位) ---
        Node calcSpot = new TaskCalculateSupportSpot(_blackboard);
        Node moveSpot = new TaskMoveToPosition(_blackboard);
        SequenceNode supportSeq = new SequenceNode(_blackboard, new List<Node> { calcSpot, moveSpot });


        // --- 分支 B 总选择器 ---
        // 优先级：接球 > 抢球 > 跑位
        SelectorNode offBallSelector = new SelectorNode(_blackboard, new List<Node>
        {
            passReceiveSeqOff, // 传球目标
            interceptSeq,      // 抢球
            supportSeq         // 跑位
        });

        // ==========================================
        // 进攻树根节点
        // ==========================================
        SelectorNode root = new SelectorNode(_blackboard, new List<Node>
        {
            hasBallSequence,
            offBallSelector
        });


        // ---------------------------------------------
        // 根节点: 选择器 (Selector)
        // ---------------------------------------------
        // 逻辑：优先看是不是有球？ -> 是，执行A。 -> 否，执行B (跑位)。

        return root;
    }
    
    // 调试用：在 Scene 窗口画出他想去哪
    void OnDrawGizmos()
    {
        if (_blackboard != null)
        {
            // 画出移动目标
            if (_blackboard.MoveTarget != Vector3.zero)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, _blackboard.MoveTarget);
                Gizmos.DrawWireSphere(_blackboard.MoveTarget, 0.3f);
            }
            // 画出传球目标连线
            if (_blackboard.BestPassTarget != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, _blackboard.BestPassTarget.transform.position);
                Gizmos.DrawIcon(transform.position + Vector3.up * 2, "Pass");
            }
        }
    }
    
    // === 新增：给 MatchManager 用的接口 ===
    public FootballBlackboard GetBlackboard()
    {
        return _blackboard;
    }
}