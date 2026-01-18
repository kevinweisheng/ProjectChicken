# UI多分辨率适配指南

本文档说明如何确保所有UI（包括技能树）在不同分辨率下都能正确显示。

## 一、Canvas Scaler 配置

### 1.1 基础设置

1. **找到场景中的 Canvas GameObject**
   - 在 Hierarchy 中选择 Canvas
   - 确保 Canvas 组件存在

2. **添加/配置 CanvasScaler 组件**
   - 如果还没有，在 Inspector 中添加 `Canvas Scaler` 组件
   - 如果已有，确保配置正确

3. **推荐配置**：
   ```
   UI Scale Mode: Scale With Screen Size
   Reference Resolution: X: 1920, Y: 1080（根据你的设计分辨率调整）
   Screen Match Mode: Match Width Or Height
   Match: 0.5（在宽度和高度之间平衡）
   ```

### 1.2 使用 UIResolutionAdapter 自动配置

1. **添加适配器脚本**：
   - 将 `UIResolutionAdapter.cs` 脚本添加到 Canvas GameObject 上
   - 脚本会自动配置 CanvasScaler

2. **在 Inspector 中配置**：
   - `Reference Resolution`: 设计时的参考分辨率（如 1920x1080）
   - `Scale Mode`: 选择 `Scale With Screen Size`
   - `Match Width Or Height`: 
     - `0` = 匹配宽度（宽屏友好）
     - `1` = 匹配高度（竖屏友好）
     - `0.5` = 平衡（推荐，适用于大多数情况）

3. **右键菜单功能**：
   - 右键组件名称，选择 "刷新UI适配配置"
   - 选择 "检查UI锚点设置" 来验证所有UI元素是否正确配置

## 二、RectTransform 锚点和边距设置

### 2.1 锚点预设（Anchors Presets）

不同类型的UI元素应使用不同的锚点预设：

#### **面板类（Panel）**：
- 全屏面板：`Stretch/Stretch`（左上、右上、左下、右下四角拉伸）
- 居中面板：先选择 `Middle/Center`，然后手动设置为 `Stretch/Stretch`

#### **按钮/图标类**：
- 固定位置：使用 `Middle/Center`、`Top/Left` 等固定锚点
- 相对位置：使用 `Top/Left` + 边距，或 `Bottom/Right` + 边距

#### **技能树节点**：
- 使用固定锚点（如 `Middle/Center`）配合 `Anchored Position`
- 或者使用百分比锚点（如 `Left: 0.2, Top: 0.8`）

### 2.2 正确的锚点设置步骤

1. **选择UI元素**
2. **点击 RectTransform 左上角的锚点预设按钮**
3. **选择合适的预设**：
   - 需要全屏拉伸：选择 `Stretch/Stretch`（四角箭头）
   - 需要固定位置：选择具体的锚点位置
4. **调整边距（Offset）**：
   - 对于拉伸模式：设置 `Left`、`Right`、`Top`、`Bottom` 边距
   - 对于固定模式：设置 `Pos X`、`Pos Y` 位置和 `Width`、`Height` 大小

### 2.3 常见错误示例

❌ **错误**：
- 使用 `Anchored Position`（世界坐标）配合不同分辨率，导致位置偏移
- 固定像素大小而不使用相对单位

✅ **正确**：
- 使用锚点 + 边距模式
- 使用百分比定位（如锚点在 `(0.2, 0.8)`）

## 三、技能树特殊处理

### 3.1 技能树面板

`SkillTreePanel` 的 RectTransform 设置：

1. **锚点**：建议使用 `Stretch/Stretch`（全屏拉伸）
2. **边距**：如果需要留边距，设置 `Left`、`Right`、`Top`、`Bottom`

### 3.2 技能槽位（SkillSlotUI）

每个技能槽位的设置：

1. **锚点**：使用固定锚点（如 `Middle/Center`）
2. **位置**：使用 `Anchored Position` 设置相对位置
3. **大小**：使用固定 `Width` 和 `Height`，Canvas Scaler 会自动缩放

**注意**：技能树节点的位置是基于父容器的，确保父容器（如技能树面板）正确设置了锚点。

### 3.3 连线系统

连线系统已经在代码中处理了 Canvas 缩放：

```csharp
// 画布缩放校正（如果 Canvas Scaler 生效，需除以 scaleFactor）
if (canvas != null && canvas.scaleFactor != 0)
{
    distance = distance / canvas.scaleFactor;
}
```

无需额外配置，连线会自动适配不同分辨率。

## 四、测试不同分辨率

### 4.1 在 Unity 编辑器中测试

1. **打开 Game 视图**
2. **点击分辨率下拉菜单**
3. **选择不同分辨率测试**：
   - 1920x1080（16:9 横屏）
   - 1280x720（16:9 小屏）
   - 2560x1440（16:9 高分辨率）
   - 1080x1920（9:16 竖屏，如果需要）
   - 其他自定义分辨率

4. **观察UI元素**：
   - 检查是否有元素超出屏幕
   - 检查是否有元素重叠
   - 检查文字是否清晰可读

### 4.2 测试检查清单

- [ ] 主菜单按钮在正确位置
- [ ] 技能树面板正确显示
- [ ] 技能节点位置正确
- [ ] 连线显示正常
- [ ] 文字大小合适
- [ ] 按钮大小合适，容易点击
- [ ] 没有任何UI元素被裁剪

## 五、常见问题解决

### 问题1：UI元素在小分辨率下被裁剪

**解决方案**：
- 检查该元素的父容器是否设置了 `Stretch/Stretch` 锚点
- 检查边距设置是否合理
- 考虑减小固定像素值，使用相对单位

### 问题2：UI元素在大分辨率下显得太小

**解决方案**：
- 调整 CanvasScaler 的 `Match Width Or Height` 值（偏向高度）
- 或者增加参考分辨率
- 考虑使用 `Constant Pixel Size` 模式（不推荐，除非特殊需求）

### 问题3：技能树节点位置偏移

**解决方案**：
- 确保技能树容器的锚点设置正确
- 检查节点的 `Anchored Position` 是否基于父容器
- 如果使用手动放置，考虑在代码中动态计算位置

### 问题4：连线在不同分辨率下错位

**解决方案**：
- 代码已经处理了 Canvas 缩放，通常不会有问题
- 如果仍有问题，检查 `lineContainer` 的锚点设置
- 确保 `linePrefab` 的 `RectTransform` 设置正确

## 六、最佳实践

1. **统一参考分辨率**：
   - 选择一个参考分辨率（如 1920x1080）作为设计标准
   - 所有UI元素按此分辨率设计

2. **使用锚点而不是固定坐标**：
   - 尽量使用锚点和边距，而不是 `Anchored Position`
   - 这样可以自动适配不同屏幕比例

3. **测试多种分辨率**：
   - 至少测试最小和最大分辨率
   - 测试不同的宽高比（16:9, 21:9, 4:3 等）

4. **使用 Canvas Groups 控制显示**：
   - 已经使用的 `CanvasGroup` 是正确的方法
   - 不要用 `SetActive(false)` 来控制UI显示

5. **文字大小考虑**：
   - 确保最小分辨率下文字仍然清晰可读
   - 考虑使用 TextMeshPro 以获得更好的缩放效果

## 七、快速配置检查

运行以下检查确保配置正确：

1. ✅ Canvas 上有 `Canvas Scaler` 组件
2. ✅ `UIResolutionAdapter` 已添加到 Canvas（可选但推荐）
3. ✅ 参考分辨率设置为你设计时的分辨率
4. ✅ 所有面板使用 `Stretch/Stretch` 锚点或正确的锚点预设
5. ✅ 技能树节点位置正确
6. ✅ 在多种分辨率下测试通过

完成以上配置后，你的UI应该能在不同分辨率下正确显示了！