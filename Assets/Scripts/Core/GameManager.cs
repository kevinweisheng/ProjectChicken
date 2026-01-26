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
        
        [Header("下蛋数量配置")]
        [Tooltip("普通鸡下蛋数量配置（根据场地等级决定下蛋数量）")]
        [SerializeField] private EggCountConfig eggCountConfig;
        
        [Header("多蛋生成配置")]
        [Tooltip("多个蛋之间的目标位置偏移（世界单位），用于避免重叠")]
        [SerializeField] private float eggTargetOffsetRadius = 0.5f;
        
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
            }
            
            // 如果没有指定玩家控制器，尝试自动查找
            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PlayerController>();
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
        /// 如果解锁了哺乳动物技能，有概率不生鸡蛋而产生一只鸡
        /// </summary>
        /// <param name="spawnPosition">生成位置（鸡的位置）</param>
        /// <param name="isGoldenChicken">下蛋的鸡是否为金鸡</param>
        private void SpawnEgg(Vector3 spawnPosition, bool isGoldenChicken)
        {
            // 检查是否解锁哺乳动物技能，如果解锁则根据概率决定是生蛋还是生鸡
            if (UpgradeManager.Instance != null && UpgradeManager.Instance.IsMammalUnlocked)
            {
                float mammalChance = UpgradeManager.Instance.MammalChance;
                if (mammalChance > 0f && UnityEngine.Random.value < mammalChance)
                {
                    SpawnMammalChicken(spawnPosition);
                    return;
                }
            }

            // 正常生蛋流程
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

            // 使用事件传递的鸡类型信息，而不是通过碰撞检测
            // 这样可以避免多只鸡同时下蛋时，普通鸡蛋被错误地标记为金蛋
            bool isGoldenEgg = isGoldenChicken;

            // 确定需要生成多少个蛋
            int eggCount = 1; // 默认生成1个蛋
            
            if (!isGoldenEgg)
            {
                // 普通蛋：根据当前场地等级决定下蛋数量
                eggCount = GetNormalEggCountByStage();
            }
            // 金蛋始终生成1个（价值通过倍率体现）

            // 生成多个蛋，每个蛋有不同的目标位置偏移
            for (int i = 0; i < eggCount; i++)
            {
                // 计算每个蛋的目标位置（添加随机偏移，避免重叠）
                Vector3 targetPosition = uiWorldPosition;
                if (eggCount > 1)
                {
                    // 在目标位置周围随机偏移
                    float angle = (360f / eggCount) * i + UnityEngine.Random.Range(-15f, 15f); // 均匀分布 + 随机抖动
                    float distance = eggTargetOffsetRadius;
                    Vector2 offset = new Vector2(
                        Mathf.Cos(angle * Mathf.Deg2Rad) * distance,
                        Mathf.Sin(angle * Mathf.Deg2Rad) * distance
                    );
                    targetPosition = uiWorldPosition + new Vector3(offset.x, offset.y, 0f);
            }

            // 生成蛋
            EggProjectile newEgg = Instantiate(eggPrefab, spawnPosition, Quaternion.identity);
                // 告诉蛋往哪里飞（使用偏移后的目标位置），并传递是否为金蛋的信息
                // 注意：每个蛋的价值都是1，这样多个蛋加起来才是总数
                newEgg.Initialize(targetPosition, isGoldenEgg);
            }
        }

        /// <summary>
        /// 触发哺乳动物效果：在指定位置生成一只新鸡
        /// </summary>
        /// <param name="spawnPosition">生成位置</param>
        private void SpawnMammalChicken(Vector3 spawnPosition)
        {
            if (ChickenSpawner.Instance == null)
            {
                Debug.LogWarning("GameManager: ChickenSpawner.Instance 为空，无法生成新鸡！", this);
                return;
            }

            // 使用 ChickenSpawner 的方法生成新鸡
            ChickenSpawner.Instance.SpawnChickenAt(spawnPosition);
        }

        /// <summary>
        /// 根据当前场地等级获取普通鸡下蛋数量（与 EggProjectile 中的逻辑保持一致）
        /// </summary>
        /// <returns>普通鸡下蛋数量</returns>
        private int GetNormalEggCountByStage()
        {
            // 如果没有配置，返回默认值1
            if (eggCountConfig == null)
            {
                return 1;
            }

            // 获取当前场地等级（优先从 AreaUpgradeManager 获取，如果没有则从 FractalUniverseManager 获取）
            int stageLevel = 0; // 默认等级0
            
            if (AreaUpgradeManager.Instance != null && AreaUpgradeManager.Instance.CurrentAreaData != null)
            {
                // 使用当前场地的鸡生命值等级作为下蛋数量等级（因为它们是对应的）
                stageLevel = AreaUpgradeManager.Instance.CurrentAreaData.ChickenHealthLevel;
            }
            else if (fractalManager != null && fractalManager.CurrentStage != null)
            {
                // 备用方案：如果没有 AreaUpgradeManager，使用 FractalUniverseManager
                stageLevel = fractalManager.CurrentStage.ChickenHealthLevel;
            }

            // 从配置中获取该等级的下蛋数量
            return eggCountConfig.GetEggCountByLevel(stageLevel);
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
                return;
            }

            GameState oldState = currentState;
            currentState = newState;
            
            // 在状态改变时执行清理逻辑
            if (newState == GameState.GameOver)
            {
                OnGameStateChanged?.Invoke(currentState);
                
                if (ResourceManager.Instance != null)
                {
                    ResourceManager.Instance.BankSessionEggs();
                }
                else
                {
                    Debug.LogWarning("GameManager: ResourceManager.Instance 为空，无法结算局内鸡蛋数！", this);
                }
                
                ClearRoundState();
                return;
            }
            
            OnGameStateChanged?.Invoke(currentState);
        }

        /// <summary>
        /// 清除回合状态：清除所有鸡和其他局内对象
        /// </summary>
        private void ClearRoundState()
        {
            if (ChickenSpawner.Instance != null)
            {
                ChickenSpawner.Instance.ClearAllChickens();
            }
            else
            {
                Debug.LogWarning("GameManager: ChickenSpawner.Instance 为空，无法清除鸡！", this);
            }

            EggProjectile[] allEggs = FindObjectsByType<EggProjectile>(FindObjectsSortMode.None);
            foreach (EggProjectile egg in allEggs)
            {
                if (egg != null && egg.gameObject != null)
                {
                    Destroy(egg.gameObject);
                }
            }
        }

        /// <summary>
        /// 结束游戏：触发游戏结束状态并显示结算画面
        /// </summary>
        public void EndGame()
        {
            if (currentState != GameState.Playing)
            {
                Debug.LogWarning($"GameManager: 当前状态为 {currentState}，不是 Playing 状态，无法结束游戏", this);
                return;
            }

            ChangeGameState(GameState.GameOver);
        }

        /// <summary>
        /// 进入准备阶段：玩家进行升级、查看全局鸡蛋数量的时候
        /// </summary>
        public void EnterPreparationPhase()
        {
            ChangeGameState(GameState.Preparation);
        }

        /// <summary>
        /// 开始新的一局游戏：重置单局分数、重置计时器，切换到 Playing 状态
        /// </summary>
        public void RestartGame()
        {
            ClearRoundState();

            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.ResetSessionScore();
            }

            ApplyAreaSettingsForNewRound();

            float currentSessionDuration = GetCurrentSessionDuration();
            remainingTime = currentSessionDuration;

            ChangeGameState(GameState.Playing);

            OnTimerUpdated?.Invoke(remainingTime);
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
                    }
                }
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
                OnTimerUpdated?.Invoke(remainingTime);
            }
        }

        /// <summary>
        /// 应用分形阶段生产倍率（当分形管理器升级阶段时调用）
        /// 可选功能：如果 ResourceManager 有生产倍率系统，可以在这里应用
        /// </summary>
        /// <param name="multiplier">倍率值（例如：1.5 表示增加 50%）</param>
        public void ApplyFractalProductionMultiplier(float multiplier)
        {
        }
    }
}