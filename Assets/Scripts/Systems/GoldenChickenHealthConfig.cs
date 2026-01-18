using UnityEngine;

namespace ProjectChicken.Systems
{
    /// <summary>
    /// 金鸡生命值配置：定义6个等级的生命值数值
    /// 每个场景（阶段）对应一个等级的生命值
    /// </summary>
    [CreateAssetMenu(fileName = "GoldenChickenHealthConfig", menuName = "Project Chicken/Golden Chicken Health Config", order = 2)]
    public class GoldenChickenHealthConfig : ScriptableObject
    {
        [Header("金鸡生命值等级配置（6个等级）")]
        [Tooltip("等级0的生命值（第1个场景）")]
        [SerializeField] private float healthLevel0 = 75f;
        
        [Tooltip("等级1的生命值（第2个场景）")]
        [SerializeField] private float healthLevel1 = 150f;
        
        [Tooltip("等级2的生命值（第3个场景）")]
        [SerializeField] private float healthLevel2 = 225f;
        
        [Tooltip("等级3的生命值（第4个场景）")]
        [SerializeField] private float healthLevel3 = 300f;
        
        [Tooltip("等级4的生命值（第5个场景）")]
        [SerializeField] private float healthLevel4 = 375f;
        
        [Tooltip("等级5的生命值（第6个场景）")]
        [SerializeField] private float healthLevel5 = 450f;

        /// <summary>
        /// 根据等级获取金鸡生命值（0-5对应6个等级）
        /// </summary>
        /// <param name="level">生命值等级（0-5）</param>
        /// <returns>该等级对应的生命值，如果等级超出范围则返回默认值</returns>
        public float GetHealthByLevel(int level)
        {
            switch (level)
            {
                case 0: return healthLevel0;
                case 1: return healthLevel1;
                case 2: return healthLevel2;
                case 3: return healthLevel3;
                case 4: return healthLevel4;
                case 5: return healthLevel5;
                default:
                    Debug.LogWarning($"GoldenChickenHealthConfig: 无效的生命值等级 {level}，使用默认值 150", this);
                    return 150f;
            }
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
