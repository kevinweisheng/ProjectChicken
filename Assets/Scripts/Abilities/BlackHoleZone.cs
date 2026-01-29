using UnityEngine;
using ProjectChicken.Units;

namespace ProjectChicken.Abilities
{
    /// <summary>
    /// 黑洞区域：持续将范围内的鸡吸引向中心，不造成伤害，仅聚拢效果。持续时间结束后消失。
    /// </summary>
    public class BlackHoleZone : MonoBehaviour
    {
        private float radius;
        private float duration;
        private float pullForce;
        private float spawnTime;

        /// <summary>
        /// 初始化黑洞：位置、吸引范围、持续时间、吸引力度。
        /// </summary>
        public void Initialize(Vector3 position, float radiusValue, float durationValue, float pullForceValue)
        {
            transform.position = position;
            radius = Mathf.Max(0.1f, radiusValue);
            duration = Mathf.Max(0.1f, durationValue);
            pullForce = Mathf.Max(0f, pullForceValue);
            spawnTime = Time.time;
        }

        private void FixedUpdate()
        {
            if (Time.time - spawnTime >= duration)
            {
                Destroy(gameObject);
                return;
            }

            Vector2 center = transform.position;
            ChickenUnit[] chickens = FindObjectsByType<ChickenUnit>(FindObjectsSortMode.None);

            foreach (ChickenUnit chicken in chickens)
            {
                if (chicken == null || !chicken.IsFat)
                {
                    continue;
                }

                Rigidbody2D rb = chicken.GetComponent<Rigidbody2D>();
                if (rb == null)
                {
                    continue;
                }

                Vector2 toChicken = (Vector2)chicken.transform.position - center;
                float dist = toChicken.magnitude;
                if (dist < 0.01f || dist > radius)
                {
                    continue;
                }

                float t = dist / radius; // 0=中心, 1=外围
                // 从外围到中心吸引力越来越大：中心最强，外围最弱
                float pullStrength = pullForce * (1f - t);

                // 向心力：指向中心
                Vector2 inward = -toChicken.normalized;
                rb.AddForce(inward * pullStrength);

                // 旋转吸引：切向力，让鸡绕中心旋转（顺时针）
                Vector2 tangent = new Vector2(toChicken.y, -toChicken.x).normalized;
                float rotationStrength = pullForce * 0.4f * (1f - t); // 旋转力也由外到内增大
                rb.AddForce(tangent * rotationStrength);
            }
        }
    }
}
