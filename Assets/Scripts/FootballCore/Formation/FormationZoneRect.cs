using UnityEngine;
using Sirenix.OdinInspector;

namespace FootballAI.FootballCore
{
[System.Serializable]
public class FormationZoneRect
{
    [HorizontalGroup("Id"), LabelWidth(55)]
    public string ZoneId = "zone_01";
    [HorizontalGroup("Id"), LabelWidth(85)]
    public string DisplayName = "Zone 01";
    [HorizontalGroup("Meta"), LabelWidth(55)]
    public bool IsEnabled = true;
    [HorizontalGroup("Meta"), LabelWidth(55)]
    public int Priority = 0;
    [HorizontalGroup("Meta"), LabelWidth(45)]
    public Color ZoneColor = new Color(0.2f, 0.8f, 0.9f, 0.25f);
    [HorizontalGroup("Rect"), LabelText("Center XZ")]
    public Vector2 CenterXZ = Vector2.zero;
    [HorizontalGroup("Rect"), LabelText("Size XZ"), MinValue(0.1f)]
    public Vector2 SizeXZ = new Vector2(8f, 12f);

    public Vector3 GetCenterWorld()
    {
        return new Vector3(CenterXZ.x, 0f, CenterXZ.y);
    }

    public Vector3[] GetRectangleWorldCorners()
    {
        float halfWidth = Mathf.Max(0.1f, SizeXZ.x) * 0.5f;
        float halfLength = Mathf.Max(0.1f, SizeXZ.y) * 0.5f;
        Vector3 center = GetCenterWorld();
        Vector3 leftBottom = new Vector3(center.x - halfWidth, 0f, center.z - halfLength);
        Vector3 leftTop = new Vector3(center.x - halfWidth, 0f, center.z + halfLength);
        Vector3 rightTop = new Vector3(center.x + halfWidth, 0f, center.z + halfLength);
        Vector3 rightBottom = new Vector3(center.x + halfWidth, 0f, center.z - halfLength);
        return new[] { leftBottom, leftTop, rightTop, rightBottom };
    }

    public void SetCenterFromWorld(Vector3 position)
    {
        CenterXZ = new Vector2(position.x, position.z);
    }

    public void SetSizeFromWorld(Vector3 size)
    {
        SizeXZ = new Vector2(Mathf.Max(0.1f, size.x), Mathf.Max(0.1f, size.z));
    }
}
}
