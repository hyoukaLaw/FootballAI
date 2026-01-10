// PlayerAI.cs (挂在圆柱体上)

using System.Collections.Generic;
using UnityEngine;
using BehaviorTree;
using Unity.VisualScripting; // 引用命名空间

public class PlayerAI : MonoBehaviour
{
    private FootballBlackboard _blackboard;
    private BehaviorTree.BehaviorTree _tree; // 指明是由于我们自定义的类
    public Node CurrentNode;

    void Awake()
    {
        // 1. 【源头】创建唯一的黑板实例
        _blackboard = new FootballBlackboard();
        _blackboard.Owner = this.gameObject; // 记录自己是谁

        // 2. 创建树，把黑板传进去
        _tree = new BehaviorTree.BehaviorTree(_blackboard);

        // 3. 构建行为树结构 (这里是关键的引用传递！)
        _tree.SetRoot(BuildOffensiveTree());
    }

    void Update()
    {
        // 4. 每帧运行
        _tree.Tick();
    }
// === 核心：组装进攻方行为树 ===
    private Node BuildOffensiveTree()
    {
        // ---------------------------------------------
        // 分支 A: 如果我有球 (Ball Carrier Logic)
        // ---------------------------------------------
        Node checkHasBall = new CheckHasBallNode(_blackboard);
        
        // 1. 思考节点：评估我是传还是带？
        // 这个节点总是返回 SUCCESS，因为它只是计算并更新黑板
        Node evaluateOptions = new TaskEvaluateOffensiveOptions(_blackboard);

        // 2. 传球分支
        // 条件：黑板里有传球目标吗？
        Node checkPassTarget = new SimpleCondition(_blackboard, bb => bb.BestPassTarget != null);
        Node passAction = new TaskPassBall(_blackboard);
        SequenceNode passSequence = new SequenceNode(_blackboard, new List<Node> { checkPassTarget, passAction });

        // 3. 盘带分支 (兜底)
        // 条件：黑板里有移动目标吗？
        Node checkDribbleTarget = new SimpleCondition(_blackboard, bb => bb.MoveTarget != Vector3.zero);
        Node dribbleAction = new TaskMoveToPosition(_blackboard); // 复用之前的移动节点！
        SequenceNode dribbleSequence = new SequenceNode(_blackboard, new List<Node> { checkDribbleTarget, dribbleAction });

        // 4. 行动选择器：优先传球，不行就盘带
        SelectorNode actionSelector = new SelectorNode(_blackboard, new List<Node> { passSequence, dribbleSequence });

        // 5. 总序列：先思考，再行动
        SequenceNode hasBallSequence = new SequenceNode(_blackboard, new List<Node> 
        { 
            checkHasBall, // 确认有球
            evaluateOptions,                   // 脑子：想
            actionSelector                     // 身体：动
        });


// ==========================================
        // 分支 B: 无球逻辑 (修正版)
        // ==========================================

        // --- 新增：子分支 B-1 (接球/抢球) ---
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


        // --- 原有：子分支 B-2 (战术跑位) ---
        Node calcSpot = new TaskCalculateSupportSpot(_blackboard);
        Node moveSpot = new TaskMoveToPosition(_blackboard);
        SequenceNode supportSeq = new SequenceNode(_blackboard, new List<Node> { calcSpot, moveSpot });


        // --- 分支 B 总选择器 ---
        // 优先抢球，不需要抢球才去跑位
        SelectorNode offBallSelector = new SelectorNode(_blackboard, new List<Node> 
        { 
            interceptSeq, 
            supportSeq 
        });

        // ==========================================
        // 根节点
        // ==========================================
        SelectorNode root = new SelectorNode(_blackboard, new List<Node> 
        { 
            hasBallSequence, 
            offBallSelector // 这里替换原来的 supportSeq
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
    // 这是一个工厂方法，负责组装具体的逻辑
    private Node BuildTestTree(FootballBlackboard bb)
    {
        // // 比如：创建 "持球判断" 节点，传入 bb
        // Node checkBall = new CheckHasBallNode(bb);
        //
        // // 比如：创建 "移动" 节点，传入 bb
        // Node move = new TaskMoveToPosition(bb);
        //
        // // 组装成序列 (Sequence 也需要 bb，因为它也是 Node 的子类)
        // Sequence attackSeq = new Sequence(bb, new List<Node>{ checkBall, move });
        //
        // return attackSeq;
// --- 测试 Setup ---

        // 强制把目标点设为 (10, 0, 10)，假装是某个智能节点计算出来的
        bb.MoveTarget = new Vector3(10, 0, 10); 
    
        // --- 构建树 ---
        // 只有这一个节点，意味着它会一直跑向 (10,0,10)
        // 到了之后返回 SUCCESS，下一帧树重新 Tick，又发现到了，继续 SUCCESS
        Node moveNode = new TaskMoveToPosition(bb);
    
        return moveNode;
    }
    
    // === 新增：给 MatchManager 用的接口 ===
    public FootballBlackboard GetBlackboard()
    {
        return _blackboard;
    }
}