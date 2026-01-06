using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using ProjectChicken.Systems.SkillTree;
using ProjectChicken.Core;

namespace ProjectChicken.UI
{
    /// <summary>
    /// 技能按钮UI：处理单个技能按钮的状态显示和交互
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class SkillSlotUI : MonoBehaviour
    {
        [Header("技能数据")]
        [SerializeField] private SkillNodeData targetSkill; // 该按钮对应的技能数据

        [Header("UI组件引用")]
        [SerializeField] private Button buttonComp; // 自身的 Button 组件
        [SerializeField] private Image iconImage; // 显示图标
        [SerializeField] private TMP_Text costText; // 显示价格
        [SerializeField] private GameObject lockCover; // 遮罩物体（用于显示"未解锁"状态）
        [SerializeField] private TMP_Text statusText; // 状态文本（可选，显示"已拥有"等）

        [Header("颜色配置")]
        [SerializeField] private Color normalColor = Color.white; // 正常颜色
        [SerializeField] private Color disabledColor = Color.gray; // 禁用颜色
        [SerializeField] private Color unlockedColor = Color.green; // 已解锁颜色

        private void Start()
        {
            // 初始化UI组件
            InitializeComponents();
            
            // 初始化显示
            InitializeDisplay();
            
            // 绑定点击事件
            if (buttonComp != null)
            {
                buttonComp.onClick.AddListener(OnButtonClicked);
            }
        }

        private void OnEnable()
        {
            // 订阅全局货币变化事件
            ResourceManager.OnGlobalEggsChanged += OnGlobalEggsChanged;
        }

        private void OnDisable()
        {
            // 取消订阅：防止内存泄漏
            ResourceManager.OnGlobalEggsChanged -= OnGlobalEggsChanged;
        }

        private void Update()
        {
            // 每帧更新UI状态（也可以使用事件驱动，这里为了简单使用Update）
            UpdateUIState();
        }

        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private void InitializeComponents()
        {
            // 如果没有手动指定，尝试自动获取
            if (buttonComp == null)
            {
                buttonComp = GetComponent<Button>();
            }

            if (iconImage == null)
            {
                iconImage = GetComponentInChildren<Image>();
            }
        }

        /// <summary>
        /// 初始化显示：根据技能数据设置图标和价格
        /// </summary>
        private void InitializeDisplay()
        {
            if (targetSkill == null)
            {
                Debug.LogWarning("SkillSlotUI: targetSkill 未配置！", this);
                return;
            }

            // 设置图标
            if (iconImage != null && targetSkill.Icon != null)
            {
                iconImage.sprite = targetSkill.Icon;
            }

            // 设置价格文本
            if (costText != null)
            {
                costText.text = targetSkill.Cost.ToString();
            }
        }

        /// <summary>
        /// 更新UI状态：检查技能解锁状态并更新按钮外观
        /// </summary>
        private void UpdateUIState()
        {
            if (targetSkill == null || UpgradeManager.Instance == null)
            {
                return;
            }

            bool isUnlocked = UpgradeManager.Instance.IsNodeUnlocked(targetSkill);
            bool canUnlock = CanUnlockSkill();
            bool hasEnoughMoney = HasEnoughMoney();

            // 状态1：已解锁
            if (isUnlocked)
            {
                SetUnlockedState();
            }
            // 状态2：可解锁（钱够 + 前置已解锁）
            else if (canUnlock && hasEnoughMoney)
            {
                SetAvailableState();
            }
            // 状态3：不可解锁（钱不够 或 前置未解锁）
            else
            {
                SetLockedState();
            }
        }

        /// <summary>
        /// 检查是否可以解锁技能（前置条件是否满足）
        /// </summary>
        /// <returns>是否可以解锁</returns>
        private bool CanUnlockSkill()
        {
            if (targetSkill.Prerequisite == null)
            {
                // 没有前置技能，可以直接解锁
                return true;
            }

            // 检查前置技能是否已解锁
            return UpgradeManager.Instance.IsNodeUnlocked(targetSkill.Prerequisite);
        }

        /// <summary>
        /// 检查是否有足够的钱（检查全局货币）
        /// </summary>
        /// <returns>是否有足够的钱</returns>
        private bool HasEnoughMoney()
        {
            if (ResourceManager.Instance == null)
            {
                return false;
            }

            return ResourceManager.Instance.TotalGlobalEggs >= targetSkill.Cost;
        }

        /// <summary>
        /// 全局货币变化事件处理
        /// </summary>
        /// <param name="newAmount">新的全局货币数量</param>
        private void OnGlobalEggsChanged(int newAmount)
        {
            // 当全局货币变化时，刷新UI状态（更新按钮的可点击状态）
            UpdateUIState();
        }

        /// <summary>
        /// 设置已解锁状态
        /// </summary>
        private void SetUnlockedState()
        {
            if (buttonComp != null)
            {
                buttonComp.interactable = false; // 按钮不可交互
            }

            // 显示"已拥有"状态
            if (statusText != null)
            {
                statusText.text = "已拥有"; // 如果字体不支持中文，使用英文
                statusText.gameObject.SetActive(true);
            }

            // 隐藏遮罩
            if (lockCover != null)
            {
                lockCover.SetActive(false);
            }

            // 设置颜色为已解锁颜色
            if (iconImage != null)
            {
                iconImage.color = unlockedColor;
            }
        }

        /// <summary>
        /// 设置可解锁状态（按钮可点击）
        /// </summary>
        private void SetAvailableState()
        {
            if (buttonComp != null)
            {
                buttonComp.interactable = true; // 按钮可点击
            }

            // 隐藏状态文本
            if (statusText != null)
            {
                statusText.gameObject.SetActive(false);
            }

            // 隐藏遮罩
            if (lockCover != null)
            {
                lockCover.SetActive(false);
            }

            // 设置正常颜色
            if (iconImage != null)
            {
                iconImage.color = normalColor;
            }
        }

        /// <summary>
        /// 设置锁定状态（按钮不可点击）
        /// </summary>
        private void SetLockedState()
        {
            if (buttonComp != null)
            {
                buttonComp.interactable = false; // 按钮不可点击
            }

            // 隐藏状态文本
            if (statusText != null)
            {
                statusText.gameObject.SetActive(false);
            }

            // 显示遮罩
            if (lockCover != null)
            {
                lockCover.SetActive(true);
            }

            // 设置灰色
            if (iconImage != null)
            {
                iconImage.color = disabledColor;
            }
        }

        /// <summary>
        /// 按钮点击事件
        /// </summary>
        private void OnButtonClicked()
        {
            if (targetSkill == null || UpgradeManager.Instance == null)
            {
                return;
            }

            // 先检查是否有足够的全局货币
            if (!HasEnoughMoney())
            {
                Debug.LogWarning($"SkillSlotUI: 全局货币不足！需要 {targetSkill.Cost}，当前只有 {ResourceManager.Instance?.TotalGlobalEggs ?? 0}。", this);
                // 可以在这里播放错误音效
                // AudioManager.Instance?.PlayErrorSound();
                return;
            }

            // 尝试解锁技能（内部会调用 ResourceManager.SpendGlobalEggs）
            bool success = UpgradeManager.Instance.TryUnlockNode(targetSkill);

            if (success)
            {
                // 解锁成功，立即刷新UI状态
                UpdateUIState();
                
                // 可以在这里添加音效、特效等反馈
                Debug.Log($"SkillSlotUI: 成功解锁技能 {targetSkill.DisplayName}！");
            }
            else
            {
                // 解锁失败，刷新UI状态（可能货币不足或其他原因）
                UpdateUIState();
            }
        }

        /// <summary>
        /// 手动刷新UI状态（供外部调用）
        /// </summary>
        public void RefreshUI()
        {
            UpdateUIState();
        }
    }
}

