using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using BehaviorTree.Runtime;
using FootballAI.FootballCore;

namespace BehaviorTree.Graph.Editor
{
    public class GraphNodeGenerator
    {
        // 生成文件的目标路径
        private const string GENERATE_PATH = "Assets/Scripts/BehaviorTree/Graph/Nodes";

        [MenuItem("Tools/AI/Generate Graph Nodes")]
        public static void Generate()
        {
            // 1. 确保目录存在
            if (!Directory.Exists(GENERATE_PATH))
            {
                Directory.CreateDirectory(GENERATE_PATH);
            }

            // 2. 获取所有 Runtime.Node 的子类
            // 注意：这里假设这些类都在 Assembly-CSharp 程序集中
            var assembly = Assembly.Load("Assembly-CSharp");
            var runtimeTypes = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(BehaviorTree.Runtime.Node)) && !t.IsAbstract)
                .ToList();

            int count = 0;

            foreach (var type in runtimeTypes)
            {
                // 生成 GraphNode 的类名，例如 TaskShoot -> TaskShootGraphNode
                string graphNodeName = type.Name + "GraphNode";
                string filePath = Path.Combine(GENERATE_PATH, graphNodeName + ".cs");

                // --- 核心要求：如果文件已存在，跳过，不覆盖 ---
                if (File.Exists(filePath))
                {
                    // Debug.Log($"[Skipped] {graphNodeName} already exists.");
                    continue;
                }

                // 3. 分析构造函数以决定生成模板
                string fileContent = GenerateScriptContent(type, graphNodeName);

                if (!string.IsNullOrEmpty(fileContent))
                {
                    File.WriteAllText(filePath, fileContent);
                    count++;
                }
            }

            if (count > 0)
            {
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Graph Node Generator", $"Generated {count} new Graph Nodes.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Graph Node Generator",$"No Nodes Generated","ok");
            }
        }

        private static string GenerateScriptContent(Type runtimeType, string graphNodeName)
        {
            // 检查构造函数签名
            // 情况 A: public Node(FootballBlackboard bb)
            var simpleConstructor = runtimeType.GetConstructor(new[] { typeof(FootballBlackboard) });
            
            // 情况 B: public Node(FootballBlackboard bb, List<Node> children)
            var compositeConstructor = runtimeType.GetConstructor(new[] { typeof(FootballBlackboard), typeof(List<BehaviorTree.Runtime.Node>) });

            if (compositeConstructor != null)
            {
                return GenerateCompositeNodeTemplate(runtimeType.Name, graphNodeName);
            }
            else if (simpleConstructor != null)
            {
                return GenerateLeafNodeTemplate(runtimeType.Name, graphNodeName);
            }
            else
            {
                MyLog.LogWarning($"[Skipped] {runtimeType.Name} does not have a standard constructor (Blackboard) or (Blackboard, List<Node>).");
                return null;
            }
        }

        // --- 模板：普通节点 (Action / Condition) ---
        private static string GenerateLeafNodeTemplate(string runtimeClassName, string graphNodeClassName)
        {
            return 
$@"using UnityEngine;
using BehaviorTree.Runtime;
using Node = BehaviorTree.Runtime.Node;

namespace BehaviorTree.Graph
{{
    [CreateNodeMenu(""Football AI/{runtimeClassName}"")]
    public class {graphNodeClassName} : BTGraphNode
    {{
        // 如果Runtime节点有可以在编辑器配置的参数，可以在这里手动添加 public 字段
        // 然后在 CreateRuntimeNode 中传进去
        
        public override Node CreateRuntimeNode(FootballBlackboard blackboard)
        {{
            // 构造运行时对象
            return new {runtimeClassName}(blackboard);
        }}
    }}
}}";
        }

        // --- 模板：组合节点 (Composite: Selector / Sequence) ---
        private static string GenerateCompositeNodeTemplate(string runtimeClassName, string graphNodeClassName)
        {
            return 
$@"using System.Collections.Generic;
using UnityEngine;
using BehaviorTree.Runtime;
using Node = BehaviorTree.Runtime.Node;

namespace BehaviorTree.Graph
{{
    [CreateNodeMenu(""Football AI/Composite/{runtimeClassName}"")]
    public class {graphNodeClassName} : BTGraphNode
    {{
        public override Node CreateRuntimeNode(FootballBlackboard blackboard)
        {{
            // 1. 获取图表中连接的子节点（已按Y轴排序）
            var graphChildren = GetSortedGraphChildren();
            
            // 2. 将它们转换为运行时节点
            var runtimeChildren = new List<Node>();
            foreach (var childGraphNode in graphChildren)
            {{
                // 递归创建子节点
                if (childGraphNode != null)
                {{
                    runtimeChildren.Add(childGraphNode.CreateRuntimeNode(blackboard));
                }}
            }}

            // 3. 构造运行时组合节点，传入子节点列表
            return new {runtimeClassName}(blackboard, runtimeChildren);
        }}
    }}
}}";
        }
    }
}
