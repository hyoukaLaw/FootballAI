using UnityEngine;
using FootballAI.FootballCore;

namespace BehaviorTree.Runtime
{
public static class ZoneUtils
{
    public class ZoneRange
    {
        public Vector3 LeftBottom;
        public float Width;
        public float Length;
    }

    public static float CalculateZonePreferenceScoreV2(Vector3 position, PlayerRole role, Vector3 myGoal,
        Vector3 enemyGoal, MatchState currentState, MatchContext context, GameObject player)
    {
        RolePreferences preferences = GetPreferencesForState(role, currentState);
        if (!TryGetZoneByPosition(context, position, out FormationZoneRect zone))
            return 0f;
        return preferences.GetZoneWeightById(zone.ZoneId);
    }

    public static float CalculateNormalizedZoneScore(Vector3 position, PlayerRole role, Vector3 myGoal,
        Vector3 enemyGoal, MatchState currentState, MatchContext context, GameObject player)
    {
        RolePreferences preferences = GetPreferencesForState(role, currentState);
        float maxWeight = GetMaxWeight(preferences);
        if (maxWeight <= FootballConstants.FloatEpsilon)
            return 0f;
        return CalculateZonePreferenceScoreV2(position, role, myGoal, enemyGoal, currentState, context, player) / maxWeight;
    }

    public static bool TryGetPreferredZoneRangeFromFormation(MatchContext context, GameObject player,
        RolePreferences preferences, out ZoneRange zoneRange)
    {
        zoneRange = null;
        if (!TryGetPreferredZone(preferences, context, out FormationZoneRect zone))
            return false;
        zoneRange = BuildZoneRange(zone);
        return zoneRange != null;
    }

    public static bool TryGetZoneByPosition(MatchContext context, Vector3 position, out FormationZoneRect zone)
    {
        zone = null;
        int bestPriority = int.MinValue;
        for (int i = 0; i < context.FormationLayout.Zones.Count; i++)
        {
            FormationZoneRect candidate = context.FormationLayout.Zones[i];
            if ( !candidate.IsEnabled || !IsInZoneRect(position, candidate)) 
                continue;
            if (zone == null || candidate.Priority > bestPriority)
            {
                zone = candidate;
                bestPriority = candidate.Priority;
            }
        }
        return zone != null;
    }

    public static bool IsPositionInRange(Vector3 position, ZoneRange zoneRange)
    {
        return position.x >= zoneRange.LeftBottom.x && position.x <= zoneRange.LeftBottom.x + zoneRange.Width
               && position.z >= zoneRange.LeftBottom.z && position.z <= zoneRange.LeftBottom.z + zoneRange.Length;
    }

    public static ZoneRange BuildFullFieldZoneRange(MatchContext context)
    {
        return new ZoneRange
        {
            LeftBottom = new Vector3(context.GetLeftBorder(), 0f, context.GetBackwardBorder()),
            Width = context.GetFieldWidth(),
            Length = context.GetFieldLength()
        };
    }

    public static bool IsInPenaltyArea(Vector3 position, Vector3 goalPosition)
    {
        MatchContext context = MatchManager.Instance != null ? MatchManager.Instance.Context : null;
        if (!TryGetPenaltyZoneRange(context, goalPosition, out ZoneRange zoneRange))
            return false;
        return IsPositionInRange(position, zoneRange);
    }

    private static RolePreferences GetPreferencesForState(PlayerRole role, MatchState state)
    {
        switch (state)
        {
            case MatchState.Attacking: return role.AttackPreferences;
            case MatchState.Defending: return role.DefendPreferences;
            case MatchState.ChasingBall: return role.ChaseBallPreferences;
            default: return role.AttackPreferences;
        }
    }

    private static float GetMaxWeight(RolePreferences preferences)
    {
        (string _, float weight) = preferences.GetHighestZoneWeight();
        return weight;
    }

    private static bool TryGetPreferredZone(RolePreferences preferences, MatchContext context, out FormationZoneRect zone)
    {
        zone = null;
        (string zoneId, float _) = preferences.GetHighestZoneWeight();
        if (string.IsNullOrEmpty(zoneId))
            return false;
        for (int i = 0; i < context.FormationLayout.Zones.Count; i++)
        {
            FormationZoneRect candidate = context.FormationLayout.Zones[i];
            if (!candidate.IsEnabled || candidate.ZoneId != zoneId)
                continue;
            zone = candidate;
            return true;
        }
        return false;
    }

    private static bool IsInZoneRect(Vector3 position, FormationZoneRect zone)
    {
        float halfWidth = Mathf.Max(0.1f, zone.SizeXZ.x) * 0.5f;
        float halfLength = Mathf.Max(0.1f, zone.SizeXZ.y) * 0.5f;
        float minX = zone.CenterXZ.x - halfWidth;
        float maxX = zone.CenterXZ.x + halfWidth;
        float minZ = zone.CenterXZ.y - halfLength;
        float maxZ = zone.CenterXZ.y + halfLength;
        return position.x >= minX && position.x <= maxX && position.z >= minZ && position.z <= maxZ;
    }

    private static ZoneRange BuildZoneRange(FormationZoneRect zone)
    {
        float width = Mathf.Max(0.1f, zone.SizeXZ.x);
        float length = Mathf.Max(0.1f, zone.SizeXZ.y);
        Vector3 leftBottom = new Vector3(zone.CenterXZ.x - width * 0.5f, 0f, zone.CenterXZ.y - length * 0.5f);
        return new ZoneRange{LeftBottom = leftBottom, Width = width, Length = length};
    }

    private static bool TryGetPenaltyZoneRange(MatchContext context, Vector3 goalPosition, out ZoneRange zoneRange)
    {
        zoneRange = null;
        string zoneId = goalPosition.z < 0f ? "penalty_backward" : "penalty_forward";
        for (int i = 0; i < context.FieldSpecialZonesConfig.Zones.Count; i++)
        {
            FormationZoneRect zone = context.FieldSpecialZonesConfig.Zones[i];
            if (!zone.IsEnabled || zone.ZoneId != zoneId)
                continue;
            zoneRange = BuildZoneRange(zone);
            return zoneRange != null;
        }
        return false;
    }
}
}
