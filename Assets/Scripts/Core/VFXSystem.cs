using UnityEngine;
using UnityEngine.UI;

// VFXSystem.cs — floating damage numbers + all SkillVFX effects
// DamageType and SkillVFX class defined in SkillHandlers.cs (same namespace)
// This file only adds:
//   - DamageNumbers (floating number UI)
//   - DamageNumberAnim
//   - All SkillVFX.Spawn* methods (partial class extension via static helpers)
//   - BurstAnim, HitFlashAnim, WarningCircleAnim, DrainAnim, ShieldAnim

namespace AngelArena.Core
{
    // ══════════════════════════════════════════════════════════════
    // DAMAGE NUMBERS
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Spawns floating damage number UI above enemies.
    /// </summary>
    public static class DamageNumbers
    {
        private static Canvas _canvas;

        public static void Spawn(Vector3 worldPos, int amount, DamageType type = DamageType.Physical)
        {
            if (!EnsureCanvas()) return;

            var go  = new GameObject("DmgNum");
            go.transform.SetParent(_canvas.transform, false);

            var txt = go.AddComponent<Text>();
            txt.text      = amount.ToString();
            txt.fontSize  = GetFontSize(amount);
            txt.color     = GetColor(type);
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(120, 45);
            var cam = Camera.main;
            if (cam) rt.position = cam.WorldToScreenPoint(worldPos);

            go.AddComponent<DamageNumberAnim>();
        }

        private static int GetFontSize(int n)
        {
            if (n >= 500) return 34;
            if (n >= 200) return 28;
            if (n >= 100) return 22;
            return 18;
        }

        private static Color GetColor(DamageType t) => t switch
        {
            DamageType.Fire      => new Color(1f, 0.5f, 0.1f),
            DamageType.Ice       => new Color(0.4f, 0.8f, 1f),
            DamageType.Lightning => new Color(0.9f, 0.9f, 0.2f),
            DamageType.Poison    => new Color(0.4f, 0.9f, 0.2f),
            DamageType.Holy      => new Color(1f, 1f, 0.7f),
            DamageType.Shadow    => new Color(0.7f, 0.3f, 1f),
            DamageType.Dark      => new Color(0.5f, 0.2f, 0.8f),
            DamageType.True      => Color.white,
            _                    => new Color(1f, 0.9f, 0.9f),
        };

        private static bool EnsureCanvas()
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

    // ══════════════════════════════════════════════════════════════
    // SKILL VFX  (all Spawn* methods called from SkillHandlers)
    // ══════════════════════════════════════════════════════════════

    public static class SkillVFX
    {
        // ── AoE expanding ring ─────────────────────────────────────
        public static void SpawnAoe(Vector2 center, float radius, Color color, float duration)
        {
            var go = CreateCircleGO("AoeVFX", center, radius * 2f, color, 8, duration);
            go.AddComponent<FadeScaleAnim>().Init(duration, radius * 2f, radius * 2.5f, color);
        }

        // ── Death burst ───────────────────────────────────────────
        public static void SpawnDeathBurst(Vector3 pos, Color color)
        {
            var go = CreateCircleGO("DeathBurst", pos, 5f, color, 9, 0.4f);
            go.AddComponent<BurstAnim>().Init(0.4f, color, 80f);
        }

        // ── Projectile impact ─────────────────────────────────────
        public static void SpawnImpact(Vector2 pos, Color color, float duration)
        {
            var go = CreateCircleGO("ImpactVFX", pos, 20f, color, 7, duration);
            go.AddComponent<FadeScaleAnim>().Init(duration, 20f, 45f, color);
        }

        // ── Warning circle (meteor, arrow rain) ───────────────────
        public static void SpawnWarningCircle(Vector2 pos, float radius, float duration)
        {
            var go = CreateCircleGO("Warning", pos, radius * 2f, new Color(1f, 0.3f, 0.1f, 0.4f), 5, duration);
            go.AddComponent<FlashAnim>().Init(duration, new Color(1f, 0.3f, 0.1f, 0.4f));
        }

        // ── Dash trail ────────────────────────────────────────────
        public static void SpawnDashTrail(Vector2 pos, Vector2 dir, Color color, float duration)
        {
            for (int i = 0; i < 5; i++)
            {
                Vector2 tp = pos - dir * (i * 15f);
                var go = CreateCircleGO($"DashTrail{i}", tp, 20f - i * 3f, color, 6, duration);
                go.AddComponent<FadeScaleAnim>().Init(duration, 20f, 5f, color);
            }
        }

        // ── On-target marker (Dark Mark) ─────────────────────────
        public static void SpawnOnTarget(Transform target, string id, Color color, float duration)
        {
            if (target == null) return;
            var go = CreateCircleGO("TargetMark", target.position, 35f, color, 10, duration);
            go.AddComponent<FollowTarget>().Init(target, duration);
            go.AddComponent<FadeScaleAnim>().Init(duration, 35f, 35f, color);
        }

        // ── Summon effect ─────────────────────────────────────────
        public static void SpawnSummonEffect(Vector2 pos, Color color)
        {
            var go = CreateCircleGO("SummonVFX", pos, 10f, color, 8, 0.6f);
            go.AddComponent<BurstAnim>().Init(0.6f, color, 60f);
        }

        // ── Shield effect ─────────────────────────────────────────
        public static void SpawnShieldEffect(Transform target, float duration)
        {
            if (target == null) return;
            var go = CreateCircleGO("ShieldVFX", target.position, 50f, new Color(1f, 1f, 0.5f, 0.3f), 9, duration);
            go.AddComponent<FollowTarget>().Init(target, duration);
            go.AddComponent<FlashAnim>().Init(duration, new Color(1f, 1f, 0.5f, 0.4f));
        }

        // ── Shield absorb ─────────────────────────────────────────
        public static void SpawnShieldAbsorb(Transform target, float shieldAmt, float duration)
            => SpawnShieldEffect(target, duration);

        // ── Drain effect ──────────────────────────────────────────
        public static void SpawnDrainEffect(Vector2 center, float range, Color color, float duration)
        {
            var go = CreateCircleGO("DrainVFX", center, range, new Color(0.3f, 0f, 0.5f, 0.4f), 7, duration);
            go.AddComponent<FadeScaleAnim>().Init(duration, range, range * 0.5f, new Color(0.3f, 0f, 0.5f, 0.4f));
        }

        // ── Heal effect ───────────────────────────────────────────
        public static void SpawnHealEffect(Transform target, float duration)
        {
            if (target == null) return;
            var go = CreateCircleGO("HealVFX", target.position, 30f, new Color(0.2f, 1f, 0.3f, 0.35f), 8, duration);
            go.AddComponent<FollowTarget>().Init(target, duration);
            go.AddComponent<FlashAnim>().Init(duration, new Color(0.2f, 1f, 0.3f, 0.35f));
        }

        // ── Hit flash ────────────────────────────────────────────
        public static void SpawnHitFlash(Vector3 pos, Color color, float size = 20f)
        {
            var go = CreateCircleGO("HitFlash", pos, size, color, 9, 0.15f);
            go.AddComponent<FadeScaleAnim>().Init(0.15f, size, size * 1.5f, color);
        }

        // ── Internal circle factory ───────────────────────────────
        private static GameObject CreateCircleGO(string name, Vector3 pos, float size, Color color, int order, float life)
        {
            var go  = new GameObject(name);
            go.transform.position   = pos;
            go.transform.localScale = Vector3.one * size;
            var sr  = go.AddComponent<SpriteRenderer>();
            sr.sprite       = MakeCircleSprite(color);
            sr.sortingOrder = order;
            Object.Destroy(go, life + 0.1f);
            return go;
        }

        private static Sprite MakeCircleSprite(Color color)
        {
            int sz  = 32;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            float c = sz / 2f;
            for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                    tex.SetPixel(x, y, Vector2.Distance(new Vector2(x,y), Vector2.one*c) < c-1 ? color : Color.clear);
            tex.Apply();
            // PPU = 32 so sprite = 1 unit at scale=1; localScale = size gives correct world-unit radius
            return Sprite.Create(tex, new Rect(0,0,sz,sz), Vector2.one*0.5f, (float)sz);
        }
    }

    // ══════════════════════════════════════════════════════════════
    // ANIMATION HELPERS
    // ══════════════════════════════════════════════════════════════

    public class DamageNumberAnim : MonoBehaviour
    {
        private float _life = 0.9f, _elapsed;
        private Text  _txt;
        private RectTransform _rt;
        private float _startY;
        private void Awake() { _txt = GetComponent<Text>(); _rt = GetComponent<RectTransform>(); _startY = _rt.anchoredPosition.y; }
        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            float t = _elapsed / _life;
            _rt.anchoredPosition = new Vector2(_rt.anchoredPosition.x, _startY + Mathf.Lerp(0, 70f, t));
            if (_txt) _txt.color = new Color(_txt.color.r, _txt.color.g, _txt.color.b, Mathf.Lerp(1f, 0f, t));
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
            float s = Mathf.Lerp(transform.localScale.x * 0.5f, _maxSize, t);
            transform.localScale = Vector3.one * s;
            if (_sr) _sr.color   = new Color(_color.r, _color.g, _color.b, Mathf.Lerp(0.8f, 0f, t));
            if (_elapsed >= _life) Destroy(gameObject);
        }
    }

    public class FadeScaleAnim : MonoBehaviour
    {
        private float _life, _elapsed, _startScale, _endScale;
        private Color _color;
        private SpriteRenderer _sr;
        private void Awake() { _sr = GetComponent<SpriteRenderer>(); }
        public void Init(float life, float start, float end, Color col)
        { _life = life; _startScale = start; _endScale = end; _color = col; }
        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = _elapsed / _life;
            transform.localScale = Vector3.one * Mathf.Lerp(_startScale, _endScale, t);
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
            float alpha = Mathf.PingPong(_elapsed * 4f, 1f) * _color.a;
            if (_sr) _sr.color = new Color(_color.r, _color.g, _color.b, alpha);
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
}
