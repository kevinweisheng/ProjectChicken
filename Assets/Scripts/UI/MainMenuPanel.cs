using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectChicken.Core;
using ProjectChicken.Systems;

namespace ProjectChicken.UI
{
    /// <summary>
    /// 主菜单面板：管理养成阶段的 UI（准备阶段，使用 CanvasGroup 控制显示/隐藏）
    /// 注意：使用 CanvasGroup 时，面板 GameObject 应始终激活，这样事件订阅才能正常工作
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class MainMenuPanel : MonoBehaviour
    {
        [Header("UI组件")]
        [SerializeField] private CanvasGroup canvasGroup; // CanvasGroup 组件（用于控制显示/隐藏）
        [SerializeField] private TMP_Text totalEggsText; // 显示全局货币数量的文本
        [SerializeField] private Button startBattleButton; // 开始战斗按钮
        [SerializeField] private Button openSkillTreeButton; // 打开技能树按钮

        [Header("系统引用")]
        [SerializeField] private ChickenSpawner spawner; // 鸡生成器（用于清理）
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
            if (spawner == null)
            {
                spawner = FindFirstObjectByType<ChickenSpawner>();
            }

            // 如果没有手动指定，尝试使用单例获取 SkillTreePanel
            if (skillTreePanel == null)
            {
                skillTreePanel = SkillTreePanel.Instance;
            }

            // 绑定开始战斗按钮的点击事件
            if (startBattleButton != null)
            {
                startBattleButton.onClick.AddListener(OnStartBattleClicked);
            }
            else
            {
                Debug.LogWarning("MainMenuPanel: startBattleButton 未配置！", this);
            }

            // 绑定打开技能树按钮的点击事件
            if (openSkillTreeButton != null)
            {
                openSkillTreeButton.onClick.AddListener(OnOpenSkillTreeClicked);
            }
            else
            {
                Debug.LogWarning("MainMenuPanel: openSkillTreeButton 未配置！", this);
            }

            // 初始状态：根据当前游戏状态决定是否显示
            UpdatePanelVisibility();
        }

        private void OnEnable()
        {
            // 订阅游戏状态改变事件
            GameManager.OnGameStateChanged += OnGameStateChanged;
            Debug.Log("MainMenuPanel: 已订阅游戏状态改变事件", this);
        }

        private void OnDisable()
        {
            // 取消订阅：防止内存泄漏
            GameManager.OnGameStateChanged -= OnGameStateChanged;
            Debug.Log("MainMenuPanel: 已取消订阅游戏状态改变事件", this);
        }

        /// <summary>
        /// 游戏状态改变事件处理
        /// </summary>
        /// <param name="newState">新游戏状态</param>
        private void OnGameStateChanged(GameState newState)
        {
            Debug.Log($"MainMenuPanel: 收到游戏状态改变事件，新状态：{newState}", this);
            
            switch (newState)
            {
                case GameState.Preparation:
                    // 进入准备阶段，显示面板并清理上一局的残余怪物
                    Debug.Log("MainMenuPanel: 进入准备阶段，显示面板", this);
                    ClearRemainingChickens(); // 清理上一局的残余怪物
                    ShowPanel(); // 显示面板（会更新全局货币显示）
                    break;

                case GameState.Playing:
                case GameState.GameOver:
                    // 游戏进行中或游戏结束，隐藏面板
                    Debug.Log($"MainMenuPanel: 状态为 {newState}，隐藏面板", this);
                    HidePanel();
                    break;
            }
        }

        /// <summary>
        /// 显示主菜单面板
        /// </summary>
        private void ShowPanel()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f; // 完全不透明
                canvasGroup.interactable = true; // 可交互
                canvasGroup.blocksRaycasts = true; // 阻挡射线（可以点击）
                UpdateTotalEggsDisplay(); // 更新全局货币显示
                Debug.Log("MainMenuPanel: 面板已显示", this);
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
        /// 更新面板显示状态（根据当前游戏状态）
        /// </summary>
        private void UpdatePanelVisibility()
        {
            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.CurrentState == GameState.Preparation)
                {
                    ShowPanel();
                }
                else
                {
                    HidePanel();
                }
            }
        }

        /// <summary>
        /// 更新全局货币显示
        /// </summary>
        private void UpdateTotalEggsDisplay()
        {
            if (totalEggsText != null)
            {
                int totalEggs = 0;
                if (ResourceManager.Instance != null)
                {
                    totalEggs = ResourceManager.Instance.TotalGlobalEggs;
                }
                else
                {
                    Debug.LogWarning("MainMenuPanel: ResourceManager.Instance 为空，无法读取全局货币！", this);
                }

                totalEggsText.text = totalEggs.ToString();
            }
            else
            {
                Debug.LogWarning("MainMenuPanel: totalEggsText 未配置！", this);
            }
        }

        /// <summary>
        /// 清理上一局的残余怪物
        /// </summary>
        private void ClearRemainingChickens()
        {
            if (spawner != null)
            {
                spawner.ClearAllChickens();
                Debug.Log("MainMenuPanel: 已清理上一局的残余怪物", this);
            }
            else
            {
                Debug.LogWarning("MainMenuPanel: spawner 为空，无法清理怪物！", this);
            }
        }

        /// <summary>
        /// 开始战斗按钮点击事件
        /// </summary>
        private void OnStartBattleClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
                Debug.Log("MainMenuPanel: 开始新一局战斗", this);
            }
            else
            {
                Debug.LogError("MainMenuPanel: GameManager.Instance 为空，无法开始战斗！", this);
            }
        }

        /// <summary>
        /// 打开技能树按钮点击事件
        /// </summary>
        private void OnOpenSkillTreeClicked()
        {
            // 隐藏主菜单面板
            HidePanel();

            // 显示技能树面板
            if (skillTreePanel != null)
            {
                skillTreePanel.Show();
                Debug.Log("MainMenuPanel: 已打开技能树面板", this);
            }
            else
            {
                Debug.LogWarning("MainMenuPanel: skillTreePanel 为空，无法打开技能树！", this);
            }
        }

        /// <summary>
        /// 手动刷新全局货币显示（供外部调用）
        /// </summary>
        public void RefreshTotalEggsDisplay()
        {
            UpdateTotalEggsDisplay();
        }
    }
}

