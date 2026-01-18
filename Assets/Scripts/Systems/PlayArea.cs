using UnityEngine;

namespace ProjectChicken.Systems
{
    /// <summary>
    /// 游戏场地管理器：定义游戏场地的边界，限制鸡的移动范围
    /// </summary>
    public class PlayArea : MonoBehaviour
    {
        public static PlayArea Instance { get; private set; }

        [Header("场地配置")]
        [SerializeField] private Vector2 areaSize = new Vector2(10f, 10f); // 场地大小（世界单位，内部使用）
        [SerializeField] private Vector2 areaCenter = Vector2.zero; // 场地中心位置（世界坐标）
        [SerializeField] private bool useWorldBounds = true; // 是否使用世界坐标边界（如果为 false，则相对于 GameObject 位置）

        [Header("鸡活动范围配置")]
        [SerializeField] private Vector2 chickenMovementAreaSize = Vector2.zero; // 鸡的活动范围大小（世界单位，如果为 (0,0) 则使用场地大小）
        [SerializeField] private Vector2 chickenMovementAreaCenter = Vector2.zero; // 鸡的活动范围中心位置（世界坐标，如果为 (0,0) 则使用场地中心）
        [SerializeField] private bool useCustomChickenArea = false; // 是否使用自定义鸡活动范围（如果为 false，则使用场地大小和中心）
        
        [Header("像素单位显示（可选）")]
        [SerializeField] private bool usePixelUnits = true; // 是否使用像素单位显示（如果为 true，areaSize 将显示为像素）
        [SerializeField] private Camera mainCamera; // 主摄像机（用于像素到世界单位的转换，如果为空则自动获取）

        [Header("美术素材（可选）")]
        [SerializeField] private SpriteRenderer areaSpriteRenderer; // 场地 Sprite 渲染器（用于显示场地美术素材）
        [SerializeField] private Sprite areaSprite; // 场地 Sprite（如果设置，会自动应用到 SpriteRenderer）
        [SerializeField] private bool autoSizeFromSprite = true; // 是否根据 Sprite 自动设置场地大小
        [SerializeField] private int sortingOrder = -100; // 渲染排序顺序（数值越小越靠后，默认 -100 确保在最底层）
        [SerializeField] private string sortingLayerName = "Default"; // 排序图层名称

        [Header("可视化（可选）")]
        [SerializeField] private bool showGizmos = true; // 在 Scene 视图中显示场地边界
        [SerializeField] private Color gizmoColor = Color.green; // Gizmo 颜色

        /// <summary>
        /// 场地边界（只读属性）
        /// </summary>
        public Bounds AreaBounds
        {
            get
            {
                Vector2 center = useWorldBounds ? areaCenter : (Vector2)transform.position + areaCenter;
                return new Bounds(center, areaSize);
            }
        }

        /// <summary>
        /// 场地大小（像素单位，只读属性）
        /// </summary>
        public Vector2 AreaSizeInPixels
        {
            get
            {
                return WorldSizeToPixelSize(areaSize);
            }
            set
            {
                areaSize = PixelSizeToWorldSize(value);
                UpdateAreaSprite();
            }
        }

        /// <summary>
        /// 场地大小（世界单位，用于 Inspector 显示）
        /// </summary>
        public Vector2 AreaSize
        {
            get
            {
                if (usePixelUnits)
                {
                    // 如果使用像素单位，返回像素值（但实际存储的是世界单位）
                    return AreaSizeInPixels;
                }
                return areaSize;
            }
            set
            {
                if (usePixelUnits)
                {
                    // 如果使用像素单位，将输入的像素值转换为世界单位
                    areaSize = PixelSizeToWorldSize(value);
                }
                else
                {
                    areaSize = value;
                }
                UpdateAreaSprite();
            }
        }

        /// <summary>
        /// 场地最小 X 坐标
        /// </summary>
        public float MinX => AreaBounds.min.x;

        /// <summary>
        /// 场地最大 X 坐标
        /// </summary>
        public float MaxX => AreaBounds.max.x;

        /// <summary>
        /// 场地最小 Y 坐标
        /// </summary>
        public float MinY => AreaBounds.min.y;

        /// <summary>
        /// 场地最大 Y 坐标
        /// </summary>
        public float MaxY => AreaBounds.max.y;

        /// <summary>
        /// 鸡活动范围边界（只读属性）
        /// </summary>
        public Bounds ChickenMovementBounds
        {
            get
            {
                // 如果启用了自定义鸡活动范围，使用它；否则使用场地边界
                Vector2 movementSize;
                Vector2 movementCenter;
                
                if (useCustomChickenArea)
                {
                    movementSize = chickenMovementAreaSize != Vector2.zero ? chickenMovementAreaSize : areaSize;
                    movementCenter = chickenMovementAreaCenter != Vector2.zero ? chickenMovementAreaCenter : areaCenter;
                }
                else
                {
                    movementSize = areaSize;
                    movementCenter = areaCenter;
                }
                
                if (!useWorldBounds)
                {
                    movementCenter = (Vector2)transform.position + movementCenter;
                }
                
                return new Bounds(movementCenter, movementSize);
            }
        }

        /// <summary>
        /// 鸡活动范围最小 X 坐标
        /// </summary>
        public float ChickenMinX => ChickenMovementBounds.min.x;

        /// <summary>
        /// 鸡活动范围最大 X 坐标
        /// </summary>
        public float ChickenMaxX => ChickenMovementBounds.max.x;

        /// <summary>
        /// 鸡活动范围最小 Y 坐标
        /// </summary>
        public float ChickenMinY => ChickenMovementBounds.min.y;

        /// <summary>
        /// 鸡活动范围最大 Y 坐标
        /// </summary>
        public float ChickenMaxY => ChickenMovementBounds.max.y;

        /// <summary>
        /// 检查位置是否在场地内
        /// </summary>
        /// <param name="position">要检查的位置</param>
        /// <param name="padding">边界内边距（可选）</param>
        /// <returns>是否在场地内</returns>
        public bool IsPositionInArea(Vector2 position, float padding = 0f)
        {
            Bounds bounds = AreaBounds;
            return position.x >= bounds.min.x + padding &&
                   position.x <= bounds.max.x - padding &&
                   position.y >= bounds.min.y + padding &&
                   position.y <= bounds.max.y - padding;
        }

        /// <summary>
        /// 将位置限制在场地区域内
        /// </summary>
        /// <param name="position">要限制的位置</param>
        /// <param name="padding">边界内边距（可选）</param>
        /// <returns>限制后的位置</returns>
        public Vector2 ClampPositionToArea(Vector2 position, float padding = 0f)
        {
            Bounds bounds = AreaBounds;
            return new Vector2(
                Mathf.Clamp(position.x, bounds.min.x + padding, bounds.max.x - padding),
                Mathf.Clamp(position.y, bounds.min.y + padding, bounds.max.y - padding)
            );
        }

        /// <summary>
        /// 获取场地内的随机位置
        /// </summary>
        /// <param name="padding">边界内边距（避免在边缘生成）</param>
        /// <param name="useChickenArea">是否使用鸡活动范围（true）还是场地范围（false）</param>
        /// <returns>随机位置</returns>
        public Vector2 GetRandomPositionInArea(float padding = 0.5f, bool useChickenArea = true)
        {
            Bounds bounds = useChickenArea ? ChickenMovementBounds : AreaBounds;
            return new Vector2(
                Random.Range(bounds.min.x + padding, bounds.max.x - padding),
                Random.Range(bounds.min.y + padding, bounds.max.y - padding)
            );
        }

        /// <summary>
        /// 检查位置是否在鸡活动范围内
        /// </summary>
        /// <param name="position">要检查的位置</param>
        /// <param name="padding">边界内边距（可选）</param>
        /// <returns>是否在鸡活动范围内</returns>
        public bool IsPositionInChickenArea(Vector2 position, float padding = 0f)
        {
            Bounds bounds = ChickenMovementBounds;
            return position.x >= bounds.min.x + padding &&
                   position.x <= bounds.max.x - padding &&
                   position.y >= bounds.min.y + padding &&
                   position.y <= bounds.max.y - padding;
        }

        /// <summary>
        /// 将位置限制在鸡活动范围内
        /// </summary>
        /// <param name="position">要限制的位置</param>
        /// <param name="padding">边界内边距（可选）</param>
        /// <returns>限制后的位置</returns>
        public Vector2 ClampPositionToChickenArea(Vector2 position, float padding = 0f)
        {
            Bounds bounds = ChickenMovementBounds;
            return new Vector2(
                Mathf.Clamp(position.x, bounds.min.x + padding, bounds.max.x - padding),
                Mathf.Clamp(position.y, bounds.min.y + padding, bounds.max.y - padding)
            );
        }

        private void Awake()
        {
            // 单例模式
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("PlayArea: 检测到多个实例，保留第一个。", this);
                return;
            }

            Instance = this;

            // 如果没有指定摄像机，尝试获取主摄像机
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            // 初始化场地素材
            InitializeAreaSprite();
        }

        private void Start()
        {
            // 确保场地素材已正确设置
            UpdateAreaSprite();
        }

        /// <summary>
        /// 初始化场地 Sprite
        /// </summary>
        private void InitializeAreaSprite()
        {
            // 如果没有手动指定 SpriteRenderer，尝试自动获取
            if (areaSpriteRenderer == null)
            {
                areaSpriteRenderer = GetComponent<SpriteRenderer>();
                
                // 如果还是没有，尝试从子对象获取
                if (areaSpriteRenderer == null)
                {
                    areaSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
                }
            }

            // 如果还是没有找到 SpriteRenderer，创建一个（用于显示场地）
            if (areaSpriteRenderer == null)
            {
                // 创建一个子对象用于显示场地
                GameObject spriteObj = new GameObject("AreaVisual");
                spriteObj.transform.SetParent(transform);
                spriteObj.transform.localPosition = Vector3.zero;
                spriteObj.transform.localRotation = Quaternion.identity;
                spriteObj.transform.localScale = Vector3.one;
                
                areaSpriteRenderer = spriteObj.AddComponent<SpriteRenderer>();
                
                Debug.Log("PlayArea: 自动创建了 SpriteRenderer 用于显示场地", this);
            }

            // 确保 SpriteRenderer 已启用
            if (areaSpriteRenderer != null)
            {
                areaSpriteRenderer.enabled = true;
            }

            // 如果设置了 Sprite，应用到 SpriteRenderer
            if (areaSprite != null && areaSpriteRenderer != null)
            {
                areaSpriteRenderer.sprite = areaSprite;
                Debug.Log($"PlayArea: 已设置场地 Sprite: {areaSprite.name}", this);
            }
            else if (areaSpriteRenderer != null && areaSpriteRenderer.sprite == null)
            {
                // 如果没有设置 Sprite，创建一个简单的白色占位符 Sprite
                areaSpriteRenderer.sprite = CreatePlaceholderSprite();
                Debug.LogWarning("PlayArea: 未设置场地 Sprite，已创建白色占位符。请在 Inspector 中设置 'Area Sprite' 以显示场地背景。", this);
            }

            // 设置 SpriteRenderer 的排序，确保在最底层
            if (areaSpriteRenderer != null)
            {
                areaSpriteRenderer.sortingOrder = sortingOrder;
                areaSpriteRenderer.sortingLayerName = sortingLayerName;
            }

            // 更新 Sprite 显示（会根据 autoSizeFromSprite 设置决定是缩放 Sprite 还是设置场地大小）
            UpdateAreaSprite();
        }

        /// <summary>
        /// 创建一个简单的白色占位符 Sprite（当没有设置场地 Sprite 时使用）
        /// </summary>
        private Sprite CreatePlaceholderSprite()
        {
            // 创建一个 1x1 的白色纹理
            int size = 1;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(1f, 1f, 1f, 0.3f); // 白色，30% 透明度
            }
            texture.SetPixels(pixels);
            texture.Apply();

            // 创建 Sprite（使用 100 pixels per unit，所以 1x1 纹理 = 0.01 世界单位）
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "PlaceholderAreaSprite";

            return sprite;
        }

        /// <summary>
        /// 更新场地 Sprite 显示
        /// </summary>
        private void UpdateAreaSprite()
        {
            if (areaSpriteRenderer == null)
            {
                Debug.LogWarning("PlayArea: areaSpriteRenderer 为空，无法更新场地显示。请确保已配置场地 Sprite Renderer。", this);
                return;
            }

            // 确保 SpriteRenderer 已启用
            if (!areaSpriteRenderer.enabled)
            {
                areaSpriteRenderer.enabled = true;
                Debug.Log("PlayArea: 已启用 SpriteRenderer", this);
            }

            // 确保 SpriteRenderer 在最底层
            areaSpriteRenderer.sortingOrder = sortingOrder;
            areaSpriteRenderer.sortingLayerName = sortingLayerName;

            // 如果启用了自动大小，根据 Sprite 设置场地大小
            if (autoSizeFromSprite && areaSpriteRenderer.sprite != null)
            {
                Sprite sprite = areaSpriteRenderer.sprite;
                // Sprite 的 bounds.size 是世界单位大小
                areaSize = sprite.bounds.size;
                // 保持原始缩放
                areaSpriteRenderer.transform.localScale = Vector3.one;
            }
            // 如果禁用了自动大小，根据场地大小调整 Sprite 缩放
            else if (!autoSizeFromSprite && areaSpriteRenderer.sprite != null)
            {
                Sprite sprite = areaSpriteRenderer.sprite;
                Vector2 spriteSize = sprite.bounds.size;
                
                // 计算缩放比例，使 Sprite 匹配场地大小
                if (spriteSize.x > 0 && spriteSize.y > 0)
                {
                    Vector3 scale = new Vector3(
                        areaSize.x / spriteSize.x,
                        areaSize.y / spriteSize.y,
                        1f
                    );
                    areaSpriteRenderer.transform.localScale = scale;
                    Debug.Log($"PlayArea: 缩放 Sprite 以匹配场地大小 {areaSize}，缩放比例: {scale}", this);
                }
            }
            else if (areaSpriteRenderer.sprite == null)
            {
                Debug.LogWarning("PlayArea: SpriteRenderer 没有 Sprite，无法显示场地。请设置 'Area Sprite' 或确保 SpriteRenderer 有 Sprite。", this);
            }

            // 设置 Sprite 位置为场地中心
            Vector2 center = useWorldBounds ? areaCenter : (Vector2)transform.position + areaCenter;
            areaSpriteRenderer.transform.position = new Vector3(center.x, center.y, transform.position.z);
        }

        /// <summary>
        /// 强制刷新场地显示（公共方法，可在 Inspector 中调用或通过代码调用）
        /// </summary>
        [ContextMenu("刷新场地显示")]
        public void RefreshAreaDisplay()
        {
            Debug.Log("PlayArea: 手动刷新场地显示", this);
            InitializeAreaSprite();
            UpdateAreaSprite();
        }

        /// <summary>
        /// 诊断场地配置（公共方法，可在 Inspector 中调用或通过代码调用）
        /// </summary>
        [ContextMenu("诊断场地配置")]
        public void DiagnoseAreaConfiguration()
        {
            Debug.Log("=== PlayArea 诊断信息 ===", this);
            Debug.Log($"场地大小: {areaSize} (世界单位)", this);
            Debug.Log($"场地中心: {areaCenter}", this);
            Debug.Log($"使用世界坐标: {useWorldBounds}", this);
            Debug.Log($"使用像素单位: {usePixelUnits}", this);
            Debug.Log($"自动大小从 Sprite: {autoSizeFromSprite}", this);
            Debug.Log($"排序顺序: {sortingOrder}", this);
            Debug.Log($"排序图层: {sortingLayerName}", this);
            
            if (areaSpriteRenderer == null)
            {
                Debug.LogError("❌ SpriteRenderer 为空！场地无法显示。", this);
            }
            else
            {
                Debug.Log($"✅ SpriteRenderer 已找到: {areaSpriteRenderer.name}", this);
                Debug.Log($"   - 已启用: {areaSpriteRenderer.enabled}", this);
                Debug.Log($"   - 排序顺序: {areaSpriteRenderer.sortingOrder}", this);
                Debug.Log($"   - 排序图层: {areaSpriteRenderer.sortingLayerName}", this);
                
                if (areaSpriteRenderer.sprite == null)
                {
                    Debug.LogWarning("⚠️ SpriteRenderer 没有 Sprite！场地将显示为白色占位符。", this);
                }
                else
                {
                    Debug.Log($"✅ Sprite 已设置: {areaSpriteRenderer.sprite.name}", this);
                    Debug.Log($"   - Sprite 大小: {areaSpriteRenderer.sprite.bounds.size} (世界单位)", this);
                }
                
                Debug.Log($"   - 位置: {areaSpriteRenderer.transform.position}", this);
                Debug.Log($"   - 缩放: {areaSpriteRenderer.transform.localScale}", this);
            }
            
            if (areaSprite == null)
            {
                Debug.LogWarning("⚠️ Area Sprite 未设置（Inspector 中的 'Area Sprite' 字段为空）", this);
            }
            else
            {
                Debug.Log($"✅ Area Sprite 已设置: {areaSprite.name}", this);
            }
            
            Bounds bounds = AreaBounds;
            Debug.Log($"场地边界: min({bounds.min.x}, {bounds.min.y}) 到 max({bounds.max.x}, {bounds.max.y})", this);
            
            if (mainCamera == null)
            {
                Debug.LogWarning("⚠️ 主摄像机未设置，像素单位转换可能不准确", this);
            }
            else
            {
                Debug.Log($"✅ 主摄像机已设置: {mainCamera.name}", this);
                Debug.Log($"   - 正交模式: {mainCamera.orthographic}", this);
                if (mainCamera.orthographic)
                {
                    Debug.Log($"   - 正交大小: {mainCamera.orthographicSize}", this);
                }
            }
            
            Debug.Log("=== 诊断完成 ===", this);
        }

        /// <summary>
        /// 将世界单位大小转换为像素大小
        /// </summary>
        /// <param name="worldSize">世界单位大小</param>
        /// <returns>像素大小</returns>
        private Vector2 WorldSizeToPixelSize(Vector2 worldSize)
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera == null || !mainCamera.orthographic)
            {
                // 如果没有正交摄像机，返回原始值（假设 1 世界单位 = 100 像素）
                return worldSize * 100f;
            }

            // 获取屏幕高度（像素）
            float screenHeight = Screen.height;
            
            // 计算像素到世界单位的转换比例
            // orthographicSize 是摄像机视口高度的一半（世界单位）
            // 所以：1 世界单位 = screenHeight / (2 * orthographicSize) 像素
            float pixelsPerUnit = screenHeight / (2f * mainCamera.orthographicSize);

            return new Vector2(
                worldSize.x * pixelsPerUnit,
                worldSize.y * pixelsPerUnit
            );
        }

        /// <summary>
        /// 将像素大小转换为世界单位大小
        /// </summary>
        /// <param name="pixelSize">像素大小</param>
        /// <returns>世界单位大小</returns>
        private Vector2 PixelSizeToWorldSize(Vector2 pixelSize)
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera == null || !mainCamera.orthographic)
            {
                // 如果没有正交摄像机，返回转换后的值（假设 1 世界单位 = 100 像素）
                return pixelSize / 100f;
            }

            // 获取屏幕高度（像素）
            float screenHeight = Screen.height;
            
            // 计算像素到世界单位的转换比例
            float pixelsPerUnit = screenHeight / (2f * mainCamera.orthographicSize);

            return new Vector2(
                pixelSize.x / pixelsPerUnit,
                pixelSize.y / pixelsPerUnit
            );
        }

        /// <summary>
        /// 在 Scene 视图中绘制场地边界（调试用，未选中时也显示）
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!showGizmos) return;
            DrawAreaGizmos();
        }

        /// <summary>
        /// 在 Scene 视图中绘制场地边界（调试用，选中时显示更明显的线条）
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;
            
            // 选中时用更亮的颜色绘制
            Color originalColor = gizmoColor;
            gizmoColor = new Color(originalColor.r * 1.5f, originalColor.g * 1.5f, originalColor.b * 1.5f, 1f);
            DrawAreaGizmos();
            gizmoColor = originalColor;
        }

        /// <summary>
        /// 绘制场地 Gizmos（辅助方法）
        /// </summary>
        private void DrawAreaGizmos()
        {
            Bounds bounds = AreaBounds;

            // 绘制场地矩形边界
            Gizmos.color = gizmoColor;
            Vector3 bottomLeft = new Vector3(bounds.min.x, bounds.min.y, 0f);
            Vector3 bottomRight = new Vector3(bounds.max.x, bounds.min.y, 0f);
            Vector3 topLeft = new Vector3(bounds.min.x, bounds.max.y, 0f);
            Vector3 topRight = new Vector3(bounds.max.x, bounds.max.y, 0f);

            // 绘制四条边
            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);

            // 绘制场地中心点
            Vector3 center = bounds.center;
            Gizmos.DrawWireSphere(center, 0.2f);

            // 如果鸡活动范围与场地范围不同，绘制鸡活动范围（黄色）
            Bounds chickenBounds = ChickenMovementBounds;
            if (chickenBounds.size != bounds.size || chickenBounds.center != bounds.center)
            {
                Gizmos.color = Color.yellow;
                Vector3 chickenBottomLeft = new Vector3(chickenBounds.min.x, chickenBounds.min.y, 0f);
                Vector3 chickenBottomRight = new Vector3(chickenBounds.max.x, chickenBounds.min.y, 0f);
                Vector3 chickenTopLeft = new Vector3(chickenBounds.min.x, chickenBounds.max.y, 0f);
                Vector3 chickenTopRight = new Vector3(chickenBounds.max.x, chickenBounds.max.y, 0f);

                // 绘制鸡活动范围的四条边（虚线效果，通过多次绘制实现）
                DrawDashedLine(chickenBottomLeft, chickenBottomRight);
                DrawDashedLine(chickenBottomRight, chickenTopRight);
                DrawDashedLine(chickenTopRight, chickenTopLeft);
                DrawDashedLine(chickenTopLeft, chickenBottomLeft);

                // 绘制鸡活动范围中心点
                Vector3 chickenCenter = chickenBounds.center;
                Gizmos.DrawWireSphere(chickenCenter, 0.15f);
            }
        }

        /// <summary>
        /// 绘制虚线（辅助方法，通过分段绘制实现虚线效果）
        /// </summary>
        private void DrawDashedLine(Vector3 start, Vector3 end)
        {
            int segments = 10;
            for (int i = 0; i < segments; i++)
            {
                if (i % 2 == 0) // 只绘制偶数段，形成虚线效果
                {
                    float t1 = (float)i / segments;
                    float t2 = (float)(i + 1) / segments;
                    Vector3 p1 = Vector3.Lerp(start, end, t1);
                    Vector3 p2 = Vector3.Lerp(start, end, t2);
                    Gizmos.DrawLine(p1, p2);
                }
            }
        }

        /// <summary>
        /// 动态更新场地设置（由 AreaUpgradeManager 调用）
        /// </summary>
        /// <param name="newSize">新的场地大小（世界单位）</param>
        /// <param name="newCenter">新的场地中心位置（世界坐标）</param>
        /// <param name="newSprite">新的场地 Sprite（可选）</param>
        /// <param name="chickenMovementSize">鸡活动范围大小（可选，如果为 Vector2.zero 则使用场地大小）</param>
        /// <param name="chickenMovementCenter">鸡活动范围中心（可选，如果为 Vector2.zero 则使用场地中心）</param>
        public void UpdateAreaSettings(Vector2 newSize, Vector2 newCenter, Sprite newSprite = null, Vector2 chickenMovementSize = default(Vector2), Vector2 chickenMovementCenter = default(Vector2))
        {
            // 更新场地大小
            if (newSize != Vector2.zero)
            {
                areaSize = newSize;
            }

            // 更新场地中心
            areaCenter = newCenter;

            // 更新鸡活动范围（如果提供）
            if (chickenMovementSize != default(Vector2))
            {
                chickenMovementAreaSize = chickenMovementSize;
                useCustomChickenArea = true; // 自动启用自定义鸡活动范围
            }
            if (chickenMovementCenter != default(Vector2))
            {
                chickenMovementAreaCenter = chickenMovementCenter;
                useCustomChickenArea = true; // 自动启用自定义鸡活动范围
            }

            // 更新 Sprite（如果提供且不为空）
            if (newSprite != null)
            {
                areaSprite = newSprite;
                if (areaSpriteRenderer != null)
                {
                    areaSpriteRenderer.sprite = areaSprite;
                }
            }
            else if (newSprite == null && areaSpriteRenderer != null)
            {
                // 如果传入 null，保持当前 Sprite 不变
                // 不执行任何操作
            }

            // 更新显示
            UpdateAreaSprite();
        }

        /// <summary>
        /// 设置鸡活动范围（公共方法，可在编辑器或代码中调用）
        /// </summary>
        /// <param name="size">活动范围大小（世界单位）</param>
        /// <param name="center">活动范围中心（世界坐标）</param>
        public void SetChickenMovementArea(Vector2 size, Vector2 center)
        {
            chickenMovementAreaSize = size;
            chickenMovementAreaCenter = center;
            useCustomChickenArea = true;
            
            Debug.Log($"PlayArea: 已设置鸡活动范围 - 大小: {size}, 中心: {center}", this);
        }

        /// <summary>
        /// 获取当前鸡活动范围设置（公共方法）
        /// </summary>
        /// <param name="size">输出：活动范围大小</param>
        /// <param name="center">输出：活动范围中心</param>
        public void GetChickenMovementArea(out Vector2 size, out Vector2 center)
        {
            size = chickenMovementAreaSize;
            center = chickenMovementAreaCenter;
        }

        /// <summary>
        /// 重置鸡活动范围（使用场地大小和中心）
        /// </summary>
        public void ResetChickenMovementArea()
        {
            useCustomChickenArea = false;
            chickenMovementAreaSize = Vector2.zero;
            chickenMovementAreaCenter = Vector2.zero;
            
            Debug.Log("PlayArea: 已重置鸡活动范围，使用场地大小和中心", this);
        }

        /// <summary>
        /// 将当前鸡活动范围设置应用到 AreaData（用于保存到资源）
        /// </summary>
        /// <param name="areaData">要更新的 AreaData 资源</param>
        public void ApplyChickenAreaToAreaData(AreaData areaData)
        {
            if (areaData == null)
            {
                Debug.LogError("PlayArea: AreaData 为空，无法应用鸡活动范围设置！", this);
                return;
            }

            // 使用反射来设置 AreaData 的私有字段
            var areaDataType = typeof(AreaData);
            var sizeField = areaDataType.GetField("chickenMovementAreaSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var centerField = areaDataType.GetField("chickenMovementAreaCenter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (sizeField != null && centerField != null)
            {
                sizeField.SetValue(areaData, chickenMovementAreaSize);
                centerField.SetValue(areaData, chickenMovementAreaCenter);
                
                // 标记资源为已修改
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(areaData);
                #endif
                
                Debug.Log($"PlayArea: 已将鸡活动范围设置应用到 AreaData: {areaData.AreaName}", this);
            }
            else
            {
                Debug.LogError("PlayArea: 无法访问 AreaData 的字段，请检查字段名称是否正确。", this);
            }
        }

        /// <summary>
        /// 在 Inspector 中修改值时调用（仅在编辑器中）
        /// </summary>
        private void OnValidate()
        {
            // 在编辑器中实时更新 Sprite 显示
            // 注意：OnValidate 在编辑器和运行时都会调用
            if (areaSpriteRenderer != null)
            {
                // 如果设置了 Sprite，应用到 SpriteRenderer
                if (areaSprite != null)
                {
                    areaSpriteRenderer.sprite = areaSprite;
                }

                // 更新 Sprite 显示
                UpdateAreaSprite();
            }
        }
    }
}

