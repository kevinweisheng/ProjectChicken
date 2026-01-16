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
            // 计算鸡蛋价值
            int eggValue = 1; // 默认普通蛋价值为1
            if (isGoldenEgg && UpgradeManager.Instance != null)
            {
                eggValue = UpgradeManager.Instance.GoldenEggMultiplier;
            }

            // 检查是否触发双倍产出
            bool isDoubleProduction = false;
            if (UpgradeManager.Instance != null && UpgradeManager.Instance.IsDoubleProductionUnlocked)
            {
                float doubleProductionChance = UpgradeManager.Instance.DoubleProductionChance;
                if (doubleProductionChance > 0f && UnityEngine.Random.value < doubleProductionChance)
                {
                    isDoubleProduction = true;
                    eggValue *= 2;
                    Debug.Log($"EggProjectile: 触发双倍产出！鸡蛋价值翻倍：{eggValue}", this);
                }
            }

            // 调用资源管理器增加鸡蛋（制造"飞到了才算数"的延迟满足感）
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.AddEgg(eggValue);
                string eggType = isGoldenEgg ? "金蛋" : "普通蛋";
                string doubleText = isDoubleProduction ? "（双倍产出）" : "";
                Debug.Log($"EggProjectile: {eggType}到达，获得 {eggValue} 个鸡蛋{doubleText}", this);
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
                    Debug.Log($"EggProjectile: 触发引力波！增加 {extensionAmount} 秒回合时间", this);
                }
            }

            // 销毁自身
            Destroy(gameObject);
        }
    }
}

