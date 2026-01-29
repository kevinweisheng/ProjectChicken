using UnityEngine;

namespace ProjectChicken.Systems
{
    /// <summary>
    /// 特殊鸡配置：统一管理所有特殊鸡的属性
    /// </summary>
    [CreateAssetMenu(fileName = "SpecialChickenConfig", menuName = "ProjectChicken/Special Chicken Config")]
    public class SpecialChickenConfig : ScriptableObject
    {
        [System.Serializable]
        public class GoldenChickenSettings
        {
            [Header("金鸡配置")]
            [Tooltip("是否启用金鸡生成")]
            public bool enabled = false;
            
            [Tooltip("金鸡生成权重（用于权重随机选择，数值越大被选中的概率越高）")]
            public float weight = 1f;
            
            [Tooltip("金鸡生成概率（0-1），由技能系统控制，这里仅用于显示")]
            [Range(0f, 1f)]
            public float spawnRate = 0.05f;
        }

        [System.Serializable]
        public class LightningChickenSettings
        {
            [Header("闪电鸡配置")]
            [Tooltip("是否启用闪电鸡生成")]
            public bool enabled = false;
            
            [Tooltip("闪电鸡生成权重（用于权重随机选择，数值越大被选中的概率越高）")]
            public float weight = 1f;
            
            [Tooltip("闪电鸡下蛋时触发闪电链的概率（0-1）")]
            [Range(0f, 1f)]
            public float eggTriggerChance = 0.5f;
            
            [Tooltip("闪电链最大目标数量（包括起始目标）")]
            public int maxTargets = 5;
            
            [Tooltip("闪电链每次跳跃后的概率衰减系数（0-1）")]
            [Range(0f, 1f)]
            public float chainDecay = 0.5f;
            
            [Tooltip("闪电链每次跳跃造成的伤害比例（相对于基础伤害）")]
            public float damagePercent = 0.5f;
            
            [Tooltip("闪电链搜索范围（世界单位）")]
            public float range = 5f;
        }

        [System.Serializable]
        public class BombChickenSettings
        {
            [Header("炸弹鸡配置")]
            [Tooltip("是否启用炸弹鸡生成")]
            public bool enabled = false;
            
            [Tooltip("炸弹鸡生成权重（用于权重随机选择，数值越大被选中的概率越高）")]
            public float weight = 1f;
            
            [Tooltip("炸弹鸡下蛋时触发爆炸的概率（0-1）")]
            [Range(0f, 1f)]
            public float eggTriggerChance = 1.0f;
            
            [Tooltip("爆炸范围（世界单位）")]
            public float explosionRadius = 3f;
            
            [Tooltip("爆炸伤害比例（相对于基础伤害，例如 1.5 表示 150%）")]
            public float damageMultiplier = 1.5f;
            
            [Tooltip("爆炸伤害衰减（距离越远伤害越低，0-1，0表示无衰减，1表示完全衰减）")]
            [Range(0f, 1f)]
            public float damageFalloff = 0.5f;
        }

        [System.Serializable]
        public class BasketballChickenSettings
        {
            [Header("篮球鸡配置")]
            [Tooltip("是否启用篮球鸡生成")]
            public bool enabled = false;

            [Tooltip("篮球鸡生成权重（用于权重随机选择）")]
            public float weight = 1f;

            [Tooltip("篮球鸡下蛋时发射篮球的概率（0-1）")]
            [Range(0f, 1f)]
            public float eggTriggerChance = 1f;

            [Tooltip("篮球飞行速度（世界单位/秒）")]
            public float speed = 8f;

            [Tooltip("篮球伤害比例（相对于基础伤害）")]
            public float damageMultiplier = 1f;

            [Tooltip("篮球与边界反弹次数上限，超过后消失")]
            public int maxBounces = 5;
        }

        [System.Serializable]
        public class BlackHoleChickenSettings
        {
            [Header("黑洞鸡配置")]
            [Tooltip("是否启用黑洞鸡生成")]
            public bool enabled = false;

            [Tooltip("黑洞鸡生成权重（用于权重随机选择）")]
            public float weight = 1f;

            [Tooltip("黑洞鸡下蛋时生成黑洞的概率（0-1）")]
            [Range(0f, 1f)]
            public float eggTriggerChance = 1f;

            [Tooltip("黑洞吸引范围（世界单位）")]
            public float radius = 4f;

            [Tooltip("黑洞持续时间（秒）")]
            public float duration = 3f;

            [Tooltip("吸引力度（施加给鸡的力的大小，不造成伤害）")]
            public float pullForce = 15f;
        }

        [Header("特殊鸡全局设置")]
        [Tooltip("是否启用特殊鸡生成系统")]
        public bool enableSpecialChickens = true;

        [Header("各特殊鸡配置")]
        public GoldenChickenSettings goldenChicken = new GoldenChickenSettings();
        public LightningChickenSettings lightningChicken = new LightningChickenSettings();
        public BombChickenSettings bombChicken = new BombChickenSettings();
        public BasketballChickenSettings basketballChicken = new BasketballChickenSettings();
        public BlackHoleChickenSettings blackHoleChicken = new BlackHoleChickenSettings();

        /// <summary>
        /// 获取指定类型的权重
        /// </summary>
        public float GetWeight(SpecialChickenType type)
        {
            switch (type)
            {
                case SpecialChickenType.Golden:
                    return goldenChicken.enabled ? goldenChicken.weight : 0f;
                case SpecialChickenType.Lightning:
                    return lightningChicken.enabled ? lightningChicken.weight : 0f;
                case SpecialChickenType.Bomb:
                    return bombChicken.enabled ? bombChicken.weight : 0f;
                case SpecialChickenType.Basketball:
                    return basketballChicken.enabled ? basketballChicken.weight : 0f;
                case SpecialChickenType.BlackHole:
                    return blackHoleChicken.enabled ? blackHoleChicken.weight : 0f;
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// 检查指定类型是否启用
        /// </summary>
        public bool IsEnabled(SpecialChickenType type)
        {
            if (!enableSpecialChickens)
            {
                return false;
            }

            switch (type)
            {
                case SpecialChickenType.Golden:
                    return goldenChicken.enabled;
                case SpecialChickenType.Lightning:
                    return lightningChicken.enabled;
                case SpecialChickenType.Bomb:
                    return bombChicken.enabled;
                case SpecialChickenType.Basketball:
                    return basketballChicken.enabled;
                case SpecialChickenType.BlackHole:
                    return blackHoleChicken.enabled;
                default:
                    return false;
            }
        }
    }
}
