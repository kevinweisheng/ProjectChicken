using UnityEngine;

namespace ProjectChicken.Systems
{
    /// <summary>
    /// 游戏配置：集中管理所有游戏的初始数值配置
    /// 在这里统一修改所有初始属性值，便于游戏平衡性调整
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Project Chicken/Game Config", order = 0)]
    public class GameConfig : ScriptableObject
    {
        [Header("战斗属性")]
        [Tooltip("初始攻击力")]
        [SerializeField] private float initialDamage = 10f;
        
        [Tooltip("初始攻击范围")]
        [SerializeField] private float initialRadius = 2f;
        
        [Tooltip("初始攻击间隔（秒，越小越快）")]
        [SerializeField] private float initialAttackInterval = 1f;
        
        [Tooltip("初始暴击率（0-1之间，0表示0%）")]
        [SerializeField] private float initialCritChance = 0f;
        
        [Tooltip("暴击倍率（暴击时的伤害倍数）")]
        [SerializeField] private float critMultiplier = 2f;

        [Header("游戏时长")]
        [Tooltip("初始回合时间（秒）")]
        [SerializeField] private float initialSessionDuration = 60f;

        [Header("鸡数量")]
        [Tooltip("初始鸡数量（回合开始时生成的数量）")]
        [SerializeField] private int initialChickenCount = 5;
        
        [Tooltip("基础最大鸡数量（可通过技能提升）")]
        [SerializeField] private int baseMaxChickens = 10;

        [Header("其他属性")]
        [Tooltip("基础恢复几率（0-1之间，0.5表示50%）")]
        [SerializeField] private float recoveryChance = 0.5f;
        
        [Tooltip("金蛋价值倍率（1个金蛋=多少个普通蛋）")]
        [SerializeField] private int goldenEggMultiplier = 5;
        
        [Tooltip("引力波触发时增加的回合时间（秒）")]
        [SerializeField] private float gravityWaveTimeExtension = 0.2f;

        // 公共属性（只读）
        public float InitialDamage => initialDamage;
        public float InitialRadius => initialRadius;
        public float InitialAttackInterval => initialAttackInterval;
        public float InitialCritChance => initialCritChance;
        public float CritMultiplier => critMultiplier;
        public float InitialSessionDuration => initialSessionDuration;
        public int InitialChickenCount => initialChickenCount;
        public int BaseMaxChickens => baseMaxChickens;
        public float RecoveryChance => recoveryChance;
        public int GoldenEggMultiplier => goldenEggMultiplier;
        public float GravityWaveTimeExtension => gravityWaveTimeExtension;
    }
}
