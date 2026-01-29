using UnityEngine;
using ProjectChicken.Systems;

namespace ProjectChicken.Abilities
{
    /// <summary>
    /// 篮球效果：篮球鸡下蛋时在此处生成篮球投射物，沿指定方向飞行、反弹、对鸡造成伤害。
    /// </summary>
    public class BasketballEffect : MonoBehaviour
    {
        public static BasketballEffect Instance { get; private set; }

        [Header("篮球预制体")]
        [Tooltip("篮球投射物预制体（需带 BasketballProjectile 和 CircleCollider2D）。为空则运行时生成简单圆形")]
        [SerializeField] private GameObject basketballPrefab;

        [Header("默认参数（预制体未指定时使用）")]
        [SerializeField] private float defaultRadius = 0.25f;
        [SerializeField] private Color defaultColor = new Color(0.9f, 0.4f, 0.1f);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// 在指定位置朝指定方向发射一颗篮球。
        /// </summary>
        /// <param name="position">发射位置</param>
        /// <param name="direction">飞行方向（会归一化）</param>
        /// <param name="damage">对鸡造成的伤害</param>
        /// <param name="speed">飞行速度（世界单位/秒）</param>
        /// <param name="maxBounces">与边界反弹次数上限，超过后消失</param>
        public void Trigger(Vector3 position, Vector2 direction, float damage, float speed, int maxBounces)
        {
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = Random.insideUnitCircle.normalized;
            }

            GameObject ballObj;
            BasketballProjectile projectile;

            if (basketballPrefab != null)
            {
                ballObj = Instantiate(basketballPrefab, position, Quaternion.identity);
                projectile = ballObj.GetComponent<BasketballProjectile>();
                if (projectile == null)
                {
                    projectile = ballObj.AddComponent<BasketballProjectile>();
                }
            }
            else
            {
                ballObj = CreateDefaultBasketball(position);
                projectile = ballObj.GetComponent<BasketballProjectile>();
            }

            if (projectile != null)
            {
                projectile.Initialize(position, direction, speed, damage, maxBounces);
            }
            else
            {
                Destroy(ballObj);
            }
        }

        private GameObject CreateDefaultBasketball(Vector3 position)
        {
            GameObject go = new GameObject("Basketball");
            go.transform.position = position;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = defaultRadius;
            col.isTrigger = true;

            var proj = go.AddComponent<BasketballProjectile>();

            // 简单圆形显示
            var sr = go.AddComponent<SpriteRenderer>();
            Texture2D tex = new Texture2D(32, 32);
            Vector2 c = new Vector2(16, 16);
            float r = 14f;
            for (int x = 0; x < 32; x++)
                for (int y = 0; y < 32; y++)
                    tex.SetPixel(x, y, (Vector2.Distance(new Vector2(x, y), c) <= r) ? defaultColor : Color.clear);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 100f);
            sr.sortingOrder = 150;

            return go;
        }
    }
}
