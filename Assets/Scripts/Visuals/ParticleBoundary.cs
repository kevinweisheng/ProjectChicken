using UnityEngine;
using ProjectChicken.Systems;
using ProjectChicken.Units;

namespace ProjectChicken.Visuals
{
    /// <summary>
    /// 粒子边界：使用粒子特效显示边界，支持基于鸡数量的动态扩张
    /// 当鸡数量增加时，边界会扩张，呈现"撑破"效果
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class ParticleBoundary : MonoBehaviour
    {
        public static ParticleBoundary Instance { get; private set; }

        [Header("边界配置")]
        [Tooltip("基础边界大小（从 PlayArea 获取）")]
        [SerializeField] private Vector2 baseSize = new Vector2(10f, 10f);
        
        [Tooltip("基础边界中心（从 PlayArea 获取）")]
        [SerializeField] private Vector2 baseCenter = Vector2.zero;

        [Header("动态扩张配置")]
        [Tooltip("每只鸡增加的边界大小（世界单位）")]
        [SerializeField] private float expansionPerChicken = 0.1f;
        
        [Tooltip("最大扩张倍数（相对于基础大小）")]
        [SerializeField] private float maxExpansionMultiplier = 1.5f;
        
        [Tooltip("扩张速度（每秒）")]
        [SerializeField] private float expansionSpeed = 2f;

        [Header("粒子特效配置")]
        [Tooltip("粒子发射速率（每秒）")]
        [SerializeField] private float emissionRate = 50f;
        
        [Tooltip("粒子大小")]
        [SerializeField] private float particleSize = 0.1f;
        
        [Tooltip("粒子颜色")]
        [SerializeField] private Color particleColor = Color.white;
        
        [Tooltip("粒子生命周期（秒）")]
        [SerializeField] private float particleLifetime = 2f;

        [Header("碰撞效果")]
        [Tooltip("碰撞时触发的粒子爆发数量")]
        [SerializeField] private int collisionBurstCount = 10;
        
        [Tooltip("碰撞时的粒子颜色")]
        [SerializeField] private Color collisionColor = Color.yellow;

        private new ParticleSystem particleSystem; // 使用 new 关键字隐藏继承的成员
        private ParticleSystem.ShapeModule shapeModule;
        private ParticleSystem.EmissionModule emissionModule;
        private ParticleSystem.MainModule mainModule;
        
        // 4 条边的粒子系统（用于在边界上显示粒子）
        private ParticleSystem[] edgeParticleSystems = new ParticleSystem[4]; // 下、右、上、左
        private GameObject[] edgeObjects = new GameObject[4]; // 4 条边的 GameObject
        
        private Vector2 currentSize; // 当前边界大小
        private Vector2 targetSize; // 目标边界大小
        private bool boundsInitialized = false;

        private void Awake()
        {
            // 单例模式
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("ParticleBoundary: 检测到多个实例，保留第一个。", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // 获取主粒子系统组件（用于碰撞效果）
            particleSystem = GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                particleSystem = gameObject.AddComponent<ParticleSystem>();
            }

            // 初始化主粒子系统（用于碰撞效果）
            InitializeMainParticleSystem();
            
            // 创建 4 条边的粒子系统
            CreateEdgeParticleSystems();
        }

        private void Start()
        {
            // 从 PlayArea 获取边界
            UpdateBoundsFromPlayArea();
        }

        private void Update()
        {
            // 每帧更新边界（如果 PlayArea 发生变化）
            UpdateBoundsFromPlayArea();

            // 根据鸡数量计算目标大小
            CalculateTargetSize();

            // 平滑过渡到目标大小
            UpdateBoundarySize();
        }

        /// <summary>
        /// 初始化主粒子系统（用于碰撞效果）
        /// </summary>
        private void InitializeMainParticleSystem()
        {
            if (particleSystem == null) return;

            // 获取模块
            mainModule = particleSystem.main;
            emissionModule = particleSystem.emission;

            // 配置主模块
            mainModule.startLifetime = particleLifetime * 0.5f; // 碰撞效果持续时间较短
            mainModule.startSize = particleSize * 1.5f; // 碰撞粒子稍大
            mainModule.startColor = collisionColor;
            mainModule.maxParticles = 500;
            mainModule.simulationSpace = ParticleSystemSimulationSpace.World;

            // 默认不发射（只在碰撞时手动发射）
            emissionModule.rateOverTime = 0f;
            emissionModule.enabled = false; // 禁用自动发射

            Debug.Log("ParticleBoundary: 主粒子系统已初始化（用于碰撞效果）", this);
        }

        /// <summary>
        /// 创建 4 条边的粒子系统
        /// </summary>
        private void CreateEdgeParticleSystems()
        {
            string[] edgeNames = { "BottomEdge", "RightEdge", "TopEdge", "LeftEdge" };
            
            for (int i = 0; i < 4; i++)
            {
                // 创建边的 GameObject
                GameObject edgeObj = new GameObject(edgeNames[i]);
                edgeObj.transform.SetParent(transform);
                edgeObj.transform.localPosition = Vector3.zero;
                edgeObj.transform.localRotation = Quaternion.identity;
                edgeObj.transform.localScale = Vector3.one;
                
                edgeObjects[i] = edgeObj;
                
                // 添加粒子系统组件
                ParticleSystem edgePS = edgeObj.AddComponent<ParticleSystem>();
                edgeParticleSystems[i] = edgePS;
                
                // 配置粒子系统
                var main = edgePS.main;
                var emission = edgePS.emission;
                var shape = edgePS.shape;
                var renderer = edgePS.GetComponent<ParticleSystemRenderer>();
                
                // 主模块配置
                main.startLifetime = particleLifetime;
                main.startSize = particleSize;
                main.startColor = particleColor;
                main.maxParticles = 500;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.startSpeed = 0f; // 粒子不移动，只显示在边界上
                
                // 发射模块配置
                emission.rateOverTime = emissionRate / 4f; // 每条边分配 1/4 的发射速率
                emission.enabled = true;
                
                // 形状模块配置（使用 Rectangle，但设置很小的厚度，让粒子只在边界上显示）
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Rectangle;
                
                // 根据边的方向设置矩形大小和旋转
                // 使用很小的厚度，让粒子看起来只在边界上
                switch (i)
                {
                    case 0: // 下边（水平，从左到右）
                        shape.rotation = new Vector3(0f, 0f, 0f);
                        shape.scale = new Vector3(1f, 0.01f, 1f); // 很薄的矩形
                        break;
                    case 1: // 右边（垂直，从下到上）
                        shape.rotation = new Vector3(0f, 0f, 0f);
                        shape.scale = new Vector3(0.01f, 1f, 1f); // 很薄的矩形
                        break;
                    case 2: // 上边（水平，从右到左）
                        shape.rotation = new Vector3(0f, 0f, 0f);
                        shape.scale = new Vector3(1f, 0.01f, 1f); // 很薄的矩形
                        break;
                    case 3: // 左边（垂直，从上到下）
                        shape.rotation = new Vector3(0f, 0f, 0f);
                        shape.scale = new Vector3(0.01f, 1f, 1f); // 很薄的矩形
                        break;
                }
            }
            
            Debug.Log("ParticleBoundary: 已创建 4 条边的粒子系统", this);
        }

        /// <summary>
        /// 从 PlayArea 更新边界
        /// </summary>
        private void UpdateBoundsFromPlayArea()
        {
            if (PlayArea.Instance != null)
            {
                Bounds chickenBounds = PlayArea.Instance.ChickenMovementBounds;
                Vector2 newSize = chickenBounds.size;
                Vector2 newCenter = chickenBounds.center;

                // 如果边界发生变化，更新
                if (!boundsInitialized || newSize != baseSize || newCenter != baseCenter)
                {
                    baseSize = newSize;
                    baseCenter = newCenter;
                    boundsInitialized = true;

                    // 初始化当前大小
                    if (currentSize == Vector2.zero)
                    {
                        currentSize = baseSize;
                        targetSize = baseSize;
                    }

                    // 更新粒子系统位置
                    transform.position = new Vector3(newCenter.x, newCenter.y, transform.position.z);
                }
            }
            else if (!boundsInitialized)
            {
                // 如果没有 PlayArea，使用默认边界
                baseSize = new Vector2(10f, 10f);
                baseCenter = Vector2.zero;
                boundsInitialized = true;
                currentSize = baseSize;
                targetSize = baseSize;
                Debug.LogWarning("ParticleBoundary: PlayArea.Instance 为空，使用默认边界", this);
            }
        }

        /// <summary>
        /// 根据鸡数量计算目标大小
        /// </summary>
        private void CalculateTargetSize()
        {
            // 获取当前鸡的数量
            int chickenCount = 0;
            if (ChickenSpawner.Instance != null)
            {
                // 统计肥鸡数量
                ChickenUnit[] allChickens = FindObjectsByType<ChickenUnit>(FindObjectsSortMode.None);
                foreach (ChickenUnit chicken in allChickens)
                {
                    if (chicken != null && chicken.IsFat)
                    {
                        chickenCount++;
                    }
                }
            }

            // 计算扩张量：每只鸡增加一定大小
            float expansionAmount = chickenCount * expansionPerChicken;
            
            // 计算目标大小（基础大小 + 扩张量）
            targetSize = baseSize + Vector2.one * expansionAmount;
            
            // 限制最大扩张
            Vector2 maxSize = baseSize * maxExpansionMultiplier;
            targetSize = Vector2.Min(targetSize, maxSize);
        }

        /// <summary>
        /// 更新边界大小（平滑过渡）
        /// </summary>
        private void UpdateBoundarySize()
        {
            if (!boundsInitialized) return;

            // 平滑过渡到目标大小
            currentSize = Vector2.Lerp(currentSize, targetSize, Time.deltaTime * expansionSpeed);

            // 更新 4 条边的粒子系统位置和大小
            UpdateEdgeParticleSystems();
        }

        /// <summary>
        /// 更新 4 条边的粒子系统位置和大小
        /// </summary>
        private void UpdateEdgeParticleSystems()
        {
            if (edgeParticleSystems == null || edgeObjects == null) return;

            Vector2 halfSize = currentSize * 0.5f;
            
            // 下边（水平，从左到右）
            if (edgeObjects[0] != null && edgeParticleSystems[0] != null)
            {
                edgeObjects[0].transform.position = new Vector3(
                    baseCenter.x,
                    baseCenter.y - halfSize.y,
                    transform.position.z
                );
                var shape = edgeParticleSystems[0].shape;
                shape.scale = new Vector3(currentSize.x, 0.01f, 1f); // 宽度 = 边界宽度，厚度很小
            }
            
            // 右边（垂直，从下到上）
            if (edgeObjects[1] != null && edgeParticleSystems[1] != null)
            {
                edgeObjects[1].transform.position = new Vector3(
                    baseCenter.x + halfSize.x,
                    baseCenter.y,
                    transform.position.z
                );
                var shape = edgeParticleSystems[1].shape;
                shape.scale = new Vector3(0.01f, currentSize.y, 1f); // 高度 = 边界高度，厚度很小
            }
            
            // 上边（水平，从右到左）
            if (edgeObjects[2] != null && edgeParticleSystems[2] != null)
            {
                edgeObjects[2].transform.position = new Vector3(
                    baseCenter.x,
                    baseCenter.y + halfSize.y,
                    transform.position.z
                );
                var shape = edgeParticleSystems[2].shape;
                shape.scale = new Vector3(currentSize.x, 0.01f, 1f); // 宽度 = 边界宽度，厚度很小
            }
            
            // 左边（垂直，从上到下）
            if (edgeObjects[3] != null && edgeParticleSystems[3] != null)
            {
                edgeObjects[3].transform.position = new Vector3(
                    baseCenter.x - halfSize.x,
                    baseCenter.y,
                    transform.position.z
                );
                var shape = edgeParticleSystems[3].shape;
                shape.scale = new Vector3(0.01f, currentSize.y, 1f); // 高度 = 边界高度，厚度很小
            }
        }

        /// <summary>
        /// 在碰撞位置触发粒子爆发效果
        /// </summary>
        /// <param name="hitPosition">碰撞位置（世界坐标）</param>
        public void TriggerCollisionEffect(Vector2 hitPosition)
        {
            if (particleSystem == null) return;

            // 创建临时粒子爆发
            ParticleSystem.Burst burst = new ParticleSystem.Burst(
                0f, // 时间
                collisionBurstCount // 粒子数量
            );

            // 在碰撞位置发射粒子
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
            emitParams.position = hitPosition;
            emitParams.startColor = collisionColor;
            emitParams.startSize = particleSize * 1.5f; // 稍大一些
            emitParams.startLifetime = particleLifetime * 0.5f; // 稍短一些

            particleSystem.Emit(emitParams, collisionBurstCount);

            Debug.Log($"ParticleBoundary: 在位置 {hitPosition} 触发碰撞效果，发射 {collisionBurstCount} 个粒子", this);
        }

        /// <summary>
        /// 获取当前边界大小
        /// </summary>
        public Vector2 GetCurrentSize()
        {
            return currentSize;
        }

        /// <summary>
        /// 获取当前边界中心
        /// </summary>
        public Vector2 GetCurrentCenter()
        {
            return baseCenter;
        }

        /// <summary>
        /// 获取当前边界（Bounds）
        /// </summary>
        public Bounds GetBounds()
        {
            return new Bounds(new Vector3(baseCenter.x, baseCenter.y, 0f), new Vector3(currentSize.x, currentSize.y, 0f));
        }

        /// <summary>
        /// 检查位置是否在边界内
        /// </summary>
        public bool IsPositionInside(Vector2 position)
        {
            Bounds bounds = GetBounds();
            return bounds.Contains(new Vector3(position.x, position.y, bounds.center.z));
        }

        /// <summary>
        /// 在 Scene 视图中绘制边界（调试用）
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!boundsInitialized) return;

            // 绘制当前边界
            Gizmos.color = Color.cyan;
            Vector3 center3D = new Vector3(baseCenter.x, baseCenter.y, transform.position.z);
            Vector3 size3D = new Vector3(currentSize.x, currentSize.y, 0f);

            // 绘制矩形的4条边
            Vector3 bottomLeft = center3D + new Vector3(-size3D.x * 0.5f, -size3D.y * 0.5f, 0f);
            Vector3 bottomRight = center3D + new Vector3(size3D.x * 0.5f, -size3D.y * 0.5f, 0f);
            Vector3 topLeft = center3D + new Vector3(-size3D.x * 0.5f, size3D.y * 0.5f, 0f);
            Vector3 topRight = center3D + new Vector3(size3D.x * 0.5f, size3D.y * 0.5f, 0f);

            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);

            // 绘制基础边界（如果不同）
            if (currentSize != baseSize)
            {
                Gizmos.color = Color.yellow;
                Vector3 baseSize3D = new Vector3(baseSize.x, baseSize.y, 0f);
                Vector3 baseBottomLeft = center3D + new Vector3(-baseSize3D.x * 0.5f, -baseSize3D.y * 0.5f, 0f);
                Vector3 baseBottomRight = center3D + new Vector3(baseSize3D.x * 0.5f, -baseSize3D.y * 0.5f, 0f);
                Vector3 baseTopLeft = center3D + new Vector3(-baseSize3D.x * 0.5f, baseSize3D.y * 0.5f, 0f);
                Vector3 baseTopRight = center3D + new Vector3(baseSize3D.x * 0.5f, baseSize3D.y * 0.5f, 0f);

                Gizmos.DrawLine(baseBottomLeft, baseBottomRight);
                Gizmos.DrawLine(baseBottomRight, baseTopRight);
                Gizmos.DrawLine(baseTopRight, baseTopLeft);
                Gizmos.DrawLine(baseTopLeft, baseBottomLeft);
            }

            // 绘制中心点
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(center3D, 0.2f);
        }
    }
}
