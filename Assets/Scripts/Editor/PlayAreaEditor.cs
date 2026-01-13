using UnityEngine;
using UnityEditor;
using ProjectChicken.Systems;

namespace ProjectChicken.Editor
{
    /// <summary>
    /// PlayArea 自定义编辑器：在 Inspector 中以像素单位显示 Area Size
    /// </summary>
    [CustomEditor(typeof(PlayArea))]
    [CanEditMultipleObjects]
    public class PlayAreaEditor : UnityEditor.Editor
    {
        private SerializedProperty areaSizeProperty;
        private SerializedProperty areaCenterProperty;
        private SerializedProperty useWorldBoundsProperty;
        private SerializedProperty usePixelUnitsProperty;
        private SerializedProperty mainCameraProperty;
        private SerializedProperty areaSpriteRendererProperty;
        private SerializedProperty areaSpriteProperty;
        private SerializedProperty autoSizeFromSpriteProperty;
        private SerializedProperty sortingOrderProperty;
        private SerializedProperty sortingLayerNameProperty;
        private SerializedProperty showGizmosProperty;
        private SerializedProperty gizmoColorProperty;

        private void OnEnable()
        {
            areaSizeProperty = serializedObject.FindProperty("areaSize");
            areaCenterProperty = serializedObject.FindProperty("areaCenter");
            useWorldBoundsProperty = serializedObject.FindProperty("useWorldBounds");
            usePixelUnitsProperty = serializedObject.FindProperty("usePixelUnits");
            mainCameraProperty = serializedObject.FindProperty("mainCamera");
            areaSpriteRendererProperty = serializedObject.FindProperty("areaSpriteRenderer");
            areaSpriteProperty = serializedObject.FindProperty("areaSprite");
            autoSizeFromSpriteProperty = serializedObject.FindProperty("autoSizeFromSprite");
            sortingOrderProperty = serializedObject.FindProperty("sortingOrder");
            sortingLayerNameProperty = serializedObject.FindProperty("sortingLayerName");
            showGizmosProperty = serializedObject.FindProperty("showGizmos");
            gizmoColorProperty = serializedObject.FindProperty("gizmoColor");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            PlayArea playArea = (PlayArea)target;

            // 获取主摄像机（用于像素转换）
            Camera camera = mainCameraProperty.objectReferenceValue as Camera;
            if (camera == null)
            {
                camera = Camera.main;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("场地配置", EditorStyles.boldLabel);

            // 像素单位开关
            EditorGUILayout.PropertyField(usePixelUnitsProperty, new GUIContent("使用像素单位", "如果启用，Area Size 将显示为像素值"));

            EditorGUILayout.Space();

            // Area Size 显示（根据 usePixelUnits 决定显示像素还是世界单位）
            if (usePixelUnitsProperty.boolValue && camera != null && camera.orthographic)
            {
                // 显示像素值
                Vector2 currentWorldSize = areaSizeProperty.vector2Value;
                Vector2 pixelSize = WorldSizeToPixelSize(currentWorldSize, camera);

                EditorGUILayout.LabelField("Area Size (像素)", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                Vector2 newPixelSize = EditorGUILayout.Vector2Field("大小", pixelSize);
                if (EditorGUI.EndChangeCheck())
                {
                    // 将像素值转换回世界单位
                    Vector2 newWorldSize = PixelSizeToWorldSize(newPixelSize, camera);
                    areaSizeProperty.vector2Value = newWorldSize;
                }

                // 显示当前屏幕分辨率信息
                EditorGUILayout.HelpBox($"当前屏幕分辨率: {Screen.width} x {Screen.height} 像素\n" +
                                       $"摄像机 Orthographic Size: {camera.orthographicSize}\n" +
                                       $"转换比例: {Screen.height / (2f * camera.orthographicSize):F2} 像素/世界单位", 
                                       MessageType.Info);
            }
            else
            {
                // 显示世界单位
                EditorGUILayout.PropertyField(areaSizeProperty, new GUIContent("Area Size (世界单位)", "场地大小（世界单位）"));
                
                if (usePixelUnitsProperty.boolValue)
                {
                    if (camera == null)
                    {
                        EditorGUILayout.HelpBox("未找到主摄像机，无法进行像素转换。请手动指定主摄像机。", MessageType.Warning);
                    }
                    else if (!camera.orthographic)
                    {
                        EditorGUILayout.HelpBox("主摄像机不是正交摄像机，无法进行像素转换。", MessageType.Warning);
                    }
                }
            }

            EditorGUILayout.PropertyField(areaCenterProperty, new GUIContent("Area Center", "场地中心位置（世界坐标）"));
            EditorGUILayout.PropertyField(useWorldBoundsProperty, new GUIContent("Use World Bounds", "是否使用世界坐标边界"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("像素单位显示（可选）", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(mainCameraProperty, new GUIContent("Main Camera", "主摄像机（用于像素到世界单位的转换，如果为空则自动获取）"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("美术素材（可选）", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(areaSpriteRendererProperty, new GUIContent("Area Sprite Renderer", "场地 Sprite 渲染器"));
            EditorGUILayout.PropertyField(areaSpriteProperty, new GUIContent("Area Sprite", "场地 Sprite"));
            EditorGUILayout.PropertyField(autoSizeFromSpriteProperty, new GUIContent("Auto Size From Sprite", "是否根据 Sprite 自动设置场地大小"));
            EditorGUILayout.PropertyField(sortingOrderProperty, new GUIContent("Sorting Order", "渲染排序顺序"));
            EditorGUILayout.PropertyField(sortingLayerNameProperty, new GUIContent("Sorting Layer Name", "排序图层名称"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("可视化（可选）", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(showGizmosProperty, new GUIContent("Show Gizmos", "在 Scene 视图中显示场地边界"));
            EditorGUILayout.PropertyField(gizmoColorProperty, new GUIContent("Gizmo Color", "Gizmo 颜色"));

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 将世界单位大小转换为像素大小
        /// </summary>
        private Vector2 WorldSizeToPixelSize(Vector2 worldSize, Camera camera)
        {
            if (camera == null || !camera.orthographic)
            {
                return worldSize * 100f; // 默认假设 1 世界单位 = 100 像素
            }

            float screenHeight = Screen.height;
            float pixelsPerUnit = screenHeight / (2f * camera.orthographicSize);

            return new Vector2(
                worldSize.x * pixelsPerUnit,
                worldSize.y * pixelsPerUnit
            );
        }

        /// <summary>
        /// 将像素大小转换为世界单位大小
        /// </summary>
        private Vector2 PixelSizeToWorldSize(Vector2 pixelSize, Camera camera)
        {
            if (camera == null || !camera.orthographic)
            {
                return pixelSize / 100f; // 默认假设 1 世界单位 = 100 像素
            }

            float screenHeight = Screen.height;
            float pixelsPerUnit = screenHeight / (2f * camera.orthographicSize);

            return new Vector2(
                pixelSize.x / pixelsPerUnit,
                pixelSize.y / pixelsPerUnit
            );
        }
    }
}

