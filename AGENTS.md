# AGENTS 规则

## 1) 命名规则
- 遵循项目现有命名风格。
- 类/结构体/枚举/接口/类型名使用 `PascalCase`。
- 公有字段/属性/方法使用 `PascalCase`。
- 私有字段使用 `_camelCase`。
- 局部变量/参数使用 `camelCase`。
- Bool 命名应清晰表达状态，优先使用 `Is/Has/Can/Enable` 前缀。
- 文件名应与主要类名保持一致。

## 2) 函数格式
- 函数内部除非存在明显的大功能边界变化，否则不要添加空行。
- 保持函数体紧凑、可读。

## 3) 注释规范
- 仅在必要处添加简短注释（如非直观逻辑、前提假设、边界情况）。
- 避免添加显而易见或重复性注释。

## 4) 属性使用规范
- 非必要不使用 Property。
- 优先使用显式的 `GetXxx()/SetXxx(...)` 函数来读写状态。

## 5) 重要约束
- 运行时代码不得直接依赖 Editor-only API；必要时使用 `#if UNITY_EDITOR` 包裹。
- 热路径中避免高频日志和不必要分配，除非功能必须。

## 6) Editor 开发规范
- Editor 代码放在 `Assets/Scripts/Editor` 或 Editor-only Assembly，避免运行时代码直接引用 `UnityEditor`。
- 优先复用项目已有工具链（如 Odin），减少手写 IMGUI 样板代码。
- Scene 交互与数据编辑分离：面板负责数据输入，`SceneView`/`Handles` 负责可视化与拖拽。
- 对可编辑资产的修改必须支持 Undo（`Undo.RecordObject(...)`）并在修改后标记脏数据（`EditorUtility.SetDirty(...)`）。
- 工具菜单统一放在 `Tools/FootballAI/...`。
- 类内成员按职责分组，使用适量 `#region`（如：字段、生命周期、按钮动作、Scene 交互、日志）。
