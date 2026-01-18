using UnityEngine;
using System.Collections.Generic;
using ProjectChicken.Units;
using ProjectChicken.Systems;

namespace ProjectChicken.Systems
{
    /// <summary>
    /// 分形宇宙管理器：协调混合进度系统
    /// 监控摄像机状态并处理"扩展"和"分形缩放"阶段之间的过渡
    /// </summary>
    public class FractalUniverseManager : MonoBehaviour
    {
        public static FractalUniverseManager Instance { get; private set; }

        [Header("阶段配置")]
        [Tooltip("阶段数据列表（按顺序排列）")]
        [SerializeField] private List<HybridStageData> stages = new List<HybridStageData>();

        [Header("系统依赖")]
        [Tooltip("玩家控制器（用于更新约束和获取缩放）")]
        [SerializeField] private PlayerController playerController;

        [Tooltip("鸡生成器（用于清理鸡）")]
        [SerializeField] private ChickenSpawner chickenSpawner;

        [Tooltip("环境根节点（背景对象的父节点）")]
        [SerializeField] private Transform environmentRoot;

        [Header("调试信息")]
        [SerializeField] private bool showDebugLogs = true; // 是否显示调试日志

        // 当前状态
        private int currentStageIndex = 0; // 当前阶段索引
        private GameObject currentWorldInstance = null; // 当前世界实例（环境预制体）

        private void Awake()
        {
            // 单例模式
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("FractalUniverseManager: 检测到多个实例，保留第一个。", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            // 验证依赖项
            if (!ValidateDependencies())
            {
                Debug.LogError("FractalUniverseManager: 依赖项验证失败！管理器可能无法正常工作。", this);
                return;
            }

            // 初始化：加载阶段 0
            if (stages != null && stages.Count > 0)
            {
                if (stages[0] != null)
                {
                    // 加载第一个阶段的环境预制体
                    LoadStageEnvironment(stages[0]);
                    
                    // 应用阶段设置（更新约束）
                    ApplyStageSettings(stages[0]);
                    
                    if (showDebugLogs)
                    {
                        Debug.Log($"FractalUniverseManager: 初始化完成，加载阶段 0: {stages[0].StageName}", this);
                    }
                }
                else
                {
                    Debug.LogError("FractalUniverseManager: 阶段 0 数据为空！", this);
                }
            }
            else
            {
                Debug.LogWarning("FractalUniverseManager: 阶段列表为空！", this);
            }
        }

        private void Update()
        {
            // 检查是否可以升级
            if (stages == null || stages.Count == 0) return;
            if (currentStageIndex < 0 || currentStageIndex >= stages.Count) return;
            if (playerController == null) return;

            // 获取当前阶段数据
            HybridStageData currentStage = stages[currentStageIndex];
            if (currentStage == null) return;

            // 检查摄像机缩放是否超过最大缩放值
            float currentZoom = playerController.GetCurrentZoom();
            if (currentZoom > currentStage.MaxCamSize)
            {
                // 触发升级
                AttemptUpgrade();
            }
        }

        /// <summary>
        /// 验证依赖项是否都已设置
        /// </summary>
        /// <returns>如果所有依赖项都有效，返回 true</returns>
        private bool ValidateDependencies()
        {
            bool isValid = true;

            if (playerController == null)
            {
                Debug.LogWarning("FractalUniverseManager: playerController 未设置，尝试自动查找...", this);
                playerController = FindFirstObjectByType<PlayerController>();
                if (playerController == null)
                {
                    Debug.LogError("FractalUniverseManager: 无法找到 PlayerController！", this);
                    isValid = false;
                }
            }

            if (chickenSpawner == null)
            {
                Debug.LogWarning("FractalUniverseManager: chickenSpawner 未设置，尝试使用单例...", this);
                chickenSpawner = ChickenSpawner.Instance;
                if (chickenSpawner == null)
                {
                    Debug.LogError("FractalUniverseManager: 无法找到 ChickenSpawner！", this);
                    isValid = false;
                }
            }

            if (environmentRoot == null)
            {
                Debug.LogWarning("FractalUniverseManager: environmentRoot 未设置，将使用当前 GameObject 作为根节点。", this);
                environmentRoot = transform;
            }

            return isValid;
        }

        /// <summary>
        /// 尝试升级到下一个阶段
        /// </summary>
        private void AttemptUpgrade()
        {
            // 检查下一个阶段是否存在
            if (currentStageIndex + 1 >= stages.Count)
            {
                if (showDebugLogs)
                {
                    Debug.Log("FractalUniverseManager: 已达到最后一个阶段，无法继续升级。", this);
                }
                return;
            }

            // 获取当前和下一个阶段数据
            HybridStageData currentStage = stages[currentStageIndex];
            HybridStageData nextStage = stages[currentStageIndex + 1];

            if (currentStage == null || nextStage == null)
            {
                Debug.LogError("FractalUniverseManager: 当前或下一个阶段数据为空！", this);
                return;
            }

            if (showDebugLogs)
            {
                Debug.Log($"FractalUniverseManager: 尝试从阶段 {currentStageIndex} ({currentStage.StageName}) 升级到阶段 {currentStageIndex + 1} ({nextStage.StageName})", this);
            }

            // 根据当前阶段的过渡类型执行不同的逻辑
            if (currentStage.TransitionType == StageTransitionType.Expansion)
            {
                // 扩展模式：只需增加索引并应用新设置
                HandleExpansionTransition(nextStage);
            }
            else if (currentStage.TransitionType == StageTransitionType.FractalZoom)
            {
                // 分形缩放模式：需要"弹出"世界并重置
                HandleFractalZoomTransition(currentStage, nextStage);
            }
            else
            {
                Debug.LogWarning($"FractalUniverseManager: 未知的过渡类型: {currentStage.TransitionType}", this);
            }
        }

        /// <summary>
        /// 处理扩展过渡（只需更新约束）
        /// </summary>
        /// <param name="newStage">新阶段数据</param>
        private void HandleExpansionTransition(HybridStageData newStage)
        {
            if (showDebugLogs)
            {
                Debug.Log($"FractalUniverseManager: 执行扩展过渡到 {newStage.StageName}", this);
            }

            // 增加索引
            currentStageIndex++;

            // 应用新阶段设置（这会更新约束，给玩家更多缩放/平移自由度）
            ApplyStageSettings(newStage);

            // 注意：已取消场地扩张时自动结束回合的功能，场地升级现在不会强制结束回合
        }

        /// <summary>
        /// 处理分形缩放过渡（需要重置世界）
        /// </summary>
        /// <param name="currentStage">当前阶段数据</param>
        /// <param name="newStage">新阶段数据</param>
        private void HandleFractalZoomTransition(HybridStageData currentStage, HybridStageData newStage)
        {
            if (showDebugLogs)
            {
                Debug.Log($"FractalUniverseManager: 执行分形缩放过渡，从 {currentStage.StageName} 到 {newStage.StageName}", this);
            }

            // 1. 清理所有鸡
            if (chickenSpawner != null)
            {
                chickenSpawner.ClearAllChickens();
            }

            // 2. 销毁当前世界实例
            if (currentWorldInstance != null)
            {
                Destroy(currentWorldInstance);
                currentWorldInstance = null;
            }

            // 3. 加载新阶段的环境预制体（使用统一的方法）
            LoadStageEnvironment(newStage);

            // 4. 实例化当前阶段的图标作为地标（在 (0,0) 位置）
            if (currentStage.PreviousStageIcon != null)
            {
                GameObject iconObject = new GameObject($"PreviousStageIcon_{currentStage.StageName}");
                iconObject.transform.position = Vector3.zero;

                // 添加 SpriteRenderer 组件
                SpriteRenderer iconRenderer = iconObject.AddComponent<SpriteRenderer>();
                iconRenderer.sprite = currentStage.PreviousStageIcon;
                iconRenderer.sortingOrder = 100; // 确保图标显示在其他对象之上

                // 如果设置了环境根节点，将图标作为子对象
                if (environmentRoot != null)
                {
                    iconObject.transform.SetParent(environmentRoot);
                }

                if (showDebugLogs)
                {
                    Debug.Log($"FractalUniverseManager: 已在 (0,0) 位置创建前一个阶段的图标: {currentStage.StageName}", this);
                }
            }
            else
            {
                if (showDebugLogs)
                {
                    Debug.Log($"FractalUniverseManager: 当前阶段 {currentStage.StageName} 没有 PreviousStageIcon，跳过图标创建。", this);
                }
            }

            // 5. 重置摄像机
            if (playerController != null)
            {
                playerController.ResetCamera(newStage.MinCamSize);
            }

            // 6. 增加索引
            currentStageIndex++;

            // 7. 应用新阶段设置
            ApplyStageSettings(newStage);
        }

        /// <summary>
        /// 加载阶段环境预制体
        /// </summary>
        /// <param name="stageData">阶段数据</param>
        private void LoadStageEnvironment(HybridStageData stageData)
        {
            if (stageData == null) return;

            // 如果已经有环境实例，先销毁它
            if (currentWorldInstance != null)
            {
                Destroy(currentWorldInstance);
                currentWorldInstance = null;
            }

            // 实例化新阶段的环境预制体
            if (stageData.EnvironmentPrefab != null)
            {
                if (environmentRoot != null)
                {
                    currentWorldInstance = Instantiate(stageData.EnvironmentPrefab, environmentRoot);
                    currentWorldInstance.transform.localPosition = Vector3.zero;
                    currentWorldInstance.transform.localRotation = Quaternion.identity;
                    currentWorldInstance.transform.localScale = Vector3.one;
                }
                else
                {
                    currentWorldInstance = Instantiate(stageData.EnvironmentPrefab);
                }

                if (showDebugLogs)
                {
                    Debug.Log($"FractalUniverseManager: 已加载环境预制体: {stageData.EnvironmentPrefab.name}", this);
                }
            }
            else
            {
                if (showDebugLogs)
                {
                    Debug.Log($"FractalUniverseManager: 阶段 {stageData.StageName} 没有环境预制体，跳过加载。", this);
                }
            }

            // 更新 PlayArea 设置
            UpdatePlayAreaForStage(stageData);
        }

        /// <summary>
        /// 为当前阶段更新 PlayArea 设置
        /// </summary>
        /// <param name="stageData">阶段数据</param>
        private void UpdatePlayAreaForStage(HybridStageData stageData)
        {
            if (stageData == null) return;

            // 查找 PlayArea 实例
            PlayArea playArea = PlayArea.Instance;
            
            // 如果环境预制体中包含 PlayArea，优先使用它
            if (currentWorldInstance != null)
            {
                PlayArea prefabPlayArea = currentWorldInstance.GetComponentInChildren<PlayArea>();
                if (prefabPlayArea != null)
                {
                    // 如果预制体中有 PlayArea，使用它
                    playArea = prefabPlayArea;
                    if (showDebugLogs)
                    {
                        Debug.Log($"FractalUniverseManager: 在环境预制体中找到 PlayArea，使用预制体中的设置", this);
                    }
                }
            }

            // 如果找到了 PlayArea，更新它的设置
            if (playArea != null)
            {
                // 确定场地大小：如果阶段数据中指定了大小，使用它；否则保持 PlayArea 的当前设置
                Vector2 targetSize = stageData.PlayAreaSize;
                
                // 如果阶段数据中没有指定大小（为 Vector2.zero），尝试从环境预制体的 PlayArea 获取
                if (targetSize == Vector2.zero && currentWorldInstance != null)
                {
                    PlayArea prefabPlayArea = currentWorldInstance.GetComponentInChildren<PlayArea>();
                    if (prefabPlayArea != null)
                    {
                        // 使用预制体中 PlayArea 的大小
                        targetSize = prefabPlayArea.AreaSize;
                        if (showDebugLogs)
                        {
                            Debug.Log($"FractalUniverseManager: 使用环境预制体中 PlayArea 的大小: {targetSize}", this);
                        }
                    }
                }

                // 如果还是没有大小，使用默认值（基于摄像机约束计算）
                if (targetSize == Vector2.zero)
                {
                    // 根据摄像机约束估算场地大小
                    // 假设场地大小约为摄像机最大尺寸的 2 倍
                    float estimatedSize = stageData.MaxCamSize * 2f;
                    targetSize = new Vector2(estimatedSize, estimatedSize);
                    if (showDebugLogs)
                    {
                        Debug.Log($"FractalUniverseManager: 使用估算的场地大小: {targetSize}", this);
                    }
                }

                // 更新 PlayArea 设置
                playArea.UpdateAreaSettings(
                    targetSize,
                    stageData.PlayAreaCenter,
                    stageData.PlayAreaSprite
                );

                if (showDebugLogs)
                {
                    Debug.Log($"FractalUniverseManager: 已更新 PlayArea 设置 - 大小: {targetSize}, 中心: {stageData.PlayAreaCenter}", this);
                }
            }
            else
            {
                Debug.LogWarning("FractalUniverseManager: 未找到 PlayArea 实例！鸡可能无法正确限制在场地内。", this);
            }
        }

        /// <summary>
        /// 应用阶段设置（更新玩家控制器的约束）
        /// </summary>
        /// <param name="stageData">阶段数据</param>
        private void ApplyStageSettings(HybridStageData stageData)
        {
            if (stageData == null)
            {
                Debug.LogError("FractalUniverseManager: 尝试应用空的阶段数据！", this);
                return;
            }

            if (playerController == null)
            {
                Debug.LogError("FractalUniverseManager: playerController 为空，无法应用阶段设置！", this);
                return;
            }

            // 更新玩家控制器的约束
            playerController.UpdateConstraints(
                stageData.MovementBounds,
                stageData.MinCamSize,
                stageData.MaxCamSize
            );

            if (showDebugLogs)
            {
                Debug.Log($"FractalUniverseManager: 已应用阶段设置 - {stageData.StageName}\n" +
                         $"  平移限制: {stageData.MovementBounds}\n" +
                         $"  缩放范围: {stageData.MinCamSize} - {stageData.MaxCamSize}", this);
            }
        }

        /// <summary>
        /// 获取当前阶段索引（只读）
        /// </summary>
        public int CurrentStageIndex => currentStageIndex;

        /// <summary>
        /// 获取当前阶段数据（只读）
        /// </summary>
        public HybridStageData CurrentStage
        {
            get
            {
                if (stages != null && currentStageIndex >= 0 && currentStageIndex < stages.Count)
                {
                    return stages[currentStageIndex];
                }
                return null;
            }
        }

        /// <summary>
        /// 手动设置阶段（用于调试或特殊场景）
        /// </summary>
        /// <param name="stageIndex">目标阶段索引</param>
        public void SetStage(int stageIndex)
        {
            if (stages == null || stageIndex < 0 || stageIndex >= stages.Count)
            {
                Debug.LogError($"FractalUniverseManager: 无效的阶段索引: {stageIndex}", this);
                return;
            }

            HybridStageData targetStage = stages[stageIndex];
            if (targetStage == null)
            {
                Debug.LogError($"FractalUniverseManager: 阶段 {stageIndex} 数据为空！", this);
                return;
            }

            // 清理当前世界
            if (currentWorldInstance != null)
            {
                Destroy(currentWorldInstance);
                currentWorldInstance = null;
            }

            // 清理所有鸡
            if (chickenSpawner != null)
            {
                chickenSpawner.ClearAllChickens();
            }

            // 设置新阶段
            currentStageIndex = stageIndex;

            // 加载新阶段的环境预制体（这会自动更新 PlayArea）
            LoadStageEnvironment(targetStage);

            // 应用新阶段设置
            ApplyStageSettings(targetStage);

            // 重置摄像机
            if (playerController != null && targetStage != null)
            {
                playerController.ResetCamera(targetStage.MinCamSize);
            }

            if (showDebugLogs)
            {
                Debug.Log($"FractalUniverseManager: 手动设置阶段为 {stageIndex}: {targetStage.StageName}", this);
            }
        }
    }
}

