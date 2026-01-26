using UnityEngine;
using ProjectChicken.Core;

namespace ProjectChicken.Systems
{
    /// <summary>
    /// 鸡蛋投射物：从鸡的位置飞向目标位置，到达后触发资源增加
    /// </summary>
    public class EggProjectile : MonoBehaviour
    {
        [Header("移动配置")]
        [SerializeField] private float moveSpeed = 5f; // 移动速度
        [SerializeField] private float arrivalDistance = 0.1f; // 到达判定距离
        
        [Header("美术素材")]
        [SerializeField] private Sprite normalEggSprite; // 普通蛋的 Sprite（可选，如果不设置则使用默认）
        [SerializeField] private Sprite goldenEggSprite; // 金蛋的 Sprite（可选，如果不设置则使用颜色变化）

        private Vector3 targetPosition; // 目标位置
        private bool isInitialized = false; // 是否已初始化
        private bool isGoldenEgg = false; // 是否为金蛋
        private SpriteRenderer spriteRenderer; // SpriteRenderer 组件缓存

        /// <summary>
        /// 初始化投射物
        /// </summary>
        /// <param name="targetPos">目标位置</param>
        /// <param name="golden">是否为金蛋</param>
        public void Initialize(Vector3 targetPos, bool golden = false)
        {
            targetPosition = targetPos;
            isGoldenEgg = golden;
            isInitialized = true;

            // 获取或缓存 SpriteRenderer 组件
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            // 根据是否为金蛋设置视觉外观
            if (spriteRenderer != null)
            {
                if (golden)
                {
                    // 如果指定了金蛋 Sprite，使用 Sprite 替换
                    if (goldenEggSprite != null)
                    {
                        spriteRenderer.sprite = goldenEggSprite;
                        spriteRenderer.color = Color.white; // 使用 Sprite 时保持白色，显示原始颜色
                    }
                    else
                    {
                        // 如果没有指定金蛋 Sprite，则使用颜色变化（向后兼容）
                        spriteRenderer.color = Color.yellow; // 金色
                    }
                }
                else
                {
                    // 普通蛋：如果指定了普通蛋 Sprite，使用它
                    if (normalEggSprite != null)
                    {
                        spriteRenderer.sprite = normalEggSprite;
                    }
                    spriteRenderer.color = Color.white; // 普通蛋保持白色
                }
            }
        }

        private void Update()
        {
            if (!isInitialized) return;

            // 使用 MoveTowards 平滑飞向目标位置
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            // 检查是否到达目标
            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
            if (distanceToTarget < arrivalDistance)
            {
                OnArrived();
            }
        }

        /// <summary>
        /// 到达目标位置：销毁自身并调用资源增加逻辑
        /// </summary>
        private void OnArrived()
        {
            int eggValue = 1;
            
            if (isGoldenEgg && UpgradeManager.Instance != null)
            {
                // 金蛋：使用金蛋倍率
                eggValue = UpgradeManager.Instance.GoldenEggMultiplier;
            }
            else
            {
                // 普通蛋：每个蛋的价值都是1（下蛋数量由生成多个蛋来实现）
                eggValue = 1;
            }

            // 检查是否触发双倍产出
            if (UpgradeManager.Instance != null && UpgradeManager.Instance.IsDoubleProductionUnlocked)
            {
                float doubleProductionChance = UpgradeManager.Instance.DoubleProductionChance;
                if (doubleProductionChance > 0f && UnityEngine.Random.value < doubleProductionChance)
                {
                    eggValue *= 2;
                }
            }

            // 调用资源管理器增加鸡蛋（制造"飞到了才算数"的延迟满足感）
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.AddEgg(eggValue);
            }
            else
            {
                Debug.LogWarning("EggProjectile: ResourceManager.Instance 为空！无法增加鸡蛋。");
            }

            // 检查是否触发引力波（鸡生蛋时，有概率让回合时间增加）
            if (UpgradeManager.Instance != null && GameManager.Instance != null && UpgradeManager.Instance.IsGravityWaveUnlocked)
            {
                float gravityWaveChance = UpgradeManager.Instance.GravityWaveChance;
                if (gravityWaveChance > 0f && UnityEngine.Random.value < gravityWaveChance)
                {
                    float extensionAmount = UpgradeManager.Instance.GravityWaveTimeExtension;
                    GameManager.Instance.ExtendSessionTime(extensionAmount);
                }
            }

            // 销毁自身
            Destroy(gameObject);
        }

        /// <summary>
        /// 根据当前场地等级获取普通鸡下蛋数量
        /// </summary>
        /// <returns>普通鸡下蛋数量</returns>
        private int GetNormalEggCountByStage()
        {
            // 从 GameManager 获取配置
            EggCountConfig config = null;
            if (GameManager.Instance != null)
            {
                // 使用反射获取 GameManager 的私有字段 eggCountConfig
                var configField = typeof(GameManager).GetField("eggCountConfig", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (configField != null)
                {
                    config = configField.GetValue(GameManager.Instance) as EggCountConfig;
                    if (config == null)
                    {
                        Debug.LogWarning("EggProjectile: 反射获取到字段，但字段值为 null。请在 GameManager 的 Inspector 中配置 eggCountConfig", this);
                    }
                }
                else
                {
                    Debug.LogError("EggProjectile: 无法通过反射找到 eggCountConfig 字段！", this);
                }
            }
            else
            {
                Debug.LogWarning("EggProjectile: GameManager.Instance 为空", this);
            }

            if (config == null)
            {
                Debug.LogWarning("EggProjectile: eggCountConfig 未配置，使用默认下蛋数量 1", this);
                return 1;
            }

            // 获取当前场地等级（优先从 AreaUpgradeManager 获取，如果没有则从 FractalUniverseManager 获取）
            int stageLevel = 0; // 默认等级0
            string levelSource = "默认值";
            
            if (AreaUpgradeManager.Instance != null)
            {
                if (AreaUpgradeManager.Instance.CurrentAreaData != null)
                {
                    // 使用当前场地的鸡生命值等级作为下蛋数量等级（因为它们是对应的）
                    stageLevel = AreaUpgradeManager.Instance.CurrentAreaData.ChickenHealthLevel;
                    levelSource = $"AreaUpgradeManager (AreaData: {AreaUpgradeManager.Instance.CurrentAreaData.AreaName}, Level: {AreaUpgradeManager.Instance.CurrentAreaLevel})";
                }
                else
                {
                    Debug.LogWarning("EggProjectile: AreaUpgradeManager.Instance 存在，但 CurrentAreaData 为 null", this);
                }
            }
            else if (FractalUniverseManager.Instance != null && FractalUniverseManager.Instance.CurrentStage != null)
            {
                // 备用方案：如果没有 AreaUpgradeManager，使用 FractalUniverseManager
                stageLevel = FractalUniverseManager.Instance.CurrentStage.ChickenHealthLevel;
                levelSource = $"FractalUniverseManager (Stage: {FractalUniverseManager.Instance.CurrentStage.StageName})";
            }
            else
            {
                Debug.LogWarning("EggProjectile: 无法获取当前场地/阶段信息，使用默认等级 0", this);
            }

            int eggCount = config.GetEggCountByLevel(stageLevel);
            
            return eggCount;
        }
    }
}

