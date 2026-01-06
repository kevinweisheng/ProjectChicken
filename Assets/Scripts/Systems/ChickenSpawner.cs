using UnityEngine;
using System.Collections.Generic;
using ProjectChicken.Units;
using ProjectChicken.Core;

namespace ProjectChicken.Systems
{
    /// <summary>
    /// 鸡生成器：持续在屏幕可见范围内生成新的鸡
    /// </summary>
    public class ChickenSpawner : MonoBehaviour
    {
        [Header("生成配置")]
        [SerializeField] private ChickenUnit chickenPrefab; // 鸡的预制体
        [SerializeField] private float spawnInterval = 3f; // 生成间隔（秒）
        [SerializeField] private int maxChickens = 10; // 屏幕上最大允许存在的鸡的数量

        [Header("生成范围")]
        [SerializeField] private float spawnPadding = 1f; // 生成边界内边距（避免在屏幕边缘生成）

        private float spawnTimer = 0f; // 生成计时器
        private Camera mainCamera; // 主摄像机
        private bool _canSpawn = false; // 是否允许生成（根据游戏状态控制，默认为 false）
        
        // 跟踪所有生成的鸡（用于清理）
        private List<ChickenUnit> spawnedChickens = new List<ChickenUnit>();

        private void Start()
        {
            // 获取主摄像机
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera == null)
            {
                Debug.LogError("ChickenSpawner: 未找到主摄像机！", this);
            }

            // 根据当前游戏状态初始化生成标志
            if (GameManager.Instance != null)
            {
                OnGameStateChanged(GameManager.Instance.CurrentState);
            }
            else
            {
                // 如果 GameManager 还未初始化，默认不允许生成
                _canSpawn = false;
            }
        }

        private void OnEnable()
        {
            // 订阅游戏状态改变事件
            GameManager.OnGameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            // 取消订阅：防止内存泄漏
            GameManager.OnGameStateChanged -= OnGameStateChanged;
        }

        private void Update()
        {
            // 如果不允许生成，直接返回
            if (!_canSpawn)
            {
                return;
            }

            // 检查必要组件
            if (chickenPrefab == null || mainCamera == null)
            {
                return;
            }

            // 生成循环：每隔 spawnInterval 秒检查一次
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                spawnTimer = 0f;
                TrySpawnChicken();
            }
        }

        /// <summary>
        /// 游戏状态改变事件处理
        /// </summary>
        /// <param name="newState">新游戏状态</param>
        private void OnGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.Playing:
                    // 游戏进行中，允许生成
                    _canSpawn = true;
                    Debug.Log("ChickenSpawner: 游戏开始，允许生成鸡", this);
                    break;

                case GameState.Preparation:
                case GameState.GameOver:
                    // 准备阶段或游戏结束，停止生成
                    _canSpawn = false;
                    Debug.Log($"ChickenSpawner: 状态为 {newState}，停止生成鸡", this);
                    break;
            }
        }

        /// <summary>
        /// 尝试生成一只新鸡
        /// </summary>
        private void TrySpawnChicken()
        {
            // 检查当前场景中 "Fat" 状态的鸡的数量
            int currentFatChickens = CountFatChickens();

            // 如果数量 < maxChickens，则生成一只新鸡
            if (currentFatChickens < maxChickens)
            {
                SpawnChicken();
            }
        }

        /// <summary>
        /// 统计当前场景中 "Fat" 状态的鸡的数量
        /// </summary>
        /// <returns>肥鸡的数量</returns>
        private int CountFatChickens()
        {
            int count = 0;
            // 查找场景中所有的 ChickenUnit
            ChickenUnit[] allChickens = FindObjectsByType<ChickenUnit>(FindObjectsSortMode.None);

            foreach (ChickenUnit chicken in allChickens)
            {
                // 检查是否为肥鸡状态
                if (chicken != null && chicken.IsFat)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 生成一只新鸡
        /// </summary>
        private void SpawnChicken()
        {
            // 获取随机生成位置
            Vector3 spawnPosition = GetRandomSpawnPosition();

            // 实例化鸡
            ChickenUnit newChicken = Instantiate(chickenPrefab, spawnPosition, Quaternion.identity);
            
            // 添加到生成列表中（用于后续清理）
            if (newChicken != null)
            {
                spawnedChickens.Add(newChicken);
            }
            
            // 调试信息（可选，可以在 Inspector 中关闭）
            Debug.Log($"ChickenSpawner: 在位置 {spawnPosition} 生成了一只鸡", this);
        }

        /// <summary>
        /// 清理所有生成的鸡（用于游戏重置）
        /// </summary>
        public void ClearAllChickens()
        {
            int destroyedCount = 0;

            // 反向遍历列表，安全删除（反向遍历避免索引问题）
            for (int i = spawnedChickens.Count - 1; i >= 0; i--)
            {
                if (spawnedChickens[i] != null)
                {
                    Destroy(spawnedChickens[i].gameObject);
                    destroyedCount++;
                }
            }

            // 清空列表
            spawnedChickens.Clear();

            Debug.Log($"ChickenSpawner: 已清理 {destroyedCount} 只鸡", this);
        }

        /// <summary>
        /// 获取随机生成位置（在屏幕可见范围内）
        /// </summary>
        /// <returns>世界坐标位置</returns>
        private Vector3 GetRandomSpawnPosition()
        {
            if (mainCamera == null)
            {
                return Vector3.zero;
            }

            // 使用 ViewportToWorldPoint 计算屏幕边界（更可靠的方法）
            // Viewport 坐标：左下角 (0,0)，右上角 (1,1)
            
            // 计算屏幕四个角的世界坐标
            Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0f, 0f, mainCamera.nearClipPlane + 10f));
            Vector3 topRight = mainCamera.ViewportToWorldPoint(new Vector3(1f, 1f, mainCamera.nearClipPlane + 10f));
            
            // 计算生成范围（减去内边距）
            float minX = bottomLeft.x + spawnPadding;
            float maxX = topRight.x - spawnPadding;
            float minY = bottomLeft.y + spawnPadding;
            float maxY = topRight.y - spawnPadding;
            
            // 确保范围有效
            if (maxX <= minX) maxX = minX + 1f;
            if (maxY <= minY) maxY = minY + 1f;
            
            // 在屏幕范围内随机生成位置
            float randomX = Random.Range(minX, maxX);
            float randomY = Random.Range(minY, maxY);
            
            // 使用固定的Z坐标（2D游戏通常为0）
            Vector3 spawnPosition = new Vector3(randomX, randomY, 0f);

            return spawnPosition;
        }

        /// <summary>
        /// 在 Scene 视图中绘制生成范围（调试用）
        /// </summary>
        private void OnDrawGizmos()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera == null) return;

            // 计算屏幕边界
            Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0f, 0f, mainCamera.nearClipPlane + 10f));
            Vector3 topRight = mainCamera.ViewportToWorldPoint(new Vector3(1f, 1f, mainCamera.nearClipPlane + 10f));
            
            // 绘制生成范围（绿色边框）
            Gizmos.color = Color.green;
            Vector3 bottomRight = new Vector3(topRight.x, bottomLeft.y, 0f);
            Vector3 topLeft = new Vector3(bottomLeft.x, topRight.y, 0f);
            
            // 绘制矩形边框
            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
            
            // 绘制减去内边距后的生成范围（黄色边框）
            Gizmos.color = Color.yellow;
            float minX = bottomLeft.x + spawnPadding;
            float maxX = topRight.x - spawnPadding;
            float minY = bottomLeft.y + spawnPadding;
            float maxY = topRight.y - spawnPadding;
            
            Vector3 innerBottomLeft = new Vector3(minX, minY, 0f);
            Vector3 innerTopRight = new Vector3(maxX, maxY, 0f);
            Vector3 innerBottomRight = new Vector3(maxX, minY, 0f);
            Vector3 innerTopLeft = new Vector3(minX, maxY, 0f);
            
            Gizmos.DrawLine(innerBottomLeft, innerBottomRight);
            Gizmos.DrawLine(innerBottomRight, innerTopRight);
            Gizmos.DrawLine(innerTopRight, innerTopLeft);
            Gizmos.DrawLine(innerTopLeft, innerBottomLeft);
        }
    }
}

