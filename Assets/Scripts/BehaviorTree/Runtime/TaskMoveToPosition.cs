using UnityEngine;

namespace BehaviorTree.Runtime
{
    public class TaskMoveToPosition : ActionNode
    {
        private const float StoppingDistance = 0.1f;

        public TaskMoveToPosition(FootballBlackboard blackboard) : base(blackboard)
        {
        }

        public override NodeState Evaluate()
        {
            GameObject owner = Blackboard.Owner;
            Vector3 target = Blackboard.MoveTarget;
            if (target == Vector3.zero)
                return NodeState.FAILURE;
            float distance = Vector3.Distance(owner.transform.position, target);
            if (distance < StoppingDistance)
            {
                return NodeState.SUCCESS;
            }
            float speed = Blackboard.Stats.MovementSpeed;
            Vector3 newPos = Vector3.MoveTowards(owner.transform.position, target, speed * Time.deltaTime);
            FaceDirection(target, owner);
            owner.transform.position = ClampToField(newPos);
            return NodeState.RUNNING;
        }

        private void FaceDirection(Vector3 target, GameObject owner)
        {
            Vector3 direction = (target - owner.transform.position).normalized;
            if (direction != Vector3.zero)
            {
                owner.transform.forward = direction;
            }
        }

        private Vector3 ClampToField(Vector3 pos)
        {
            float leftBorder = Blackboard.MatchContext.GetLeftBorder();
            float rightBorder = Blackboard.MatchContext.GetRightBorder();
            float forwardBorder = Blackboard.MatchContext.GetForwardBorder();
            float backwardBorder = Blackboard.MatchContext.GetBackwardBorder();

            pos.x = Mathf.Clamp(pos.x, leftBorder, rightBorder);
            pos.z = Mathf.Clamp(pos.z, backwardBorder, forwardBorder);
            return pos;
        }
    }
}
