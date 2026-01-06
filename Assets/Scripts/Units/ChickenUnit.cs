using UnityEngine;
using System;
using ProjectChicken.Core;

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
        private float wanderTimer = 0f; // 游荡计时器
        private Camera mainCamera; // 主摄像机（用于边界检测）

        /// <summary>
        /// 是否为肥鸡状态（只读属性，供外部查询）
        /// </summary>
        public bool IsFat => isFat;
        private Rigidbody2D rb; // 刚体组件
        private SpriteRenderer spriteRenderer; // 精灵渲染器
        private Color originalColor; // 原始颜色（用于受击反馈）
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
            
            // 保存原始颜色
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }

            // 初始化血量
            currentHP = maxHP;
        }

        private void Update()
        {
            // 如果已经产出（不再是肥鸡），停止所有行为
            if (!isFat)
            {
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

            // 停止受击判定（通过移除 Collider 或改变 Layer 实现）
            // 这里我们通过 isFat 标志来控制，TakeDamage 中已经检查了
        }
    }
}

