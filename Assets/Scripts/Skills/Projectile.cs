using UnityEngine;
using AngelArena.Core;

namespace AngelArena.Core
{
    /// <summary>
    /// Projectile behavior: moves in a direction, deals damage on hit, handles pierce and AoE.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        // ── Config (set via Init) ──────────────────────────────────
        private Vector2 _direction;
        private float   _speed;
        private float   _damage;
        private float   _maxRange;
        private float   _aoeMult;
        private float   _lifesteal;
        private bool    _piercing;
        private PlayerController _owner;

        public float AoeRadius { get; set; } = 0f;

        // ── Runtime ───────────────────────────────────────────────
        private Vector2 _startPos;
        private Rigidbody2D _rb;

        // ─────────────────────────────────────────────────────────
        private void Awake() => _rb = GetComponent<Rigidbody2D>();

        public void Init(Vector2 dir, float speed, float damage, float maxRange,
                         float aoeMult, float lifesteal, bool piercing, PlayerController owner)
        {
            _direction = dir.normalized;
            _speed     = speed;
            _damage    = damage;
            _maxRange  = maxRange;
            _aoeMult   = aoeMult;
            _lifesteal = lifesteal;
            _piercing  = piercing;
            _owner     = owner;
            _startPos  = transform.position;

            // Rotate sprite to face direction
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            // Auto-destroy if no impact
            Destroy(gameObject, maxRange / speed + 0.5f);
        }

        private void FixedUpdate()
        {
            if (_rb) _rb.linearVelocity = _direction * _speed;

            // Check range
            if (Vector2.Distance(_startPos, transform.position) >= _maxRange)
                DestroyProjectile();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Enemy")) return;

            var enemy = other.GetComponent<EnemyController>();
            if (enemy == null || !enemy.IsAlive) return;

            if (AoeRadius > 0)
            {
                // AoE explosion
                var hits = Physics2D.OverlapCircleAll(transform.position, AoeRadius * _aoeMult);
                foreach (var hit in hits)
                {
                    var e2 = hit.GetComponent<EnemyController>();
                    if (e2 != null && e2.IsAlive)
                    {
                        e2.TakeDamage(_damage);
                        _owner?.ApplyLifesteal(_damage);
                    }
                }
                SkillVFX.SpawnAoe(transform.position, AoeRadius * _aoeMult, Color.red, 0.3f);
                DestroyProjectile();
            }
            else
            {
                enemy.TakeDamage(_damage);
                _owner?.ApplyLifesteal(_damage);
                if (!_piercing) DestroyProjectile();
            }
        }

        private void DestroyProjectile() => Destroy(gameObject);
    }
}
