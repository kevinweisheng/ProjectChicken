using UnityEngine;

namespace ProjectChicken.Abilities
{
    /// <summary>
    /// 黑洞效果：黑洞鸡下蛋时在此处生成黑洞区域，吸引范围内的鸡向中心聚拢（不造成伤害）。
    /// </summary>
    public class BlackHoleEffect : MonoBehaviour
    {
        public static BlackHoleEffect Instance { get; private set; }

        [Header("黑洞视觉（可选）")]
        [SerializeField] private GameObject blackHoleVisualPrefab;
        [SerializeField] private float defaultVisualRadius = 0.5f;
        [SerializeField] private Color defaultVisualColor = new Color(0.2f, 0f, 0.3f, 0.5f);

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
        /// 在指定位置生成一个黑洞。
        /// </summary>
        /// <param name="position">黑洞中心位置</param>
        /// <param name="radius">吸引范围（世界单位）</param>
        /// <param name="duration">持续时间（秒）</param>
        /// <param name="pullForce">吸引力度</param>
        public void Trigger(Vector3 position, float radius, float duration, float pullForce)
        {
            GameObject zoneObj = new GameObject("BlackHoleZone");
            zoneObj.transform.position = position;

            BlackHoleZone zone = zoneObj.AddComponent<BlackHoleZone>();
            zone.Initialize(position, radius, duration, pullForce);

            if (blackHoleVisualPrefab != null)
            {
                GameObject visual = Instantiate(blackHoleVisualPrefab, position, Quaternion.identity, zoneObj.transform);
                visual.transform.localScale = Vector3.one * (radius * 2f / defaultVisualRadius);
            }
            else
            {
                CreateDefaultVisual(zoneObj, radius);
            }
        }

        private void CreateDefaultVisual(GameObject parent, float radius)
        {
            GameObject visual = new GameObject("BlackHoleVisual");
            visual.transform.SetParent(parent.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = Vector3.one * radius * 2f;

            SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
            int size = 64;
            Texture2D tex = new Texture2D(size, size);
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float r = size * 0.45f;
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c);
                    float a = d <= r ? (1f - d / r) * 0.6f : 0f;
                    tex.SetPixel(x, y, new Color(defaultVisualColor.r, defaultVisualColor.g, defaultVisualColor.b, a));
                }
            }
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            sr.sortingOrder = 50;
        }
    }
}
