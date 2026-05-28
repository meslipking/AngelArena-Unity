using UnityEngine;
using AngelArena.Core;

namespace AngelArena.Core
{
    /// <summary>
    /// Lingering damage zone (Consecration, Briar Patch, etc).
    /// Deals damage per second to all enemies inside.
    /// </summary>
    public class DamageZone : MonoBehaviour
    {
        private float  _dmgPerSec;
        private float  _radius;
        private float  _duration;
        private DamageType _dmgType;
        private PlayerController _owner;
        private float  _elapsed;
        private float  _tickTimer;
        private const float TICK_INTERVAL = 0.3f; // Apply damage every 0.3s

        public void Init(float dmgPerSec, float radius, float duration,
                         DamageType type, PlayerController owner)
        {
            _dmgPerSec = dmgPerSec;
            _radius    = radius;
            _duration  = duration;
            _dmgType   = type;
            _owner     = owner;

            // Visual scale
            transform.localScale = Vector3.one * (radius / 50f);

            Destroy(gameObject, duration + 0.1f);
        }

        private void Update()
        {
            _elapsed   += Time.deltaTime;
            _tickTimer -= Time.deltaTime;

            if (_tickTimer <= 0)
            {
                _tickTimer = TICK_INTERVAL;
                ApplyDamage();
            }
        }

        private void ApplyDamage()
        {
            float dmg = _dmgPerSec * TICK_INTERVAL * (_owner?.DamageMult ?? 1f);
            var hits  = Physics2D.OverlapCircleAll(transform.position, _radius);

            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<EnemyController>();
                if (enemy == null || !enemy.IsAlive) continue;
                enemy.TakeDamage(dmg, _dmgType);
                _owner?.ApplyLifesteal(dmg);

                // Poison: apply slow
                if (_dmgType == DamageType.Poison)
                    enemy.ApplySlow(0.25f, 0.6f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
