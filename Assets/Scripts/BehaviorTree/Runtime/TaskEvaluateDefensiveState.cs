using UnityEngine;

namespace BehaviorTree.Runtime
{
    public class TaskEvaluateDefensiveState : Node
    {
        public TaskEvaluateDefensiveState(FootballBlackboard bb) : base(bb) { }

        public override NodeState Evaluate()
        {

            // 如果球是无主的（Loose Ball），防守逻辑暂时不处理，交给通用的抢球逻辑
            if (Blackboard.MatchContext.BallHolder == null)
            {
                return NodeState.FAILURE;
            }

            GameObject owner = Blackboard.Owner;
            GameObject ballHolder = Blackboard.MatchContext.BallHolder;

            // 1. 判断是否需要"施压" (Pressing)
            // 逻辑：如果我是离持球人最近的队友，我就负责去抢球
            if (IsClosestTeammateToTarget(ballHolder.transform.position) || IsLastDefensePlayer())
            {
                // 决策：去抢球！
                Debug.Log($"{Blackboard.Owner.name} 防守选择：施压 IsClosestTeammateToTarget: " +
                          $"{IsClosestTeammateToTarget(ballHolder.transform.position)}, IsLastDefensePlayer: {IsLastDefensePlayer()}");
                // 将移动目标设为持球人位置
                Vector3 tackleTarget = ballHolder.transform.position;
                Blackboard.MoveTarget = Blackboard.Owner.transform.position + (tackleTarget - Blackboard.Owner.transform.position).normalized * FootballConstants.DecideMinStep;
                Blackboard.MarkedPlayer = null; // 不需要盯无球人

                // 返回 SUCCESS 表示我们做出了决策：去施压
                return NodeState.SUCCESS;
            }

            // 2. 如果不需要施压，则执行"盯人" (Marking)
            // 逻辑：找到离我最近的无球敌人，作为我的盯防对象
            GameObject bestTarget = FindClosestEnemyToMark(owner, ballHolder);

            if (bestTarget != null)
            {
                Debug.Log($"{Blackboard.Owner.name} 防守选择：盯人 {bestTarget.name}");
                Blackboard.MarkedPlayer = bestTarget;
                

                // 计算盯防站位：站在"敌人"和"我方球门"之间
                Vector3 targetPos = bestTarget.transform.position;
                Vector3 ballPos = Blackboard.MatchContext.Ball.transform.position;

                // 站位策略：站在敌人和球连线的 20% 处（靠近敌人，阻断接球）
                Vector3 idealPos = targetPos + (ballPos - targetPos).normalized;
                Blackboard.MoveTarget = Blackboard.Owner.transform.position + (idealPos - Blackboard.Owner.transform.position).normalized * FootballConstants.DecideMinStep;
                Blackboard.MoveTarget = TeamPositionUtils.FindUnoccupiedPosition(owner, Blackboard.MoveTarget,
                    Blackboard.MatchContext.GetTeammates(owner), Blackboard.MatchContext.GetOpponents(owner));
                return NodeState.SUCCESS;
            }
            Debug.Log($"{Blackboard.Owner.name} TaskEvaluateDefensiveState: Failure");
            return NodeState.FAILURE;
        }


        // 辅助：我是不是离目标点最近的队友？
        private bool IsClosestTeammateToTarget(Vector3 targetPos)
        {
            if (Blackboard.MatchContext == null) return false;

            float myDist = Vector3.Distance(Blackboard.Owner.transform.position, targetPos);
            var teammates = Blackboard.MatchContext.GetTeammates(Blackboard.Owner);
            if (teammates == null) return false;

            foreach (var mate in teammates)
            {
                if (mate == Blackboard.Owner) continue;
                if (Vector3.Distance(mate.transform.position, targetPos) < myDist - 0.5f) // 0.5f 容错
                {
                    return false;
                }
            }
            return true;
        }

        // 是防线上的最后一人，也应该上前拦截
        private bool IsLastDefensePlayer()
        {
            if (Blackboard.MatchContext == null) return false;
            bool isLastDefense = true;
            var teammates = Blackboard.MatchContext.GetTeammates(Blackboard.Owner);
            float goalPosX = Blackboard.MatchContext.GetMyGoalPosition(Blackboard.Owner).x;
            foreach (var mate in teammates)
            {
                if (mate == Blackboard.Owner) continue;
                if (Mathf.Abs(mate.transform.position.x - goalPosX) < 
                    Mathf.Abs(Blackboard.Owner.transform.position.x - goalPosX))
                {
                    isLastDefense = false;
                    break;
                }
            }
            return isLastDefense ;
        }

        // 辅助：找个没人盯的或者离我最近的无球敌人
        private GameObject FindClosestEnemyToMark(GameObject me, GameObject ballHolder)
        {
            if (Blackboard.MatchContext == null) return null;

            GameObject bestTarget = null;
            float closestDist = float.MaxValue;

            var opponents = Blackboard.MatchContext.GetOpponents(Blackboard.Owner);
            if (opponents == null) return null;

            foreach (var enemy in opponents)
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