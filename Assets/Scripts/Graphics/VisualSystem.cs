using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AngelArena.Core;
using AngelArena.Data;

/// <summary>
/// Complete visual overhaul synced from the HTML PVE game.
/// Handles: character sprites, enemy sprites, HUD, skill effects, arena, animations.
/// Attach to a GameObject in GameScene — auto-runs on Start.
/// </summary>
namespace AngelArena.Graphics
{
    // ══════════════════════════════════════════════════════════════════
    //  CHARACTER DEFINITIONS  (synced from HTML pve.js CLASS_DEFS)
    // ══════════════════════════════════════════════════════════════════
    public static class CharacterDefs
    {
        public struct CharDef
        {
            public string id;
            public Color  bodyColor;
            public Color  glowColor;
            public Color  accentColor;
            public string icon;        // unicode emoji
            public float  drawRadius;  // visual radius in world units
        }

        public static readonly CharDef[] All = {
            new CharDef { id="fighter",     bodyColor=new Color(0.95f,0.45f,0.20f), glowColor=new Color(1f,0.65f,0.35f),   accentColor=new Color(0.8f,0.3f,0.1f), icon="⚔",  drawRadius=28 },
            new CharDef { id="mage",        bodyColor=new Color(0.22f,0.56f,0.97f), glowColor=new Color(0.50f,0.80f,1.0f), accentColor=new Color(0.1f,0.4f,0.8f), icon="🔮", drawRadius=26 },
            new CharDef { id="assassin",    bodyColor=new Color(0.60f,0.20f,0.90f), glowColor=new Color(0.80f,0.45f,1.0f), accentColor=new Color(0.4f,0.1f,0.7f), icon="🗡",  drawRadius=24 },
            new CharDef { id="ranger",      bodyColor=new Color(0.20f,0.78f,0.35f), glowColor=new Color(0.45f,0.95f,0.45f),accentColor=new Color(0.1f,0.6f,0.2f), icon="🏹", drawRadius=25 },
            new CharDef { id="paladin",     bodyColor=new Color(0.99f,0.88f,0.20f), glowColor=new Color(1.0f,0.96f,0.60f), accentColor=new Color(0.8f,0.7f,0.1f), icon="🛡",  drawRadius=30 },
            new CharDef { id="necromancer", bodyColor=new Color(0.35f,0.20f,0.55f), glowColor=new Color(0.60f,0.40f,0.80f),accentColor=new Color(0.2f,0.1f,0.4f), icon="💀", drawRadius=26 },
            new CharDef { id="druid",       bodyColor=new Color(0.25f,0.65f,0.20f), glowColor=new Color(0.55f,0.90f,0.35f),accentColor=new Color(0.2f,0.5f,0.1f), icon="🌿", drawRadius=26 },
        };

        public static CharDef Get(string id)
        {
            foreach (var c in All) if (c.id.ToLower() == id.ToLower()) return c;
            return All[0]; // default = fighter
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  ENEMY VISUAL DEFINITIONS  (synced from HTML pve.js ENEMY_TYPES)
    // ══════════════════════════════════════════════════════════════════
    public static class EnemyDefs
    {
        public struct EnemyDef
        {
            public string id;
            public Color  color;
            public Color  glowColor;
            public float  radius;     // world-unit radius
            public bool   isBlob;     // wavy blob shape
            public bool   isDiamond;  // diamond shape
            public bool   isStar;     // star shape
            public bool   isGhost;    // ghost shape
            public bool   isHex;      // hexagon shape
        }

        public static readonly EnemyDef[] All = {
            new EnemyDef { id="slime",      color=new Color(0.25f,0.90f,0.25f), glowColor=new Color(0.4f,1f,0.4f),   radius=20, isBlob=true   },
            new EnemyDef { id="goblin",     color=new Color(0.60f,0.90f,0.15f), glowColor=new Color(0.8f,1f,0.3f),   radius=22, isDiamond=true },
            new EnemyDef { id="skeleton",   color=new Color(0.88f,0.88f,0.88f), glowColor=new Color(1f,1f,1f),       radius=22            },
            new EnemyDef { id="orc",        color=new Color(0.35f,0.70f,0.15f), glowColor=new Color(0.5f,0.9f,0.3f), radius=26            },
            new EnemyDef { id="demon",      color=new Color(0.88f,0.18f,0.12f), glowColor=new Color(1f,0.4f,0.3f),   radius=28, isStar=true   },
            new EnemyDef { id="wraith",     color=new Color(0.45f,0.25f,0.75f), glowColor=new Color(0.7f,0.5f,1f),   radius=26, isGhost=true  },
            new EnemyDef { id="golem",      color=new Color(0.48f,0.50f,0.58f), glowColor=new Color(0.7f,0.7f,0.8f), radius=32, isHex=true    },
            new EnemyDef { id="vampire",    color=new Color(0.72f,0.10f,0.22f), glowColor=new Color(0.9f,0.3f,0.4f), radius=26            },
            new EnemyDef { id="witch",      color=new Color(0.40f,0.12f,0.72f), glowColor=new Color(0.65f,0.3f,1f),  radius=24, isDiamond=true},
            new EnemyDef { id="giant",      color=new Color(0.55f,0.40f,0.22f), glowColor=new Color(0.75f,0.6f,0.35f),radius=42           },
            new EnemyDef { id="elite_orc",  color=new Color(1f,0.80f,0.10f),   glowColor=new Color(1f,1f,0.4f),     radius=30, isStar=true   },
            new EnemyDef { id="elite_demon",color=new Color(1f,0.45f,0.10f),   glowColor=new Color(1f,0.7f,0.3f),   radius=32, isStar=true   },
            new EnemyDef { id="boss_dragon",color=new Color(0.85f,0.10f,0.10f), glowColor=new Color(1f,0.3f,0.2f),  radius=70            },
            new EnemyDef { id="boss_lich",  color=new Color(0.35f,0.10f,0.50f), glowColor=new Color(0.7f,0.3f,1f),  radius=65            },
            new EnemyDef { id="boss_golem", color=new Color(0.45f,0.45f,0.55f), glowColor=new Color(0.7f,0.7f,0.9f),radius=75, isHex=true  },
            new EnemyDef { id="boss_demon", color=new Color(0.90f,0.15f,0.08f), glowColor=new Color(1f,0.35f,0.2f), radius=68, isStar=true  },
            new EnemyDef { id="boss_vampire",color=new Color(0.65f,0.08f,0.18f),glowColor=new Color(0.9f,0.2f,0.3f),radius=60            },
            new EnemyDef { id="boss_witch", color=new Color(0.35f,0.08f,0.65f), glowColor=new Color(0.6f,0.2f,1f),  radius=60, isDiamond=true},
        };

        public static EnemyDef Get(string id)
        {
            string lower = id?.ToLower() ?? "";
            foreach (var e in All)
                if (lower.Contains(e.id) || e.id.Contains(lower)) return e;
            return All[0]; // default slime
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  PROCEDURAL SPRITE FACTORY
    // ══════════════════════════════════════════════════════════════════
    public static class SpriteFactory
    {
        private static readonly Dictionary<string, Sprite> _cache = new();

        public static Sprite GetOrCreate(string key, System.Func<Sprite> factory)
        {
            if (_cache.TryGetValue(key, out var s)) return s;
            s = factory(); _cache[key] = s; return s;
        }

        // ── Character: gradient glow circle ────────────────────
        public static Sprite CharSprite(CharacterDefs.CharDef def)
        {
            string k = $"char_{def.id}";
            return GetOrCreate(k, () => {
                int sz = 128;
                var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
                float c = sz / 2f;
                for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    float d = Vector2.Distance(new Vector2(x,y), Vector2.one*c);
                    if (d >= c) { tex.SetPixel(x,y,Color.clear); continue; }
                    float t = d / c;
                    // outer glow ring
                    bool ring = d > c * 0.78f && d < c * 0.92f;
                    Color col;
                    if (ring)
                        col = new Color(def.glowColor.r, def.glowColor.g, def.glowColor.b,
                                        Mathf.Lerp(0.6f, 0.9f, 1f - Mathf.Abs(t - 0.85f) / 0.07f));
                    else
                        col = Color.Lerp(Color.Lerp(Color.white, def.bodyColor, t * t * 0.7f),
                                         def.accentColor, t > 0.65f ? (t - 0.65f) / 0.35f : 0f);
                    col.a = d < c * 0.85f ? 0.95f : Mathf.Clamp01((c - d) / (c * 0.15f));
                    tex.SetPixel(x, y, col);
                }
                // face details — eyes
                DrawCircle(tex, (int)(c - c*0.28f), (int)(c + c*0.12f), (int)(c*0.13f), new Color(0.05f,0.05f,0.1f,0.9f));
                DrawCircle(tex, (int)(c + c*0.28f), (int)(c + c*0.12f), (int)(c*0.13f), new Color(0.05f,0.05f,0.1f,0.9f));
                DrawCircle(tex, (int)(c - c*0.28f), (int)(c + c*0.12f), (int)(c*0.05f), Color.white);
                DrawCircle(tex, (int)(c + c*0.28f), (int)(c + c*0.12f), (int)(c*0.05f), Color.white);
                tex.Apply();
                return Sprite.Create(tex, new Rect(0,0,sz,sz), Vector2.one*0.5f, (float)sz);
            });
        }

        // ── Enemy sprite based on shape type ───────────────────
        public static Sprite EnemySprite(EnemyDefs.EnemyDef def)
        {
            string k = $"enemy_{def.id}";
            return GetOrCreate(k, () => {
                int sz = 128;
                var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
                float c = sz / 2f;
                if      (def.isBlob)    DrawBlob(tex, sz, def.color, def.glowColor);
                else if (def.isDiamond) DrawDiamond(tex, sz, def.color, def.glowColor);
                else if (def.isStar)    DrawStar(tex, sz, def.color, def.glowColor);
                else if (def.isGhost)   DrawGhost(tex, sz, def.color, def.glowColor);
                else if (def.isHex)     DrawHexagon(tex, sz, def.color, def.glowColor);
                else                    DrawCircleEnemy(tex, sz, def.color, def.glowColor);
                // Eyes for non-boss
                if (def.radius < 55)
                {
                    DrawCircle(tex, (int)(c-c*0.30f), (int)(c+c*0.10f), (int)(c*0.14f), new Color(0.05f,0.05f,0.08f,0.9f));
                    DrawCircle(tex, (int)(c+c*0.30f), (int)(c+c*0.10f), (int)(c*0.14f), new Color(0.05f,0.05f,0.08f,0.9f));
                    DrawCircle(tex, (int)(c-c*0.28f), (int)(c+c*0.08f), (int)(c*0.05f), new Color(1f,0.2f,0.2f,0.9f));
                    DrawCircle(tex, (int)(c+c*0.28f), (int)(c+c*0.08f), (int)(c*0.05f), new Color(1f,0.2f,0.2f,0.9f));
                }
                tex.Apply();
                return Sprite.Create(tex, new Rect(0,0,sz,sz), Vector2.one*0.5f, (float)sz);
            });
        }

        // ── Shared drawing helpers ──────────────────────────────
        private static void DrawBlob(Texture2D tex, int sz, Color c, Color glow)
        {
            float center = sz / 2f;
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float dx = (x - center)/center, dy = (y - center)/center;
                float blob = dx*dx + dy*dy
                            + 0.12f * Mathf.Sin(dx * 9f + 0.5f)
                            + 0.08f * Mathf.Cos(dy * 7f);
                if (blob > 0.82f) { tex.SetPixel(x,y,Color.clear); continue; }
                float t = blob / 0.82f;
                Color pc = Color.Lerp(glow, c, t * t);
                pc.a = Mathf.Clamp01((0.82f - blob) * 7f);
                tex.SetPixel(x, y, pc);
            }
        }

        private static void DrawDiamond(Texture2D tex, int sz, Color c, Color glow)
        {
            float center = sz / 2f;
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float nx = Mathf.Abs(x - center)/center, ny = Mathf.Abs(y - center)/center;
                float dist = nx + ny;
                if (dist > 0.88f) { tex.SetPixel(x,y,Color.clear); continue; }
                Color pc = Color.Lerp(glow, c, dist/0.88f);
                pc.a = Mathf.Clamp01((0.88f - dist) * 9f);
                tex.SetPixel(x, y, pc);
            }
        }

        private static void DrawStar(Texture2D tex, int sz, Color c, Color glow)
        {
            float center = sz / 2f;
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float dx = (x - center)/center, dy = (y - center)/center;
                float angle = Mathf.Atan2(dy, dx);
                float r2    = Mathf.Sqrt(dx*dx + dy*dy);
                float star  = r2 * (0.5f + 0.5f * Mathf.Abs(Mathf.Cos(5f * angle)));
                if (star > 0.65f) { tex.SetPixel(x,y,Color.clear); continue; }
                Color pc = Color.Lerp(glow, c, star/0.65f);
                pc.a = Mathf.Clamp01((0.65f - star) * 10f);
                tex.SetPixel(x, y, pc);
            }
        }

        private static void DrawGhost(Texture2D tex, int sz, Color c, Color glow)
        {
            float center = sz / 2f;
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float dx = (x - center)/center, dy = (y - center)/center;
                float dist = Mathf.Sqrt(dx*dx + (dy - 0.08f)*(dy - 0.08f));
                float wave = y > center ? 0.18f * Mathf.Sin((x - center)*0.25f) : 0;
                if (dist + wave > 0.82f) { tex.SetPixel(x,y,Color.clear); continue; }
                Color pc = Color.Lerp(glow, c, dist/0.82f);
                pc.a = 0.75f * Mathf.Clamp01((0.82f - dist - wave) * 5f);
                tex.SetPixel(x, y, pc);
            }
        }

        private static void DrawHexagon(Texture2D tex, int sz, Color c, Color glow)
        {
            float center = sz / 2f;
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float dx = Mathf.Abs(x - center)/center, dy = Mathf.Abs(y - center)/center;
                float hex = Mathf.Max(dx * 1.0f, 0.577f * dx + dy * 0.866f);
                if (hex > 0.80f) { tex.SetPixel(x,y,Color.clear); continue; }
                float crack = Mathf.Abs(Mathf.Sin((x+y)*0.35f)) * 0.15f;
                Color pc = Color.Lerp(glow, c * (0.7f + crack), hex/0.8f);
                pc.a = Mathf.Clamp01((0.80f - hex) * 9f);
                tex.SetPixel(x, y, pc);
            }
        }

        private static void DrawCircleEnemy(Texture2D tex, int sz, Color c, Color glow)
        {
            float center = sz / 2f;
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float d = Vector2.Distance(new Vector2(x,y), Vector2.one*center);
                if (d >= center) { tex.SetPixel(x,y,Color.clear); continue; }
                float t = d/center;
                bool ring = d > center * 0.75f && d < center * 0.90f;
                Color pc = ring ? new Color(glow.r,glow.g,glow.b,0.7f)
                                : Color.Lerp(glow, c, t*t);
                pc.a = d < center * 0.88f ? 0.92f : Mathf.Clamp01((center - d)/(center*0.12f));
                tex.SetPixel(x, y, pc);
            }
        }

        private static void DrawCircle(Texture2D tex, int cx, int cy, int r, Color col)
        {
            for (int y = cy - r; y <= cy + r; y++)
            for (int x = cx - r; x <= cx + r; x++)
            {
                if (x < 0 || y < 0 || x >= tex.width || y >= tex.height) continue;
                if ((x-cx)*(x-cx)+(y-cy)*(y-cy) < r*r) tex.SetPixel(x, y, col);
            }
        }

        // ── Simple pixel sprites ────────────────────────────────
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
            string k = $"circ_{col.r:F2}_{sz}";
            return GetOrCreate(k, () => {
                var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
                float c = sz/2f;
                for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                    tex.SetPixel(x,y, Vector2.Distance(new Vector2(x,y),Vector2.one*c) < c-1 ? col : Color.clear);
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

        // Config  ── synced from HTML pve.js
        const float CAMERA_ORTHO   = 250f;   // HTML viewport = 960x540 of 3840x2160 world
        const float PLAYER_SCALE   = 55f;    // world units (≈55px in our coord system)
        const float PLAYER_COLLIDE = 22f;    // collider radius
        const float SPAWN_MIN      = 350f;   // enemy spawn min distance
        const float SPAWN_MAX      = 600f;   // enemy spawn max distance

        private HashSet<int> _visualizedEnemies = new();
        private Camera _cam;
        private EnemySpawner _spawner;
        private GameObject _playerGO;
        private SpriteRenderer _playerSR;
        private GameObject _playerGlowRing;

        private void Awake()  { Instance = this; }

        private void Start()
        {
            StartCoroutine(InitAfterFrame());
        }

        private IEnumerator InitAfterFrame()
        {
            yield return null; // wait one frame for all MonoBehaviours to Start

            _cam = Camera.main;
            _spawner = FindAnyObjectByType<EnemySpawner>();

            FixCamera();
            SetupPlayer();
            SetupArena();
            SetupHUD();
            FixSpawnDistance();

            StartCoroutine(EnemyVisualLoop());
            StartCoroutine(PlayerAnimLoop());
        }

        // ══ Camera ══════════════════════════════════════════════
        private void FixCamera()
        {
            if (_cam == null) return;
            _cam.orthographicSize = CAMERA_ORTHO;
            _cam.backgroundColor  = new Color(0.035f, 0.045f, 0.08f);
            // Update CameraController halfCam values
            var cc = _cam.GetComponent<CameraController>();
            // Force re-init by using reflection or just leave it — clamping will adjust
        }

        // ══ Player Setup ═════════════════════════════════════════
        private void SetupPlayer()
        {
            _playerGO = GameObject.FindWithTag("Player");
            if (_playerGO == null) return;

            // Fix collider
            var col = _playerGO.GetComponent<CircleCollider2D>();
            if (col) col.radius = PLAYER_COLLIDE;

            // Attach new 2.5D visual component
            var vis = GetOrAddComponent<CharacterVisuals>(_playerGO);
            vis.playerCtrl = _playerGO.GetComponent<PlayerController>();
        }

        // ══ Arena ════════════════════════════════════════════════
        private void SetupArena()
        {
            // Remove old arena objects
            foreach (var name in new[]{"Arena Ground","Border_0","Border_1","Border_2","Border_3"})
            {
                var go = GameObject.Find(name);
                if (go) Destroy(go);
            }

            // ── Tiled dark floor ──
            var floor = new GameObject("Arena Floor");
            var fsr   = floor.AddComponent<SpriteRenderer>();
            
            // Checkerboard Grass Floor Sync
            fsr.sprite    = SpriteFactory.GetOrCreate("checkerboard_grass_floor", () => GenerateCheckerboardGrassFloor());
            fsr.drawMode  = SpriteDrawMode.Tiled;
            fsr.tileMode  = SpriteTileMode.Continuous;
            fsr.size      = new Vector2(GameConstants.WORLD_W / 96f, GameConstants.WORLD_H / 96f);
            fsr.sortingOrder = -20;
            floor.transform.localScale = new Vector3(96f, 96f, 1f);

            // ── Glowing borders ──
            float W = GameConstants.WORLD_W, H = GameConstants.WORLD_H;
            float bw = 20f;
            var borderCol = new Color(0.2f, 0.83f, 0.6f, 0.9f);
            CreateBorder("Border_N", new Vector3(0,  H/2 + bw/2, 0), new Vector3(W + bw*2, bw, 1), borderCol);
            CreateBorder("Border_S", new Vector3(0, -H/2 - bw/2, 0), new Vector3(W + bw*2, bw, 1), borderCol);
            CreateBorder("Border_E", new Vector3( W/2 + bw/2, 0, 0), new Vector3(bw, H, 1), borderCol);
            CreateBorder("Border_W", new Vector3(-W/2 - bw/2, 0, 0), new Vector3(bw, H, 1), borderCol);

            // ── Ambient particle system ──
            var psGO = new GameObject("AmbientDust");
            var ps   = psGO.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor    = new ParticleSystem.MinMaxGradient(
                new Color(0.2f,0.8f,0.4f,0.05f), new Color(0.4f,0.95f,0.6f,0.15f));
            main.startSize     = new ParticleSystem.MinMaxCurve(1f, 4f);
            main.startSpeed    = new ParticleSystem.MinMaxCurve(8f, 25f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 10f);
            main.maxParticles  = 300;
            var em = ps.emission; em.rateOverTime = 20f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Rectangle;
            sh.scale     = new Vector3(W * 0.9f, H * 0.9f, 1f);
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = -10;
        }

        private Sprite GenerateCheckerboardGrassFloor()
        {
            int tileSize = 96;
            int sz = tileSize * 2;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            
            Sprite tl = GenerateGrassTile(true);
            Sprite tr = GenerateGrassTile(false);
            Sprite bl = GenerateGrassTile(false);
            Sprite br = GenerateGrassTile(true);
            
            CopyPixels(tl.texture, tex, 0, tileSize);
            CopyPixels(tr.texture, tex, tileSize, tileSize);
            CopyPixels(bl.texture, tex, 0, 0);
            CopyPixels(br.texture, tex, tileSize, 0);
            
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, tileSize);
        }

        private Sprite GenerateGrassTile(bool isEven)
        {
            int sz = 96;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            Color bgCol = isEven ? new Color(0.027f, 0.07f, 0.04f) : new Color(0.043f, 0.1f, 0.06f);
            
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                tex.SetPixel(x, y, bgCol);
            }

            Color[] bladeColors = {
                new Color(0.12f, 0.3f, 0.1f),
                new Color(0.16f, 0.4f, 0.13f),
                new Color(0.1f, 0.33f, 0.09f),
                new Color(0.23f, 0.48f, 0.18f)
            };
            
            Random.InitState(isEven ? 42 : 1337);
            for (int i = 0; i < 22; i++)
            {
                int gx = Random.Range(4, sz - 4);
                int gy = Random.Range(4, sz - 4);
                int h = Random.Range(5, 14);
                Color col = bladeColors[Random.Range(0, bladeColors.Length)];
                DrawLine(tex, gx, gy, gx - 1, gy + h, col);
                DrawLine(tex, gx - 1, gy + h, gx + 1, gy + h - 2, col);
            }

            Color[] flowerColors = {
                new Color(0.98f, 0.75f, 0.14f),
                new Color(0.95f, 0.45f, 0.71f),
                new Color(0.22f, 0.74f, 0.97f),
                new Color(0.65f, 0.54f, 0.98f)
            };
            for (int i = 0; i < 3; i++)
            {
                int fx = Random.Range(10, sz - 10);
                int fy = Random.Range(10, sz - 10);
                Color fc = flowerColors[Random.Range(0, flowerColors.Length)];
                DrawCircle(tex, fx - 2, fy, 2, fc);
                DrawCircle(tex, fx + 2, fy, 2, fc);
                DrawCircle(tex, fx, fy - 2, 2, fc);
                DrawCircle(tex, fx, fy + 2, 2, fc);
                DrawCircle(tex, fx, fy, 1, Color.white);
            }

            Color gridColor = new Color(0.2f, 0.83f, 0.6f, 0.03f);
            for (int i = 0; i < sz; i++)
            {
                tex.SetPixel(i, 0, gridColor);
                tex.SetPixel(i, sz - 1, gridColor);
                tex.SetPixel(0, i, gridColor);
                tex.SetPixel(sz - 1, i, gridColor);
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, sz);
        }

        private void CopyPixels(Texture2D src, Texture2D dest, int destX, int destY)
        {
            for (int y = 0; y < src.height; y++)
            for (int x = 0; x < src.width; x++)
            {
                dest.SetPixel(destX + x, destY + y, src.GetPixel(x, y));
            }
        }

        private void DrawLine(Texture2D tex, float x0, float y0, float x1, float y1, Color col)
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

        private void DrawCircle(Texture2D tex, int cx, int cy, int r, Color col)
        {
            for (int y = cy - r; y <= cy + r; y++)
            for (int x = cx - r; x <= cx + r; x++)
            {
                if (x < 0 || y < 0 || x >= tex.width || y >= tex.height) continue;
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) < r * r) tex.SetPixel(x, y, col);
            }
        }

        private void CreateBorder(string name, Vector3 pos, Vector3 scale, Color col)
        {
            var go = new GameObject(name);
            go.transform.position   = pos;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.SolidRect(col, 1, 1);
            sr.sortingOrder = -5;
        }

        // ══ HUD ══════════════════════════════════════════════════
        private void SetupHUD()
        {
            // Remove old HUDs that might conflict
            var oldHUD = GameObject.Find("HUD Canvas");
            if (oldHUD) Destroy(oldHUD);

            // Ensure HUDOverlay is present
            var existing = FindAnyObjectByType<HUDOverlay>();
            if (existing == null)
            {
                var hudGO = new GameObject("HUDCanvas");
                var canvas = hudGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                var scaler = hudGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight  = 0.5f;
                hudGO.AddComponent<GraphicRaycaster>();
                hudGO.AddComponent<HUDOverlay>();
            }
        }

        // ══ Spawn Distance Fix ════════════════════════════════════
        private void FixSpawnDistance()
        {
            if (_spawner == null) return;
            _spawner.spawnDistMin = SPAWN_MIN;
            _spawner.spawnDistMax = SPAWN_MAX;
        }

        private IEnumerator EnemyVisualLoop()
        {
            while (true)
            {
                var enemies = EnemySpawner.AllEnemies;
                foreach (var enemy in enemies)
                {
                    if (enemy == null) continue;
                    int id = enemy.GetHashCode();
                    if (_visualizedEnemies.Contains(id)) continue;
                    _visualizedEnemies.Add(id);
                    ApplyEnemyVisuals(enemy);
                }
                yield return new WaitForSeconds(0.3f);
            }
        }

        private void ApplyEnemyVisuals(EnemyController enemy)
        {
            var def = EnemyDefs.Get(enemy.EnemyName ?? "slime");
            float scale = enemy.isBoss ? def.radius * 2.2f
                        : enemy.isElite ? def.radius * 1.6f
                        : def.radius * 2f;

            // Attach new 2.5D visual component
            var vis = GetOrAddComponent<CharacterVisuals>(enemy.gameObject);
            vis.enemyCtrl = enemy;

            // HP bar above enemy
            AddEnemyHPBar(enemy, scale);
        }

        private void AddEnemyHPBar(EnemyController enemy, float scale)
        {
            // World-space HP bar
            var barRoot = new GameObject("HPBar");
            barRoot.transform.SetParent(enemy.transform, false);
            barRoot.transform.localPosition = new Vector3(0, scale * 0.65f + 8f, 0);

            float barW = Mathf.Max(scale * 0.9f, 20f);
            float barH = Mathf.Max(scale * 0.09f, 4f);

            // Background
            var bgGO = new GameObject("BG");
            bgGO.transform.SetParent(barRoot.transform, false);
            var bgSR = bgGO.AddComponent<SpriteRenderer>();
            bgSR.sprite       = SpriteFactory.SolidRect(new Color(0.1f,0.1f,0.1f,0.8f), 1, 1);
            bgSR.sortingOrder = 15;
            bgGO.transform.localScale = new Vector3(barW + 2f, barH + 2f, 1f);

            // HP fill
            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(barRoot.transform, false);
            Color hpCol = enemy.isBoss ? new Color(0.9f,0.1f,0.1f) : new Color(0.2f,0.8f,0.2f);
            var fillSR  = fillGO.AddComponent<SpriteRenderer>();
            fillSR.sprite       = SpriteFactory.SolidRect(hpCol, 1, 1);
            fillSR.sortingOrder = 16;
            fillGO.transform.localScale    = new Vector3(barW, barH, 1f);
            fillGO.transform.localPosition = Vector3.zero;

            // Attach updater
            var updater = barRoot.AddComponent<EnemyHPBarUpdater>();
            updater.Init(enemy, fillGO.transform, barW, barH);
        }

        // ══ Player Anim Loop ══════════════════════════════════════
        private IEnumerator PlayerAnimLoop()
        {
            float t = 0;
            while (true)
            {
                t += Time.deltaTime;
                if (_playerGlowRing)
                {
                    float pulse = 1f + 0.08f * Mathf.Sin(t * 2.2f);
                    _playerGlowRing.transform.localScale = Vector3.one * (PLAYER_SCALE * 1.55f * pulse);
                    var gsr = _playerGlowRing.GetComponent<SpriteRenderer>();
                    if (gsr) gsr.color = new Color(gsr.color.r, gsr.color.g, gsr.color.b,
                                                   0.18f + 0.08f * Mathf.Sin(t * 1.8f));
                }
                yield return null;
            }
        }

        // ══ Utilities ════════════════════════════════════════════
        private static GameObject CreateChild(GameObject parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
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
        private float           _barW;
        private float           _barH;

        public void Init(EnemyController e, Transform fill, float w, float h)
        {
            _enemy = e; _fill = fill; _barW = w; _barH = h;
        }

        private void Update()
        {
            if (_enemy == null || _fill == null) { Destroy(gameObject); return; }
            float pct = _enemy.MaxHp > 0 ? Mathf.Clamp01(_enemy.Hp / _enemy.MaxHp) : 1f;
            _fill.localScale    = new Vector3(_barW * pct, _barH, 1f);
            _fill.localPosition = new Vector3(_barW * (pct - 1f) * 0.5f, 0, 0); // left-align
            gameObject.SetActive(_enemy.IsAlive);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  ANIMATION COMPONENTS
    // ══════════════════════════════════════════════════════════════════
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
        public float minScale = 0.9f, maxScale = 1.1f, speed = 1.5f;
        private Vector3 _base;
        private void Start()  { _base = transform.localScale; }
        private void Update()
        {
            float s = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(Time.time * speed) + 1f) * 0.5f);
            transform.localScale = _base * s;
        }
    }
}
