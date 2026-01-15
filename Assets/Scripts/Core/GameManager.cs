using UnityEngine;
using System;
using ProjectChicken.Units; // 引用鸡的命名空间
using ProjectChicken.Systems; // 引用蛋和分形系统的命名空间

namespace ProjectChicken.Core
{
    /// <summary>
    /// 游戏状态枚举
    /// </summary>
    public enum GameState
    {
        Preparation,    // 准备阶段（玩家进行升级、查看全局鸡蛋数量）
        Playing,        // 游戏中
        GameOver        // 游戏结束
    }

    /// <summary>
    /// 游戏管理器：负责游戏核心逻辑，包括游戏状态管理、倒计时、鸡蛋生成等
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // 游戏状态事件
        public static event Action<GameState> OnGameStateChanged;
        // 倒计时更新事件
        public static event Action<float> OnTimerUpdated;

        [Header("游戏配置")]
        [SerializeField] private float sessionDuration = 60f; // 初始单局游戏时长（秒，可在 Inspector 中配置，实际值从 UpgradeManager 读取）

        [Header("游戏对象配置")]
        [SerializeField] private EggProjectile eggPrefab; // 拖入蛋的Prefab
        [SerializeField] private RectTransform uiTarget;  // 拖入UI上的目标位置(比如右上角的金币图标)
        [SerializeField] private Camera mainCamera;      // 主摄像机（用于坐标转换）
        
        [Header("分形系统")]
        [Tooltip("分形宇宙管理器（用于阶段进度系统）")]
        [SerializeField] public FractalUniverseManager fractalManager;
        
        [Header("系统引用")]
        [Tooltip("玩家控制器（用于重置摄像机位置）")]
        [SerializeField] private PlayerController playerController;

        // 游戏状态
        private GameState currentState = GameState.Preparation;
        private float remainingTime; // 剩余时间

        /// <summary>
        /// 当前游戏状态（只读属性）
        /// </summary>
        public GameState CurrentState => currentState;

        /// <summary>
        /// 剩余时间（只读属性）
        /// </summary>
        public float RemainingTime => remainingTime;

        private void Awake()
        {
            // 单例模式：防止重复创建
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            
            // 如果没有指定摄像机，尝试获取主摄像机
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
            
            // 如果没有指定分形管理器，尝试自动查找
            if (fractalManager == null)
            {
                fractalManager = FindFirstObjectByType<FractalUniverseManager>();
                if (fractalManager != null)
                {
                    Debug.Log("GameManager: 自动找到 FractalUniverseManager", this);
                }
            }
            
            // 如果没有指定玩家控制器，尝试自动查找
            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PlayerController>();
                if (playerController != null)
                {
                    Debug.Log("GameManager: 自动找到 PlayerController", this);
                }
            }
            
            // 如果需要跨场景保持，取消下面这行的注释
            // DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // 游戏启动时不自动进入准备阶段，让主菜单显示
            // 主菜单会处理新游戏/继续游戏的逻辑
            // EnterPreparationPhase(); // 已注释，由主菜单控制
        }

        private void Update()
        {
            // 只在游戏进行中时更新倒计时
            if (currentState == GameState.Playing)
            {
                // 更新倒计时
                remainingTime -= Time.deltaTime;

                // 触发倒计时更新事件
                OnTimerUpdated?.Invoke(remainingTime);

                // 检查时间是否用完
                if (remainingTime <= 0f)
                {
                    remainingTime = 0f; // 确保不会变成负数
                    ChangeGameState(GameState.GameOver);
                }
            }
        }

        private void OnEnable()
        {
            // 订阅事件：当鸡产出时，执行 SpawnEgg
            ChickenUnit.OnChickenProduct += SpawnEgg;
        }

        private void OnDisable()
        {
            // 取消订阅：养成好习惯，防止内存泄漏
            ChickenUnit.OnChickenProduct -= SpawnEgg;
        }

        /// <summary>
        /// 生成鸡蛋：当鸡产出时调用
        /// </summary>
        /// <param name="spawnPosition">生成位置（鸡的位置）</param>
        private void SpawnEgg(Vector3 spawnPosition)
        {
            // 检查必要组件是否配置
            if (eggPrefab == null)
            {
                Debug.LogWarning("GameManager: eggPrefab 未配置！请在 Inspector 中拖入鸡蛋预制体。", this);
                return;
            }

            if (uiTarget == null)
            {
                Debug.LogWarning("GameManager: uiTarget 未配置！请在 Inspector 中拖入UI目标位置。", this);
                return;
            }

            if (mainCamera == null)
            {
                Debug.LogWarning("GameManager: mainCamera 未配置！", this);
                return;
            }

            // 将UI的世界坐标转换为场景世界坐标
            Vector3 uiWorldPosition = ConvertUIToWorldPosition(uiTarget);

            // 查找生成位置附近的鸡，判断是否为金鸡
            bool isGoldenEgg = false;
            Collider2D[] colliders = Physics2D.OverlapCircleAll(spawnPosition, 0.5f);
            foreach (Collider2D col in colliders)
            {
                ChickenUnit chicken = col.GetComponent<ChickenUnit>();
                if (chicken != null && chicken.IsGolden)
                {
                    isGoldenEgg = true;
                    break;
                }
            }

            // 生成蛋
            EggProjectile newEgg = Instantiate(eggPrefab, spawnPosition, Quaternion.identity);
            // 告诉蛋往哪里飞（使用转换后的世界坐标），并传递是否为金蛋的信息
            newEgg.Initialize(uiWorldPosition, isGoldenEgg);
        }

        /// <summary>
        /// 将UI坐标转换为场景世界坐标
        /// </summary>
        /// <param name="rectTransform">UI元素的RectTransform</param>
        /// <returns>场景世界坐标</returns>
        private Vector3 ConvertUIToWorldPosition(RectTransform rectTransform)
        {
            // 获取Canvas的Camera（用于坐标转换）
            Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
            Camera canvasCamera = canvas != null ? canvas.worldCamera : null;
            
            // 如果Canvas没有指定Camera（Screen Space - Overlay模式），使用null
            // 如果Canvas指定了Camera（Screen Space - Camera模式），使用Canvas的Camera
            Camera uiCamera = canvasCamera != null ? canvasCamera : null;
            
            // 将UI的世界坐标转换为屏幕坐标
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, rectTransform.position);
            
            // 将屏幕坐标转换为场景世界坐标
            // 设置合适的Z深度（2D游戏通常使用固定的Z值，比如0或摄像机的nearClipPlane）
            Vector3 screenPoint3D = new Vector3(screenPoint.x, screenPoint.y, mainCamera.nearClipPlane + 10f);
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPoint3D);
            
            // 保持Z坐标为场景中的Z坐标（2D游戏通常为0）
            worldPosition.z = 0f; // 根据你的2D游戏设置调整这个值
            
            return worldPosition;
        }

        /// <summary>
        /// 改变游戏状态
        /// </summary>
        /// <param name="newState">新状态</param>
        private void ChangeGameState(GameState newState)
        {
            if (currentState == newState)
            {
                Debug.Log($"GameManager: 状态没有变化（当前状态：{currentState}），跳过处理", this);
                return; // 状态没有变化，不需要处理
            }

            GameState oldState = currentState;
            currentState = newState;
            
            // 在状态改变时执行清理逻辑
            if (newState == GameState.GameOver)
            {
                // 先触发状态改变事件，让结算面板能够读取并保存当前的局内鸡蛋数
                // 注意：这里先触发事件，让 SettlementPanel 能够读取 CurrentSessionEggs
                Debug.Log($"GameManager: 准备触发状态改变事件，从 {oldState} 到 {newState}，订阅者数量：{(OnGameStateChanged?.GetInvocationList().Length ?? 0)}", this);
                OnGameStateChanged?.Invoke(currentState);
                
                // 然后结算局内鸡蛋数（存入总鸡蛋数）
                if (ResourceManager.Instance != null)
                {
                    ResourceManager.Instance.BankSessionEggs();
                    Debug.Log("GameManager: 回合结束，已将局内鸡蛋数存入总鸡蛋数", this);
                }
                else
                {
                    Debug.LogWarning("GameManager: ResourceManager.Instance 为空，无法结算局内鸡蛋数！", this);
                }
                
                // 最后清除所有局内状态
                ClearRoundState();
                
                // 注意：状态改变事件已在上面触发，这里不再重复触发
                Debug.Log($"GameManager: 游戏状态改变为 {currentState}", this);
                return; // 提前返回，避免重复触发事件
            }
            
            // 触发状态改变事件
            Debug.Log($"GameManager: 准备触发状态改变事件，从 {oldState} 到 {newState}，订阅者数量：{(OnGameStateChanged?.GetInvocationList().Length ?? 0)}", this);
            OnGameStateChanged?.Invoke(currentState);

            Debug.Log($"GameManager: 游戏状态改变为 {currentState}", this);
        }

        /// <summary>
        /// 清除回合状态：清除所有鸡和其他局内对象
        /// </summary>
        private void ClearRoundState()
        {
            Debug.Log("GameManager: 开始清除回合状态", this);

            // 清除所有鸡
            if (ChickenSpawner.Instance != null)
            {
                ChickenSpawner.Instance.ClearAllChickens();
                Debug.Log("GameManager: 已清除所有鸡", this);
            }
            else
            {
                Debug.LogWarning("GameManager: ChickenSpawner.Instance 为空，无法清除鸡！", this);
            }

            // 清除所有鸡蛋（如果有的话）
            EggProjectile[] allEggs = FindObjectsByType<EggProjectile>(FindObjectsSortMode.None);
            foreach (EggProjectile egg in allEggs)
            {
                if (egg != null && egg.gameObject != null)
                {
                    Destroy(egg.gameObject);
                }
            }
            if (allEggs.Length > 0)
            {
                Debug.Log($"GameManager: 已清除 {allEggs.Length} 个鸡蛋", this);
            }
            
            // 注意：局内分数已在 ChangeGameState 中通过 BankSessionEggs() 处理并重置
            // BankSessionEggs() 会自动将局内鸡蛋数存入总鸡蛋数，并重置 CurrentSessionEggs
        }

        /// <summary>
        /// 进入准备阶段：玩家进行升级、查看全局鸡蛋数量的时候
        /// </summary>
        public void EnterPreparationPhase()
        {
            // 切换到准备阶段
            ChangeGameState(GameState.Preparation);
            
            Debug.Log("GameManager: 进入准备阶段，玩家可以进行升级和查看全局鸡蛋数量", this);
        }

        /// <summary>
        /// 开始新的一局游戏：重置单局分数、重置计时器，切换到 Playing 状态
        /// </summary>
        public void RestartGame()
        {
            Debug.Log("GameManager: 开始新的一局游戏", this);

            // 第一步：清除上一局的所有状态（确保干净的开始）
            ClearRoundState();

            // 第二步：重置单局分数
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.ResetSessionScore();
            }

            // 第三步：重新应用场地设置（确保场地配置正确）
            ApplyAreaSettingsForNewRound();

            // 第四步：从 UpgradeManager 获取当前回合时间（如果可用），否则使用初始值
            float currentSessionDuration = GetCurrentSessionDuration();
            
            // 第五步：重置倒计时
            remainingTime = currentSessionDuration;

            // 第六步：切换到 Playing 状态（这会触发 ChickenSpawner 生成初始鸡）
            ChangeGameState(GameState.Playing);

            // 第七步：触发倒计时更新事件（通知UI更新）
            OnTimerUpdated?.Invoke(remainingTime);

            Debug.Log($"GameManager: 游戏重新开始，时长 {currentSessionDuration} 秒", this);
        }

        /// <summary>
        /// 为新回合应用场地设置
        /// </summary>
        private void ApplyAreaSettingsForNewRound()
        {
            // 重新应用当前场地配置（确保场地设置正确）
            if (AreaUpgradeManager.Instance != null)
            {
                // 重新应用当前场地数据，确保场地配置正确
                var currentAreaData = AreaUpgradeManager.Instance.CurrentAreaData;
                if (currentAreaData != null)
                {
                    AreaUpgradeManager.Instance.ApplyAreaDataManually(currentAreaData);
                    Debug.Log($"GameManager: 已重新应用场地设置 - {currentAreaData.AreaName}", this);
                }
                else
                {
                    Debug.LogWarning("GameManager: 当前场地数据为空，无法应用场地设置！", this);
                }
            }
            else
            {
                Debug.LogWarning("GameManager: AreaUpgradeManager.Instance 为空，无法应用场地设置！", this);
            }
            
            // 重置摄像机位置和约束（确保摄像机回到初始状态）
            if (playerController != null && mainCamera != null)
            {
                // 重置摄像机位置到中心
                mainCamera.transform.position = new Vector3(0f, 0f, mainCamera.transform.position.z);
                
                // 如果场地数据存在，重置摄像机缩放
                if (AreaUpgradeManager.Instance != null && AreaUpgradeManager.Instance.CurrentAreaData != null)
                {
                    var areaData = AreaUpgradeManager.Instance.CurrentAreaData;
                    if (mainCamera.orthographic)
                    {
                        mainCamera.orthographicSize = areaData.MinCamSize;
                        Debug.Log($"GameManager: 已重置摄像机缩放为 {areaData.MinCamSize}", this);
                    }
                }
                
                Debug.Log("GameManager: 已重置摄像机位置", this);
            }
        }

        /// <summary>
        /// 获取当前回合时间（从 UpgradeManager 读取）
        /// </summary>
        /// <returns>当前回合时间（秒）</returns>
        private float GetCurrentSessionDuration()
        {
            if (UpgradeManager.Instance != null)
            {
                return UpgradeManager.Instance.CurrentSessionDuration;
            }

            // 如果 UpgradeManager 未初始化，返回初始值
            return sessionDuration;
        }

        /// <summary>
        /// 延长回合时间（由产蛋时触发）
        /// </summary>
        /// <param name="amount">延长时间（秒）</param>
        public void ExtendSessionTime(float amount)
        {
            if (currentState == GameState.Playing && amount > 0f)
            {
                remainingTime += amount;
                // 触发倒计时更新事件，通知UI更新
                OnTimerUpdated?.Invoke(remainingTime);
                Debug.Log($"GameManager: 回合时间延长 {amount} 秒，当前剩余时间：{remainingTime} 秒", this);
            }
        }

        /// <summary>
        /// 应用分形阶段生产倍率（当分形管理器升级阶段时调用）
        /// 可选功能：如果 ResourceManager 有生产倍率系统，可以在这里应用
        /// </summary>
        /// <param name="multiplier">倍率值（例如：1.5 表示增加 50%）</param>
        public void ApplyFractalProductionMultiplier(float multiplier)
        {
            // 这里可以添加生产倍率逻辑
            // 例如：如果 ResourceManager 有生产倍率字段，可以在这里更新它
            
            // 示例实现（如果需要在 ResourceManager 中添加生产倍率）：
            // if (ResourceManager.Instance != null)
            // {
            //     ResourceManager.Instance.SetProductionMultiplier(multiplier);
            // }
            
            Debug.Log($"GameManager: 应用分形阶段生产倍率 {multiplier}x", this);
        }
    }
}