using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectChicken.Core;
using ProjectChicken.Systems;

namespace ProjectChicken.UI
{
    /// <summary>
    /// 结算面板：显示游戏结束界面和重置功能（使用 CanvasGroup 控制显示/隐藏）
    /// 注意：使用 CanvasGroup 时，面板 GameObject 应始终激活，这样事件订阅才能正常工作
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class SettlementPanel : MonoBehaviour
    {
        [Header("UI组件")]
        [SerializeField] private CanvasGroup canvasGroup; // CanvasGroup 组件（用于控制显示/隐藏）
        [SerializeField] private TMP_Text scoreText; // 显示分数/时间的文本
        [SerializeField] private Button claimButton; // 领取按钮（进入技能树）
        [SerializeField] private Button startNextRoundButton; // 开始下一回合按钮（直接开始游戏）

        [Header("系统引用")]
        [SerializeField] private SkillTreePanel skillTreePanel; // 技能树面板引用（可选，如果为空则使用单例）
        
        // 保存本局结算的鸡蛋数（在结算前读取）
        private int lastSessionEggs = 0;

        private void Awake()
        {
            // 如果没有手动指定，尝试自动获取 CanvasGroup
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (canvasGroup == null)
            {
                Debug.LogError("SettlementPanel: 未找到 CanvasGroup 组件！请确保 GameObject 上有 CanvasGroup 组件。", this);
            }
        }

        private void Start()
        {
            // 如果没有手动指定，尝试使用单例获取 SkillTreePanel
            if (skillTreePanel == null)
            {
                skillTreePanel = SkillTreePanel.Instance;
            }

            // 绑定领取按钮的点击事件
            if (claimButton != null)
            {
                claimButton.onClick.AddListener(OnClaimClicked);
            }
            else
            {
                Debug.LogWarning("SettlementPanel: claimButton 未配置！", this);
            }

            // 绑定开始下一回合按钮的点击事件
            if (startNextRoundButton != null)
            {
                startNextRoundButton.onClick.AddListener(OnStartNextRoundClicked);
            }
            else
            {
                Debug.LogWarning("SettlementPanel: startNextRoundButton 未配置！", this);
            }

            // 初始状态：隐藏面板
            HidePanel();
        }

        private void OnEnable()
        {
            // 订阅游戏状态改变事件
            GameManager.OnGameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            // 取消订阅：防止内存泄漏
            GameManager.OnGameStateChanged -= OnGameStateChanged;
        }

        /// <summary>
        /// 游戏状态改变事件处理
        /// </summary>
        /// <param name="newState">新游戏状态</param>
        private void OnGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.GameOver:
                    // 游戏结束，先保存当前的局内鸡蛋数（在结算之前）
                    if (ResourceManager.Instance != null)
                    {
                        lastSessionEggs = ResourceManager.Instance.CurrentSessionEggs;
                        Debug.Log($"SettlementPanel: 保存本局鸡蛋数 {lastSessionEggs}", this);
                    }
                    else
                    {
                        lastSessionEggs = 0;
                        Debug.LogWarning("SettlementPanel: ResourceManager.Instance 为空，无法读取分数！", this);
                    }
                    
                    // 然后显示结算面板
                    Debug.Log("SettlementPanel: 游戏结束，显示结算面板", this);
                    ShowPanel();
                    break;

                case GameState.Playing:
                case GameState.Preparation:
                    // 游戏开始或准备阶段，隐藏结算面板
                    Debug.Log($"SettlementPanel: 状态为 {newState}，隐藏结算面板", this);
                    HidePanel();
                    break;
            }
        }

        /// <summary>
        /// 显示结算面板
        /// </summary>
        private void ShowPanel()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f; // 完全不透明
                canvasGroup.interactable = true; // 可交互
                canvasGroup.blocksRaycasts = true; // 阻挡射线（可以点击）
                Debug.Log("SettlementPanel: 面板已显示", this);
            }
            else
            {
                Debug.LogWarning("SettlementPanel: canvasGroup 为空，无法显示面板！", this);
            }

            // 显示本局分数（使用保存的值，因为结算后 CurrentSessionEggs 已被重置为 0）
            if (scoreText != null)
            {
                // 使用保存的 lastSessionEggs，而不是从 ResourceManager 读取（因为已经结算并重置了）
                // 只显示数字
                scoreText.text = lastSessionEggs.ToString();
                Debug.Log($"SettlementPanel: 显示分数 {lastSessionEggs}", this);
            }
            else
            {
                Debug.LogWarning("SettlementPanel: scoreText 为空，无法显示分数！", this);
            }
        }

        /// <summary>
        /// 隐藏结算面板
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
        /// 领取按钮点击事件：直接进入技能树界面
        /// 注意：局内鸡蛋数已在回合结束时自动存入总鸡蛋数（GameManager.ChangeGameState）
        /// </summary>
        private void OnClaimClicked()
        {
            Debug.Log("SettlementPanel: 点击领取按钮", this);

            // 隐藏结算面板
            HidePanel();

            // 直接进入技能树界面（不再进入主菜单）
            if (skillTreePanel != null)
            {
                skillTreePanel.Show();
                Debug.Log("SettlementPanel: 已打开技能树面板", this);
            }
            else
            {
                Debug.LogWarning("SettlementPanel: skillTreePanel 为空，无法打开技能树！", this);
            }
        }

        /// <summary>
        /// 开始下一回合按钮点击事件：直接开始新的一局游戏
        /// 注意：局内鸡蛋数已在回合结束时自动存入总鸡蛋数（GameManager.ChangeGameState）
        /// </summary>
        private void OnStartNextRoundClicked()
        {
            Debug.Log("SettlementPanel: 点击开始下一回合按钮", this);

            // 隐藏结算面板
            HidePanel();

            // 直接开始新的一局游戏
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
                Debug.Log("SettlementPanel: 已开始下一回合", this);
            }
            else
            {
                Debug.LogError("SettlementPanel: GameManager.Instance 为空，无法开始游戏！", this);
            }
        }
    }
}

