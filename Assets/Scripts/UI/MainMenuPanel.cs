using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using ProjectChicken.Core;
using ProjectChicken.Systems;

namespace ProjectChicken.UI
{
    /// <summary>
    /// 主菜单面板：游戏启动时的主菜单（新游戏、继续游戏、退出游戏）
    /// 注意：使用 CanvasGroup 时，面板 GameObject 应始终激活，这样事件订阅才能正常工作
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class MainMenuPanel : MonoBehaviour
    {
        [Header("UI组件")]
        [SerializeField] private CanvasGroup canvasGroup; // CanvasGroup 组件（用于控制显示/隐藏）
        [SerializeField] private Button newGameButton; // 新游戏按钮
        [SerializeField] private Button continueGameButton; // 继续游戏按钮
        [SerializeField] private Button exitGameButton; // 退出游戏按钮

        [Header("系统引用")]
        [SerializeField] private SkillTreePanel skillTreePanel; // 技能树面板引用（可选，如果为空则使用单例）

        private void Awake()
        {
            // 如果没有手动指定，尝试自动获取 CanvasGroup
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (canvasGroup == null)
            {
                Debug.LogError("MainMenuPanel: 未找到 CanvasGroup 组件！请确保 GameObject 上有 CanvasGroup 组件。", this);
            }
        }

        private void Start()
        {
            // 如果没有手动指定，尝试使用单例获取 SkillTreePanel
            if (skillTreePanel == null)
            {
                skillTreePanel = SkillTreePanel.Instance;
            }

            // 绑定新游戏按钮的点击事件
            if (newGameButton != null)
            {
                newGameButton.onClick.AddListener(OnNewGameClicked);
            }
            else
            {
                Debug.LogWarning("MainMenuPanel: newGameButton 未配置！", this);
            }

            // 绑定继续游戏按钮的点击事件
            if (continueGameButton != null)
            {
                continueGameButton.onClick.AddListener(OnContinueGameClicked);
            }
            else
            {
                Debug.LogWarning("MainMenuPanel: continueGameButton 未配置！", this);
            }

            // 绑定退出游戏按钮的点击事件
            if (exitGameButton != null)
            {
                exitGameButton.onClick.AddListener(OnExitGameClicked);
            }
            else
            {
                Debug.LogWarning("MainMenuPanel: exitGameButton 未配置！", this);
            }

            // 初始状态：显示主菜单（游戏启动时）
            ShowPanel();
        }

        // Update 方法已移除，主菜单不再需要快捷键功能

        // 主菜单不再订阅游戏状态改变事件，只在游戏启动时显示

        /// <summary>
        /// 显示主菜单面板
        /// </summary>
        private void ShowPanel()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            else
            {
                Debug.LogWarning("MainMenuPanel: canvasGroup 为空，无法显示面板！", this);
            }
        }

        /// <summary>
        /// 显示主菜单面板（公共方法，供外部调用）
        /// </summary>
        public void ShowMainMenu()
        {
            ShowPanel();
        }

        /// <summary>
        /// 隐藏主菜单面板
        /// </summary>
        private void HidePanel()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f; // 完全透明
                canvasGroup.interactable = false; // 不可交互
                canvasGroup.blocksRaycasts = false; // 不阻挡射线（不能点击）
            }
        }

        /// <summary>
        /// 新游戏按钮点击事件
        /// </summary>
        private void OnNewGameClicked()
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.ClearSaveData();
            }

            HidePanel();

            if (skillTreePanel != null)
            {
                skillTreePanel.Show();
            }
            else
            {
                Debug.LogWarning("MainMenuPanel: skillTreePanel 为空，无法打开技能树！", this);
            }
        }

        /// <summary>
        /// 继续游戏按钮点击事件
        /// </summary>
        private void OnContinueGameClicked()
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.LoadGame();
            }

            HidePanel();

            if (skillTreePanel != null)
            {
                skillTreePanel.Show();
            }
            else
            {
                Debug.LogWarning("MainMenuPanel: skillTreePanel 为空，无法打开技能树！", this);
            }
        }

        /// <summary>
        /// 退出游戏按钮点击事件
        /// </summary>
        private void OnExitGameClicked()
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.SaveGame();
            }

            // 退出游戏
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }
}

