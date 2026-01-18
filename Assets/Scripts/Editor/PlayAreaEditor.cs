using UnityEngine;
using UnityEditor;
using ProjectChicken.Systems;

namespace ProjectChicken.Editor
{
    /// <summary>
    /// PlayArea 自定义编辑器：在 Inspector 中以像素单位显示 Area Size，并在 Scene 视图中提供可视化调整
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
        
        // 鸡活动范围相关属性
        private SerializedProperty chickenMovementAreaSizeProperty;
        private SerializedProperty chickenMovementAreaCenterProperty;
        private SerializedProperty useCustomChickenAreaProperty;

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
            
            // 鸡活动范围属性
            chickenMovementAreaSizeProperty = serializedObject.FindProperty("chickenMovementAreaSize");
            chickenMovementAreaCenterProperty = serializedObject.FindProperty("chickenMovementAreaCenter");
            useCustomChickenAreaProperty = serializedObject.FindProperty("useCustomChickenArea");
        }
        
        private void OnSceneGUI()
        {
            PlayArea playArea = (PlayArea)target;
            
            // 只在未运行时显示调整手柄
            if (Application.isPlaying) return;
            
            // 绘制鸡活动范围的可视化调整手柄
            DrawChickenAreaHandles(playArea);
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
            EditorGUILayout.LabelField("鸡活动范围配置", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(useCustomChickenAreaProperty, new GUIContent("使用自定义鸡活动范围", "如果启用，可以设置与场地不同的鸡活动范围"));
            
            if (useCustomChickenAreaProperty.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(chickenMovementAreaSizeProperty, new GUIContent("活动范围大小", "鸡的活动范围大小（世界单位），如果为 (0,0) 则使用场地大小"));
                EditorGUILayout.PropertyField(chickenMovementAreaCenterProperty, new GUIContent("活动范围中心", "鸡的活动范围中心位置（世界坐标）"));
                EditorGUI.indentLevel--;
                
                EditorGUILayout.HelpBox("提示：在 Scene 视图中选中 PlayArea，可以拖动手柄来可视化调整鸡活动范围。", MessageType.Info);
                
                // 应用到 AreaData 的按钮
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("应用到 AreaData", GUILayout.Height(25)))
                {
                    ShowAreaDataSelectionWindow(playArea);
                }
                if (GUILayout.Button("重置为场地大小", GUILayout.Height(25)))
                {
                    playArea.ResetChickenMovementArea();
                    serializedObject.Update();
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("鸡活动范围将使用场地大小和中心。", MessageType.Info);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("可视化（可选）", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(showGizmosProperty, new GUIContent("Show Gizmos", "在 Scene 视图中显示场地边界"));
            EditorGUILayout.PropertyField(gizmoColorProperty, new GUIContent("Gizmo Color", "Gizmo 颜色"));

            serializedObject.ApplyModifiedProperties();
        }
        
        /// <summary>
        /// 在 Scene 视图中绘制鸡活动范围的调整手柄
        /// </summary>
        private void DrawChickenAreaHandles(PlayArea playArea)
        {
            // 使用反射获取私有字段（避免使用 serializedObject）
            System.Reflection.FieldInfo useCustomChickenAreaField = typeof(PlayArea).GetField("useCustomChickenArea", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            System.Reflection.FieldInfo chickenMovementAreaSizeField = typeof(PlayArea).GetField("chickenMovementAreaSize", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            System.Reflection.FieldInfo chickenMovementAreaCenterField = typeof(PlayArea).GetField("chickenMovementAreaCenter", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            System.Reflection.FieldInfo useWorldBoundsField = typeof(PlayArea).GetField("useWorldBounds", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (useCustomChickenAreaField == null || chickenMovementAreaSizeField == null || 
                chickenMovementAreaCenterField == null || useWorldBoundsField == null)
            {
                return; // 如果无法访问字段，直接返回
            }
            
            bool useCustomChickenArea = (bool)useCustomChickenAreaField.GetValue(playArea);
            if (!useCustomChickenArea) return;
            
            Bounds chickenBounds = playArea.ChickenMovementBounds;
            Vector3 center = chickenBounds.center;
            Vector3 size = chickenBounds.size;
            
            // 计算四个角的位置
            Vector3 bottomLeft = new Vector3(chickenBounds.min.x, chickenBounds.min.y, 0f);
            Vector3 bottomRight = new Vector3(chickenBounds.max.x, chickenBounds.min.y, 0f);
            Vector3 topLeft = new Vector3(chickenBounds.min.x, chickenBounds.max.y, 0f);
            Vector3 topRight = new Vector3(chickenBounds.max.x, chickenBounds.max.y, 0f);
            
            // 绘制黄色虚线边框
            Handles.color = Color.yellow;
            Handles.DrawDottedLine(bottomLeft, bottomRight, 5f);
            Handles.DrawDottedLine(bottomRight, topRight, 5f);
            Handles.DrawDottedLine(topRight, topLeft, 5f);
            Handles.DrawDottedLine(topLeft, bottomLeft, 5f);
            
            // 绘制调整手柄（四个角和中心）
            float handleSize = HandleUtility.GetHandleSize(center) * 0.1f;
            
            // 四个角的手柄（用于调整大小）
            EditorGUI.BeginChangeCheck();
            Vector3 newBottomLeft = Handles.PositionHandle(bottomLeft, Quaternion.identity);
            Vector3 newBottomRight = Handles.PositionHandle(bottomRight, Quaternion.identity);
            Vector3 newTopLeft = Handles.PositionHandle(topLeft, Quaternion.identity);
            Vector3 newTopRight = Handles.PositionHandle(topRight, Quaternion.identity);
            
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(playArea, "调整鸡活动范围大小");
                
                // 计算新的边界
                float newMinX = Mathf.Min(newBottomLeft.x, newTopLeft.x);
                float newMaxX = Mathf.Max(newBottomRight.x, newTopRight.x);
                float newMinY = Mathf.Min(newBottomLeft.y, newBottomRight.y);
                float newMaxY = Mathf.Max(newTopLeft.y, newTopRight.y);
                
                Vector2 newSize = new Vector2(newMaxX - newMinX, newMaxY - newMinY);
                Vector2 newCenter = new Vector2((newMinX + newMaxX) * 0.5f, (newMinY + newMaxY) * 0.5f);
                
                // 如果使用世界坐标，需要转换为相对坐标
                bool useWorldBounds = (bool)useWorldBoundsField.GetValue(playArea);
                if (useWorldBounds)
                {
                    chickenMovementAreaSizeField.SetValue(playArea, newSize);
                    chickenMovementAreaCenterField.SetValue(playArea, newCenter);
                }
                else
                {
                    Vector3 playAreaPos = playArea.transform.position;
                    chickenMovementAreaSizeField.SetValue(playArea, newSize);
                    chickenMovementAreaCenterField.SetValue(playArea, newCenter - (Vector2)playAreaPos);
                }
                
                EditorUtility.SetDirty(playArea);
            }
            
            // 中心点手柄（用于移动）
            EditorGUI.BeginChangeCheck();
            Vector3 newCenterPos = Handles.PositionHandle(center, Quaternion.identity);
            
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(playArea, "移动鸡活动范围中心");
                
                Vector2 centerOffset = (Vector2)newCenterPos - (Vector2)center;
                
                Vector2 currentCenter = (Vector2)chickenMovementAreaCenterField.GetValue(playArea);
                chickenMovementAreaCenterField.SetValue(playArea, currentCenter + centerOffset);
                
                EditorUtility.SetDirty(playArea);
            }
            
            // 绘制标签
            Handles.Label(center + Vector3.up * 0.5f, "鸡活动范围", EditorStyles.boldLabel);
        }
        
        /// <summary>
        /// 显示 AreaData 选择窗口
        /// </summary>
        private void ShowAreaDataSelectionWindow(PlayArea playArea)
        {
            // 创建一个简单的选择窗口
            GenericMenu menu = new GenericMenu();
            
            // 查找所有 AreaData 资源
            string[] guids = AssetDatabase.FindAssets("t:AreaData");
            
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "未找到任何 AreaData 资源。请先创建 AreaData 资源。", "确定");
                return;
            }
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AreaData areaData = AssetDatabase.LoadAssetAtPath<AreaData>(path);
                
                if (areaData != null)
                {
                    menu.AddItem(new GUIContent(areaData.AreaName), false, () =>
                    {
                        playArea.ApplyChickenAreaToAreaData(areaData);
                        EditorUtility.SetDirty(areaData);
                        AssetDatabase.SaveAssets();
                        EditorUtility.DisplayDialog("成功", $"已将鸡活动范围设置应用到 {areaData.AreaName}", "确定");
                    });
                }
            }
            
            menu.ShowAsContext();
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

