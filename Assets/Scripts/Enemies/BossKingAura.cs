using UnityEngine;

namespace AngelArena.Enemies
{
    /// <summary>
    /// Procedural Boss King Aura. Draws an elip rotating fire ring and spawns flame particles
    /// around any Boss character to make them look aggressive and intimidating.
    /// </summary>
    public class BossKingAura : MonoBehaviour
    {
        private float _baseRadius = 50f; // matches boss scaling
        private Transform _ringA;
        private Transform _ringB;
        private float _particleTimer;

        private void Start()
        {
            // Adjust radius based on collider or visual scale
            var col = GetComponent<CircleCollider2D>();
            if (col != null)
            {
                _baseRadius = col.radius * transform.localScale.x * 1.5f;
            }

            // Create two rotating elip rings with custom colors
            _ringA = CreateRingGO("AuraRing_Outer", new Color(1f, 0.3f, 0f, 0.4f), _baseRadius * 1.8f, _baseRadius * 0.9f);
            _ringB = CreateRingGO("AuraRing_Inner", new Color(1f, 0.8f, 0f, 0.5f), _baseRadius * 1.4f, _baseRadius * 0.7f);
        }

        private void Update()
        {
            float time = Time.time;

            // Animate Outer Ring (Rotate clockwise, pulsate elip ratio)
            if (_ringA != null)
            {
                _ringA.position = transform.position;
                _ringA.Rotate(0f, 0f, -40f * Time.deltaTime);
                float scalePulse = 1f + 0.08f * Mathf.Sin(time * 4f);
                _ringA.localScale = new Vector3(_baseRadius * 1.8f * scalePulse, _baseRadius * 0.9f * scalePulse, 1f);
            }

            // Animate Inner Ring (Rotate counter-clockwise, pulse out of phase)
            if (_ringB != null)
            {
                _ringB.position = transform.position;
                _ringB.Rotate(0f, 0f, 60f * Time.deltaTime);
                float scalePulse = 1f + 0.08f * Mathf.Cos(time * 5f);
                _ringB.localScale = new Vector3(_baseRadius * 1.4f * scalePulse, _baseRadius * 0.7f * scalePulse, 1f);
            }

            // Spawn floating fire embers/particles
            _particleTimer -= Time.deltaTime;
            if (_particleTimer <= 0)
            {
                _particleTimer = 0.12f; // spawn every 120ms
                SpawnEmber();
            }
        }

        private Transform CreateRingGO(string name, Color color, float width, float height)
        {
            var go = new GameObject(name);
            go.transform.position = transform.position;
            go.transform.localScale = new Vector3(width, height, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = MakeRingSprite(color);
            sr.sortingOrder = 3; // slightly above normal ground but below character body
            
            // Parent to this so it is destroyed when the boss dies
            go.transform.SetParent(transform);
            return go.transform;
        }

        private void SpawnEmber()
        {
            // Spawn random fire ember around the base radius
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(angle) * _baseRadius * 1.2f, Mathf.Sin(angle) * _baseRadius * 0.6f);
            Vector3 spawnPos = transform.position + (Vector3)offset;

            var ember = new GameObject("Ember");
            ember.transform.position = spawnPos;
            ember.transform.localScale = Vector3.one * Random.Range(6f, 12f);

            var sr = ember.AddComponent<SpriteRenderer>();
            Color col = Random.value > 0.5f ? new Color(1f, 0.4f, 0f, 0.8f) : new Color(1f, 0.7f, 0.2f, 0.8f);
            sr.sprite = MakeCircleSprite(col);
            sr.sortingOrder = 11; // float in front of boss

            var anim = ember.AddComponent<EmberAnim>();
            anim.Init(Random.Range(0.6f, 1.2f), col);

            // Parent to map or keep independent so it floats properly if boss moves
            if (transform.parent != null) ember.transform.SetParent(transform.parent);
        }

        private Sprite MakeRingSprite(Color color)
        {
            int sz = 64;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            float c = sz / 2f;
            float rOuter = sz / 2f - 2f;
            float rInner = sz / 2f - 6f; // thickness

            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), Vector2.one * c);
                    if (d < rOuter && d > rInner)
                    {
                        // anti-alias edges a bit
                        float alpha = 1f;
                        if (d > rOuter - 1f) alpha = (rOuter - d);
                        else if (d < rInner + 1f) alpha = (d - rInner);
                        tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, (float)sz);
        }

        private Sprite MakeCircleSprite(Color color)
        {
            int sz = 16;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            float c = sz / 2f;
            for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                    tex.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), Vector2.one * c) < c - 1 ? color : Color.clear);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, (float)sz);
        }
    }

    public class EmberAnim : MonoBehaviour
    {
        private float _life, _elapsed;
        private Color _color;
        private SpriteRenderer _sr;
        private Vector3 _velocity;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _velocity = new Vector3(Random.Range(-15f, 15f), Random.Range(35f, 75f), 0f);
        }

        public void Init(float life, Color col)
        {
            _life = life;
            _color = col;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = _elapsed / _life;

            // Float upward and expand/shrink
            transform.position += _velocity * Time.deltaTime;
            transform.localScale = Vector3.one * Mathf.Lerp(8f, 1f, t);

            if (_sr)
            {
                _sr.color = new Color(_color.r, _color.g, _color.b, Mathf.Lerp(0.8f, 0f, t));
            }

            if (_elapsed >= _life)
            {
                Destroy(gameObject);
            }
        }
    }
}
