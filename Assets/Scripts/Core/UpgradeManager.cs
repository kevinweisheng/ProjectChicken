using UnityEngine;
using System.Collections.Generic;
using ProjectChicken.Systems.SkillTree;
using ProjectChicken.Systems.Save;
using ProjectChicken.Systems;

namespace ProjectChicken.Core
{
    /// <summary>
    /// 升级管理器：管理玩家属性升级和技能解锁
    /// </summary>
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        [Header("Data Configuration")]
        [SerializeField] private List<SkillNodeData> skillRegistry; // Database of all skills to look up by ID during loading (auto-populated at runtime).
        [SerializeField] private bool autoPopulateRegistry = true; // 是否自动填充注册表（运行时从技能树UI收集所有技能）
        [SerializeField] private GameConfig gameConfig; // 游戏配置（集中管理所有初始值）

        [Header("当前属性值（运行时显示）")]
        [SerializeField] private float currentDamage; // 当前攻击力
        [SerializeField] private float currentRadius; // 当前攻击范围
        [SerializeField] private float currentAttackInterval; // 当前攻击间隔（秒）
        [SerializeField] private float currentSessionDuration; // 当前回合时间（秒）

        // 技能等级字典：存储每个技能的当前等级（key: 技能ID, value: 当前等级，0表示未解锁）
        private Dictionary<string, int> skillLevels = new Dictionary<string, int>();

        // 恢复与分裂相关属性（初始值从 GameConfig 读取）
        private float recoveryChance;
        private bool isMitosisUnlocked = false; // 是否解锁分裂能力
        private float mitosisChance = 0f; // 分裂几率
        private int extraMaxChickens = 0; // 额外最大鸡数量
        private int extraInitialChickens = 0; // 额外初始鸡数量（回合开始时生成的额外数量）

        // 金鸡相关属性
        private bool isGoldenChickenUnlocked = false; // 是否解锁金鸡生成能力（初始未解锁）
        private float goldenChickenSpawnRate = 0f; // 金鸡生成率（初始0%，需要先解锁才能生成）
        private int goldenEggMultiplier; // 金蛋价值倍率（从 GameConfig 读取）

        // 暴击相关属性（初始值从 GameConfig 读取）
        private float critChance;
        private float critMultiplier;

        // 双倍产出相关属性
        private bool isDoubleProductionUnlocked = false; // 是否解锁双倍产出能力
        private float doubleProductionChance = 0f; // 双倍产出概率（0-1之间，基础0%）

        // 引力波相关属性
        private bool isGravityWaveUnlocked = false; // 是否解锁引力波能力
        private float gravityWaveChance = 0.01f; // 引力波触发概率（基础1%，鸡生蛋时触发引力波的概率）
        private float gravityWaveTimeExtension; // 引力波触发时增加的回合时间（从 GameConfig 读取）

        // 哺乳动物相关属性
        private bool isMammalUnlocked = false; // 是否解锁哺乳动物能力
        private float mammalChance = 0f; // 哺乳动物概率（0-1之间，基础0%，解锁后初始1%）

        /// <summary>
        /// 当前攻击力（只读属性）
        /// </summary>
        public float CurrentDamage => currentDamage;

        /// <summary>
        /// 当前攻击范围（只读属性）
        /// </summary>
        public float CurrentRadius => currentRadius;

        /// <summary>
        /// 当前攻击间隔（只读属性，秒）
        /// </summary>
        public float CurrentAttackInterval => currentAttackInterval;

        /// <summary>
        /// 当前回合时间（只读属性，秒）
        /// </summary>
        public float CurrentSessionDuration => currentSessionDuration;

        /// <summary>
        /// 恢复几率（只读属性，0-1之间）
        /// </summary>
        public float RecoveryChance => recoveryChance;

        /// <summary>
        /// 是否解锁分裂能力（只读属性）
        /// </summary>
        public bool IsMitosisUnlocked => isMitosisUnlocked;

        /// <summary>
        /// 分裂几率（只读属性，0-1之间）
        /// </summary>
        public float MitosisChance => mitosisChance;

        /// <summary>
        /// 额外最大鸡数量（只读属性）
        /// </summary>
        public int ExtraMaxChickens => extraMaxChickens;

        /// <summary>
        /// 额外初始鸡数量（只读属性）
        /// </summary>
        public int ExtraInitialChickens => extraInitialChickens;

        /// <summary>
        /// 是否解锁金鸡生成能力（只读属性）
        /// </summary>
        public bool IsGoldenChickenUnlocked => isGoldenChickenUnlocked;

        /// <summary>
        /// 金鸡生成率（只读属性，0-1之间）
        /// 注意：只有解锁后才能生成金鸡
        /// </summary>
        public float GoldenChickenSpawnRate => goldenChickenSpawnRate;

        /// <summary>
        /// 金蛋价值倍率（只读属性）
        /// </summary>
        public int GoldenEggMultiplier => goldenEggMultiplier;

        /// <summary>
        /// 暴击率（只读属性，0-1之间）
        /// </summary>
        public float CritChance => critChance;

        /// <summary>
        /// 暴击倍率（只读属性）
        /// </summary>
        public float CritMultiplier => critMultiplier;

        /// <summary>
        /// 是否解锁双倍产出能力（只读属性）
        /// </summary>
        public bool IsDoubleProductionUnlocked => isDoubleProductionUnlocked;

        /// <summary>
        /// 双倍产出概率（只读属性，0-1之间）
        /// </summary>
        public float DoubleProductionChance => doubleProductionChance;

        /// <summary>
        /// 是否解锁引力波能力（只读属性）
        /// 解锁后，鸡生蛋时有概率触发引力波，增加回合时间
        /// </summary>
        public bool IsGravityWaveUnlocked => isGravityWaveUnlocked;

        /// <summary>
        /// 引力波触发概率（只读属性，0-1之间）
        /// 鸡生蛋时，有该概率触发引力波，增加回合时间
        /// </summary>
        public float GravityWaveChance => gravityWaveChance;

        /// <summary>
        /// 引力波触发时增加的回合时间（只读属性，秒）
        /// </summary>
        public float GravityWaveTimeExtension => gravityWaveTimeExtension;

        /// <summary>
        /// 是否解锁哺乳动物能力（只读属性）
        /// 解锁后，每只鸡下蛋时，有概率不生鸡蛋而产生一只鸡
        /// </summary>
        public bool IsMammalUnlocked => isMammalUnlocked;

        /// <summary>
        /// 哺乳动物概率（只读属性，0-1之间）
        /// 每只鸡下蛋时，有该概率不生鸡蛋而产生一只鸡
        /// </summary>
        public float MammalChance => mammalChance;

        private void Awake()
        {
            // 单例模式：防止重复创建
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // 初始化属性值：从 GameConfig 读取初始值（如果没有配置则使用默认值）
            InitializeFromConfig();

            // 自动填充技能注册表（如果需要）
            if (autoPopulateRegistry)
            {
                PopulateSkillRegistry();
            }

            // 安全检查：确保技能注册表已配置
            if (skillRegistry == null || skillRegistry.Count == 0)
            {
                Debug.LogWarning("UpgradeManager: Skill Registry is empty! Save/Load will not restore stats.", this);
            }

            // 如果需要跨场景保持，取消下面这行的注释
            // DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 获取技能的当前等级
        /// </summary>
        /// <param name="node">技能节点数据</param>
        /// <returns>当前等级（0表示未解锁）</returns>
        public int GetSkillLevel(SkillNodeData node)
        {
            if (node == null)
            {
                Debug.LogWarning("UpgradeManager: 传入的技能节点为空！", this);
                return 0;
            }

            if (skillLevels.TryGetValue(node.ID, out int level))
            {
                return level;
            }

            return 0; // 默认等级为0（未解锁）
        }

        /// <summary>
        /// 检查技能是否已达到最大等级
        /// </summary>
        /// <param name="node">技能节点数据</param>
        /// <returns>是否已达到最大等级</returns>
        public bool IsMaxLevel(SkillNodeData node)
        {
            if (node == null)
            {
                Debug.LogWarning("UpgradeManager: 传入的技能节点为空！", this);
                return true; // 如果节点为空，视为已满级
            }

            int currentLevel = GetSkillLevel(node);
            return currentLevel >= node.MaxLevel;
        }

        /// <summary>
        /// 检查某个技能是否已解锁（兼容旧代码）
        /// </summary>
        /// <param name="node">技能节点数据</param>
        /// <returns>是否已解锁（等级 > 0）</returns>
        public bool IsNodeUnlocked(SkillNodeData node)
        {
            return GetSkillLevel(node) > 0;
        }

        /// <summary>
        /// 尝试升级技能（支持多级升级）
        /// </summary>
        /// <param name="node">要升级的技能节点</param>
        /// <returns>是否成功升级</returns>
        public bool TryUpgradeSkill(SkillNodeData node)
        {
            if (node == null)
            {
                Debug.LogWarning("UpgradeManager: 尝试升级的技能节点为空！", this);
                return false;
            }

            if (IsMaxLevel(node))
            {
                return false;
            }

            // 检查前置技能是否解锁（对于首次解锁，需要检查前置）
            int currentLevel = GetSkillLevel(node);
            if (currentLevel == 0 && node.Prerequisite != null && !IsNodeUnlocked(node.Prerequisite))
            {
                Debug.LogWarning($"UpgradeManager: 前置技能 {node.Prerequisite.DisplayName} 未解锁，无法解锁 {node.DisplayName}！", this);
                return false;
            }

            // 获取当前等级的消耗
            int cost = node.GetCost(currentLevel);
            if (cost <= 0)
            {
                Debug.LogWarning($"UpgradeManager: 技能 {node.DisplayName} 等级 {currentLevel} 的消耗无效！", this);
                return false;
            }

            // 检查资源是否足够并扣钱
            if (ResourceManager.Instance == null)
            {
                Debug.LogError("UpgradeManager: ResourceManager.Instance 为空！无法检查资源。", this);
                return false;
            }

            if (!ResourceManager.Instance.SpendEgg(cost))
            {
                // SpendEgg 方法内部已经输出了警告信息
                return false;
            }

            // 获取当前等级的效果值增量
            float effectValue = node.GetEffectValue(currentLevel);

            // 应用属性提升（传入具体的效果值）
            ApplySkillEffect(node, effectValue);

            // 更新等级
            skillLevels[node.ID] = currentLevel + 1;

            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.SaveGame();
            }

            return true;
        }

        /// <summary>
        /// 尝试解锁技能（兼容旧代码，内部调用 TryUpgradeSkill）
        /// </summary>
        /// <param name="node">要解锁的技能节点</param>
        /// <returns>是否成功解锁</returns>
        public bool TryUnlockNode(SkillNodeData node)
        {
            return TryUpgradeSkill(node);
        }

        /// <summary>
        /// 应用技能效果
        /// </summary>
        /// <param name="node">技能节点</param>
        /// <param name="value">效果值增量</param>
        private void ApplySkillEffect(SkillNodeData node, float value)
        {
            switch (node.EffectType)
            {
                case SkillEffectType.AttackDamage:
                    currentDamage += value;
                    break;

                case SkillEffectType.AttackRadius:
                    currentRadius += value;
                    break;

                case SkillEffectType.AttackSpeed:
                    currentAttackInterval = Mathf.Max(0.1f, currentAttackInterval + value);
                    break;

                case SkillEffectType.SessionDuration:
                    currentSessionDuration += value;
                    break;

                case SkillEffectType.MoveSpeed:
                    break;

                case SkillEffectType.RecoveryChance:
                    recoveryChance = Mathf.Clamp01(recoveryChance + value);
                    break;

                case SkillEffectType.UnlockMitosis:
                    isMitosisUnlocked = true;
                    break;

                case SkillEffectType.MitosisChance:
                    mitosisChance = Mathf.Clamp01(mitosisChance + value);
                    break;

                case SkillEffectType.MaxChickenCount:
                    extraMaxChickens += Mathf.RoundToInt(value);
                    break;

                case SkillEffectType.InitialChickenCount:
                    extraInitialChickens += Mathf.RoundToInt(value);
                    break;

                case SkillEffectType.UnlockGoldenChicken:
                    isGoldenChickenUnlocked = true;
                    break;

                case SkillEffectType.GoldenChickenSpawnRate:
                    goldenChickenSpawnRate = Mathf.Clamp01(goldenChickenSpawnRate + value);
                    break;

                case SkillEffectType.GoldenEggMultiplier:
                    goldenEggMultiplier += Mathf.RoundToInt(value);
                    break;

                case SkillEffectType.CritChance:
                    critChance = Mathf.Clamp01(critChance + value);
                    break;

                case SkillEffectType.UnlockDoubleProduction:
                    isDoubleProductionUnlocked = true;
                    break;

                case SkillEffectType.DoubleProductionChance:
                    doubleProductionChance = Mathf.Clamp01(doubleProductionChance + value);
                    break;

                case SkillEffectType.UnlockGravityWave:
                    isGravityWaveUnlocked = true;
                    break;

                case SkillEffectType.GravityWaveChance:
                    gravityWaveChance = Mathf.Clamp01(gravityWaveChance + value);
                    break;

                case SkillEffectType.UnlockMammal:
                    isMammalUnlocked = true;
                    if (mammalChance <= 0f)
                    {
                        mammalChance = 0.01f;
                    }
                    break;

                case SkillEffectType.MammalChance:
                    mammalChance = Mathf.Clamp01(mammalChance + value);
                    break;

                default:
                    Debug.LogWarning($"UpgradeManager: 未知的技能效果类型：{node.EffectType}", this);
                    break;
            }
        }

        /// <summary>
        /// 从 GameConfig 初始化所有属性值（如果没有配置则使用默认值）
        /// </summary>
        private void InitializeFromConfig()
        {
            float initialDamage = gameConfig != null ? gameConfig.InitialDamage : 10f;
            float initialRadius = gameConfig != null ? gameConfig.InitialRadius : 2f;
            float initialAttackInterval = gameConfig != null ? gameConfig.InitialAttackInterval : 1f;
            float initialSessionDuration = gameConfig != null ? gameConfig.InitialSessionDuration : 60f;
            
            // 从 GameConfig 读取其他初始值
            recoveryChance = gameConfig != null ? gameConfig.RecoveryChance : 0.5f;
            critChance = gameConfig != null ? gameConfig.InitialCritChance : 0f;
            critMultiplier = gameConfig != null ? gameConfig.CritMultiplier : 2f;
            goldenEggMultiplier = gameConfig != null ? gameConfig.GoldenEggMultiplier : 5;
            gravityWaveTimeExtension = gameConfig != null ? gameConfig.GravityWaveTimeExtension : 0.2f;

            // 设置当前属性值
            currentDamage = initialDamage;
            currentRadius = initialRadius;
            currentAttackInterval = initialAttackInterval;
            currentSessionDuration = initialSessionDuration;
        }

        /// <summary>
        /// 自动填充技能注册表：从技能树UI中收集所有使用的 SkillNodeData
        /// 如果 skillRegistry 为空或 autoPopulateRegistry 为 true，则会自动收集所有技能节点
        /// </summary>
        private void PopulateSkillRegistry()
        {
            // 确保 skillRegistry 已初始化
            if (skillRegistry == null)
            {
                skillRegistry = new List<SkillNodeData>();
            }

            // 使用 HashSet 来避免重复添加
            HashSet<string> existingIDs = new HashSet<string>();
            foreach (var skill in skillRegistry)
            {
                if (skill != null && !string.IsNullOrEmpty(skill.ID))
                {
                    existingIDs.Add(skill.ID);
                }
            }

            int addedCount = 0;

            // 方法1：从技能树UI中收集所有使用的 SkillNodeData
            // 查找场景中所有的 SkillSlotUI 组件，收集它们的 TargetSkill
            var skillSlotUIs = FindObjectsByType<ProjectChicken.UI.SkillSlotUI>(FindObjectsSortMode.None);
            foreach (var slotUI in skillSlotUIs)
            {
                if (slotUI != null)
                {
                    // 使用公开的 TargetSkill 属性获取技能数据
                    SkillNodeData skill = slotUI.TargetSkill;
                    if (skill != null && !string.IsNullOrEmpty(skill.ID))
                    {
                        if (!existingIDs.Contains(skill.ID))
                        {
                            skillRegistry.Add(skill);
                            existingIDs.Add(skill.ID);
                            addedCount++;
                        }
                    }
                }
            }

            // 方法2：如果方法1没有收集到技能，尝试从 Resources 文件夹加载所有 SkillNodeData
            // 注意：这需要将 SkillNodeData 放在 Resources 文件夹中
            if (skillRegistry.Count == 0)
            {
                SkillNodeData[] allSkills = Resources.LoadAll<SkillNodeData>("");
                foreach (var skill in allSkills)
                {
                    if (skill != null && !string.IsNullOrEmpty(skill.ID))
                    {
                        if (!existingIDs.Contains(skill.ID))
                        {
                            skillRegistry.Add(skill);
                            existingIDs.Add(skill.ID);
                            addedCount++;
                        }
                    }
                }
            }

        }

        /// <summary>
        /// 通过 ID 查找技能节点数据
        /// </summary>
        /// <param name="id">技能ID</param>
        /// <returns>找到的技能节点数据，如果未找到则返回 null</returns>
        private SkillNodeData GetSkillByID(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("UpgradeManager: 传入的技能ID为空！", this);
                return null;
            }

            if (skillRegistry == null || skillRegistry.Count == 0)
            {
                Debug.LogWarning("UpgradeManager: 技能注册表为空！请在 Inspector 中配置 skillRegistry。", this);
                return null;
            }

            // 遍历注册表查找匹配的技能
            foreach (SkillNodeData skill in skillRegistry)
            {
                if (skill != null && skill.ID == id)
                {
                    return skill;
                }
            }

            Debug.LogWarning($"UpgradeManager: 未找到ID为 '{id}' 的技能节点！请确保该技能已添加到 skillRegistry 中。", this);
            return null;
        }

        /// <summary>
        /// 加载技能等级数据（用于从保存文件恢复）
        /// </summary>
        /// <param name="skillRecords">保存的技能记录列表</param>
        public void LoadSkills(List<GameSaveData.SkillSaveRecord> skillRecords)
        {
            if (skillRecords == null || skillRecords.Count == 0)
            {
                return;
            }

            // 清除当前技能等级字典
            skillLevels.Clear();

            // 重置属性值到初始值（重要！否则属性会无限叠加）
            InitializeFromConfig();

            // 重置恢复与分裂相关属性（已在 InitializeFromConfig 中设置 recoveryChance）
            isMitosisUnlocked = false;
            mitosisChance = 0f;
            extraMaxChickens = 0;
            extraInitialChickens = 0; // 重置额外初始鸡数量（重要！否则会在加载时叠加）

            // 重置金鸡相关属性（goldenEggMultiplier 已在 InitializeFromConfig 中设置）
            isGoldenChickenUnlocked = false;
            goldenChickenSpawnRate = 0f;

            // 重置暴击相关属性（已在 InitializeFromConfig 中设置 critChance 和 critMultiplier）

            // 重置双倍产出相关属性
            isDoubleProductionUnlocked = false;
            doubleProductionChance = 0f;

            // 重置引力波相关属性
            isGravityWaveUnlocked = false;
            gravityWaveChance = 0.01f; // 引力波触发概率（基础1%）

            isMammalUnlocked = false;
            mammalChance = 0f;

            // 遍历保存的技能记录列表
            foreach (GameSaveData.SkillSaveRecord record in skillRecords)
            {
                if (string.IsNullOrEmpty(record.id))
                {
                    Debug.LogWarning("UpgradeManager: 遇到空的技能ID，跳过。", this);
                    continue;
                }

                // 通过ID查找技能节点
                SkillNodeData skill = GetSkillByID(record.id);

                if (skill != null)
                {
                    // 设置技能等级
                    skillLevels[record.id] = record.level;

                    // 重新应用所有等级的效果（从等级0到record.level-1，迭代应用以确保逻辑一致性）
                    for (int level = 0; level < record.level; level++)
                    {
                        float effectValue = skill.GetEffectValue(level);
                        ApplySkillEffect(skill, effectValue);
                    }
                }
                else
                {
                    Debug.LogWarning($"UpgradeManager: 无法加载技能 ID '{record.id}'，该技能可能已被删除或未添加到注册表。", this);
                }
            }
        }

        /// <summary>
        /// 加载已解锁的技能列表（兼容旧代码，用于向后兼容）
        /// </summary>
        /// <param name="savedIDs">保存的已解锁技能ID列表</param>
        public void LoadSkills(List<string> savedIDs)
        {
            if (savedIDs == null || savedIDs.Count == 0)
            {
                return;
            }

            // 转换为新的格式：每个技能等级为1
            List<GameSaveData.SkillSaveRecord> records = new List<GameSaveData.SkillSaveRecord>();
            foreach (string id in savedIDs)
            {
                records.Add(new GameSaveData.SkillSaveRecord(id, 1));
            }

            LoadSkills(records);
        }

        /// <summary>
        /// 获取技能保存数据（用于保存）
        /// </summary>
        /// <returns>技能保存记录列表</returns>
        public List<GameSaveData.SkillSaveRecord> GetSkillSaveData()
        {
            List<GameSaveData.SkillSaveRecord> records = new List<GameSaveData.SkillSaveRecord>();

            foreach (var kvp in skillLevels)
            {
                if (kvp.Value > 0) // 只保存等级大于0的技能
                {
                    records.Add(new GameSaveData.SkillSaveRecord(kvp.Key, kvp.Value));
                }
            }

            return records;
        }

        /// <summary>
        /// 获取已解锁的技能ID列表（兼容旧代码，用于向后兼容）
        /// </summary>
        /// <returns>已解锁技能ID列表</returns>
        public List<string> GetUnlockedSkillIDs()
        {
            List<string> ids = new List<string>();
            foreach (var kvp in skillLevels)
            {
                if (kvp.Value > 0) // 只返回等级大于0的技能
                {
                    ids.Add(kvp.Key);
                }
            }
            return ids;
        }

        /// <summary>
        /// 重置所有技能状态（用于清除存档）
        /// </summary>
        public void ResetSkills()
        {
            // 清除技能等级字典
            skillLevels.Clear();

            // 重置属性值到初始值
            InitializeFromConfig();

            // 重置恢复与分裂相关属性（已在 InitializeFromConfig 中设置 recoveryChance）
            isMitosisUnlocked = false;
            mitosisChance = 0f;
            extraMaxChickens = 0;
            extraInitialChickens = 0; // 重置额外初始鸡数量

            // 重置金鸡相关属性（goldenEggMultiplier 已在 InitializeFromConfig 中设置）
            isGoldenChickenUnlocked = false;
            goldenChickenSpawnRate = 0f;

            // 重置暴击相关属性（已在 InitializeFromConfig 中设置 critChance 和 critMultiplier）

            // 重置双倍产出相关属性
            isDoubleProductionUnlocked = false;
            doubleProductionChance = 0f;

            // 重置引力波相关属性
            isGravityWaveUnlocked = false;
            gravityWaveChance = 0.01f; // 引力波触发概率（基础1%）

            isMammalUnlocked = false;
            mammalChance = 0f;
        }
    }
}

