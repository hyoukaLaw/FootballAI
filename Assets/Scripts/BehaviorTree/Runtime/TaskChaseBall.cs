using UnityEngine;

namespace BehaviorTree.Runtime
{
    public class TaskChaseBall : ActionNode
    {
        public TaskChaseBall(FootballBlackboard bb) : base(bb) { }

        public override NodeState Evaluate()
        {
            if (Blackboard.MatchContext == null || Blackboard.MatchContext.Ball == null)
                return NodeState.FAILURE;
            Vector3 ballPos = Blackboard.MatchContext.Ball.transform.position;
            // 核心逻辑：把要去的地方，设为球当前的位置
            // 这样 TaskMoveToPosition 就会让你直接跑向球
            Blackboard.MoveTarget = Blackboard.Owner.transform.position +
                                    (ballPos - Blackboard.Owner.transform.position).normalized * FootballConstants.DecideMinStep;
            //Debug.Log($"chaseball: {Blackboard.MoveTarget} {Blackboard.MatchContext.Ball.transform.position}");
            // 简单的预测优化（可选）：
            // 如果你想"迎球"迎得更准，可以加上球的速度预测，跑向球未来 0.5秒 的位置
            // BallController ballCtrl = Blackboard.MatchContext.Ball.GetComponent<BallController>();
            // if (ballCtrl != null) ...

            return NodeState.SUCCESS;
        }
    }
}