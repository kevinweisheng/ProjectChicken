using UnityEngine;
using System;
using ProjectChicken.Units; // 引用鸡的命名空间
using ProjectChicken.Systems; // 引用蛋的命名空间

namespace ProjectChicken.Core
{
    /// <summary>
    /// 游戏状态枚举
    /// </summary>
    public enum GameState
    {
        Preparation,    // 准备阶段（玩家进行升级、查看全局鸡蛋数量）
        Playing,        // 游戏中
        GameOver        // 游戏结束
    }

    /// <summary>
    /// 游戏管理器：负责游戏核心逻辑，包括游戏状态管理、倒计时、鸡蛋生成等
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // 游戏状态事件
        public static event Action<GameState> OnGameStateChanged;
        // 倒计时更新事件
        public static event Action<float> OnTimerUpdated;

        [Header("游戏配置")]
        [SerializeField] private float sessionDuration = 60f; // 初始单局游戏时长（秒，可在 Inspector 中配置，实际值从 UpgradeManager 读取）

        [Header("游戏对象配置")]
        [SerializeField] private EggProjectile eggPrefab; // 拖入蛋的Prefab
        [SerializeField] private RectTransform uiTarget;  // 拖入UI上的目标位置(比如右上角的金币图标)
        [SerializeField] private Camera mainCamera;      // 主摄像机（用于坐标转换）

        // 游戏状态
        private GameState currentState = GameState.Preparation;
        private float remainingTime; // 剩余时间

        /// <summary>
        /// 当前游戏状态（只读属性）
        /// </summary>
        public GameState CurrentState => currentState;

        /// <summary>
        /// 剩余时间（只读属性）
        /// </summary>
        public float RemainingTime => remainingTime;

        private void Awake()
        {
            // 单例模式：防止重复创建
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            
            // 如果没有指定摄像机，尝试获取主摄像机
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
            
            // 如果需要跨场景保持，取消下面这行的注释
            // DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // 初始化游戏：进入准备阶段（玩家进行升级、查看全局鸡蛋数量）
            EnterPreparationPhase();
        }

        private void Update()
        {
            // 只在游戏进行中时更新倒计时
            if (currentState == GameState.Playing)
            {
                // 更新倒计时
                remainingTime -= Time.deltaTime;

                // 触发倒计时更新事件
                OnTimerUpdated?.Invoke(remainingTime);

                // 检查时间是否用完
                if (remainingTime <= 0f)
                {
                    remainingTime = 0f; // 确保不会变成负数
                    ChangeGameState(GameState.GameOver);
                }
            }
        }

        private void OnEnable()
        {
            // 订阅事件：当鸡产出时，执行 SpawnEgg
            ChickenUnit.OnChickenProduct += SpawnEgg;
        }

        private void OnDisable()
        {
            // 取消订阅：养成好习惯，防止内存泄漏
            ChickenUnit.OnChickenProduct -= SpawnEgg;
        }

        /// <summary>
        /// 生成鸡蛋：当鸡产出时调用
        /// </summary>
        /// <param name="spawnPosition">生成位置（鸡的位置）</param>
        private void SpawnEgg(Vector3 spawnPosition)
        {
            // 检查必要组件是否配置
            if (eggPrefab == null)
            {
                Debug.LogWarning("GameManager: eggPrefab 未配置！请在 Inspector 中拖入鸡蛋预制体。", this);
                return;
            }

            if (uiTarget == null)
            {
                Debug.LogWarning("GameManager: uiTarget 未配置！请在 Inspector 中拖入UI目标位置。", this);
                return;
            }

            if (mainCamera == null)
            {
                Debug.LogWarning("GameManager: mainCamera 未配置！", this);
                return;
            }

            // 将UI的世界坐标转换为场景世界坐标
            Vector3 uiWorldPosition = ConvertUIToWorldPosition(uiTarget);

            // 查找生成位置附近的鸡，判断是否为金鸡
            bool isGoldenEgg = false;
            Collider2D[] colliders = Physics2D.OverlapCircleAll(spawnPosition, 0.5f);
            foreach (Collider2D col in colliders)
            {
                ChickenUnit chicken = col.GetComponent<ChickenUnit>();
                if (chicken != null && chicken.IsGolden)
                {
                    isGoldenEgg = true;
                    break;
                }
            }

            // 生成蛋
            EggProjectile newEgg = Instantiate(eggPrefab, spawnPosition, Quaternion.identity);
            // 告诉蛋往哪里飞（使用转换后的世界坐标），并传递是否为金蛋的信息
            newEgg.Initialize(uiWorldPosition, isGoldenEgg);
        }

        /// <summary>
        /// 将UI坐标转换为场景世界坐标
        /// </summary>
        /// <param name="rectTransform">UI元素的RectTransform</param>
        /// <returns>场景世界坐标</returns>
        private Vector3 ConvertUIToWorldPosition(RectTransform rectTransform)
        {
            // 获取Canvas的Camera（用于坐标转换）
            Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
            Camera canvasCamera = canvas != null ? canvas.worldCamera : null;
            
            // 如果Canvas没有指定Camera（Screen Space - Overlay模式），使用null
            // 如果Canvas指定了Camera（Screen Space - Camera模式），使用Canvas的Camera
            Camera uiCamera = canvasCamera != null ? canvasCamera : null;
            
            // 将UI的世界坐标转换为屏幕坐标
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, rectTransform.position);
            
            // 将屏幕坐标转换为场景世界坐标
            // 设置合适的Z深度（2D游戏通常使用固定的Z值，比如0或摄像机的nearClipPlane）
            Vector3 screenPoint3D = new Vector3(screenPoint.x, screenPoint.y, mainCamera.nearClipPlane + 10f);
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPoint3D);
            
            // 保持Z坐标为场景中的Z坐标（2D游戏通常为0）
            worldPosition.z = 0f; // 根据你的2D游戏设置调整这个值
            
            return worldPosition;
        }

        /// <summary>
        /// 改变游戏状态
        /// </summary>
        /// <param name="newState">新状态</param>
        private void ChangeGameState(GameState newState)
        {
            if (currentState == newState)
            {
                Debug.Log($"GameManager: 状态没有变化（当前状态：{currentState}），跳过处理", this);
                return; // 状态没有变化，不需要处理
            }

            GameState oldState = currentState;
            currentState = newState;
            
            // 触发状态改变事件
            Debug.Log($"GameManager: 准备触发状态改变事件，从 {oldState} 到 {newState}，订阅者数量：{(OnGameStateChanged?.GetInvocationList().Length ?? 0)}", this);
            OnGameStateChanged?.Invoke(currentState);

            Debug.Log($"GameManager: 游戏状态改变为 {currentState}", this);
        }

        /// <summary>
        /// 进入准备阶段：玩家进行升级、查看全局鸡蛋数量的时候
        /// </summary>
        public void EnterPreparationPhase()
        {
            // 切换到准备阶段
            ChangeGameState(GameState.Preparation);
            
            Debug.Log("GameManager: 进入准备阶段，玩家可以进行升级和查看全局鸡蛋数量", this);
        }

        /// <summary>
        /// 开始新的一局游戏：重置单局分数、重置计时器，切换到 Playing 状态
        /// </summary>
        public void RestartGame()
        {
            // 必须先调用 ResetSessionScore() 清空上一局的临时分数
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.ResetSessionScore();
            }

            // 从 UpgradeManager 获取当前回合时间（如果可用），否则使用初始值
            float currentSessionDuration = GetCurrentSessionDuration();
            
            // 重置倒计时
            remainingTime = currentSessionDuration;

            // 切换到 Playing 状态
            ChangeGameState(GameState.Playing);

            // 触发倒计时更新事件（通知UI更新）
            OnTimerUpdated?.Invoke(remainingTime);

            Debug.Log($"GameManager: 游戏重新开始，时长 {currentSessionDuration} 秒", this);
        }

        /// <summary>
        /// 获取当前回合时间（从 UpgradeManager 读取）
        /// </summary>
        /// <returns>当前回合时间（秒）</returns>
        private float GetCurrentSessionDuration()
        {
            if (UpgradeManager.Instance != null)
            {
                return UpgradeManager.Instance.CurrentSessionDuration;
            }

            // 如果 UpgradeManager 未初始化，返回初始值
            return sessionDuration;
        }
    }
}