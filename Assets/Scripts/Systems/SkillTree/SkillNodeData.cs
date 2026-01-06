using UnityEngine;

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
        MoveSpeed        // 移动速度提升（预留）
    }

    /// <summary>
    /// 技能节点数据：使用 ScriptableObject 存储技能信息
    /// </summary>
    [CreateAssetMenu(fileName = "New Skill Node", menuName = "Project Chicken/Skill Node", order = 1)]
    public class SkillNodeData : ScriptableObject
    {
        [Header("基础信息")]
        [SerializeField] private string id; // 唯一标识符
        [SerializeField] private Sprite icon; // UI图标
        [SerializeField] private string displayName; // 技能名称
        [TextArea(3, 5)]
        [SerializeField] private string description; // 描述

        [Header("购买条件")]
        [SerializeField] private int cost; // 购买所需鸡蛋数
        [SerializeField] private SkillNodeData prerequisite; // 前置技能节点（可为空）

        [Header("效果")]
        [SerializeField] private SkillEffectType effectType; // 效果类型
        [SerializeField] private float effectValue; // 提升的数值

        // 公共属性（只读）
        public string ID => id;
        public Sprite Icon => icon;
        public string DisplayName => displayName;
        public string Description => description;
        public int Cost => cost;
        public SkillNodeData Prerequisite => prerequisite;
        public SkillEffectType EffectType => effectType;
        public float EffectValue => effectValue;
    }
}

