using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ProjectChicken.Units;

namespace ProjectChicken.Abilities
{
    /// <summary>
    /// 链式闪电效果：对目标造成伤害，然后以衰减概率链式传播到附近目标
    /// 注意：可以通过 ChainLightningEffect.Instance 在任意位置触发
    /// </summary>
    public class ChainLightningEffect : MonoBehaviour
    {
        public static ChainLightningEffect Instance { get; private set; }
        [Header("视觉效果配置")]
        [Tooltip("闪电视觉效果持续时间（秒）")]
        [SerializeField] private float visualDuration = 0.2f;
        
        [Tooltip("闪电颜色")]
        [SerializeField] private Color lightningColor = Color.yellow;
        
        [Tooltip("闪电宽度")]
        [SerializeField] private float lightningWidth = 0.1f;
        
        [Tooltip("闪电排序顺序（应大于鸡的排序顺序，确保显示在鸡上方）")]
        [SerializeField] private int lightningSortingOrder = 300;
        
        [Tooltip("闪电排序图层名称")]
        [SerializeField] private string lightningSortingLayerName = "Default";
        
        [Tooltip("敌人所在的 Layer（用于检测目标）")]
        [SerializeField] private LayerMask enemyLayer;

        private LineRenderer lineRenderer; // 保留用于向后兼容（如果 Inspector 中已配置）

        private void Awake()
        {
            // 初始化全局单例，方便在其他脚本中通过 ChainLightningEffect.Instance 访问
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 如果在同一物体上挂了 LineRenderer，可以在需要时复用（例如调试用）
            lineRenderer = GetComponent<LineRenderer>();
        }

        /// <summary>
        /// 触发链式闪电效果
        /// </summary>
        /// <param name="startTarget">起始目标</param>
        /// <param name="baseDamage">基础伤害值</param>
        /// <param name="maxTargets">最大目标数量（包括起始目标）</param>
        /// <param name="initialChainChance">初始链式概率（0-1，通常为1.0表示100%）</param>
        /// <param name="decayFactor">衰减因子（每次链式后概率乘以这个值，例如0.5表示每次减半）</param>
        /// <param name="damagePercent">伤害百分比（每次链式的伤害 = baseDamage * damagePercent）</param>
        /// <param name="chainRange">链式范围（世界单位）</param>
        public void Trigger(ChickenUnit startTarget, float baseDamage, int maxTargets, float initialChainChance, float decayFactor, float damagePercent, float chainRange)
        {
            if (startTarget == null)
            {
                Debug.LogWarning("ChainLightningEffect: 起始目标为空！", this);
                return;
            }

            // 存储所有被击中的目标位置（用于绘制闪电链）
            List<Vector3> hitPositions = new List<Vector3>();
            
            // 已访问的目标列表（防止重复击中）
            HashSet<ChickenUnit> visited = new HashSet<ChickenUnit>();
            
            // 确定用于链式传播的 LayerMask：
            // 如果在 Inspector 中已经配置了 enemyLayer，则直接使用；
            // 否则默认使用起始目标所在的 Layer（确保至少能命中同一层的其他鸡）。
            int effectiveLayerMask = enemyLayer.value != 0
                ? enemyLayer.value
                : (1 << startTarget.gameObject.layer);

            // 当前目标
            ChickenUnit currentTarget = startTarget;
            
            // 当前链式概率
            float currentChainChance = initialChainChance;

            // Step 1: 对起始目标造成伤害
            float damage = baseDamage * damagePercent;
            currentTarget.TakeDamage(damage);
            hitPositions.Add(currentTarget.transform.position);
            visited.Add(currentTarget);

            // Step 2 & 3: 链式传播循环
            for (int i = 1; i < maxTargets; i++)
            {
                // 检查是否继续链式
                // 如果随机值大于等于当前概率，停止链式（例如：概率0.5时，Random.value在0.5-1.0之间会停止）
                if (Random.value >= currentChainChance)
                {
                    break;
                }

                // 在当前目标位置查找附近的目标
                Vector3 searchPosition = currentTarget.transform.position;
                Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(searchPosition, chainRange, effectiveLayerMask);

                // 找到最近的未访问目标
                ChickenUnit nextTarget = null;
                float closestDistance = float.MaxValue;

                foreach (Collider2D collider in nearbyColliders)
                {
                    ChickenUnit chicken = collider.GetComponent<ChickenUnit>();
                    // 允许链式闪电命中所有鸡（不再仅限制为肥鸡），并避免重复命中已访问的目标
                    if (chicken != null && !visited.Contains(chicken))
                    {
                        float distance = Vector3.Distance(searchPosition, chicken.transform.position);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            nextTarget = chicken;
                        }
                    }
                }

                // 如果没有找到目标，停止链式
                if (nextTarget == null)
                {
                    break;
                }

                // 对找到的目标造成伤害
                nextTarget.TakeDamage(damage);
                hitPositions.Add(nextTarget.transform.position);
                visited.Add(nextTarget);

                // 衰减概率
                currentChainChance *= decayFactor;

                // 更新当前目标
                currentTarget = nextTarget;
            }

            // 绘制闪电链（即使只有一个目标也显示，至少显示起始位置）
            if (hitPositions.Count >= 1)
            {
                DrawLightningChain(hitPositions);
            }
        }

        /// <summary>
        /// 绘制闪电链（每次调用创建独立的 LineRenderer，支持多个同时存在）
        /// </summary>
        /// <param name="positions">所有被击中的目标位置</param>
        private void DrawLightningChain(List<Vector3> positions)
        {
            // 为每次触发创建独立的闪电链对象
            GameObject lightningObject = new GameObject("LightningChain");
            lightningObject.transform.SetParent(transform);
            lightningObject.transform.localPosition = Vector3.zero;
            lightningObject.transform.localRotation = Quaternion.identity;
            lightningObject.transform.localScale = Vector3.one;
            
            LineRenderer newLineRenderer = lightningObject.AddComponent<LineRenderer>();
            
            // 配置 LineRenderer
            newLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            newLineRenderer.startColor = lightningColor;
            newLineRenderer.endColor = lightningColor;
            newLineRenderer.startWidth = lightningWidth;
            newLineRenderer.endWidth = lightningWidth;
            newLineRenderer.useWorldSpace = true;
            newLineRenderer.sortingOrder = lightningSortingOrder;
            newLineRenderer.sortingLayerName = lightningSortingLayerName;

            // 如果只有一个位置，创建一个小的闪电效果（从位置向上一点）
            if (positions.Count == 1)
            {
                newLineRenderer.positionCount = 2;
                Vector3 startPos = positions[0];
                Vector3 endPos = startPos + Vector3.up * 0.5f; // 向上延伸一点
                newLineRenderer.SetPosition(0, startPos);
                newLineRenderer.SetPosition(1, endPos);
            }
            else
            {
                // 设置 LineRenderer 的点数
                newLineRenderer.positionCount = positions.Count;

                // 设置所有位置
                for (int i = 0; i < positions.Count; i++)
                {
                    newLineRenderer.SetPosition(i, positions[i]);
                }
            }

            // 启动协程，在指定时间后销毁闪电链对象
            StartCoroutine(DestroyLightningAfterDelay(lightningObject));
        }

        /// <summary>
        /// 延迟销毁闪电效果对象
        /// </summary>
        /// <param name="lightningObject">要销毁的闪电链对象</param>
        private IEnumerator DestroyLightningAfterDelay(GameObject lightningObject)
        {
            yield return new WaitForSeconds(visualDuration);
            
            if (lightningObject != null)
            {
                Destroy(lightningObject);
            }
        }
    }
}
