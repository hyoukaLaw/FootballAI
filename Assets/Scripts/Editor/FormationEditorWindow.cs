using System.Collections.Generic;
using FootballAI.FootballCore;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace FootballAI.Editor
{
public class FormationEditorWindow : OdinEditorWindow
{
    #region 常量与字段
    private const float ZoneLabelYOffset = 0.1f;
    private readonly BoxBoundsHandle _zoneBoundsHandle = new BoxBoundsHandle();
    #endregion
    
    #region 面板数据
    [MenuItem("Tools/FootballAI/Formation Editor")]
    private static void OpenWindow()
    {
        FormationEditorWindow window = GetWindow<FormationEditorWindow>("Formation Editor");
        window.minSize = new Vector2(520f, 640f);
        window.Show();
    }

    [ShowInInspector]
    [LabelText("Formation Layout")]
    [OnValueChanged(nameof(OnLayoutChanged))]
    [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
    public FormationLayout FormationLayout;

    [ShowInInspector]
    [LabelText("Selected Zone")]
    [ValueDropdown(nameof(GetZoneIndexOptions))]
    [OnValueChanged(nameof(OnSelectedZoneIndexChanged))]
    public int SelectedZoneIndex = -1;

    [PropertySpace]
    [ShowInInspector]
    [ShowIf(nameof(HasSelectedZone))]
    [InlineProperty]
    [HideLabel]
    [OnValueChanged(nameof(OnSelectedZoneEdited))]
    public FormationZoneRect SelectedZone
    {
        get
        {
            if (!HasSelectedZone())
                return null;
            return FormationLayout.GetZoneAt(SelectedZoneIndex);
        }
    }
    #endregion

    #region 生命周期
    protected override void OnEnable()
    {
        base.OnEnable();
        SceneView.duringSceneGui += OnSceneGUI;
    }

    protected override void OnDestroy()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        base.OnDestroy();
    }
    #endregion

    #region 按钮动作
    [PropertyOrder(-4)]
    [Button(ButtonSizes.Medium)]
    private void CreateLayoutAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject("Create Formation Layout", "FormationLayout", "asset",
            "Select location for new FormationLayout asset");
        if (string.IsNullOrEmpty(path))
            return;
        FormationLayout asset = CreateInstance<FormationLayout>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        FormationLayout = asset;
        SelectedZoneIndex = -1;
        Selection.activeObject = asset;
        Repaint();
        SceneView.RepaintAll();
    }

    [PropertyOrder(-3)]
    [Button(ButtonSizes.Medium)]
    [EnableIf(nameof(HasLayout))]
    private void AddZone()
    {
        Undo.RecordObject(FormationLayout, "Add Zone");
        int nextIndex = FormationLayout.GetZoneCount() + 1;
        FormationZoneRect zone = new FormationZoneRect
        {
            ZoneId = $"zone_{nextIndex:00}",
            DisplayName = $"Zone {nextIndex:00}",
            IsEnabled = true,
            Priority = 0,
            ZoneColor = new Color(Random.Range(0.15f, 0.95f), Random.Range(0.15f, 0.95f), Random.Range(0.15f, 0.95f), 0.25f),
            CenterXZ = Vector2.zero,
            SizeXZ = new Vector2(8f, 12f)
        };
        FormationLayout.AddZone(zone);
        SelectedZoneIndex = FormationLayout.GetZoneCount() - 1;
        MarkLayoutDirty();
    }

    [PropertyOrder(-2)]
    [Button(ButtonSizes.Medium)]
    [EnableIf(nameof(HasSelectedZone))]
    private void DuplicateSelectedZone()
    {
        FormationZoneRect source = FormationLayout.GetZoneAt(SelectedZoneIndex);
        if (source == null)
            return;
        Undo.RecordObject(FormationLayout, "Duplicate Zone");
        FormationZoneRect duplicate = new FormationZoneRect
        {
            ZoneId = source.ZoneId + "_copy",
            DisplayName = source.DisplayName + " Copy",
            IsEnabled = source.IsEnabled,
            Priority = source.Priority,
            ZoneColor = source.ZoneColor,
            CenterXZ = source.CenterXZ + new Vector2(1f, 1f),
            SizeXZ = source.SizeXZ
        };
        FormationLayout.AddZone(duplicate);
        SelectedZoneIndex = FormationLayout.GetZoneCount() - 1;
        MarkLayoutDirty();
    }

    [PropertyOrder(-1)]
    [Button(ButtonSizes.Medium)]
    [EnableIf(nameof(HasSelectedZone))]
    private void RemoveSelectedZone()
    {
        if (!HasSelectedZone())
            return;
        Undo.RecordObject(FormationLayout, "Remove Zone");
        FormationLayout.RemoveZoneAt(SelectedZoneIndex);
        if (SelectedZoneIndex >= FormationLayout.GetZoneCount())
            SelectedZoneIndex = FormationLayout.GetZoneCount() - 1;
        MarkLayoutDirty();
    }
    #endregion

    #region 面板回调
    private void OnLayoutChanged()
    {
        SelectedZoneIndex = -1;
        SceneView.RepaintAll();
        Repaint();
    }

    private void OnSelectedZoneIndexChanged()
    {
        SceneView.RepaintAll();
        Repaint();
    }

    private void OnSelectedZoneEdited()
    {
        MarkLayoutDirty();
    }
    #endregion

    #region 面板数据源
    private IEnumerable<ValueDropdownItem<int>> GetZoneIndexOptions()
    {
        yield return new ValueDropdownItem<int>("None", -1);
        if (!HasLayout())
            yield break;
        for (int i = 0; i < FormationLayout.GetZoneCount(); i++)
        {
            FormationZoneRect zone = FormationLayout.GetZoneAt(i);
            if (zone == null)
                continue;
            string label = $"{i}: {zone.DisplayName} ({zone.ZoneId})";
            yield return new ValueDropdownItem<int>(label, i);
        }
    }
    #endregion

    #region 场景交互
    private bool HasLayout()
    {
        return FormationLayout != null;
    }

    private bool HasSelectedZone()
    {
        return HasLayout() && SelectedZoneIndex >= 0 && SelectedZoneIndex < FormationLayout.GetZoneCount() && FormationLayout.GetZoneAt(SelectedZoneIndex) != null;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!HasLayout())
            return;
        for (int i = 0; i < FormationLayout.GetZoneCount(); i++)
        {
            FormationZoneRect zone = FormationLayout.GetZoneAt(i);
            if (zone == null || !zone.IsEnabled)
                continue;
            DrawZoneGizmo(zone, i == SelectedZoneIndex);
        }
        if (HasSelectedZone())
            DrawSelectedZoneHandles(FormationLayout.GetZoneAt(SelectedZoneIndex));
    }

    private void DrawZoneGizmo(FormationZoneRect zone, bool isSelected)
    {
        Color fillColor = zone.ZoneColor;
        fillColor.a = isSelected ? 0.35f : Mathf.Min(0.18f, zone.ZoneColor.a);
        Color outlineColor = isSelected ? Color.yellow : new Color(zone.ZoneColor.r, zone.ZoneColor.g, zone.ZoneColor.b, 1f);
        Vector3[] corners = zone.GetRectangleWorldCorners();
        Handles.DrawSolidRectangleWithOutline(corners, fillColor, outlineColor);
        Handles.color = outlineColor;
        Vector3 labelPos = zone.GetCenterWorld() + Vector3.up * ZoneLabelYOffset;
        Handles.Label(labelPos, $"{zone.DisplayName} [{zone.ZoneId}]", EditorStyles.whiteLabel);
    }

    private void DrawSelectedZoneHandles(FormationZoneRect zone)
    {
        Handles.color = Color.yellow;
        Vector3 center = zone.GetCenterWorld();
        Vector3 size = new Vector3(Mathf.Max(0.1f, zone.SizeXZ.x), 0.01f, Mathf.Max(0.1f, zone.SizeXZ.y));
        _zoneBoundsHandle.center = center;
        _zoneBoundsHandle.size = size;
        EditorGUI.BeginChangeCheck();
        _zoneBoundsHandle.DrawHandle();
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(FormationLayout, "Edit Zone Bounds");
            zone.SetCenterFromWorld(_zoneBoundsHandle.center);
            zone.SetSizeFromWorld(new Vector3(_zoneBoundsHandle.size.x, 0f, _zoneBoundsHandle.size.z));
            MarkLayoutDirty();
        }
    }
    #endregion

    #region 工具方法
    private void MarkLayoutDirty()
    {
        if (!HasLayout())
            return;
        EditorUtility.SetDirty(FormationLayout);
        SceneView.RepaintAll();
        Repaint();
    }
    #endregion
}
}
