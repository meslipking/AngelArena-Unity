using System;
using UnityEngine;
using AngelArena.Core;
using AngelArena.Audio;

namespace AngelArena.Skills
{
    public class LootGem : MonoBehaviour
    {
        public string gemType = "xp_orb"; // xp_orb, gold_orb
        public string gemTier = "small";  // small, medium, large, boss
        public int    value   = 1;
 
        private Transform      _player;
        private SpriteRenderer _sr;
        private Vector2        _velocity;
        private float          _pulseTimer;
 
        // Visual properties based on tier
        private Color _color;
        private float _size;
 
        private void Start()
        {
            var pGO = GameObject.FindWithTag("Player");
            if (pGO != null) _player = pGO.transform;
 
            _sr = gameObject.GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
 
            SetupVisuals();
 
            // Random initial pop-out velocity
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float speed = UnityEngine.Random.Range(45f, 65f);
            _velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
 
            _pulseTimer = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        }

        private void SetupVisuals()
        {
            if (gemType == "gold_orb")
            {
                _color = new Color(0.98f, 0.75f, 0.14f); // #fbbf24
                _size = gemTier == "boss" ? 14f : 8f;
                _sr.sprite = Graphics.SpriteFactory.GetOrCreate($"gem_gold_{gemTier}", () => GenerateCoinTexture());
                _sr.sortingOrder = -4;
            }
            else
            {
                // XP Gem
                switch (gemTier)
                {
                    case "boss":
                        _color = new Color(0.98f, 0.75f, 0.14f); // vàng #fbbf24
                        _size = 13f;
                        break;
                    case "large":
                        _color = new Color(0.75f, 0.52f, 0.99f); // tím #c084fc
                        _size = 10f;
                        break;
                    case "medium":
                        _color = new Color(0.22f, 0.74f, 0.97f); // xanh dương #38bdf8
                        _size = 8f;
                        break;
                    default:
                        _color = new Color(0.29f, 0.87f, 0.50f); // xanh lá #4ade80
                        _size = 6f;
                        break;
                }

                _sr.sprite = Graphics.SpriteFactory.GetOrCreate($"gem_xp_{gemTier}", () => GenerateDiamondTexture());
                _sr.sortingOrder = -4;
            }

            transform.localScale = Vector3.one * _size;
        }

        private void Update()
        {
            if (!GameManager.Instance.GameRunning || _player == null) return;

            // Pulse scale anim like the HTML game
            _pulseTimer += Time.deltaTime * 6.5f;
            float pulse = 1f + 0.15f * Mathf.Sin(_pulseTimer);
            transform.localScale = Vector3.one * (_size * pulse);

            float dx = _player.position.x - transform.position.x;
            float dy = _player.position.y - transform.position.y;
            float distSq = dx * dx + dy * dy;

            // Collect condition
            if (distSq < 150f) // ~12 world units distance
            {
                Collect();
                return;
            }

            float dist = Mathf.Sqrt(distSq);
            var playerCtrl = GameManager.Instance.playerController;
            float magnetRadius = playerCtrl != null ? playerCtrl.MagnetRadius : 100f;
            bool autoVacuum = GameManager.Instance.enemySpawner != null && GameManager.Instance.enemySpawner.autoVacuumActive;

            bool shouldPull = dist < magnetRadius || autoVacuum;

            if (shouldPull)
            {
                float speedBase = autoVacuum ? 850f : 240f;
                // Speeds up as it gets closer
                float spd = speedBase + (1f - Mathf.Clamp01(dist / magnetRadius)) * 520f;
                transform.position = Vector3.MoveTowards(transform.position, _player.position, spd * Time.deltaTime);
            }
            else
            {
                // Slow down initial pop velocity
                transform.Translate(_velocity * Time.deltaTime, Space.World);
                _velocity *= 0.86f;
            }
        }

        private void Collect()
        {
            if (gemType == "gold_orb")
            {
                GameManager.Instance.AddGoldSession(value);
                VFXSystem.SpawnFloatText(transform.position + Vector3.up * 18f, $"+{value}🪙", new Color(0.98f, 0.75f, 0.14f), true);
            }
            else
            {
                GameManager.Instance.playerController?.GainXp(value);
                // Float text for XP
                VFXSystem.SpawnFloatText(transform.position + Vector3.up * 18f, $"+{value} XP", new Color(0.65f, 0.54f, 0.98f), false);
            }

            // Audio trigger if available
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxLevelUp, 0.3f);
            }

            Destroy(gameObject);
        }

        // ── Drawing dynamic coin texture ──
        private Sprite GenerateCoinTexture()
        {
            int sz = 32;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            float c = sz / 2f;
            float r = sz * 0.4f;

            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), Vector2.one * c);
                if (d > r)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                // Shading border
                if (d > r * 0.8f)
                {
                    tex.SetPixel(x, y, new Color(0.8f, 0.55f, 0.05f)); // darker outline
                }
                else if (d < r * 0.35f && (x - c) * (x - c) + (y - c) * (y - c) < r * r * 0.08f)
                {
                    tex.SetPixel(x, y, new Color(0.98f, 0.75f, 0.14f)); // center highlight
                }
                else
                {
                    tex.SetPixel(x, y, new Color(0.95f, 0.68f, 0.08f)); // coin face
                }
            }

            // Specular highlight top-left
            DrawCircle(tex, (int)(c - r * 0.35f), (int)(c + r * 0.35f), (int)(r * 0.18f), Color.white);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, sz);
        }

        // ── Drawing dynamic diamond shape ──
        private Sprite GenerateDiamondTexture()
        {
            int sz = 32;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            float c = sz / 2f;
            float r = sz * 0.45f;

            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                // Convert coordinates to centered
                float dx = x - c;
                float dy = y - c;

                // Diamond shape formula: |dx| + |dy| <= r
                // Multipliers to stretch heights/widths like the HTML star:
                // ctx.moveTo(0, -rc * 1.5); ctx.lineTo(rc * 0.9, 0); ctx.lineTo(0, rc * 1.0); ctx.lineTo(-rc * 0.9, 0);
                float nx = Mathf.Abs(dx) / (r * 0.9f);
                float ny = dy < 0 ? -dy / (r * 1.5f) : dy / (r * 1.0f);
                float dist = nx + ny;

                if (dist > 1.0f)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                // Base glow shading
                Color col = _color;
                col.a = Mathf.Clamp01((1.0f - dist) * 8f);
                tex.SetPixel(x, y, col);

                // Inner facet highlight (top facet)
                float inX = Mathf.Abs(dx) / (r * 0.4f);
                float inY = dy < 0 ? -dy / (r * 1.5f) : dy / (r * 0.1f);
                if (dy <= 0.1f && inX + inY <= 1.0f)
                {
                    // Overlay bright highlight
                    Color hi = Color.white;
                    hi.a = 0.5f * (1.0f - (inX + inY));
                    tex.SetPixel(x, y, Color.Lerp(col, Color.white, 0.55f));
                }
            }

            // Outline edge
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float dx = x - c;
                float dy = y - c;
                float nx = Mathf.Abs(dx) / (r * 0.9f);
                float ny = dy < 0 ? -dy / (r * 1.5f) : dy / (r * 1.0f);
                float dist = nx + ny;

                if (dist > 0.88f && dist <= 1.0f)
                {
                    // Darken outline
                    Color dark = new Color(_color.r * 0.5f, _color.g * 0.5f, _color.b * 0.5f, 0.9f);
                    tex.SetPixel(x, y, dark);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, sz);
        }

        private void DrawCircle(Texture2D tex, int cx, int cy, int r, Color col)
        {
            for (int y = cy - r; y <= cy + r; y++)
            for (int x = cx - r; x <= cx + r; x++)
            {
                if (x < 0 || y < 0 || x >= tex.width || y >= tex.height) continue;
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) < r * r)
                {
                    tex.SetPixel(x, y, col);
                }
            }
        }
    }
}
