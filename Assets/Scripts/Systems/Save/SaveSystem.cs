using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ProjectChicken.Systems.Save
{
    /// <summary>
    /// 保存系统：提供静态方法用于保存和加载游戏数据
    /// </summary>
    public static class SaveSystem
    {
        /// <summary>
        /// 保存文件路径
        /// </summary>
        private static string SaveFilePath => Path.Combine(Application.persistentDataPath, "savefile.json");

        /// <summary>
        /// 保存游戏数据到文件
        /// </summary>
        /// <param name="data">要保存的游戏数据</param>
        public static void Save(GameSaveData data)
        {
            if (data == null)
            {
                Debug.LogError("SaveSystem: 尝试保存空数据！");
                return;
            }

            try
            {
                // 更新保存时间
                data.lastSaveTime = System.DateTime.Now.ToString("o"); // ISO 8601 格式

                // 将数据序列化为 JSON 字符串
                string json = JsonUtility.ToJson(data, true); // true 表示格式化输出（便于阅读）

                // 写入文件
                File.WriteAllText(SaveFilePath, json);

                Debug.Log($"SaveSystem: 游戏数据已保存到 {SaveFilePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"SaveSystem: 保存数据时发生错误：{e.Message}");
            }
        }

        /// <summary>
        /// 从文件加载游戏数据
        /// </summary>
        /// <returns>加载的游戏数据，如果文件不存在则返回新的 GameSaveData</returns>
        public static GameSaveData Load()
        {
            // 检查文件是否存在
            if (!File.Exists(SaveFilePath))
            {
                Debug.Log($"SaveSystem: 保存文件不存在，返回新游戏数据。路径：{SaveFilePath}");
                return new GameSaveData();
            }

            try
            {
                // 读取文件内容
                string json = File.ReadAllText(SaveFilePath);

                // 检查文件是否为空
                if (string.IsNullOrEmpty(json))
                {
                    Debug.LogWarning("SaveSystem: 保存文件为空，返回新游戏数据。");
                    return new GameSaveData();
                }

                // 反序列化 JSON 字符串为 GameSaveData 对象
                GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

                if (data == null)
                {
                    Debug.LogWarning("SaveSystem: 反序列化失败，返回新游戏数据。");
                    return new GameSaveData();
                }

                // 确保列表已初始化（兼容旧版本保存文件）
                if (data.skillRecords == null)
                {
                    data.skillRecords = new List<GameSaveData.SkillSaveRecord>();
                }

                Debug.Log($"SaveSystem: 游戏数据已从 {SaveFilePath} 加载");
                return data;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"SaveSystem: 加载数据时发生错误：{e.Message}，返回新游戏数据。");
                return new GameSaveData();
            }
        }

        /// <summary>
        /// 检查保存文件是否存在
        /// </summary>
        /// <returns>文件是否存在</returns>
        public static bool SaveFileExists()
        {
            return File.Exists(SaveFilePath);
        }

        /// <summary>
        /// 删除保存文件（用于重置游戏）
        /// </summary>
        public static void DeleteSaveFile()
        {
            if (File.Exists(SaveFilePath))
            {
                try
                {
                    File.Delete(SaveFilePath);
                    Debug.Log($"SaveSystem: 保存文件已删除：{SaveFilePath}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"SaveSystem: 删除保存文件时发生错误：{e.Message}");
                }
            }
        }
    }
}

