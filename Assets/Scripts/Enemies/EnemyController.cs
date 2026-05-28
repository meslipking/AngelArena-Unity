using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngelArena.Core;
using AngelArena.Data;

namespace AngelArena.Core
{
    /// <summary>
    /// Enemy controller: HP, AI behavior, damage, death.
    /// Attach to Enemy prefab.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class EnemyController : MonoBehaviour
    {
        // ── Config ────────────────────────────────────────────────
        [HideInInspector] public EnemyData data;
        [HideInInspector] public float     hpMult  = 1f;
        [HideInInspector] public float     dmgMult = 1f;
        [HideInInspector] public bool      isElite;
        [HideInInspector] public bool      isBoss;

        // ── Runtime stats ─────────────────────────────────────────
        public float Hp    { get; private set; }
        public float MaxHp { get; private set; }
        public bool  IsAlive => Hp > 0;
        public int   Phase  { get; private set; } = 1;
        public string EnemyName => data ? data.enemyName : "Enemy";
        public bool  IsMoving => _rb != null && _rb.linearVelocity.sqrMagnitude > 0.01f;
        public Vector2 Velocity => _rb != null ? _rb.linearVelocity : Vector2.zero;

        // ── Status effects ────────────────────────────────────────
        private float _slowTimer;
        private float _slowAmount;   // 0-1 fraction reduction
        private float _markTimer;
        private float _markAmplify;  // e.g. 0.4 = 40% extra damage
        private bool  _marked;

        // ── Components ────────────────────────────────────────────
        private Rigidbody2D    _rb;
        private SpriteRenderer _sr;
        private Animator       _animator;
        private Transform      _playerTr;

        // ── AI state ──────────────────────────────────────────────
        private float _contactDmgTimer;
        private float _rangedTimer;
        private float _summonTimer;
        private Vector2 _flankOffset;

        // ── Flash ─────────────────────────────────────────────────
        private float _flashTimer;
        private Color _baseColor;

        // ─────────────────────────────────────────────────────────
        private void Awake()
        {
            _rb       = GetComponent<Rigidbody2D>();
            _sr       = GetComponentInChildren<SpriteRenderer>();
            _animator = GetComponentInChildren<Animator>();

            // Random flank offset
            _flankOffset = Random.insideUnitCircle.normalized * 80f;
        }

        public void Init(EnemyData d, float hp, float dmg, bool elite, bool boss)
        {
            data    = d;
            isElite = elite;
            isBoss  = boss;
            MaxHp   = hp;
            Hp      = hp;
            _baseColor = d ? d.color : Color.white;
            if (_sr) _sr.color = _baseColor;

            _playerTr = GameObject.FindWithTag("Player")?.transform;

            // Elite visual — gold tint instead of outline shader (avoids missing shader error)
            if (elite && !boss && _sr)
                _sr.color = new Color(1f, 0.85f, 0.2f); // gold tint
        }

        private void Update()
        {
            if (!IsAlive || !GameManager.Instance.GameRunning) return;

            // Update status effect timers
            if (_slowTimer  > 0) _slowTimer  -= Time.deltaTime;
            if (_markTimer  > 0) { _markTimer -= Time.deltaTime; if (_markTimer <= 0) _marked = false; }
            if (_flashTimer > 0)
            {
                _flashTimer -= Time.deltaTime;
                if (_sr) _sr.color = _flashTimer > 0 ? Color.white : _baseColor;
            }

            // Boss phase transitions
            if (isBoss) UpdateBossPhase();
        }

        private void FixedUpdate()
        {
            if (!IsAlive || _playerTr == null) return;
            MoveAI();
        }

        private void MoveAI()
        {
            if (data == null) return;
            float speed = data.moveSpeed * (1f - (_slowTimer > 0 ? _slowAmount : 0f));

            Vector2 targetPos;
            if (data.behavior == EnemyBehaviorType.Flank)
                targetPos = (Vector2)_playerTr.position + _flankOffset;
            else
                targetPos = _playerTr.position;

            Vector2 dir   = ((Vector2)targetPos - (Vector2)transform.position).normalized;
            _rb.linearVelocity = dir * speed;

            // Flip sprite
            if (_sr && dir.x != 0) _sr.flipX = dir.x < 0;
        }

        private void OnCollisionStay2D(Collision2D col)
        {
            if (!IsAlive) return;
            if (!col.gameObject.CompareTag("Player")) return;

            _contactDmgTimer -= Time.deltaTime;
            if (_contactDmgTimer <= 0)
            {
                _contactDmgTimer = data?.contactDamageCooldown ?? 1f;
                float dmg = (data?.baseDamage ?? 10f) * dmgMult * (isElite ? 1.5f : 1f);
                col.gameObject.GetComponent<PlayerController>()?.TakeDamage(dmg);
            }
        }

        // ── Status Effects ────────────────────────────────────────
        public void ApplySlow(float amount, float duration)
        {
            _slowAmount = Mathf.Max(_slowAmount, amount);
            _slowTimer  = Mathf.Max(_slowTimer, duration);
        }

        public void ApplyMark(float duration, float amplify)
        {
            _marked     = true;
            _markTimer  = duration;
            _markAmplify = amplify;
        }

        public void ApplyKnockback(Vector2 force)
            => _rb.AddForce(force, ForceMode2D.Impulse);

        // ── Damage ────────────────────────────────────────────────
        public void TakeDamage(float amount, DamageType type = DamageType.Physical)
        {
            if (!IsAlive) return;

            float finalDmg = _marked ? amount * (1f + _markAmplify) : amount;

            Hp -= finalDmg;
            _flashTimer = 0.1f;
            if (_sr) _sr.color = Color.white;

            // Floating damage number
            DamageNumbers.Spawn(transform.position, (int)finalDmg, type);

            if (Hp <= 0) Die();
        }

        private void Die()
        {
            Hp = 0;
            int xp   = Mathf.RoundToInt((data?.baseXp   ?? 5)  * (isElite ? 3 : 1) * (isBoss ? 20 : 1));
            int gold = Mathf.RoundToInt((data?.baseGold  ?? 1)  * (isElite ? 2 : 1) * (isBoss ? 15 : 1));

            GameManager.Instance.RegisterKill(xp, gold);

            // Death VFX
            SkillVFX.SpawnDeathBurst(transform.position, _baseColor);

            // Remove from spawner tracking
            EnemySpawner.Instance?.OnEnemyDied(this);

            Destroy(gameObject, 0.05f);
        }

        // ── Boss Phase ────────────────────────────────────────────
        private void UpdateBossPhase()
        {
            float pct = Hp / MaxHp;
            int   newPhase = pct > (data?.phase2HpThreshold ?? 0.6f) ? 1
                           : pct > (data?.phase3HpThreshold ?? 0.3f) ? 2 : 3;
            if (newPhase != Phase)
            {
                Phase = newPhase;
                OnBossPhaseChange(Phase);
            }
        }

        private void OnBossPhaseChange(int phase)
        {
            CameraController.Instance?.Shake(12f, 0.6f);
            // Speed boost per phase
            if (data != null) data.moveSpeed *= 1.25f;
            // TODO: trigger phase-specific attack patterns
        }
    }
}
