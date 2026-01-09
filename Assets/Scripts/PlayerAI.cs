// PlayerAI.cs (挂在圆柱体上)

using System.Collections.Generic;
using UnityEngine;
using BehaviorTree;
using Unity.VisualScripting; // 引用命名空间

public class PlayerAI : MonoBehaviour
{
    private FootballBlackboard _blackboard;
    private BehaviorTree.BehaviorTree _tree; // 指明是由于我们自定义的类

    void Awake()
    {
        // 1. 【源头】创建唯一的黑板实例
        _blackboard = new FootballBlackboard();
        _blackboard.Owner = this.gameObject; // 记录自己是谁

        // 2. 创建树，把黑板传进去
        _tree = new BehaviorTree.BehaviorTree(_blackboard);

        // 3. 构建行为树结构 (这里是关键的引用传递！)
        // 注意：我们在创建 Node 时，把 _blackboard 传进去
        Node root = BuildTree(_blackboard); 
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
        
        // 目前我们没写复杂的带球/射门逻辑。
        // 所以，如果有球，我们简单地让他停在原地 (或者你可以让他慢速盘带)
        // 这里为了简单，如果 checkHasBall 成功，Sequence 就会结束并返回 Success。
        // 这意味着：持球人会站着不动。
        SequenceNode hasBallSequence = new SequenceNode(_blackboard, new List<Node> 
        { 
            checkHasBall 
            // 可以在这里加一个 TaskStop(_blackboard) 
        });


        // ---------------------------------------------
        // 分支 B: 如果我没球 (Support Logic)
        // ---------------------------------------------
        // 1. 计算最佳接应点 (大脑)
        Node calcSupportSpot = new TaskCalculateSupportSpot(_blackboard);
        // 2. 跑到那个点 (大腿)
        Node moveToSpot = new TaskMoveToPosition(_blackboard);

        SequenceNode supportSequence = new SequenceNode(_blackboard, new List<Node> 
        { 
            calcSupportSpot, 
            moveToSpot 
        });


        // ---------------------------------------------
        // 根节点: 选择器 (Selector)
        // ---------------------------------------------
        // 逻辑：优先看是不是有球？ -> 是，执行A。 -> 否，执行B (跑位)。
        SelectorNode root = new SelectorNode(_blackboard, new List<Node> 
        { 
            hasBallSequence, 
            supportSequence 
        });

        return root;
    }
    
    // 调试用：在 Scene 窗口画出他想去哪
    void OnDrawGizmos()
    {
        if (_blackboard != null && _blackboard.MoveTarget != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _blackboard.MoveTarget);
            Gizmos.DrawWireSphere(_blackboard.MoveTarget, 0.5f);
        }
    }
    // 这是一个工厂方法，负责组装具体的逻辑
    private Node BuildTree(FootballBlackboard bb)
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