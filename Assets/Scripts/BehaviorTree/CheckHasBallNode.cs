using UnityEngine; // 为了使用 GameObject 比较

namespace BehaviorTree
{
    public class CheckHasBallNode : Node
    {
        // 构造函数透传黑板引用
        public CheckHasBallNode(FootballBlackboard blackboard) : base(blackboard)
        {
        }

        public override NodeState Evaluate()
        {
            // 防御性编程：如果上下文或黑板数据没准备好，默认为失败
            if (Blackboard.MatchContext == null ||
                Blackboard.MatchContext.BallHolder == null ||
                Blackboard.Owner == null)
            {
                NodeState = NodeState.FAILURE;
                return NodeState;
            }

            // 核心逻辑：
            // 比较 "全场谁拿着球" (BallHolder) 和 "我是谁" (Owner)
            // 这里的 Owner 是在 PlayerAI 初始化黑板时赋值的自身 GameObject
            if (Blackboard.MatchContext.BallHolder == Blackboard.Owner)
            {
                NodeState = NodeState.SUCCESS;
                return NodeState;
            }
            else
            {
                NodeState = NodeState.FAILURE;
                return NodeState;
            }
        }
    }
}