using UnityEngine;

namespace BehaviorTree.Runtime
{
    public class TaskStunWait : Node
    {
        public TaskStunWait(FootballBlackboard bb) : base(bb) { }

        public override NodeState Evaluate()
        {
            if (!Blackboard.IsStunned)
            {
                return NodeState.FAILURE;
            }

            // 更新停顿计时器
            Blackboard.StunTimer -= Time.deltaTime;

            if (Blackboard.StunTimer <= 0f)
            {
                // 停顿结束，清除状态
                Blackboard.IsStunned = false;
                Blackboard.StunTimer = 0f;
                return NodeState.SUCCESS;
            }

            // 继续停顿中
            return NodeState.RUNNING;
        }
    }
}
