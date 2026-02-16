using UnityEngine;
using UnityEditor;
using BehaviorTree.Runtime;
using FootballAI.FootballCore;

namespace BehaviorTree.Editor
{
    public class RolePresetGenerator : EditorWindow
    {
        [MenuItem("Tools/FootballAI/Generate Role Presets")]
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
        }

        private static void GenerateMidfielderPreset(string folderPath)
        {
            PlayerRole preset = ScriptableObject.CreateInstance<PlayerRole>();
            SetUpMidfielderPreset(preset);
            string assetPath = $"{folderPath}/MidfielderRole.asset";
            AssetDatabase.CreateAsset(preset, assetPath);
            AssetDatabase.SaveAssets();
        }

        private static void GenerateDefenderPreset(string folderPath)
        {
            PlayerRole preset = ScriptableObject.CreateInstance<PlayerRole>();
            SetUpDefenderPreset(preset);
            string assetPath = $"{folderPath}/DefenderRole.asset";
            AssetDatabase.CreateAsset(preset, assetPath);
            AssetDatabase.SaveAssets();
        }
        
        private static void SetUpForwardPreset(PlayerRole preset)
        {
            preset.RoleType = PlayerRoleType.Forward;
            preset.RoleName = "Forward";

            preset.AttackPreferences = new RolePreferences
            {
                ZoneWeights = BuildZoneWeights(0.5f, 2.0f, 6.0f, 10.0f),
            };
            preset.DefendPreferences = new RolePreferences
            {
                ZoneWeights = BuildZoneWeights(1.0f, 3.0f, 4.0f, 2.0f),
            };
            preset.ChaseBallPreferences = new RolePreferences
            {
                ZoneWeights = BuildZoneWeights(1.0f, 3.0f, 5.0f, 8.0f),
            };
        }
        
        private static void SetUpMidfielderPreset(PlayerRole preset)
        {
            preset.RoleType = PlayerRoleType.Midfielder;
            preset.RoleName = "Midfielder";
            preset.AttackPreferences = new RolePreferences
            {
                ZoneWeights = BuildZoneWeights(2.0f, 5.0f, 5.0f, 2.0f),
            };

            preset.DefendPreferences = new RolePreferences
            {
                ZoneWeights = BuildZoneWeights(4.0f, 5.0f, 3.0f, 1.0f),
            };

            preset.ChaseBallPreferences = new RolePreferences
            {
                ZoneWeights = BuildZoneWeights(3.0f, 5.0f, 5.0f, 2.0f),
            };
        }
        
        private static void SetUpDefenderPreset(PlayerRole preset)
        {
            preset.RoleType = PlayerRoleType.Defender;
            preset.RoleName = "Defender";        
            preset.AttackPreferences = new RolePreferences
            {
                ZoneWeights = BuildZoneWeights(6.0f, 4.0f, 1.5f, 0.5f),
            };

            preset.DefendPreferences = new RolePreferences
            {
                ZoneWeights = BuildZoneWeights(10.0f, 3.0f, 0.5f, 0.2f),
            };

            preset.ChaseBallPreferences = new RolePreferences
            {
                ZoneWeights = BuildZoneWeights(5.0f, 4.0f, 2.0f, 1.0f),
            };
        }

        private static System.Collections.Generic.List<ZoneWeightEntry> BuildZoneWeights(float zone01, float zone02,
            float zone03, float zone04)
        {
            return new System.Collections.Generic.List<ZoneWeightEntry>
            {
                new ZoneWeightEntry{ZoneId = "zone_01", Weight = zone01},
                new ZoneWeightEntry{ZoneId = "zone_02", Weight = zone02},
                new ZoneWeightEntry{ZoneId = "zone_03", Weight = zone03},
                new ZoneWeightEntry{ZoneId = "zone_04", Weight = zone04}
            };
        }
    }
}
