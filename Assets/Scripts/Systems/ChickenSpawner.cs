using UnityEngine;
using System.Collections.Generic;
using ProjectChicken.Units;
using ProjectChicken.Core;

namespace ProjectChicken.Systems
{
    /// <summary>
    /// 特殊鸡类型枚举
    /// </summary>
    public enum SpecialChickenType
    {
        None,       // 普通鸡
        Golden,     // 金鸡
        Lightning,   // 闪电鸡
        Bomb        // 炸弹鸡
    }

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

        [Header("特殊鸡配置")]
        [Tooltip("特殊鸡配置资源（统一管理所有特殊鸡的属性）")]
        [SerializeField] private SpecialChickenConfig specialChickenConfig;

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
                    SpawnInitialChickens();
                    break;

                case GameState.Preparation:
                case GameState.GameOver:
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

            EnsureAtLeastOneGoldenChicken(generatedChickens);
            
            // 统计并输出鸡的种类信息
            LogChickenStatistics();
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
                    
                    SetChickenHealthByStage(selectedChicken, true);
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
                
                newChicken.SetUniqueSortingOffset(sortingOrderCounter++);
                
                spawnedChickens.Add(newChicken);
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
                // 根据权重随机选择特殊鸡类型（一只鸡只能是一种特殊类型）
                SpecialChickenType selectedType = SelectSpecialChickenTypeByWeight();
                bool isGolden = selectedType == SpecialChickenType.Golden;
                bool isLightning = selectedType == SpecialChickenType.Lightning;
                bool isBomb = selectedType == SpecialChickenType.Bomb;

                newChicken.SetGolden(isGolden);
                newChicken.SetLightningChicken(isLightning);
                newChicken.SetBombChicken(isBomb);
                
                // 如果配置存在，应用特殊鸡的属性配置
                if (specialChickenConfig != null)
                {
                    if (isLightning)
                    {
                        ApplyLightningChickenConfig(newChicken, specialChickenConfig.lightningChicken);
                    }
                    if (isBomb)
                    {
                        ApplyBombChickenConfig(newChicken, specialChickenConfig.bombChicken);
                    }
                }
                
                // 调试信息：输出生成的鸡的类型
                string chickenType = "普通鸡";
                if (isGolden)
                {
                    chickenType = "金鸡";
                }
                else if (isLightning)
                {
                    chickenType = "闪电鸡";
                }
                else if (isBomb)
                {
                    chickenType = "炸弹鸡";
                }
                Debug.LogWarning($"ChickenSpawner: 生成了一只 {chickenType} (位置: {spawnPosition})", this);
                
                // 延迟一帧后设置生命值，确保 Start() 已经执行完毕
                // 特殊鸡的生命值逻辑与金鸡相同，因此传入 (isGolden || isLightning || isBomb)
                StartCoroutine(DelayedSetChickenHealth(newChicken, isGolden || isLightning || isBomb));
                
                newChicken.SetUniqueSortingOffset(sortingOrderCounter++);
                
                spawnedChickens.Add(newChicken);
            }
        }

        /// <summary>
        /// 延迟设置鸡的生命值（确保 Start() 已经执行完毕）
        /// </summary>
        private System.Collections.IEnumerator DelayedSetChickenHealth(ChickenUnit chicken, bool isGolden)
        {
            // 等待一帧，确保 Start() 已经执行完毕
            yield return null;
            
            if (chicken != null)
            {
                SetChickenHealthByStage(chicken, isGolden);
            }
        }

        /// <summary>
        /// 根据当前场地等级的生命值等级和鸡的类型设置鸡的生命值
        /// </summary>
        /// <param name="chicken">鸡单位</param>
        /// <param name="isGolden">是否为金鸡</param>
        public void SetChickenHealthByStage(ChickenUnit chicken, bool isGolden)
        {
            if (chicken == null)
            {
                Debug.LogWarning("ChickenSpawner: SetChickenHealthByStage - chicken 为空", this);
                return; // 如果鸡为空，使用默认生命值
            }

            // 获取当前场地等级的生命值等级（优先使用场地等级）
            int healthLevel = 0; // 默认等级0
            string levelSource = "默认值";
            
            // 优先使用场地等级的生命值等级
            if (AreaUpgradeManager.Instance != null && AreaUpgradeManager.Instance.CurrentAreaData != null)
            {
                healthLevel = AreaUpgradeManager.Instance.CurrentAreaData.ChickenHealthLevel;
                levelSource = $"AreaUpgradeManager (场地等级: {AreaUpgradeManager.Instance.CurrentAreaLevel})";
            }
            // 如果没有场地等级，回退到阶段等级
            else if (FractalUniverseManager.Instance != null && FractalUniverseManager.Instance.CurrentStage != null)
            {
                healthLevel = FractalUniverseManager.Instance.CurrentStage.ChickenHealthLevel;
                levelSource = $"FractalUniverseManager (阶段: {FractalUniverseManager.Instance.CurrentStage.StageName})";
            }
            else
            {
                Debug.LogWarning($"ChickenSpawner: 无法获取场地等级或阶段等级，使用默认生命值等级 0", this);
            }

            float health = 0f;
            string chickenType = isGolden ? "金鸡" : "普通鸡";

            // 根据鸡的类型选择不同的配置
            if (isGolden)
            {
                if (goldenChickenHealthConfig != null)
                {
                    health = goldenChickenHealthConfig.GetHealthByLevel(healthLevel);
                }
                else
                {
                    if (healthConfig != null)
                    {
                        float baseHealth = healthConfig.GetHealthByLevel(healthLevel);
                        health = baseHealth * 1.5f;
                    }
                    else
                    {
                        health = 150f; // 最终默认值
                        Debug.LogWarning($"ChickenSpawner: 金鸡和普通鸡生命值配置都未设置，使用最终默认值 {health}", this);
                    }
                }
            }
            else
            {
                if (healthConfig != null)
                {
                    health = healthConfig.GetHealthByLevel(healthLevel);
                }
                else
                {
                    health = 100f;
                    Debug.LogWarning($"ChickenSpawner: 普通鸡生命值配置未设置 (healthConfig 为 null)，使用默认值 {health}", this);
                }
            }

            chicken.SetMaxHP(health);
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
        /// 根据权重随机选择特殊鸡类型
        /// </summary>
        /// <returns>选中的特殊鸡类型</returns>
        private SpecialChickenType SelectSpecialChickenTypeByWeight()
        {
            // 如果没有配置，返回普通鸡
            if (specialChickenConfig == null || !specialChickenConfig.enableSpecialChickens)
            {
                return SpecialChickenType.None;
            }

            // 构建权重列表
            List<(SpecialChickenType type, float weight)> weightedOptions = new List<(SpecialChickenType, float)>();

            // 检查金鸡（使用配置中的权重，不依赖技能解锁，方便测试）
            if (specialChickenConfig.IsEnabled(SpecialChickenType.Golden))
            {
                float weight = specialChickenConfig.GetWeight(SpecialChickenType.Golden);
                if (weight > 0f)
                {
                    weightedOptions.Add((SpecialChickenType.Golden, weight));
                }
            }

            // 检查闪电鸡
            if (specialChickenConfig.IsEnabled(SpecialChickenType.Lightning))
            {
                float weight = specialChickenConfig.GetWeight(SpecialChickenType.Lightning);
                if (weight > 0f)
                {
                    weightedOptions.Add((SpecialChickenType.Lightning, weight));
                }
            }

            // 检查炸弹鸡
            if (specialChickenConfig.IsEnabled(SpecialChickenType.Bomb))
            {
                float weight = specialChickenConfig.GetWeight(SpecialChickenType.Bomb);
                if (weight > 0f)
                {
                    weightedOptions.Add((SpecialChickenType.Bomb, weight));
                }
            }

            // 如果没有可选项，返回普通鸡
            if (weightedOptions.Count == 0)
            {
                return SpecialChickenType.None;
            }

            // 计算总权重
            float totalWeight = 0f;
            foreach (var option in weightedOptions)
            {
                totalWeight += option.weight;
            }

            // 如果总权重为0，返回普通鸡
            if (totalWeight <= 0f)
            {
                return SpecialChickenType.None;
            }

            // 随机选择一个值（0 到 totalWeight 之间）
            float randomValue = UnityEngine.Random.Range(0f, totalWeight);

            // 根据权重选择类型
            float currentWeight = 0f;
            foreach (var option in weightedOptions)
            {
                currentWeight += option.weight;
                if (randomValue < currentWeight)
                {
                    return option.type;
                }
            }

            // 如果出现意外情况，返回最后一个选项
            return weightedOptions[weightedOptions.Count - 1].type;
        }

        /// <summary>
        /// 统计并输出当前场景中所有鸡的种类信息
        /// </summary>
        private void LogChickenStatistics()
        {
            // 查找场景中所有的鸡
            ChickenUnit[] allChickens = FindObjectsByType<ChickenUnit>(FindObjectsSortMode.None);
            
            int totalCount = 0;
            int normalCount = 0;
            int goldenCount = 0;
            int lightningCount = 0;
            int bombCount = 0;
            
            foreach (ChickenUnit chicken in allChickens)
            {
                if (chicken != null)
                {
                    totalCount++;
                    bool isGolden = chicken.IsGolden;
                    bool isLightning = chicken.IsLightningChicken;
                    bool isBomb = chicken.IsBombChicken;
                    
                    // 一只鸡只能是一种特殊类型
                    if (isGolden)
                    {
                        goldenCount++;
                    }
                    else if (isLightning)
                    {
                        lightningCount++;
                    }
                    else if (isBomb)
                    {
                        bombCount++;
                    }
                    else
                    {
                        normalCount++;
                    }
                }
            }
            
            // 输出统计信息
            Debug.LogWarning($"=== 鸡的种类统计 ===\n" +
                $"总鸡数: {totalCount}\n" +
                $"普通鸡: {normalCount}\n" +
                $"金鸡: {goldenCount}\n" +
                $"闪电鸡: {lightningCount}\n" +
                $"炸弹鸡: {bombCount}\n" +
                $"特殊鸡配置: {(specialChickenConfig != null ? "已配置" : "未配置")}", this);
        }

        /// <summary>
        /// 应用闪电鸡配置到鸡单位
        /// </summary>
        private void ApplyLightningChickenConfig(ChickenUnit chicken, SpecialChickenConfig.LightningChickenSettings config)
        {
            if (chicken == null || config == null) return;

            // 使用反射设置私有字段
            var lightningEggTriggerChanceField = typeof(ChickenUnit).GetField("lightningEggTriggerChance", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var lightningMaxTargetsField = typeof(ChickenUnit).GetField("lightningMaxTargets", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var lightningChainDecayField = typeof(ChickenUnit).GetField("lightningChainDecay", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var lightningDamagePercentField = typeof(ChickenUnit).GetField("lightningDamagePercent", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var lightningRangeField = typeof(ChickenUnit).GetField("lightningRange", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (lightningEggTriggerChanceField != null) lightningEggTriggerChanceField.SetValue(chicken, config.eggTriggerChance);
            if (lightningMaxTargetsField != null) lightningMaxTargetsField.SetValue(chicken, config.maxTargets);
            if (lightningChainDecayField != null) lightningChainDecayField.SetValue(chicken, config.chainDecay);
            if (lightningDamagePercentField != null) lightningDamagePercentField.SetValue(chicken, config.damagePercent);
            if (lightningRangeField != null) lightningRangeField.SetValue(chicken, config.range);
        }

        /// <summary>
        /// 应用炸弹鸡配置到鸡单位
        /// </summary>
        private void ApplyBombChickenConfig(ChickenUnit chicken, SpecialChickenConfig.BombChickenSettings config)
        {
            if (chicken == null || config == null) return;

            // 使用反射设置私有字段
            var bombEggTriggerChanceField = typeof(ChickenUnit).GetField("bombEggTriggerChance", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var bombExplosionRadiusField = typeof(ChickenUnit).GetField("bombExplosionRadius", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var bombDamageMultiplierField = typeof(ChickenUnit).GetField("bombDamageMultiplier", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var bombDamageFalloffField = typeof(ChickenUnit).GetField("bombDamageFalloff", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (bombEggTriggerChanceField != null) bombEggTriggerChanceField.SetValue(chicken, config.eggTriggerChance);
            if (bombExplosionRadiusField != null) bombExplosionRadiusField.SetValue(chicken, config.explosionRadius);
            if (bombDamageMultiplierField != null) bombDamageMultiplierField.SetValue(chicken, config.damageMultiplier);
            if (bombDamageFalloffField != null) bombDamageFalloffField.SetValue(chicken, config.damageFalloff);
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
            
            sortingOrderCounter = 0;
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

