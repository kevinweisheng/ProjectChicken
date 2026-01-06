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
        [SerializeField] private Button claimButton; // 领取按钮

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
            // 绑定领取按钮的点击事件
            if (claimButton != null)
            {
                claimButton.onClick.AddListener(OnClaimClicked);
            }
            else
            {
                Debug.LogWarning("SettlementPanel: claimButton 未配置！", this);
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
                    // 游戏结束，显示结算面板
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

            // 读取并显示本局分数
            if (scoreText != null)
            {
                int currentScore = 0;
                if (ResourceManager.Instance != null)
                {
                    currentScore = ResourceManager.Instance.CurrentScore;
                }
                else
                {
                    Debug.LogWarning("SettlementPanel: ResourceManager.Instance 为空，无法读取分数！", this);
                }
                
                // 格式化显示："本局产出: {score} 蛋"
                scoreText.text = $"本局产出: {currentScore} 蛋";
                Debug.Log($"SettlementPanel: 显示分数 {currentScore}", this);
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
        /// 领取按钮点击事件：将单局分数存入全局货币，然后进入准备阶段
        /// </summary>
        private void OnClaimClicked()
        {
            Debug.Log("SettlementPanel: 点击领取按钮", this);

            // 第一步：将单局分数存入全局货币仓库（会自动重置局内分数）
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.BankSessionEggs();
            }
            else
            {
                Debug.LogError("SettlementPanel: ResourceManager.Instance 为空，无法结算分数！", this);
                return; // 如果 ResourceManager 为空，直接返回
            }

            // 第二步：进入准备/养成界面
            if (GameManager.Instance != null)
            {
                GameManager.Instance.EnterPreparationPhase();
                Debug.Log("SettlementPanel: 已调用 EnterPreparationPhase()", this);
            }
            else
            {
                Debug.LogError("SettlementPanel: GameManager.Instance 为空，无法进入准备阶段！", this);
            }
        }
    }
}

