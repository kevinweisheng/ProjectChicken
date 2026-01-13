using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using TMPro;
using ProjectChicken.Core;
using ProjectChicken.Systems.SkillTree;

namespace ProjectChicken.UI
{
    /// <summary>
    /// 技能树面板：控制技能树UI的显示和隐藏（使用 CanvasGroup）
    /// 注意：使用 CanvasGroup 时，面板 GameObject 应始终激活，这样 Update 才能执行
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class SkillTreePanel : MonoBehaviour
    {
        public static SkillTreePanel Instance { get; private set; }

        [Header("按键配置")]
        [SerializeField] private KeyCode toggleKey = KeyCode.K; // 切换面板显示的按键（默认K键）
        [SerializeField] private KeyCode alternativeKey = KeyCode.Tab; // 备用按键（Tab键）

        [Header("UI组件")]
        [SerializeField] private CanvasGroup canvasGroup; // CanvasGroup 组件（用于控制显示/隐藏）
        [SerializeField] private Button closeButton; // 返回主菜单按钮

        [Header("系统引用")]
        [SerializeField] private MainMenuPanel mainMenuPanel; // 主菜单面板引用（如果为空则自动查找）

        [Header("Tooltip 系统")]
        [SerializeField] private GameObject tooltipPanel; // Tooltip 面板
        [SerializeField] private Image tooltipIcon; // Tooltip 技能图标
        [SerializeField] private TMP_Text tooltipTitle; // Tooltip 标题文本（技能名称）
        [SerializeField] private TMP_Text tooltipDesc; // Tooltip 描述文本
        [SerializeField] private TMP_Text tooltipCost; // Tooltip 成本文本（等级和消耗）
        [SerializeField] private Vector2 tooltipOffset = new Vector2(10f, 10f); // Tooltip 偏移量

        [Header("连线系统")]
        [SerializeField] private GameObject linePrefab; // 连线预制体（包含 Image 组件）
        [SerializeField] private Transform lineContainer; // 连线容器（用于存放所有连线）
        [SerializeField] private float lineWidth = 3f; // 连线宽度

        private bool isVisible = false; // 当前是否可见
        private Dictionary<SkillNodeData, SkillSlotUI> skillSlotMap; // 技能数据到槽位的映射
        private Canvas canvas; // Canvas 引用（用于计算缩放）

        private void Awake()
        {
            // 单例模式：允许外部通过静态引用访问
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("SkillTreePanel: 检测到多个实例，保留第一个。", this);
                return;
            }

            Instance = this;

            // 如果没有手动指定，尝试自动获取 CanvasGroup
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (canvasGroup == null)
            {
                Debug.LogError("SkillTreePanel: 未找到 CanvasGroup 组件！请确保 GameObject 上有 CanvasGroup 组件。", this);
            }
        }

        private void Start()
        {
            // 如果没有手动指定，尝试自动查找 MainMenuPanel
            if (mainMenuPanel == null)
            {
                mainMenuPanel = FindFirstObjectByType<MainMenuPanel>();
            }

            // 绑定返回按钮的点击事件
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseButtonClicked);
            }
            else
            {
                Debug.LogWarning("SkillTreePanel: closeButton 未配置！", this);
            }

            // 获取 Canvas 引用
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }

            // 初始化技能槽位映射和订阅事件
            InitializeSkillSlots();

            // 记录初始状态
            Debug.Log($"SkillTreePanel: 初始化完成。初始状态: 隐藏", this);

            // 初始化：默认隐藏面板
            Hide();
        }

        private void OnEnable()
        {
            // 刷新 UI 显示，确保显示的"当前拥有鸡蛋数"是最新的全局货币
            RefreshUI();
            
            // 订阅全局货币变化事件，自动刷新UI
            ResourceManager.OnGlobalEggsChanged += OnGlobalEggsChanged;
        }

        private void OnDisable()
        {
            // 取消订阅：防止内存泄漏
            ResourceManager.OnGlobalEggsChanged -= OnGlobalEggsChanged;
        }

        private void Update()
        {
            // 检测按键输入（使用新的 Input System）
            // 注意：使用 CanvasGroup 时，面板 GameObject 必须始终激活，Update 才能执行
            if (Keyboard.current == null)
            {
                return; // 键盘设备不可用
            }

            bool keyPressed = false;

            // 将 KeyCode 转换为新的 Key 枚举并检测按键
            Key? toggleKeyNew = ConvertKeyCodeToKey(toggleKey);
            Key? alternativeKeyNew = ConvertKeyCodeToKey(alternativeKey);

            if (toggleKeyNew.HasValue && Keyboard.current[toggleKeyNew.Value].wasPressedThisFrame)
            {
                keyPressed = true;
            }
            else if (alternativeKeyNew.HasValue && Keyboard.current[alternativeKeyNew.Value].wasPressedThisFrame)
            {
                keyPressed = true;
            }

            if (keyPressed)
            {
                Debug.Log($"SkillTreePanel: 检测到按键按下，切换面板显示状态。当前状态：{(isVisible ? "显示" : "隐藏")}", this);
                TogglePanel();
            }

            // Tooltip 位置在显示时已确定，不需要每帧更新
            // （如果需要跟随技能节点移动，可以在这里更新）
        }

        /// <summary>
        /// 将 KeyCode 转换为新的 Input System 的 Key 枚举
        /// </summary>
        /// <param name="keyCode">旧的 KeyCode</param>
        /// <returns>新的 Key 枚举，如果无法转换则返回 null</returns>
        private Key? ConvertKeyCodeToKey(KeyCode keyCode)
        {
            // 特殊处理常用按键
            switch (keyCode)
            {
                // 字母键
                case KeyCode.A: return Key.A;
                case KeyCode.B: return Key.B;
                case KeyCode.C: return Key.C;
                case KeyCode.D: return Key.D;
                case KeyCode.E: return Key.E;
                case KeyCode.F: return Key.F;
                case KeyCode.G: return Key.G;
                case KeyCode.H: return Key.H;
                case KeyCode.I: return Key.I;
                case KeyCode.J: return Key.J;
                case KeyCode.K: return Key.K;
                case KeyCode.L: return Key.L;
                case KeyCode.M: return Key.M;
                case KeyCode.N: return Key.N;
                case KeyCode.O: return Key.O;
                case KeyCode.P: return Key.P;
                case KeyCode.Q: return Key.Q;
                case KeyCode.R: return Key.R;
                case KeyCode.S: return Key.S;
                case KeyCode.T: return Key.T;
                case KeyCode.U: return Key.U;
                case KeyCode.V: return Key.V;
                case KeyCode.W: return Key.W;
                case KeyCode.X: return Key.X;
                case KeyCode.Y: return Key.Y;
                case KeyCode.Z: return Key.Z;
                
                // 功能键
                case KeyCode.Tab: return Key.Tab;
                case KeyCode.Space: return Key.Space;
                case KeyCode.Return:
                case KeyCode.KeypadEnter: return Key.Enter;
                case KeyCode.Escape: return Key.Escape;
                case KeyCode.LeftShift: return Key.LeftShift;
                case KeyCode.RightShift: return Key.RightShift;
                case KeyCode.LeftControl: return Key.LeftCtrl;
                case KeyCode.RightControl: return Key.RightCtrl;
                case KeyCode.LeftAlt: return Key.LeftAlt;
                case KeyCode.RightAlt: return Key.RightAlt;
                
                default:
                    // 对于其他按键，尝试通过名称转换
                    string keyName = keyCode.ToString();
                    if (System.Enum.TryParse<Key>(keyName, true, out Key result))
                    {
                        return result;
                    }
                    Debug.LogWarning($"SkillTreePanel: 无法转换 KeyCode {keyCode} 到新的 Key 枚举", this);
                    return null;
            }
        }

        /// <summary>
        /// 显示技能树面板
        /// </summary>
        public void Show()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f; // 完全不透明
                canvasGroup.interactable = true; // 可交互
                canvasGroup.blocksRaycasts = true; // 阻挡射线（可以点击）
                isVisible = true;
                Debug.Log("SkillTreePanel: 面板已显示", this);
            }
            else
            {
                Debug.LogWarning("SkillTreePanel: canvasGroup 为空，无法显示面板！", this);
            }
        }

        /// <summary>
        /// 隐藏技能树面板
        /// </summary>
        public void Hide()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f; // 完全透明
                canvasGroup.interactable = false; // 不可交互
                canvasGroup.blocksRaycasts = false; // 不阻挡射线（不能点击）
                isVisible = false;
                Debug.Log("SkillTreePanel: 面板已隐藏", this);
            }
            else
            {
                Debug.LogWarning("SkillTreePanel: canvasGroup 为空，无法隐藏面板！", this);
            }
        }

        /// <summary>
        /// 切换面板显示/隐藏状态
        /// </summary>
        public void TogglePanel()
        {
            if (isVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        /// <summary>
        /// 检查面板是否可见
        /// </summary>
        /// <returns>是否可见</returns>
        public bool IsVisible()
        {
            return isVisible;
        }

        /// <summary>
        /// 返回主菜单按钮点击事件
        /// </summary>
        private void OnCloseButtonClicked()
        {
            Debug.Log("SkillTreePanel: 点击返回按钮", this);

            // 隐藏技能树面板（使用 CanvasGroup 方式）
            Hide();

            // 显示主菜单面板
            if (mainMenuPanel != null)
            {
                // 确保当前在 Preparation 状态
                if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Preparation)
                {
                    // 主动显示主菜单面板（因为可能被隐藏了）
                    mainMenuPanel.ShowMainMenu();
                    Debug.Log("SkillTreePanel: 已返回主菜单", this);
                }
                else
                {
                    Debug.LogWarning($"SkillTreePanel: 当前不在 Preparation 状态（当前状态：{GameManager.Instance?.CurrentState}），主菜单可能不会显示", this);
                }
            }
            else
            {
                Debug.LogWarning("SkillTreePanel: mainMenuPanel 为空，无法返回主菜单！请确保 MainMenuPanel 在场景中存在。", this);
            }
        }

        /// <summary>
        /// 刷新 UI 显示（确保显示的鸡蛋数是最新的全局货币）
        /// </summary>
        private void RefreshUI()
        {
            // 刷新所有技能按钮的 UI 状态
            SkillSlotUI[] skillSlots = GetComponentsInChildren<SkillSlotUI>(true);
            foreach (SkillSlotUI slot in skillSlots)
            {
                if (slot != null)
                {
                    slot.RefreshUI();
                }
            }

            Debug.Log("SkillTreePanel: UI 已刷新", this);
        }

        /// <summary>
        /// 全局货币变化事件处理
        /// </summary>
        /// <param name="newAmount">新的全局货币数量</param>
        private void OnGlobalEggsChanged(int newAmount)
        {
            // 当全局货币变化时，刷新所有技能按钮的UI
            RefreshUI();
        }

        /// <summary>
        /// 初始化技能槽位：建立映射并订阅悬停事件
        /// </summary>
        private void InitializeSkillSlots()
        {
            skillSlotMap = new Dictionary<SkillNodeData, SkillSlotUI>();
            SkillSlotUI[] skillSlots = GetComponentsInChildren<SkillSlotUI>(true);

            foreach (SkillSlotUI slot in skillSlots)
            {
                if (slot != null)
                {
                    SkillNodeData skillData = slot.TargetSkill;
                    if (skillData != null)
                    {
                        skillSlotMap[skillData] = slot;

                        // 订阅悬停事件
                        SubscribeToHoverEvents(slot);
                    }
                }
            }

            // 绘制连线
            DrawConnections();
        }

        /// <summary>
        /// 订阅技能槽位的悬停事件
        /// </summary>
        /// <param name="slot">技能槽位</param>
        private void SubscribeToHoverEvents(SkillSlotUI slot)
        {
            // 通过反射获取 OnHoverEnter 和 OnHoverExit 事件
            var hoverEnterField = typeof(SkillSlotUI).GetField("OnHoverEnter", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var hoverExitField = typeof(SkillSlotUI).GetField("OnHoverExit", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (hoverEnterField != null)
            {
                var hoverEnterEvent = hoverEnterField.GetValue(slot) as UnityEngine.Events.UnityEvent<SkillNodeData>;
                if (hoverEnterEvent != null)
                {
                    hoverEnterEvent.AddListener(ShowTooltip);
                }
            }

            if (hoverExitField != null)
            {
                var hoverExitEvent = hoverExitField.GetValue(slot) as UnityEngine.Events.UnityEvent<SkillNodeData>;
                if (hoverExitEvent != null)
                {
                    hoverExitEvent.AddListener(HideTooltip);
                }
            }
        }

        /// <summary>
        /// 显示 Tooltip
        /// </summary>
        /// <param name="data">技能节点数据</param>
        private void ShowTooltip(SkillNodeData data)
        {
            if (tooltipPanel == null || data == null)
            {
                return;
            }

            // 设置技能图标
            if (tooltipIcon != null)
            {
                if (data.Icon != null)
                {
                    tooltipIcon.sprite = data.Icon;
                    tooltipIcon.gameObject.SetActive(true);
                }
                else
                {
                    tooltipIcon.gameObject.SetActive(false);
                }
            }

            // 设置技能名称（标题）
            if (tooltipTitle != null)
            {
                tooltipTitle.text = data.DisplayName;
            }

            // 设置技能描述
            if (tooltipDesc != null)
            {
                tooltipDesc.text = data.Description;
            }

            // 设置等级和消耗信息
            if (tooltipCost != null && UpgradeManager.Instance != null)
            {
                int currentLevel = UpgradeManager.Instance.GetSkillLevel(data);
                int maxLevel = data.MaxLevel;
                bool isMaxLevel = UpgradeManager.Instance.IsMaxLevel(data);
                
                if (isMaxLevel)
                {
                    // 已满级：显示 "等级: 最大等级/最大等级"
                    tooltipCost.text = $"等级: {maxLevel}/{maxLevel} (已满级)";
                }
                else
                {
                    // 未满级：显示 "等级: 当前等级/最大等级 | 升级消耗: X 鸡蛋"
                    int nextLevelCost = data.GetCost(currentLevel);
                    int nextLevel = currentLevel + 1;
                    tooltipCost.text = $"等级: {currentLevel}/{maxLevel} → {nextLevel}/{maxLevel} | 消耗: {nextLevelCost} 鸡蛋";
                }
            }
            else if (tooltipCost != null)
            {
                // 如果 UpgradeManager 不可用，至少显示最大等级
                tooltipCost.text = $"最大等级: {data.MaxLevel}";
            }

            // 定位 Tooltip（显示在技能节点上方，保持在屏幕内）
            PositionTooltipAboveSkill(data);

            // 激活面板
            tooltipPanel.SetActive(true);
        }

        /// <summary>
        /// 隐藏 Tooltip
        /// </summary>
        private void HideTooltip(SkillNodeData data)
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }

        /// <summary>
        /// 定位 Tooltip 面板（显示在技能节点上方，保持在屏幕内）
        /// </summary>
        /// <param name="skillData">技能节点数据</param>
        private void PositionTooltipAboveSkill(SkillNodeData skillData)
        {
            if (tooltipPanel == null || canvas == null || skillData == null)
            {
                return;
            }

            RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
            if (tooltipRect == null)
            {
                return;
            }

            // 查找对应的技能槽位
            if (skillSlotMap == null || !skillSlotMap.ContainsKey(skillData))
            {
                Debug.LogWarning($"SkillTreePanel: 未找到技能 {skillData.DisplayName} 对应的槽位", this);
                return;
            }

            SkillSlotUI skillSlot = skillSlotMap[skillData];
            if (skillSlot == null)
            {
                return;
            }

            RectTransform skillSlotRect = skillSlot.GetComponent<RectTransform>();
            if (skillSlotRect == null)
            {
                return;
            }

            // 获取技能槽位在 Canvas 中的位置
            Vector3[] skillSlotCorners = new Vector3[4];
            skillSlotRect.GetWorldCorners(skillSlotCorners);
            
            // 获取技能槽位的顶部中心位置（世界坐标）
            Vector3 skillSlotTopCenter = (skillSlotCorners[1] + skillSlotCorners[2]) / 2f; // 顶部两个角的中心

            // 转换为 Canvas 本地坐标
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                RectTransformUtility.WorldToScreenPoint(canvas.worldCamera ?? Camera.main, skillSlotTopCenter),
                canvas.worldCamera ?? Camera.main,
                out Vector2 localPoint);

            // 获取 Tooltip 的尺寸（需要先激活才能获取正确尺寸）
            bool wasActive = tooltipPanel.activeSelf;
            if (!wasActive)
            {
                tooltipPanel.SetActive(true);
            }
            
            // 强制布局更新
            Canvas.ForceUpdateCanvases();
            
            Vector2 tooltipSize = tooltipRect.sizeDelta;
            if (canvas.scaleFactor != 0)
            {
                tooltipSize *= canvas.scaleFactor;
            }

            // 计算 Tooltip 位置：技能槽位上方，居中对齐
            // localPoint 已经是技能槽位顶部中心的位置
            // 需要向上偏移 Tooltip 的高度 + 一些间距
            float spacing = 10f; // Tooltip 和技能槽位之间的间距
            localPoint.y += tooltipSize.y / 2f + spacing;

            // 水平居中：Tooltip 的中心对齐技能槽位的中心
            // localPoint.x 已经是中心位置，不需要额外调整

            // 获取 Canvas 的边界
            RectTransform canvasRect = canvas.transform as RectTransform;
            Rect canvasRectBounds = canvasRect.rect;

            // 确保 Tooltip 在屏幕内
            float minX = canvasRectBounds.xMin + tooltipSize.x / 2f;
            float maxX = canvasRectBounds.xMax - tooltipSize.x / 2f;
            float minY = canvasRectBounds.yMin + tooltipSize.y / 2f;
            float maxY = canvasRectBounds.yMax - tooltipSize.y / 2f;

            localPoint.x = Mathf.Clamp(localPoint.x, minX, maxX);
            localPoint.y = Mathf.Clamp(localPoint.y, minY, maxY);

            // 设置位置
            tooltipRect.localPosition = localPoint;

            // 如果之前是非激活状态，恢复状态（但这里应该保持激活，因为 ShowTooltip 会激活它）
        }

        /// <summary>
        /// 绘制连接线（根据 prerequisites）
        /// </summary>
        private void DrawConnections()
        {
            if (linePrefab == null || lineContainer == null || skillSlotMap == null)
            {
                Debug.LogWarning("SkillTreePanel: 连线系统配置不完整，无法绘制连线", this);
                return;
            }

            // 清除现有连线
            foreach (Transform child in lineContainer)
            {
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            // 为每个槽位绘制到其前置技能的连线
            foreach (var kvp in skillSlotMap)
            {
                SkillNodeData childData = kvp.Key;
                SkillSlotUI childSlot = kvp.Value;

                if (childData.Prerequisites != null && childData.Prerequisites.Count > 0)
                {
                    foreach (SkillNodeData prerequisiteData in childData.Prerequisites)
                    {
                        if (prerequisiteData != null && skillSlotMap.ContainsKey(prerequisiteData))
                        {
                            SkillSlotUI parentSlot = skillSlotMap[prerequisiteData];
                            CreateLine(parentSlot.GetComponent<RectTransform>(), 
                                      childSlot.GetComponent<RectTransform>(), 
                                      linePrefab, 
                                      lineContainer);
                        }
                    }
                }
                // 兼容旧的单前置技能系统
                else if (childData.Prerequisite != null && skillSlotMap.ContainsKey(childData.Prerequisite))
                {
                    SkillSlotUI parentSlot = skillSlotMap[childData.Prerequisite];
                    CreateLine(parentSlot.GetComponent<RectTransform>(), 
                              childSlot.GetComponent<RectTransform>(), 
                              linePrefab, 
                              lineContainer);
                }
            }
        }

        /// <summary>
        /// 创建连接线
        /// </summary>
        /// <param name="start">起始 RectTransform</param>
        /// <param name="end">结束 RectTransform</param>
        /// <param name="linePrefab">连线预制体</param>
        /// <param name="container">容器 Transform</param>
        private void CreateLine(RectTransform start, RectTransform end, GameObject linePrefab, Transform container)
        {
            if (start == null || end == null || linePrefab == null || container == null)
            {
                return;
            }

            GameObject line = Instantiate(linePrefab, container);
            RectTransform lineRect = line.GetComponent<RectTransform>();
            
            if (lineRect == null)
            {
                Debug.LogWarning("SkillTreePanel: 连线预制体缺少 RectTransform 组件", this);
                Destroy(line);
                return;
            }

            // 设置锚点为左中，方便旋转和拉伸
            lineRect.pivot = new Vector2(0, 0.5f);
            lineRect.position = start.position;
            
            Vector3 diff = end.position - start.position;
            float distance = diff.magnitude;
            
            // 画布缩放校正（如果 Canvas Scaler 生效，需除以 scaleFactor）
            if (canvas != null && canvas.scaleFactor != 0)
            {
                distance = distance / canvas.scaleFactor;
            }
            
            lineRect.sizeDelta = new Vector2(distance, lineWidth);
            float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
            lineRect.rotation = Quaternion.Euler(0, 0, angle);
            
            // 将线放到底层，避免挡住图标
            lineRect.SetAsFirstSibling();
        }
    }
}

