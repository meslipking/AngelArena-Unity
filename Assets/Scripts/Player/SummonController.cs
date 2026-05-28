using UnityEngine;
using AngelArena.Core;

namespace AngelArena.Core
{
    /// <summary>
    /// Summon controller: skeleton/wolf/bear summoned by player skills.
    /// Follows player when no enemies nearby, attacks enemies when in range.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class SummonController : MonoBehaviour
    {
        // ── Config ────────────────────────────────────────────────
        private PlayerController _owner;
        private SkillSystem      _skillSystem;
        private float _hp, _maxHp, _damage, _moveSpeed;

        // ── Runtime ───────────────────────────────────────────────
        private Rigidbody2D    _rb;
        private SpriteRenderer _sr;
        private EnemyController _targetEnemy;
        private float _attackTimer;

        private const float ATTACK_RANGE    = 60f;
        private const float ATTACK_INTERVAL = 1.2f;
        private const float FOLLOW_DIST     = 220f; // Distance to start following player

        // ─────────────────────────────────────────────────────────
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _sr = GetComponentInChildren<SpriteRenderer>();
        }

        public void Init(PlayerController owner, float hp, float damage, float speed, SkillSystem sys)
        {
            _owner      = owner;
            _skillSystem = sys;
            _hp         = hp;
            _maxHp      = hp;
            _damage     = damage;
            _moveSpeed  = speed;
        }

        private void Update()
        {
            if (!GameManager.Instance.GameRunning) return;

            _attackTimer -= Time.deltaTime;

            // Find nearest enemy
            _targetEnemy = FindTarget();

            // Attack if in range
            if (_targetEnemy != null)
            {
                float dist = Vector2.Distance(transform.position, _targetEnemy.transform.position);
                if (dist <= ATTACK_RANGE && _attackTimer <= 0)
                {
                    _attackTimer = ATTACK_INTERVAL;
                    _targetEnemy.TakeDamage(_damage);
                }
            }
        }

        private void FixedUpdate()
        {
            if (_owner == null) { Destroy(gameObject); return; }

            Vector2 moveTarget;
            float   speed = _moveSpeed;

            if (_targetEnemy != null && _targetEnemy.IsAlive)
            {
                // Chase enemy
                moveTarget = _targetEnemy.transform.position;
                speed = _moveSpeed * 1.2f;
            }
            else
            {
                // Follow player if too far
                float distToPlayer = Vector2.Distance(transform.position, _owner.transform.position);
                if (distToPlayer > FOLLOW_DIST)
                    moveTarget = _owner.transform.position;
                else
                    moveTarget = transform.position; // idle in place
            }

            Vector2 dir = ((Vector2)transform.position == (Vector2)moveTarget)
                ? Vector2.zero
                : ((Vector2)moveTarget - (Vector2)transform.position).normalized;

            _rb.linearVelocity = dir * speed;

            // Flip sprite
            if (_sr && dir.x != 0) _sr.flipX = dir.x < 0;
        }

        private EnemyController FindTarget()
        {
            EnemyController best = null;
            float minDist = 300f; // Summon aggro range
            foreach (var e in EnemySpawner.AllEnemies)
            {
                if (e == null || !e.IsAlive) continue;
                float d = Vector2.Distance(transform.position, e.transform.position);
                if (d < minDist) { minDist = d; best = e; }
            }
            return best;
        }

        public void TakeDamage(float amount)
        {
            _hp -= amount;
            if (_hp <= 0) Destroy(gameObject);
        }
    }
}
