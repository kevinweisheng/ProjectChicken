# 新UI流程配置指南

## 概述

新的UI流程如下：
1. **游戏启动** → 显示主菜单（新游戏、继续游戏、退出游戏）
2. **主菜单选择** → 进入技能树界面
3. **技能树界面** → 点击开始游戏按钮进入游戏回合
4. **回合结束** → 结算面板 → 领取 → 直接进入技能树界面

---

## 一、MainMenuPanel（主菜单面板）配置

### 1.1 移除旧的UI组件

在Unity Inspector中，找到 `MainMenuPanel` 组件，需要移除以下字段的引用：
- ❌ `Start Battle Button`（开始战斗按钮）
- ❌ `Open Skill Tree Button`（打开技能树按钮）
- ❌ `Clear Save Button`（清除存档按钮）
- ❌ `Total Eggs Text`（显示全局货币的文本）
- ❌ `Spawner`（鸡生成器引用）

### 1.2 添加新的UI组件

在Unity场景中创建或配置以下按钮：

#### 创建新游戏按钮
1. 在 `MainMenuPanel` 的 GameObject 下创建一个新的 Button
2. 命名为 `NewGameButton`
3. 设置按钮文本为 "新游戏" 或 "New Game"
4. 在 `MainMenuPanel` 组件的 Inspector 中，将 `New Game Button` 字段拖入这个按钮

#### 创建继续游戏按钮
1. 在 `MainMenuPanel` 的 GameObject 下创建一个新的 Button
2. 命名为 `ContinueGameButton`
3. 设置按钮文本为 "继续游戏" 或 "Continue Game"
4. 在 `MainMenuPanel` 组件的 Inspector 中，将 `Continue Game Button` 字段拖入这个按钮

#### 创建退出游戏按钮
1. 在 `MainMenuPanel` 的 GameObject 下创建一个新的 Button
2. 命名为 `ExitGameButton`
3. 设置按钮文本为 "退出游戏" 或 "Exit Game"
4. 在 `MainMenuPanel` 组件的 Inspector 中，将 `Exit Game Button` 字段拖入这个按钮

### 1.3 配置系统引用

- **Skill Tree Panel**：将场景中的 `SkillTreePanel` GameObject 拖入此字段（如果为空会自动查找）

### 1.4 最终配置检查

MainMenuPanel 的 Inspector 应该包含：
- ✅ `Canvas Group`（自动获取）
- ✅ `New Game Button`
- ✅ `Continue Game Button`
- ✅ `Exit Game Button`
- ✅ `Skill Tree Panel`（可选，会自动查找）

---

## 二、SkillTreePanel（技能树面板）配置

### 2.1 添加开始游戏按钮

1. 在 `SkillTreePanel` 的 GameObject 下创建一个新的 Button
2. 命名为 `StartGameButton`
3. 设置按钮文本为 "开始游戏" 或 "Start Game"
4. 在 `SkillTreePanel` 组件的 Inspector 中，将 `Start Game Button` 字段拖入这个按钮

### 2.2 配置返回按钮（可选）

- **Close Button**：如果不需要返回主菜单功能，可以留空
- 如果需要返回主菜单功能，确保 `Close Button` 字段已配置

### 2.3 配置系统引用

- **Main Menu Panel**：将场景中的 `MainMenuPanel` GameObject 拖入此字段（如果为空会自动查找）

### 2.4 最终配置检查

SkillTreePanel 的 Inspector 应该包含：
- ✅ `Canvas Group`（自动获取）
- ✅ `Start Game Button`（**新增，必需**）
- ✅ `Close Button`（可选）
- ✅ `Main Menu Panel`（可选，会自动查找）
- ✅ 其他现有配置（Tooltip、连线系统等）

---

## 三、SettlementPanel（结算面板）配置

### 3.1 添加开始下一回合按钮

1. 在 `SettlementPanel` 的 GameObject 下创建一个新的 Button
2. 命名为 `StartNextRoundButton`
3. 设置按钮文本为 "开始下一回合" 或 "Start Next Round"
4. 在 `SettlementPanel` 组件的 Inspector 中，将 `Start Next Round Button` 字段拖入这个按钮

### 3.2 配置系统引用

1. 在 `SettlementPanel` 组件的 Inspector 中，找到 "系统引用" 部分
2. 将场景中的 `SkillTreePanel` GameObject 拖入 `Skill Tree Panel` 字段
3. 如果留空，系统会自动查找（使用单例）

### 3.3 最终配置检查

SettlementPanel 的 Inspector 应该包含：
- ✅ `Canvas Group`（自动获取）
- ✅ `Score Text`（显示分数的文本）
- ✅ `Claim Button`（领取按钮，进入技能树）
- ✅ `Start Next Round Button`（**新增，开始下一回合按钮，直接开始游戏**）
- ✅ `Skill Tree Panel`（可选，会自动查找）

---

## 四、GameManager 配置

### 4.1 检查启动逻辑

`GameManager` 的 `Start()` 方法已经修改，不再自动进入准备阶段。这是正确的，因为现在由主菜单控制流程。

**无需额外配置**，保持现有配置即可。

---

## 五、UI层级和显示顺序

### 5.1 Canvas 设置建议

建议的 Canvas 层级（从下到上）：
1. **MainMenuPanel** - 主菜单（游戏启动时显示）
2. **SkillTreePanel** - 技能树（从主菜单进入）
3. **SettlementPanel** - 结算面板（回合结束时显示）
4. 其他游戏UI（HUD、倒计时等）

### 5.2 初始显示状态

在Unity场景中设置初始状态：
- **MainMenuPanel**：`CanvasGroup.alpha = 1`（显示）
- **SkillTreePanel**：`CanvasGroup.alpha = 0`（隐藏）
- **SettlementPanel**：`CanvasGroup.alpha = 0`（隐藏）

---

## 六、完整流程测试

### 6.1 测试步骤

1. **游戏启动测试**
   - 运行游戏
   - 应该看到主菜单（新游戏、继续游戏、退出游戏三个按钮）
   - 技能树和结算面板应该隐藏

2. **新游戏流程测试**
   - 点击"新游戏"按钮
   - 主菜单应该隐藏
   - 技能树应该显示
   - 存档应该被清除

3. **继续游戏流程测试**
   - 点击"继续游戏"按钮
   - 主菜单应该隐藏
   - 技能树应该显示
   - 存档应该被加载

4. **技能树流程测试**
   - 在技能树界面点击"开始游戏"按钮
   - 技能树应该隐藏
   - 游戏应该开始（进入 Playing 状态）

5. **回合结束流程测试**
   - 等待回合结束
   - 结算面板应该显示
   - **测试领取按钮**：
     - 点击"领取"按钮
     - 结算面板应该隐藏
     - 技能树应该显示（**不再进入主菜单**）
   - **测试开始下一回合按钮**：
     - 点击"开始下一回合"按钮
     - 结算面板应该隐藏
     - 游戏应该直接开始新的一局（**跳过技能树**）

6. **返回主菜单测试**
   - 在技能树界面点击"返回"按钮（如果配置了）
   - 技能树应该隐藏
   - 主菜单应该显示

---

## 七、常见问题排查

### 7.1 主菜单不显示

**问题**：游戏启动时看不到主菜单

**解决方案**：
- 检查 `MainMenuPanel` 的 `CanvasGroup` 组件
- 确保 `Alpha = 1`，`Interactable = true`，`Blocks Raycasts = true`
- 检查 GameObject 是否激活

### 7.2 技能树不显示

**问题**：点击新游戏/继续游戏后，技能树不显示

**解决方案**：
- 检查 `MainMenuPanel` 的 `Skill Tree Panel` 字段是否配置
- 检查 `SkillTreePanel` 的 `CanvasGroup` 设置
- 查看 Console 是否有错误信息

### 7.3 开始游戏按钮无效

**问题**：在技能树界面点击开始游戏按钮没有反应

**解决方案**：
- 检查 `SkillTreePanel` 的 `Start Game Button` 字段是否配置
- 检查 `GameManager` 是否存在于场景中
- 查看 Console 是否有错误信息

### 7.4 回合结束后进入主菜单

**问题**：回合结束后进入了主菜单而不是技能树

**解决方案**：
- 检查 `SettlementPanel` 的 `Skill Tree Panel` 字段是否配置
- 检查 `OnClaimClicked()` 方法是否正确调用 `skillTreePanel.Show()`
- 查看 Console 是否有错误信息

---

## 八、配置清单

在开始配置前，请确认以下清单：

### MainMenuPanel
- [ ] 移除了旧的按钮引用（Start Battle、Open Skill Tree、Clear Save）
- [ ] 添加了新游戏按钮并配置引用
- [ ] 添加了继续游戏按钮并配置引用
- [ ] 添加了退出游戏按钮并配置引用
- [ ] 配置了 Skill Tree Panel 引用（可选）

### SkillTreePanel
- [ ] 添加了开始游戏按钮并配置引用
- [ ] 配置了 Close Button（可选）
- [ ] 配置了 Main Menu Panel 引用（可选）

### SettlementPanel
- [ ] 添加了开始下一回合按钮并配置引用
- [ ] 配置了 Skill Tree Panel 引用（可选，会自动查找）

### 场景设置
- [ ] MainMenuPanel 初始显示（Alpha = 1）
- [ ] SkillTreePanel 初始隐藏（Alpha = 0）
- [ ] SettlementPanel 初始隐藏（Alpha = 0）

---

## 九、代码变更总结

### 9.1 MainMenuPanel.cs
- ✅ 移除了 `startBattleButton`、`openSkillTreeButton`、`clearSaveButton`
- ✅ 移除了 `totalEggsText`、`spawner` 引用
- ✅ 添加了 `newGameButton`、`continueGameButton`、`exitGameButton`
- ✅ 移除了游戏状态订阅逻辑
- ✅ 添加了 `OnNewGameClicked()`、`OnContinueGameClicked()`、`OnExitGameClicked()` 方法

### 9.2 SkillTreePanel.cs
- ✅ 添加了 `startGameButton` 字段
- ✅ 添加了 `OnStartGameClicked()` 方法
- ✅ 修改了 `OnCloseButtonClicked()` 方法（简化逻辑）

### 9.3 SettlementPanel.cs
- ✅ 添加了 `skillTreePanel` 引用
- ✅ 添加了 `startNextRoundButton` 字段
- ✅ 添加了 `OnStartNextRoundClicked()` 方法（直接开始下一回合）
- ✅ 修改了 `OnClaimClicked()` 方法（直接进入技能树）

### 9.4 GameManager.cs
- ✅ 修改了 `Start()` 方法（不再自动进入准备阶段）

---

## 十、注意事项

1. **主菜单只在游戏启动时显示**，回合结束后不会自动显示主菜单
2. **技能树是核心界面**，从主菜单进入，回合结束后也会进入
3. **返回主菜单功能是可选的**，如果不需要可以留空 `Close Button`
4. **所有系统引用都是可选的**，如果留空会自动查找（使用单例或 FindFirstObjectByType）
5. **确保 CanvasGroup 组件存在**，所有面板都使用 CanvasGroup 控制显示/隐藏

---

## 完成配置后

配置完成后，请按照"六、完整流程测试"中的步骤进行测试，确保所有功能正常工作。

如果遇到问题，请查看"七、常见问题排查"部分，或检查 Unity Console 中的错误信息。

