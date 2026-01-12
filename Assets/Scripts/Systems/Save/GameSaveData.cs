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
        /// 技能保存记录：存储技能的ID和等级
        /// </summary>
        [System.Serializable]
        public class SkillSaveRecord
        {
            public string id;
            public int level;

            public SkillSaveRecord(string i, int l)
            {
                id = i;
                level = l;
            }
        }

        /// <summary>
        /// 全局永久货币（跨局保存）
        /// </summary>
        public int totalGlobalEggs;

        /// <summary>
        /// 技能记录列表：存储每个技能的ID和当前等级
        /// </summary>
        public List<SkillSaveRecord> skillRecords;

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
            skillRecords = new List<SkillSaveRecord>();
            lastSaveTime = DateTime.Now.ToString("o"); // ISO 8601 格式
        }
    }
}

