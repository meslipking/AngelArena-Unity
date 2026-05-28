using UnityEngine;
using AngelArena.Core;
using AngelArena.Audio;

namespace AngelArena.Skills
{
    public class VacuumItem : MonoBehaviour
    {
        private Transform      _player;
        private SpriteRenderer _sr;
        private float          _pulseTimer;
        private float          _size = 14f;

        private void Start()
        {
            var pGO = GameObject.FindWithTag("Player");
            if (pGO != null) _player = pGO.transform;

            _sr = gameObject.GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();

            _sr.sprite = Graphics.SpriteFactory.GetOrCreate("vacuum_item_magnet", () => GenerateMagnetTexture());
            _sr.sortingOrder = -3;
            transform.localScale = Vector3.one * _size;

            _pulseTimer = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            if (!GameManager.Instance.GameRunning || _player == null) return;

            // Pulse bounce visual
            _pulseTimer += Time.deltaTime * 5f;
            float scale = 1f + 0.18f * Mathf.Sin(_pulseTimer);
            transform.localScale = Vector3.one * (_size * scale);

            // Collide with player check
            float dist = Vector2.Distance(transform.position, _player.position);
            if (dist < 15f) // Close enough to collect
            {
                Collect();
            }
        }

        private void Collect()
        {
            // Activate screen vacuum
            EnemySpawner.Instance?.TriggerVacuum(8f);

            // Floating text
            VFXSystem.SpawnFloatText(transform.position + Vector3.up * 18f, "🧲 MAGNET VACUUM!", new Color(0.9f, 0.2f, 0.2f), true);

            // Sound
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxLevelUp, 0.8f);
            }

            Destroy(gameObject);
        }

        private Sprite GenerateMagnetTexture()
        {
            int sz = 32;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            float c = sz / 2f;
            float r = sz * 0.42f;

            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float dx = x - c;
                float dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                if (d > r)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                // Draw U-shape Magnet
                // Left prong, right prong and bottom curve
                bool isMagnetU = (dx * dx + dy * dy > r * r * 0.18f) && 
                                 (dy <= r * 0.2f || Mathf.Abs(dx) > r * 0.3f) &&
                                 (dy > -r * 0.8f);

                if (isMagnetU)
                {
                    // Bottom curve and tips color
                    if (dy > r * 0.2f)
                    {
                        tex.SetPixel(x, y, Color.white); // Silver tips
                    }
                    else
                    {
                        tex.SetPixel(x, y, new Color(0.85f, 0.15f, 0.15f)); // Red horseshoe
                    }
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }

            // Outline
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                if (tex.GetPixel(x, y) != Color.clear)
                {
                    // Check if adjacent is clear to draw outline
                    bool edge = false;
                    for (int ny = -1; ny <= 1; ny++)
                    for (int nx = -1; nx <= 1; nx++)
                    {
                        int px = x + nx, py = y + ny;
                        if (px >= 0 && px < sz && py >= 0 && py < sz)
                        {
                            if (tex.GetPixel(px, py) == Color.clear) edge = true;
                        }
                    }
                    if (edge) tex.SetPixel(x, y, new Color(0.2f, 0.05f, 0.05f));
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, sz);
        }
    }
}
