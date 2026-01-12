using UnityEngine;
using System;
using ProjectChicken.Core;
using ProjectChicken.Systems;

namespace ProjectChicken.Units
{
    /// <summary>
    /// 鸡单位：实现 IDamageable 接口，具有游荡、受击、产出等行为
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class ChickenUnit : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHP = 100f; // 最大血量
        [SerializeField] private float wanderInterval = 2f; // 游荡方向改变间隔（秒）
        [SerializeField] private float wanderSpeed = 2f; // 游荡速度
        [SerializeField] private float hitFlashDuration = 0.1f; // 受击闪烁持续时间
        [SerializeField] private float boundaryPadding = 0.5f; // 边界内边距（避免鸡紧贴屏幕边缘）

        private float currentHP; // 当前血量
        private bool isFat = true; // 是否为肥鸡状态（默认为 true）
        private bool isGolden = false; // 是否为金鸡（默认为 false）
        private float wanderTimer = 0f; // 游荡计时器
        private Camera mainCamera; // 主摄像机（用于边界检测）

        // 恢复相关计时器
        private float recoveryInterval = 1f; // 固定恢复间隔（1秒）
        private float recoveryTimer = 0f; // 恢复计时器

        /// <summary>
        /// 是否为肥鸡状态（只读属性，供外部查询）
        /// </summary>
        public bool IsFat => isFat;

        /// <summary>
        /// 是否为金鸡（只读属性，供外部查询）
        /// </summary>
        public bool IsGolden => isGolden;

        /// <summary>
        /// 设置是否为金鸡（供生成器调用）
        /// </summary>
        /// <param name="golden">是否为金鸡</param>
        public void SetGolden(bool golden)
        {
            isGolden = golden;
            // 如果是金鸡，改变视觉外观（例如改变颜色为金色）
            if (spriteRenderer != null)
            {
                if (golden)
                {
                    // 保存原始颜色，然后设置为金色
                    originalColor = Color.yellow; // 金鸡使用金色作为"原始"颜色
                    spriteRenderer.color = Color.yellow;
                }
                // 如果不是金鸡，保持原始颜色（在 Start 中已设置）
            }
        }
        private Rigidbody2D rb; // 刚体组件
        private SpriteRenderer spriteRenderer; // 精灵渲染器
        private Color originalColor; // 原始颜色（用于受击反馈）
        private Vector3 originalScale; // 原始大小（用于恢复）
        private float hitFlashTimer = 0f; // 受击闪烁计时器

        // 静态事件：当鸡产出时触发（传递位置信息）
        public static event Action<Vector3> OnChickenProduct;

        private void Start()
        {
            // 初始化组件
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            
            // 获取主摄像机
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
            
            // 保存原始颜色和大小
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
            originalScale = transform.localScale;

            // 初始化血量
            currentHP = maxHP;
        }

        private void Update()
        {
            // 如果已经产出（不再是肥鸡），尝试恢复
            if (!isFat)
            {
                // 恢复计时器：每1秒尝试一次恢复
                recoveryTimer += Time.deltaTime;
                if (recoveryTimer >= recoveryInterval)
                {
                    recoveryTimer = 0f;
                    TryRecover();
                }
                // 瘦鸡状态下不执行其他行为
                return;
            }

            // 边界检测：如果超出屏幕范围，改变移动方向
            CheckAndAdjustBoundary();

            // 游荡行为：每隔几秒随机改变移动方向
            wanderTimer += Time.deltaTime;
            if (wanderTimer >= wanderInterval)
            {
                wanderTimer = 0f;
                ChangeWanderDirection();
            }

            // 受击闪烁效果
            UpdateHitFlash();
        }

        /// <summary>
        /// 改变游荡方向：随机设置线性速度
        /// </summary>
        private void ChangeWanderDirection()
        {
            if (rb == null) return;

            // 生成随机方向
            Vector2 randomDirection = new Vector2(
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-1f, 1f)
            ).normalized;

            // 设置线性速度（Unity 6 使用 linearVelocity）
            rb.linearVelocity = randomDirection * wanderSpeed;
        }

        /// <summary>
        /// 检查并调整边界：如果鸡超出屏幕范围，改变移动方向使其回到屏幕内
        /// </summary>
        private void CheckAndAdjustBoundary()
        {
            if (mainCamera == null || rb == null) return;

            // 获取屏幕边界（世界坐标）
            Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0f, 0f, mainCamera.nearClipPlane + 10f));
            Vector3 topRight = mainCamera.ViewportToWorldPoint(new Vector3(1f, 1f, mainCamera.nearClipPlane + 10f));

            // 计算有效活动范围（减去内边距）
            float minX = bottomLeft.x + boundaryPadding;
            float maxX = topRight.x - boundaryPadding;
            float minY = bottomLeft.y + boundaryPadding;
            float maxY = topRight.y - boundaryPadding;

            Vector3 currentPos = transform.position;
            Vector2 currentVelocity = rb.linearVelocity;
            bool needAdjust = false;
            Vector2 newDirection = currentVelocity.normalized;

            // 检查 X 轴边界
            if (currentPos.x < minX)
            {
                // 超出左边界，强制向右移动
                newDirection.x = Mathf.Abs(newDirection.x); // 确保向右
                needAdjust = true;
            }
            else if (currentPos.x > maxX)
            {
                // 超出右边界，强制向左移动
                newDirection.x = -Mathf.Abs(newDirection.x); // 确保向左
                needAdjust = true;
            }

            // 检查 Y 轴边界
            if (currentPos.y < minY)
            {
                // 超出下边界，强制向上移动
                newDirection.y = Mathf.Abs(newDirection.y); // 确保向上
                needAdjust = true;
            }
            else if (currentPos.y > maxY)
            {
                // 超出上边界，强制向下移动
                newDirection.y = -Mathf.Abs(newDirection.y); // 确保向下
                needAdjust = true;
            }

            // 如果需要调整，改变移动方向
            if (needAdjust)
            {
                newDirection.Normalize();
                rb.linearVelocity = newDirection * wanderSpeed;
            }
        }

        /// <summary>
        /// 受到伤害（实现 IDamageable 接口）
        /// </summary>
        /// <param name="amount">伤害值</param>
        public void TakeDamage(float amount)
        {
            // 如果已经产出，不再受到伤害
            if (!isFat)
            {
                return;
            }

            // 扣血
            currentHP -= amount;

            // 播放受击反馈（Sprite 变红）
            TriggerHitFlash();

            // 检查是否死亡并产出
            if (currentHP <= 0f && isFat)
            {
                OnChickenProducted();
            }
        }

        /// <summary>
        /// 触发受击闪烁效果
        /// </summary>
        private void TriggerHitFlash()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.red;
                hitFlashTimer = hitFlashDuration;
            }
        }

        /// <summary>
        /// 更新受击闪烁效果
        /// </summary>
        private void UpdateHitFlash()
        {
            if (hitFlashTimer > 0f)
            {
                hitFlashTimer -= Time.deltaTime;
                if (hitFlashTimer <= 0f)
                {
                    // 恢复原始颜色
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.color = originalColor;
                    }
                }
            }
        }

        /// <summary>
        /// 鸡产出：触发事件，切换状态，改变视觉
        /// </summary>
        private void OnChickenProducted()
        {
            // 触发静态事件，传递位置信息
            OnChickenProduct?.Invoke(transform.position);

            // 切换状态为已产出
            isFat = false;

            // 视觉变化：变为灰色并缩小
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.gray;
            }
            transform.localScale = transform.localScale * 0.5f; // 缩小到一半

            // 停止移动
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            // 重置恢复计时器
            recoveryTimer = 0f;

            // 停止受击判定（通过移除 Collider 或改变 Layer 实现）
            // 这里我们通过 isFat 标志来控制，TakeDamage 中已经检查了
        }

        /// <summary>
        /// 尝试恢复：从瘦鸡状态恢复为肥鸡状态
        /// </summary>
        private void TryRecover()
        {
            // 获取恢复几率
            float chance = 0.5f; // 默认50%
            if (UpgradeManager.Instance != null)
            {
                chance = UpgradeManager.Instance.RecoveryChance;
            }

            // 随机判定是否恢复成功
            if (UnityEngine.Random.value < chance)
            {
                // 恢复成功：变回肥鸡状态
                isFat = true;

                // 恢复视觉（颜色和大小）
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = originalColor;
                }
                transform.localScale = originalScale; // 恢复原始大小

                // 恢复血量
                currentHP = maxHP;

                // 重置游荡计时器
                wanderTimer = 0f;

                Debug.Log($"ChickenUnit: 鸡在位置 {transform.position} 恢复为肥鸡状态", this);

                // 分裂检查：如果解锁了分裂能力且可以生成更多鸡
                if (UpgradeManager.Instance != null && UpgradeManager.Instance.IsMitosisUnlocked)
                {
                    if (ChickenSpawner.Instance != null && ChickenSpawner.Instance.CanSpawnMore())
                    {
                        // 获取分裂几率
                        float mitosisChance = UpgradeManager.Instance.MitosisChance;

                        // 随机判定是否分裂
                        if (UnityEngine.Random.value < mitosisChance)
                        {
                            // 分裂成功：在当前位置生成一只新鸡
                            ChickenSpawner.Instance.SpawnChickenAt(transform.position);
                            Debug.Log($"ChickenUnit: 鸡在位置 {transform.position} 发生分裂！", this);
                        }
                    }
                }
            }
        }
    }
}

