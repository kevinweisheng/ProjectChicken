# SkillTreePanel 新功能使用说明

## 概述

`SkillTreePanel` 现在支持两个新功能：
1. **Tooltip 系统** - 鼠标悬停时显示技能详细信息
2. **连线系统** - 根据技能的前置依赖自动绘制连接线

---

## 一、Tooltip 系统配置

### 步骤 1：创建 Tooltip UI 结构

在 Unity 编辑器中：

1. 在 `SkillTreePanel` GameObject 下创建一个新的子对象，命名为 `TooltipPanel`
2. 为 `TooltipPanel` 添加以下组件：
   - `RectTransform`（自动添加）
   - `Image` 组件（作为背景，可选）
   - `Layout Group`（可选，用于自动布局）

3. 在 `TooltipPanel` 下创建三个子对象作为文本显示：
   - `TitleText` - 添加 `TextMeshPro - Text (UI)` 组件
   - `DescText` - 添加 `TextMeshPro - Text (UI)` 组件  
   - `CostText` - 添加 `TextMeshPro - Text (UI)` 组件

4. 初始设置：
   - 将 `TooltipPanel` 设置为**非激活状态**（取消勾选 Inspector 中的 Active 复选框）
   - 调整 `TooltipPanel` 的大小和样式（背景颜色、边框等）

### 步骤 2：配置 SkillTreePanel

1. 选择场景中的 `SkillTreePanel` GameObject
2. 在 Inspector 中找到 `SkillTreePanel` 组件
3. 在 **Tooltip 系统** 部分：
   - 将 `Tooltip Panel` 拖拽到 `tooltipPanel` 字段
   - 将 `TitleText` 拖拽到 `tooltipTitle` 字段
   - 将 `DescText` 拖拽到 `tooltipDesc` 字段
   - 将 `CostText` 拖拽到 `tooltipCost` 字段
   - 调整 `Tooltip Offset`（默认：X=10, Y=10）以设置 Tooltip 相对于鼠标的偏移量

### 步骤 3：测试 Tooltip

1. 运行游戏
2. 打开技能树面板（按 K 键或 Tab 键）
3. 将鼠标悬停在任意技能图标上
4. 应该看到 Tooltip 显示技能的名称、描述和成本

---

## 二、连线系统配置

### 步骤 1：创建连线预制体

1. 在 Project 窗口中，右键点击 `Assets/Prefabs` 文件夹（如果没有则创建）
2. 选择 `UI > Image` 创建新的 Image GameObject
3. 重命名为 `SkillLinePrefab`
4. 配置 `SkillLinePrefab`：
   - 在 Inspector 中，设置 `Image` 组件的 `Color`（例如：白色或灰色）
   - 设置 `Image` 组件的 `Raycast Target` 为 **false**（避免阻挡鼠标事件）
   - 调整 `RectTransform` 的 `Width` 和 `Height`（初始值不重要，代码会自动调整）

5. 将 `SkillLinePrefab` 拖拽到 `Assets/Prefabs` 文件夹中保存为预制体
6. 从场景中删除 `SkillLinePrefab`（保留预制体即可）

### 步骤 2：创建连线容器

1. 在 `SkillTreePanel` GameObject 下创建一个新的子对象，命名为 `LineContainer`
2. 配置 `LineContainer`：
   - 确保它在 Hierarchy 中的位置**在所有 SkillSlotUI 之后**（这样连线会显示在图标下方）
   - 或者使用 `SetAsFirstSibling()` 确保连线在底层（代码已自动处理）

### 步骤 3：配置 SkillTreePanel

1. 选择场景中的 `SkillTreePanel` GameObject
2. 在 Inspector 中找到 `SkillTreePanel` 组件
3. 在 **连线系统** 部分：
   - 将 `SkillLinePrefab` 预制体拖拽到 `Line Prefab` 字段
   - 将 `LineContainer` GameObject 拖拽到 `Line Container` 字段
   - 设置 `Line Width`（默认：3，表示连线宽度为 3 像素）

### 步骤 4：配置技能节点的前置依赖

1. 选择任意 `SkillNodeData` ScriptableObject 资源
2. 在 Inspector 中，找到 **购买条件** 部分
3. 有两种方式设置前置技能：

   **方式 A：使用 Prerequisites 列表（推荐）**
   - 展开 `Prerequisites` 列表
   - 设置 `Size` 为所需的前置技能数量
   - 将前置技能节点拖拽到列表的各个元素中

   **方式 B：使用旧的 Prerequisite 字段（单个前置）**
   - 将前置技能节点拖拽到 `Prerequisite` 字段
   - 注意：如果同时设置了 `Prerequisites` 和 `Prerequisite`，系统会优先使用 `Prerequisites`

### 步骤 5：测试连线

1. 运行游戏
2. 打开技能树面板（按 K 键或 Tab 键）
3. 应该看到技能节点之间根据 `prerequisites` 关系自动绘制的连接线

---

## 三、完整示例配置

### UI 层级结构示例：

```
SkillTreePanel
├── CanvasGroup (组件)
├── CloseButton
├── TooltipPanel (初始非激活)
│   ├── TitleText (TMP_Text)
│   ├── DescText (TMP_Text)
│   └── CostText (TMP_Text)
├── LineContainer
└── SkillSlots (你的技能槽位容器)
    ├── SkillSlot1 (SkillSlotUI)
    ├── SkillSlot2 (SkillSlotUI)
    └── ...
```

### 技能节点配置示例：

假设有三个技能：
- `SkillA` - 无前置（根节点）
- `SkillB` - 需要 `SkillA`
- `SkillC` - 需要 `SkillA` 和 `SkillB`

配置步骤：
1. `SkillA` 的 `Prerequisites` 列表为空
2. `SkillB` 的 `Prerequisites` 列表：添加 `SkillA`
3. `SkillC` 的 `Prerequisites` 列表：添加 `SkillA` 和 `SkillB`

结果：会绘制 `SkillA → SkillB` 和 `SkillA → SkillC` 和 `SkillB → SkillC` 的连线

---

## 四、常见问题

### Q1: Tooltip 不显示？
- 检查 `TooltipPanel` 是否已正确拖拽到 `tooltipPanel` 字段
- 检查 `TooltipPanel` 的初始状态是否为非激活
- 检查 `SkillSlotUI` 是否正确配置了 `targetSkill`

### Q2: 连线不显示？
- 检查 `Line Prefab` 和 `Line Container` 是否已正确配置
- 检查技能节点的 `Prerequisites` 是否已正确设置
- 检查 `LineContainer` 是否在正确的层级位置
- 检查连线预制体的 `Image` 组件颜色是否可见

### Q3: 连线位置不正确？
- 检查 Canvas 的 `Scale Factor` 设置
- 确保技能槽位的 `RectTransform` 位置正确
- 连线会自动从父节点（前置技能）指向子节点（当前技能）

### Q4: Tooltip 位置超出屏幕？
- 调整 `Tooltip Offset` 的值
- 代码会自动将 Tooltip 保持在屏幕范围内

---

## 五、代码说明

### Tooltip 系统工作原理：
1. 当鼠标进入 `SkillSlotUI` 时，触发 `OnPointerEnter`
2. `SkillSlotUI` 调用 `OnHoverEnter` 事件，传递 `SkillNodeData`
3. `SkillTreePanel` 的 `ShowTooltip` 方法被调用
4. 设置文本内容并定位 Tooltip 面板
5. 在 `Update()` 中持续更新 Tooltip 位置（跟随鼠标）

### 连线系统工作原理：
1. 在 `Start()` 中调用 `InitializeSkillSlots()`
2. 遍历所有 `SkillSlotUI`，建立技能数据到槽位的映射
3. 调用 `DrawConnections()` 绘制连线
4. 对于每个技能槽位，检查其 `Prerequisites`
5. 为每个前置技能创建一条连线，从父节点指向子节点

---

## 六、高级配置

### 自定义连线样式：
- 修改 `SkillLinePrefab` 的 `Image` 组件：
  - 更改 `Color` 以改变连线颜色
  - 使用 `Sprite` 可以创建带纹理的连线
  - 调整 `Material` 可以添加发光效果

### 自定义 Tooltip 样式：
- 在 `TooltipPanel` 上添加 `Vertical Layout Group` 实现自动布局
- 添加背景图片或边框
- 使用 `Content Size Fitter` 自动调整大小

### 性能优化：
- 连线在初始化时一次性创建，不会每帧更新
- Tooltip 只在鼠标悬停时显示，不会影响性能
- 如果技能节点很多，考虑使用对象池管理连线

---

## 七、注意事项

1. **Canvas 设置**：确保 Canvas 的 `Render Mode` 设置正确，代码会自动获取 Canvas 引用
2. **坐标系统**：连线使用世界坐标，会自动处理 Canvas 缩放
3. **事件订阅**：系统在 `Start()` 时自动订阅事件，无需手动配置
4. **连线层级**：连线会自动设置为底层，不会遮挡技能图标

---

## 完成！

配置完成后，你的技能树系统将具备：
- ✅ 鼠标悬停显示详细信息
- ✅ 自动绘制技能依赖关系
- ✅ 美观的 UI 展示

享受你的技能树系统吧！

