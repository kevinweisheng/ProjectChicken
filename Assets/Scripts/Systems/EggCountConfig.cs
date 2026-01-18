using UnityEngine;

namespace ProjectChicken.Systems
{
    /// <summary>
    /// 普通鸡下蛋数量配置：为每个场地等级配置普通鸡下蛋的数量
    /// </summary>
    [CreateAssetMenu(fileName = "EggCountConfig", menuName = "Project Chicken/Egg Count Config", order = 3)]
    public class EggCountConfig : ScriptableObject
    {
        [Header("普通鸡下蛋数量配置（6个等级）")]
        [Tooltip("等级0的下蛋数量（第1个场景）")]
        [SerializeField] private int eggCountLevel0 = 1;
        
        [Tooltip("等级1的下蛋数量（第2个场景）")]
        [SerializeField] private int eggCountLevel1 = 1;
        
        [Tooltip("等级2的下蛋数量（第3个场景）")]
        [SerializeField] private int eggCountLevel2 = 2;
        
        [Tooltip("等级3的下蛋数量（第4个场景）")]
        [SerializeField] private int eggCountLevel3 = 2;
        
        [Tooltip("等级4的下蛋数量（第5个场景）")]
        [SerializeField] private int eggCountLevel4 = 3;
        
        [Tooltip("等级5的下蛋数量（第6个场景）")]
        [SerializeField] private int eggCountLevel5 = 3;

        /// <summary>
        /// 根据等级获取普通鸡下蛋数量
        /// </summary>
        /// <param name="level">场地等级（0-5）</param>
        /// <returns>该等级的下蛋数量</returns>
        public int GetEggCountByLevel(int level)
        {
            switch (level)
            {
                case 0: return eggCountLevel0;
                case 1: return eggCountLevel1;
                case 2: return eggCountLevel2;
                case 3: return eggCountLevel3;
                case 4: return eggCountLevel4;
                case 5: return eggCountLevel5;
                default:
                    Debug.LogWarning($"EggCountConfig: 无效的等级 {level}，返回默认值 1", this);
                    return 1;
            }
        }

        /// <summary>
        /// 获取所有等级的下蛋数量（用于调试）
        /// </summary>
        /// <returns>所有等级的下蛋数量数组</returns>
        public int[] GetAllEggCounts()
        {
            return new int[]
            {
                eggCountLevel0,
                eggCountLevel1,
                eggCountLevel2,
                eggCountLevel3,
                eggCountLevel4,
                eggCountLevel5
            };
        }
    }
}
