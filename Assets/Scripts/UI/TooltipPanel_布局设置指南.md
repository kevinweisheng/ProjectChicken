# TooltipPanel 布局设置指南

## 目标布局结构

```
TooltipPanel
├── Header (顶部区域)
│   ├── IconImage (技能图标)
│   └── TitleText (技能名称)
├── DescText (中间区域 - 技能描述)
└── CostText (底部区域 - 等级和消耗信息)
```

---

## 步骤 1：创建 TooltipPanel 基础结构

1. 在 Hierarchy 中找到 `Canvas > SkillTreePanel`
2. 右键点击 `SkillTreePanel`，选择 `UI > Panel`（或 `Create Empty`）
3. 重命名为 `TooltipPanel`
4. 在 Inspector 中：
   - 添加 `Image` 组件作为背景（可选，用于显示背景色）
   - 设置 `RectTransform` 的宽度和高度（例如：300 x 200）
   - **重要**：取消勾选 `Active` 复选框（初始状态为非激活）

---

## 步骤 2：设置布局组件

1. 选中 `TooltipPanel`
2. 在 Inspector 中，点击 `Add Component`
3. 添加 `Vertical Layout Group` 组件
4. 配置 `Vertical Layout Group`：
   - ✅ 勾选 `Child Force Expand` 的 `Width`
   - ❌ 取消勾选 `Child Force Expand` 的 `Height`
   - `Spacing`: 10（元素之间的间距）
   - `Padding`: 
     - Left: 10
     - Right: 10
     - Top: 10
     - Bottom: 10

5. 添加 `Content Size Fitter` 组件（可选，自动调整大小）：
   - `Horizontal Fit`: Preferred Size
   - `Vertical Fit`: Preferred Size

---

## 步骤 3：创建顶部区域（图标 + 名称）

### 3.1 创建 Header 容器

1. 右键点击 `TooltipPanel`，选择 `Create Empty`
2. 重命名为 `Header`
3. 选中 `Header`，添加 `Horizontal Layout Group` 组件：
   - `Spacing`: 10
   - `Child Alignment`: Middle Left
   - ✅ 勾选 `Child Force Expand` 的 `Height`
   - ❌ 取消勾选 `Child Force Expand` 的 `Width`

4. 添加 `Layout Element` 组件：
   - `Preferred Height`: 50（设置固定高度）

### 3.2 创建技能图标

1. 右键点击 `Header`，选择 `UI > Image`
2. 重命名为 `IconImage`
3. 在 Inspector 中：
   - 设置 `RectTransform` 的 `Width` 和 `Height` 为 40 x 40（或你想要的图标大小）
   - 在 `Image` 组件中：
     - `Image Type`: Simple
     - `Preserve Aspect`: ✅ 勾选（保持图标比例）

### 3.3 创建技能名称文本

1. 右键点击 `Header`，选择 `UI > Text - TextMeshPro`（首次使用会弹出导入窗口，点击 Import）
2. 重命名为 `TitleText`
3. 在 Inspector 中：
   - 设置 `RectTransform`：
     - `Width`: 可以留空（由 Layout Group 自动调整）
     - `Height`: 30
   - 在 `TextMeshPro - Text (UI)` 组件中：
     - `Font Size`: 18
     - `Font Style`: Bold（加粗）
     - `Alignment`: Left（左对齐）
     - `Text`: "技能名称"（临时文本，代码会自动设置）

---

## 步骤 4：创建中间区域（技能描述）

1. 右键点击 `TooltipPanel`（不是 Header），选择 `UI > Text - TextMeshPro`
2. 重命名为 `DescText`
3. 在 Inspector 中：
   - 在 `TextMeshPro - Text (UI)` 组件中：
     - `Font Size`: 14
     - `Alignment`: Left Top（左上对齐）
     - `Text Wrapping`: ✅ 启用（自动换行）
     - `Text`: "技能描述内容..."（临时文本）
   - 添加 `Layout Element` 组件：
     - `Preferred Height`: 80（或根据内容调整）
     - `Flexible Height`: 1（允许扩展）

---

## 步骤 5：创建底部区域（等级和消耗）

1. 右键点击 `TooltipPanel`，选择 `UI > Text - TextMeshPro`
2. 重命名为 `CostText`
3. 在 Inspector 中：
   - 在 `TextMeshPro - Text (UI)` 组件中：
     - `Font Size`: 14
     - `Font Style`: Normal
     - `Alignment`: Left（左对齐）
     - `Text`: "等级: 0/5 | 消耗: 100 鸡蛋"（临时文本）
   - 添加 `Layout Element` 组件：
     - `Preferred Height`: 25（固定高度）

---

## 步骤 6：配置 SkillTreePanel 组件

1. 选中 `SkillTreePanel` GameObject
2. 在 Inspector 中找到 `SkillTreePanel` 组件
3. 在 **Tooltip 系统** 部分：
   - `Tooltip Panel`: 拖拽 `TooltipPanel` GameObject
   - `Tooltip Icon`: 拖拽 `Header > IconImage` GameObject
   - `Tooltip Title`: 拖拽 `Header > TitleText` GameObject
   - `Tooltip Desc`: 拖拽 `DescText` GameObject
   - `Tooltip Cost`: 拖拽 `CostText` GameObject
   - `Tooltip Offset`: (10, 10)（根据需要调整）

---

## 最终 Hierarchy 结构

```
Canvas
└── SkillTreePanel
    ├── SkillTreePanel (组件)
    ├── CloseButton
    ├── TooltipPanel (初始非激活)
    │   ├── Header
    │   │   ├── IconImage (Image 组件)
    │   │   └── TitleText (TMP_Text 组件)
    │   ├── DescText (TMP_Text 组件)
    │   └── CostText (TMP_Text 组件)
    └── LineContainer
    └── ... (其他子对象)
```

---

## 布局效果说明

### 顶部区域（Header）
- **左侧**：技能图标（40x40 像素）
- **右侧**：技能名称（加粗，18号字体）

### 中间区域（DescText）
- 技能描述文本（14号字体，自动换行）
- 占据剩余空间

### 底部区域（CostText）
- 显示格式：`等级: 当前等级/最大等级 → 下一级/最大等级 | 消耗: X 鸡蛋`
- 如果已满级：`等级: 最大等级/最大等级 (已满级)`

---

## 样式建议

### TooltipPanel 背景
- 在 `TooltipPanel` 的 `Image` 组件中：
  - `Color`: 半透明黑色 (R:0, G:0, B:0, A:200)
  - 或使用带圆角的背景图片

### 文本颜色
- `TitleText`: 白色或黄色（突出显示）
- `DescText`: 浅灰色（易读）
- `CostText`: 白色或绿色（强调消耗信息）

### 图标设置
- `IconImage` 的 `Image` 组件：
  - `Preserve Aspect`: ✅ 勾选（保持图标原始比例）
  - `Raycast Target`: ❌ 取消勾选（避免阻挡鼠标事件）

---

## 测试

1. 运行游戏
2. 打开技能树面板（按 K 键）
3. 将鼠标悬停在技能图标上
4. 应该看到：
   - ✅ 顶部：技能图标 + 技能名称
   - ✅ 中间：技能描述
   - ✅ 底部：等级信息和消耗

---

## 常见问题

### Q: Tooltip 显示不完整？
- 检查 `Content Size Fitter` 是否正确配置
- 检查 `Vertical Layout Group` 的 `Padding` 设置
- 调整 `DescText` 的 `Preferred Height`

### Q: 图标不显示？
- 检查 `Tooltip Icon` 字段是否正确拖拽了 `IconImage`
- 检查技能数据的 `Icon` 字段是否已设置
- 检查 `IconImage` 的 `Image` 组件是否启用

### Q: 文本重叠？
- 检查 `Vertical Layout Group` 的 `Spacing` 设置
- 检查各个文本对象的 `Layout Element` 高度设置

---

## 完成！

配置完成后，你的 Tooltip 将按照以下布局显示：
- 📌 **顶部**：图标 + 名称
- 📝 **中间**：描述
- 💰 **底部**：等级 + 消耗

享受你的新 Tooltip 布局！

