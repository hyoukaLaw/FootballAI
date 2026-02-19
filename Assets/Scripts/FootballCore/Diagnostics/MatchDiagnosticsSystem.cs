using System.Collections.Generic;
using BehaviorTree.Runtime;
using UnityEngine;

namespace FootballAI.FootballCore
{
public class MatchDiagnosticsSystem
{
    private float _blueOverlapDiagnosticsTimer;

    #region 日志
    public void LogZoneInfo(MatchContext context)
    {
        if (context.FormationLayout != null && context.FormationLayout.Zones != null)
        {
            for (int i = 0; i < context.FormationLayout.Zones.Count; i++)
            {
                ZoneRect zone = context.FormationLayout.Zones[i];
                if (zone == null)
                    continue;
                MyLog.LogInfo($"formationZone: id={zone.ZoneId} name={zone.DisplayName} center={zone.CenterXZ} size={zone.SizeXZ} priority={zone.Priority} enabled={zone.IsEnabled}");
            }
        }
        for (int i = 0; i < context.FieldSpecialZonesConfig.Zones.Count; i++)
        {
            ZoneRect zone = context.FieldSpecialZonesConfig.Zones[i];
            if (zone == null)
                continue;
            MyLog.LogInfo($"specialZone: id={zone.ZoneId} name={zone.DisplayName} center={zone.CenterXZ} size={zone.SizeXZ} priority={zone.Priority} enabled={zone.IsEnabled}");
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
