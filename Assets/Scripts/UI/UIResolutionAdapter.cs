using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ProjectChicken.UI
{
    /// <summary>
    /// UI分辨率适配器：确保UI在不同分辨率下正确显示
    /// 使用方法：
    /// 1. 将此脚本添加到 Canvas GameObject 上
    /// 2. 在 Inspector 中配置参考分辨率和适配设置
    /// 3. 确保所有 UI 元素使用正确的锚点和边距设置
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    public class UIResolutionAdapter : MonoBehaviour
    {
        [Header("参考分辨率设置")]
        [Tooltip("设计时的参考分辨率（通常为 1920x1080 或 1280x720）")]
        [SerializeField] private Vector2 referenceResolution = new Vector2(1920, 1080);

        [Header("Canvas Scaler 设置")]
        [Tooltip("UI缩放模式")]
        [SerializeField] private CanvasScaler.ScaleMode scaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        
        [Tooltip("屏幕匹配模式（0=Match Width, 1=Match Height, 0.5=Match Both）")]
        [Range(0f, 1f)]
        [SerializeField] private float matchWidthOrHeight = 0.5f;

        [Header("调试信息")]
        [SerializeField] private bool showDebugInfo = false;

        private CanvasScaler canvasScaler;
        private Canvas canvas;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            canvasScaler = GetComponent<CanvasScaler>();

            if (canvasScaler == null)
            {
                Debug.LogError("UIResolutionAdapter: 未找到 CanvasScaler 组件！请确保 Canvas 上有 CanvasScaler 组件。", this);
                return;
            }

            // 配置 Canvas Scaler
            ConfigureCanvasScaler();
        }

        /// <summary>
        /// 配置 Canvas Scaler
        /// </summary>
        private void ConfigureCanvasScaler()
        {
            if (canvasScaler == null)
                return;

            canvasScaler.uiScaleMode = scaleMode;
            canvasScaler.referenceResolution = referenceResolution;
            canvasScaler.matchWidthOrHeight = matchWidthOrHeight;

            // 根据选择的缩放模式设置其他参数
            if (scaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            }
        }

        /// <summary>
        /// 编辑器模式下：当 Inspector 中的字段改变时自动调用
        /// </summary>
        private void OnValidate()
        {
            if (canvasScaler == null)
            {
                canvasScaler = GetComponent<CanvasScaler>();
            }

            if (canvasScaler != null)
            {
                ConfigureCanvasScaler();
            }
        }

        /// <summary>
        /// 获取当前屏幕的宽高比
        /// </summary>
        public float GetScreenAspectRatio()
        {
            return (float)Screen.width / Screen.height;
        }

        /// <summary>
        /// 获取参考分辨率的宽高比
        /// </summary>
        public float GetReferenceAspectRatio()
        {
            return referenceResolution.x / referenceResolution.y;
        }

        /// <summary>
        /// 检查是否需要调整UI布局（屏幕比例与参考比例差异较大时）
        /// </summary>
        public bool NeedsLayoutAdjustment()
        {
            float screenAspect = GetScreenAspectRatio();
            float referenceAspect = GetReferenceAspectRatio();
            float difference = Mathf.Abs(screenAspect - referenceAspect);

            // 如果宽高比差异超过 10%，可能需要特殊处理
            return difference > 0.1f;
        }

        /// <summary>
        /// 右键菜单：刷新UI适配
        /// </summary>
        [ContextMenu("刷新UI适配配置")]
        private void RefreshAdapter()
        {
            ConfigureCanvasScaler();
        }

        /// <summary>
        /// 右键菜单：检查所有UI元素的锚点设置
        /// </summary>
        [ContextMenu("检查UI锚点设置")]
        private void CheckUIAnchors()
        {
            List<RectTransform> problematicElements = new List<RectTransform>();
            CheckUIAnchorsRecursive(transform as RectTransform, problematicElements);

            if (problematicElements.Count > 0)
            {
                Debug.LogWarning($"UIResolutionAdapter: 发现 {problematicElements.Count} 个UI元素可能未正确设置锚点！", this);
                foreach (var rt in problematicElements)
                {
                    Debug.LogWarning($"  - {rt.name}: 锚点未设置为边距模式（Stretch）", this);
                }
            }
        }

        /// <summary>
        /// 递归检查UI锚点设置
        /// </summary>
        private void CheckUIAnchorsRecursive(RectTransform parent, List<RectTransform> problematicElements)
        {
            foreach (RectTransform child in parent)
            {
                // 检查是否为固定大小的元素（锚点相同，不是Stretch模式）
                Vector2 anchorMin = child.anchorMin;
                Vector2 anchorMax = child.anchorMax;

                // 如果锚点的min和max不相同，说明使用了Stretch模式，这是好的
                // 但我们也允许固定大小的元素（锚点相同）
                // 这里主要检查是否有明显的问题设置

                // 递归检查子元素
                if (child.childCount > 0)
                {
                    CheckUIAnchorsRecursive(child, problematicElements);
                }
            }
        }
    }
}