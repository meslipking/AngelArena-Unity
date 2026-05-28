using UnityEngine;
using UnityEngine.UI;
using AngelArena.Graphics;

// ══════════════════════════════════════════════════════════════════
//  ANGEL ARENA  — Premium 2.5D VFX System
//
//  Provides:
//    · DamageNumbers — stylized floating damage pop-ups with glow
//    · SkillVFX      — neon skill burst, expanding rings, hit splashes,
//                      death explosions, portals, shield domes, drain
//    · Animation components: FadeScaleAnim, BurstAnim, FlashAnim,
//                            FollowTarget, PortalAnim, NeonRingAnim,
//                            ShockwaveAnim, SparkShowerAnim
// ══════════════════════════════════════════════════════════════════

namespace AngelArena.Core
{
    // ──────────────────────────────────────────────────────────────
    // VFX SYSTEM WRAPPER
    // ──────────────────────────────────────────────────────────────
    public static class VFXSystem
    {
        public static void SpawnFloatText(Vector3 worldPos, string text, Color color, bool big = false)
            => DamageNumbers.SpawnFloatText(worldPos, text, color, big);
    }

    // ──────────────────────────────────────────────────────────────
    //  DAMAGE NUMBERS  — Floating pop-up with outline glow
    // ──────────────────────────────────────────────────────────────
    public static class DamageNumbers
    {
        private static Canvas _canvas;

        public static void SpawnFloatText(Vector3 worldPos, string text, Color color, bool big = false)
        {
            if (!EnsureCanvas()) return;
            var go  = new GameObject("FloatText");
            go.transform.SetParent(_canvas.transform, false);

            var txt = go.AddComponent<Text>();
            txt.text       = text;
            txt.fontSize   = big ? 28 : 20;
            txt.color      = color;
            txt.font       = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontStyle  = FontStyle.Bold;
            txt.alignment  = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;

            SetWorldPosition(go, worldPos);
            var anim = go.AddComponent<DamageNumberAnim>();
            anim.Init(1.1f, 85f, 0.22f);   // life, rise distance, initial horizontal drift
        }

        public static void Spawn(Vector3 worldPos, int amount, DamageType type = DamageType.Physical)
        {
            if (!EnsureCanvas()) return;
            var go = new GameObject("DmgNum");
            go.transform.SetParent(_canvas.transform, false);

            var txt = go.AddComponent<Text>();
            txt.text       = FormatAmount(amount);
            txt.fontSize   = GetFontSize(amount);
            txt.color      = GetColor(type);
            txt.font       = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontStyle  = FontStyle.Bold;
            txt.alignment  = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;

            SetWorldPosition(go, worldPos);
            var anim = go.AddComponent<DamageNumberAnim>();
            anim.Init(0.85f, 60f, Random.Range(-15f, 15f));
        }

        static string FormatAmount(int n)
        {
            if (n >= 1000) return $"{n/1000f:F1}K";
            return n.ToString();
        }

        static int GetFontSize(int n)
        {
            if (n >= 1000) return 38;
            if (n >= 500)  return 32;
            if (n >= 200)  return 26;
            if (n >= 100)  return 22;
            return 18;
        }

        static Color GetColor(DamageType t) => t switch
        {
            DamageType.Fire      => new Color(1f,  0.55f, 0.1f),
            DamageType.Ice       => new Color(0.4f,0.85f, 1f),
            DamageType.Lightning => new Color(0.9f,0.95f, 0.2f),
            DamageType.Poison    => new Color(0.4f,0.92f, 0.22f),
            DamageType.Holy      => new Color(1f,  1f,   0.65f),
            DamageType.Shadow    => new Color(0.72f,0.30f,1f),
            DamageType.Dark      => new Color(0.55f,0.18f,0.88f),
            DamageType.True      => Color.white,
            _                    => new Color(1f,  0.88f,0.88f),
        };

        static void SetWorldPosition(GameObject go, Vector3 worldPos)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 52);
            var cam = Camera.main;
            if (cam) rt.position = cam.WorldToScreenPoint(worldPos);
        }

        static bool EnsureCanvas()
        {
            if (_canvas != null) return true;
            var go = new GameObject("DamageNumberCanvas");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            go.AddComponent<CanvasScaler>();
            Object.DontDestroyOnLoad(go);
            return true;
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  SKILL VFX  — Premium 2.5D neon effects
    // ──────────────────────────────────────────────────────────────
    public static class SkillVFX
    {
        // ── AoE expanding neon ring ──────────────────────────────
        public static void SpawnAoe(Vector2 center, float radius, Color color, float duration)
        {
            // Inner fill pulse
            var fill = CircleGO("AoeFill", center, radius * 0.15f, Fade(color, 0.22f), 7, duration);
            fill.AddComponent<FadeScaleAnim>().Init(duration, radius * 0.15f, radius * 1.05f, Fade(color, 0.22f));

            // Neon ring expand
            var ring = CircleRingGO("AoeRing", center, radius * 0.2f, color, 8, duration);
            ring.AddComponent<FadeScaleAnim>().Init(duration, radius * 0.2f, radius * 2.1f, color);

            // Secondary thin outer ring
            var ring2 = CircleRingGO("AoeRing2", center, radius * 0.15f, Fade(color, 0.5f), 8, duration);
            ring2.AddComponent<FadeScaleAnim>().Init(duration, radius * 0.15f, radius * 2.4f, Fade(color, 0.5f));

            // Sparks
            SpawnSparks(center, color, Mathf.Min(8, 12), radius * 0.5f, duration * 0.6f);
        }

        // ── Death explosion (premium comic-book blast) ───────────
        public static void SpawnDeathBurst(Vector3 pos, Color color)
        {
            const float life = 0.5f;
            const float maxR = 90f;

            // Hot white flash core
            var core = CircleGO("DeathCore", pos, 10f, Color.white, 10, life * 0.4f);
            core.AddComponent<FadeScaleAnim>().Init(life * 0.4f, 10f, maxR * 0.7f, Color.white);

            // Colored mid burst
            var mid = CircleGO("DeathMid", pos, 5f, color, 9, life);
            mid.AddComponent<BurstAnim>().Init(life, color, maxR);

            // Neon ring shockwave
            var shock = CircleRingGO("DeathShock", pos, 10f, color, 8, life);
            shock.AddComponent<ShockwaveAnim>().Init(life, maxR * 1.5f, color);

            // Spark shower
            SpawnSparks(pos, color, 12, maxR * 0.8f, life * 0.8f);

            // Second delayed ring
            var ring2 = CircleRingGO("DeathRing2", pos, 8f, Fade(Color.white, 0.7f), 9, life * 0.6f);
            ring2.AddComponent<FadeScaleAnim>().Init(life * 0.6f, 8f, maxR * 1.2f, Fade(Color.white, 0.7f));
        }

        // ── Projectile impact (neon splash) ─────────────────────
        public static void SpawnImpact(Vector2 pos, Color color, float duration)
        {
            var go = CircleGO("Impact", pos, 8f, color, 7, duration);
            go.AddComponent<FadeScaleAnim>().Init(duration, 8f, 55f, color);

            var ring = CircleRingGO("ImpactRing", pos, 6f, color, 8, duration);
            ring.AddComponent<FadeScaleAnim>().Init(duration, 6f, 65f, Fade(color, 0.7f));

            SpawnSparks(pos, color, 5, 35f, duration * 0.7f);
        }

        // ── Warning circle (red pulsing danger zone) ────────────
        public static void SpawnWarningCircle(Vector2 pos, float radius, float duration)
        {
            Color warn = new Color(1f, 0.28f, 0.08f, 0.38f);
            var fill   = CircleGO("WarnFill", pos, radius * 1.8f, warn, 5, duration);
            fill.AddComponent<FlashAnim>().Init(duration, warn);

            var ring = CircleRingGO("WarnRing", pos, radius * 1.8f, new Color(1f, 0.38f, 0.08f, 0.85f), 6, duration);
            ring.AddComponent<FlashAnim>().Init(duration, new Color(1f, 0.38f, 0.08f, 0.85f));
        }

        // ── Dash trail ──────────────────────────────────────────
        public static void SpawnDashTrail(Vector2 pos, Vector2 dir, Color color, float duration)
        {
            for (int i = 0; i < 6; i++)
            {
                Vector2 tp    = pos - dir * (i * 14f);
                float   trailR = 22f - i * 2.5f;
                var go = CircleGO($"DashTrail{i}", tp, trailR, Fade(color, 0.7f - i*0.1f), 6, duration);
                go.AddComponent<FadeScaleAnim>().Init(duration, trailR, trailR * 0.2f, Fade(color, 0.7f - i*0.1f));
            }
        }

        // ── Target mark ─────────────────────────────────────────
        public static void SpawnOnTarget(Transform target, string id, Color color, float duration)
        {
            if (target == null) return;
            var go   = CircleRingGO("TargetMark", target.position, 38f, color, 10, duration);
            go.AddComponent<FollowTarget>().Init(target, duration);
            go.AddComponent<FlashAnim>().Init(duration, color);
        }

        // ── Summon burst ─────────────────────────────────────────
        public static void SpawnSummonEffect(Vector2 pos, Color color)
        {
            var go = CircleGO("SummonVFX", pos, 10f, color, 8, 0.65f);
            go.AddComponent<BurstAnim>().Init(0.65f, color, 65f);
            SpawnSparks(pos, color, 8, 55f, 0.5f);
        }

        // ── Shield dome ──────────────────────────────────────────
        public static void SpawnShieldEffect(Transform target, float duration)
        {
            if (target == null) return;
            Color shieldCol = new Color(0.95f, 0.92f, 0.35f, 0.28f);
            var go = CircleGO("ShieldVFX", target.position, 55f, shieldCol, 9, duration);
            go.AddComponent<FollowTarget>().Init(target, duration);
            go.AddComponent<FlashAnim>().Init(duration, shieldCol);

            var ring = CircleRingGO("ShieldRing", target.position, 55f, new Color(1f,1f,0.5f,0.75f), 10, duration);
            ring.AddComponent<FollowTarget>().Init(target, duration);
        }

        public static void SpawnShieldAbsorb(Transform target, float shieldAmt, float duration)
            => SpawnShieldEffect(target, duration);

        // ── Drain / life siphon ─────────────────────────────────
        public static void SpawnDrainEffect(Vector2 center, float range, Color color, float duration)
        {
            Color drainCol = new Color(0.3f, 0f, 0.55f, 0.38f);
            var go = CircleGO("DrainVFX", center, range, drainCol, 7, duration);
            go.AddComponent<FadeScaleAnim>().Init(duration, range, range * 0.45f, drainCol);

            var ring = CircleRingGO("DrainRing", center, range * 0.9f, Fade(color, 0.8f), 8, duration);
            ring.AddComponent<FadeScaleAnim>().Init(duration, range * 0.9f, range * 0.3f, Fade(color, 0.8f));
        }

        // ── Heal burst ──────────────────────────────────────────
        public static void SpawnHealEffect(Transform target, float duration)
        {
            if (target == null) return;
            Color healCol = new Color(0.18f, 0.95f, 0.38f, 0.32f);
            var go = CircleGO("HealVFX", target.position, 28f, healCol, 8, duration);
            go.AddComponent<FollowTarget>().Init(target, duration);
            go.AddComponent<FlashAnim>().Init(duration, healCol);

            SpawnSparks(target.position, new Color(0.3f, 1f, 0.5f), 5, 30f, duration * 0.8f);
        }

        // ── Hit flash ───────────────────────────────────────────
        public static void SpawnHitFlash(Vector3 pos, Color color, float size = 22f)
        {
            var go = CircleGO("HitFlash", pos, size, color, 9, 0.14f);
            go.AddComponent<FadeScaleAnim>().Init(0.14f, size, size * 1.6f, color);
        }

        // ── Speed ghost trail ───────────────────────────────────
        public static void SpawnSpeedTrail(Vector2 pos, Vector2 dir, Color color, float duration)
        {
            var go = CircleGO("SpeedTrail", pos, 20f, Fade(color, 0.55f), 4, duration);
            go.AddComponent<FadeScaleAnim>().Init(duration, 20f, 2f, Fade(color, 0.55f));
        }

        // ── BOOM Explosion ───────────────────────────────────────
        public static void SpawnBoomExplosion(Vector2 pos, float radius, float duration)
        {
            // White flash core
            var core = CircleGO("BoomCore", pos, radius * 0.35f, Color.white, 11, duration * 0.3f);
            core.AddComponent<FadeScaleAnim>().Init(duration * 0.3f, radius * 0.35f, radius * 1.1f, Color.white);

            // Fire ball
            var fire = CircleGO("BoomFire", pos, radius * 0.25f, new Color(1f, 0.45f, 0.05f, 0.85f), 10, duration);
            fire.AddComponent<BurstAnim>().Init(duration, new Color(1f, 0.45f, 0.05f, 0.85f), radius * 1.2f);

            // Shockwave ring
            var shock = CircleRingGO("BoomShock", pos, radius * 0.3f, new Color(1f, 0.7f, 0.2f, 0.9f), 9, duration);
            shock.AddComponent<ShockwaveAnim>().Init(duration, radius * 2.2f, new Color(1f, 0.7f, 0.2f));

            // Smoke puffs
            for (int i = 0; i < 7; i++)
            {
                float angle  = (360f / 7f) * i * Mathf.Deg2Rad;
                Vector2 off  = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (radius * 0.55f);
                var puff     = CircleGO($"BoomSmoke{i}", (Vector3)(pos+off), radius * 0.5f,
                                        new Color(0.18f, 0.15f, 0.22f, 0.65f), 9, duration * 1.15f);
                puff.AddComponent<FadeScaleAnim>().Init(duration * 1.15f, radius * 0.25f, radius * 0.82f,
                                                        new Color(0.15f, 0.12f, 0.20f, 0.65f));
            }

            // Spark shower
            SpawnSparks(pos, new Color(1f, 0.72f, 0.2f), 14, radius * 0.9f, duration * 0.75f);
            DamageNumbers.SpawnFloatText((Vector3)pos + Vector3.up * 35f, "💥 BOOM!", new Color(1f,0.8f,0.1f), big:true);
        }

        // ── Portal ───────────────────────────────────────────────
        public static void SpawnPortal(Vector2 pos, float duration)
        {
            var go  = new GameObject("PortalVFX");
            go.transform.position   = pos;
            go.transform.localScale = new Vector3(85f, 42f, 1f);

            var sr  = go.AddComponent<SpriteRenderer>();
            sr.sprite       = MakeGlowCircle(new Color(0.52f, 0.05f, 0.88f, 0.65f));
            sr.sortingOrder = 3;

            var ring = new GameObject("PortalRing");
            ring.transform.SetParent(go.transform, false);
            var ringSR = ring.AddComponent<SpriteRenderer>();
            ringSR.sprite       = MakeRingSprite(new Color(0.82f, 0.28f, 1f, 0.95f));
            ringSR.sortingOrder = 4;
            ring.transform.localScale = Vector3.one;

            go.AddComponent<PortalAnim>().Init(duration);
        }

        // ── Lightning strike ─────────────────────────────────────
        public static void SpawnLightning(Vector2 from, Vector2 to, Color color, float duration)
        {
            // Chain of overlapping thin streaks
            int segments = 6;
            Vector2 prev = from;
            for (int i = 1; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector2 mid = Vector2.Lerp(from, to, t)
                            + new Vector2(Random.Range(-18f, 18f), Random.Range(-18f, 18f));

                float len  = Vector2.Distance(prev, mid);
                Vector2 ctr= (prev + mid) * 0.5f;
                float angle= Mathf.Atan2(mid.y - prev.y, mid.x - prev.x) * Mathf.Rad2Deg;

                var seg = new GameObject($"LightSeg{i}");
                seg.transform.position   = ctr;
                seg.transform.rotation   = Quaternion.Euler(0,0,angle);
                seg.transform.localScale = new Vector3(len, 4f, 1f);
                var sr = seg.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteFactory.SolidRect(color, 1, 1);
                sr.sortingOrder = 8;
                Object.Destroy(seg, duration);
                seg.AddComponent<FadeScaleAnim>().Init(duration, len, len, color);

                prev = mid;
            }
        }

        // ── Neon ring pulse (for skills with ring zone) ──────────
        public static void SpawnNeonRing(Vector2 center, float radius, Color color, float duration)
        {
            var ring = CircleRingGO("NeonRing", center, radius, color, 8, duration);
            ring.AddComponent<NeonRingAnim>().Init(duration, color);
        }

        // ── Spark shower helper ──────────────────────────────────
        static void SpawnSparks(Vector3 pos, Color color, int count, float spread, float life)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float dist  = Random.Range(spread * 0.2f, spread);
                Vector3 tp  = pos + new Vector3(Mathf.Cos(angle)*dist, Mathf.Sin(angle)*dist, 0);

                float sz = Random.Range(3f, 9f);
                var go   = CircleGO($"Spark{i}", tp, sz, color, 9, life);
                go.AddComponent<FadeScaleAnim>().Init(life, sz, 0.5f, color);
            }
        }

        // ── Internal circle factory ───────────────────────────────
        static GameObject CircleGO(string name, Vector3 pos, float size, Color color, int order, float life)
        {
            var go = new GameObject(name);
            go.transform.position   = pos;
            go.transform.localScale = Vector3.one * size;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = MakeGlowCircle(color);
            sr.sortingOrder = order;
            Object.Destroy(go, life + 0.05f);
            return go;
        }

        static GameObject CircleRingGO(string name, Vector3 pos, float size, Color color, int order, float life)
        {
            var go = new GameObject(name);
            go.transform.position   = pos;
            go.transform.localScale = Vector3.one * size;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = MakeRingSprite(color);
            sr.sortingOrder = order;
            Object.Destroy(go, life + 0.05f);
            return go;
        }

        // ── Sprite generators ─────────────────────────────────────
        static Sprite MakeGlowCircle(Color color)
        {
            return SpriteFactory.GetOrCreate($"glow_{color.r:F2}_{color.g:F2}_{color.b:F2}_{color.a:F2}", () => {
                int sz = 64;
                var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
                float c = sz / 2f;
                for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    float d = Mathf.Sqrt((x-c)*(x-c) + (y-c)*(y-c)) / c;
                    if (d >= 1f) { tex.SetPixel(x, y, Color.clear); continue; }
                    // Soft radial glow: bright center, feathered edges
                    float alpha = Mathf.Pow(1f - d, 1.5f) * color.a;
                    tex.SetPixel(x, y, new Color(color.r, color.g, color.b, alpha));
                }
                tex.Apply();
                return Sprite.Create(tex, new Rect(0,0,sz,sz), Vector2.one*0.5f, (float)sz);
            });
        }

        static Sprite MakeRingSprite(Color color)
        {
            return SpriteFactory.GetOrCreate($"ring_{color.r:F2}_{color.g:F2}_{color.b:F2}_{color.a:F2}", () => {
                int sz = 64;
                var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
                float c = sz / 2f;
                float outerR = c - 1f;
                float innerR = c - 6f;
                for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    float d = Mathf.Sqrt((x-c)*(x-c) + (y-c)*(y-c));
                    if (d > outerR || d < innerR) { tex.SetPixel(x, y, Color.clear); continue; }
                    float t = 1f - Mathf.Abs(d - (outerR+innerR)*0.5f) / ((outerR-innerR)*0.5f);
                    tex.SetPixel(x, y, new Color(color.r, color.g, color.b, t * t * color.a));
                }
                tex.Apply();
                return Sprite.Create(tex, new Rect(0,0,sz,sz), Vector2.one*0.5f, (float)sz);
            });
        }

        static Color Fade(Color c, float alpha) => new Color(c.r, c.g, c.b, alpha);
    }

    // ──────────────────────────────────────────────────────────────
    //  ANIMATION COMPONENTS
    // ──────────────────────────────────────────────────────────────
    public class DamageNumberAnim : MonoBehaviour
    {
        private float _life, _elapsed, _rise, _drift;
        private Text _txt;
        private RectTransform _rt;
        private float _startY;

        public void Init(float life, float rise, float drift)
        { _life = life; _rise = rise; _drift = drift; }

        private void Awake()
        {
            _txt = GetComponent<Text>();
            _rt  = GetComponent<RectTransform>();
            _startY = _rt.anchoredPosition.y;
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            float t = _elapsed / _life;
            // Ease-out rise
            float ease = 1f - (1f - t) * (1f - t);
            _rt.anchoredPosition = new Vector2(
                _rt.anchoredPosition.x + _drift * Time.unscaledDeltaTime * 0.5f,
                _startY + _rise * ease);

            // Scale punch at start then normalize
            float scaleFactor = t < 0.1f ? Mathf.Lerp(1.5f, 1f, t / 0.1f) : 1f;
            _rt.localScale = Vector3.one * scaleFactor;

            // Fade out in last 30%
            if (_txt) _txt.color = new Color(_txt.color.r, _txt.color.g, _txt.color.b,
                t < 0.7f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.7f) / 0.3f));

            if (_elapsed >= _life) Destroy(gameObject);
        }
    }

    public class BurstAnim : MonoBehaviour
    {
        private float _life, _elapsed, _maxSize;
        private Color _color;
        private SpriteRenderer _sr;
        private void Awake() { _sr = GetComponent<SpriteRenderer>(); }
        public void Init(float life, Color col, float maxSize) { _life = life; _color = col; _maxSize = maxSize; }
        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = _elapsed / _life;
            // Ease-out expansion
            float ease = 1f - Mathf.Pow(1f - t, 2.5f);
            transform.localScale = Vector3.one * Mathf.Lerp(0f, _maxSize, ease);
            if (_sr) _sr.color   = new Color(_color.r, _color.g, _color.b, Mathf.Lerp(_color.a, 0f, t));
            if (_elapsed >= _life) Destroy(gameObject);
        }
    }

    public class FadeScaleAnim : MonoBehaviour
    {
        private float _life, _elapsed, _startS, _endS;
        private Color _color;
        private SpriteRenderer _sr;
        private void Awake() { _sr = GetComponent<SpriteRenderer>(); }
        public void Init(float life, float start, float end, Color col)
        { _life = life; _startS = start; _endS = end; _color = col; }
        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t  = _elapsed / _life;
            float ease = Mathf.SmoothStep(0f, 1f, t);
            transform.localScale = Vector3.one * Mathf.Lerp(_startS, _endS, ease);
            if (_sr) _sr.color   = new Color(_color.r, _color.g, _color.b, Mathf.Lerp(_color.a, 0f, t));
            if (_elapsed >= _life) Destroy(gameObject);
        }
    }

    public class FlashAnim : MonoBehaviour
    {
        private float _life, _elapsed;
        private Color _color;
        private SpriteRenderer _sr;
        private void Awake() { _sr = GetComponent<SpriteRenderer>(); }
        public void Init(float life, Color col) { _life = life; _color = col; }
        private void Update()
        {
            _elapsed += Time.deltaTime;
            float alpha = Mathf.PingPong(_elapsed * 4.5f, 1f) * _color.a;
            if (_sr) _sr.color = new Color(_color.r, _color.g, _color.b, alpha);
            if (_elapsed >= _life) Destroy(gameObject);
        }
    }

    public class ShockwaveAnim : MonoBehaviour
    {
        private float _life, _elapsed, _targetSize;
        private Color _color;
        private SpriteRenderer _sr;
        private void Awake() { _sr = GetComponent<SpriteRenderer>(); }
        public void Init(float life, float targetSize, Color col)
        { _life = life; _targetSize = targetSize; _color = col; }
        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t    = _elapsed / _life;
            // Shockwave: fast initial expand, then slow down and fade
            float ease = Mathf.Pow(t, 0.4f);
            transform.localScale = Vector3.one * Mathf.Lerp(0f, _targetSize, ease);
            if (_sr)
            {
                float alpha = Mathf.Lerp(_color.a, 0f, t * t);
                _sr.color   = new Color(_color.r, _color.g, _color.b, alpha);
            }
            if (_elapsed >= _life) Destroy(gameObject);
        }
    }

    public class NeonRingAnim : MonoBehaviour
    {
        private float _life, _elapsed;
        private Color _color;
        private SpriteRenderer _sr;
        private void Awake() { _sr = GetComponent<SpriteRenderer>(); }
        public void Init(float life, Color col) { _life = life; _color = col; }
        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t  = _elapsed / _life;
            // Pulsing neon glow
            float pulse = 0.7f + 0.3f * Mathf.Sin(t * Mathf.PI * 8f);
            if (_sr) _sr.color = new Color(_color.r, _color.g, _color.b, pulse * _color.a * (1f - t));
            if (_elapsed >= _life) Destroy(gameObject);
        }
    }

    public class FollowTarget : MonoBehaviour
    {
        private Transform _target;
        private float _life, _elapsed;
        public void Init(Transform t, float life) { _target = t; _life = life; }
        private void Update()
        {
            if (_target) transform.position = _target.position;
            _elapsed += Time.deltaTime;
            if (_elapsed >= _life) Destroy(gameObject);
        }
    }

    public class PortalAnim : MonoBehaviour
    {
        private float _life, _elapsed;
        private Vector3 _baseScale;
        private SpriteRenderer[] _srs;

        private void Awake()
        {
            _baseScale = transform.localScale;
            _srs = GetComponentsInChildren<SpriteRenderer>();
        }

        public void Init(float life) { _life = life; }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t     = _elapsed / _life;
            float pulse = 1f + 0.09f * Mathf.Sin(Time.time * 7f);

            // Spin + scale collapse at end
            transform.Rotate(0f, 0f, 95f * Time.deltaTime);
            transform.localScale = _baseScale * (pulse * Mathf.Lerp(1f, 0f, t * t));

            foreach (var sr in _srs)
            {
                if (sr == null) continue;
                Color c = sr.color;
                sr.color = new Color(c.r, c.g, c.b, Mathf.Lerp(c.a, 0f, t));
            }

            if (_elapsed >= _life) Destroy(gameObject);
        }
    }
}
