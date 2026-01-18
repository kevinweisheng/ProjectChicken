using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;
using System.Collections;
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
        [SerializeField] private Button closeButton; // 返回主菜单按钮（可选）
        [SerializeField] private Button startGameButton; // 开始游戏按钮
        [SerializeField] private TMP_Text eggsCountText; // 剩余鸡蛋数显示文本

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

        [Header("缩放和拖拽系统")]
        [SerializeField] private RectTransform skillTreeContent; // 技能树内容容器（用于缩放和拖拽）
        [SerializeField] private float minScale = 0.5f; // 最小缩放比例
        [SerializeField] private float maxScale = 2.0f; // 最大缩放比例
        [SerializeField] private float zoomSpeed = 0.1f; // 缩放速度
        [SerializeField] private float dragSpeed = 1.0f; // 拖拽速度

        private bool isVisible = false; // 当前是否可见
        private Dictionary<SkillNodeData, SkillSlotUI> skillSlotMap; // 技能数据到槽位的映射
        private Canvas canvas; // Canvas 引用（用于计算缩放）
        
        // 拖拽相关
        private bool isDragging = false; // 是否正在拖拽
        private Vector2 lastMousePosition; // 上次鼠标位置

        /// <summary>
        /// 编辑器模式下：当Inspector中的字段改变时自动调用（用于刷新连线显示）
        /// </summary>
        private void OnValidate()
        {
            // 只在编辑模式下执行，且不运行游戏时
            if (!Application.isPlaying)
            {
                // 确保 Canvas 引用已获取
                if (canvas == null)
                {
                    canvas = GetComponentInParent<Canvas>();
                    if (canvas == null)
                    {
                        canvas = FindFirstObjectByType<Canvas>();
                    }
                }
                
                // 注意：不能在 OnValidate 中直接调用 DestroyImmediate
                // 使用编辑器延迟回调来刷新连线
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null && !Application.isPlaying)
                    {
                        RefreshConnectionsInEditor();
                    }
                };
                #endif
            }
        }

        /// <summary>
        /// 编辑器模式下刷新连线（右键菜单选项）
        /// </summary>
        [ContextMenu("刷新连线")]
        private void RefreshConnectionsInEditor()
        {
            if (!Application.isPlaying)
            {
                // 编辑模式下也需要初始化 parentSlots，确保连线关系正确
                InitializeSkillSlotsForEditor();
                DrawConnections();
            }
        }

        /// <summary>
        /// 编辑器模式下自动配置所有 SkillSlotUI 的 parentSlots（右键菜单选项）
        /// </summary>
        [ContextMenu("自动配置父子关系")]
        private void AutoConfigureParentSlots()
        {
            if (!Application.isPlaying)
            {
                InitializeSkillSlotsForEditor();
                Debug.Log("SkillTreePanel: 已自动配置所有 SkillSlotUI 的 parentSlots。请在 Inspector 中查看 SkillSlotUI 组件确认。", this);
            }
            else
            {
                Debug.LogWarning("SkillTreePanel: 此功能仅在编辑模式下可用！", this);
            }
        }

        /// <summary>
        /// 编辑器模式下初始化技能槽位的 parentSlots（不依赖运行时系统）
        /// </summary>
        private void InitializeSkillSlotsForEditor()
        {
            #if UNITY_EDITOR
            Dictionary<SkillNodeData, SkillSlotUI> localSkillSlotMap = new Dictionary<SkillNodeData, SkillSlotUI>();
            SkillSlotUI[] skillSlots = GetComponentsInChildren<SkillSlotUI>(true);

            Debug.Log($"SkillTreePanel: 开始初始化 {skillSlots.Length} 个技能槽位的父子关系", this);

            // 第一遍：建立映射
            foreach (SkillSlotUI slot in skillSlots)
            {
                if (slot != null)
                {
                    SkillNodeData skillData = slot.TargetSkill;
                    if (skillData != null)
                    {
                        localSkillSlotMap[skillData] = slot;
                    }
                }
            }

            Debug.Log($"SkillTreePanel: 已建立 {localSkillSlotMap.Count} 个技能数据到槽位的映射", this);

            int slotsConfigured = 0;
            int totalParentSlotsAdded = 0;

            // 第二遍：根据 SkillNodeData 的购买条件自动设置 parentSlots
            foreach (SkillSlotUI slot in skillSlots)
            {
                if (slot != null)
                {
                    SkillNodeData skillData = slot.TargetSkill;
                    if (skillData != null)
                    {
                        int beforeCount = slot.parentSlots.Count;
                        
                        // 清空现有的 parentSlots（如果之前手动配置过）
                        slot.parentSlots.Clear();

                        // 根据 SkillNodeData 的 prerequisite 设置父槽位
                        if (skillData.Prerequisite != null)
                        {
                            if (localSkillSlotMap.TryGetValue(skillData.Prerequisite, out SkillSlotUI parentSlot))
                            {
                                if (!slot.parentSlots.Contains(parentSlot))
                                {
                                    slot.parentSlots.Add(parentSlot);
                                    totalParentSlotsAdded++;
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"SkillTreePanel: 技能 {skillData.DisplayName} 的前置技能 {skillData.Prerequisite.DisplayName} 未在场景中找到对应的 SkillSlotUI", this);
                            }
                        }

                        // 根据 SkillNodeData 的 prerequisites 列表设置父槽位
                        if (skillData.Prerequisites != null && skillData.Prerequisites.Count > 0)
                        {
                            foreach (SkillNodeData prereq in skillData.Prerequisites)
                            {
                                if (prereq != null)
                                {
                                    if (localSkillSlotMap.TryGetValue(prereq, out SkillSlotUI parentSlot))
                                    {
                                        if (!slot.parentSlots.Contains(parentSlot))
                                        {
                                            slot.parentSlots.Add(parentSlot);
                                            totalParentSlotsAdded++;
                                        }
                                    }
                                    else
                                    {
                                        Debug.LogWarning($"SkillTreePanel: 技能 {skillData.DisplayName} 的前置技能 {prereq.DisplayName} 未在场景中找到对应的 SkillSlotUI", this);
                                    }
                                }
                            }
                        }

                        // 标记对象为已修改，以便 Unity 保存更改并在 Inspector 中显示
                        if (slot.parentSlots.Count != beforeCount)
                        {
                            UnityEditor.EditorUtility.SetDirty(slot);
                            slotsConfigured++;
                        }
                    }
                }
            }

            // 标记当前对象为已修改
            UnityEditor.EditorUtility.SetDirty(this);

            Debug.Log($"SkillTreePanel: 编辑器模式下已初始化 {slotsConfigured} 个技能槽位的父子关系，共添加 {totalParentSlotsAdded} 个父技能引用", this);
            #endif
        }

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
            // 注意：返回按钮是可选的，不配置也不会报错

            // 绑定开始游戏按钮的点击事件
            if (startGameButton != null)
            {
                startGameButton.onClick.AddListener(OnStartGameClicked);
            }
            else
            {
                Debug.LogWarning("SkillTreePanel: startGameButton 未配置！", this);
            }

            // 获取 Canvas 引用
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }

            // 如果没有手动指定技能树内容容器，尝试自动查找（通常是第一个RectTransform子对象）
            if (skillTreeContent == null)
            {
                skillTreeContent = GetComponent<RectTransform>();
                // 或者查找第一个子对象的RectTransform
                if (skillTreeContent != null && skillTreeContent.childCount > 0)
                {
                    Transform firstChild = skillTreeContent.GetChild(0);
                    RectTransform childRect = firstChild.GetComponent<RectTransform>();
                    if (childRect != null)
                    {
                        skillTreeContent = childRect;
                    }
                }
            }

            // 确保连线容器是技能树内容容器的子对象，这样连线才能跟随缩放和拖拽
            EnsureLineContainerParentage();

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

            // 如果面板可见，处理缩放和拖拽
            if (isVisible)
            {
                HandleSkillTreeZoom();
                HandleSkillTreeDrag();
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
            
            // 确保连线容器是激活的
            if (lineContainer != null && !lineContainer.gameObject.activeSelf)
            {
                lineContainer.gameObject.SetActive(true);
            }
            
            // 刷新所有技能槽位的UI状态（确保显示正确的解锁状态）
            RefreshAllSkillSlots();
            
            // 强制更新 Canvas，确保所有 UI 状态已更新
            Canvas.ForceUpdateCanvases();
            
            // 延迟一帧后重新绘制连线，确保所有技能槽位的状态已更新
            if (Application.isPlaying)
            {
                StartCoroutine(DelayedDrawConnections());
            }
            else
            {
                // 编辑模式下直接绘制
                DrawConnections();
            }
            
            // 更新剩余鸡蛋数显示
            UpdateEggsCountDisplay();
        }

        /// <summary>
        /// 延迟绘制连线（确保技能槽位状态已更新）
        /// </summary>
        private IEnumerator DelayedDrawConnections()
        {
            // 等待一帧，确保所有技能槽位的 UpdateUIState() 已完成
            yield return null;
            
            // 重新绘制连线
            DrawConnections();
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

            // 隐藏技能树面板
            Hide();

            // 显示主菜单面板（游戏启动时的主菜单）
            if (mainMenuPanel != null)
            {
                mainMenuPanel.ShowMainMenu();
                Debug.Log("SkillTreePanel: 已返回主菜单", this);
            }
            else
            {
                Debug.LogWarning("SkillTreePanel: mainMenuPanel 为空，无法返回主菜单！请确保 MainMenuPanel 在场景中存在。", this);
            }
        }

        /// <summary>
        /// 开始游戏按钮点击事件
        /// </summary>
        private void OnStartGameClicked()
        {
            Debug.Log("SkillTreePanel: 点击开始游戏按钮", this);

            // 隐藏技能树面板
            Hide();

            // 开始新的一局游戏
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
                Debug.Log("SkillTreePanel: 已开始新一局游戏", this);
            }
            else
            {
                Debug.LogError("SkillTreePanel: GameManager.Instance 为空，无法开始游戏！", this);
            }
        }

        /// <summary>
        /// 刷新 UI 显示（确保显示的鸡蛋数是最新的全局货币）
        /// </summary>
        private void RefreshUI()
        {
            RefreshAllSkillSlots();
            
            // 更新剩余鸡蛋数显示
            UpdateEggsCountDisplay();

            Debug.Log("SkillTreePanel: UI 已刷新", this);
        }

        /// <summary>
        /// 刷新所有技能槽位的UI状态（供外部调用，例如技能升级后）
        /// </summary>
        public void RefreshAllSkillSlots()
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
        }

        /// <summary>
        /// 全局货币变化事件处理
        /// </summary>
        /// <param name="newAmount">新的全局货币数量</param>
        private void OnGlobalEggsChanged(int newAmount)
        {
            // 当全局货币变化时，刷新所有技能按钮的UI
            RefreshUI();
            
            // 更新剩余鸡蛋数显示
            UpdateEggsCountDisplay();
        }
        
        /// <summary>
        /// 更新剩余鸡蛋数显示
        /// </summary>
        private void UpdateEggsCountDisplay()
        {
            if (eggsCountText != null)
            {
                if (ResourceManager.Instance != null)
                {
                    int totalEggs = ResourceManager.Instance.TotalGlobalEggs;
                    eggsCountText.text = totalEggs.ToString();
                }
                else
                {
                    eggsCountText.text = "0";
                    Debug.LogWarning("SkillTreePanel: ResourceManager.Instance 为空，无法读取剩余鸡蛋数！", this);
                }
            }
        }

        /// <summary>
        /// 确保连线容器是技能树内容容器的子对象，这样连线才能跟随缩放和拖拽
        /// </summary>
        private void EnsureLineContainerParentage()
        {
            if (skillTreeContent == null || lineContainer == null)
            {
                return; // 如果没有设置，无法处理
            }

            // 检查连线容器是否已经是技能树内容容器的子对象
            if (lineContainer.parent != skillTreeContent)
            {
                // 如果不是，将其设置为子对象
                lineContainer.SetParent(skillTreeContent, false); // false = 保持世界坐标位置不变
                
                // 设置连线容器在最底层（作为第一个子对象）
                lineContainer.SetAsFirstSibling();
                
                Debug.Log("SkillTreePanel: 已将连线容器设置为技能树内容容器的子对象，连线现在会跟随缩放和拖拽", this);
            }
        }

        /// <summary>
        /// 初始化技能槽位：建立映射、自动设置父子关系并订阅悬停事件
        /// </summary>
        private void InitializeSkillSlots()
        {
            skillSlotMap = new Dictionary<SkillNodeData, SkillSlotUI>();
            SkillSlotUI[] skillSlots = GetComponentsInChildren<SkillSlotUI>(true);

            // 第一遍：建立映射
            foreach (SkillSlotUI slot in skillSlots)
            {
                if (slot != null)
                {
                    SkillNodeData skillData = slot.TargetSkill;
                    if (skillData != null)
                    {
                        skillSlotMap[skillData] = slot;
                    }
                }
            }

            // 第二遍：根据 SkillNodeData 的购买条件自动设置 parentSlots
            foreach (SkillSlotUI slot in skillSlots)
            {
                if (slot != null)
                {
                    SkillNodeData skillData = slot.TargetSkill;
                    if (skillData != null)
                    {
                        // 清空现有的 parentSlots（如果之前手动配置过）
                        slot.parentSlots.Clear();

                        // 根据 SkillNodeData 的 prerequisite 设置父槽位
                        if (skillData.Prerequisite != null)
                        {
                            if (skillSlotMap.TryGetValue(skillData.Prerequisite, out SkillSlotUI parentSlot))
                            {
                                if (!slot.parentSlots.Contains(parentSlot))
                                {
                                    slot.parentSlots.Add(parentSlot);
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"SkillTreePanel: 技能 {skillData.DisplayName} 的前置技能 {skillData.Prerequisite.DisplayName} 未在场景中找到对应的 SkillSlotUI", this);
                            }
                        }

                        // 根据 SkillNodeData 的 prerequisites 列表设置父槽位
                        if (skillData.Prerequisites != null && skillData.Prerequisites.Count > 0)
                        {
                            foreach (SkillNodeData prereq in skillData.Prerequisites)
                            {
                                if (prereq != null)
                                {
                                    if (skillSlotMap.TryGetValue(prereq, out SkillSlotUI parentSlot))
                                    {
                                        if (!slot.parentSlots.Contains(parentSlot))
                                        {
                                            slot.parentSlots.Add(parentSlot);
                                        }
                                    }
                                    else
                                    {
                                        Debug.LogWarning($"SkillTreePanel: 技能 {skillData.DisplayName} 的前置技能 {prereq.DisplayName} 未在场景中找到对应的 SkillSlotUI", this);
                                    }
                                }
                            }
                        }

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
        /// 绘制连接线（根据 SkillSlotUI.parentSlots 手动引用）
        /// </summary>
        private void DrawConnections()
        {
            if (linePrefab == null || lineContainer == null)
            {
                Debug.LogWarning("SkillTreePanel: 连线系统配置不完整，无法绘制连线", this);
                return;
            }

            // 确保连线容器是激活的
            if (lineContainer != null && !lineContainer.gameObject.activeSelf)
            {
                lineContainer.gameObject.SetActive(true);
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

            // 获取所有技能槽位UI组件（包括隐藏的，用于检查连线关系）
            SkillSlotUI[] allSkillSlots = GetComponentsInChildren<SkillSlotUI>(true);

            int linesCreated = 0;
            int skippedHidden = 0;
            int skippedNoParentSlots = 0;

            // 遍历每个子槽位，为其绘制到所有父槽位的连线
            foreach (SkillSlotUI childSlot in allSkillSlots)
            {
                if (childSlot == null)
                {
                    continue;
                }

                if (childSlot.parentSlots == null || childSlot.parentSlots.Count == 0)
                {
                    skippedNoParentSlots++;
                    continue;
                }

                // 编辑模式下：显示所有连线（不检查 activeInHierarchy）
                // 运行时：只绘制可见的技能槽位之间的连线（检查 activeInHierarchy）
                bool isEditorMode = !Application.isPlaying;
                bool shouldDrawChild = isEditorMode || childSlot.gameObject.activeInHierarchy;

                if (!shouldDrawChild)
                {
                    skippedHidden++;
                    continue; // 运行时跳过隐藏的技能槽位
                }

                // 遍历该子槽位的所有父槽位
                foreach (SkillSlotUI parentSlot in childSlot.parentSlots)
                {
                    if (parentSlot != null)
                    {
                        // 编辑模式下：显示所有连线（不检查 activeInHierarchy）
                        // 运行时：只绘制可见的父技能槽位之间的连线（检查 activeInHierarchy）
                        bool shouldDrawParent = isEditorMode || parentSlot.gameObject.activeInHierarchy;

                        if (!shouldDrawParent)
                        {
                            skippedHidden++;
                            continue; // 运行时跳过隐藏的父技能槽位
                        }

                        // 获取 RectTransform（如果不存在则跳过）
                        RectTransform parentRect = parentSlot.GetComponent<RectTransform>();
                        RectTransform childRect = childSlot.GetComponent<RectTransform>();

                        if (parentRect == null || childRect == null)
                        {
                            Debug.LogWarning($"SkillTreePanel: 技能槽位缺少 RectTransform 组件。父: {parentSlot.name}, 子: {childSlot.name}", this);
                            continue;
                        }

                        // 检查子槽位是否已解锁（用于设置线条颜色/材质）
                        bool isUnlocked = IsSlotUnlocked(childSlot);
                        
                        // 创建连线
                        CreateLine(
                            parentRect, 
                            childRect, 
                            linePrefab, 
                            lineContainer,
                            isUnlocked
                        );
                        
                        linesCreated++;
                    }
                }
            }

            Debug.Log($"SkillTreePanel: 绘制连线完成，共创建 {linesCreated} 条连线（编辑模式: {!Application.isPlaying}，跳过隐藏: {skippedHidden}，跳过无父槽位: {skippedNoParentSlots}）", this);
            
            // 运行时添加详细调试信息，检查连线是否真的被创建
            if (Application.isPlaying && linesCreated > 0)
            {
                int activeLineCount = 0;
                foreach (Transform child in lineContainer)
                {
                    if (child != null && child.gameObject.activeSelf)
                    {
                        activeLineCount++;
                        Image img = child.GetComponent<Image>();
                        if (img != null)
                        {
                            Debug.Log($"SkillTreePanel: 连线 {child.name} - 颜色: {img.color}, 启用: {img.enabled}, 可见: {child.gameObject.activeInHierarchy}", this);
                        }
                    }
                }
                Debug.Log($"SkillTreePanel: 连线容器中共有 {lineContainer.childCount} 个子对象，其中 {activeLineCount} 个是激活的", this);
            }
        }

        /// <summary>
        /// 检查技能槽位是否应该隐藏（不可解锁的技能）
        /// </summary>
        /// <param name="slot">技能槽位</param>
        /// <returns>是否应该隐藏</returns>
        private bool ShouldHideSlot(SkillSlotUI slot)
        {
            if (slot == null || slot.TargetSkill == null)
            {
                return true; // 无效的槽位，应该隐藏
            }

            // 编辑模式下：不隐藏（显示所有技能以便编辑）
            if (!Application.isPlaying || UpgradeManager.Instance == null)
            {
                return false;
            }

            // 检查技能是否已解锁
            bool isUnlocked = UpgradeManager.Instance.IsNodeUnlocked(slot.TargetSkill);
            
            // 如果已解锁，不隐藏
            if (isUnlocked)
            {
                return false;
            }

            // 检查前置条件是否满足（是否可以解锁）
            // 只要任意一个前置技能已解锁（level > 0），就可以解锁当前技能
            bool canUnlock = false;
            
            // 如果没有前置技能（既没有 Prerequisite 也没有 parentSlots），可以直接解锁
            if (slot.TargetSkill.Prerequisite == null && (slot.parentSlots == null || slot.parentSlots.Count == 0))
            {
                canUnlock = true;
            }
            else
            {
                // 优先检查 parentSlots 列表（手动配置的父子关系）
                if (slot.parentSlots != null && slot.parentSlots.Count > 0)
                {
                    foreach (SkillSlotUI parentSlot in slot.parentSlots)
                    {
                        if (parentSlot != null && parentSlot.TargetSkill != null)
                        {
                            // 只要任意一个父技能已解锁（level > 0），就可以解锁当前技能
                            if (UpgradeManager.Instance.IsNodeUnlocked(parentSlot.TargetSkill))
                            {
                                canUnlock = true;
                                break;
                            }
                        }
                    }
                }
                
                // 如果 parentSlots 中没有已解锁的，回退检查 Prerequisite（单个前置技能）
                if (!canUnlock && slot.TargetSkill.Prerequisite != null)
                {
                    canUnlock = UpgradeManager.Instance.IsNodeUnlocked(slot.TargetSkill.Prerequisite);
                }
            }

            // 如果不可解锁（未解锁且前置不满足），应该隐藏
            return !canUnlock;
        }

        /// <summary>
        /// 检查技能槽位是否已解锁
        /// </summary>
        /// <param name="slot">技能槽位</param>
        /// <returns>是否已解锁</returns>
        private bool IsSlotUnlocked(SkillSlotUI slot)
        {
            if (slot == null || slot.TargetSkill == null)
            {
                return false;
            }

            // 编辑模式下：默认返回 false（显示为灰色线条）
            if (!Application.isPlaying || UpgradeManager.Instance == null)
            {
                return false;
            }

            return UpgradeManager.Instance.IsNodeUnlocked(slot.TargetSkill);
        }

        /// <summary>
        /// 创建连接线
        /// </summary>
        /// <param name="start">起始 RectTransform</param>
        /// <param name="end">结束 RectTransform</param>
        /// <param name="linePrefab">连线预制体</param>
        /// <param name="container">容器 Transform</param>
        /// <param name="isUnlocked">是否已解锁（用于设置线条颜色）</param>
        private void CreateLine(RectTransform start, RectTransform end, GameObject linePrefab, Transform container, bool isUnlocked = false)
        {
            if (start == null || end == null || linePrefab == null || container == null)
            {
                return;
            }

            GameObject line;
            
            // 编辑模式下实例化并注册到场景
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // 编辑模式下使用 PrefabUtility.InstantiatePrefab 以保持与预制体的连接
                line = UnityEditor.PrefabUtility.InstantiatePrefab(linePrefab, container) as GameObject;
                if (line != null)
                {
                    // 确保对象在场景中可见
                    line.hideFlags = HideFlags.DontSave;
                    // 标记场景为已修改
                    UnityEditor.EditorUtility.SetDirty(container.gameObject);
                    UnityEditor.Undo.RegisterCreatedObjectUndo(line, "Create Skill Tree Line");
                }
            }
            else
            {
                line = Instantiate(linePrefab, container);
            }
            #else
            line = Instantiate(linePrefab, container);
            #endif
            
            if (line == null)
            {
                return;
            }
            
            RectTransform lineRect = line.GetComponent<RectTransform>();
            
            if (lineRect == null)
            {
                Debug.LogWarning("SkillTreePanel: 连线预制体缺少 RectTransform 组件", this);
                #if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    UnityEditor.Undo.DestroyObjectImmediate(line);
                }
                else
                #endif
                {
                    Destroy(line);
                }
                return;
            }

            // 确保连线 GameObject 是激活的
            line.SetActive(true);
            
            // 设置锚点为左中，方便旋转和拉伸
            lineRect.pivot = new Vector2(0, 0.5f);
            
            // 获取世界坐标
            Vector3 startWorldPos = start.position;
            Vector3 endWorldPos = end.position;
            
            // 声明变量（用于调试日志）
            float distance = 0f;
            float angle = 0f;
            
            // 将世界坐标转换为相对于 lineContainer 的本地坐标
            // 因为 lineContainer 可能是 skillTreeContent 的子对象，需要正确转换
            RectTransform containerRect = container as RectTransform;
            if (containerRect != null)
            {
                // 使用 RectTransformUtility 进行正确的坐标转换
                Camera uiCamera = null;
                if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    uiCamera = canvas.worldCamera;
                }
                
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    containerRect, 
                    RectTransformUtility.WorldToScreenPoint(uiCamera, startWorldPos), 
                    uiCamera, 
                    out Vector2 startLocal);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    containerRect, 
                    RectTransformUtility.WorldToScreenPoint(uiCamera, endWorldPos), 
                    uiCamera, 
                    out Vector2 endLocal);
                
                lineRect.localPosition = startLocal;
                
                Vector2 diff = endLocal - startLocal;
                distance = diff.magnitude;
                
                lineRect.sizeDelta = new Vector2(distance, lineWidth);
                angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
                lineRect.localRotation = Quaternion.Euler(0, 0, angle);
            }
            else
            {
                // 回退到简单的世界坐标转换
                Vector3 startLocalPos = container.InverseTransformPoint(startWorldPos);
                Vector3 endLocalPos = container.InverseTransformPoint(endWorldPos);
                lineRect.localPosition = startLocalPos;
                
                Vector3 diff = endLocalPos - startLocalPos;
                distance = diff.magnitude;
                
                lineRect.sizeDelta = new Vector2(distance, lineWidth);
                angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
                lineRect.localRotation = Quaternion.Euler(0, 0, angle);
            }
            
            // 确保连线在编辑模式下可见
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // 确保连线容器是激活的
                if (container != null && !container.gameObject.activeSelf)
                {
                    container.gameObject.SetActive(true);
                }
                // 确保连线本身是激活的
                if (!line.activeSelf)
                {
                    line.SetActive(true);
                }
            }
            #endif
            
            // 根据解锁状态设置线条颜色/材质
            Image lineImage = line.GetComponent<Image>();
            if (lineImage != null)
            {
                // 已解锁：使用明显的颜色（蓝色，避免与白色背景混淆）
                // 未解锁：使用半透明灰色
                Color unlockedColor = new Color(0.2f, 0.6f, 1.0f, 1.0f); // 亮蓝色，完全不透明
                Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // 半透明灰色
                lineImage.color = isUnlocked ? unlockedColor : lockedColor;
                
                // 确保 Image 组件启用且可渲染
                if (!lineImage.enabled)
                {
                    lineImage.enabled = true;
                }
                
                // 确保 Image 的 raycastTarget 为 false，避免阻挡鼠标事件
                lineImage.raycastTarget = false;
            }
            else
            {
                Debug.LogWarning($"SkillTreePanel: 连线预制体 {linePrefab.name} 缺少 Image 组件！", this);
            }
            
            // 将线放到底层，避免挡住图标
            // 但需要确保连线在背景之上，所以不使用 SetAsFirstSibling
            // 而是将连线放在技能槽位之前，但确保它们是可见的
            // 注意：如果需要连线在图标下方，保持 SetAsFirstSibling
            // 如果需要连线在图标上方，使用 SetAsLastSibling 或移除此调用
            lineRect.SetAsFirstSibling();
            
            // 确保连线的 Renderer 或 CanvasRenderer 正常工作
            CanvasRenderer canvasRenderer = line.GetComponent<CanvasRenderer>();
            if (canvasRenderer != null && canvasRenderer.cullTransparentMesh)
            {
                // 如果启用了剔除透明网格，可能需要禁用（确保半透明线条也能显示）
                // 但通常应该保持启用以优化性能
            }
            
            // 编辑模式下添加调试信息
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Debug.Log($"SkillTreePanel: 创建连线 - 从 {start.name} 到 {end.name}, 距离: {distance:F2}, 角度: {angle:F2}°, 位置: {lineRect.position}, 大小: {lineRect.sizeDelta}", this);
            }
            #endif
        }

        /// <summary>
        /// 处理技能树的滑轮缩放
        /// </summary>
        private void HandleSkillTreeZoom()
        {
            if (skillTreeContent == null || !isVisible)
            {
                return;
            }

            // 检查鼠标是否在技能树面板区域内
            if (Mouse.current == null)
            {
                return;
            }

            // 获取鼠标滚轮输入
            float scrollDelta = Mouse.current.scroll.ReadValue().y;
            
            if (scrollDelta != 0f)
            {
                // 计算新的缩放比例
                Vector3 currentScale = skillTreeContent.localScale;
                float newScale = currentScale.x + scrollDelta * zoomSpeed;
                
                // 限制缩放范围
                newScale = Mathf.Clamp(newScale, minScale, maxScale);
                
                // 应用缩放
                skillTreeContent.localScale = new Vector3(newScale, newScale, 1f);
            }
        }

        /// <summary>
        /// 处理技能树的鼠标右键拖拽
        /// </summary>
        private void HandleSkillTreeDrag()
        {
            if (skillTreeContent == null || !isVisible)
            {
                return;
            }

            if (Mouse.current == null)
            {
                return;
            }

            // 检测鼠标右键按下
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                isDragging = true;
                lastMousePosition = Mouse.current.position.ReadValue();
            }

            // 检测鼠标右键释放
            if (Mouse.current.rightButton.wasReleasedThisFrame)
            {
                isDragging = false;
            }

            // 如果正在拖拽，移动内容
            if (isDragging)
            {
                Vector2 currentMousePosition = Mouse.current.position.ReadValue();
                Vector2 mouseDelta = currentMousePosition - lastMousePosition;
                
                // 转换为本地坐标的偏移（考虑Canvas缩放）
                if (canvas != null)
                {
                    mouseDelta /= canvas.scaleFactor;
                }
                
                // 应用拖拽速度
                mouseDelta *= dragSpeed;
                
                // 移动内容
                Vector3 currentPosition = skillTreeContent.localPosition;
                skillTreeContent.localPosition = new Vector3(
                    currentPosition.x + mouseDelta.x,
                    currentPosition.y + mouseDelta.y,
                    currentPosition.z
                );
                
                lastMousePosition = currentMousePosition;
            }
        }
    }
}

