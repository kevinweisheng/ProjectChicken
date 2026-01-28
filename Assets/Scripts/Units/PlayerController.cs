using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using ProjectChicken.Core;
using ProjectChicken.Abilities;
using ProjectChicken.Units;

namespace ProjectChicken.Units
{
    /// <summary>
    /// 玩家控制器：跟随鼠标，对范围内敌人造成 AoE 伤害
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("基础配置")]
        [SerializeField] private LayerMask enemyLayer; // 敌人所在的 Layer
        [SerializeField] private Camera mainCamera; // 主摄像机（用于屏幕坐标转世界坐标）

        [Header("鼠标光标")]
        [SerializeField] private Texture2D customCursor; // 自定义鼠标光标图片（默认状态）
        [SerializeField] private Vector2 cursorHotspot = Vector2.zero; // 光标热点位置（通常是图片中心或点击点）
        
        [Header("抚摸动画（序列帧）")]
        [Tooltip("抚摸动画的序列帧（按顺序排列）")]
        [SerializeField] private Texture2D[] petAnimationFrames = new Texture2D[0];
        [Tooltip("每帧的显示时间（秒）")]
        [SerializeField] private float petAnimationFrameTime = 0.1f;
        [Tooltip("是否循环播放抚摸动画（如果为 false，播放一次后恢复默认光标）")]
        [SerializeField] private bool loopPetAnimation = false;

        [Header("攻击范围显示")]
        [SerializeField] private SpriteRenderer rangeIndicator; // 攻击范围指示器（SpriteRenderer）
        [SerializeField] private float borderWidth = 3f; // 边框粗细（像素）

        [Header("攻击配置")]
        [Tooltip("攻击间隔（秒）- 仅在 UpgradeManager 未初始化时使用")]
        [SerializeField] private float fallbackAttackInterval = 0.5f; // 备用攻击间隔（默认 0.5 秒）
        
        [Header("能力：链式闪电（鼠标攻击）")]
        [Tooltip("是否启用鼠标攻击的链式闪电（如果只想用闪电鸡下蛋触发闪电，请关闭此选项）")]
        [SerializeField] private bool enableMouseChainLightning = false;
        
        [Tooltip("链式闪电效果组件（拖入场景中的 ChainLightningEffect 对象）")]
        [SerializeField] private ChainLightningEffect lightningEffect;
        
        [Tooltip("链式闪电触发概率（0-1，例如 0.1 表示 10% 概率）")]
        [SerializeField, Range(0f, 1f)] private float lightningTriggerChance = 0.1f;
        
        [Tooltip("链式闪电最大目标数量（包括起始目标）")]
        [SerializeField] private int lightningMaxTargets = 5;
        
        [Tooltip("链式闪电衰减因子（每次链式后概率乘以这个值，例如 0.5 表示每次减半）")]
        [SerializeField, Range(0f, 1f)] private float lightningChainDecay = 0.5f;
        
        [Tooltip("链式闪电伤害百分比（每次链式的伤害 = 基础伤害 * 此值，例如 0.5 表示 50%）")]
        [SerializeField] private float lightningDamagePercent = 0.5f;
        
        [Tooltip("链式闪电搜索范围（世界单位，用于查找下一个目标）")]
        [SerializeField] private float lightningRange = 5.0f;
        
        [Header("调试显示（仅用于查看，实际值从 UpgradeManager 读取）")]
        [SerializeField] private float debugAttackRange = 2f; // 调试：攻击范围显示
        [SerializeField] private float debugDamageAmount = 10f; // 调试：伤害显示

        private float attackTimer = 0f; // 攻击计时器（累加 Time.deltaTime）
        private float lastRange = -1f; // 上次的攻击范围（用于检测是否需要更新圆圈大小）
        
        // 抚摸动画相关
        private bool isPlayingPetAnimation = false; // 是否正在播放抚摸动画
        private Coroutine petAnimationCoroutine = null; // 抚摸动画协程

        [Header("分形系统约束（由 FractalManager 动态设置）")]
        // 分形系统约束变量
        private Vector2 _panLimit = Vector2.zero; // 平移限制（X 和 Y 的最大偏移）
        private float _minZoom = 1f; // 最小缩放值
        private float _maxZoom = 20f; // 最大缩放值

        private void Start()
        {
            // 如果没有指定摄像机，尝试获取主摄像机
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            // 初始化自定义鼠标光标
            InitializeCustomCursor();

            // 初始化攻击范围指示器
            InitializeRangeIndicator();
        }

        /// <summary>
        /// 初始化自定义鼠标光标
        /// </summary>
        private void InitializeCustomCursor()
        {
            if (customCursor != null)
            {
                // 设置自定义光标
                Cursor.SetCursor(customCursor, cursorHotspot, CursorMode.Auto);
            }
            else
            {
                // 如果没有设置自定义光标，使用默认光标
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }

        private void OnDestroy()
        {
            // 游戏对象销毁时恢复默认光标（可选）
            // Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        private void Update()
        {
            // 状态检查：如果不在游戏中（准备阶段或游戏结束），禁止玩家移动和交互
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            // 视觉跟随：让物体跟随鼠标位置
            FollowMousePosition();

            // 处理摄像机缩放（如果摄像机存在且是正交摄像机）
            HandleCameraZoom();

            // 更新攻击范围指示器（如果范围发生变化）
            UpdateRangeIndicator();

            // 攻击循环：使用计时器控制攻击频率，确保不受帧率影响
            UpdateAttackTimer();
        }
        
        /// <summary>
        /// 更新攻击计时器并执行攻击（确保攻击频率受控，不依赖帧率）
        /// </summary>
        private void UpdateAttackTimer()
        {
            // 获取当前攻击间隔（从 UpgradeManager 或使用备用值）
            float currentAttackInterval = GetCurrentAttackInterval();
            
            // 累加时间（与帧率无关）
            attackTimer += Time.deltaTime;
            
            // 当计时器达到攻击间隔时，执行攻击并重置计时器
            if (attackTimer >= currentAttackInterval)
            {
                attackTimer = 0f;
                PerformAttack();
            }
        }

        /// <summary>
        /// 处理摄像机缩放输入
        /// </summary>
        private void HandleCameraZoom()
        {
            if (mainCamera == null || !mainCamera.orthographic) return;

            // 禁止在回合内使用滑轮进行缩放
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
            {
                return; // 游戏进行中时禁用滑轮缩放
            }

            // 检查是否有缩放约束
            if (_minZoom <= 0f && _maxZoom <= 0f)
            {
                return; // 如果没有设置约束，不处理缩放
            }

            // 获取鼠标滚轮输入（使用新的 Input System）
            if (Mouse.current != null)
            {
                float scrollDelta = Mouse.current.scroll.ReadValue().y;
                
                if (scrollDelta != 0f)
                {
                    // 调整摄像机缩放
                    float currentSize = mainCamera.orthographicSize;
                    float zoomSpeed = 0.5f; // 缩放速度
                    float newSize = currentSize - scrollDelta * zoomSpeed;
                    
                    // 应用缩放限制
                    newSize = Mathf.Clamp(newSize, _minZoom, _maxZoom);
                    
                    mainCamera.orthographicSize = newSize;
                }
            }
        }

        /// <summary>
        /// 跟随鼠标位置（转换为世界坐标）
        /// </summary>
        private void FollowMousePosition()
        {
            if (mainCamera == null) return;
            if (Mouse.current == null) return; // 安全检查：确保鼠标设备可用

            // 使用新的 Input System 获取鼠标位置
            Vector2 mouseScreenPos2D = Mouse.current.position.ReadValue();
            Vector3 mouseScreenPos = new Vector3(mouseScreenPos2D.x, mouseScreenPos2D.y, mainCamera.nearClipPlane + 1f);
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
            mouseWorldPos.z = transform.position.z; // 保持原有的 Z 坐标（2D 游戏）
            
            // 应用平移限制（如果设置了约束）
            if (_panLimit != Vector2.zero)
            {
                mouseWorldPos.x = Mathf.Clamp(mouseWorldPos.x, -_panLimit.x, _panLimit.x);
                mouseWorldPos.y = Mathf.Clamp(mouseWorldPos.y, -_panLimit.y, _panLimit.y);
            }
            
            transform.position = mouseWorldPos;
        }

        /// <summary>
        /// 执行攻击：检测范围内所有敌人并造成伤害/充能
        /// 注意：此方法由计时器控制调用，不会每帧执行
        /// </summary>
        private void PerformAttack()
        {
            // 播放抚摸动画
            PlayPetAnimation();

            // 从 UpgradeManager 获取当前属性值
            float currentAttackRange = GetCurrentAttackRange();
            float currentDamage = GetCurrentDamage();

            // 检查是否触发暴击
            if (UpgradeManager.Instance != null)
            {
                float critChance = UpgradeManager.Instance.CritChance;
                if (critChance > 0f && UnityEngine.Random.value < critChance)
                {
                    float critMultiplier = UpgradeManager.Instance.CritMultiplier;
                    currentDamage *= critMultiplier;
                }
            }

            // 使用 Physics2D.OverlapCircle 检测范围内的敌人
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, currentAttackRange, enemyLayer);

            // 存储所有被击中的鸡（用于链式闪电）
            List<ChickenUnit> hitChickens = new List<ChickenUnit>();

            foreach (Collider2D hit in hits)
            {
                // 检查是否实现了 IDamageable 接口
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(currentDamage);
                    
                    // 记录所有被击中的鸡（无论是否为肥鸡），用于链式闪电
                    ChickenUnit chicken = hit.GetComponent<ChickenUnit>();
                    if (chicken != null)
                    {
                        hitChickens.Add(chicken);
                    }
                }
            }
            
            // 对每个被击中的鸡，检查是否触发链式闪电（仅当启用了鼠标链式闪电时）
            if (enableMouseChainLightning && hitChickens.Count > 0 && lightningEffect != null)
            {
                foreach (ChickenUnit hitChicken in hitChickens)
                {
                    // 检查是否触发链式闪电
                    if (Random.value < lightningTriggerChance)
                    {
                        // 触发链式闪电（初始链式概率为 1.0，表示第一次链式是 100%）
                        lightningEffect.Trigger(
                            startTarget: hitChicken,
                            baseDamage: currentDamage,
                            maxTargets: lightningMaxTargets,
                            initialChainChance: 1.0f,
                            decayFactor: lightningChainDecay,
                            damagePercent: lightningDamagePercent,
                            chainRange: lightningRange
                        );
                    }
                }
            }

            // 检查是否触发引力波（需要先解锁）
            if (UpgradeManager.Instance != null && UpgradeManager.Instance.IsGravityWaveUnlocked)
            {
                float gravityWaveChance = UpgradeManager.Instance.GravityWaveChance;
                if (gravityWaveChance > 0f && UnityEngine.Random.value < gravityWaveChance)
                {
                    TriggerGravityWave(transform.position, currentAttackRange);
                }
            }
        }

        /// <summary>
        /// 触发引力波：对更大范围内的敌人造成伤害
        /// </summary>
        /// <param name="center">引力波中心位置</param>
        /// <param name="baseRange">基础攻击范围</param>
        private void TriggerGravityWave(Vector3 center, float baseRange)
        {
            // 引力波范围是基础范围的2倍
            float gravityWaveRange = baseRange * 2f;
            
            // 从 UpgradeManager 获取当前伤害
            float currentDamage = GetCurrentDamage();
            
            // 引力波伤害为基础伤害的50%（可调整）
            float gravityWaveDamage = currentDamage * 0.5f;

            // 使用 Physics2D.OverlapCircle 检测引力波范围内的敌人
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, gravityWaveRange, enemyLayer);

            int hitCount = 0;
            foreach (Collider2D hit in hits)
            {
                // 检查是否实现了 IDamageable 接口
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(gravityWaveDamage);
                    hitCount++;
                }
            }
        }

        /// <summary>
        /// 获取当前攻击范围（从 UpgradeManager 读取）
        /// </summary>
        /// <param name="suppressWarning">是否抑制警告（用于编辑器中的 Gizmos 绘制）</param>
        /// <returns>当前攻击范围</returns>
        private float GetCurrentAttackRange(bool suppressWarning = false)
        {
            if (UpgradeManager.Instance != null)
            {
                return UpgradeManager.Instance.CurrentRadius;
            }

            // 如果 UpgradeManager 未初始化，返回默认值
            // 只在运行时且不抑制警告时输出警告（避免编辑器中的 Gizmos 调用产生大量日志）
            if (Application.isPlaying && !suppressWarning)
            {
                Debug.LogWarning("PlayerController: UpgradeManager.Instance 为空，使用默认攻击范围！", this);
            }
            return debugAttackRange;
        }

        /// <summary>
        /// 获取当前攻击力（从 UpgradeManager 读取）
        /// </summary>
        /// <param name="suppressWarning">是否抑制警告（用于编辑器中的 Gizmos 绘制）</param>
        /// <returns>当前攻击力</returns>
        private float GetCurrentDamage(bool suppressWarning = false)
        {
            if (UpgradeManager.Instance != null)
            {
                return UpgradeManager.Instance.CurrentDamage;
            }

            // 如果 UpgradeManager 未初始化，返回默认值
            // 只在运行时且不抑制警告时输出警告（避免编辑器中的 Gizmos 调用产生大量日志）
            if (Application.isPlaying && !suppressWarning)
            {
                Debug.LogWarning("PlayerController: UpgradeManager.Instance 为空，使用默认攻击力！", this);
            }
            return debugDamageAmount;
        }

        /// <summary>
        /// 获取当前攻击间隔（从 UpgradeManager 读取，如果未初始化则使用备用值）
        /// </summary>
        /// <returns>当前攻击间隔（秒）</returns>
        private float GetCurrentAttackInterval()
        {
            if (UpgradeManager.Instance != null)
            {
                float interval = UpgradeManager.Instance.CurrentAttackInterval;
                // 确保攻击间隔有效（大于 0）
                if (interval > 0f)
                {
                    return interval;
                }
            }

            // 如果 UpgradeManager 未初始化或返回无效值，使用备用值
            return fallbackAttackInterval;
        }

        /// <summary>
        /// 初始化攻击范围指示器
        /// </summary>
        private void InitializeRangeIndicator()
        {
            // 如果没有手动指定，尝试自动获取或创建 SpriteRenderer
            if (rangeIndicator == null)
            {
                rangeIndicator = GetComponentInChildren<SpriteRenderer>();
                if (rangeIndicator == null)
                {
                    // 如果没有找到，创建一个新的 GameObject 作为子对象
                    GameObject indicatorObj = new GameObject("RangeIndicator");
                    indicatorObj.transform.SetParent(transform);
                    indicatorObj.transform.localPosition = Vector3.zero;
                    indicatorObj.transform.localRotation = Quaternion.identity;
                    indicatorObj.transform.localScale = Vector3.one;
                    rangeIndicator = indicatorObj.AddComponent<SpriteRenderer>();
                }
            }

            if (rangeIndicator != null)
            {
                // 创建或使用圆形 Sprite
                Sprite circleSprite = CreateCircleSprite();
                if (circleSprite != null)
                {
                    rangeIndicator.sprite = circleSprite;
                }

                // 配置 SpriteRenderer
                // 注意：如果 Inspector 中已经设置了颜色，这里不会覆盖
                // 如果没有设置，使用默认的红色半透明
                if (rangeIndicator.color == Color.white)
                {
                    rangeIndicator.color = new Color(1f, 0f, 0f, 0.3f); // 默认颜色（红色，半透明）
                }
                rangeIndicator.sortingOrder = 1; // 确保在其他对象之上
                rangeIndicator.sortingLayerName = "Default";
                
                // 初始时隐藏，等范围更新后再显示
                rangeIndicator.enabled = false;
            }
        }

        /// <summary>
        /// 创建圆形 Sprite（使用代码生成）
        /// </summary>
        /// <returns>圆形 Sprite</returns>
        private Sprite CreateCircleSprite()
        {
            // 创建一个简单的圆形纹理
            int size = 256; // 纹理大小
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 2f; // 留一点边距
            
            // 绘制圆形（只绘制边缘，内部透明）
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    // 只绘制边缘（使用可配置的边框粗细）
                    if (distance >= radius - borderWidth && distance <= radius)
                    {
                        texture.SetPixel(x, y, Color.white);
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }
            
            texture.Apply();
            
            // 创建 Sprite
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), // 中心点
                100f // pixels per unit
            );
            
            return sprite;
        }

        /// <summary>
        /// 更新攻击范围指示器
        /// </summary>
        private void UpdateRangeIndicator()
        {
            if (rangeIndicator == null) return;

            // 获取当前攻击范围
            float currentRange = GetCurrentAttackRange(suppressWarning: true);

            // 如果范围没有变化，不需要更新
            if (Mathf.Approximately(currentRange, lastRange))
            {
                return;
            }

            lastRange = currentRange;

            // 更新圆圈大小（Sprite 的本地缩放）
            // Sprite 的默认大小是 1 unit，所以需要根据范围调整缩放
            // 假设 Sprite 的原始半径是 1 unit（因为 pixels per unit = 100，纹理大小 256，半径约 1.28）
            // 我们需要将缩放设置为 currentRange / 1.28
            float spriteRadius = 1.28f; // 根据 CreateCircleSprite 中的设置计算
            float scale = currentRange / spriteRadius;
            rangeIndicator.transform.localScale = new Vector3(scale, scale, 1f);
            
            // 确保显示
            rangeIndicator.enabled = true;
        }

        /// <summary>
        /// 更新约束：由 FractalManager 调用以动态更新摄像机约束
        /// </summary>
        /// <param name="panLimit">平移限制（X 和 Y 的最大偏移）</param>
        /// <param name="minZoom">最小缩放值</param>
        /// <param name="maxZoom">最大缩放值</param>
        public void UpdateConstraints(Vector2 panLimit, float minZoom, float maxZoom)
        {
            _panLimit = panLimit;
            _minZoom = minZoom;
            _maxZoom = maxZoom;

            // 立即应用缩放限制到当前摄像机
            if (mainCamera != null && mainCamera.orthographic)
            {
                mainCamera.orthographicSize = Mathf.Clamp(mainCamera.orthographicSize, _minZoom, _maxZoom);
            }
        }

        /// <summary>
        /// 获取当前缩放值（摄像机正交尺寸）
        /// </summary>
        /// <returns>当前缩放值</returns>
        public float GetCurrentZoom()
        {
            if (mainCamera != null && mainCamera.orthographic)
            {
                return mainCamera.orthographicSize;
            }
            return 5f; // 默认值
        }

        /// <summary>
        /// 重置摄像机：设置位置为 (0,0,0) 并设置正交尺寸
        /// </summary>
        /// <param name="size">目标正交尺寸</param>
        public void ResetCamera(float size)
        {
            if (mainCamera != null)
            {
                // 重置摄像机位置
                mainCamera.transform.position = new Vector3(0f, 0f, mainCamera.transform.position.z);
                
                // 设置正交尺寸
                if (mainCamera.orthographic)
                {
                    mainCamera.orthographicSize = size;
                }
            }
        }

        /// <summary>
        /// 播放抚摸动画（序列帧）
        /// </summary>
        private void PlayPetAnimation()
        {
            // 如果没有设置动画帧，不播放
            if (petAnimationFrames == null || petAnimationFrames.Length == 0)
            {
                return;
            }

            // 如果正在播放动画且不循环，不重复播放
            if (isPlayingPetAnimation && !loopPetAnimation)
            {
                return;
            }

            // 如果已经有协程在运行，先停止它
            if (petAnimationCoroutine != null)
            {
                StopCoroutine(petAnimationCoroutine);
            }

            // 启动新的动画协程
            petAnimationCoroutine = StartCoroutine(PlayPetAnimationCoroutine());
        }

        /// <summary>
        /// 播放抚摸动画的协程
        /// </summary>
        private System.Collections.IEnumerator PlayPetAnimationCoroutine()
        {
            isPlayingPetAnimation = true;

            do
            {
                // 遍历所有动画帧
                for (int i = 0; i < petAnimationFrames.Length; i++)
                {
                    if (petAnimationFrames[i] != null)
                    {
                        // 创建可读的纹理副本（如果原纹理不可读）
                        Texture2D readableTexture = CreateReadableTexture(petAnimationFrames[i]);
                        if (readableTexture != null)
                        {
                            // 设置当前帧为鼠标光标
                            Cursor.SetCursor(readableTexture, cursorHotspot, CursorMode.Auto);
                        }
                    }

                    // 等待帧时间
                    yield return new WaitForSeconds(petAnimationFrameTime);
                }
            } while (loopPetAnimation); // 如果循环，重复播放

            // 动画播放完毕，恢复默认光标
            if (customCursor != null)
            {
                Texture2D readableDefaultCursor = CreateReadableTexture(customCursor);
                if (readableDefaultCursor != null)
                {
                    Cursor.SetCursor(readableDefaultCursor, cursorHotspot, CursorMode.Auto);
                }
                else
                {
                    Cursor.SetCursor(customCursor, cursorHotspot, CursorMode.Auto);
                }
            }
            else
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }

            isPlayingPetAnimation = false;
            petAnimationCoroutine = null;
        }

        /// <summary>
        /// 创建可读的纹理副本（如果原纹理不可读）
        /// </summary>
        /// <param name="source">源纹理</param>
        /// <returns>可读的纹理副本，如果原纹理已可读则返回原纹理</returns>
        private Texture2D CreateReadableTexture(Texture2D source)
        {
            if (source == null) return null;

            // 检查纹理是否已经可读
            try
            {
                // 尝试读取一个像素来检查是否可读
                source.GetPixel(0, 0);
                // 如果成功，说明纹理已可读，直接返回
                return source;
            }
            catch
            {
                // 纹理不可读，需要创建可读副本
            }

            // 使用 RenderTexture 创建可读副本
            RenderTexture renderTexture = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);

            Graphics.Blit(source, renderTexture);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;

            // 创建新的可读纹理
            Texture2D readableTexture = new Texture2D(source.width, source.height);
            readableTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            readableTexture.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);

            return readableTexture;
        }

        /// <summary>
        /// 在 Scene 视图中绘制攻击范围（红色圆圈）
        /// </summary>
        protected void OnDrawGizmos()
        {
            // 绘制当前攻击范围（从 UpgradeManager 读取，如果可用）
            // 在编辑器中抑制警告，避免产生大量日志
            float currentRange = GetCurrentAttackRange(suppressWarning: true);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, currentRange);
        }
    }
}

