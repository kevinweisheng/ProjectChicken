using UnityEngine;

namespace ProjectChicken.Systems
{
    /// <summary>
    /// 肥鸡生命值配置：定义6个等级的生命值数值
    /// 每个场景（阶段）对应一个等级的生命值
    /// </summary>
    [CreateAssetMenu(fileName = "ChickenHealthConfig", menuName = "Project Chicken/Chicken Health Config", order = 1)]
    public class ChickenHealthConfig : ScriptableObject
    {
        [Header("生命值等级配置（6个等级）")]
        [Tooltip("等级0的生命值（第1个场景）")]
        [SerializeField] private float healthLevel0 = 50f;
        
        [Tooltip("等级1的生命值（第2个场景）")]
        [SerializeField] private float healthLevel1 = 100f;
        
        [Tooltip("等级2的生命值（第3个场景）")]
        [SerializeField] private float healthLevel2 = 150f;
        
        [Tooltip("等级3的生命值（第4个场景）")]
        [SerializeField] private float healthLevel3 = 200f;
        
        [Tooltip("等级4的生命值（第5个场景）")]
        [SerializeField] private float healthLevel4 = 250f;
        
        [Tooltip("等级5的生命值（第6个场景）")]
        [SerializeField] private float healthLevel5 = 300f;

        /// <summary>
        /// 根据等级获取生命值（0-5对应6个等级）
        /// </summary>
        /// <param name="level">生命值等级（0-5）</param>
        /// <returns>该等级对应的生命值，如果等级超出范围则返回默认值</returns>
        public float GetHealthByLevel(int level)
        {
            float health = 0f;
            switch (level)
            {
                case 0: 
                    health = healthLevel0;
                    break;
                case 1: 
                    health = healthLevel1;
                    break;
                case 2: 
                    health = healthLevel2;
                    break;
                case 3: 
                    health = healthLevel3;
                    break;
                case 4: 
                    health = healthLevel4;
                    break;
                case 5: 
                    health = healthLevel5;
                    break;
                default:
                    Debug.LogWarning($"ChickenHealthConfig: 无效的生命值等级 {level}，使用默认值 100 (配置对象: {name})", this);
                    return 100f;
            }
            
            return health;
        }

        /// <summary>
        /// 获取所有等级的生命值（用于调试和验证）
        /// </summary>
        /// <returns>包含6个等级生命值的数组</returns>
        public float[] GetAllHealthValues()
        {
            return new float[]
            {
                healthLevel0,
                healthLevel1,
                healthLevel2,
                healthLevel3,
                healthLevel4,
                healthLevel5
            };
        }
    }
}