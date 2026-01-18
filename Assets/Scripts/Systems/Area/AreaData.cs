using UnityEngine;

namespace ProjectChicken.Systems
{
    /// <summary>
    /// 场地升级类型
    /// </summary>
    public enum AreaUpgradeType
    {
        Expand,         // 场地尺寸扩大
        FractalZoom     // 摄像机拉远，穿过云层，更换场地
    }

    /// <summary>
    /// 场地数据：定义场地的配置信息
    /// </summary>
    [CreateAssetMenu(fileName = "NewArea", menuName = "ProjectChicken/Area Data")]
    public class AreaData : ScriptableObject
    {
        [Header("基本信息")]
        [Tooltip("场地名称（例如：\"鸡舍\", \"农场\"）")]
        [SerializeField] private string areaName;

        [Header("场地配置")]
        [Tooltip("场地大小（世界单位）")]
        [SerializeField] private Vector2 areaSize = new Vector2(10f, 10f);

        [Tooltip("场地中心位置（世界坐标）")]
        [SerializeField] private Vector2 areaCenter = Vector2.zero;

        [Header("鸡活动范围配置")]
        [Tooltip("鸡的活动范围大小（世界单位）。如果为 (0,0)，则使用场地大小。可以设置得比场地小，限制鸡的活动区域")]
        [SerializeField] private Vector2 chickenMovementAreaSize = Vector2.zero;

        [Tooltip("鸡的活动范围中心位置（世界坐标）。如果场地中心与活动范围中心不同，可以在此设置")]
        [SerializeField] private Vector2 chickenMovementAreaCenter = Vector2.zero;

        [Header("美术素材")]
        [Tooltip("场地背景 Sprite")]
        [SerializeField] private Sprite backgroundSprite;

        [Header("升级配置")]
        [Tooltip("升级类型：Expand（扩大）或 FractalZoom（分形缩放）")]
        [SerializeField] private AreaUpgradeType upgradeType = AreaUpgradeType.Expand;

        [Tooltip("触发升级所需的鸡数量阈值（当鸡数量超过此值时触发升级）")]
        [SerializeField] private int chickenCountThreshold = 10;

        [Header("摄像机约束（用于 FractalZoom 类型）")]
        [Tooltip("摄像机最小尺寸")]
        [SerializeField] private float minCamSize = 5f;

        [Tooltip("摄像机最大尺寸")]
        [SerializeField] private float maxCamSize = 15f;

        [Tooltip("摄像机平移限制（+/- X 和 Y）")]
        [SerializeField] private Vector2 movementBounds = Vector2.zero;

        [Header("肥鸡生命值等级")]
        [Tooltip("该场地等级对应的肥鸡生命值等级（0-5，共6个等级），用于匹配下蛋数量配置")]
        [Range(0, 5)]
        [SerializeField] private int chickenHealthLevel = 0;

        // 公共属性
        public string AreaName => areaName;
        public Vector2 AreaSize => areaSize;
        public Vector2 AreaCenter => areaCenter;
        public Sprite BackgroundSprite => backgroundSprite;
        public AreaUpgradeType UpgradeType => upgradeType;
        public int ChickenCountThreshold => chickenCountThreshold;
        public float MinCamSize => minCamSize;
        public float MaxCamSize => maxCamSize;
        public Vector2 MovementBounds => movementBounds;
        public Vector2 ChickenMovementAreaSize => chickenMovementAreaSize;
        public Vector2 ChickenMovementAreaCenter => chickenMovementAreaCenter;
        public int ChickenHealthLevel => chickenHealthLevel;
    }
}

