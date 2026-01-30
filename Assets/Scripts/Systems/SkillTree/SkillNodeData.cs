using UnityEngine;
using System.Collections.Generic;

namespace ProjectChicken.Systems.SkillTree
{
    /// <summary>
    /// 技能效果类型枚举
    /// </summary>
    public enum SkillEffectType
    {
        AttackDamage,    // 攻击力提升
        AttackRadius,    // 攻击范围提升
        AttackSpeed,     // 攻击速度提升（减少攻击间隔）
        SessionDuration, // 回合时间提升（增加游戏时长）
        MoveSpeed,       // 移动速度提升（预留）
        RecoveryChance,  // 恢复几率提升（增加从瘦鸡变回肥鸡的成功率）
        UnlockMitosis,   // 解锁分裂能力
        MitosisChance,   // 分裂几率提升（增加分裂的概率）
        MaxChickenCount, // 最大鸡数量提升（增加人口上限）
        InitialChickenCount, // 初始鸡数量提升（回合开始时生成的鸡数量）
        UnlockGoldenChicken, // 解锁金鸡生成能力（初始没有金鸡）
        GoldenChickenSpawnRate, // 金鸡生成率提升
        GoldenEggMultiplier,     // 金蛋价值倍率提升
        CritChance,              // 暴击率提升（攻击时有概率造成额外伤害）
        UnlockDoubleProduction,  // 解锁双倍产出能力
        DoubleProductionChance,  // 双倍产出概率提升（产蛋时有概率产出双倍）
        UnlockGravityWave,        // 解锁引力波能力（鸡生蛋时，有概率让回合时间增加0.2秒）
        GravityWaveChance,        // 引力波概率提升（提升鸡生蛋时触发引力波的概率）
        UnlockMammal,             // 解锁哺乳动物能力（每只鸡下蛋时，有概率不生鸡蛋而产生一只鸡）
        MammalChance,             // 哺乳动物概率提升（提升鸡生鸡的概率）
        // 闪电鸡
        UnlockLightningChicken,   // 解锁闪电鸡（EffectValue 为基础权重）
        LightningChickenSpawnRate, // 增加闪电鸡出现概率（累加到权重）
        LightningChickenDamageMultiplier, // 增加闪电鸡伤害（攻击力倍率，累加）
        LightningChainCount,     // 增加闪电连锁数量（累加）
        // 炸弹鸡
        UnlockBombChicken,        // 解锁炸弹鸡（EffectValue 为基础权重）
        BombChickenSpawnRate,    // 增加炸弹鸡出现概率（累加到权重）
        BombChickenDamageMultiplier, // 增加炸弹鸡伤害（攻击力倍率，累加）
        BombExplosionRadius,     // 增加爆炸范围（累加）
        // 黑洞鸡
        UnlockBlackHoleChicken,  // 解锁黑洞鸡（EffectValue 为基础权重）
        BlackHoleChickenSpawnRate, // 增加黑洞鸡出现概率（累加到权重）
        BlackHolePullForce,       // 增加黑洞质量/吸引力（累加）
        BlackHoleRadius,         // 增加黑洞范围（累加）
        // 篮球鸡
        UnlockBasketballChicken,  // 解锁篮球鸡（EffectValue 为基础权重）
        BasketballChickenSpawnRate, // 增加篮球鸡出现概率（累加到权重）
        BasketballDamageMultiplier, // 增加篮球伤害（累加）
        BasketballBounceCount    // 增加篮球反弹次数（累加）
    }

    /// <summary>
    /// 技能节点数据：使用 ScriptableObject 存储技能信息
    /// </summary>
    [CreateAssetMenu(fileName = "New Skill Node", menuName = "Project Chicken/Skill Node", order = 1)]
    public class SkillNodeData : ScriptableObject
    {
        /// <summary>
        /// 技能等级信息：包含每级的消耗和效果值
        /// </summary>
        [System.Serializable]
        public struct SkillLevelInfo
        {
            public int cost; // 升级到此等级所需的消耗
            public float effectValue; // 此等级增加的效果值（增量）
        }

        [Header("基础信息")]
        [SerializeField] private string id; // 唯一标识符
        [SerializeField] private Sprite icon; // UI图标
        [SerializeField] private string displayName; // 技能名称
        [TextArea(3, 5)]
        [SerializeField] private string description; // 描述

        [Header("购买条件")]
        [SerializeField] private SkillNodeData prerequisite; // 前置技能节点（可为空）
        [SerializeField] private List<SkillNodeData> prerequisites; // 前置技能节点列表（依赖连接）

        [Header("效果")]
        [SerializeField] private SkillEffectType effectType; // 效果类型

        [Header("等级配置")]
        [SerializeField] private List<SkillLevelInfo> levels; // 技能等级列表（索引0为第1级，索引1为第2级，以此类推）

        // 公共属性（只读）
        public string ID => id;
        public Sprite Icon => icon;
        public string DisplayName => displayName;
        public string Description => description;
        public SkillNodeData Prerequisite => prerequisite;
        public List<SkillNodeData> Prerequisites => prerequisites;
        public SkillEffectType EffectType => effectType;

        /// <summary>
        /// 最大等级数
        /// </summary>
        public int MaxLevel => levels != null ? levels.Count : 0;

        /// <summary>
        /// 获取指定等级的消耗
        /// </summary>
        /// <param name="currentLevel">当前等级（0-based，0表示第1级）</param>
        /// <returns>升级到该等级所需的消耗，如果索引超出范围则返回0</returns>
        public int GetCost(int currentLevel)
        {
            if (levels == null || currentLevel < 0 || currentLevel >= levels.Count)
            {
                return 0;
            }
            return levels[currentLevel].cost;
        }

        /// <summary>
        /// 获取指定等级的效果值
        /// </summary>
        /// <param name="currentLevel">当前等级（0-based，0表示第1级）</param>
        /// <returns>该等级增加的效果值，如果索引超出范围则返回0</returns>
        public float GetEffectValue(int currentLevel)
        {
            if (levels == null || currentLevel < 0 || currentLevel >= levels.Count)
            {
                return 0f;
            }
            return levels[currentLevel].effectValue;
        }
    }
}

