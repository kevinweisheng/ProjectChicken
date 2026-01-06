using UnityEngine;
using System.Collections.Generic;
using ProjectChicken.Systems.SkillTree;

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

        // 已解锁的技能节点集合
        private HashSet<string> unlockedNodeIDs = new HashSet<string>();

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
        /// 检查某个技能是否已解锁
        /// </summary>
        /// <param name="node">技能节点数据</param>
        /// <returns>是否已解锁</returns>
        public bool IsNodeUnlocked(SkillNodeData node)
        {
            if (node == null)
            {
                Debug.LogWarning("UpgradeManager: 传入的技能节点为空！", this);
                return false;
            }

            return unlockedNodeIDs.Contains(node.ID);
        }

        /// <summary>
        /// 尝试解锁技能
        /// </summary>
        /// <param name="node">要解锁的技能节点</param>
        /// <returns>是否成功解锁</returns>
        public bool TryUnlockNode(SkillNodeData node)
        {
            if (node == null)
            {
                Debug.LogWarning("UpgradeManager: 尝试解锁的技能节点为空！", this);
                return false;
            }

            // 检查是否已经解锁
            if (IsNodeUnlocked(node))
            {
                Debug.Log($"UpgradeManager: 技能 {node.DisplayName} 已经解锁！", this);
                return false;
            }

            // 检查前置技能是否解锁
            if (node.Prerequisite != null && !IsNodeUnlocked(node.Prerequisite))
            {
                Debug.LogWarning($"UpgradeManager: 前置技能 {node.Prerequisite.DisplayName} 未解锁，无法解锁 {node.DisplayName}！", this);
                return false;
            }

            // 检查资源是否足够并扣钱
            if (ResourceManager.Instance == null)
            {
                Debug.LogError("UpgradeManager: ResourceManager.Instance 为空！无法检查资源。", this);
                return false;
            }

            if (!ResourceManager.Instance.SpendEgg(node.Cost))
            {
                // SpendEgg 方法内部已经输出了警告信息
                return false;
            }

            // 应用属性提升
            ApplySkillEffect(node);

            // 记录解锁状态
            unlockedNodeIDs.Add(node.ID);

            // 保存游戏数据（确保技能解锁状态被保存）
            // 注意：SpendEgg 已经保存了一次，但那时技能还未解锁，所以需要再次保存
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.SaveGame();
            }

            Debug.Log($"UpgradeManager: 成功解锁技能 {node.DisplayName}！", this);
            return true;
        }

        /// <summary>
        /// 应用技能效果
        /// </summary>
        /// <param name="node">技能节点</param>
        private void ApplySkillEffect(SkillNodeData node)
        {
            switch (node.EffectType)
            {
                case SkillEffectType.AttackDamage:
                    currentDamage += node.EffectValue;
                    Debug.Log($"UpgradeManager: 攻击力提升 {node.EffectValue}，当前攻击力：{currentDamage}", this);
                    break;

                case SkillEffectType.AttackRadius:
                    currentRadius += node.EffectValue;
                    Debug.Log($"UpgradeManager: 攻击范围提升 {node.EffectValue}，当前范围：{currentRadius}", this);
                    break;

                case SkillEffectType.AttackSpeed:
                    // 攻击速度提升：减少攻击间隔（注意：effectValue 应该是负数或正数，取决于设计）
                    // 如果 effectValue 是正数，表示减少间隔（例如：-0.1 表示减少 0.1 秒）
                    // 如果 effectValue 是负数，表示增加速度倍数（例如：0.1 表示速度提升 10%）
                    // 这里假设 effectValue 是负数，表示减少间隔
                    currentAttackInterval = Mathf.Max(0.1f, currentAttackInterval + node.EffectValue); // 最小间隔 0.1 秒
                    Debug.Log($"UpgradeManager: 攻击间隔减少 {node.EffectValue}，当前间隔：{currentAttackInterval}", this);
                    break;

                case SkillEffectType.SessionDuration:
                    // 回合时间提升：增加游戏时长
                    currentSessionDuration += node.EffectValue;
                    Debug.Log($"UpgradeManager: 回合时间增加 {node.EffectValue}，当前回合时间：{currentSessionDuration}", this);
                    break;

                case SkillEffectType.MoveSpeed:
                    // 预留：移动速度提升（如果需要）
                    Debug.Log($"UpgradeManager: 移动速度提升 {node.EffectValue}（功能预留）", this);
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
        /// 加载已解锁的技能列表（用于从保存文件恢复）
        /// </summary>
        /// <param name="savedIDs">保存的已解锁技能ID列表</param>
        public void LoadSkills(List<string> savedIDs)
        {
            if (savedIDs == null || savedIDs.Count == 0)
            {
                Debug.Log("UpgradeManager: 没有需要加载的技能数据。", this);
                return;
            }

            // 清除当前已解锁的技能列表
            unlockedNodeIDs.Clear();

            // 重置属性值到初始值（重要！否则属性会无限叠加）
            currentDamage = initialDamage;
            currentRadius = initialRadius;
            currentAttackInterval = initialAttackInterval;
            currentSessionDuration = initialSessionDuration;

            Debug.Log($"UpgradeManager: 开始加载 {savedIDs.Count} 个已解锁技能...", this);

            // 遍历保存的技能ID列表
            foreach (string id in savedIDs)
            {
                // 通过ID查找技能节点
                SkillNodeData skill = GetSkillByID(id);

                if (skill != null)
                {
                    // 添加到已解锁列表
                    unlockedNodeIDs.Add(id);

                    // 重新应用技能效果以恢复属性加成
                    ApplySkillEffect(skill);

                    Debug.Log($"UpgradeManager: 已加载技能 '{skill.DisplayName}' (ID: {id})", this);
                }
                else
                {
                    Debug.LogWarning($"UpgradeManager: 无法加载技能 ID '{id}'，该技能可能已被删除或未添加到注册表。", this);
                }
            }

            Debug.Log($"UpgradeManager: 技能加载完成，共加载 {unlockedNodeIDs.Count} 个技能。", this);
        }

        /// <summary>
        /// 获取已解锁的技能ID列表（用于保存）
        /// </summary>
        /// <returns>已解锁技能ID列表</returns>
        public List<string> GetUnlockedSkillIDs()
        {
            return new List<string>(unlockedNodeIDs);
        }
    }
}

