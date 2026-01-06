using UnityEngine;
using TMPro;
using System;
using ProjectChicken.Systems.Save;

namespace ProjectChicken.Core
{
    /// <summary>
    /// 资源管理器：负责管理游戏资源（如鸡蛋数量）并更新UI显示
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        // 全局货币变化事件
        public static event Action<int> OnGlobalEggsChanged;

        [Header("UI配置")]
        [SerializeField] private TMP_Text eggCountText; // 全局货币显示文本（TextMeshPro）
        [SerializeField] private TMP_Text sessionEggsText; // 局内分数显示文本（TextMeshPro，可选）

        // 双货币系统
        private int totalGlobalEggs = 0; // 全局永久货币/仓库（跨局保存）

        /// <summary>
        /// 当前局产蛋量（只读属性，单局分数）
        /// </summary>
        public int CurrentSessionEggs { get; private set; }

        /// <summary>
        /// 全局永久货币/仓库（只读属性，跨局保存）
        /// </summary>
        public int TotalGlobalEggs 
        { 
            get => totalGlobalEggs; 
            private set => totalGlobalEggs = value; 
        }

        /// <summary>
        /// 当前拥有的鸡蛋数量（只读属性，兼容旧代码，返回全局货币）
        /// </summary>
        public int TotalEggs => TotalGlobalEggs;

        /// <summary>
        /// 当前局产蛋量（只读属性，兼容旧代码）
        /// </summary>
        public int CurrentScore => CurrentSessionEggs;

        private void Awake()
        {
            // 单例模式：防止重复创建
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            
            // 初始化单局分数
            CurrentSessionEggs = 0;
            
            // 如果需要跨场景保持，取消下面这行的注释
            // DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // 在 Start() 中加载游戏数据，确保所有单例都已初始化
            // Start() 在所有 Awake() 执行完毕后才会执行
            LoadGame();
            
            // 初始化UI显示
            UpdateUI();
        }

        /// <summary>
        /// 增加鸡蛋数量（只增加局内分数，不直接增加全局货币）
        /// </summary>
        /// <param name="amount">增加的数量</param>
        public void AddEgg(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning("ResourceManager: 尝试增加非正数的鸡蛋数量！", this);
                return;
            }

            CurrentSessionEggs += amount; // 只增加单局分数
            UpdateUI();
        }

        /// <summary>
        /// 消费鸡蛋数量（从全局货币中扣除）
        /// </summary>
        /// <param name="amount">消费的数量</param>
        /// <returns>是否成功消费</returns>
        public bool SpendEgg(int amount)
        {
            return SpendGlobalEggs(amount);
        }

        /// <summary>
        /// 消费全局货币（从全局货币中扣除）
        /// </summary>
        /// <param name="amount">消费的数量</param>
        /// <returns>是否成功消费</returns>
        public bool SpendGlobalEggs(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning("ResourceManager: 尝试消费非正数的鸡蛋数量！", this);
                return false;
            }

            if (TotalGlobalEggs < amount)
            {
                Debug.LogWarning($"ResourceManager: 全局货币不足！需要 {amount}，当前只有 {TotalGlobalEggs}。", this);
                return false;
            }

            TotalGlobalEggs -= amount;
            SaveGame(); // 保存游戏数据
            UpdateUI();
            
            // 触发全局货币变化事件
            OnGlobalEggsChanged?.Invoke(TotalGlobalEggs);
            
            return true;
        }

        /// <summary>
        /// 重置单局分数：将当前局产蛋量设为 0，并更新UI
        /// 注意：不影响全局货币 TotalGlobalEggs
        /// </summary>
        public void ResetSessionScore()
        {
            CurrentSessionEggs = 0;
            UpdateUI();
            Debug.Log("ResourceManager: 单局分数已重置", this);
        }

        /// <summary>
        /// 结算局内分数：将当前局产蛋量存入全局货币仓库
        /// 注意：结算后会自动重置 CurrentSessionEggs
        /// </summary>
        public void BankSessionEggs()
        {
            if (CurrentSessionEggs > 0)
            {
                TotalGlobalEggs += CurrentSessionEggs;
                SaveGame(); // 保存游戏数据
                Debug.Log($"ResourceManager: 结算完成，将 {CurrentSessionEggs} 个鸡蛋存入全局仓库，当前全局货币：{TotalGlobalEggs}", this);
                
                // 结算后重置局内分数
                CurrentSessionEggs = 0;
                UpdateUI();
                
                // 触发全局货币变化事件
                OnGlobalEggsChanged?.Invoke(TotalGlobalEggs);
            }
        }

        /// <summary>
        /// 保存游戏数据到文件
        /// </summary>
        public void SaveGame()
        {
            // 创建保存数据对象
            GameSaveData data = new GameSaveData();
            
            // 设置全局货币
            data.totalGlobalEggs = TotalGlobalEggs;
            
            // 设置已解锁技能ID列表
            if (UpgradeManager.Instance != null)
            {
                data.unlockedSkillIDs = UpgradeManager.Instance.GetUnlockedSkillIDs();
            }
            else
            {
                Debug.LogWarning("ResourceManager: UpgradeManager.Instance 为空，无法保存技能数据！", this);
                data.unlockedSkillIDs = new System.Collections.Generic.List<string>();
            }
            
            // 保存到文件
            SaveSystem.Save(data);
            
            Debug.Log("ResourceManager: Game Saved", this);
        }

        /// <summary>
        /// 从文件加载游戏数据
        /// </summary>
        public void LoadGame()
        {
            // 从文件加载数据
            GameSaveData data = SaveSystem.Load();
            
            // 恢复全局货币
            TotalGlobalEggs = data.totalGlobalEggs;
            
            // 恢复已解锁技能
            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.LoadSkills(data.unlockedSkillIDs);
            }
            else
            {
                Debug.LogWarning("ResourceManager: UpgradeManager.Instance 为空，无法加载技能数据！", this);
            }
            
            // 更新UI显示
            UpdateUI();
            
            Debug.Log("ResourceManager: Game Loaded", this);
        }

        /// <summary>
        /// 更新UI显示（显示全局货币和局内分数）
        /// </summary>
        private void UpdateUI()
        {
            // 更新全局货币显示
            if (eggCountText != null)
            {
                eggCountText.text = TotalGlobalEggs.ToString();
            }
            else
            {
                Debug.LogWarning("ResourceManager: eggCountText 未配置！请在 Inspector 中拖入 TextMeshPro 文本组件。", this);
            }

            // 更新局内分数显示（如果配置了）
            if (sessionEggsText != null)
            {
                sessionEggsText.text = CurrentSessionEggs.ToString();
            }
        }
    }
}

