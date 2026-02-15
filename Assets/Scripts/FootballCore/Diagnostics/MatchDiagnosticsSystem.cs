using System.Collections.Generic;
using BehaviorTree.Runtime;
using UnityEngine;

namespace FootballAI.FootballCore
{
public class MatchDiagnosticsSystem
{
    private float _blueOverlapDiagnosticsTimer;

    #region 日志
    public void LogZoneInfo(MatchContext context, List<GameObject> teamRedPlayers)
    {
        if (context == null || teamRedPlayers == null || teamRedPlayers.Count == 0 || teamRedPlayers[0] == null)
            return;
        GameObject referencePlayer = teamRedPlayers[0];
        for (int i = 0; i < System.Enum.GetValues(typeof(FieldZone)).Length; i++)
        {
            FieldZone fieldZone = (FieldZone)i;
            ZoneUtils.ZoneRange zoneRange = ZoneUtils.GetZoneRange(fieldZone,
                context.GetEnemyGoalPosition(referencePlayer), context.GetMyGoalPosition(referencePlayer));
            MyLog.LogInfo($"fieldZone: {fieldZone} {zoneRange.LeftBottom} {zoneRange.Width} {zoneRange.Length}");
        }
    }

    public void UpdateBlueOverlapDiagnostics(List<GameObject> teamBluePlayers)
    {
        if (!RuntimeDebugSettings.EnableBlueOverlapDiagnostics)
            return;
        _blueOverlapDiagnosticsTimer += TimeManager.Instance.GetDeltaTime();
        if (_blueOverlapDiagnosticsTimer < RuntimeDebugSettings.BlueOverlapDiagnosticInterval)
            return;
        _blueOverlapDiagnosticsTimer = 0f;
        const float minDistance = 0.5f;
        for (int i = 0; i < teamBluePlayers.Count; i++)
        {
            GameObject playerA = teamBluePlayers[i];
            if (playerA == null)
                continue;
            for (int j = i + 1; j < teamBluePlayers.Count; j++)
            {
                GameObject playerB = teamBluePlayers[j];
                if (playerB == null)
                    continue;
                float distance = Vector3.Distance(playerA.transform.position, playerB.transform.position);
                if (distance < minDistance)
                    MyLog.LogError($"[BlueOverlap] {playerA.name} and {playerB.name} distance={distance:F3} (< {minDistance:F1})");
            }
        }
    }
    #endregion
}
}
