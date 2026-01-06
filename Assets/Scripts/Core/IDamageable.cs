using UnityEngine;

namespace ProjectChicken.Core
{
    /// <summary>
    /// 伤害接口：所有可受到伤害的对象都应实现此接口
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// 受到伤害
        /// </summary>
        /// <param name="amount">伤害值</param>
        void TakeDamage(float amount);
    }
}

