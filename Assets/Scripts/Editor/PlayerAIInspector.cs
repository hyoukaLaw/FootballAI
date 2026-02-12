using BehaviorTree.Runtime;
using UnityEditor;
using UnityEngine;
using FootballAI.FootballCore;

[CustomEditor(typeof(PlayerAI))]
public class PlayerAIInspector : Editor
{
    private string _matchContextInfo = "No Match Context";

    public override void OnInspectorGUI()
    {
        PlayerAI playerAI = (PlayerAI)target;

        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("调试信息", EditorStyles.boldLabel);

        // ExecutionPathField(playerAI);
        MatchContextField(playerAI);
        BlackboardField(playerAI);
    }

    private void ExecutionPathField(PlayerAI playerAI)
    {
        EditorGUILayout.LabelField("执行路径 (Execution Path)");
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.TextArea(playerAI.ExecutionPath, GUILayout.Height(60));
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private void MatchContextField(PlayerAI playerAI)
    {
        var blackboard = GetBlackboard(playerAI);
        if (blackboard?.MatchContext == null)
        {
            EditorGUILayout.HelpBox("MatchContext 为空", MessageType.Warning);
            return;
        }

        var context = blackboard.MatchContext;

        EditorGUILayout.LabelField("MatchContext 信息", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"球 (Ball): {(context.Ball != null ? context.Ball.name : "None")}");
        sb.AppendLine($"持球人 (BallHolder): {(context.GetBallHolder() != null ? context.GetBallHolder().name : "None")}");
        sb.AppendLine($"传球目标 (IncomingPassTarget): {(context.IncomingPassTarget != null ? context.IncomingPassTarget.name : "None")}");
        sb.AppendLine($"抢断保护期 (StealCooldown): {context.IsInStealCooldown}");
        sb.AppendLine($"红队人数: {context.TeamRedPlayers?.Count ?? 0}");
        sb.AppendLine($"蓝队人数: {context.TeamBluePlayers?.Count ?? 0}");
        sb.AppendLine($"红队球门: {(context.RedGoal != null ? context.RedGoal.position.ToString() : "None")}");
        sb.AppendLine($"蓝队球门: {(context.BlueGoal != null ? context.BlueGoal.position.ToString() : "None")}");
        sb.AppendLine($"球场边界: Left={context.GetLeftBorder():F1}, Right={context.GetRightBorder():F1}, Forward={context.GetForwardBorder():F1}, Backward={context.GetBackwardBorder():F1}");

        _matchContextInfo = sb.ToString();
        EditorGUILayout.TextArea(_matchContextInfo, GUILayout.Height(180));
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private void BlackboardField(PlayerAI playerAI)
    {
        var blackboard = GetBlackboard(playerAI);
        if (blackboard == null)
        {
            EditorGUILayout.HelpBox("Blackboard 为空", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("Blackboard 信息", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"所属球员 (Owner): {(blackboard.Owner != null ? blackboard.Owner.name : "None")}");
        sb.AppendLine($"移动目标 (MoveTarget): {FormatVector3(blackboard.MoveTarget)}");
        sb.AppendLine($"传球目标 (BestPassTarget): {(blackboard.BestPassTarget != null ? blackboard.BestPassTarget.name : "None")}");
        sb.AppendLine($"可射门 (CanShoot): {blackboard.CanShoot}");
        sb.AppendLine($"盯防对象 (MarkedPlayer): {(blackboard.MarkedPlayer != null ? blackboard.MarkedPlayer.name : "None")}");
        sb.AppendLine($"防守位置 (DefensePosition): {FormatVector3(blackboard.DefensePosition)}");
        sb.AppendLine($"眩晕状态 (IsStunned): {blackboard.IsStunned}");
        sb.AppendLine($"眩晕计时器 (StunTimer): {blackboard.StunTimer:F2}s");
        sb.AppendLine($"晕眩时长 (StunDuration): {blackboard.StunDuration}s");

        EditorGUILayout.TextArea(sb.ToString(), GUILayout.Height(150));
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private FootballBlackboard GetBlackboard(PlayerAI playerAI)
    {
        var method = typeof(PlayerAI).GetMethod("GetBlackboard", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (method != null)
        {
            return method.Invoke(playerAI, null) as FootballBlackboard;
        }
        return null;
    }

    private string FormatVector3(Vector3 v)
    {
        if (v == Vector3.zero) return "Zero";
        return $"({v.x:F2}, {v.y:F2}, {v.z:F2})";
    }
}
