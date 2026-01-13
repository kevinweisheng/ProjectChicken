using UnityEngine;
using System.Collections.Generic;
using ProjectChicken.Units;
using ProjectChicken.Core;

namespace ProjectChicken.Systems
{
    /// <summary>
    /// 鸡生成器：在回合开始时生成固定数量的鸡
    /// </summary>
    public class ChickenSpawner : MonoBehaviour
    {
        public static ChickenSpawner Instance { get; private set; }

        [Header("生成配置")]
        [SerializeField] private ChickenUnit chickenPrefab; // 鸡的预制体
        [SerializeField] private int baseInitialChickenCount = 5; // 基础初始鸡数量（回合开始时生成的数量）
        [SerializeField] private int baseMaxChickens = 10; // 基础最大鸡数量（可通过技能提升，用于分裂功能上限）

        [Header("生成范围")]
        [SerializeField] private float spawnPadding = 1f; // 生成边界内边距（避免在屏幕边缘生成）

        private Camera mainCamera; // 主摄像机
        private bool hasSpawnedInitialChickens = false; // 是否已经生成初始鸡（防止重复生成）
        
        // 跟踪所有生成的鸡（用于清理）
        private List<ChickenUnit> spawnedChickens = new List<ChickenUnit>();

        private void Awake()
        {
            // 单例模式：防止重复创建
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

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

            // 根据当前游戏状态初始化
            if (GameManager.Instance != null)
            {
                OnGameStateChanged(GameManager.Instance.CurrentState);
            }
            // 如果 GameManager 还未初始化，会在 OnGameStateChanged 事件触发时处理
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

        // 移除 Update 方法，不再需要持续生成

        /// <summary>
        /// 游戏状态改变事件处理
        /// </summary>
        /// <param name="newState">新游戏状态</param>
        private void OnGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.Playing:
                    // 游戏开始时，一次性生成初始数量的鸡
                    if (!hasSpawnedInitialChickens)
                    {
                        SpawnInitialChickens();
                        hasSpawnedInitialChickens = true;
                    }
                    Debug.Log("ChickenSpawner: 游戏开始，已生成初始鸡", this);
                    break;

                case GameState.Preparation:
                case GameState.GameOver:
                    // 准备阶段或游戏结束，重置生成标志
                    hasSpawnedInitialChickens = false;
                    Debug.Log($"ChickenSpawner: 状态为 {newState}，重置生成标志", this);
                    break;
            }
        }

        /// <summary>
        /// 在回合开始时生成初始数量的鸡
        /// </summary>
        private void SpawnInitialChickens()
        {
            if (chickenPrefab == null || mainCamera == null)
            {
                Debug.LogWarning("ChickenSpawner: chickenPrefab 或 mainCamera 为空，无法生成初始鸡！", this);
                return;
            }

            // 计算初始鸡数量：基础值 + 技能加成
            int initialCount = baseInitialChickenCount;
            if (UpgradeManager.Instance != null)
            {
                initialCount += UpgradeManager.Instance.ExtraInitialChickens;
            }

            Debug.Log($"ChickenSpawner: 开始生成 {initialCount} 只初始鸡", this);

            // 生成指定数量的鸡
            for (int i = 0; i < initialCount; i++)
            {
                SpawnChicken();
            }

            Debug.Log($"ChickenSpawner: 已生成 {initialCount} 只初始鸡", this);
        }

        /// <summary>
        /// 检查是否可以生成更多鸡（用于分裂功能）
        /// </summary>
        /// <returns>是否可以生成更多鸡</returns>
        public bool CanSpawnMore()
        {
            int currentFatChickens = CountFatChickens();
            int maxChickens = baseMaxChickens;
            if (UpgradeManager.Instance != null)
            {
                maxChickens += UpgradeManager.Instance.ExtraMaxChickens;
            }
            return currentFatChickens < maxChickens;
        }

        /// <summary>
        /// 在指定位置生成一只新鸡（用于分裂功能）
        /// </summary>
        /// <param name="position">生成位置</param>
        public void SpawnChickenAt(Vector3 position)
        {
            if (chickenPrefab == null)
            {
                Debug.LogWarning("ChickenSpawner: chickenPrefab 为空，无法生成鸡！", this);
                return;
            }

            // 添加小随机偏移，避免重叠
            Vector2 randomOffset = Random.insideUnitCircle * 0.5f;
            Vector3 spawnPosition = position + new Vector3(randomOffset.x, randomOffset.y, 0f);

            // 实例化鸡
            ChickenUnit newChicken = Instantiate(chickenPrefab, spawnPosition, Quaternion.identity);
            
            // 添加到生成列表中（用于后续清理）
            if (newChicken != null)
            {
                // 分裂生成的鸡继承原鸡的类型（普通鸡）
                newChicken.SetGolden(false);
                
                spawnedChickens.Add(newChicken);
                Debug.Log($"ChickenSpawner: 在位置 {spawnPosition} 通过分裂生成了一只鸡", this);
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
                // 根据金鸡生成率决定是否生成金鸡
                bool isGolden = ShouldSpawnGoldenChicken();
                newChicken.SetGolden(isGolden);
                
                spawnedChickens.Add(newChicken);
                
                // 调试信息
                string chickenType = isGolden ? "金鸡" : "普通鸡";
                Debug.Log($"ChickenSpawner: 在位置 {spawnPosition} 生成了一只{chickenType}", this);
            }
        }

        /// <summary>
        /// 判断是否应该生成金鸡
        /// </summary>
        /// <returns>是否生成金鸡</returns>
        private bool ShouldSpawnGoldenChicken()
        {
            if (UpgradeManager.Instance == null)
            {
                return false;
            }

            float spawnRate = UpgradeManager.Instance.GoldenChickenSpawnRate;
            return UnityEngine.Random.value < spawnRate;
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

            // 清理场景中所有剩余的鸡（包括瘦鸡预制体替换后的鸡）
            // 这样可以确保即使瘦鸡没有被添加到列表中，也能被清理
            ChickenUnit[] allChickens = FindObjectsByType<ChickenUnit>(FindObjectsSortMode.None);
            foreach (ChickenUnit chicken in allChickens)
            {
                if (chicken != null && chicken.gameObject != null)
                {
                    Destroy(chicken.gameObject);
                    destroyedCount++;
                }
            }

            // 重置生成标志，允许下一回合重新生成
            hasSpawnedInitialChickens = false;

            Debug.Log($"ChickenSpawner: 已清理 {destroyedCount} 只鸡（包括瘦鸡）", this);
        }

        /// <summary>
        /// 获取随机生成位置（在场地内或屏幕可见范围内）
        /// </summary>
        /// <returns>世界坐标位置</returns>
        private Vector3 GetRandomSpawnPosition()
        {
            // 优先使用场地边界，如果没有场地则使用屏幕边界（向后兼容）
            if (PlayArea.Instance != null)
            {
                // 使用场地边界生成
                Vector2 randomPos = PlayArea.Instance.GetRandomPositionInArea(spawnPadding);
                return new Vector3(randomPos.x, randomPos.y, 0f);
            }
            else if (mainCamera != null)
            {
                // 回退到屏幕边界（向后兼容）
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
            else
            {
                Debug.LogWarning("ChickenSpawner: 未找到 PlayArea 或 mainCamera，无法生成鸡！", this);
                return Vector3.zero;
            }
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

