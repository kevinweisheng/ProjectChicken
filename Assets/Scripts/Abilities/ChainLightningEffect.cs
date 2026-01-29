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
        
        [Tooltip("闪电链材质（拖入你制作的材质即可替换默认闪电外观；留空则使用默认线条）")]
        [SerializeField] private Material lightningChainMaterial;
        
        [Tooltip("闪电颜色（使用自定义材质时，若材质支持 _Color 等则可能被覆盖）")]
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

        // 递归深度限制，防止无限循环
        private const int MAX_RECURSION_DEPTH = 5;
        private int currentRecursionDepth = 0;

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
            TriggerInternal(startTarget, baseDamage, maxTargets, initialChainChance, decayFactor, damagePercent, chainRange);
        }

        /// <summary>
        /// 内部触发方法，支持递归深度限制
        /// </summary>
        private void TriggerInternal(ChickenUnit startTarget, float baseDamage, int maxTargets, float initialChainChance, float decayFactor, float damagePercent, float chainRange)
        {
            if (startTarget == null)
            {
                Debug.LogWarning("ChainLightningEffect: 起始目标为空！", this);
                return;
            }

            // 检查递归深度，防止无限循环
            if (currentRecursionDepth >= MAX_RECURSION_DEPTH)
            {
                Debug.LogWarning($"ChainLightningEffect: 达到最大递归深度 {MAX_RECURSION_DEPTH}，停止触发闪电链", this);
                return;
            }

            // 增加递归深度
            currentRecursionDepth++;

            // 存储所有被击中的目标位置（用于绘制闪电链）
            List<Vector3> hitPositions = new List<Vector3>();
            
            // 已访问的目标列表（防止重复击中）
            HashSet<ChickenUnit> visited = new HashSet<ChickenUnit>();
            
            // 收集因为闪电链而下蛋的闪电鸡（用于后续触发它们的闪电链）
            List<ChickenUnit> triggeredLightningChickens = new List<ChickenUnit>();
            
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
            bool wasFatBeforeDamage = currentTarget.IsFat;
            bool wasLightningBeforeDamage = currentTarget.IsLightningChicken;
            currentTarget.TakeDamage(damage);
            hitPositions.Add(currentTarget.transform.position);
            visited.Add(currentTarget);
            
            // 检查起始目标是否因为伤害而下蛋，如果是闪电鸡，记录它
            // 注意：需要等待一帧，让 TakeDamage 和 OnChickenProducted 完成
            if (wasFatBeforeDamage && !currentTarget.IsFat && wasLightningBeforeDamage)
            {
                triggeredLightningChickens.Add(currentTarget);
            }

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
                    // 只对肥鸡（isFat == true）造成伤害，避免对已经下蛋的鸡造成伤害，防止无限循环
                    // 并避免重复命中已访问的目标
                    if (chicken != null && !visited.Contains(chicken) && chicken.IsFat)
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
                bool wasFatBeforeDamage2 = nextTarget.IsFat;
                bool wasLightningBeforeDamage2 = nextTarget.IsLightningChicken;
                nextTarget.TakeDamage(damage);
                hitPositions.Add(nextTarget.transform.position);
                visited.Add(nextTarget);
                
                // 检查目标是否因为伤害而下蛋，如果是闪电鸡，记录它
                // 注意：需要等待一帧，让 TakeDamage 和 OnChickenProducted 完成
                if (wasFatBeforeDamage2 && !nextTarget.IsFat && wasLightningBeforeDamage2)
                {
                    triggeredLightningChickens.Add(nextTarget);
                }

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

            // 减少递归深度
            currentRecursionDepth--;

            // 延迟触发所有因为闪电链而下蛋的闪电鸡的闪电链（形成连锁反应）
            if (triggeredLightningChickens.Count > 0)
            {
                StartCoroutine(TriggerDelayedLightningChains(triggeredLightningChickens, baseDamage, maxTargets, initialChainChance, decayFactor, damagePercent, chainRange));
            }
        }

        /// <summary>
        /// 延迟触发闪电鸡的闪电链（形成连锁反应）
        /// </summary>
        private IEnumerator TriggerDelayedLightningChains(List<ChickenUnit> lightningChickens, float baseDamage, int maxTargets, float initialChainChance, float decayFactor, float damagePercent, float chainRange)
        {
            // 等待一小段时间，确保所有鸡的下蛋逻辑都已完成
            yield return new WaitForSeconds(0.1f);

            // 触发每只闪电鸡的闪电链
            foreach (ChickenUnit chicken in lightningChickens)
            {
                if (chicken != null && chicken.IsLightningChicken)
                {
                    // 获取这只鸡的闪电链配置
                    float chickenTriggerChance = chicken.GetComponent<ChickenUnit>() != null ? 
                        GetLightningEggTriggerChance(chicken) : 1.0f;
                    
                    if (UnityEngine.Random.value < chickenTriggerChance)
                    {
                        TriggerInternal(chicken, baseDamage, maxTargets, initialChainChance, decayFactor, damagePercent, chainRange);
                    }
                }
            }
        }

        /// <summary>
        /// 获取鸡的闪电链触发概率
        /// </summary>
        private float GetLightningEggTriggerChance(ChickenUnit chicken)
        {
            if (chicken != null)
            {
                return chicken.LightningEggTriggerChance;
            }
            return 1.0f; // 默认100%概率
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
            
            // 配置 LineRenderer：优先使用你指定的闪电链材质
            if (lightningChainMaterial != null)
            {
                newLineRenderer.material = new Material(lightningChainMaterial);
            }
            else
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                newLineRenderer.material = shader != null ? new Material(shader) : new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended"));
            }
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
