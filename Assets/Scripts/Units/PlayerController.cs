using UnityEngine;
using UnityEngine.InputSystem;
using ProjectChicken.Core;

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

        [Header("攻击范围显示")]
        [SerializeField] private SpriteRenderer rangeIndicator; // 攻击范围指示器（SpriteRenderer）
        [SerializeField] private float borderWidth = 3f; // 边框粗细（像素）

        [Header("调试显示（仅用于查看，实际值从 UpgradeManager 读取）")]
        [SerializeField] private float debugAttackRange = 2f; // 调试：攻击范围显示
        [SerializeField] private float debugDamageAmount = 10f; // 调试：伤害显示
        [SerializeField] private float debugAttackInterval = 1f; // 调试：攻击间隔显示

        private float attackTimer = 0f; // 攻击计时器
        private float lastRange = -1f; // 上次的攻击范围（用于检测是否需要更新圆圈大小）

        private void Start()
        {
            // 如果没有指定摄像机，尝试获取主摄像机
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            // 初始化攻击范围指示器
            InitializeRangeIndicator();
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

            // 更新攻击范围指示器（如果范围发生变化）
            UpdateRangeIndicator();

            // 攻击循环：从 UpgradeManager 获取当前攻击间隔
            float currentAttackInterval = GetCurrentAttackInterval();
            attackTimer += Time.deltaTime;
            if (attackTimer >= currentAttackInterval)
            {
                attackTimer = 0f;
                PerformAttack();
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
            
            transform.position = mouseWorldPos;
        }

        /// <summary>
        /// 执行攻击：检测范围内所有敌人并造成伤害
        /// </summary>
        private void PerformAttack()
        {
            // 从 UpgradeManager 获取当前属性值
            float currentAttackRange = GetCurrentAttackRange();
            float currentDamage = GetCurrentDamage();

            // 使用 Physics2D.OverlapCircle 检测范围内的敌人
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, currentAttackRange, enemyLayer);

            foreach (Collider2D hit in hits)
            {
                // 检查是否实现了 IDamageable 接口
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(currentDamage);
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
        /// 获取当前攻击间隔（从 UpgradeManager 读取）
        /// </summary>
        /// <returns>当前攻击间隔（秒）</returns>
        private float GetCurrentAttackInterval()
        {
            if (UpgradeManager.Instance != null)
            {
                return UpgradeManager.Instance.CurrentAttackInterval;
            }

            // 如果 UpgradeManager 未初始化，返回默认值
            return debugAttackInterval;
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
        /// 在 Scene 视图中绘制攻击范围（红色圆圈）
        /// </summary>
        private void OnDrawGizmos()
        {
            // 绘制当前攻击范围（从 UpgradeManager 读取，如果可用）
            // 在编辑器中抑制警告，避免产生大量日志
            float currentRange = GetCurrentAttackRange(suppressWarning: true);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, currentRange);
        }
    }
}

