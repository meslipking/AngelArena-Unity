/// <summary>
/// Placeholder registry for skill prefabs.
/// Assign all prefabs via Inspector on a GameObject with this component.
/// SkillVFX, DamageNumbers — defined in VFXSystem.cs
/// </summary>

using UnityEngine;

namespace AngelArena.Core
{
    /// <summary>
    /// Registry for skill prefab lookup by skillId.
    /// Attach to a singleton GameObject in the scene and assign prefabs in Inspector.
    /// </summary>
    public class SkillPrefabs : MonoBehaviour
    {
        public static SkillPrefabs Instance { get; private set; }

        [System.Serializable]
        public struct PrefabEntry { public string id; public GameObject prefab; }

        [Header("Skill Prefabs — assign all in Inspector")]
        public PrefabEntry[] entries;

        private System.Collections.Generic.Dictionary<string, GameObject> _map;

        private void Awake()
        {
            Instance = this;
            _map = new();
            foreach (var e in entries) if (e.prefab != null) _map[e.id] = e.prefab;
        }

        /// <summary>Get prefab by ID. Returns a circle placeholder if not found.</summary>
        public static GameObject Get(string id)
        {
            if (Instance != null && Instance._map.TryGetValue(id, out var go)) return go;
            Debug.LogWarning($"[SkillPrefabs] Prefab not found: '{id}' — using placeholder");
            return CreatePlaceholder(id);
        }

        private static GameObject CreatePlaceholder(string id)
        {
            var go  = new GameObject($"Proj_{id}");
            var sr  = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCustomProjectileSprite(id);
            sr.color  = Color.white;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius    = GetProjectileRadius(id);
            var rb  = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            go.AddComponent<Projectile>();

            if (id.Contains("blade") || id.Contains("reaver") || id.Contains("shuriken") || id.Contains("reaper"))
            {
                go.AddComponent<ProjectileRotator>().speed = 720f;
            }
            return go;
        }

        private static Sprite CreateCustomProjectileSprite(string id)
        {
            int sz = 32;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            float c = sz / 2f;
            
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++) tex.SetPixel(x, y, Color.clear);

            if (id.Contains("arrow"))
            {
                for (int x = 4; x < sz - 4; x++) tex.SetPixel(x, (int)c, new Color(0.47f, 0.25f, 0.15f));
                for (int y = (int)c - 3; y <= (int)c + 3; y++)
                {
                    if (y != (int)c)
                    {
                        tex.SetPixel(4, y, Color.white);
                        tex.SetPixel(5, y, Color.white);
                    }
                }
                tex.SetPixel(sz - 5, (int)c - 1, Color.white);
                tex.SetPixel(sz - 5, (int)c + 1, Color.white);
                tex.SetPixel(sz - 4, (int)c, Color.white);
            }
            else if (id.Contains("blade") || id.Contains("dagger"))
            {
                for (int y = 4; y < sz - 4; y++)
                {
                    int width = 3 - (int)(Mathf.Abs(y - c) * 0.7f);
                    if (width < 0) continue;
                    for (int x = (int)c - width; x <= (int)c + width; x++)
                    {
                        if (y < c - 4)
                            tex.SetPixel(x, y, new Color(0.12f, 0.08f, 0.22f));
                        else
                            tex.SetPixel(x, y, new Color(0.7f, 0.3f, 1f));
                    }
                }
            }
            else if (id.Contains("lightning") || id.Contains("thunder"))
            {
                DrawLine(tex, c - 8, c - 8, c, c + 2, Color.yellow);
                DrawLine(tex, c, c + 2, c - 2, c - 2, Color.yellow);
                DrawLine(tex, c - 2, c - 2, c + 8, c + 8, Color.yellow);
            }
            else if (id.Contains("fireball") || id.Contains("meteor"))
            {
                for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                    if (d < c * 0.8f)
                    {
                        float f = d / (c * 0.8f);
                        Color col = Color.Lerp(Color.white, new Color(1f, 0.35f, 0.1f), f);
                        col.a = 1f - f * 0.3f;
                        tex.SetPixel(x, y, col);
                    }
                }
            }
            else if (id.Contains("reaver") || id.Contains("scythe") || id.Contains("reaper"))
            {
                for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    float dx = x - c, dy = y - c;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);
                    if (dist > c * 0.5f && dist < c * 0.9f && angle > -1f && angle < 1.5f)
                    {
                        tex.SetPixel(x, y, Color.Lerp(Color.white, new Color(0.6f, 0.1f, 0.8f), (dist - c * 0.5f) / (c * 0.4f)));
                    }
                }
            }
            else if (id.Contains("poison") || id.Contains("dart"))
            {
                for (int x = 6; x < sz - 6; x++)
                {
                    int w = 2 - (int)(Mathf.Abs(x - c) * 0.3f);
                    for (int y = (int)c - w; y <= (int)c + w; y++)
                    {
                        tex.SetPixel(x, y, new Color(0.12f, 0.8f, 0.3f));
                    }
                }
            }
            else if (id.Contains("wolf"))
            {
                for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                    if (d < c * 0.75f)
                    {
                        tex.SetPixel(x, y, new Color(0.22f, 0.56f, 0.97f, 0.85f));
                    }
                }
                DrawTriangle(tex, new Vector2(c - 6, c + 3), new Vector2(c - 8, c + 10), new Vector2(c - 2, c + 5), Color.cyan);
                DrawTriangle(tex, new Vector2(c + 2, c + 5), new Vector2(c + 8, c + 10), new Vector2(c + 6, c + 3), Color.cyan);
            }
            else
            {
                for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                    if (d < c * 0.75f)
                    {
                        float f = d / (c * 0.75f);
                        Color col = Color.Lerp(Color.white, new Color(0.22f, 0.56f, 0.97f), f);
                        col.a = 1f - f * 0.25f;
                        tex.SetPixel(x, y, col);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, sz);
        }

        private static float GetProjectileRadius(string id)
        {
            if (id.Contains("fireball") || id.Contains("meteor")) return 14f;
            if (id.Contains("reaver") || id.Contains("reaper")) return 16f;
            if (id.Contains("wolf")) return 12f;
            return 8f;
        }

        private static void DrawLine(Texture2D tex, float x0, float y0, float x1, float y1, Color col)
        {
            int w = tex.width; int h = tex.height;
            int x = (int)x0; int y = (int)y0;
            int dx = (int)Mathf.Abs(x1 - x0); int dy = (int)Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1; int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            while (true)
            {
                if (x >= 0 && x < w && y >= 0 && y < h) tex.SetPixel(x, y, col);
                if (x == (int)x1 && y == (int)y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x += sx; }
                if (e2 < dx) { err += dx; y += sy; }
            }
        }

        private static void DrawTriangle(Texture2D tex, Vector2 p0, Vector2 p1, Vector2 p2, Color col)
        {
            int minX = (int)Mathf.Min(p0.x, Mathf.Min(p1.x, p2.x));
            int maxX = (int)Mathf.Max(p0.x, Mathf.Max(p1.x, p2.x));
            int minY = (int)Mathf.Min(p0.y, Mathf.Min(p1.y, p2.y));
            int maxY = (int)Mathf.Max(p0.y, Mathf.Max(p1.y, p2.y));

            minX = Mathf.Clamp(minX, 0, tex.width - 1);
            maxX = Mathf.Clamp(maxX, 0, tex.width - 1);
            minY = Mathf.Clamp(minY, 0, tex.height - 1);
            maxY = Mathf.Clamp(maxY, 0, tex.height - 1);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (PointInTriangle(new Vector2(x, y), p0, p1, p2))
                    {
                        tex.SetPixel(x, y, col);
                    }
                }
            }
        }

        private static bool PointInTriangle(Vector2 p, Vector2 p0, Vector2 p1, Vector2 p2)
        {
            float s = p0.y * p2.x - p0.x * p2.y + (p2.y - p0.y) * p.x + (p0.x - p2.x) * p.y;
            float t = p0.x * p1.y - p0.y * p1.x + (p0.y - p1.y) * p.x + (p1.x - p0.x) * p.y;

            if ((s < 0) != (t < 0))
                return false;

            float a = -p1.y * p2.x + p0.y * (p2.x - p1.x) + p0.x * (p1.y - p2.y) + p1.x * p2.y;
            return a < 0 ?
                    (s <= 0 && s + t >= a) :
                    (s >= 0 && s + t <= a);
        }
    }

    public class ProjectileRotator : MonoBehaviour
    {
        public float speed = 360f;
        private void Update()
        {
            transform.Rotate(0f, 0f, speed * Time.deltaTime);
        }
    }
    }

    // NOTE: SkillVFX, DamageNumbers, BurstAnim, etc. → defined in VFXSystem.cs
    // NOTE: DamageType enum → defined in SkillHandlers.cs
}
