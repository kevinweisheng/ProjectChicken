using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using ProjectChicken.UI;

namespace ProjectChicken.Editor
{
    /// <summary>
    /// SkillTreePanel 自动配置工具
    /// </summary>
    public class SkillTreePanelSetup : EditorWindow
    {
        [MenuItem("Project Chicken/配置 SkillTreePanel Tooltip")]
        public static void SetupTooltip()
        {
            // 查找 SkillTreePanel
            SkillTreePanel panel = FindFirstObjectByType<SkillTreePanel>();
            if (panel == null)
            {
                Debug.LogError("未找到 SkillTreePanel！");
                return;
            }

            // 查找 TooltipPanel
            Transform tooltipPanel = panel.transform.Find("TooltipPanel");
            if (tooltipPanel == null)
            {
                Debug.LogError("未找到 TooltipPanel！");
                return;
            }

            // 使用反射设置私有字段
            var panelType = typeof(SkillTreePanel);
            var tooltipPanelField = panelType.GetField("tooltipPanel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var tooltipIconField = panelType.GetField("tooltipIcon", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var tooltipTitleField = panelType.GetField("tooltipTitle", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var tooltipDescField = panelType.GetField("tooltipDesc", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var tooltipCostField = panelType.GetField("tooltipCost", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // 设置 tooltipPanel
            if (tooltipPanelField != null)
            {
                tooltipPanelField.SetValue(panel, tooltipPanel.gameObject);
            }

            // 查找并设置 Icon
            Transform iconTransform = tooltipPanel.Find("Header/Icon");
            if (iconTransform != null)
            {
                Image iconImage = iconTransform.GetComponent<Image>();
                if (iconImage != null && tooltipIconField != null)
                {
                    tooltipIconField.SetValue(panel, iconImage);
                    // 配置 Icon 属性
                    iconImage.preserveAspect = true;
                    iconImage.raycastTarget = false;
                    // 设置图标大小
                    RectTransform iconRect = iconTransform.GetComponent<RectTransform>();
                    if (iconRect != null)
                    {
                        iconRect.sizeDelta = new Vector2(40, 40);
                    }
                }
            }

            // 查找并设置 TitleText
            Transform titleTransform = tooltipPanel.Find("Header/TitleText");
            if (titleTransform != null)
            {
                TMP_Text titleText = titleTransform.GetComponent<TMP_Text>();
                if (titleText != null && tooltipTitleField != null)
                {
                    tooltipTitleField.SetValue(panel, titleText);
                    // 配置文本样式
                    titleText.fontSize = 18;
                    titleText.fontStyle = FontStyles.Bold;
                    titleText.alignment = TextAlignmentOptions.Left;
                }
            }

            // 查找并设置 DescText
            Transform descTransform = tooltipPanel.Find("DescText");
            if (descTransform != null)
            {
                TMP_Text descText = descTransform.GetComponent<TMP_Text>();
                if (descText != null && tooltipDescField != null)
                {
                    tooltipDescField.SetValue(panel, descText);
                    // 配置文本样式
                    descText.fontSize = 14;
                    descText.alignment = TextAlignmentOptions.TopLeft;
                    descText.textWrappingMode = TextWrappingModes.Normal;
                }
            }

            // 查找并设置 CostText
            Transform costTransform = tooltipPanel.Find("CostText");
            if (costTransform != null)
            {
                TMP_Text costText = costTransform.GetComponent<TMP_Text>();
                if (costText != null && tooltipCostField != null)
                {
                    tooltipCostField.SetValue(panel, costText);
                    // 配置文本样式
                    costText.fontSize = 14;
                    costText.alignment = TextAlignmentOptions.Left;
                }
            }

            // 确保 TooltipPanel 初始为非激活
            tooltipPanel.gameObject.SetActive(false);

            // 标记场景为已修改
            EditorUtility.SetDirty(panel);
            if (iconTransform != null) EditorUtility.SetDirty(iconTransform.gameObject);
            if (titleTransform != null) EditorUtility.SetDirty(titleTransform.gameObject);
            if (descTransform != null) EditorUtility.SetDirty(descTransform.gameObject);
            if (costTransform != null) EditorUtility.SetDirty(costTransform.gameObject);
        }
    }
}

