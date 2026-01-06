using UnityEngine;
using ProjectChicken.Core;

namespace ProjectChicken.Systems
{
    /// <summary>
    /// 鸡蛋投射物：从鸡的位置飞向目标位置，到达后触发资源增加
    /// </summary>
    public class EggProjectile : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f; // 移动速度
        [SerializeField] private float arrivalDistance = 0.1f; // 到达判定距离

        private Vector3 targetPosition; // 目标位置
        private bool isInitialized = false; // 是否已初始化

        /// <summary>
        /// 初始化投射物
        /// </summary>
        /// <param name="targetPos">目标位置</param>
        public void Initialize(Vector3 targetPos)
        {
            targetPosition = targetPos;
            isInitialized = true;
        }

        private void Update()
        {
            if (!isInitialized) return;

            // 使用 MoveTowards 平滑飞向目标位置
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            // 检查是否到达目标
            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
            if (distanceToTarget < arrivalDistance)
            {
                OnArrived();
            }
        }

        /// <summary>
        /// 到达目标位置：销毁自身并调用资源增加逻辑
        /// </summary>
        private void OnArrived()
        {
            // 调用资源管理器增加鸡蛋（制造"飞到了才算数"的延迟满足感）
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.AddEgg(1);
            }
            else
            {
                Debug.LogWarning("EggProjectile: ResourceManager.Instance 为空！无法增加鸡蛋。");
            }

            // 销毁自身
            Destroy(gameObject);
        }
    }
}

