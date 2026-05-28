using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AngelArena.Core;
using AngelArena.Data;

/// <summary>
/// Premium 2.5D Arena Visual System — Angel Arena PVE.
/// Handles: dark fantasy background (3-layer parallax), character scaling,
/// neon arena borders, ambient particle effects, enemy HP bars, HUD setup.
/// 
/// Background: deep-space dark purple/navy, floating magic debris, 
///             glowing rune floor, soft vignette — never competes with characters.
/// </summary>
namespace AngelArena.Graphics
{
    // ══════════════════════════════════════════════════════════════════
    //  CHARACTER DEFINITIONS  (synced from HTML PVE design)
    // ══════════════════════════════════════════════════════════════════
    public static class CharacterDefs
    {
        public struct CharDef
        {
            public string id;
            public Color  bodyColor;
            public Color  glowColor;
            public Color  accentColor;
            public string icon;
            public float  drawRadius;
        }

        public static readonly CharDef[] All = {
            new CharDef { id="fighter",     bodyColor=new Color(0.96f,0.38f,0.22f), glowColor=new Color(1f,0.55f,0.15f),   accentColor=new Color(0.8f,0.2f,0.05f), icon="⚔",  drawRadius=30 },
            new CharDef { id="mage",        bodyColor=new Color(0.48f,0.28f,0.90f), glowColor=new Color(0.70f,0.45f,1.0f), accentColor=new Color(0.2f,0.1f,0.7f),  icon="🔮", drawRadius=28 },
            new CharDef { id="assassin",    bodyColor=new Color(0.12f,0.62f,0.45f), glowColor=new Color(0.20f,0.90f,0.65f),accentColor=new Color(0.05f,0.4f,0.3f), icon="🗡",  drawRadius=26 },
            new CharDef { id="ranger",      bodyColor=new Color(0.22f,0.78f,0.32f), glowColor=new Color(0.35f,1.00f,0.45f),accentColor=new Color(0.1f,0.55f,0.2f), icon="🏹", drawRadius=27 },
            new CharDef { id="paladin",     bodyColor=new Color(1.00f,0.82f,0.18f), glowColor=new Color(1.00f,0.96f,0.45f),accentColor=new Color(0.8f,0.6f,0.08f), icon="🛡",  drawRadius=32 },
            new CharDef { id="necromancer", bodyColor=new Color(0.22f,0.18f,0.38f), glowColor=new Color(0.55f,0.20f,0.90f),accentColor=new Color(0.15f,0.08f,0.3f),icon="💀", drawRadius=28 },
            new CharDef { id="druid",       bodyColor=new Color(0.35f,0.68f,0.28f), glowColor=new Color(0.50f,0.90f,0.35f),accentColor=new Color(0.2f,0.5f,0.1f),  icon="🌿", drawRadius=28 },
        };

        public static CharDef Get(string id)
        {
            foreach (var c in All) if (c.id.ToLower() == id?.ToLower()) return c;
            return All[0];
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  ENEMY VISUAL DEFINITIONS
    // ══════════════════════════════════════════════════════════════════
    public static class EnemyDefs
    {
        public struct EnemyDef
        {
            public string id;
            public Color  color;
            public Color  glowColor;
            public float  radius;
            public bool   isBlob;
            public bool   isDiamond;
            public bool   isStar;
            public bool   isGhost;
            public bool   isHex;
        }

        public static readonly EnemyDef[] All = {
            new EnemyDef { id="slime",       color=new Color(0.32f,0.90f,0.38f), glowColor=new Color(0.45f,1f,0.52f),   radius=20, isBlob=true    },
            new EnemyDef { id="goblin",      color=new Color(0.48f,0.72f,0.22f), glowColor=new Color(0.65f,1f,0.35f),   radius=22, isDiamond=true  },
            new EnemyDef { id="skeleton",    color=new Color(0.92f,0.92f,0.86f), glowColor=new Color(1f,1f,0.96f),      radius=22                  },
            new EnemyDef { id="orc",         color=new Color(0.38f,0.58f,0.18f), glowColor=new Color(0.55f,0.85f,0.3f), radius=26                  },
            new EnemyDef { id="demon",       color=new Color(0.88f,0.18f,0.12f), glowColor=new Color(1f,0.35f,0.22f),   radius=28, isStar=true     },
            new EnemyDef { id="wraith",      color=new Color(0.52f,0.32f,0.85f), glowColor=new Color(0.72f,0.52f,1f),   radius=26, isGhost=true    },
            new EnemyDef { id="golem",       color=new Color(0.58f,0.58f,0.68f), glowColor=new Color(0.78f,0.78f,0.92f),radius=32, isHex=true      },
            new EnemyDef { id="vampire",     color=new Color(0.78f,0.10f,0.22f), glowColor=new Color(1f,0.25f,0.38f),   radius=26                  },
            new EnemyDef { id="witch",       color=new Color(0.45f,0.18f,0.78f), glowColor=new Color(0.68f,0.32f,1f),   radius=24, isDiamond=true  },
            new EnemyDef { id="giant",       color=new Color(0.68f,0.48f,0.28f), glowColor=new Color(0.88f,0.68f,0.42f),radius=42                  },
            new EnemyDef { id="elite_orc",   color=new Color(1f,0.80f,0.10f),    glowColor=new Color(1f,1f,0.4f),       radius=30, isStar=true     },
            new EnemyDef { id="elite_demon", color=new Color(1f,0.45f,0.10f),    glowColor=new Color(1f,0.72f,0.3f),    radius=32, isStar=true     },
            new EnemyDef { id="boss_dragon", color=new Color(1.0f,0.42f,0.08f),  glowColor=new Color(1f,0.6f,0.2f),    radius=70                  },
            new EnemyDef { id="boss_lich",   color=new Color(0.52f,0.72f,0.95f), glowColor=new Color(0.6f,0.85f,1f),   radius=65                  },
            new EnemyDef { id="boss_golem",  color=new Color(0.72f,0.72f,0.45f), glowColor=new Color(0.92f,0.92f,0.62f),radius=75, isHex=true     },
            new EnemyDef { id="boss_demon",  color=new Color(0.92f,0.12f,0.08f), glowColor=new Color(1f,0.32f,0.18f),  radius=68, isStar=true     },
            new EnemyDef { id="boss_vampire",color=new Color(0.78f,0.10f,0.38f), glowColor=new Color(1f,0.22f,0.52f),  radius=60                  },
            new EnemyDef { id="boss_witch",  color=new Color(0.55f,0.18f,0.88f), glowColor=new Color(0.72f,0.28f,1f),  radius=60, isDiamond=true  },
        };

        public static EnemyDef Get(string id)
        {
            string lower = id?.ToLower() ?? "";
            foreach (var e in All)
                if (lower.Contains(e.id) || e.id.Contains(lower)) return e;
            return All[0];
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  SPRITE FACTORY  (shared pixel-texture builder)
    // ══════════════════════════════════════════════════════════════════
    public static class SpriteFactory
    {
        private static readonly Dictionary<string, Sprite> _cache = new();

        public static Sprite GetOrCreate(string key, System.Func<Sprite> factory)
        {
            if (_cache.TryGetValue(key, out var s) && s != null) return s;
            s = factory(); _cache[key] = s; return s;
        }

        public static Sprite SolidRect(Color col, int w = 4, int h = 4)
        {
            string k = $"rect_{col.r:F2}_{col.g:F2}_{col.b:F2}_{w}_{h}";
            return GetOrCreate(k, () => {
                var tex = new Texture2D(w, h);
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++) tex.SetPixel(x, y, col);
                tex.Apply();
                return Sprite.Create(tex, new Rect(0,0,w,h), Vector2.one*0.5f, 1f);
            });
        }

        public static Sprite SolidCircle(Color col, int sz = 64)
        {
            string k = $"circ_{col.r:F2}_{col.g:F2}_{sz}";
            return GetOrCreate(k, () => {
                var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
                float c = sz / 2f;
                for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                    tex.SetPixel(x, y, Vector2.Distance(new Vector2(x,y), Vector2.one*c) < c-1 ? col : Color.clear);
                tex.Apply();
                return Sprite.Create(tex, new Rect(0,0,sz,sz), Vector2.one*0.5f, (float)sz);
            });
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  GAME VISUAL MANAGER — Main runtime component
    // ══════════════════════════════════════════════════════════════════
    public class GameVisualManager : MonoBehaviour
    {
        public static GameVisualManager Instance { get; private set; }

        // ── Config ────────────────────────────────────────────────────
        const float CAMERA_ORTHO  = 260f;   // slightly wider view
        const float PLAYER_SCALE  = 70f;    // bigger characters (was 55)
        const float PLAYER_COLLIDE= 24f;
        const float SPAWN_MIN     = 380f;
        const float SPAWN_MAX     = 650f;

        private HashSet<int>  _visualizedEnemies = new();
        private Camera        _cam;
        private EnemySpawner  _spawner;

        private void Awake() { Instance = this; }

        private void Start() { StartCoroutine(InitAfterFrame()); }

        private IEnumerator InitAfterFrame()
        {
            yield return null;

            _cam     = Camera.main;
            _spawner = FindAnyObjectByType<EnemySpawner>();

            ConfigureCamera();
            SetupPlayer();
            BuildArena();
            EnsureHUD();
            FixSpawnDistance();

            StartCoroutine(EnemyVisualLoop());
        }

        // ── Camera ────────────────────────────────────────────────────
        private void ConfigureCamera()
        {
            if (_cam == null) return;
            _cam.orthographicSize = CAMERA_ORTHO;
            // Rich dark navy background — arena sky
            _cam.backgroundColor  = new Color(0.032f, 0.028f, 0.072f);
        }

        // ── Player ────────────────────────────────────────────────────
        private void SetupPlayer()
        {
            var pGO = GameObject.FindWithTag("Player");
            if (pGO == null) return;

            var col = pGO.GetComponent<CircleCollider2D>();
            if (col) col.radius = PLAYER_COLLIDE;

            // Scale up player object
            pGO.transform.localScale = Vector3.one;

            var vis = GetOrAdd<CharacterVisuals>(pGO);
            vis.playerCtrl = pGO.GetComponent<PlayerController>();
        }

        // ── Arena Background (3-layer parallax) ───────────────────────
        private void BuildArena()
        {
            // Clean up old elements
            foreach (var n in new[]{"Arena Floor","Arena Ground","Border_N","Border_S","Border_E","Border_W",
                                    "AmbientDust","ArenaBack","ArenaMid","ArenaGround","ArenaVignette"})
            {
                var g = GameObject.Find(n);
                if (g) Destroy(g);
            }

            // ── LAYER 1: Deep space sky (furthest back) ──────────────
            CreateSkyLayer();

            // ── LAYER 2: Mid floating magic debris / runes ───────────
            CreateMidLayer();

            // ── LAYER 3: Ground floor (subtle, low-contrast) ─────────
            CreateGroundLayer();

            // ── Vignette overlay ─────────────────────────────────────
            CreateVignette();

            // ── Neon arena borders ───────────────────────────────────
            CreateArenaBorders();

            // ── Ambient particles ────────────────────────────────────
            SpawnAmbientParticles();
        }

        private void CreateSkyLayer()
        {
            // Radial gradient dark sky with stars
            int sz = 256;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            float c = sz / 2f;

            // Sky colors: deep navy center → dark purple edge
            Color skyCenter = new Color(0.038f, 0.030f, 0.095f);
            Color skyEdge   = new Color(0.012f, 0.010f, 0.042f);

            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float dx = (x - c)/c, dy = (y - c)/c;
                float dist = Mathf.Sqrt(dx*dx + dy*dy);
                Color sky = Color.Lerp(skyCenter, skyEdge, Mathf.Clamp01(dist));
                tex.SetPixel(x, y, sky);
            }

            // Scatter stars
            Random.InitState(7331);
            for (int i = 0; i < 180; i++)
            {
                int sx = Random.Range(0, sz);
                int sy = Random.Range(0, sz);
                float bright = Random.Range(0.3f, 1.0f);
                Color starCol = Random.value > 0.7f
                    ? new Color(0.6f, 0.5f, 1.0f, bright)   // purple tint
                    : new Color(bright, bright, bright * 0.9f, bright * 0.8f);
                int starR = Random.value > 0.92f ? 2 : 1;
                for (int dy = -starR; dy <= starR; dy++)
                for (int dx = -starR; dx <= starR; dx++)
                {
                    float dd = Mathf.Sqrt(dx*dx + dy*dy);
                    if (dd <= starR)
                    {
                        int px = Mathf.Clamp(sx+dx, 0, sz-1);
                        int py = Mathf.Clamp(sy+dy, 0, sz-1);
                        Color existing = tex.GetPixel(px, py);
                        tex.SetPixel(px, py, Color.Lerp(existing, starCol, bright * 0.8f));
                    }
                }
            }

            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0,0,sz,sz), Vector2.one*0.5f, 1f);

            float W = GameConstants.WORLD_W, H = GameConstants.WORLD_H;
            var skyGO = new GameObject("ArenaBack");
            var sr    = skyGO.AddComponent<SpriteRenderer>();
            sr.sprite       = sprite;
            sr.sortingOrder = -30;
            skyGO.transform.localScale = new Vector3(W * 1.02f / sz, H * 1.02f / sz, 1f);
        }

        private void CreateMidLayer()
        {
            // Floating runic debris / magical orbs in the mid-ground
            float W = GameConstants.WORLD_W, H = GameConstants.WORLD_H;
            var midParent = new GameObject("ArenaMid");
            midParent.transform.position = Vector3.zero;

            Random.InitState(9999);

            // Large dim glowing circles (distant magical sources)
            for (int i = 0; i < 8; i++)
            {
                float angle  = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float radius = Random.Range(W * 0.2f, W * 0.48f);
                Vector3 pos  = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * 0.6f, 0);

                float   sz      = Random.Range(30f, 90f);
                float   hue     = Random.Range(0.55f, 0.85f);
                Color   glowCol = Color.HSVToRGB(hue, 0.7f, 0.5f);
                glowCol.a = Random.Range(0.04f, 0.12f);

                var debrisGO = new GameObject($"MidDebris{i}");
                debrisGO.transform.SetParent(midParent.transform, false);
                debrisGO.transform.position   = pos;
                debrisGO.transform.localScale  = Vector3.one * sz;
                var sr = debrisGO.AddComponent<SpriteRenderer>();
                sr.sprite       = SpriteFactory.SolidCircle(glowCol, 32);
                sr.sortingOrder = -18;

                // Slow drift animation
                var drift = debrisGO.AddComponent<SlowDriftAnim>();
                drift.speed     = Random.Range(0.5f, 1.5f);
                drift.amplitude = Random.Range(8f, 22f);
                drift.offset    = Random.Range(0f, Mathf.PI * 2f);
            }

            // Rune circle decorations (faint ring glyphs on ground layer behind floor)
            for (int i = 0; i < 4; i++)
            {
                float angle = (i / 4f) * 360f * Mathf.Deg2Rad + 0.3f;
                Vector3 pos = new Vector3(
                    Mathf.Cos(angle) * W * 0.28f,
                    Mathf.Sin(angle) * H * 0.22f,
                    0);
                float   sz = Random.Range(80f, 160f);
                Color runeCol = new Color(0.35f, 0.22f, 0.72f, 0.07f);

                var runeGO = new GameObject($"Rune{i}");
                runeGO.transform.SetParent(midParent.transform, false);
                runeGO.transform.position  = pos;
                runeGO.transform.localScale = Vector3.one * sz;
                var sr = runeGO.AddComponent<SpriteRenderer>();
                sr.sprite       = MakeRuneRingSprite();
                sr.sortingOrder = -19;

                runeGO.AddComponent<SlowSpinAnim>().speed = (i % 2 == 0) ? 3f : -2.5f;
            }
        }

        private void CreateGroundLayer()
        {
            // Dark arena floor — subtle hex/stone pattern, very low contrast
            float W = GameConstants.WORLD_W, H = GameConstants.WORLD_H;

            // Generate a premium dark stone-hex floor tile
            int tileSize = 128;
            var tex = new Texture2D(tileSize, tileSize, TextureFormat.RGBA32, false);

            Color base1 = new Color(0.042f, 0.038f, 0.098f);  // dark purple-navy
            Color base2 = new Color(0.052f, 0.045f, 0.112f);  // slightly lighter cell
            Color grout = new Color(0.025f, 0.022f, 0.068f);  // dark grout lines
            Color glint = new Color(0.12f,  0.10f,  0.28f,  0.3f); // purple glint

            // Fill base
            for (int y = 0; y < tileSize; y++)
            for (int x = 0; x < tileSize; x++)
                tex.SetPixel(x, y, base1);

            // Draw hex-like grid (flat-topped hexagons)
            float hexW = 32f, hexH = 28f;
            for (int row = -1; row <= (int)(tileSize / hexH) + 1; row++)
            for (int col = -1; col <= (int)(tileSize / hexW) + 1; col++)
            {
                float cx = col * hexW + (row % 2 == 0 ? 0 : hexW * 0.5f);
                float cy = row * hexH * 0.75f;

                // Draw hex border dots
                for (int angle = 0; angle < 6; angle++)
                {
                    float a0 = angle * 60f * Mathf.Deg2Rad;
                    float a1 = (angle+1) * 60f * Mathf.Deg2Rad;
                    float r  = hexH * 0.5f;
                    DrawTexLine(tex, tileSize,
                        cx + Mathf.Cos(a0) * r, cy + Mathf.Sin(a0) * r,
                        cx + Mathf.Cos(a1) * r, cy + Mathf.Sin(a1) * r,
                        grout);
                }
                // Inner cell fill variant
                DrawTexDot(tex, (int)cx, (int)cy, 2, base2);
            }

            // Occasional glint points
            Random.InitState(42);
            for (int i = 0; i < 12; i++)
            {
                int gx = Random.Range(4, tileSize-4);
                int gy = Random.Range(4, tileSize-4);
                DrawTexDot(tex, gx, gy, 1, glint);
            }

            tex.Apply();
            var groundSprite = Sprite.Create(tex, new Rect(0,0,tileSize,tileSize), Vector2.one*0.5f, tileSize);

            var floor = new GameObject("ArenaGround");
            var fsr   = floor.AddComponent<SpriteRenderer>();
            fsr.sprite       = groundSprite;
            fsr.drawMode     = SpriteDrawMode.Tiled;
            fsr.tileMode     = SpriteTileMode.Continuous;
            fsr.size         = new Vector2(W / tileSize, H / tileSize);
            fsr.sortingOrder = -22;
            floor.transform.localScale = new Vector3(tileSize, tileSize, 1f);
        }

        private void CreateVignette()
        {
            // Circular dark vignette to darken edges, focus on center combat
            float W = GameConstants.WORLD_W, H = GameConstants.WORLD_H;
            int sz = 128;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            float c = sz / 2f;

            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float dx = (x - c)/c, dy = (y - c)/c;
                float dist = Mathf.Sqrt(dx*dx + dy*dy);
                // Alpha: 0 at center, 0.75 at edges
                float alpha = Mathf.Clamp01(Mathf.Pow(dist, 1.8f) * 0.72f);
                tex.SetPixel(x, y, new Color(0.01f, 0.008f, 0.03f, alpha));
            }

            tex.Apply();
            var vigSprite = Sprite.Create(tex, new Rect(0,0,sz,sz), Vector2.one*0.5f, 1f);

            var vig = new GameObject("ArenaVignette");
            var sr  = vig.AddComponent<SpriteRenderer>();
            sr.sprite       = vigSprite;
            sr.sortingOrder = -10;
            vig.transform.localScale = new Vector3(W * 1.05f / sz, H * 1.05f / sz, 1f);
        }

        private void CreateArenaBorders()
        {
            float W  = GameConstants.WORLD_W, H = GameConstants.WORLD_H;
            float bw = 18f;
            // Teal-cyan neon borders (matches original arena color)
            Color borderCol  = new Color(0.15f, 0.85f, 0.65f, 0.92f);
            Color glowCol    = new Color(0.15f, 0.85f, 0.65f, 0.25f);

            // Main solid borders
            SpawnBorder("Border_N", new Vector3(0,  H/2+bw/2,  0), new Vector3(W+bw*2, bw,    1), borderCol, -5);
            SpawnBorder("Border_S", new Vector3(0, -H/2-bw/2,  0), new Vector3(W+bw*2, bw,    1), borderCol, -5);
            SpawnBorder("Border_E", new Vector3( W/2+bw/2, 0,  0), new Vector3(bw,     H,     1), borderCol, -5);
            SpawnBorder("Border_W", new Vector3(-W/2-bw/2, 0,  0), new Vector3(bw,     H,     1), borderCol, -5);

            // Outer glow halos (slightly bigger, transparent)
            SpawnBorder("BorderGlow_N", new Vector3(0,  H/2+bw,    0), new Vector3(W+bw*4, bw*2.5f, 1), glowCol, -6);
            SpawnBorder("BorderGlow_S", new Vector3(0, -H/2-bw,    0), new Vector3(W+bw*4, bw*2.5f, 1), glowCol, -6);
            SpawnBorder("BorderGlow_E", new Vector3( W/2+bw, 0,    0), new Vector3(bw*2.5f, H+bw*2, 1), glowCol, -6);
            SpawnBorder("BorderGlow_W", new Vector3(-W/2-bw, 0,    0), new Vector3(bw*2.5f, H+bw*2, 1), glowCol, -6);

            // Corner accent gems
            float cornerX = W / 2 + bw * 0.5f;
            float cornerY = H / 2 + bw * 0.5f;
            SpawnCornerGem( cornerX,  cornerY);
            SpawnCornerGem(-cornerX,  cornerY);
            SpawnCornerGem( cornerX, -cornerY);
            SpawnCornerGem(-cornerX, -cornerY);
        }

        private void SpawnBorder(string name, Vector3 pos, Vector3 scale, Color col, int order)
        {
            var go = new GameObject(name);
            go.transform.position   = pos;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = SpriteFactory.SolidRect(col, 1, 1);
            sr.sortingOrder = order;
        }

        private void SpawnCornerGem(float x, float y)
        {
            var go = new GameObject("CornerGem");
            go.transform.position = new Vector3(x, y, 0);
            go.transform.localScale = Vector3.one * 28f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = SpriteFactory.SolidCircle(new Color(0.15f, 0.85f, 0.65f, 1f), 32);
            sr.sortingOrder = -4;
            go.AddComponent<PulseAnim>().Init(0.88f, 1.08f, 2.2f);
        }

        private void SpawnAmbientParticles()
        {
            float W = GameConstants.WORLD_W, H = GameConstants.WORLD_H;

            // Slow magic dust particles
            var psGO = new GameObject("AmbientDust");
            var ps   = psGO.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor    = new ParticleSystem.MinMaxGradient(
                new Color(0.35f, 0.22f, 0.85f, 0.06f),
                new Color(0.15f, 0.62f, 0.88f, 0.14f));
            main.startSize     = new ParticleSystem.MinMaxCurve(1.5f, 6f);
            main.startSpeed    = new ParticleSystem.MinMaxCurve(5f, 18f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 12f);
            main.maxParticles  = 250;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f);
            main.gravityModifier = -0.02f; // very slight upward drift

            var em = ps.emission;
            em.rateOverTime = 16f;

            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Rectangle;
            sh.scale     = new Vector3(W * 0.88f, H * 0.88f, 1f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.x       = new ParticleSystem.MinMaxCurve(-3f, 3f);
            vel.y       = new ParticleSystem.MinMaxCurve(2f, 8f);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = -12;
        }

        // ── HUD ───────────────────────────────────────────────────────
        private void EnsureHUD()
        {
            var oldHUD = GameObject.Find("HUD Canvas");
            if (oldHUD) Destroy(oldHUD);

            var existing = FindAnyObjectByType<HUDOverlay>();
            if (existing != null) return;

            var hudGO   = new GameObject("HUDCanvas");
            var canvas  = hudGO.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler  = hudGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0.5f;
            hudGO.AddComponent<GraphicRaycaster>();
            hudGO.AddComponent<HUDOverlay>();
        }

        // ── Spawn distance fix ────────────────────────────────────────
        private void FixSpawnDistance()
        {
            if (_spawner == null) return;
            _spawner.spawnDistMin = SPAWN_MIN;
            _spawner.spawnDistMax = SPAWN_MAX;
        }

        // ── Enemy visual loop ─────────────────────────────────────────
        private IEnumerator EnemyVisualLoop()
        {
            while (true)
            {
                var enemies = EnemySpawner.AllEnemies;
                if (enemies != null)
                {
                    foreach (var enemy in enemies)
                    {
                        if (enemy == null) continue;
                        int id = enemy.GetHashCode();
                        if (_visualizedEnemies.Contains(id)) continue;
                        _visualizedEnemies.Add(id);
                        ApplyEnemyVisuals(enemy);
                    }
                }
                yield return new WaitForSeconds(0.25f);
            }
        }

        private void ApplyEnemyVisuals(EnemyController enemy)
        {
            var def = EnemyDefs.Get(enemy.EnemyName ?? "slime");

            // Scale: boss > elite > normal — all bigger than before
            float scale = enemy.isBoss  ? def.radius * 2.5f
                        : enemy.isElite ? def.radius * 1.9f
                        : def.radius * 2.1f;

            var vis = GetOrAdd<CharacterVisuals>(enemy.gameObject);
            vis.enemyCtrl = enemy;

            // HP bar
            AddEnemyHPBar(enemy, scale);
        }

        private void AddEnemyHPBar(EnemyController enemy, float scale)
        {
            // Remove old HP bar if any
            var old = enemy.transform.Find("HPBar");
            if (old) Destroy(old.gameObject);

            var barRoot = new GameObject("HPBar");
            barRoot.transform.SetParent(enemy.transform, false);
            barRoot.transform.localPosition = new Vector3(0, scale * 0.7f + 10f, 0);

            float barW = Mathf.Max(scale * 0.95f, 24f);
            float barH = Mathf.Max(scale * 0.10f, 5f);

            // Background (dark + slight outline)
            MakeBarPart(barRoot, "BG_Outline", new Color(0f,0f,0f,0.9f), barW+4f, barH+4f, 14);
            MakeBarPart(barRoot, "BG",         new Color(0.08f,0.06f,0.12f,0.9f), barW+2f, barH+2f, 15);

            // HP fill (gradient from green to red based on boss status)
            Color hpCol = enemy.isBoss
                ? new Color(0.92f, 0.12f, 0.12f)
                : enemy.isElite
                    ? new Color(1.00f, 0.72f, 0.08f)
                    : new Color(0.18f, 0.82f, 0.28f);

            var fillGO = MakeBarPart(barRoot, "Fill", hpCol, barW, barH, 16);

            // Shine stripe on HP bar
            var shineGO = MakeBarPart(barRoot, "Shine", new Color(1f,1f,1f,0.22f), barW, barH*0.38f, 17);
            shineGO.transform.localPosition = new Vector3(0, barH*0.22f, 0);

            var updater = barRoot.AddComponent<EnemyHPBarUpdater>();
            updater.Init(enemy, fillGO.transform, barW, barH);
        }

        private GameObject MakeBarPart(GameObject parent, string name, Color col, float w, float h, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localScale    = new Vector3(w, h, 1f);
            go.transform.localPosition = Vector3.zero;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = SpriteFactory.SolidRect(col, 1, 1);
            sr.sortingOrder = order;
            return go;
        }

        // ── Sprite generators for mid layer ──────────────────────────
        private static Sprite MakeRuneRingSprite()
        {
            return SpriteFactory.GetOrCreate("rune_ring", () => {
                int sz = 64;
                var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
                float c = sz / 2f;
                for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    float d = Mathf.Sqrt((x-c)*(x-c) + (y-c)*(y-c));
                    if (d > c*0.88f || d < c*0.72f) { tex.SetPixel(x, y, Color.clear); continue; }
                    float t = 1f - Mathf.Abs(d - c*0.8f) / (c*0.08f);
                    // Dashed effect using angle
                    float angle = Mathf.Atan2(y-c, x-c);
                    float dash  = Mathf.Abs(Mathf.Sin(angle * 8f));
                    tex.SetPixel(x, y, new Color(0.5f, 0.35f, 1f, t * dash * 0.85f));
                }
                tex.Apply();
                return Sprite.Create(tex, new Rect(0,0,sz,sz), Vector2.one*0.5f, (float)sz);
            });
        }

        // ── Drawing helpers ───────────────────────────────────────────
        private static void DrawTexLine(Texture2D tex, int sz, float x0, float y0, float x1, float y1, Color col)
        {
            int steps = (int)(Mathf.Max(Mathf.Abs(x1-x0), Mathf.Abs(y1-y0)) + 1);
            for (int i = 0; i <= steps; i++)
            {
                float t = steps > 0 ? (float)i / steps : 0;
                int px = Mathf.Clamp((int)Mathf.Lerp(x0, x1, t), 0, sz-1);
                int py = Mathf.Clamp((int)Mathf.Lerp(y0, y1, t), 0, sz-1);
                Color existing = tex.GetPixel(px, py);
                tex.SetPixel(px, py, Color.Lerp(existing, col, col.a > 0 ? 1f : 0f));
            }
        }

        private static void DrawTexDot(Texture2D tex, int cx, int cy, int r, Color col)
        {
            for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
            {
                int px = Mathf.Clamp(cx+dx, 0, tex.width-1);
                int py = Mathf.Clamp(cy+dy, 0, tex.height-1);
                if (dx*dx+dy*dy <= r*r)
                {
                    Color e = tex.GetPixel(px, py);
                    tex.SetPixel(px, py, Color.Lerp(e, col, col.a));
                }
            }
        }

        // ── Utilities ─────────────────────────────────────────────────
        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  ENEMY HP BAR UPDATER
    // ══════════════════════════════════════════════════════════════════
    public class EnemyHPBarUpdater : MonoBehaviour
    {
        private EnemyController _enemy;
        private Transform       _fill;
        private float           _barW, _barH;
        private float           _smoothPct = 1f;

        public void Init(EnemyController e, Transform fill, float w, float h)
        { _enemy = e; _fill = fill; _barW = w; _barH = h; }

        private void Update()
        {
            if (_enemy == null || _fill == null) { Destroy(gameObject); return; }
            float targetPct = _enemy.MaxHp > 0 ? Mathf.Clamp01(_enemy.Hp / _enemy.MaxHp) : 1f;
            _smoothPct = Mathf.Lerp(_smoothPct, targetPct, Time.deltaTime * 8f);

            _fill.localScale    = new Vector3(_barW * _smoothPct, _barH, 1f);
            _fill.localPosition = new Vector3(_barW * (_smoothPct - 1f) * 0.5f, 0, 0);
            gameObject.SetActive(_enemy.IsAlive);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  ANIMATION HELPER COMPONENTS
    // ══════════════════════════════════════════════════════════════════
    public class SlowDriftAnim : MonoBehaviour
    {
        public float speed = 1f, amplitude = 10f, offset = 0f;
        private Vector3 _base;
        private void Start() { _base = transform.localPosition; }
        private void Update()
        {
            var p = _base;
            p.y += Mathf.Sin(Time.time * speed + offset) * amplitude;
            p.x += Mathf.Cos(Time.time * speed * 0.6f + offset) * amplitude * 0.5f;
            transform.localPosition = p;
        }
    }

    public class SlowSpinAnim : MonoBehaviour
    {
        public float speed = 5f;
        private void Update() { transform.Rotate(0f, 0f, speed * Time.deltaTime); }
    }

    public class EnemyBobAnim : MonoBehaviour
    {
        public float amount = 2f, speed = 1.5f, offset = 0f;
        private void Update()
        {
            var p = transform.localPosition;
            p.y = Mathf.Sin(Time.time * speed + offset) * amount;
            transform.localPosition = p;
        }
    }

    public class PulseAnim : MonoBehaviour
    {
        private float _minS, _maxS, _speed;
        private Vector3 _base;
        public void Init(float min, float max, float speed) { _minS = min; _maxS = max; _speed = speed; }
        private void Start()  { _base = transform.localScale; }
        private void Update()
        {
            float s = Mathf.Lerp(_minS, _maxS, (Mathf.Sin(Time.time * _speed) + 1f) * 0.5f);
            transform.localScale = _base * s;
        }
    }
}
