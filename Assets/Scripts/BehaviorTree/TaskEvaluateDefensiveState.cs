using UnityEngine;

namespace BehaviorTree
{
    public class TaskEvaluateDefensiveState : Node
    {
        public TaskEvaluateDefensiveState(FootballBlackboard bb) : base(bb) { }

        public override NodeState Evaluate()
        {
            // 如果球是无主的（Loose Ball），防守逻辑暂时不处理，交给通用的抢球逻辑
            if (Blackboard.BallHolder == null)
            {
                return NodeState.FAILURE;
            }

            GameObject owner = Blackboard.Owner;
            GameObject ballHolder = Blackboard.BallHolder;

            // 1. 判断是否需要“施压” (Pressing)
            // 逻辑：如果我是离持球人最近的队友，我就负责去抢球
            if (IsClosestTeammateToTarget(ballHolder.transform.position))
            {
                // 决策：去抢球！
                // 将移动目标设为持球人位置
                Blackboard.MoveTarget = ballHolder.transform.position;
                Blackboard.MarkedPlayer = null; // 不需要盯无球人
                
                // 返回 SUCCESS 表示我们做出了决策：去施压
                Debug.Log($"{Blackboard.Owner.name} 抢球");
                return NodeState.SUCCESS;
            }

            // 2. 如果不需要施压，则执行“盯人” (Marking)
            // 逻辑：找到离我最近的无球敌人，作为我的盯防对象
            GameObject bestTarget = FindClosestEnemyToMark(owner, ballHolder);
            
            if (bestTarget != null)
            {
                Blackboard.MarkedPlayer = bestTarget;

                // 计算盯防站位：站在“敌人”和“我方球门”之间
                Vector3 targetPos = bestTarget.transform.position;
                Vector3 ballPos = Blackboard.Ball.transform.position;
                
                // 站位策略：站在敌人和球连线的 20% 处（靠近敌人，阻断接球）
                Vector3 idealPos = targetPos + (ballPos - targetPos).normalized * 1.5f;
                
                Blackboard.MoveTarget = idealPos;
                return NodeState.SUCCESS;
            }

            return NodeState.FAILURE;
        }

        // 辅助：我是不是离目标点最近的队友？
        private bool IsClosestTeammateToTarget(Vector3 targetPos)
        {
            float myDist = Vector3.Distance(Blackboard.Owner.transform.position, targetPos);
            foreach (var mate in Blackboard.Teammates)
            {
                if (mate == Blackboard.Owner) continue;
                if (Vector3.Distance(mate.transform.position, targetPos) < myDist - 0.5f) // 0.5f 容错
                {
                    return false;
                }
            }
            return true;
        }

        // 辅助：找个没人盯的或者离我最近的无球敌人
        private GameObject FindClosestEnemyToMark(GameObject me, GameObject ballHolder)
        {
            GameObject bestTarget = null;
            float closestDist = float.MaxValue;

            foreach (var enemy in Blackboard.Opponents)
            {
                if (enemy == ballHolder) continue; // 不盯持球人，持球人由施压者负责

                float d = Vector3.Distance(me.transform.position, enemy.transform.position);
                if (d < closestDist)
                {
                    closestDist = d;
                    bestTarget = enemy;
                }
            }
            return bestTarget;
        }
    }
}