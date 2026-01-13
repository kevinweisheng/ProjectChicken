using UnityEngine;
using System.Collections.Generic;
using ProjectChicken.Core;
using ProjectChicken.Units;

namespace ProjectChicken.Systems
{
    /// <summary>
    /// 场地升级管理器：管理场地的永久升级
    /// </summary>
    public class AreaUpgradeManager : MonoBehaviour
    {
        public static AreaUpgradeManager Instance { get; private set; }

        [Header("场地配置")]
        [Tooltip("场地数据列表（按升级顺序排列）")]
        [SerializeField] private List<AreaData> areaDataList = new List<AreaData>();

        [Header("系统依赖")]
        [Tooltip("PlayArea 组件（如果为空，会自动查找）")]
        [SerializeField] private PlayArea playArea;

        [Tooltip("ChickenSpawner 组件（用于检测鸡数量）")]
        [SerializeField] private ChickenSpawner chickenSpawner;

        [Tooltip("PlayerController 组件（用于 FractalZoom 类型的升级）")]
        [SerializeField] private PlayerController playerController;

        [Header("调试")]
        [SerializeField] private bool showDebugLogs = true;

        // 当前场地等级（从 0 开始，永久保存）
        private int currentAreaLevel = 0;

        // 当前场地数据
        private AreaData currentAreaData = null;

        private void Awake()
        {
            // 单例模式
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("AreaUpgradeManager: 检测到多个实例，保留第一个。", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            // 验证依赖项
            ValidateDependencies();

            // 从存档加载场地等级（如果 ResourceManager 还未加载，使用默认值）
            // ResourceManager 会在加载后调用 LoadAreaLevel(int) 来应用场地
            LoadAreaLevel();

            // 确保应用当前场地（如果 ResourceManager 还未加载，使用默认等级 0）
            // 如果 ResourceManager 已经加载过，它会调用 LoadAreaLevel(int) 来应用场地
            if (currentAreaData == null)
            {
                // 确保等级在有效范围内
                if (currentAreaLevel < 0) currentAreaLevel = 0;
                if (areaDataList != null && currentAreaLevel >= areaDataList.Count)
                {
                    currentAreaLevel = 0; // 如果等级超出范围，重置为 0
                }
                ApplyCurrentArea();
            }
        }

        private void Update()
        {
            // 检查是否需要升级场地
            CheckAndUpgradeArea();
        }

        /// <summary>
        /// 验证依赖项
        /// </summary>
        private void ValidateDependencies()
        {
            if (playArea == null)
            {
                playArea = PlayArea.Instance;
                if (playArea == null)
                {
                    Debug.LogError("AreaUpgradeManager: 未找到 PlayArea 实例！", this);
                }
            }

            if (chickenSpawner == null)
            {
                chickenSpawner = ChickenSpawner.Instance;
                if (chickenSpawner == null)
                {
                    Debug.LogError("AreaUpgradeManager: 未找到 ChickenSpawner 实例！", this);
                }
            }

            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PlayerController>();
            }
        }

        /// <summary>
        /// 检查并升级场地
        /// </summary>
        private void CheckAndUpgradeArea()
        {
            // 只在游戏进行中检查
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            // 检查是否有下一个场地
            if (currentAreaLevel + 1 >= areaDataList.Count)
            {
                return; // 已达到最高等级
            }

            // 获取当前和下一个场地数据
            AreaData nextAreaData = areaDataList[currentAreaLevel + 1];
            if (nextAreaData == null) return;

            // 检查鸡数量是否超过阈值
            int currentChickenCount = GetCurrentChickenCount();
            if (currentChickenCount >= nextAreaData.ChickenCountThreshold)
            {
                // 检查鸡是否真的溢出了场地边界
                if (AreChickensOverflowingArea())
                {
                    // 触发升级
                    UpgradeToNextArea();
                }
            }
        }

        /// <summary>
        /// 检查鸡是否溢出了场地边界
        /// </summary>
        /// <returns>如果鸡溢出场地，返回 true</returns>
        private bool AreChickensOverflowingArea()
        {
            if (playArea == null) return false;

            // 获取所有鸡
            ChickenUnit[] allChickens = FindObjectsByType<ChickenUnit>(FindObjectsSortMode.None);
            int overflowCount = 0;

            foreach (ChickenUnit chicken in allChickens)
            {
                if (chicken == null || !chicken.IsFat) continue;

                // 检查鸡是否在场地内
                Vector2 chickenPos = chicken.transform.position;
                if (!playArea.IsPositionInArea(chickenPos, 0.5f)) // 0.5f 是边界内边距
                {
                    overflowCount++;
                }
            }

            // 如果有超过 20% 的鸡溢出，认为场地已溢出
            int totalFatChickens = GetCurrentChickenCount();
            if (totalFatChickens == 0) return false;

            float overflowRatio = (float)overflowCount / totalFatChickens;
            bool isOverflowing = overflowRatio > 0.2f; // 20% 阈值

            if (showDebugLogs && isOverflowing)
            {
                Debug.Log($"AreaUpgradeManager: 检测到场地溢出 - 溢出鸡数量: {overflowCount}/{totalFatChickens} ({overflowRatio * 100:F1}%)", this);
            }

            return isOverflowing;
        }

        /// <summary>
        /// 获取当前鸡的数量
        /// </summary>
        /// <returns>当前鸡的数量</returns>
        private int GetCurrentChickenCount()
        {
            if (chickenSpawner == null) return 0;

            // 统计所有肥鸡的数量
            ChickenUnit[] allChickens = FindObjectsByType<ChickenUnit>(FindObjectsSortMode.None);
            int count = 0;
            foreach (ChickenUnit chicken in allChickens)
            {
                if (chicken != null && chicken.IsFat)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 升级到下一个场地
        /// </summary>
        private void UpgradeToNextArea()
        {
            if (currentAreaLevel + 1 >= areaDataList.Count)
            {
                if (showDebugLogs)
                {
                    Debug.Log("AreaUpgradeManager: 已达到最高场地等级，无法继续升级。", this);
                }
                return;
            }

            currentAreaLevel++;
            AreaData newAreaData = areaDataList[currentAreaLevel];

            if (newAreaData == null)
            {
                Debug.LogError($"AreaUpgradeManager: 场地等级 {currentAreaLevel} 的数据为空！", this);
                return;
            }

            if (showDebugLogs)
            {
                Debug.Log($"AreaUpgradeManager: 升级场地到等级 {currentAreaLevel}: {newAreaData.AreaName}", this);
            }

            // 应用新场地
            ApplyAreaData(newAreaData);

            // 保存场地等级
            SaveAreaLevel();
        }

        /// <summary>
        /// 应用当前场地配置
        /// </summary>
        private void ApplyCurrentArea()
        {
            if (areaDataList == null || areaDataList.Count == 0)
            {
                Debug.LogWarning("AreaUpgradeManager: 场地数据列表为空！", this);
                return;
            }

            // 确保等级在有效范围内
            if (currentAreaLevel < 0) currentAreaLevel = 0;
            if (currentAreaLevel >= areaDataList.Count) currentAreaLevel = areaDataList.Count - 1;

            AreaData areaData = areaDataList[currentAreaLevel];
            if (areaData == null)
            {
                Debug.LogError($"AreaUpgradeManager: 场地等级 {currentAreaLevel} 的数据为空！", this);
                return;
            }

            ApplyAreaData(areaData);
        }

        /// <summary>
        /// 应用场地数据
        /// </summary>
        /// <param name="areaData">场地数据</param>
        private void ApplyAreaData(AreaData areaData)
        {
            if (areaData == null)
            {
                Debug.LogError("AreaUpgradeManager: 尝试应用空的场地数据！", this);
                return;
            }

            currentAreaData = areaData;

            // 更新 PlayArea
            if (playArea != null)
            {
                // 安全地获取 Sprite（检查是否为 null）
                Sprite backgroundSprite = areaData.BackgroundSprite;
                
                playArea.UpdateAreaSettings(
                    areaData.AreaSize,
                    areaData.AreaCenter,
                    backgroundSprite // 可能为 null，但 UpdateAreaSettings 会处理
                );
            }
            else
            {
                Debug.LogWarning("AreaUpgradeManager: PlayArea 为空，无法更新场地设置！", this);
            }

            // 如果是 FractalZoom 类型，更新摄像机约束
            if (areaData.UpgradeType == AreaUpgradeType.FractalZoom && playerController != null)
            {
                playerController.UpdateConstraints(
                    areaData.MovementBounds,
                    areaData.MinCamSize,
                    areaData.MaxCamSize
                );
            }

            if (showDebugLogs)
            {
                Debug.Log($"AreaUpgradeManager: 已应用场地 - {areaData.AreaName} (等级 {currentAreaLevel}), Sprite: {(areaData.BackgroundSprite != null ? areaData.BackgroundSprite.name : "未设置")}", this);
            }
        }

        /// <summary>
        /// 保存场地等级到存档
        /// </summary>
        private void SaveAreaLevel()
        {
            // 通过 ResourceManager 保存
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.SaveGame();
                if (showDebugLogs)
                {
                    Debug.Log($"AreaUpgradeManager: 已保存场地等级 {currentAreaLevel}", this);
                }
            }
        }

        /// <summary>
        /// 从存档加载场地等级（由 ResourceManager 调用）
        /// </summary>
        /// <param name="savedLevel">保存的场地等级</param>
        public void LoadAreaLevel(int savedLevel)
        {
            currentAreaLevel = savedLevel;
            
            // 确保等级在有效范围内
            if (currentAreaLevel < 0) currentAreaLevel = 0;
            if (areaDataList != null && currentAreaLevel >= areaDataList.Count)
            {
                currentAreaLevel = 0; // 如果超出范围，使用第一个场地
            }

            if (showDebugLogs)
            {
                Debug.Log($"AreaUpgradeManager: 从存档加载场地等级 {currentAreaLevel}", this);
            }

            // 应用加载的场地配置
            ApplyCurrentArea();
        }

        /// <summary>
        /// 从存档加载场地等级（内部方法，用于 Start）
        /// </summary>
        private void LoadAreaLevel()
        {
            // 初始化：默认使用第一个场地（等级 0）
            // 如果 ResourceManager 已经加载过存档，它会调用 LoadAreaLevel(int) 来覆盖这个值
            currentAreaLevel = 0;
            
            if (showDebugLogs)
            {
                Debug.Log("AreaUpgradeManager: 初始化，默认使用第一个场地（等级 0）", this);
            }
        }

        /// <summary>
        /// 获取当前场地等级（只读）
        /// </summary>
        public int CurrentAreaLevel => currentAreaLevel;

        /// <summary>
        /// 获取当前场地数据（只读）
        /// </summary>
        public AreaData CurrentAreaData => currentAreaData;

        /// <summary>
        /// 重置场地等级（用于清除存档）
        /// </summary>
        public void ResetAreaLevel()
        {
            currentAreaLevel = 0;
            ApplyCurrentArea();
            SaveAreaLevel();

            if (showDebugLogs)
            {
                Debug.Log("AreaUpgradeManager: 已重置场地等级为 0", this);
            }
        }
    }
}

