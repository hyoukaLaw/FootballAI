using UnityEngine;
using System.Collections.Generic;

namespace BehaviorTree.Runtime
{
    /// <summary>
    //移动避让工具类 - 简化版，只保留核心避让功能
    //负责处理球员移动过程中的避让逻辑，避免与队友重叠
    //</summary>
    public static class MovementAvoidance
    {
        [Header("避让开关")]
        public static bool EnableAvoidance = false; // 可以通过代码随时开关
        
        [Header("避让参数")]
        public static float AvoidanceDistance = 0.8f;      // 避让触发距离
        public static float AvoidanceStrength = 0.5f;      // 避让强度 (0-1)
        
        /// <summary>
        /// 应用避让机制到目标位置
        /// </summary>
        /// <param name="owner">移动的球员</param>
        /// <param name="intendedPosition">预期目标位置</param>
        /// <returns>经过避让处理后的最终位置</returns>
        public static Vector3 ApplyAvoidance(GameObject owner, Vector3 intendedPosition, List<GameObject> teammates)
        {
            if (!EnableAvoidance)
                return intendedPosition;
            // 计算避让力
            Vector3 avoidanceForce = CalculateAvoidanceForce(owner, intendedPosition, teammates);
            // 如果避让力有效，则应用
            if (avoidanceForce.magnitude > FootballConstants.FloatEpsilon)
            {
                Vector3 finalPosition = intendedPosition + avoidanceForce;
                LogAvoidanceInfo(owner, intendedPosition, finalPosition);
                return finalPosition;
            }
            return intendedPosition;
        }
        
        /// <summary>
        /// 计算避让力（只处理队友避让）
        /// </summary>
        private static Vector3 CalculateAvoidanceForce(GameObject owner, Vector3 intendedPosition, List<GameObject> teammates)
        {
            Vector3 totalForce = Vector3.zero;
            foreach (var teammate in teammates)
            {
                if (teammate == owner) continue;
                    
                totalForce += CalculateSingleAvoidanceForce(intendedPosition, owner, teammate);
            }
            
            return totalForce * AvoidanceStrength;
        }
        
        /// <summary>
        /// 计算单个队友的避让力（方案D：侧向避让）
        /// </summary>
        private static Vector3 CalculateSingleAvoidanceForce(Vector3 intendedPosition, GameObject owner, GameObject teammate)
        {
            float distance = Vector3.Distance(owner.transform.position, teammate.transform.position);
            
            // 只在避让距离内才计算
            if (distance >= AvoidanceDistance)
                return Vector3.zero;
            // 【方案D】侧向避让，垂直于到队友的连线
            Vector3 toTeammate = teammate.transform.position - owner.transform.position;
            // 计算侧向避让方向（垂直于到队友的连线）
            Vector3 lateralDirection = Vector3.Cross(Vector3.up, toTeammate).normalized;
            // 根据移动方向选择侧向避让方向
            Vector3 moveDirection = (intendedPosition - owner.transform.position).normalized;
            float alignment1 = Vector3.Dot(moveDirection, lateralDirection);
            float alignment2 = Vector3.Dot(moveDirection, -lateralDirection);
            Vector3 avoidDirection = Mathf.Abs(alignment1) > Mathf.Abs(alignment2) ? lateralDirection : -lateralDirection;
            // 避让力度：距离越近避让越强
            float strength = (1f - distance / AvoidanceDistance);
            return avoidDirection * strength;
        }
        
        /// <summary>
        /// 获取移动方向
        /// </summary>
        private static Vector3 GetOwnerMoveDirection(GameObject owner)
        {
            var ownerAI = owner.GetComponent<PlayerAI>();
            if (ownerAI != null)
            {
                var bb = ownerAI.GetBlackboard();
                if (bb != null && bb.MoveTarget != Vector3.zero)
                {
                    return (bb.MoveTarget - owner.transform.position).normalized;
                }
            }
            
            // 没有MoveTarget时，使用forward
            return owner.transform.forward;
        }
        
        /// <summary>
        /// 获取避让调试信息
        /// </summary>
        public static string GetAvoidanceDebugInfo(GameObject owner, Vector3 intendedPosition, Vector3 finalPosition)
        {
            if (!EnableAvoidance)
                return "避让机制已禁用";
                
            Vector3 avoidanceForce = finalPosition - intendedPosition;
            
            return $"避让机制: {(EnableAvoidance ? "启用" : "禁用")}\n" +
                   $"意图位置: {intendedPosition:F2}\n" +
                   $"最终位置: {finalPosition:F2}\n" +
                   $"避让力: {avoidanceForce:F3} (强度: {avoidanceForce.magnitude:F3})";
        }
        
        /// <summary>
        /// 临时调试日志（只在调试模式输出）
        /// </summary>
        public static void LogAvoidanceInfo(GameObject owner, Vector3 intendedPosition, Vector3 finalPosition)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (!EnableAvoidance) return;
                if (finalPosition != intendedPosition)
                {
                    string debugInfo = GetAvoidanceDebugInfo(owner, intendedPosition, finalPosition);
                    MyLog.LogInfo($"[{Time.frameCount}帧] {owner.name}: {debugInfo}");
                }
            #endif
        }
    }
}
