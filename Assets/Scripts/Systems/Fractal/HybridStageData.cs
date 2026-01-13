using UnityEngine;

namespace ProjectChicken.Systems
{
    /// <summary>
    /// 阶段过渡类型枚举
    /// </summary>
    public enum StageTransitionType
    {
        Expansion,      // 扩展：摄像机边界变大
        FractalZoom     // 分形缩放：重置缩放，加载新世界
    }

    /// <summary>
    /// 混合阶段数据：用于"分形宇宙"进度系统
    /// 阶段可以"扩展"（摄像机边界变大）或"分形缩放"（重置缩放，加载新世界）
    /// </summary>
    [CreateAssetMenu(fileName = "NewStage", menuName = "ProjectChicken/Fractal Stage")]
    public class HybridStageData : ScriptableObject
    {
        [Header("阶段基本信息")]
        [Tooltip("阶段名称（例如：\"Coop\", \"Farm\"）")]
        [SerializeField] private string stageName;

        [Header("过渡类型")]
        [Tooltip("决定下一个阶段的逻辑类型")]
        [SerializeField] private StageTransitionType transitionType;

        [Header("环境预制体")]
        [Tooltip("视觉背景/网格（仅视觉效果）")]
        [SerializeField] private GameObject environmentPrefab;

        [Header("分形缩放图标（可选）")]
        [Tooltip("仅在 TransitionType == FractalZoom 时使用。此精灵代表当前阶段在下一个阶段世界中的样子")]
        [SerializeField] private Sprite previousStageIcon;

        [Header("摄像机约束")]
        [Tooltip("摄像机最小尺寸（默认 5）")]
        [SerializeField] private float minCamSize = 5f;

        [Tooltip("摄像机最大尺寸（触发下一个阶段的点）")]
        [SerializeField] private float maxCamSize;

        [Tooltip("摄像机平移的 +/- X 和 Y 限制")]
        [SerializeField] private Vector2 movementBounds;

        [Header("场地配置（PlayArea）")]
        [Tooltip("场地大小（世界单位）。如果为 (0,0)，则使用环境预制体中的 PlayArea 设置")]
        [SerializeField] private Vector2 playAreaSize = Vector2.zero;

        [Tooltip("场地中心位置（世界坐标）。如果环境预制体包含 PlayArea，此设置将被忽略")]
        [SerializeField] private Vector2 playAreaCenter = Vector2.zero;

        [Tooltip("场地背景 Sprite（可选）。如果设置，将应用到 PlayArea 的 SpriteRenderer")]
        [SerializeField] private Sprite playAreaSprite;

        // 公共属性（只读）
        public string StageName => stageName;
        public StageTransitionType TransitionType => transitionType;
        public GameObject EnvironmentPrefab => environmentPrefab;
        public Sprite PreviousStageIcon => previousStageIcon;
        public float MinCamSize => minCamSize;
        public float MaxCamSize => maxCamSize;
        public Vector2 MovementBounds => movementBounds;
        public Vector2 PlayAreaSize => playAreaSize;
        public Vector2 PlayAreaCenter => playAreaCenter;
        public Sprite PlayAreaSprite => playAreaSprite;
    }
}

