using UnityEngine;
using System.Collections.Generic;
using ProjectChicken.Systems.SkillTree;
using ProjectChicken.Systems.Save;

namespace ProjectChicken.Core
{
    /// <summary>
    /// 升级管理器：管理玩家属性升级和技能解锁
    /// </summary>
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        [Header("Data Configuration")]
        [SerializeField] private List<SkillNodeData> skillRegistry; // Database of all skills to look up by ID during loading.

        [Header("初始属性值（可在 Inspector 中配置）")]
        [SerializeField] private float initialDamage = 10f; // 初始攻击力
        [SerializeField] private float initialRadius = 2f; // 初始攻击范围
        [SerializeField] private float initialAttackInterval = 1f; // 初始攻击间隔（秒，越小越快）
        [SerializeField] private float initialSessionDuration = 60f; // 初始回合时间（秒）

        [Header("当前属性值（运行时显示）")]
        [SerializeField] private float currentDamage; // 当前攻击力
        [SerializeField] private float currentRadius; // 当前攻击范围
        [SerializeField] private float currentAttackInterval; // 当前攻击间隔（秒）
        [SerializeField] private float currentSessionDuration; // 当前回合时间（秒）

        // 技能等级字典：存储每个技能的当前等级（key: 技能ID, value: 当前等级，0表示未解锁）
        private Dictionary<string, int> skillLevels = new Dictionary<string, int>();

        // 恢复与分裂相关属性
        private float recoveryChance = 0.5f; // 基础恢复几率（50%）
        private bool isMitosisUnlocked = false; // 是否解锁分裂能力
        private float mitosisChance = 0f; // 分裂几率
        private int extraMaxChickens = 0; // 额外最大鸡数量

        // 金鸡相关属性
        private float goldenChickenSpawnRate = 0.05f; // 金鸡生成率（基础5%）
        private int goldenEggMultiplier = 5; // 金蛋价值倍率（基础1金蛋=5普通蛋）

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
        /// 金鸡生成率（只读属性，0-1之间）
        /// </summary>
        public float GoldenChickenSpawnRate => goldenChickenSpawnRate;

        /// <summary>
        /// 金蛋价值倍率（只读属性）
        /// </summary>
        public int GoldenEggMultiplier => goldenEggMultiplier;

        private void Awake()
        {
            // 单例模式：防止重复创建
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // 初始化属性值：从初始值设置当前值
            // 注意：每次游戏开始时都会重置为初始值
            currentDamage = initialDamage;
            currentRadius = initialRadius;
            currentAttackInterval = initialAttackInterval;
            currentSessionDuration = initialSessionDuration;

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

            // 检查是否已达到最大等级
            if (IsMaxLevel(node))
            {
                Debug.Log($"UpgradeManager: 技能 {node.DisplayName} 已达到最大等级！", this);
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

            // 保存游戏数据
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.SaveGame();
            }

            Debug.Log($"UpgradeManager: 成功升级技能 {node.DisplayName} 到等级 {currentLevel + 1}！", this);
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
                    Debug.Log($"UpgradeManager: 攻击力提升 {value}，当前攻击力：{currentDamage}", this);
                    break;

                case SkillEffectType.AttackRadius:
                    currentRadius += value;
                    Debug.Log($"UpgradeManager: 攻击范围提升 {value}，当前范围：{currentRadius}", this);
                    break;

                case SkillEffectType.AttackSpeed:
                    // 攻击速度提升：减少攻击间隔（注意：value 应该是负数或正数，取决于设计）
                    // 如果 value 是正数，表示减少间隔（例如：-0.1 表示减少 0.1 秒）
                    // 如果 value 是负数，表示增加速度倍数（例如：0.1 表示速度提升 10%）
                    // 这里假设 value 是负数，表示减少间隔
                    currentAttackInterval = Mathf.Max(0.1f, currentAttackInterval + value); // 最小间隔 0.1 秒
                    Debug.Log($"UpgradeManager: 攻击间隔减少 {value}，当前间隔：{currentAttackInterval}", this);
                    break;

                case SkillEffectType.SessionDuration:
                    // 回合时间提升：增加游戏时长
                    currentSessionDuration += value;
                    Debug.Log($"UpgradeManager: 回合时间增加 {value}，当前回合时间：{currentSessionDuration}", this);
                    break;

                case SkillEffectType.MoveSpeed:
                    // 预留：移动速度提升（如果需要）
                    Debug.Log($"UpgradeManager: 移动速度提升 {value}（功能预留）", this);
                    break;

                case SkillEffectType.RecoveryChance:
                    recoveryChance = Mathf.Clamp01(recoveryChance + value);
                    Debug.Log($"UpgradeManager: 恢复几率提升 {value}，当前恢复几率：{recoveryChance * 100f}%", this);
                    break;

                case SkillEffectType.UnlockMitosis:
                    isMitosisUnlocked = true;
                    Debug.Log($"UpgradeManager: 已解锁分裂能力！", this);
                    break;

                case SkillEffectType.MitosisChance:
                    mitosisChance = Mathf.Clamp01(mitosisChance + value);
                    Debug.Log($"UpgradeManager: 分裂几率提升 {value}，当前分裂几率：{mitosisChance * 100f}%", this);
                    break;

                case SkillEffectType.MaxChickenCount:
                    extraMaxChickens += Mathf.RoundToInt(value);
                    Debug.Log($"UpgradeManager: 最大鸡数量提升 {value}，当前额外上限：{extraMaxChickens}", this);
                    break;

                case SkillEffectType.GoldenChickenSpawnRate:
                    goldenChickenSpawnRate = Mathf.Clamp01(goldenChickenSpawnRate + value);
                    Debug.Log($"UpgradeManager: 金鸡生成率提升 {value}，当前生成率：{goldenChickenSpawnRate * 100f}%", this);
                    break;

                case SkillEffectType.GoldenEggMultiplier:
                    goldenEggMultiplier += Mathf.RoundToInt(value);
                    Debug.Log($"UpgradeManager: 金蛋价值倍率提升 {value}，当前倍率：{goldenEggMultiplier}", this);
                    break;

                default:
                    Debug.LogWarning($"UpgradeManager: 未知的技能效果类型：{node.EffectType}", this);
                    break;
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
                Debug.Log("UpgradeManager: 没有需要加载的技能数据。", this);
                return;
            }

            // 清除当前技能等级字典
            skillLevels.Clear();

            // 重置属性值到初始值（重要！否则属性会无限叠加）
            currentDamage = initialDamage;
            currentRadius = initialRadius;
            currentAttackInterval = initialAttackInterval;
            currentSessionDuration = initialSessionDuration;

            // 重置恢复与分裂相关属性
            recoveryChance = 0.5f;
            isMitosisUnlocked = false;
            mitosisChance = 0f;
            extraMaxChickens = 0;

            // 重置金鸡相关属性
            goldenChickenSpawnRate = 0.05f;
            goldenEggMultiplier = 5;

            Debug.Log($"UpgradeManager: 开始加载 {skillRecords.Count} 个技能记录...", this);

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

                    Debug.Log($"UpgradeManager: 已加载技能 '{skill.DisplayName}' (ID: {record.id}, 等级: {record.level})", this);
                }
                else
                {
                    Debug.LogWarning($"UpgradeManager: 无法加载技能 ID '{record.id}'，该技能可能已被删除或未添加到注册表。", this);
                }
            }

            Debug.Log($"UpgradeManager: 技能加载完成，共加载 {skillLevels.Count} 个技能。", this);
        }

        /// <summary>
        /// 加载已解锁的技能列表（兼容旧代码，用于向后兼容）
        /// </summary>
        /// <param name="savedIDs">保存的已解锁技能ID列表</param>
        public void LoadSkills(List<string> savedIDs)
        {
            if (savedIDs == null || savedIDs.Count == 0)
            {
                Debug.Log("UpgradeManager: 没有需要加载的技能数据。", this);
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
            currentDamage = initialDamage;
            currentRadius = initialRadius;
            currentAttackInterval = initialAttackInterval;
            currentSessionDuration = initialSessionDuration;

            // 重置恢复与分裂相关属性
            recoveryChance = 0.5f;
            isMitosisUnlocked = false;
            mitosisChance = 0f;
            extraMaxChickens = 0;

            // 重置金鸡相关属性
            goldenChickenSpawnRate = 0.05f;
            goldenEggMultiplier = 5;

            Debug.Log("UpgradeManager: 所有技能已重置", this);
        }
    }
}

