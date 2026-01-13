using UnityEngine;
using System;
using System.Collections;
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

        [Header("瘦鸡预制体（可选）")]
        [SerializeField] private GameObject thinChickenPrefab; // 瘦鸡预制体（如果设置，产出时会替换为这个预制体）
        [SerializeField] private bool useThinChickenPrefab = false; // 是否使用瘦鸡预制体（如果为 false，则使用代码方式改变外观）
        
        private bool wasReplacedByPrefab = false; // 标记当前对象是否是通过预制体替换生成的瘦鸡

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

            // 立即开始移动（不等待第一个 wanderInterval）
            if (isFat && rb != null)
            {
                ChangeWanderDirection();
                // 随机化每只鸡的游荡计时器，让它们不会同时改变方向
                wanderTimer = UnityEngine.Random.Range(0f, wanderInterval);
            }
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
        /// 检查并调整边界：如果鸡超出场地范围，改变移动方向使其回到场地内
        /// </summary>
        private void CheckAndAdjustBoundary()
        {
            if (rb == null) return;

            // 优先使用场地边界，如果没有场地则使用屏幕边界（向后兼容）
            float minX, maxX, minY, maxY;

            if (PlayArea.Instance != null)
            {
                // 使用场地边界
                minX = PlayArea.Instance.MinX + boundaryPadding;
                maxX = PlayArea.Instance.MaxX - boundaryPadding;
                minY = PlayArea.Instance.MinY + boundaryPadding;
                maxY = PlayArea.Instance.MaxY - boundaryPadding;
            }
            else if (mainCamera != null)
            {
                // 回退到屏幕边界（向后兼容）
                Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0f, 0f, mainCamera.nearClipPlane + 10f));
                Vector3 topRight = mainCamera.ViewportToWorldPoint(new Vector3(1f, 1f, mainCamera.nearClipPlane + 10f));
                minX = bottomLeft.x + boundaryPadding;
                maxX = topRight.x - boundaryPadding;
                minY = bottomLeft.y + boundaryPadding;
                maxY = topRight.y - boundaryPadding;
            }
            else
            {
                return; // 没有可用的边界
            }

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

            // 保存当前速度（用于瘦鸡继承移动行为）
            Vector2 currentVelocity = Vector2.zero;
            if (rb != null)
            {
                currentVelocity = rb.linearVelocity;
            }

            // 视觉变化：根据配置选择使用预制体或代码方式
            if (useThinChickenPrefab && thinChickenPrefab != null)
            {
                // 使用瘦鸡预制体替换当前对象（传递当前速度）
                ReplaceWithThinChicken(currentVelocity);
            }
            else
            {
                // 使用代码方式改变外观（原有逻辑）
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.gray;
                }
                transform.localScale = transform.localScale * 0.5f; // 缩小到一半
                
                // 继承速度，但稍微减慢，然后逐渐停止
                if (rb != null && currentVelocity != Vector2.zero)
                {
                    rb.linearVelocity = currentVelocity * 0.7f;
                    StartCoroutine(GraduallyStopMovement(rb));
                }
                else if (rb != null)
                {
                    // 如果没有速度，直接停止
                    rb.linearVelocity = Vector2.zero;
                }
            }

            // 重置恢复计时器
            recoveryTimer = 0f;

            // 停止受击判定（通过移除 Collider 或改变 Layer 实现）
            // 这里我们通过 isFat 标志来控制，TakeDamage 中已经检查了
        }

        /// <summary>
        /// 使用瘦鸡预制体替换当前鸡对象
        /// </summary>
        /// <param name="parentVelocity">父对象（胖鸡）的速度</param>
        private void ReplaceWithThinChicken(Vector2 parentVelocity)
        {
            if (thinChickenPrefab == null)
            {
                Debug.LogWarning("ChickenUnit: thinChickenPrefab 为空，无法替换！使用代码方式改变外观。", this);
                // 回退到代码方式
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.gray;
                }
                transform.localScale = transform.localScale * 0.5f;
                
                // 继承速度并逐渐停止
                if (rb != null)
                {
                    if (parentVelocity != Vector2.zero)
                    {
                        rb.linearVelocity = parentVelocity * 0.7f;
                        StartCoroutine(GraduallyStopMovement(rb));
                    }
                    else
                    {
                        rb.linearVelocity = Vector2.zero;
                    }
                }
                return;
            }

            // 保存当前状态
            Vector3 currentPosition = transform.position;
            bool wasGolden = isGolden;

            // 实例化瘦鸡预制体
            GameObject thinChicken = Instantiate(thinChickenPrefab, currentPosition, transform.rotation);
            
            if (thinChicken == null)
            {
                Debug.LogError("ChickenUnit: 瘦鸡预制体实例化失败！回退到代码方式。", this);
                // 回退到代码方式
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.gray;
                }
                transform.localScale = transform.localScale * 0.5f;
                if (rb != null)
                {
                    if (parentVelocity != Vector2.zero)
                    {
                        rb.linearVelocity = parentVelocity * 0.7f;
                        StartCoroutine(GraduallyStopMovement(rb));
                    }
                    else
                    {
                        rb.linearVelocity = Vector2.zero;
                    }
                }
                return;
            }

            // 如果瘦鸡预制体有 ChickenUnit 组件，需要同步状态
            ChickenUnit thinChickenUnit = thinChicken.GetComponent<ChickenUnit>();
            if (thinChickenUnit != null)
            {
                // 确保瘦鸡状态正确（通过反射设置 isFat 为 false）
                var isFatField = typeof(ChickenUnit).GetField("isFat", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (isFatField != null)
                {
                    isFatField.SetValue(thinChickenUnit, false);
                }
                
                // 标记瘦鸡是通过预制体替换生成的（用于恢复时替换回肥鸡）
                var wasReplacedField = typeof(ChickenUnit).GetField("wasReplacedByPrefab", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (wasReplacedField != null)
                {
                    wasReplacedField.SetValue(thinChickenUnit, true);
                }
                
                // 同步金鸡状态
                thinChickenUnit.SetGolden(wasGolden);
                
                // 继承胖鸡的移动速度，让瘦鸡延续移动行为
                Rigidbody2D thinRb = thinChicken.GetComponent<Rigidbody2D>();
                if (thinRb != null && parentVelocity != Vector2.zero)
                {
                    // 继承速度，但稍微减慢（例如减少到原来的 70%）
                    thinRb.linearVelocity = parentVelocity * 0.7f;
                    
                    // 在瘦鸡对象上启动协程，让瘦鸡逐渐减速并停止
                    thinChickenUnit.StartCoroutine(thinChickenUnit.GraduallyStopMovement(thinRb));
                }
                else if (thinRb != null)
                {
                    // 如果没有速度，直接停止
                    thinRb.linearVelocity = Vector2.zero;
                }
                
                // 将瘦鸡添加到生成器列表中，以便清理时能够找到它
                if (ChickenSpawner.Instance != null)
                {
                    // 使用反射访问私有字段 spawnedChickens
                    var spawnerType = typeof(ChickenSpawner);
                    var spawnedChickensField = spawnerType.GetField("spawnedChickens", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (spawnedChickensField != null)
                    {
                        var spawnedChickensList = spawnedChickensField.GetValue(ChickenSpawner.Instance) as System.Collections.Generic.List<ChickenUnit>;
                        if (spawnedChickensList != null && !spawnedChickensList.Contains(thinChickenUnit))
                        {
                            spawnedChickensList.Add(thinChickenUnit);
                        }
                    }
                }
                
                Debug.Log($"ChickenUnit: 成功替换为瘦鸡预制体，位置：{currentPosition}", this);
            }
            else
            {
                Debug.LogWarning("ChickenUnit: 瘦鸡预制体没有 ChickenUnit 组件！瘦鸡可能无法正常工作。回退到代码方式。", this);
                // 如果瘦鸡预制体没有 ChickenUnit 组件，销毁它并回退到代码方式
                Destroy(thinChicken);
                
                // 回退到代码方式
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.gray;
                }
                transform.localScale = transform.localScale * 0.5f;
                if (rb != null)
                {
                    if (parentVelocity != Vector2.zero)
                    {
                        rb.linearVelocity = parentVelocity * 0.7f;
                        StartCoroutine(GraduallyStopMovement(rb));
                    }
                    else
                    {
                        rb.linearVelocity = Vector2.zero;
                    }
                }
                return; // 不销毁当前对象，因为已经回退到代码方式
            }

            // 销毁当前对象（在最后销毁，确保瘦鸡已经创建）
            Destroy(gameObject);
        }

        /// <summary>
        /// 协程：让瘦鸡逐渐减速并停止移动
        /// </summary>
        /// <param name="thinRb">瘦鸡的刚体组件</param>
        public System.Collections.IEnumerator GraduallyStopMovement(Rigidbody2D thinRb)
        {
            if (thinRb == null) yield break;

            float decelerationTime = 1.5f; // 减速时间（秒）
            float elapsedTime = 0f;
            Vector2 initialVelocity = thinRb.linearVelocity;

            while (elapsedTime < decelerationTime && thinRb != null)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / decelerationTime;
                
                // 使用平滑插值逐渐减速
                thinRb.linearVelocity = Vector2.Lerp(initialVelocity, Vector2.zero, t);
                
                yield return null;
            }

            // 确保最终停止
            if (thinRb != null)
            {
                thinRb.linearVelocity = Vector2.zero;
            }
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

                // 如果当前是通过预制体替换生成的瘦鸡，需要替换回肥鸡预制体
                if (wasReplacedByPrefab && ChickenSpawner.Instance != null)
                {
                    // 从 ChickenSpawner 获取肥鸡预制体
                    var spawnerType = typeof(ChickenSpawner);
                    var chickenPrefabField = spawnerType.GetField("chickenPrefab", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (chickenPrefabField != null)
                    {
                        ChickenUnit fatChickenPrefab = chickenPrefabField.GetValue(ChickenSpawner.Instance) as ChickenUnit;
                        
                        if (fatChickenPrefab != null)
                        {
                            // 保存当前状态
                            Vector3 currentPosition = transform.position;
                            bool wasGolden = isGolden;
                            
                            // 实例化肥鸡预制体
                            ChickenUnit fatChicken = Instantiate(fatChickenPrefab, currentPosition, transform.rotation);
                            
                            if (fatChicken != null)
                            {
                                // 确保肥鸡状态正确
                                var isFatField = typeof(ChickenUnit).GetField("isFat", 
                                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                if (isFatField != null)
                                {
                                    isFatField.SetValue(fatChicken, true);
                                }
                                
                                // 同步金鸡状态
                                fatChicken.SetGolden(wasGolden);
                                
                                // 恢复血量
                                var currentHPField = typeof(ChickenUnit).GetField("currentHP", 
                                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                var maxHPField = typeof(ChickenUnit).GetField("maxHP", 
                                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                
                                if (currentHPField != null && maxHPField != null)
                                {
                                    float maxHP = (float)maxHPField.GetValue(fatChicken);
                                    currentHPField.SetValue(fatChicken, maxHP);
                                }
                                
                                // 将肥鸡添加到生成器列表中，并从列表中移除瘦鸡
                                var spawnedChickensField = spawnerType.GetField("spawnedChickens", 
                                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                
                                if (spawnedChickensField != null)
                                {
                                    var spawnedChickensList = spawnedChickensField.GetValue(ChickenSpawner.Instance) as System.Collections.Generic.List<ChickenUnit>;
                                    if (spawnedChickensList != null)
                                    {
                                        // 从生成器列表中移除瘦鸡
                                        if (spawnedChickensList.Contains(this))
                                        {
                                            spawnedChickensList.Remove(this);
                                        }
                                        
                                        // 添加肥鸡到生成器列表
                                        if (!spawnedChickensList.Contains(fatChicken))
                                        {
                                            spawnedChickensList.Add(fatChicken);
                                        }
                                    }
                                }
                                
                                Debug.Log($"ChickenUnit: 瘦鸡在位置 {currentPosition} 恢复为肥鸡预制体", this);
                                
                                // 销毁瘦鸡对象
                                Destroy(gameObject);
                                return; // 提前返回，因为对象已被销毁
                            }
                        }
                    }
                    
                    // 如果替换失败，回退到代码方式恢复视觉
                    Debug.LogWarning("ChickenUnit: 无法获取肥鸡预制体，回退到代码方式恢复视觉", this);
                }
                
                // 使用代码方式恢复视觉（颜色和大小）
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

