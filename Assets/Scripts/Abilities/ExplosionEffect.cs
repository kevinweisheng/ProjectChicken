using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ProjectChicken.Core;

namespace ProjectChicken.Abilities
{
    /// <summary>
    /// 爆炸效果：对指定位置周围一定范围内的所有目标造成伤害
    /// </summary>
    public class ExplosionEffect : MonoBehaviour
    {
        public static ExplosionEffect Instance { get; private set; }

        [Header("视觉效果配置")]
        [Tooltip("爆炸视觉效果持续时间（秒）")]
        [SerializeField] private float visualDuration = 0.3f;
        
        [Tooltip("爆炸颜色")]
        [SerializeField] private Color explosionColor = Color.red;
        
        [Tooltip("爆炸范围指示器（可选，用于显示爆炸范围）")]
        [SerializeField] private GameObject explosionRangeIndicator;
        
        [Tooltip("爆炸粒子特效预制体（可选）")]
        [SerializeField] private GameObject explosionParticlePrefab;
        
        [Tooltip("敌人所在的 Layer（用于检测目标）")]
        [SerializeField] private LayerMask enemyLayer;

        private void Awake()
        {
            // 初始化全局单例
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// 触发爆炸效果
        /// </summary>
        /// <param name="position">爆炸位置</param>
        /// <param name="baseDamage">基础伤害值</param>
        /// <param name="explosionRadius">爆炸范围（世界单位）</param>
        /// <param name="damageFalloff">伤害衰减（距离越远伤害越低，0-1，0表示无衰减，1表示完全衰减）</param>
        public void Trigger(Vector3 position, float baseDamage, float explosionRadius, float damageFalloff = 0.5f)
        {
            // 确定用于检测的 LayerMask
            int effectiveLayerMask = enemyLayer.value != 0
                ? enemyLayer.value
                : -1; // 如果未设置，检测所有层

            // 在爆炸范围内查找所有目标
            Collider2D[] targets = Physics2D.OverlapCircleAll(position, explosionRadius, effectiveLayerMask);

            // 对每个目标造成伤害（根据距离衰减）
            foreach (Collider2D target in targets)
            {
                IDamageable damageable = target.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    // 计算距离
                    float distance = Vector3.Distance(position, target.transform.position);
                    
                    // 计算伤害衰减（距离越远伤害越低）
                    float distancePercent = Mathf.Clamp01(distance / explosionRadius);
                    float damageMultiplier = 1f - (distancePercent * damageFalloff);
                    float finalDamage = baseDamage * damageMultiplier;
                    
                    // 造成伤害
                    damageable.TakeDamage(finalDamage);
                }
            }

            // 显示爆炸视觉效果
            ShowExplosionVisual(position, explosionRadius);
        }

        /// <summary>
        /// 显示爆炸视觉效果
        /// </summary>
        private void ShowExplosionVisual(Vector3 position, float radius)
        {
            // 如果有粒子特效预制体，实例化它
            if (explosionParticlePrefab != null)
            {
                GameObject particleInstance = Instantiate(explosionParticlePrefab, position, Quaternion.identity);
                // 粒子系统会自动销毁，或者可以在这里设置销毁时间
                Destroy(particleInstance, visualDuration + 1f);
            }

            // 如果有范围指示器，显示它
            if (explosionRangeIndicator != null)
            {
                GameObject indicator = Instantiate(explosionRangeIndicator, position, Quaternion.identity);
                // 设置指示器大小
                indicator.transform.localScale = Vector3.one * radius * 2f;
                Destroy(indicator, visualDuration);
            }
            else
            {
                // 如果没有指定特效，创建一个简单的圆形指示器
                StartCoroutine(DrawExplosionCircle(position, radius));
            }
        }

        /// <summary>
        /// 绘制爆炸圆形指示器（临时方案，如果没有指定特效）
        /// </summary>
        private IEnumerator DrawExplosionCircle(Vector3 position, float radius)
        {
            // 创建一个简单的圆形指示器
            GameObject circleObject = new GameObject("ExplosionIndicator");
            circleObject.transform.position = position;
            circleObject.transform.localScale = Vector3.one * radius * 2f;

            // 添加 SpriteRenderer 来显示圆形
            SpriteRenderer sr = circleObject.AddComponent<SpriteRenderer>();
            
            // 创建一个简单的圆形纹理
            Texture2D texture = new Texture2D(64, 64);
            Vector2 center = new Vector2(32, 32);
            float circleRadius = 30f;
            
            for (int x = 0; x < 64; x++)
            {
                for (int y = 0; y < 64; y++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist <= circleRadius && dist >= circleRadius - 2f)
                    {
                        texture.SetPixel(x, y, explosionColor);
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }
            texture.Apply();
            
            Sprite circleSprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 100f);
            sr.sprite = circleSprite;
            sr.color = explosionColor;
            sr.sortingOrder = 300;
            sr.sortingLayerName = "Default";

            // 等待指定时间后销毁
            yield return new WaitForSeconds(visualDuration);
            
            if (circleObject != null)
            {
                Destroy(circleObject);
            }
        }
    }
}
