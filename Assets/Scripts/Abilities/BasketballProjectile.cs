using UnityEngine;
using System.Collections.Generic;
using ProjectChicken.Units;
using ProjectChicken.Core;
using ProjectChicken.Systems;

namespace ProjectChicken.Abilities
{
    /// <summary>
    /// 篮球投射物：沿直线飞行，与场地边界反弹，对碰到的鸡造成伤害，反弹次数用尽后消失。
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class BasketballProjectile : MonoBehaviour
    {
        private Vector2 velocity;
        private float damage;
        private int maxBounces;
        private int bouncesLeft;
        private HashSet<ChickenUnit> hitChickens = new HashSet<ChickenUnit>();
        private float radius;

        /// <summary>
        /// 初始化篮球：位置、飞行方向（会归一化）、速度、伤害、最大反弹次数。
        /// </summary>
        public void Initialize(Vector3 position, Vector2 direction, float speed, float damageAmount, int maxBounceCount)
        {
            transform.position = position;
            velocity = direction.normalized * speed;
            damage = damageAmount;
            maxBounces = Mathf.Max(0, maxBounceCount);
            bouncesLeft = maxBounces;
            hitChickens.Clear();

            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.simulated = true;
                rb.linearVelocity = Vector2.zero;
            }

            var col = GetComponent<CircleCollider2D>();
            if (col != null)
            {
                col.isTrigger = true;
                radius = col.radius;
            }
            else
            {
                radius = 0.2f;
            }
        }

        private void Update()
        {
            if (velocity.sqrMagnitude <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            float dt = Time.deltaTime;
            Vector2 pos = transform.position;
            pos += velocity * dt;
            transform.position = pos;

            // 使用鸡活动边界作为反弹边界（与 PlayArea 一致）
            if (PlayArea.Instance == null)
            {
                return;
            }

            float minX = PlayArea.Instance.ChickenMinX;
            float maxX = PlayArea.Instance.ChickenMaxX;
            float minY = PlayArea.Instance.ChickenMinY;
            float maxY = PlayArea.Instance.ChickenMaxY;

            bool bounced = false;
            if (pos.x - radius < minX)
            {
                velocity.x = Mathf.Abs(velocity.x);
                pos.x = minX + radius;
                bounced = true;
            }
            else if (pos.x + radius > maxX)
            {
                velocity.x = -Mathf.Abs(velocity.x);
                pos.x = maxX - radius;
                bounced = true;
            }

            if (pos.y - radius < minY)
            {
                velocity.y = Mathf.Abs(velocity.y);
                pos.y = minY + radius;
                bounced = true;
            }
            else if (pos.y + radius > maxY)
            {
                velocity.y = -Mathf.Abs(velocity.y);
                pos.y = maxY - radius;
                bounced = true;
            }

            if (bounced)
            {
                transform.position = pos;
                bouncesLeft--;
                if (bouncesLeft < 0)
                {
                    Destroy(gameObject);
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            ChickenUnit chicken = other.GetComponent<ChickenUnit>();
            if (chicken == null)
            {
                return;
            }

            // 只对肥鸡造成伤害，且每只鸡只伤害一次
            if (!chicken.IsFat)
            {
                return;
            }
            if (hitChickens.Contains(chicken))
            {
                return;
            }

            hitChickens.Add(chicken);
            IDamageable damageable = chicken.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }

            // 与鸡“碰撞”反弹：速度改为从鸡指向球的方向，球不再穿过鸡
            Vector2 ballPos = transform.position;
            Vector2 chickenPos = other.ClosestPoint(ballPos);
            Vector2 away = (ballPos - chickenPos).normalized;
            if (away.sqrMagnitude < 0.001f)
            {
                away = -velocity.normalized;
            }
            float speed = velocity.magnitude;
            velocity = away * speed;

            // 把球稍微推出鸡的碰撞体，避免卡在鸡里连续触发
            float pushOut = radius + 0.15f;
            transform.position = ballPos + away * pushOut;
        }
    }
}
