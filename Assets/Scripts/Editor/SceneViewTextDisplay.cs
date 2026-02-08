using UnityEditor;
using UnityEngine;

// 自定义编辑器脚本，用于在Scene窗口中显示文本信息
[ExecuteInEditMode]
public class SceneViewTextDisplay : MonoBehaviour
{
    // 在Editor文件夹中，这个脚本会被Unity自动识别为编辑器脚本
    // 它只会在编辑器模式下运行，不会影响游戏构建

    // 这个方法会在Scene窗口每帧绘制时调用
    [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Pickable)]
    static void DrawSceneGizmos(Transform transform, GizmoType gizmoType)
    {
        // 检查是否是Scene视图
        if (SceneView.currentDrawingSceneView != null)
        {
            // 设置文本样式
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 14;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.UpperRight;
            style.padding = new RectOffset(10, 10, 10, 10);

            // 计算文本位置（Scene窗口右上角）
            float width = 200f;
            float height = 30f;
            int i = 1;
            Rect rect = new Rect(SceneView.currentDrawingSceneView.position.width - width*i - 20, 20, width, height);
            GameObject playerGo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            // 显示文本
            string diameterText = $"Player当前直径: {playerGo.transform.lossyScale.x}";
            Handles.BeginGUI();
            GUI.Label(rect, diameterText, style);
            Handles.EndGUI();
            
            playerGo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Plane.prefab");
            // 显示文本
            string fieldText = $"球场当前长度: {playerGo.transform.lossyScale.z * 10} 宽度: {playerGo.transform.lossyScale.x * 10}";
            rect = new Rect(SceneView.currentDrawingSceneView.position.width - width*i - 20, 50, width, height);
            Handles.BeginGUI();
            GUI.Label(rect, fieldText, style);
            Handles.EndGUI();
        }
    }
}
