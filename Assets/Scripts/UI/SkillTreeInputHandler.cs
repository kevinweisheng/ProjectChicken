using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectChicken.UI
{
    /// <summary>
    /// 技能树输入处理器：处理按键检测（应挂载在始终激活的 GameObject 上，如 GameManager）
    /// </summary>
    public class SkillTreeInputHandler : MonoBehaviour
    {
        [Header("按键配置")]
        [SerializeField] private KeyCode toggleKey = KeyCode.K; // 切换面板显示的按键（默认K键）
        [SerializeField] private KeyCode alternativeKey = KeyCode.Tab; // 备用按键（Tab键）

        private void Update()
        {
            // 检测按键输入（同时支持新旧 Input 系统）
            bool keyPressed = false;

            // 尝试使用新的 Input System
            if (Keyboard.current != null)
            {
                // 将 KeyCode 转换为新的 Key 枚举（简化处理，只处理常用按键）
                if ((toggleKey == KeyCode.K && Keyboard.current.kKey.wasPressedThisFrame) ||
                    (toggleKey == KeyCode.Tab && Keyboard.current.tabKey.wasPressedThisFrame) ||
                    (alternativeKey == KeyCode.K && Keyboard.current.kKey.wasPressedThisFrame) ||
                    (alternativeKey == KeyCode.Tab && Keyboard.current.tabKey.wasPressedThisFrame))
                {
                    keyPressed = true;
                }
            }

            // 如果新 Input System 不可用，回退到旧的 Input 系统
            if (!keyPressed)
            {
                if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(alternativeKey))
                {
                    keyPressed = true;
                }
            }

            if (keyPressed)
            {
                // 通过静态引用调用 SkillTreePanel 的切换方法
                if (SkillTreePanel.Instance != null)
                {
                    Debug.Log($"SkillTreeInputHandler: 检测到按键按下，切换技能树面板", this);
                    SkillTreePanel.Instance.TogglePanel();
                }
                else
                {
                    Debug.LogWarning("SkillTreeInputHandler: SkillTreePanel.Instance 为空！请确保场景中存在 SkillTreePanel 组件。", this);
                }
            }
        }
    }
}

