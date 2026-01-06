using System;
using System.Collections.Generic;

namespace ProjectChicken.Systems.Save
{
    /// <summary>
    /// 游戏保存数据：存储需要持久化的游戏数据
    /// </summary>
    [System.Serializable]
    public class GameSaveData
    {
        /// <summary>
        /// 全局永久货币（跨局保存）
        /// </summary>
        public int totalGlobalEggs;

        /// <summary>
        /// 已解锁的技能ID列表
        /// </summary>
        public List<string> unlockedSkillIDs;

        /// <summary>
        /// 最后保存时间（ISO 8601 格式字符串）
        /// </summary>
        public string lastSaveTime;

        /// <summary>
        /// 构造函数：初始化默认值
        /// </summary>
        public GameSaveData()
        {
            totalGlobalEggs = 0;
            unlockedSkillIDs = new List<string>();
            lastSaveTime = DateTime.Now.ToString("o"); // ISO 8601 格式
        }
    }
}

