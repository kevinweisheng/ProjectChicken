using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using TMPro;
using System;
using System.Collections.Generic;
using ProjectChicken.Systems.SkillTree;
using ProjectChicken.Core;

namespace ProjectChicken.UI
{
    /// <summary>
    /// 技能按钮UI：处理单个技能按钮的状态显示和交互
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class SkillSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("技能数据")]
        [SerializeField] private SkillNodeData targetSkill; // 该按钮对应的技能数据
        
        [Header("父子关系（自动配置）")]
        [Tooltip("该技能槽位的前置技能槽位列表（根据 SkillNodeData 的购买条件自动设置，也可手动调整）")]
        public List<SkillSlotUI> parentSlots = new List<SkillSlotUI>(); // 前置技能槽位列表

        [Header("UI组件引用")]
        [SerializeField] private Button buttonComp; // 自身的 Button 组件
        [SerializeField] private Image iconImage; // 显示图标
        [SerializeField] private TMP_Text costText; // 显示价格
        [SerializeField] private GameObject lockCover; // 遮罩物体（用于显示"未解锁"状态）
        [SerializeField] private TMP_Text statusText; // 状态文本（可选，显示"已拥有"等）

        [Header("图标配置")]
        [Tooltip("满级时的图标 Sprite（如果设置，满级时会切换到此图标）")]
        [SerializeField] private Sprite maxLevelIconSprite; // 满级时的图标

        [Header("颜色配置")]
        [SerializeField] private Color normalColor = Color.white; // 正常颜色
        [SerializeField] private Color disabledColor = Color.gray; // 禁用颜色
        [SerializeField] private Color unlockedColor = Color.green; // 已解锁颜色
        [SerializeField] private Color errorColor = Color.red; // 错误颜色（货币不足时）

        [Header("动画配置")]
        [SerializeField] private float hoverShakeIntensity = 5f; // 悬停晃动强度
        [SerializeField] private float hoverShakeDuration = 0.3f; // 悬停晃动持续时间
        [SerializeField] private float upgradeShakeIntensity = 8f; // 升级晃动强度
        [SerializeField] private float upgradeScaleAmount = 1.2f; // 升级放大倍数
        [SerializeField] private float upgradeAnimationDuration = 0.4f; // 升级动画持续时间
        [SerializeField] private float errorFlashDuration = 0.5f; // 错误闪烁持续时间

        [Header("悬停事件")]
        [SerializeField] private UnityEvent<SkillNodeData> OnHoverEnter; // 鼠标进入事件
        [SerializeField] private UnityEvent<SkillNodeData> OnHoverExit; // 鼠标离开事件

        // 动画相关
        private Vector3 originalScale; // 原始缩放
        private Color originalIconColor; // 原始图标颜色
        private bool isAnimating = false; // 是否正在播放动画

        /// <summary>
        /// 编辑器模式下：当Inspector中的字段改变时自动调用（用于在编辑时刷新图标显示）
        /// </summary>
        private void OnValidate()
        {
            // 只在编辑模式下执行，且不运行游戏时
            if (!Application.isPlaying)
            {
                // 确保组件已初始化
                InitializeComponents();
                // 刷新图标显示（不依赖运行时系统）
                RefreshDisplayInEditor();
            }
        }

        /// <summary>
        /// 编辑器模式下刷新显示（不依赖运行时系统）
        /// </summary>
        private void RefreshDisplayInEditor()
        {
            if (targetSkill == null)
            {
                return;
            }

            // 设置图标（在编辑模式下也显示）
            if (iconImage != null && targetSkill.Icon != null)
            {
                iconImage.sprite = targetSkill.Icon;
                // 在编辑模式下使用正常颜色
                iconImage.color = normalColor;
            }

            // 隐藏文本（编辑模式下）
            if (costText != null)
            {
                costText.gameObject.SetActive(false);
            }
            if (statusText != null)
            {
                statusText.gameObject.SetActive(false);
            }

            // 隐藏遮罩（编辑模式下）
            if (lockCover != null)
            {
                lockCover.SetActive(false);
            }
        }

        /// <summary>
        /// 编辑器模式下：当组件首次添加到GameObject时调用（用于初始化显示）
        /// </summary>
        private void Reset()
        {
            // 确保组件引用已设置
            InitializeComponents();
            // 刷新显示
            RefreshDisplayInEditor();
        }

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

            // 保存原始缩放和颜色
            if (iconImage != null)
            {
                originalScale = iconImage.transform.localScale;
                originalIconColor = iconImage.color;
            }
            else
            {
                originalScale = transform.localScale;
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

            // 确保只有图标可见，禁用名称和成本文本
            if (costText != null)
            {
                costText.gameObject.SetActive(false);
            }
            if (statusText != null)
            {
                statusText.gameObject.SetActive(false);
            }

            // 更新价格和状态文本（使用当前等级信息）
            UpdateCostAndStatusText();
        }

        /// <summary>
        /// 更新价格和状态文本（显示当前等级和下一级消耗）
        /// </summary>
        private void UpdateCostAndStatusText()
        {
            if (targetSkill == null || UpgradeManager.Instance == null)
            {
                return;
            }

            // 获取当前等级
            int currentLevel = UpgradeManager.Instance.GetSkillLevel(targetSkill);
            bool isMaxLevel = UpgradeManager.Instance.IsMaxLevel(targetSkill);

            // 更新状态文本：显示等级信息
            if (statusText != null)
            {
                statusText.text = $"Lv. {currentLevel} / {targetSkill.MaxLevel}";
            }

            // 更新价格文本
            if (costText != null)
            {
                if (isMaxLevel)
                {
                    costText.text = "MAX";
                }
                else
                {
                    // 显示下一级的消耗
                    int nextLevelCost = targetSkill.GetCost(currentLevel);
                    costText.text = nextLevelCost.ToString();
                }
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

            // 获取当前等级和最大等级状态
            int currentLevel = UpgradeManager.Instance.GetSkillLevel(targetSkill);
            bool isMaxLevel = UpgradeManager.Instance.IsMaxLevel(targetSkill);
            bool isUnlocked = currentLevel > 0;
            bool canUnlock = CanUnlockSkill();
            bool hasEnoughMoney = HasEnoughMoney();

            // 检查技能是否不可解锁（未解锁且前置条件不满足）
            bool isSkillHidden = !isUnlocked && !canUnlock;

            // 如果技能不可解锁，隐藏整个 GameObject
            if (isSkillHidden)
            {
                gameObject.SetActive(false);
                return;
            }

            // 确保 GameObject 可见
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            // 更新价格和状态文本
            UpdateCostAndStatusText();

            // 状态1：已达到最大等级
            if (isMaxLevel)
            {
                SetMaxLevelState();
            }
            // 状态2：已解锁但未满级
            else if (isUnlocked)
            {
                if (hasEnoughMoney)
                {
                    SetAvailableState(); // 可以继续升级
                }
                else
                {
                    SetLockedState(); // 钱不够
                }
            }
            // 状态3：可解锁（钱够 + 前置已解锁）
            else if (canUnlock && hasEnoughMoney)
            {
                SetAvailableState();
            }
            // 状态4：不可解锁（钱不够 或 前置未解锁）
            else
            {
                SetLockedState();
            }
        }

        /// <summary>
        /// 检查是否可以解锁技能（前置条件是否满足）
        /// 只要任意一个前置技能已解锁（level > 0），就可以解锁当前技能
        /// </summary>
        /// <returns>是否可以解锁</returns>
        private bool CanUnlockSkill()
        {
            if (UpgradeManager.Instance == null)
            {
                return false;
            }

            // 如果没有前置技能（既没有 Prerequisite 也没有 parentSlots），可以直接解锁
            if (targetSkill.Prerequisite == null && (parentSlots == null || parentSlots.Count == 0))
            {
                return true;
            }

            // 优先检查 parentSlots 列表（手动配置的父子关系）
            if (parentSlots != null && parentSlots.Count > 0)
            {
                foreach (SkillSlotUI parentSlot in parentSlots)
                {
                    if (parentSlot != null && parentSlot.TargetSkill != null)
                    {
                        // 只要任意一个父技能已解锁（level > 0），就可以解锁当前技能
                        if (UpgradeManager.Instance.IsNodeUnlocked(parentSlot.TargetSkill))
                        {
                            return true;
                        }
                    }
                }
            }

            // 回退检查 Prerequisite（单个前置技能）
            if (targetSkill.Prerequisite != null)
            {
                return UpgradeManager.Instance.IsNodeUnlocked(targetSkill.Prerequisite);
            }

            return false;
        }

        /// <summary>
        /// 检查是否有足够的钱（检查全局货币）
        /// </summary>
        /// <returns>是否有足够的钱</returns>
        private bool HasEnoughMoney()
        {
            if (ResourceManager.Instance == null || UpgradeManager.Instance == null)
            {
                return false;
            }

            // 获取当前等级和下一级的消耗
            int currentLevel = UpgradeManager.Instance.GetSkillLevel(targetSkill);
            bool isMaxLevel = UpgradeManager.Instance.IsMaxLevel(targetSkill);

            if (isMaxLevel)
            {
                return false; // 已满级，不需要钱
            }

            int nextLevelCost = targetSkill.GetCost(currentLevel);
            return ResourceManager.Instance.TotalGlobalEggs >= nextLevelCost;
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
        /// 设置最大等级状态（已满级）
        /// </summary>
        private void SetMaxLevelState()
        {
            if (buttonComp != null)
            {
                buttonComp.interactable = false; // 按钮不可交互
            }

            // 显示"MAX"状态
            if (statusText != null)
            {
                statusText.text = "MAX";
                statusText.gameObject.SetActive(true);
            }

            // 隐藏遮罩
            if (lockCover != null)
            {
                lockCover.SetActive(false);
            }

            // 如果指定了满级图标，切换图标
            if (iconImage != null)
            {
                if (maxLevelIconSprite != null)
                {
                    iconImage.sprite = maxLevelIconSprite;
                    iconImage.color = Color.white; // 使用 Sprite 的原始颜色
                }
                else
                {
                    // 如果没有指定满级图标，使用颜色变化
                    iconImage.color = unlockedColor;
                }
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

            // 恢复普通图标（如果之前切换到了满级图标）
            if (iconImage != null)
            {
                // 如果当前使用的是满级图标，恢复为普通图标
                if (maxLevelIconSprite != null && iconImage.sprite == maxLevelIconSprite && targetSkill != null)
                {
                    iconImage.sprite = targetSkill.Icon;
                }
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

            // 恢复普通图标（如果之前切换到了满级图标）
            if (iconImage != null)
            {
                // 如果当前使用的是满级图标，恢复为普通图标
                if (maxLevelIconSprite != null && iconImage.sprite == maxLevelIconSprite && targetSkill != null)
                {
                    iconImage.sprite = targetSkill.Icon;
                }
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

            if (UpgradeManager.Instance.IsMaxLevel(targetSkill))
            {
                return;
            }

            // 获取当前等级和下一级的消耗
            int currentLevel = UpgradeManager.Instance.GetSkillLevel(targetSkill);
            int nextLevelCost = targetSkill.GetCost(currentLevel);

            // 先检查是否有足够的全局货币
            if (!HasEnoughMoney())
            {
                Debug.LogWarning($"SkillSlotUI: 全局货币不足！需要 {nextLevelCost}，当前只有 {ResourceManager.Instance?.TotalGlobalEggs ?? 0}。", this);
                
                // 播放错误动画：图标变红
                if (iconImage != null)
                {
                    StartCoroutine(ErrorFlashAnimation());
                }
                
                // 可以在这里播放错误音效
                // AudioManager.Instance?.PlayErrorSound();
                return;
            }

            // 尝试升级技能（内部会调用 ResourceManager.SpendGlobalEggs）
            bool success = UpgradeManager.Instance.TryUpgradeSkill(targetSkill);

            if (success)
            {
                // 播放升级成功动画：图标晃动放大
                if (iconImage != null)
                {
                    StartCoroutine(UpgradeShakeScaleAnimation());
                }

                // 升级成功，立即刷新UI状态
                UpdateUIState();
                
                // 刷新所有技能槽位的UI（以便子技能能立即检测到父技能已解锁）
                if (SkillTreePanel.Instance != null)
                {
                    SkillTreePanel.Instance.RefreshAllSkillSlots();
                    
                    if (currentLevel == 0)
                    {
                        SkillTreePanel.Instance.AddLinesForUnlockedSkill(this);
                    }
                    
                    StartCoroutine(DelayedRedrawConnections());
                }
            }
            else
            {
                // 升级失败，刷新UI状态（可能货币不足或其他原因）
                UpdateUIState();
            }
        }

        /// <summary>
        /// 延迟重新绘制连线（确保技能状态已更新）
        /// </summary>
        private System.Collections.IEnumerator DelayedRedrawConnections()
        {
            yield return null; // 等待一帧，确保所有状态已更新
            
            if (SkillTreePanel.Instance != null)
            {
                SkillTreePanel.Instance.DrawConnections();
            }
        }

        /// <summary>
        /// 手动刷新UI状态（供外部调用）
        /// </summary>
        public void RefreshUI()
        {
            UpdateUIState();
        }

        /// <summary>
        /// 获取当前槽位对应的技能数据（供外部访问）
        /// </summary>
        public SkillNodeData TargetSkill => targetSkill;

        /// <summary>
        /// 鼠标进入事件处理
        /// </summary>
        /// <param name="eventData">事件数据</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (targetSkill != null && OnHoverEnter != null)
            {
                OnHoverEnter.Invoke(targetSkill);
            }

            // 播放悬停晃动动画
            if (iconImage != null && !isAnimating)
            {
                StartCoroutine(HoverShakeAnimation());
            }
        }

        /// <summary>
        /// 鼠标离开事件处理
        /// </summary>
        /// <param name="eventData">事件数据</param>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (targetSkill != null && OnHoverExit != null)
            {
                OnHoverExit.Invoke(targetSkill);
            }
        }

        /// <summary>
        /// 悬停晃动动画
        /// </summary>
        private System.Collections.IEnumerator HoverShakeAnimation()
        {
            if (iconImage == null) yield break;

            isAnimating = true;
            Transform iconTransform = iconImage.transform;
            Vector3 startPos = iconTransform.localPosition;
            float elapsed = 0f;

            while (elapsed < hoverShakeDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / hoverShakeDuration;
                
                // 使用衰减的正弦波创建晃动效果
                float shakeAmount = hoverShakeIntensity * (1f - progress);
                float angle = progress * Mathf.PI * 4f; // 晃动2次
                Vector3 offset = new Vector3(
                    Mathf.Sin(angle) * shakeAmount,
                    Mathf.Cos(angle * 0.7f) * shakeAmount,
                    0f
                );
                
                iconTransform.localPosition = startPos + offset;
                yield return null;
            }

            // 恢复原始位置
            iconTransform.localPosition = startPos;
            isAnimating = false;
        }

        /// <summary>
        /// 升级晃动放大动画
        /// </summary>
        private System.Collections.IEnumerator UpgradeShakeScaleAnimation()
        {
            if (iconImage == null) yield break;

            isAnimating = true;
            Transform iconTransform = iconImage.transform;
            Vector3 startPos = iconTransform.localPosition;
            Vector3 startScale = iconTransform.localScale;
            float elapsed = 0f;

            while (elapsed < upgradeAnimationDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / upgradeAnimationDuration;
                
                // 前半段：放大并晃动
                if (progress < 0.5f)
                {
                    float phase = progress * 2f; // 0 到 1
                    float scale = Mathf.Lerp(1f, upgradeScaleAmount, phase);
                    iconTransform.localScale = originalScale * scale;
                    
                    // 晃动效果
                    float shakeAmount = upgradeShakeIntensity * (1f - phase);
                    float angle = phase * Mathf.PI * 3f;
                    Vector3 offset = new Vector3(
                        Mathf.Sin(angle) * shakeAmount,
                        Mathf.Cos(angle * 0.7f) * shakeAmount,
                        0f
                    );
                    iconTransform.localPosition = startPos + offset;
                }
                // 后半段：恢复
                else
                {
                    float phase = (progress - 0.5f) * 2f; // 0 到 1
                    float scale = Mathf.Lerp(upgradeScaleAmount, 1f, phase);
                    iconTransform.localScale = originalScale * scale;
                    iconTransform.localPosition = Vector3.Lerp(startPos + new Vector3(
                        Mathf.Sin(Mathf.PI * 1.5f) * upgradeShakeIntensity * 0.5f,
                        Mathf.Cos(Mathf.PI * 1.5f * 0.7f) * upgradeShakeIntensity * 0.5f,
                        0f
                    ), startPos, phase);
                }
                
                yield return null;
            }

            // 确保恢复到原始状态
            iconTransform.localPosition = startPos;
            iconTransform.localScale = originalScale;
            isAnimating = false;
        }

        /// <summary>
        /// 错误闪烁动画（货币不足时）
        /// </summary>
        private System.Collections.IEnumerator ErrorFlashAnimation()
        {
            if (iconImage == null) yield break;

            isAnimating = true;
            Color startColor = iconImage.color;
            float elapsed = 0f;

            // 闪烁2次
            int flashCount = 2;
            float flashDuration = errorFlashDuration / flashCount;

            for (int i = 0; i < flashCount; i++)
            {
                elapsed = 0f;
                while (elapsed < flashDuration)
                {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / flashDuration;
                    
                    // 前半段：变红
                    if (progress < 0.5f)
                    {
                        iconImage.color = Color.Lerp(startColor, errorColor, progress * 2f);
                    }
                    // 后半段：恢复
                    else
                    {
                        iconImage.color = Color.Lerp(errorColor, startColor, (progress - 0.5f) * 2f);
                    }
                    
                    yield return null;
                }
            }

            // 确保恢复到原始颜色
            iconImage.color = startColor;
            isAnimating = false;
        }
    }
}

