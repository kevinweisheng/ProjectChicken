using UnityEngine;
using ProjectChicken.Systems;

namespace ProjectChicken.Visuals
{
    /// <summary>
    /// 软绳边界：使用 LineRenderer 绘制动态矩形边界，当鸡碰撞时产生变形效果
    /// 矩形边界与 PlayArea 的鸡活动范围相同
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class RopeBoundary : MonoBehaviour
    {
        public static RopeBoundary Instance { get; private set; }

        [Header("边界配置")]
        [Tooltip("每条边的顶点数量。\n" +
                 "• 值越大：绳子越平滑，变形效果越细腻，但性能开销越大\n" +
                 "• 值越小：绳子越粗糙，但性能更好\n" +
                 "• 推荐值：8-32（默认16）\n" +
                 "• 总顶点数 = pointsPerSide × 4（4条边）")]
        [SerializeField] private int pointsPerSide = 16;

        [Header("物理参数")]
        [Tooltip("弹簧刚度（恢复力强度）。\n" +
                 "• 值越大：绳子越硬，变形后恢复越快，弹性越强\n" +
                 "• 值越小：绳子越软，变形后恢复越慢，更柔软\n" +
                 "• 推荐值：20-100（默认30，降低以增强形变可见性）\n" +
                 "• 如果绳子恢复太快，减小此值\n" +
                 "• 如果绳子太软、恢复太慢，增大此值")]
        [SerializeField] private float stiffness = 30f; // 从50降低到30，让形变更持久

        [Tooltip("阻尼系数（速度衰减）。\n" +
                 "• 范围：0-1\n" +
                 "• 值越大（接近1）：速度衰减越快，摆动停止越快，更稳定\n" +
                 "• 值越小（接近0）：速度衰减越慢，摆动持续越久，更弹性\n" +
                 "• 推荐值：0.7-0.95（默认0.85，降低以增强形变可见性）\n" +
                 "• 如果绳子摆动太久，增大此值\n" +
                 "• 如果绳子立即停止、没有弹性感，减小此值")]
        [SerializeField] private float damping = 0.85f; // 从0.9降低到0.85，让形变更持久

        [Tooltip("最大变形距离（世界单位）。\n" +
                 "• 限制绳子顶点可以偏离原始位置的最大距离\n" +
                 "• 值越大：允许更大的变形，但可能看起来不自然\n" +
                 "• 值越小：变形更小，更稳定\n" +
                 "• 推荐值：0.2-1.0（默认1.0，增大以增强形变可见性）\n" +
                 "• 如果绳子变形太大、看起来奇怪，减小此值\n" +
                 "• 如果碰撞时变形不明显，增大此值")]
        [SerializeField] private float maxDeformation = 1.0f; // 从0.5增加到1.0，允许更大的形变

        [Header("视觉效果")]
        [Tooltip("线条宽度（世界单位）。\n" +
                 "• 控制绳子的粗细\n" +
                 "• 推荐值：0.05-0.2（默认0.1）")]
        [SerializeField] private float lineWidth = 0.1f;

        [Tooltip("线条材质（可选）。\n" +
                 "• 如果为空，使用默认材质\n" +
                 "• 可以设置自定义材质来改变绳子的外观（例如：发光、纹理等）")]
        [SerializeField] private Material lineMaterial;

        [Tooltip("线条颜色。\n" +
                 "• 控制绳子的颜色\n" +
                 "• 如果材质支持颜色，此颜色会应用到材质上")]
        [SerializeField] private Color lineColor = Color.white;

        private LineRenderer lineRenderer;
        private Vector2[] ropePoints; // 绳子的顶点位置（本地坐标）
        private Vector2[] ropeVelocities; // 绳子的顶点速度
        private Vector2[] baseRectanglePoints; // 基础矩形位置（目标位置）
        private int totalPointCount; // 总顶点数（4条边）

        // 矩形边界信息（从 PlayArea 获取）
        private Bounds rectangleBounds;
        private bool boundsInitialized = false;

        private void Awake()
        {
            // 单例模式
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("RopeBoundary: 检测到多个实例，保留第一个。", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // 获取 LineRenderer 组件
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            // 初始化 LineRenderer
            InitializeLineRenderer();
        }

        private void Start()
        {
            // 从 PlayArea 获取矩形边界
            UpdateBoundsFromPlayArea();
            
            // 初始化绳子顶点
            InitializeRopePoints();
        }

        private void Update()
        {
            // 每帧更新边界（如果 PlayArea 发生变化）
            UpdateBoundsFromPlayArea();

            // 更新基础矩形位置
            UpdateBaseRectanglePoints();

            // 更新绳子物理模拟
            UpdateRopePhysics();

            // 更新 LineRenderer 位置
            UpdateLineRenderer();
        }

        /// <summary>
        /// 初始化 LineRenderer
        /// </summary>
        private void InitializeLineRenderer()
        {
            if (lineRenderer == null) return;

            // 计算总顶点数（4条边，每条边 pointsPerSide 个顶点）
            totalPointCount = pointsPerSide * 4;

            lineRenderer.loop = true;
            lineRenderer.useWorldSpace = false; // 使用本地坐标
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.positionCount = totalPointCount;

            // 设置材质
            if (lineMaterial != null)
            {
                lineRenderer.material = lineMaterial;
            }

            // 设置颜色
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;
        }

        /// <summary>
        /// 从 PlayArea 更新矩形边界
        /// </summary>
        private void UpdateBoundsFromPlayArea()
        {
            if (PlayArea.Instance != null)
            {
                Bounds newBounds = PlayArea.Instance.ChickenMovementBounds;
                
                // 如果边界发生变化，重新初始化
                if (!boundsInitialized || newBounds != rectangleBounds)
                {
                    rectangleBounds = newBounds;
                    boundsInitialized = true;
                    
                    // 如果已经初始化了顶点，重新计算基础位置
                    if (ropePoints != null)
                    {
                        UpdateBaseRectanglePoints();
                    }
                }
            }
            else if (!boundsInitialized)
            {
                // 如果没有 PlayArea，使用默认边界
                rectangleBounds = new Bounds(Vector3.zero, new Vector3(10f, 10f, 0f));
                boundsInitialized = true;
                Debug.LogWarning("RopeBoundary: PlayArea.Instance 为空，使用默认边界", this);
            }
        }

        /// <summary>
        /// 初始化绳子顶点
        /// </summary>
        private void InitializeRopePoints()
        {
            totalPointCount = pointsPerSide * 4;
            ropePoints = new Vector2[totalPointCount];
            ropeVelocities = new Vector2[totalPointCount];
            baseRectanglePoints = new Vector2[totalPointCount];

            // 更新基础矩形位置
            UpdateBaseRectanglePoints();

            // 初始化绳子顶点位置为矩形
            for (int i = 0; i < totalPointCount; i++)
            {
                ropePoints[i] = baseRectanglePoints[i];
                ropeVelocities[i] = Vector2.zero;
            }
        }

        /// <summary>
        /// 更新基础矩形位置（目标位置）
        /// </summary>
        private void UpdateBaseRectanglePoints()
        {
            if (!boundsInitialized) return;

            // 将世界坐标转换为本地坐标（相对于 transform.position）
            // 如果 transform.position 不在原点，需要调整
            Vector3 worldCenter = rectangleBounds.center;
            Vector3 localCenter = transform.InverseTransformPoint(worldCenter);
            Vector2 localSize = rectangleBounds.size;
            Vector2 localMin = new Vector2(localCenter.x - localSize.x * 0.5f, localCenter.y - localSize.y * 0.5f);
            Vector2 localMax = new Vector2(localCenter.x + localSize.x * 0.5f, localCenter.y + localSize.y * 0.5f);

            // 计算4条边的顶点位置
            // 下边（从左到右）
            for (int i = 0; i < pointsPerSide; i++)
            {
                float t = (float)i / (pointsPerSide - 1);
                baseRectanglePoints[i] = new Vector2(
                    Mathf.Lerp(localMin.x, localMax.x, t),
                    localMin.y
                );
            }

            // 右边（从下到上）
            for (int i = 0; i < pointsPerSide; i++)
            {
                float t = (float)i / (pointsPerSide - 1);
                baseRectanglePoints[pointsPerSide + i] = new Vector2(
                    localMax.x,
                    Mathf.Lerp(localMin.y, localMax.y, t)
                );
            }

            // 上边（从右到左）
            for (int i = 0; i < pointsPerSide; i++)
            {
                float t = (float)i / (pointsPerSide - 1);
                baseRectanglePoints[pointsPerSide * 2 + i] = new Vector2(
                    Mathf.Lerp(localMax.x, localMin.x, t),
                    localMax.y
                );
            }

            // 左边（从上到下）
            for (int i = 0; i < pointsPerSide; i++)
            {
                float t = (float)i / (pointsPerSide - 1);
                baseRectanglePoints[pointsPerSide * 3 + i] = new Vector2(
                    localMin.x,
                    Mathf.Lerp(localMax.y, localMin.y, t)
                );
            }
        }

        /// <summary>
        /// 更新绳子物理模拟（弹簧质量系统）
        /// </summary>
        private void UpdateRopePhysics()
        {
            if (ropePoints == null || ropeVelocities == null || baseRectanglePoints == null)
            {
                return;
            }

            float deltaTime = Time.deltaTime;

            for (int i = 0; i < totalPointCount; i++)
            {
                // 计算弹簧力（Hooke's Law）：F = -k * (x - x0)
                Vector2 targetPos = baseRectanglePoints[i];
                Vector2 currentPos = ropePoints[i];
                Vector2 displacement = targetPos - currentPos;
                Vector2 force = displacement * stiffness;

                // 更新速度：v += F * dt
                ropeVelocities[i] += force * deltaTime;

                // 应用阻尼：v *= damping
                ropeVelocities[i] *= damping;

                // 更新位置：x += v * dt
                ropePoints[i] += ropeVelocities[i] * deltaTime;

                // 限制最大变形（防止过度变形）
                float distanceFromBase = Vector2.Distance(ropePoints[i], targetPos);
                if (distanceFromBase > maxDeformation)
                {
                    Vector2 direction = (ropePoints[i] - targetPos).normalized;
                    ropePoints[i] = targetPos + direction * maxDeformation;
                    // 重置该点的速度，防止继续变形
                    ropeVelocities[i] *= 0.5f;
                }
            }
        }

        /// <summary>
        /// 更新 LineRenderer 位置
        /// </summary>
        private void UpdateLineRenderer()
        {
            if (lineRenderer == null || ropePoints == null)
            {
                return;
            }

            // 直接使用本地坐标（因为 useWorldSpace = false）
            for (int i = 0; i < totalPointCount; i++)
            {
                lineRenderer.SetPosition(i, new Vector3(ropePoints[i].x, ropePoints[i].y, 0f));
            }
        }

        /// <summary>
        /// 应用力到绳子（当鸡碰撞边界时调用）
        /// </summary>
        /// <param name="hitPosition">碰撞位置（世界坐标）</param>
        /// <param name="force">力的大小</param>
        public void ApplyForce(Vector2 hitPosition, float force)
        {
            if (ropePoints == null || ropeVelocities == null)
            {
                return;
            }

            // 将世界坐标转换为本地坐标
            Vector2 localHitPos = transform.InverseTransformPoint(hitPosition);

            // 找到最近的顶点
            int nearestIndex = 0;
            float minDistance = float.MaxValue;

            for (int i = 0; i < totalPointCount; i++)
            {
                float distance = Vector2.Distance(localHitPos, ropePoints[i]);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestIndex = i;
                }
            }

            // 计算力的方向（始终指向边界外，即向外推）
            // 从基础位置（边界上的点）指向碰撞点的方向
            Vector2 toHit = localHitPos - baseRectanglePoints[nearestIndex];
            Vector2 direction = Vector2.zero;
            
            // 根据顶点索引判断是哪条边，确定向外推的方向
            int sideIndex = nearestIndex / pointsPerSide;
            switch (sideIndex)
            {
                case 0: // 下边 - 向下推（向外，远离边界）
                    direction = Vector2.down;
                    break;
                case 1: // 右边 - 向右推（向外，远离边界）
                    direction = Vector2.right;
                    break;
                case 2: // 上边 - 向上推（向外，远离边界）
                    direction = Vector2.up;
                    break;
                case 3: // 左边 - 向左推（向外，远离边界）
                    direction = Vector2.left;
                    break;
            }
            
            // 如果碰撞点在边界内（toHit指向边界内），需要反转方向
            // 检查 toHit 是否指向边界内（与向外方向相反）
            if (toHit.magnitude > 0.01f)
            {
                float dot = Vector2.Dot(toHit.normalized, direction);
                // 如果点积为负，说明 toHit 指向边界内，需要反转方向
                if (dot < 0f)
                {
                    direction = -direction; // 反转方向，确保向外推
                }
            }

            // 应用力到最近的顶点及其相邻顶点（产生更自然的变形）
            // 增大影响范围，使变形效果更明显
            int influenceRange = Mathf.Max(3, pointsPerSide / 2); // 影响范围：约1/2边的顶点（从1/3改为1/2）

            for (int offset = -influenceRange; offset <= influenceRange; offset++)
            {
                int index = (nearestIndex + offset + totalPointCount) % totalPointCount;
                // 使用更平滑的衰减曲线（平方衰减，而不是线性）
                float normalizedOffset = Mathf.Abs(offset) / (float)(influenceRange + 1);
                float influence = 1f - (normalizedOffset * normalizedOffset); // 平方衰减，中心点影响更大
                
                // 应用力（影响系数随距离衰减）
                // 大幅增大力的应用，确保有可见的变形效果
                ropeVelocities[index] += direction * force * influence * 3.0f; // 从1.5倍增加到3.0倍
            }

            Debug.Log($"RopeBoundary: 在位置 {hitPosition} 应用力 {force}，影响顶点 {nearestIndex}，影响范围: {influenceRange}", this);
        }

        /// <summary>
        /// 设置矩形边界（从 PlayArea 调用）
        /// </summary>
        /// <param name="bounds">矩形边界</param>
        public void SetBounds(Bounds bounds)
        {
            rectangleBounds = bounds;
            boundsInitialized = true;
            UpdateBaseRectanglePoints();
            Debug.Log($"RopeBoundary: 设置矩形边界 - 中心: {bounds.center}, 大小: {bounds.size}", this);
        }

        /// <summary>
        /// 获取当前矩形边界
        /// </summary>
        public Bounds GetBounds()
        {
            return rectangleBounds;
        }

        /// <summary>
        /// 检查位置是否在边界内
        /// </summary>
        /// <param name="position">要检查的位置（世界坐标）</param>
        /// <returns>是否在边界内</returns>
        public bool IsPositionInside(Vector2 position)
        {
            return rectangleBounds.Contains(new Vector3(position.x, position.y, rectangleBounds.center.z));
        }

        /// <summary>
        /// 获取从边界到指定位置的方向（用于反弹计算）
        /// </summary>
        /// <param name="position">位置（世界坐标）</param>
        /// <returns>从边界指向位置的方向向量（归一化）</returns>
        public Vector2 GetDirectionFromBoundary(Vector2 position)
        {
            Vector3 pos3D = new Vector3(position.x, position.y, rectangleBounds.center.z);
            
            // 计算到矩形边界最近点的方向
            Vector3 closestPoint = rectangleBounds.ClosestPoint(pos3D);
            Vector2 direction = (position - new Vector2(closestPoint.x, closestPoint.y));
            
            if (direction.magnitude < 0.01f)
            {
                // 如果位置在边界内，返回从中心向外的方向
                Vector2 center2D = new Vector2(rectangleBounds.center.x, rectangleBounds.center.y);
                direction = (position - center2D);
            }
            
            if (direction.magnitude < 0.01f)
            {
                return Vector2.up; // 默认方向
            }
            
            return direction.normalized;
        }

        /// <summary>
        /// 在 Scene 视图中绘制边界（调试用）
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!boundsInitialized) return;

            // 绘制目标矩形边界
            Gizmos.color = Color.cyan;
            Vector3 center3D = rectangleBounds.center;
            Vector3 size3D = rectangleBounds.size;
            
            // 绘制矩形的4条边
            Vector3 bottomLeft = center3D + new Vector3(-size3D.x * 0.5f, -size3D.y * 0.5f, 0f);
            Vector3 bottomRight = center3D + new Vector3(size3D.x * 0.5f, -size3D.y * 0.5f, 0f);
            Vector3 topLeft = center3D + new Vector3(-size3D.x * 0.5f, size3D.y * 0.5f, 0f);
            Vector3 topRight = center3D + new Vector3(size3D.x * 0.5f, size3D.y * 0.5f, 0f);

            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);

            // 绘制中心点
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(center3D, 0.2f);
        }
    }
}
