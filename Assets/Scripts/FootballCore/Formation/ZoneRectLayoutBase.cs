using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FootballAI.FootballCore
{
public abstract class ZoneRectLayoutBase : ScriptableObject
{
    [LabelText("Layout Name")]
    public string LayoutName = "Default Layout";

    [TableList(AlwaysExpanded = true, DrawScrollView = true, NumberOfItemsPerPage = 12)]
    public List<FormationZoneRect> Zones = new List<FormationZoneRect>();

    public int GetZoneCount()
    {
        return Zones.Count;
    }

    public FormationZoneRect GetZoneAt(int index)
    {
        if (index < 0 || index >= Zones.Count)
            return null;
        return Zones[index];
    }

    public void AddZone(FormationZoneRect zone)
    {
        if (zone == null)
            return;
        Zones.Add(zone);
    }

    public void RemoveZoneAt(int index)
    {
        if (index < 0 || index >= Zones.Count)
            return;
        Zones.RemoveAt(index);
    }
}
}
