using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;
using ProjectChicken.Core;

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

        private bool isVisible = false; // 当前是否可见

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
    }
}

