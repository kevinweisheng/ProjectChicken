using UnityEngine;
using System.Collections.Generic;
using ProjectChicken.Units;
using ProjectChicken.Core;

namespace ProjectChicken.Systems
{
    /// <summary>
    /// 鸡生成器：在回合开始时生成固定数量的鸡
    /// </summary>
    public class ChickenSpawner : MonoBehaviour
    {
        public static ChickenSpawner Instance { get; private set; }

        [Header("生成配置")]
        [SerializeField] private ChickenUnit chickenPrefab; // 鸡的预制体
        [SerializeField] private GameConfig gameConfig; // 游戏配置（从 GameConfig 读取初始鸡数量等）

        [Header("生命值配置")]
        [SerializeField] private ChickenHealthConfig healthConfig; // 普通鸡生命值配置（6个等级的生命值）
        [SerializeField] private GoldenChickenHealthConfig goldenChickenHealthConfig; // 金鸡生命值配置（6个等级的生命值）

        [Header("生成范围")]
        [SerializeField] private float spawnPadding = 1f; // 生成边界内边距（避免在屏幕边缘生成）

        private Camera mainCamera; // 主摄像机
        
        // 跟踪所有生成的鸡（用于清理）
        private List<ChickenUnit> spawnedChickens = new List<ChickenUnit>();
        
        // 排序顺序计数器（为每只新生成的鸡分配唯一的排序偏移）
        private static int sortingOrderCounter = 0;

        private void Awake()
        {
            // 单例模式：防止重复创建
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            // 获取主摄像机
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera == null)
            {
                Debug.LogError("ChickenSpawner: 未找到主摄像机！", this);
            }

            // 根据当前游戏状态初始化
            if (GameManager.Instance != null)
            {
                OnGameStateChanged(GameManager.Instance.CurrentState);
            }
            // 如果 GameManager 还未初始化，会在 OnGameStateChanged 事件触发时处理
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

        // 移除 Update 方法，不再需要持续生成

        /// <summary>
        /// 游戏状态改变事件处理
        /// </summary>
        /// <param name="newState">新游戏状态</param>
        private void OnGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.Playing:
                    // 游戏开始时，生成初始数量的鸡
                    SpawnInitialChickens();
                    Debug.Log("ChickenSpawner: 游戏开始，已生成初始鸡", this);
                    break;

                case GameState.Preparation:
                case GameState.GameOver:
                    // 准备阶段或游戏结束
                    // 注意：鸡的清除由 GameManager.ClearRoundState() 处理
                    Debug.Log($"ChickenSpawner: 状态为 {newState}", this);
                    break;
            }
        }

        /// <summary>
        /// 在回合开始时生成初始数量的鸡
        /// </summary>
        private void SpawnInitialChickens()
        {
            if (chickenPrefab == null || mainCamera == null)
            {
                Debug.LogWarning("ChickenSpawner: chickenPrefab 或 mainCamera 为空，无法生成初始鸡！", this);
                return;
            }

            // 计算初始鸡数量：从 GameConfig 读取基础值 + 技能加成
            int baseInitialChickenCount = gameConfig != null ? gameConfig.InitialChickenCount : 5;
            int extraInitialChickens = UpgradeManager.Instance != null ? UpgradeManager.Instance.ExtraInitialChickens : 0;
            int initialCount = baseInitialChickenCount + extraInitialChickens;

            Debug.Log($"ChickenSpawner: 开始生成初始鸡 - 基础值: {baseInitialChickenCount}, 技能加成: {extraInitialChickens}, 总计: {initialCount}", this);

            // 生成指定数量的鸡，并收集生成的鸡列表
            List<ChickenUnit> generatedChickens = new List<ChickenUnit>();
            int initialSpawnedCount = spawnedChickens.Count;
            
            for (int i = 0; i < initialCount; i++)
            {
                SpawnChicken();
            }
            
            // 收集刚刚生成的鸡（从上次的计数位置到现在的列表末尾）
            for (int i = initialSpawnedCount; i < spawnedChickens.Count; i++)
            {
                if (spawnedChickens[i] != null)
                {
                    generatedChickens.Add(spawnedChickens[i]);
                }
            }

            // 确保至少有一只金鸡（如果解锁了金鸡技能）
            EnsureAtLeastOneGoldenChicken(generatedChickens);

            Debug.Log($"ChickenSpawner: 已生成 {initialCount} 只初始鸡", this);
        }

        /// <summary>
        /// 确保初始生成的鸡中至少有一只金鸡（如果解锁了金鸡技能）
        /// </summary>
        /// <param name="chickens">生成的鸡列表</param>
        private void EnsureAtLeastOneGoldenChicken(List<ChickenUnit> chickens)
        {
            // 检查是否解锁了金鸡技能
            if (UpgradeManager.Instance == null || !UpgradeManager.Instance.IsGoldenChickenUnlocked)
            {
                // 如果未解锁金鸡技能，不需要确保金鸡
                return;
            }

            if (chickens == null || chickens.Count == 0)
            {
                return;
            }

            // 检查是否已经有金鸡
            bool hasGoldenChicken = false;
            foreach (ChickenUnit chicken in chickens)
            {
                if (chicken != null && chicken.IsGolden)
                {
                    hasGoldenChicken = true;
                    break;
                }
            }

            // 如果没有金鸡，随机选择一只普通鸡变成金鸡
            if (!hasGoldenChicken)
            {
                // 收集所有普通鸡
                List<ChickenUnit> normalChickens = new List<ChickenUnit>();
                foreach (ChickenUnit chicken in chickens)
                {
                    if (chicken != null && !chicken.IsGolden)
                    {
                        normalChickens.Add(chicken);
                    }
                }

                // 随机选择一只普通鸡变成金鸡
                if (normalChickens.Count > 0)
                {
                    int randomIndex = Random.Range(0, normalChickens.Count);
                    ChickenUnit selectedChicken = normalChickens[randomIndex];
                    
                    // 将该鸡设置为金鸡
                    selectedChicken.SetGolden(true);
                    
                    // 重新设置生命值（使用金鸡的生命值配置）
                    SetChickenHealthByStage(selectedChicken, true);
                    
                    Debug.Log($"ChickenSpawner: 为了确保至少有一只金鸡，将一只普通鸡转换为金鸡", this);
                }
                else
                {
                    Debug.LogWarning("ChickenSpawner: 无法找到普通鸡转换为金鸡！", this);
                }
            }
        }

        /// <summary>
        /// 检查是否可以生成更多鸡（用于分裂功能）
        /// </summary>
        /// <returns>是否可以生成更多鸡</returns>
        public bool CanSpawnMore()
        {
            int currentFatChickens = CountFatChickens();
            int baseMaxChickens = gameConfig != null ? gameConfig.BaseMaxChickens : 10;
            int maxChickens = baseMaxChickens;
            if (UpgradeManager.Instance != null)
            {
                maxChickens += UpgradeManager.Instance.ExtraMaxChickens;
            }
            return currentFatChickens < maxChickens;
        }

        /// <summary>
        /// 在指定位置生成一只新鸡（用于分裂功能）
        /// </summary>
        /// <param name="position">生成位置</param>
        public void SpawnChickenAt(Vector3 position)
        {
            if (chickenPrefab == null)
            {
                Debug.LogWarning("ChickenSpawner: chickenPrefab 为空，无法生成鸡！", this);
                return;
            }

            // 添加小随机偏移，避免重叠
            Vector2 randomOffset = Random.insideUnitCircle * 0.5f;
            Vector3 spawnPosition = position + new Vector3(randomOffset.x, randomOffset.y, 0f);

            // 实例化鸡
            ChickenUnit newChicken = Instantiate(chickenPrefab, spawnPosition, Quaternion.identity);
            
            // 添加到生成列表中（用于后续清理）
            if (newChicken != null)
            {
                // 分裂生成的鸡继承原鸡的类型（普通鸡）
                bool isGolden = false;
                newChicken.SetGolden(isGolden);
                
                // 根据当前阶段的生命值等级设置鸡的生命值
                SetChickenHealthByStage(newChicken, isGolden);
                
                // 为每只新生成的鸡分配唯一的排序偏移（每只鸡 +1）
                newChicken.SetUniqueSortingOffset(sortingOrderCounter++);
                
                spawnedChickens.Add(newChicken);
                Debug.Log($"ChickenSpawner: 在位置 {spawnPosition} 通过分裂生成了一只鸡", this);
            }
        }

        /// <summary>
        /// 统计当前场景中 "Fat" 状态的鸡的数量
        /// </summary>
        /// <returns>肥鸡的数量</returns>
        private int CountFatChickens()
        {
            int count = 0;
            // 查找场景中所有的 ChickenUnit
            ChickenUnit[] allChickens = FindObjectsByType<ChickenUnit>(FindObjectsSortMode.None);

            foreach (ChickenUnit chicken in allChickens)
            {
                // 检查是否为肥鸡状态
                if (chicken != null && chicken.IsFat)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 生成一只新鸡
        /// </summary>
        private void SpawnChicken()
        {
            // 获取随机生成位置
            Vector3 spawnPosition = GetRandomSpawnPosition();

            // 实例化鸡
            ChickenUnit newChicken = Instantiate(chickenPrefab, spawnPosition, Quaternion.identity);
            
            // 添加到生成列表中（用于后续清理）
            if (newChicken != null)
            {
                // 根据金鸡生成率决定是否生成金鸡（需要在设置生命值之前确定类型）
                bool isGolden = ShouldSpawnGoldenChicken();
                newChicken.SetGolden(isGolden);
                
                // 根据当前阶段的生命值等级和鸡的类型设置鸡的生命值
                SetChickenHealthByStage(newChicken, isGolden);
                
                // 为每只新生成的鸡分配唯一的排序偏移（每只鸡 +1）
                newChicken.SetUniqueSortingOffset(sortingOrderCounter++);
                
                spawnedChickens.Add(newChicken);
                
                // 调试信息
                string chickenType = isGolden ? "金鸡" : "普通鸡";
                Debug.Log($"ChickenSpawner: 在位置 {spawnPosition} 生成了一只{chickenType}", this);
            }
        }

        /// <summary>
        /// 根据当前阶段的生命值等级和鸡的类型设置鸡的生命值
        /// </summary>
        /// <param name="chicken">鸡单位</param>
        /// <param name="isGolden">是否为金鸡</param>
        private void SetChickenHealthByStage(ChickenUnit chicken, bool isGolden)
        {
            if (chicken == null)
            {
                return; // 如果鸡为空，使用默认生命值
            }

            // 获取当前阶段的生命值等级
            int healthLevel = 0; // 默认等级0
            
            if (FractalUniverseManager.Instance != null && FractalUniverseManager.Instance.CurrentStage != null)
            {
                healthLevel = FractalUniverseManager.Instance.CurrentStage.ChickenHealthLevel;
            }

            float health = 0f;
            string chickenType = isGolden ? "金鸡" : "普通鸡";

            // 根据鸡的类型选择不同的配置
            if (isGolden)
            {
                // 使用金鸡生命值配置
                if (goldenChickenHealthConfig != null)
                {
                    health = goldenChickenHealthConfig.GetHealthByLevel(healthLevel);
                }
                else
                {
                    // 如果没有配置金鸡生命值，使用普通鸡配置的1.5倍（默认值）
                    if (healthConfig != null)
                    {
                        health = healthConfig.GetHealthByLevel(healthLevel) * 1.5f;
                    }
                    else
                    {
                        health = 150f; // 最终默认值
                    }
                    Debug.LogWarning($"ChickenSpawner: 金鸡生命值配置未设置，使用默认值 {health}（普通鸡配置的1.5倍）", this);
                }
            }
            else
            {
                // 使用普通鸡生命值配置
                if (healthConfig != null)
                {
                    health = healthConfig.GetHealthByLevel(healthLevel);
                }
                else
                {
                    health = 100f; // 默认值
                    Debug.LogWarning($"ChickenSpawner: 普通鸡生命值配置未设置，使用默认值 {health}", this);
                }
            }

            // 设置生命值
            chicken.SetMaxHP(health);
            
            Debug.Log($"ChickenSpawner: 设置{chickenType}的生命值为 {health}（等级 {healthLevel}）", this);
        }

        /// <summary>
        /// 判断是否应该生成金鸡
        /// </summary>
        /// <returns>是否生成金鸡</returns>
        private bool ShouldSpawnGoldenChicken()
        {
            if (UpgradeManager.Instance == null)
            {
                return false;
            }

            // 只有解锁了金鸡生成能力才能生成金鸡
            if (!UpgradeManager.Instance.IsGoldenChickenUnlocked)
            {
                return false;
            }

            // 根据生成率随机决定
            float spawnRate = UpgradeManager.Instance.GoldenChickenSpawnRate;
            return UnityEngine.Random.value < spawnRate;
        }

        /// <summary>
        /// 清理所有生成的鸡（用于游戏重置）
        /// </summary>
        public void ClearAllChickens()
        {
            int destroyedCount = 0;

            // 反向遍历列表，安全删除（反向遍历避免索引问题）
            for (int i = spawnedChickens.Count - 1; i >= 0; i--)
            {
                if (spawnedChickens[i] != null)
                {
                    Destroy(spawnedChickens[i].gameObject);
                    destroyedCount++;
                }
            }

            // 清空列表
            spawnedChickens.Clear();

            // 清理场景中所有剩余的鸡（包括瘦鸡预制体替换后的鸡）
            // 这样可以确保即使瘦鸡没有被添加到列表中，也能被清理
            ChickenUnit[] allChickens = FindObjectsByType<ChickenUnit>(FindObjectsSortMode.None);
            foreach (ChickenUnit chicken in allChickens)
            {
                if (chicken != null && chicken.gameObject != null)
                {
                    Destroy(chicken.gameObject);
                    destroyedCount++;
                }
            }
            
            // 重置排序顺序计数器（每回合重新计数）
            sortingOrderCounter = 0;

            Debug.Log($"ChickenSpawner: 已清理 {destroyedCount} 只鸡（包括瘦鸡）", this);
        }

        /// <summary>
        /// 获取随机生成位置（在场地内或屏幕可见范围内）
        /// </summary>
        /// <returns>世界坐标位置</returns>
        private Vector3 GetRandomSpawnPosition()
        {
            // 优先使用场地边界，如果没有场地则使用屏幕边界（向后兼容）
            if (PlayArea.Instance != null)
            {
                // 使用鸡活动范围生成（而不是场地边界）
                Vector2 randomPos = PlayArea.Instance.GetRandomPositionInArea(spawnPadding, useChickenArea: true);
                return new Vector3(randomPos.x, randomPos.y, 0f);
            }
            else if (mainCamera != null)
            {
                // 回退到屏幕边界（向后兼容）
                // 使用 ViewportToWorldPoint 计算屏幕边界（更可靠的方法）
                // Viewport 坐标：左下角 (0,0)，右上角 (1,1)
                
                // 计算屏幕四个角的世界坐标
                Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0f, 0f, mainCamera.nearClipPlane + 10f));
                Vector3 topRight = mainCamera.ViewportToWorldPoint(new Vector3(1f, 1f, mainCamera.nearClipPlane + 10f));
                
                // 计算生成范围（减去内边距）
                float minX = bottomLeft.x + spawnPadding;
                float maxX = topRight.x - spawnPadding;
                float minY = bottomLeft.y + spawnPadding;
                float maxY = topRight.y - spawnPadding;
                
                // 确保范围有效
                if (maxX <= minX) maxX = minX + 1f;
                if (maxY <= minY) maxY = minY + 1f;
                
                // 在屏幕范围内随机生成位置
                float randomX = Random.Range(minX, maxX);
                float randomY = Random.Range(minY, maxY);
                
                // 使用固定的Z坐标（2D游戏通常为0）
                Vector3 spawnPosition = new Vector3(randomX, randomY, 0f);

                return spawnPosition;
            }
            else
            {
                Debug.LogWarning("ChickenSpawner: 未找到 PlayArea 或 mainCamera，无法生成鸡！", this);
                return Vector3.zero;
            }
        }

        /// <summary>
        /// 在 Scene 视图中绘制生成范围（调试用）
        /// </summary>
        private void OnDrawGizmos()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera == null) return;

            // 计算屏幕边界
            Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0f, 0f, mainCamera.nearClipPlane + 10f));
            Vector3 topRight = mainCamera.ViewportToWorldPoint(new Vector3(1f, 1f, mainCamera.nearClipPlane + 10f));
            
            // 绘制生成范围（绿色边框）
            Gizmos.color = Color.green;
            Vector3 bottomRight = new Vector3(topRight.x, bottomLeft.y, 0f);
            Vector3 topLeft = new Vector3(bottomLeft.x, topRight.y, 0f);
            
            // 绘制矩形边框
            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
            
            // 绘制减去内边距后的生成范围（黄色边框）
            Gizmos.color = Color.yellow;
            float minX = bottomLeft.x + spawnPadding;
            float maxX = topRight.x - spawnPadding;
            float minY = bottomLeft.y + spawnPadding;
            float maxY = topRight.y - spawnPadding;
            
            Vector3 innerBottomLeft = new Vector3(minX, minY, 0f);
            Vector3 innerTopRight = new Vector3(maxX, maxY, 0f);
            Vector3 innerBottomRight = new Vector3(maxX, minY, 0f);
            Vector3 innerTopLeft = new Vector3(minX, maxY, 0f);
            
            Gizmos.DrawLine(innerBottomLeft, innerBottomRight);
            Gizmos.DrawLine(innerBottomRight, innerTopRight);
            Gizmos.DrawLine(innerTopRight, innerTopLeft);
            Gizmos.DrawLine(innerTopLeft, innerBottomLeft);
        }
    }
}

