using UnityEngine;
using UnityEditor;
using BehaviorTree.Runtime;

namespace BehaviorTree.Editor
{
    public class RolePresetGenerator : EditorWindow
    {
        [MenuItem("临时工具/Generate Role Presets")]
        public static void ShowWindow()
        {
            GetWindow<RolePresetGenerator>("Role Presets Generator");
        }

        void OnGUI()
        {
            GUILayout.Label("角色预设生成器", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            GUILayout.Label("点击下方按钮生成三个角色预设资产：", EditorStyles.helpBox);
            EditorGUILayout.Space();

            if (GUILayout.Button("生成所有角色预设", GUILayout.Height(40)))
            {
                GenerateAllPresets();
            }

            EditorGUILayout.Space();
            GUILayout.Label("将在以下路径生成资产：", EditorStyles.miniLabel);
            GUILayout.Label("Assets/Resources/Roles/", EditorStyles.miniLabel);
        }

        private static void GenerateAllPresets()
        {
            string folderPath = "Assets/Resources/Roles";
            System.IO.Directory.CreateDirectory(folderPath);

            GenerateForwardPreset(folderPath);
            GenerateMidfielderPreset(folderPath);
            GenerateDefenderPreset(folderPath);

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("完成", "已成功生成三个角色预设资产！", "确定");
        }

        private static void GenerateForwardPreset(string folderPath)
        {
            PlayerRole preset = ScriptableObject.CreateInstance<PlayerRole>();
            SetUpForwardPreset(preset);
            string assetPath = $"{folderPath}/ForwardRole.asset";
            AssetDatabase.CreateAsset(preset, assetPath);
            AssetDatabase.SaveAssets();

            MyLog.LogInfo($"已生成前锋角色预设: {assetPath}");
        }

        private static void GenerateMidfielderPreset(string folderPath)
        {
            PlayerRole preset = ScriptableObject.CreateInstance<PlayerRole>();
            SetUpMidfielderPreset(preset);
            string assetPath = $"{folderPath}/MidfielderRole.asset";
            AssetDatabase.CreateAsset(preset, assetPath);
            AssetDatabase.SaveAssets();

            MyLog.LogInfo($"已生成中场角色预设: {assetPath}");
        }

        private static void GenerateDefenderPreset(string folderPath)
        {
            PlayerRole preset = ScriptableObject.CreateInstance<PlayerRole>();
            SetUpDefenderPreset(preset);
            string assetPath = $"{folderPath}/DefenderRole.asset";
            AssetDatabase.CreateAsset(preset, assetPath);
            AssetDatabase.SaveAssets();

            MyLog.LogInfo($"已生成后卫角色预设: {assetPath}");
        }
        
        private static void SetUpForwardPreset(PlayerRole preset)
        {
            preset.RoleType = PlayerRoleType.Forward;
            preset.RoleName = "Forward";

            preset.AttackPreferences = new RolePreferences
            {
                OwnDefensiveZoneWeight = 0.5f,
                OwnOffensiveZoneWeight = 2.0f,
                EnemyOffensiveZoneWeight = 6.0f,
                EnemyDefensiveZoneWeight = 10.0f,
                DistanceDecayRate = 0.15f,
                MaxZoneDeviation = 10f
            };
            preset.DefendPreferences = new RolePreferences
            {
                OwnDefensiveZoneWeight = 1.0f,
                OwnOffensiveZoneWeight = 3.0f,
                EnemyOffensiveZoneWeight = 4.0f,
                EnemyDefensiveZoneWeight = 2.0f,
                DistanceDecayRate = 0.1f,
                MaxZoneDeviation = 12f
            };
            preset.ChaseBallPreferences = new RolePreferences
            {
                OwnDefensiveZoneWeight = 1.0f,
                OwnOffensiveZoneWeight = 3.0f,
                EnemyOffensiveZoneWeight = 5.0f,
                EnemyDefensiveZoneWeight = 8.0f,
                DistanceDecayRate = 0.2f,
                MaxZoneDeviation = 15f
            };

            preset.OffensiveBias = 0.8f;
            preset.DefensiveBias = 0.2f;
            preset.SupportBias = 0.4f;
            preset.HomeZoneRadius = 8f;
            preset.MaximumRoamingDistance = 20f;
        }
        
        private static void SetUpMidfielderPreset(PlayerRole preset)
        {
            preset.RoleType = PlayerRoleType.Midfielder;
            preset.RoleName = "Midfielder";
            preset.AttackPreferences = new RolePreferences
            {
                OwnDefensiveZoneWeight = 2.0f,
                OwnOffensiveZoneWeight = 5.0f,
                EnemyOffensiveZoneWeight = 5.0f,
                EnemyDefensiveZoneWeight = 2.0f,
                DistanceDecayRate = 0.12f,
                MaxZoneDeviation = 12f
            };

            preset.DefendPreferences = new RolePreferences
            {
                OwnDefensiveZoneWeight = 4.0f,
                OwnOffensiveZoneWeight = 5.0f,
                EnemyOffensiveZoneWeight = 3.0f,
                EnemyDefensiveZoneWeight = 1.0f,
                DistanceDecayRate = 0.1f,
                MaxZoneDeviation = 15f
            };

            preset.ChaseBallPreferences = new RolePreferences
            {
                OwnDefensiveZoneWeight = 3.0f,
                OwnOffensiveZoneWeight = 5.0f,
                EnemyOffensiveZoneWeight = 5.0f,
                EnemyDefensiveZoneWeight = 2.0f,
                DistanceDecayRate = 0.15f,
                MaxZoneDeviation = 18f
            };

            preset.OffensiveBias = 0.5f;
            preset.DefensiveBias = 0.5f;
            preset.SupportBias = 0.7f;
            preset.HomeZoneRadius = 10f;
            preset.MaximumRoamingDistance = 18f;
        }
        
        private static void SetUpDefenderPreset(PlayerRole preset)
        {
            preset.RoleType = PlayerRoleType.Defender;
            preset.RoleName = "Defender";        
            preset.AttackPreferences = new RolePreferences
            {
                OwnDefensiveZoneWeight = 6.0f,
                OwnOffensiveZoneWeight = 4.0f,
                EnemyOffensiveZoneWeight = 1.5f,
                EnemyDefensiveZoneWeight = 0.5f,
                DistanceDecayRate = 0.1f,
                MaxZoneDeviation = 10f
            };

            preset.DefendPreferences = new RolePreferences
            {
                OwnDefensiveZoneWeight = 10.0f,
                OwnOffensiveZoneWeight = 3.0f,
                EnemyOffensiveZoneWeight = 0.5f,
                EnemyDefensiveZoneWeight = 0.2f,
                DistanceDecayRate = 0.08f,
                MaxZoneDeviation = 8f
            };

            preset.ChaseBallPreferences = new RolePreferences
            {
                OwnDefensiveZoneWeight = 5.0f,
                OwnOffensiveZoneWeight = 4.0f,
                EnemyOffensiveZoneWeight = 2.0f,
                EnemyDefensiveZoneWeight = 1.0f,
                DistanceDecayRate = 0.12f,
                MaxZoneDeviation = 12f
            };

            preset.OffensiveBias = 0.3f;
            preset.DefensiveBias = 0.8f;
            preset.SupportBias = 0.5f;
            preset.HomeZoneRadius = 6f;
            preset.MaximumRoamingDistance = 15f;
        }
    }
}
