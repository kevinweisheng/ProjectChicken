using UnityEngine;
using System;
using System.Collections;
using ProjectChicken.Core;
using ProjectChicken.Systems;
using Spine.Unity;

namespace ProjectChicken.Units
{
    /// <summary>
    /// 鸡单位：实现 IDamageable 接口，具有游荡、受击、产出等行为
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class ChickenUnit : MonoBehaviour, IDamageable
    {
        [Header("系统设置")]
        [Tooltip("是否使用充能系统（true=充能系统，false=伤害系统）")]
        [SerializeField] private bool useChargeSystem = true;
        
        [Header("生命值配置")]
        [SerializeField] private float maxHP = 100f; // 最大血量
        
        [Header("充能系统配置")]
        [Tooltip("最大充能值")]
        [SerializeField] private float maxCharge = 100f;
        
        [Tooltip("充能开始衰减前的延迟时间（秒）")]
        [SerializeField] private float decayDelay = 1.0f;
        
        [Tooltip("充能衰减速率（每秒减少的充能值）")]
        [SerializeField] private float decayRate = 5.0f;
        
        [Header("充能槽显示配置")]
        [Tooltip("充能槽显示位置偏移（相对于鸡的位置，Y 轴向上为正）")]
        [SerializeField] private Vector2 chargeBarOffset = new Vector2(0f, 1.2f);
        
        [Tooltip("充能槽宽度（世界单位）")]
        [SerializeField] private float chargeBarWidth = 1f;
        
        [Tooltip("充能槽高度（世界单位）")]
        [SerializeField] private float chargeBarHeight = 0.1f;
        
        [Tooltip("充能槽背景颜色")]
        [SerializeField] private Color chargeBarBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        [Tooltip("充能槽填充颜色")]
        [SerializeField] private Color chargeBarFillColor = new Color(0f, 1f, 0f, 1f);
        
        [Tooltip("充能槽排序顺序（应大于鸡的排序顺序）")]
        [SerializeField] private int chargeBarSortingOrder = 200;
        
        [Tooltip("充能槽排序图层名称")]
        [SerializeField] private string chargeBarSortingLayerName = "Default";
        
        [Header("移动配置")]
        [Tooltip("游荡速度（世界单位/秒）")]
        [SerializeField] private float wanderSpeed = 2f;
        
        [Tooltip("游荡方向改变间隔（秒）")]
        [SerializeField] private float wanderInterval = 2f;
        
        [Tooltip("边界内边距（世界单位，已废弃，保留用于兼容）")]
        [SerializeField] private float boundaryPadding = 0.5f;
        
        [Header("受击效果配置")]
        [SerializeField] private float hitFlashDuration = 0.1f; // 受击闪烁持续时间
        [SerializeField] private Color hitFlashColor = Color.red; // 受击闪烁颜色

        [Header("瘦鸡预制体（可选）")]
        [SerializeField] private GameObject thinChickenPrefab; // 瘦鸡预制体（如果设置，产出时会替换为这个预制体）
        [SerializeField] private bool useThinChickenPrefab = false; // 是否使用瘦鸡预制体（如果为 false，则使用代码方式改变外观）
        
        [Header("Spine 动画（可选）")]
        [SerializeField] private string idleAnimationName = "idle"; // 待机动画名称
        [SerializeField] private string hitAnimationName = "hit"; // 被攻击动画名称（胖鸡）
        [SerializeField] private string spawnAnimationName = "spawn"; // 出现动画名称（胖鸡，瘦鸡变回胖鸡时播放）
        [SerializeField] private bool loopIdleAnimation = true; // 是否循环播放 Idle 动画
        [SerializeField] private string normalSkinName = "default"; // 普通鸡皮肤名称
        [SerializeField] private string goldenSkinName = "golden"; // 黄金鸡皮肤名称
        
        [Header("抚摸序列帧动画（可选）")]
        [Tooltip("是否启用抚摸动画（如果为 false，抚摸时不会播放序列帧动画）")]
        [SerializeField] private bool enablePetAnimation = true;
        
        [Tooltip("抚摸时显示的序列帧 Sprite（按顺序排列）")]
        [SerializeField] private Sprite[] petAnimationSprites = new Sprite[0];
        [Tooltip("每帧的显示时间（秒）")]
        [SerializeField] private float petAnimationFrameTime = 0.1f;
        [Tooltip("序列帧显示位置偏移（相对于鸡的位置，Y 轴向上为正）")]
        [SerializeField] private Vector2 petAnimationOffset = new Vector2(0f, 1f);
        [Tooltip("序列帧的排序顺序（应大于鸡的排序顺序，确保显示在鸡的上方）")]
        [SerializeField] private int petAnimationSortingOrder = 100;
        [Tooltip("序列帧的排序图层名称")]
        [SerializeField] private string petAnimationSortingLayerName = "Default";
        
        [Header("闪电鸡设置（可选）")]
        [Tooltip("是否为闪电鸡（闪电鸡下蛋时有概率触发闪电链）")]
        [SerializeField] private bool isLightningChicken = false;
        [Tooltip("闪电鸡下蛋时触发闪电链的概率（0-1）")]
        [SerializeField, Range(0f, 1f)] private float lightningEggTriggerChance = 0.5f;
        [Tooltip("闪电链最大目标数量（包括起始目标）")]
        [SerializeField] private int lightningMaxTargets = 5;
        [Tooltip("闪电链每次跳跃后的概率衰减系数（0-1）")]
        [SerializeField, Range(0f, 1f)] private float lightningChainDecay = 0.5f;
        [Tooltip("闪电链每次跳跃造成的伤害比例（相对于基础伤害）")]
        [SerializeField] private float lightningDamagePercent = 0.5f;
        [Tooltip("闪电链搜索范围（世界单位）")]
        [SerializeField] private float lightningRange = 5f;

        [Header("下蛋特效（可选）")]
        [Tooltip("下蛋时播放的特效预制体（可以是粒子系统、序列帧动画等）")]
        [SerializeField] private GameObject eggEffectPrefab;
        [Tooltip("特效显示位置偏移（相对于鸡的位置，Y 轴向上为正）")]
        [SerializeField] private Vector2 eggEffectOffset = Vector2.zero;
        [Tooltip("特效持续时间（秒，0 表示使用特效自身的生命周期）")]
        [SerializeField] private float eggEffectDuration = 0f;
        
        [Header("渲染排序（解决重叠闪烁）")]
        [SerializeField] private bool useDynamicSorting = true; // 是否使用动态排序（根据 Y 坐标）
        [SerializeField] private bool useYCoordinateSorting = false; // 是否使用 Y 坐标动态排序（如果为 false，只使用唯一偏移）
        [SerializeField] private int baseSortingOrder = 0; // 基础排序顺序（应大于背景的 sortingOrder，背景默认是 -100）
        [SerializeField] private float sortingOrderMultiplier = -100f; // 排序顺序倍数（Y 坐标越大，sortingOrder 越大）
        [SerializeField] private string sortingLayerName = "Default"; // 排序图层名称（应与背景在同一图层）
        [SerializeField] private int minSortingOrder = -50; // 最小排序顺序（防止被背景遮挡，背景默认是 -100）
        [SerializeField] private int uniqueSortingOffset = 0; // 唯一排序偏移（每只鸡不同，用于避免重叠闪烁）
        
        private bool wasReplacedByPrefab = false; // 标记当前对象是否是通过预制体替换生成的瘦鸡
        private bool shouldPlaySpawnAnimation = false; // 标记是否应该播放出现动画（从瘦鸡恢复时）
        
        private SkeletonAnimation skeletonAnimation; // Spine 动画组件

        private float currentHP; // 当前血量
        private bool isFat = true; // 是否为肥鸡状态（默认为 true）
        private bool isGolden = false; // 是否为金鸡（默认为 false）
        private float wanderTimer = 0f; // 游荡计时器
        private Camera mainCamera; // 主摄像机（用于边界检测）

        // 恢复相关计时器
        private float recoveryInterval = 1f; // 固定恢复间隔（1秒）
        private float recoveryTimer = 0f; // 恢复计时器
        
        // 充能系统相关
        private float currentCharge = 0f; // 当前充能值
        private float lastInteractionTime = 0f; // 最后一次交互时间

        /// <summary>
        /// 是否为肥鸡状态（只读属性，供外部查询）
        /// </summary>
        public bool IsFat => isFat;

        /// <summary>
        /// 是否为金鸡（只读属性，供外部查询）
        /// </summary>
        public bool IsGolden => isGolden;
        
        /// <summary>
        /// 是否为闪电鸡（只读属性，供外部查询）
        /// </summary>
        public bool IsLightningChicken => isLightningChicken;

        /// <summary>
        /// 设置最大生命值（供生成器调用，根据阶段等级设置）
        /// </summary>
        /// <param name="health">最大生命值</param>
        public void SetMaxHP(float health)
        {
            if (health > 0f)
            {
                float oldMaxHP = maxHP;
                maxHP = health;
                currentHP = maxHP;
            }
            else
            {
                Debug.LogWarning($"ChickenUnit: 尝试设置无效的生命值 {health}，生命值必须大于0", this);
            }
        }

        /// <summary>
        /// 设置是否为金鸡（供生成器调用）
        /// </summary>
        /// <param name="golden">是否为金鸡</param>
        public void SetGolden(bool golden)
        {
            isGolden = golden;
            
            // 如果使用 Spine 动画，切换皮肤
            if (skeletonAnimation != null && !skeletonAnimation.Equals(null) && skeletonAnimation.skeleton != null && skeletonAnimation.skeleton.Data != null)
            {
                string targetSkinName = golden ? goldenSkinName : normalSkinName;
                
                if (!string.IsNullOrEmpty(targetSkinName))
                {
                    // 查找皮肤
                    var targetSkin = skeletonAnimation.skeleton.Data.FindSkin(targetSkinName);
                    if (targetSkin != null)
                    {
                        // 切换皮肤
                        skeletonAnimation.skeleton.SetSkin(targetSkin);
                        skeletonAnimation.skeleton.SetSlotsToSetupPose();
                    }
                    else
                    {
                        Debug.LogWarning($"ChickenUnit: 找不到皮肤 '{targetSkinName}'，请检查皮肤名称是否正确。", this);
                        // 回退到颜色方式
                        if (golden)
                        {
                            skeletonAnimation.skeleton.SetColor(Color.yellow);
                        }
                        else
                        {
                            skeletonAnimation.skeleton.SetColor(Color.white);
                        }
                    }
                }
                else
                {
                    // 如果皮肤名称为空，使用颜色方式
                    if (golden)
                    {
                        skeletonAnimation.skeleton.SetColor(Color.yellow);
                    }
                    else
                    {
                        skeletonAnimation.skeleton.SetColor(Color.white);
                    }
                }
            }
            // 如果没有 Spine 动画，使用 SpriteRenderer 颜色
            else if (spriteRenderer != null && spriteRenderer.enabled)
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

        /// <summary>
        /// 设置是否为闪电鸡（供生成器调用）
        /// </summary>
        /// <param name="isLightning">是否为闪电鸡</param>
        public void SetLightningChicken(bool isLightning)
        {
            isLightningChicken = isLightning;

            // 可以在这里添加额外的视觉效果（例如改变颜色），便于在场景中区分闪电鸡
            if (isLightningChicken)
            {
                if (skeletonAnimation != null && skeletonAnimation.skeleton != null)
                {
                    // 将闪电鸡染成蓝色调，便于区分
                    skeletonAnimation.skeleton.SetColor(Color.cyan);
                }
                else if (spriteRenderer != null && spriteRenderer.enabled)
                {
                    spriteRenderer.color = Color.cyan;
                }
            }
        }
        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;
        private Collider2D chickenCollider;
        private Color originalColor;
        private Vector3 originalScale;
        private float hitFlashTimer = 0f;
        private MeshRenderer meshRenderer;
        
        // 抚摸序列帧动画相关
        private GameObject petAnimationObject = null; // 序列帧显示对象
        private SpriteRenderer petAnimationRenderer = null; // 序列帧渲染器
        private Coroutine petAnimationCoroutine = null; // 序列帧动画协程
        
        // 充能槽相关
        private GameObject chargeBarObject = null; // 充能槽对象
        private SpriteRenderer chargeBarBackground = null; // 充能槽背景
        private SpriteRenderer chargeBarFill = null; // 充能槽填充条

        // 静态事件：当鸡产出时触发（传递位置信息和是否为金鸡）
        public static event Action<Vector3, bool> OnChickenProduct;

        private void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            
            chickenCollider = GetComponent<Collider2D>();
            if (chickenCollider == null)
            {
                chickenCollider = GetComponentInChildren<Collider2D>();
            }
            
            skeletonAnimation = GetComponent<SkeletonAnimation>();
            if (skeletonAnimation == null)
            {
                skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();
            }
            
            // 获取 MeshRenderer（用于 Spine 排序）
            if (skeletonAnimation != null)
            {
                meshRenderer = skeletonAnimation.GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                {
                    meshRenderer = skeletonAnimation.GetComponentInChildren<MeshRenderer>();
                }
                
                // 初始化 Spine 的排序图层
                if (meshRenderer != null)
                {
                    meshRenderer.sortingLayerName = sortingLayerName;
                }
            }
            
            // 初始化 SpriteRenderer 的排序图层
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingLayerName = sortingLayerName;
            }
            
            // 初始化排序顺序
            UpdateSortingOrder();
            
            // 播放动画（Idle 或出现动画）
            if (skeletonAnimation != null)
            {
                if (skeletonAnimation.AnimationState != null)
                {
                    // 如果是从瘦鸡恢复的胖鸡，播放出现动画
                    if (shouldPlaySpawnAnimation && !string.IsNullOrEmpty(spawnAnimationName))
                    {
                        var trackEntry = skeletonAnimation.AnimationState.SetAnimation(0, spawnAnimationName, false);
                        if (trackEntry != null)
                        {
                            // 出现动画播放完成后，恢复 Idle 动画
                            if (!string.IsNullOrEmpty(idleAnimationName))
                            {
                                trackEntry.Complete += (entry) =>
                                {
                                    if (this != null && skeletonAnimation != null && !skeletonAnimation.Equals(null) && skeletonAnimation.AnimationState != null)
                                    {
                                        skeletonAnimation.AnimationState.SetAnimation(0, idleAnimationName, loopIdleAnimation);
                                    }
                                };
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"ChickenUnit: 无法播放出现动画 '{spawnAnimationName}'，trackEntry 为 null。", this);
                            // 回退到播放 Idle
                            if (!string.IsNullOrEmpty(idleAnimationName))
                            {
                                skeletonAnimation.AnimationState.SetAnimation(0, idleAnimationName, loopIdleAnimation);
                            }
                        }
                        // 重置标志
                        shouldPlaySpawnAnimation = false;
                    }
                    // 否则正常播放 Idle 动画
                    else if (!string.IsNullOrEmpty(idleAnimationName))
                    {
                        var trackEntry = skeletonAnimation.AnimationState.SetAnimation(0, idleAnimationName, loopIdleAnimation);
                        if (trackEntry == null)
                        {
                            Debug.LogWarning($"ChickenUnit: 无法播放 Idle 动画 '{idleAnimationName}'，trackEntry 为 null。请检查动画名称是否正确。", this);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("ChickenUnit: Idle 动画名称为空，请设置 Idle Animation Name。", this);
                    }
                }
                else
                {
                    Debug.LogWarning("ChickenUnit: SkeletonAnimation 的 AnimationState 为 null，请检查 Spine 数据是否正确配置。", this);
                }
            }
            else
            {
                Debug.LogWarning("ChickenUnit: 未找到 SkeletonAnimation 组件。如果使用 Spine 动画，请确保已添加 SkeletonAnimation 组件。", this);
            }
            
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

            if (currentHP <= 0f)
            {
                currentHP = maxHP;
            }
            
            // 如果已经是金鸡，应用金鸡皮肤
            if (isGolden)
            {
                SetGolden(true);
            }

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
                
                // 瘦鸡也需要边界检测，防止跑出场地外
                CheckAndAdjustBoundary();
                
                // 瘦鸡状态下不执行其他行为（不执行游荡和受击闪烁）
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
            
            // 充能系统：充能衰减逻辑和UI更新
            if (useChargeSystem)
            {
                UpdateChargeDecay();
                UpdateChargeBar();
            }
        }
        
        /// <summary>
        /// 更新充能衰减
        /// </summary>
        private void UpdateChargeDecay()
        {
            // 如果距离最后一次交互超过延迟时间，开始衰减
            if (Time.time > lastInteractionTime + decayDelay)
            {
                currentCharge -= decayRate * Time.deltaTime;
                currentCharge = Mathf.Clamp(currentCharge, 0f, maxCharge);
            }
        }
        
        /// <summary>
        /// 更新充能槽显示
        /// </summary>
        private void UpdateChargeBar()
        {
            // 确保充能槽对象存在
            EnsureChargeBarObject();
            
            if (chargeBarObject == null) return;
            
            // 根据充能值显示/隐藏充能槽
            bool shouldShow = currentCharge > 0f && isFat;
            chargeBarObject.SetActive(shouldShow);
            
            if (!shouldShow) return;
            
            // 更新充能槽位置（跟随鸡的位置）
            chargeBarObject.transform.position = transform.position + new Vector3(chargeBarOffset.x, chargeBarOffset.y, 0f);
            
            // 计算充能百分比
            float chargePercent = Mathf.Clamp01(currentCharge / maxCharge);
            
            // 更新填充条的宽度
            if (chargeBarFill != null)
            {
                Vector3 fillScale = chargeBarFill.transform.localScale;
                fillScale.x = chargePercent;
                chargeBarFill.transform.localScale = fillScale;
            }
        }
        
        /// <summary>
        /// 确保充能槽对象存在
        /// </summary>
        private void EnsureChargeBarObject()
        {
            if (chargeBarObject != null) return;
            
            // 创建充能槽对象
            chargeBarObject = new GameObject("ChargeBar");
            chargeBarObject.transform.SetParent(transform);
            chargeBarObject.transform.localPosition = chargeBarOffset;
            chargeBarObject.transform.localRotation = Quaternion.identity;
            chargeBarObject.transform.localScale = Vector3.one;
            
            // 创建背景（中心对齐）
            GameObject backgroundObj = new GameObject("Background");
            backgroundObj.transform.SetParent(chargeBarObject.transform);
            backgroundObj.transform.localPosition = Vector3.zero;
            backgroundObj.transform.localRotation = Quaternion.identity;
            backgroundObj.transform.localScale = Vector3.one;
            
            chargeBarBackground = backgroundObj.AddComponent<SpriteRenderer>();
            chargeBarBackground.sprite = CreateChargeBarSprite(chargeBarWidth, chargeBarHeight, true); // 中心对齐
            chargeBarBackground.color = chargeBarBackgroundColor;
            chargeBarBackground.sortingOrder = chargeBarSortingOrder;
            chargeBarBackground.sortingLayerName = chargeBarSortingLayerName;
            
            // 创建填充条（左对齐）
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(chargeBarObject.transform);
            fillObj.transform.localPosition = new Vector3(-chargeBarWidth * 0.5f, 0f, -0.01f); // 左边缘位置
            fillObj.transform.localRotation = Quaternion.identity;
            fillObj.transform.localScale = new Vector3(1f, 1f, 1f);
            
            chargeBarFill = fillObj.AddComponent<SpriteRenderer>();
            chargeBarFill.sprite = CreateChargeBarSprite(chargeBarWidth, chargeBarHeight, false); // 左对齐
            chargeBarFill.color = chargeBarFillColor;
            chargeBarFill.sortingOrder = chargeBarSortingOrder + 1; // 在背景之上
            chargeBarFill.sortingLayerName = chargeBarSortingLayerName;
            
            // 初始时隐藏
            chargeBarObject.SetActive(false);
        }
        
        /// <summary>
        /// 创建充能槽Sprite（简单的矩形）
        /// </summary>
        /// <param name="width">宽度（世界单位）</param>
        /// <param name="height">高度（世界单位）</param>
        /// <param name="centerPivot">是否使用中心对齐（true=中心对齐用于背景，false=左对齐用于填充条）</param>
        private Sprite CreateChargeBarSprite(float width, float height, bool centerPivot)
        {
            int textureWidth = Mathf.RoundToInt(width * 100f); // 假设 100 pixels per unit
            int textureHeight = Mathf.RoundToInt(height * 100f);
            
            textureWidth = Mathf.Max(1, textureWidth);
            textureHeight = Mathf.Max(1, textureHeight);
            
            Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[textureWidth * textureHeight];
            
            // 填充所有像素为白色（颜色由SpriteRenderer控制）
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            // 根据用途选择锚点
            Vector2 pivot = centerPivot ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 0.5f);
            
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, textureWidth, textureHeight),
                pivot,
                100f // pixels per unit
            );
            
            return sprite;
        }
        
        private void LateUpdate()
        {
            // 在每帧结束时更新排序顺序，确保渲染顺序正确
            if (useDynamicSorting)
            {
                UpdateSortingOrder();
            }
        }
        
        /// <summary>
        /// 设置唯一排序偏移（由生成器调用，为每只鸡分配不同的值）
        /// </summary>
        /// <param name="offset">排序偏移值</param>
        public void SetUniqueSortingOffset(int offset)
        {
            uniqueSortingOffset = offset;
            // 立即更新排序顺序
            UpdateSortingOrder();
        }
        
        /// <summary>
        /// 更新渲染排序顺序（根据 Y 坐标或唯一偏移）
        /// </summary>
        private void UpdateSortingOrder()
        {
            if (!useDynamicSorting) return;
            
            // 计算排序顺序
            int sortingOrder = baseSortingOrder + uniqueSortingOffset;
            
            // 如果启用 Y 坐标排序，加上 Y 坐标的影响
            if (useYCoordinateSorting)
            {
                sortingOrder += Mathf.RoundToInt(transform.position.y * sortingOrderMultiplier);
            }
            
            // 确保排序顺序不小于最小值（防止被背景遮挡，背景默认是 -100）
            sortingOrder = Mathf.Max(sortingOrder, minSortingOrder);
            
            // 设置 Spine 的排序顺序
            if (meshRenderer != null)
            {
                meshRenderer.sortingOrder = sortingOrder;
                meshRenderer.sortingLayerName = sortingLayerName;
            }
            // 设置 SpriteRenderer 的排序顺序
            else if (spriteRenderer != null && spriteRenderer.enabled)
            {
                spriteRenderer.sortingOrder = sortingOrder;
                spriteRenderer.sortingLayerName = sortingLayerName;
            }
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
            if (rb == null || chickenCollider == null) return;

            if (!(chickenCollider is CircleCollider2D circleCollider))
            {
                return;
            }

            float minX, maxX, minY, maxY;

            if (PlayArea.Instance != null)
            {
                minX = PlayArea.Instance.ChickenMinX;
                maxX = PlayArea.Instance.ChickenMaxX;
                minY = PlayArea.Instance.ChickenMinY;
                maxY = PlayArea.Instance.ChickenMaxY;
            }
            else
            {
                return;
            }

            Vector2 colliderCenter = circleCollider.bounds.center;
            float colliderRadius = circleCollider.radius;
            
            float colliderLeft = colliderCenter.x - colliderRadius;
            float colliderRight = colliderCenter.x + colliderRadius;
            float colliderBottom = colliderCenter.y - colliderRadius;
            float colliderTop = colliderCenter.y + colliderRadius;

            Vector2 currentVelocity = rb.linearVelocity;
            bool needAdjust = false;
            Vector2 newDirection = currentVelocity.normalized;

            if (colliderLeft < minX)
            {
                newDirection.x = Mathf.Abs(newDirection.x);
                needAdjust = true;
            }
            else if (colliderRight > maxX)
            {
                newDirection.x = -Mathf.Abs(newDirection.x);
                needAdjust = true;
            }

            if (colliderBottom < minY)
            {
                newDirection.y = Mathf.Abs(newDirection.y);
                needAdjust = true;
            }
            else if (colliderTop > maxY)
            {
                newDirection.y = -Mathf.Abs(newDirection.y);
                needAdjust = true;
            }

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

            // 根据系统模式选择处理方式
            if (useChargeSystem)
            {
                // 充能系统：添加充能而不是扣血
                AddCharge(amount);
            }
            else
            {
                // 伤害系统：原有逻辑
                currentHP -= amount;

                // 播放受击反馈（Sprite 变红）
                TriggerHitFlash();

                // 检查是否死亡并产出
                if (currentHP <= 0f && isFat)
                {
                    OnChickenProducted();
                }
            }
        }
        
        /// <summary>
        /// 添加充能值
        /// </summary>
        /// <param name="amount">充能值</param>
        private void AddCharge(float amount)
        {
            if (!isFat)
            {
                return;
            }

            // 增加充能
            currentCharge += amount;
            
            // 更新最后交互时间
            lastInteractionTime = Time.time;
            
            // 播放受击反馈（Sprite 变红）
            TriggerHitFlash();
            
            // 检查是否达到最大充能
            if (currentCharge >= maxCharge)
            {
                // 确保充能值不超过最大值
                currentCharge = maxCharge;
                
                // 先更新充能槽显示为100%，然后再下蛋
                UpdateChargeBar();
                
                LayEgg();
                currentCharge = 0f; // 重置充能
            }
        }
        
        /// <summary>
        /// 下蛋（充能系统专用，与伤害系统行为一致，鸡会变成瘦鸡）
        /// </summary>
        private void LayEgg()
        {
            // 调用原有的产出方法，确保行为一致（鸡会变成瘦鸡）
            OnChickenProducted();
            
            Debug.Log($"ChickenUnit: 充能达到最大值，下蛋！位置: {transform.position}, 金鸡: {isGolden}", this);
        }

        /// <summary>
        /// 触发受击闪烁效果
        /// </summary>
        private void TriggerHitFlash()
        {
            // 播放抚摸序列帧动画
            PlayPetAnimation();
            
            // 瞬间变色效果（无论是否有 Spine 动画，都会触发）
            ApplyHitFlashColor();
            
            // 如果使用 Spine 动画，播放被攻击动画（仅胖鸡）
            if (skeletonAnimation != null && skeletonAnimation.AnimationState != null && isFat)
            {
                // 胖鸡播放被攻击动画
                if (!string.IsNullOrEmpty(hitAnimationName))
                {
                    // 检查动画是否存在
                    var skeletonData = skeletonAnimation.skeleton?.Data;
                    if (skeletonData != null)
                    {
                        var hitAnimation = skeletonData.FindAnimation(hitAnimationName);
                        if (hitAnimation == null)
                        {
                            Debug.LogWarning($"ChickenUnit: 找不到被攻击动画 '{hitAnimationName}'，请检查动画名称是否正确。", this);
                            return;
                        }
                    }
                    
                    // 播放被攻击动画（不循环，播放一次）
                    var trackEntry = skeletonAnimation.AnimationState.SetAnimation(0, hitAnimationName, false);
                    
                    if (trackEntry == null)
                    {
                        Debug.LogWarning($"ChickenUnit: 无法播放被攻击动画 '{hitAnimationName}'，trackEntry 为 null。请检查动画名称是否正确，以及 Spine 数据是否已正确加载。", this);
                        return;
                    }
                    
                    // 被攻击动画播放完成后，恢复 Idle 动画
                    if (!string.IsNullOrEmpty(idleAnimationName))
                    {
                        trackEntry.Complete += (entry) =>
                        {
                            if (this != null && isFat && skeletonAnimation != null && !skeletonAnimation.Equals(null) && skeletonAnimation.AnimationState != null)
                            {
                                skeletonAnimation.AnimationState.SetAnimation(0, idleAnimationName, loopIdleAnimation);
                            }
                        };
                    }
                }
            }
        }

        /// <summary>
        /// 应用受击闪烁颜色（瞬间变色）
        /// </summary>
        private void ApplyHitFlashColor()
        {
            // 设置受击闪烁计时器
            hitFlashTimer = hitFlashDuration;
            
            // 如果使用 Spine 动画，设置颜色
            if (skeletonAnimation != null && skeletonAnimation.skeleton != null)
            {
                skeletonAnimation.skeleton.SetColor(hitFlashColor);
            }
            // 如果使用 SpriteRenderer，设置颜色
            else if (spriteRenderer != null && spriteRenderer.enabled)
            {
                spriteRenderer.color = hitFlashColor;
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
                    if (skeletonAnimation != null && skeletonAnimation.skeleton != null)
                    {
                        skeletonAnimation.skeleton.SetColor(Color.white);
                    }
                    else if (spriteRenderer != null && spriteRenderer.enabled)
                    {
                        spriteRenderer.color = originalColor;
                    }
                }
            }
        }

        /// <summary>
        /// 播放抚摸序列帧动画
        /// </summary>
        private void PlayPetAnimation()
        {
            // 如果未启用抚摸动画，不播放
            if (!enablePetAnimation)
            {
                return;
            }
            
            // 如果没有设置序列帧，不播放
            if (petAnimationSprites == null || petAnimationSprites.Length == 0)
            {
                return;
            }

            // 如果已经有协程在运行，先停止它
            if (petAnimationCoroutine != null)
            {
                StopCoroutine(petAnimationCoroutine);
            }

            // 确保序列帧显示对象存在
            EnsurePetAnimationObject();

            // 启动新的动画协程
            petAnimationCoroutine = StartCoroutine(PlayPetAnimationCoroutine());
        }

        /// <summary>
        /// 确保抚摸序列帧显示对象存在
        /// </summary>
        private void EnsurePetAnimationObject()
        {
            if (petAnimationObject == null)
            {
                // 创建序列帧显示对象
                petAnimationObject = new GameObject("PetAnimation");
                petAnimationObject.transform.SetParent(transform);
                petAnimationObject.transform.localPosition = petAnimationOffset;
                petAnimationObject.transform.localRotation = Quaternion.identity;
                petAnimationObject.transform.localScale = Vector3.one;

                // 添加 SpriteRenderer
                petAnimationRenderer = petAnimationObject.AddComponent<SpriteRenderer>();
                petAnimationRenderer.sortingOrder = petAnimationSortingOrder;
                petAnimationRenderer.sortingLayerName = petAnimationSortingLayerName;
                
                // 初始时隐藏
                petAnimationRenderer.enabled = false;
            }
        }

        /// <summary>
        /// 播放抚摸序列帧动画的协程
        /// </summary>
        private IEnumerator PlayPetAnimationCoroutine()
        {
            // 确保对象存在
            EnsurePetAnimationObject();
            
            if (petAnimationRenderer == null)
            {
                yield break;
            }

            // 显示序列帧对象
            petAnimationRenderer.enabled = true;

            // 遍历所有动画帧
            for (int i = 0; i < petAnimationSprites.Length; i++)
            {
                if (petAnimationSprites[i] != null)
                {
                    // 设置当前帧
                    petAnimationRenderer.sprite = petAnimationSprites[i];
                }

                // 等待帧时间
                yield return new WaitForSeconds(petAnimationFrameTime);
            }

            // 动画播放完毕，隐藏序列帧对象
            if (petAnimationRenderer != null)
            {
                petAnimationRenderer.enabled = false;
            }

            petAnimationCoroutine = null;
        }

        /// <summary>
        /// 播放下蛋特效
        /// </summary>
        private void PlayEggEffect()
        {
            if (eggEffectPrefab == null) return;

            // 计算特效位置
            Vector3 effectPosition = transform.position + (Vector3)eggEffectOffset;

            // 实例化特效
            GameObject effectInstance = Instantiate(eggEffectPrefab, effectPosition, Quaternion.identity);

            // 如果设置了持续时间，在指定时间后销毁特效
            if (eggEffectDuration > 0f)
            {
                Destroy(effectInstance, eggEffectDuration);
            }
            // 否则，尝试自动检测特效的生命周期
            else
            {
                // 检查是否有粒子系统
                ParticleSystem particles = effectInstance.GetComponent<ParticleSystem>();
                if (particles != null)
                {
                    // 如果粒子系统存在，使用其主模块的持续时间
                    float particleDuration = particles.main.duration + particles.main.startLifetime.constantMax;
                    Destroy(effectInstance, particleDuration);
                }
                else
                {
                    // 如果没有粒子系统，默认 5 秒后销毁
                    Destroy(effectInstance, 5f);
                }
            }
        }

        /// <summary>
        /// 鸡产出：触发事件，切换状态，改变视觉
        /// </summary>
        private void OnChickenProducted()
        {
            // 播放下蛋特效
            PlayEggEffect();

            // 触发静态事件，传递位置信息和是否为金鸡
            OnChickenProduct?.Invoke(transform.position, isGolden);

            // 如果是闪电鸡，则在下蛋时有概率触发链式闪电
            if (isLightningChicken && ProjectChicken.Abilities.ChainLightningEffect.Instance != null)
            {
                if (UnityEngine.Random.value < lightningEggTriggerChance)
                {
                    // 使用当前攻击力作为基础伤害（与玩家抚摸伤害保持一致）
                    float baseDamage = ProjectChicken.Core.UpgradeManager.Instance != null 
                        ? ProjectChicken.Core.UpgradeManager.Instance.CurrentDamage 
                        : 10f;

                    ProjectChicken.Abilities.ChainLightningEffect.Instance.Trigger(
                        this,
                        baseDamage,
                        lightningMaxTargets,
                        1.0f,                  // 第一次必定跳跃
                        lightningChainDecay,
                        lightningDamagePercent,
                        lightningRange
                    );
                }
            }

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
                // 瘦鸡状态下播放待机动画（如果 Spine 动画可用）
                if (skeletonAnimation != null && skeletonAnimation.AnimationState != null)
                {
                    if (!string.IsNullOrEmpty(idleAnimationName))
                    {
                        // 瘦鸡播放待机动画
                        skeletonAnimation.AnimationState.SetAnimation(0, idleAnimationName, loopIdleAnimation);
                    }
                }
                // 如果使用 Spine 动画，可以通过改变颜色
                if (skeletonAnimation != null && skeletonAnimation.skeleton != null)
                {
                    skeletonAnimation.skeleton.SetColor(Color.gray);
                }
                else if (spriteRenderer != null && spriteRenderer.enabled)
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
            // 保存 Spine 动画引用（如果有）
            SkeletonAnimation originalSkeletonAnimation = skeletonAnimation;
            if (thinChickenPrefab == null)
            {
                Debug.LogWarning("ChickenUnit: thinChickenPrefab 为空，无法替换！使用代码方式改变外观。", this);
                // 回退到代码方式
                if (skeletonAnimation != null && skeletonAnimation.skeleton != null)
                {
                    skeletonAnimation.skeleton.SetColor(Color.gray);
                }
                else if (spriteRenderer != null && spriteRenderer.enabled)
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

            // 保存当前状态（包括原始的最大生命值）
            Vector3 currentPosition = transform.position;
            bool wasGolden = isGolden;
            float originalMaxHP = maxHP;

            // 实例化瘦鸡预制体
            GameObject thinChicken = Instantiate(thinChickenPrefab, currentPosition, transform.rotation);
            
            if (thinChicken == null)
            {
                Debug.LogError("ChickenUnit: 瘦鸡预制体实例化失败！回退到代码方式。", this);
                // 回退到代码方式
                if (skeletonAnimation != null && skeletonAnimation.skeleton != null)
                {
                    skeletonAnimation.skeleton.SetColor(Color.gray);
                }
                else if (spriteRenderer != null && spriteRenderer.enabled)
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
                
                thinChickenUnit.SetMaxHP(originalMaxHP);
                
                // 确保瘦鸡播放待机动画
                var thinSkeletonAnimation = thinChicken.GetComponent<SkeletonAnimation>();
                if (thinSkeletonAnimation == null)
                {
                    thinSkeletonAnimation = thinChicken.GetComponentInChildren<SkeletonAnimation>();
                }
                if (thinSkeletonAnimation != null && thinSkeletonAnimation.AnimationState != null)
                {
                    // 使用反射获取瘦鸡的 idleAnimationName
                    var idleAnimationNameField = typeof(ChickenUnit).GetField("idleAnimationName", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (idleAnimationNameField != null)
                    {
                        string thinIdleName = idleAnimationNameField.GetValue(thinChickenUnit) as string;
                        if (!string.IsNullOrEmpty(thinIdleName))
                        {
                            thinSkeletonAnimation.AnimationState.SetAnimation(0, thinIdleName, true);
                        }
                    }
                }
                
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
            }
            else
            {
                Debug.LogWarning("ChickenUnit: 瘦鸡预制体没有 ChickenUnit 组件！瘦鸡可能无法正常工作。回退到代码方式。", this);
                // 如果瘦鸡预制体没有 ChickenUnit 组件，销毁它并回退到代码方式
                Destroy(thinChicken);
                
                // 回退到代码方式
                if (skeletonAnimation != null && skeletonAnimation.skeleton != null)
                {
                    skeletonAnimation.skeleton.SetColor(Color.gray);
                }
                else if (spriteRenderer != null && spriteRenderer.enabled)
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
                                
                                // 设置标志，让 Start() 方法播放出现动画而不是 Idle
                                var shouldPlaySpawnField = typeof(ChickenUnit).GetField("shouldPlaySpawnAnimation", 
                                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                if (shouldPlaySpawnField != null)
                                {
                                    shouldPlaySpawnField.SetValue(fatChicken, true);
                                }
                                
                                // 根据当前场地等级重新设置生命值（而不是使用保存的原始值）
                                // 这样可以确保恢复后的鸡使用当前场地等级的生命值
                                if (ChickenSpawner.Instance != null)
                                {
                                    ChickenSpawner.Instance.SetChickenHealthByStage(fatChicken, wasGolden);
                                }
                                else
                                {
                                    // 如果 ChickenSpawner 为空，使用保存的原始值作为回退
                                    fatChicken.SetMaxHP(maxHP);
                                    Debug.LogWarning($"ChickenUnit: ChickenSpawner.Instance 为空，使用保存的原始生命值 {maxHP}", this);
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
                // 如果不是通过预制体替换，在当前对象上播放出现动画
                if (!wasReplacedByPrefab)
                {
                    // 播放出现动画（胖鸡从瘦鸡恢复时）
                    if (skeletonAnimation != null && skeletonAnimation.AnimationState != null)
                    {
                        if (!string.IsNullOrEmpty(spawnAnimationName))
                        {
                            var trackEntry = skeletonAnimation.AnimationState.SetAnimation(0, spawnAnimationName, false);
                            
                            // 出现动画播放完成后，恢复 Idle 动画
                            if (trackEntry != null && !string.IsNullOrEmpty(idleAnimationName))
                            {
                                trackEntry.Complete += (entry) =>
                                {
                                    if (this != null && isFat && skeletonAnimation != null && !skeletonAnimation.Equals(null) && skeletonAnimation.AnimationState != null)
                                    {
                                        skeletonAnimation.AnimationState.SetAnimation(0, idleAnimationName, loopIdleAnimation);
                                    }
                                };
                            }
                        }
                        else if (!string.IsNullOrEmpty(idleAnimationName))
                        {
                            // 如果没有出现动画，直接播放 Idle
                            skeletonAnimation.AnimationState.SetAnimation(0, idleAnimationName, loopIdleAnimation);
                        }
                    }
                }
                
                if (skeletonAnimation != null && skeletonAnimation.skeleton != null)
                {
                    skeletonAnimation.skeleton.SetColor(Color.white);
                }
                else if (spriteRenderer != null && spriteRenderer.enabled)
                {
                    spriteRenderer.color = originalColor;
                }
                transform.localScale = originalScale; // 恢复原始大小

                if (ChickenSpawner.Instance != null)
                {
                    ChickenSpawner.Instance.SetChickenHealthByStage(this, isGolden);
                }
                else
                {
                    // 如果 ChickenSpawner 为空，使用保存的原始值作为回退
                    currentHP = maxHP;
                    Debug.LogWarning($"ChickenUnit: ChickenSpawner.Instance 为空，使用保存的原始生命值 {maxHP}", this);
                }

                wanderTimer = 0f;

                // 分裂检查：如果解锁了分裂能力且可以生成更多鸡
                if (UpgradeManager.Instance != null && UpgradeManager.Instance.IsMitosisUnlocked)
                {
                    if (ChickenSpawner.Instance != null && ChickenSpawner.Instance.CanSpawnMore())
                    {
                        // 获取分裂几率
                        float mitosisChance = UpgradeManager.Instance.MitosisChance;

                        if (UnityEngine.Random.value < mitosisChance)
                        {
                            ChickenSpawner.Instance.SpawnChickenAt(transform.position);
                        }
                    }
                }
            }
        }

        private void OnDestroy()
        {
            // 清理引用，防止编辑器访问已销毁的对象
            skeletonAnimation = null;
            chickenCollider = null;
            rb = null;
            spriteRenderer = null;
            meshRenderer = null;
        }
    }
}


