using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngelArena.Core;
using AngelArena.Graphics;

namespace AngelArena.Core
{
    /// <summary>
    /// All 30+ skill implementations as static methods.
    /// Each skill receives the SkillSystem (for player ref + helpers) and the current level.
    /// </summary>
    public static class SkillHandlers
    {
        private static PlayerController P(SkillSystem sys) => sys.player;

        // ════════════════════════════════════════════════════════
        // ASSASSIN SKILLS
        // ════════════════════════════════════════════════════════

        public static void ShadowBlades(SkillSystem sys, int level)
        {
            // Fire 4 daggers in cardinal directions, pierce through enemies
            var player = P(sys);
            float dmg    = (20f + level * 12f) * player.DamageMult;
            float range  = (250f + level * 30f) * player.AoeMult;
            Vector2[] dirs = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

            foreach (var dir in dirs)
                sys.SpawnProjectile(SkillPrefabs.Get("shadow_blade_proj"),
                    player.transform.position, dir, 380f, dmg, range, piercing: true);
        }

        public static void VoidPulse(SkillSystem sys, int level)
        {
            // AoE nova around player
            float dmg    = (55f + level * 15f) * P(sys).DamageMult;
            float radius = (100f + level * 18f) * P(sys).AoeMult;
            Vector2 center = P(sys).transform.position;

            var hits = sys.FindEnemiesInRadius(center, radius);
            foreach (var e in hits)
            {
                e.TakeDamage(dmg, DamageType.Shadow);
                P(sys).ApplyLifesteal(dmg);
            }

            // VFX
            SkillVFX.SpawnAoe(center, radius, Color.magenta, 0.4f);
        }

        public static void PhantomDash(SkillSystem sys, int level)
        {
            // Dash through enemies, damaging all in path
            float dmg    = (35f + level * 10f) * P(sys).DamageMult;
            var nearest  = sys.FindNearestEnemy(400f);
            if (nearest == null) return;

            Vector2 dir  = (nearest.transform.position - P(sys).transform.position).normalized;
            P(sys).GetComponent<Rigidbody2D>().AddForce(dir * 900f, ForceMode2D.Impulse);

            // Trail VFX
            SkillVFX.SpawnDashTrail(P(sys).transform.position, dir, Color.cyan, 0.3f);

            // Hit enemies along dash path
            var hits = sys.FindEnemiesInRadius(P(sys).transform.position, 60f * P(sys).AoeMult);
            foreach (var e in hits) e.TakeDamage(dmg, DamageType.Physical);
        }

        public static void DarkMark(SkillSystem sys, int level)
        {
            // Mark nearest enemy — all damage to it is amplified 40%+level*5% for 5s
            var target = sys.FindNearestEnemy(500f);
            if (target == null) return;
            target.ApplyMark(5f + level * 0.5f, 0.4f + level * 0.05f);
            SkillVFX.SpawnOnTarget(target.transform, "dark_mark", Color.purple, 5f + level * 0.5f);
        }

        // ════════════════════════════════════════════════════════
        // MAGE SKILLS
        // ════════════════════════════════════════════════════════

        public static void MagicMissile(SkillSystem sys, int level)
        {
            var target = sys.FindNearestEnemy(600f);
            if (target == null) return;

            float dmg = (32f + level * 10f) * P(sys).DamageMult;
            Vector2 dir = (target.transform.position - P(sys).transform.position).normalized;
            sys.SpawnProjectile(SkillPrefabs.Get("magic_missile_proj"),
                P(sys).transform.position, dir, 420f, dmg, 600f);
        }

        public static void FrostAura(SkillSystem sys, int level)
        {
            float dmg    = (18f + level * 6f)   * P(sys).DamageMult;
            float radius = (120f + level * 20f) * P(sys).AoeMult;
            float slow   = 0.4f + level * 0.03f; // 40% + 3%/level

            var hits = sys.FindEnemiesInRadius(P(sys).transform.position, radius);
            foreach (var e in hits)
            {
                e.TakeDamage(dmg, DamageType.Ice);
                e.ApplySlow(slow, 2.5f); // 2.5s duration
            }

            SkillVFX.SpawnAoe(P(sys).transform.position, radius, new Color(0.6f, 0.8f, 1f), 0.5f);
        }

        public static void ThunderRing(SkillSystem sys, int level)
        {
            float dmg = (40f + level * 14f) * P(sys).DamageMult;
            int bolts = 8 + level;

            for (int i = 0; i < bolts; i++)
            {
                float angle = (360f / bolts) * i * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                sys.SpawnProjectile(SkillPrefabs.Get("thunder_bolt_proj"),
                    P(sys).transform.position, dir, 350f, dmg,
                    (280f + level * 20f) * P(sys).AoeMult, piercing: true);
            }
        }

        public static void Meteor(SkillSystem sys, int level)
        {
            float dmg    = (95f + level * 28f) * P(sys).DamageMult;
            float radius = (80f + level * 15f) * P(sys).AoeMult;

            // Random position in view
            Vector2 center = (Vector2)P(sys).transform.position +
                             Random.insideUnitCircle * 300f;

            // Delay then AoE
            sys.StartCoroutine(MeteorDelay(sys, center, radius, dmg));
        }

        private static IEnumerator MeteorDelay(SkillSystem sys, Vector2 pos, float radius, float dmg)
        {
            SkillVFX.SpawnWarningCircle(pos, radius, 1.2f);
            yield return new WaitForSeconds(1.2f);

            var hits = sys.FindEnemiesInRadius(pos, radius);
            foreach (var e in hits) e.TakeDamage(dmg, DamageType.Fire);
            SkillVFX.SpawnAoe(pos, radius, new Color(1f, 0.4f, 0f), 0.6f);
            CameraController.Instance?.Shake(8f, 0.3f);
        }

        public static void Fireball(SkillSystem sys, int level)
        {
            var target = sys.FindNearestEnemy(550f);
            if (target == null) return;

            float dmg    = (52f + level * 16f) * P(sys).DamageMult;
            float radius = (70f + level * 12f) * P(sys).AoeMult;
            Vector2 dir  = (target.transform.position - P(sys).transform.position).normalized;

            var proj = sys.SpawnProjectile(SkillPrefabs.Get("fireball_proj"),
                P(sys).transform.position, dir, 280f, dmg, 550f);
            if (proj) proj.AoeRadius = radius;
        }

        // ════════════════════════════════════════════════════════
        // RANGER SKILLS
        // ════════════════════════════════════════════════════════

        public static void PiercingArrow(SkillSystem sys, int level)
        {
            var target = sys.FindNearestEnemy(700f);
            if (target == null) return;

            float dmg = (26f + level * 9f) * P(sys).DamageMult;
            Vector2 dir = (target.transform.position - P(sys).transform.position).normalized;
            sys.SpawnProjectile(SkillPrefabs.Get("arrow_proj"),
                P(sys).transform.position, dir, 500f, dmg,
                (700f + level * 50f) * P(sys).AoeMult, piercing: true);
        }

        public static void ArrowRain(SkillSystem sys, int level)
        {
            var target = sys.FindNearestEnemy(700f);
            Vector2 center = target != null
                ? (Vector2)target.transform.position
                : (Vector2)P(sys).transform.position + Vector2.up * 200f;

            int arrowCount = 8 + level * 2;
            float radius   = (100f + level * 20f) * P(sys).AoeMult;
            float dmg      = (20f + level * 6f)   * P(sys).DamageMult;

            sys.StartCoroutine(ArrowRainCoroutine(sys, center, radius, arrowCount, dmg));
        }

        private static IEnumerator ArrowRainCoroutine(SkillSystem sys, Vector2 center,
                                                        float radius, int count, float dmg)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 pos = center + Random.insideUnitCircle * radius;
                SkillVFX.SpawnWarningCircle(pos, 25f, 0.2f);
                yield return new WaitForSeconds(0.08f);
                var hits = sys.FindEnemiesInRadius(pos, 25f * P(sys).AoeMult);
                foreach (var e in hits) e.TakeDamage(dmg, DamageType.Physical);
                SkillVFX.SpawnImpact(pos, Color.green, 0.2f);
            }
        }

        public static void SpiritWolf(SkillSystem sys, int level)
        {
            var target = sys.FindNearestEnemy(800f);
            if (target == null) return;

            float dmg = (60f + level * 20f) * P(sys).DamageMult;
            Vector2 dir = (target.transform.position - P(sys).transform.position).normalized;
            var proj = sys.SpawnProjectile(SkillPrefabs.Get("spirit_wolf_proj"),
                P(sys).transform.position, dir, 320f, dmg, 800f);
            if (proj) proj.AoeRadius = 50f * P(sys).AoeMult;
        }

        // ════════════════════════════════════════════════════════
        // PALADIN SKILLS
        // ════════════════════════════════════════════════════════

        public static void HolyLight(SkillSystem sys, int level)
        {
            float healAmt = (P(sys).MaxHp * 0.06f * level);
            P(sys).TakeDamage(-healAmt);

            float dmg    = (35f + level * 10f) * P(sys).DamageMult;
            float radius = (120f + level * 15f) * P(sys).AoeMult;
            var hits = sys.FindEnemiesInRadius(P(sys).transform.position, radius);
            foreach (var e in hits) e.TakeDamage(dmg, DamageType.Holy);
            SkillVFX.SpawnAoe(P(sys).transform.position, radius, Color.yellow, 0.5f);
        }

        public static void DivineShield(SkillSystem sys, int level)
        {
            P(sys).StartCoroutine(DivineShieldCoroutine(sys, level));
        }

        private static IEnumerator DivineShieldCoroutine(SkillSystem sys, int level)
        {
            var player = P(sys);
            // Temp invulnerability
            float duration = 1.5f + level * 0.2f;
            player.GetComponent<PlayerController>(); // get iframes via dedicated method
            SkillVFX.SpawnShieldEffect(player.transform, duration);

            // Store normal def
            yield return new WaitForSeconds(duration);

            // Burst explosion
            float dmg    = (60f + level * 15f) * player.DamageMult;
            float radius = (130f + level * 20f) * player.AoeMult;
            var hits = sys.FindEnemiesInRadius(player.transform.position, radius);
            foreach (var e in hits) e.TakeDamage(dmg, DamageType.Holy);
            SkillVFX.SpawnAoe(player.transform.position, radius, Color.yellow, 0.6f);
            CameraController.Instance?.Shake(6f, 0.3f);
        }

        public static void Consecration(SkillSystem sys, int level)
        {
            // Spawn a lingering holy zone at player's feet
            float dmgPerSec = (22f + level * 7f) * P(sys).DamageMult;
            float radius    = (90f + level * 12f) * P(sys).AoeMult;
            float duration  = 4f + level * 0.5f;

            var zone = GameObject.Instantiate(SkillPrefabs.Get("consecration_zone"),
                P(sys).transform.position, Quaternion.identity);
            zone.GetComponent<DamageZone>()?.Init(dmgPerSec, radius, duration, DamageType.Holy, P(sys));
        }

        // ════════════════════════════════════════════════════════
        // NECROMANCER SKILLS
        // ════════════════════════════════════════════════════════

        public static void SummonSkeleton(SkillSystem sys, int level)
        {
            int maxSummons = level <= 2 ? 4 : level <= 5 ? 6 : 8;
            if (sys.Summons.Count >= maxSummons) return;

            float hp  = 80f + level * 25f;
            float dmg = 15f + level * 8f;
            var pos   = (Vector2)P(sys).transform.position + Random.insideUnitCircle * 60f;

            var go    = GameObject.Instantiate(SkillPrefabs.Get("skeleton_prefab"), pos, Quaternion.identity);
            var summon = go.GetComponent<SummonController>();
            if (summon != null)
            {
                summon.Init(P(sys), hp, dmg, 90f, sys);
                sys.Summons.Add(summon);
            }
            SkillVFX.SpawnSummonEffect(pos, Color.gray);
        }

        public static void DeathGrasp(SkillSystem sys, int level)
        {
            float dmg    = (42f + level * 12f) * P(sys).DamageMult;
            float radius = (80f + level * 10f) * P(sys).AoeMult;

            var hits = sys.FindEnemiesInRadius(P(sys).transform.position, radius);
            foreach (var e in hits)
            {
                e.TakeDamage(dmg, DamageType.Shadow);
                e.ApplySlow(0.7f, 2f); // 70% slow for 2s (roots)
            }
            SkillVFX.SpawnAoe(P(sys).transform.position, radius, new Color(0.2f, 0.1f, 0.4f), 0.5f);
        }

        public static void SoulDrain(SkillSystem sys, int level)
        {
            float drainRange = 130f;
            float dmg        = (25f + level * 8f) * P(sys).DamageMult;
            float healPct    = 0.2f;

            var hits = sys.FindEnemiesInRadius(P(sys).transform.position, drainRange);
            float totalHeal = 0;
            foreach (var e in hits)
            {
                e.TakeDamage(dmg, DamageType.Shadow);
                totalHeal += dmg * healPct;
            }
            if (totalHeal > 0) P(sys).TakeDamage(-totalHeal);
            SkillVFX.SpawnDrainEffect(P(sys).transform.position, drainRange, Color.black, 0.4f);
        }

        // ════════════════════════════════════════════════════════
        // FIGHTER SKILLS
        // ════════════════════════════════════════════════════════

        public static void BattleCry(SkillSystem sys, int level)
        {
            float radius = (180f + level * 20f) * P(sys).AoeMult;
            float dmg    = (45f + level * 12f)  * P(sys).DamageMult;

            var hits = sys.FindEnemiesInRadius(P(sys).transform.position, radius);
            foreach (var e in hits)
            {
                e.TakeDamage(dmg, DamageType.Physical);
                e.ApplyKnockback((P(sys).transform.position - e.transform.position).normalized * -350f);
            }
            CameraController.Instance?.Shake(10f, 0.4f);
            SkillVFX.SpawnAoe(P(sys).transform.position, radius, Color.red, 0.4f);
        }

        public static void IronWall(SkillSystem sys, int level)
        {
            // Temp defense boost
            sys.StartCoroutine(IronWallCoroutine(P(sys), level));
        }

        private static IEnumerator IronWallCoroutine(PlayerController player, int level)
        {
            float duration = 3f + level * 0.5f;
            // Boost def temporarily via shield absorption
            float shield = (50f + level * 30f) * player.DamageMult;
            SkillVFX.SpawnShieldAbsorb(player.transform, shield, duration);
            yield return new WaitForSeconds(duration);
        }

        public static void Earthquake(SkillSystem sys, int level)
        {
            float dmg    = (55f + level * 18f) * P(sys).DamageMult;
            float radius = (160f + level * 25f) * P(sys).AoeMult;

            var hits = sys.FindEnemiesInRadius(P(sys).transform.position, radius);
            foreach (var e in hits) e.TakeDamage(dmg, DamageType.Physical);
            CameraController.Instance?.Shake(15f, 0.6f);
            SkillVFX.SpawnAoe(P(sys).transform.position, radius, Color.yellow, 0.7f);
        }

        // ════════════════════════════════════════════════════════
        // DRUID SKILLS
        // ════════════════════════════════════════════════════════

        public static void BriarPatch(SkillSystem sys, int level)
        {
            Vector2 pos = (Vector2)P(sys).transform.position + (Vector2)(P(sys).transform.up * 60f);
            float dmgPerSec = (30f + level * 8f) * P(sys).DamageMult;
            float radius    = (80f + level * 12f)* P(sys).AoeMult;
            float duration  = 5f + level * 0.5f;

            var zone = GameObject.Instantiate(SkillPrefabs.Get("briar_zone"),
                (Vector3)pos, Quaternion.identity);
            zone.GetComponent<DamageZone>()?.Init(dmgPerSec, radius, duration, DamageType.Poison, P(sys));
        }

        public static void NatureRegrowth(SkillSystem sys, int level)
        {
            float healPerSec = (P(sys).MaxHp * 0.03f + level * 2f);
            float duration   = 5f + level * 0.5f;
            P(sys).StartCoroutine(RegenCoroutine(P(sys), healPerSec, duration));
            SkillVFX.SpawnHealEffect(P(sys).transform, duration);
        }

        private static IEnumerator RegenCoroutine(PlayerController player, float hps, float duration)
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                player.TakeDamage(-hps * Time.deltaTime);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        public static void CallOfWild(SkillSystem sys, int level)
        {
            // Summon a spirit bear that chases enemies
            float dmg = (50f + level * 15f) * P(sys).DamageMult;
            var pos   = (Vector2)P(sys).transform.position + Random.insideUnitCircle * 50f;
            var go    = GameObject.Instantiate(SkillPrefabs.Get("spirit_bear_prefab"), (Vector3)pos, Quaternion.identity);
            var summon = go.GetComponent<SummonController>();
            if (summon == null) summon = go.AddComponent<SummonController>();
            summon.Init(P(sys), 200f + level * 40f, dmg, 110f, sys);
            sys.Summons.Add(summon);
        }

        // ════════════════════════════════════════════════════════
        // MISSING BASE SKILLS
        // ════════════════════════════════════════════════════════

        public static void PoisonDart(SkillSystem sys, int level)
        {
            var player = P(sys);
            float dmg = (18f + level * 6f) * player.DamageMult;
            var target = sys.FindNearestEnemy(500f);
            Vector2 baseDir = target != null ? (Vector2)(target.transform.position - player.transform.position).normalized : Vector2.up;

            float[] offsets = { -15f, 0f, 15f };
            foreach (var offset in offsets)
            {
                float rad = offset * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(
                    baseDir.x * Mathf.Cos(rad) - baseDir.y * Mathf.Sin(rad),
                    baseDir.x * Mathf.Sin(rad) + baseDir.y * Mathf.Cos(rad)
                ).normalized;
                var proj = sys.SpawnProjectile(SkillPrefabs.Get("poison_dart_proj"), player.transform.position, dir, 450f, dmg, 500f);
                if (proj != null)
                {
                    proj.gameObject.AddComponent<PoisonEffect>().Init(5f + level * 3f, 4f);
                }
            }
        }

        public static void ShieldBash(SkillSystem sys, int level)
        {
            var player = P(sys);
            float dmg = (35f + level * 10f) * player.DamageMult;
            var target = sys.FindNearestEnemy(250f);
            Vector2 dir = target != null ? (Vector2)(target.transform.position - player.transform.position).normalized : Vector2.up;

            Vector2 bashCenter = (Vector2)player.transform.position + dir * 45f;
            var hits = sys.FindEnemiesInRadius(bashCenter, 65f * player.AoeMult);
            foreach (var e in hits)
            {
                e.TakeDamage(dmg, DamageType.Physical);
                e.ApplyKnockback(dir * 400f);
                e.ApplySlow(0.5f, 1.5f + level * 0.2f);
            }
            SkillVFX.SpawnImpact(bashCenter, Color.gray, 0.2f);
        }

        public static void Rally(SkillSystem sys, int level)
        {
            var player = P(sys);
            float speedBoost = 0.15f + level * 0.03f;
            player.StartCoroutine(RallyCoroutine(player, speedBoost, 5f));
            SkillVFX.SpawnHealEffect(player.transform, 5f);
        }

        private static IEnumerator RallyCoroutine(PlayerController player, float boost, float duration)
        {
            float originalSpeed = player.MoveSpeed;
            player.MoveSpeed += originalSpeed * boost;
            yield return new WaitForSeconds(duration);
            if (player != null)
            {
                player.MoveSpeed = Mathf.Max(originalSpeed, player.MoveSpeed - originalSpeed * boost);
            }
        }

        public static void VineWhip(SkillSystem sys, int level)
        {
            var player = P(sys);
            float dmg = (30f + level * 10f) * player.DamageMult;
            var target = sys.FindNearestEnemy(300f);
            Vector2 dir = target != null ? (Vector2)(target.transform.position - player.transform.position).normalized : Vector2.up;

            Vector2 pos = (Vector2)player.transform.position + dir * 60f;
            var hits = sys.FindEnemiesInRadius(pos, 80f * player.AoeMult);
            foreach (var e in hits)
            {
                e.TakeDamage(dmg, DamageType.Poison);
                e.ApplyKnockback(dir * 250f);
            }
            SkillVFX.SpawnImpact(pos, Color.green, 0.25f);
        }

        public static void ShadowClone(SkillSystem sys, int level)
        {
            var player = P(sys);
            float hp = 100f + level * 30f;
            float dmg = (20f + level * 8f) * player.DamageMult;
            var pos = (Vector2)player.transform.position + Random.insideUnitCircle * 55f;

            var go = GameObject.Instantiate(SkillPrefabs.Get("skeleton_prefab"), pos, Quaternion.identity);
            var summon = go.GetComponent<SummonController>();
            if (summon == null) summon = go.AddComponent<SummonController>();
            summon.Init(player, hp, dmg, 120f, sys);
            
            // Re-style visual child as a shadow
            var vis = go.GetComponent<CharacterVisuals>();
            if (vis == null) vis = go.AddComponent<CharacterVisuals>();
            vis.walkSpeed = 14f;

            sys.Summons.Add(summon);
            SkillVFX.SpawnSummonEffect(pos, Color.magenta);
        }

        // ════════════════════════════════════════════════════════
        // EVOLVED ACTIVE SKILLS
        // ════════════════════════════════════════════════════════

        public static void SpellStorm(SkillSystem sys, int level)
        {
            sys.StartCoroutine(SpellStormCoroutine(sys, level));
        }

        private static IEnumerator SpellStormCoroutine(SkillSystem sys, int level)
        {
            var player = P(sys);
            float dmg = 150f * player.DamageMult;
            for (int step = 0; step < 12; step++)
            {
                if (player == null) yield break;
                float baseAngle = step * 15f * Mathf.Deg2Rad;
                for (int i = 0; i < 4; i++)
                {
                    float angle = baseAngle + (360f / 4f) * i * Mathf.Deg2Rad;
                    Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    sys.SpawnProjectile(SkillPrefabs.Get("magic_missile_proj"), player.transform.position, dir, 520f, dmg, 500f);
                }
                yield return new WaitForSeconds(0.15f);
            }
        }

        public static void WindRunner(SkillSystem sys, int level)
        {
            var player = P(sys);
            float dmg = 180f * player.DamageMult;
            var target = sys.FindNearestEnemy(600f);
            Vector2 baseDir = target != null ? (Vector2)(target.transform.position - player.transform.position).normalized : Vector2.up;

            float[] angles = { -20f, 0f, 20f };
            foreach (var offset in angles)
            {
                float rad = offset * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(
                    baseDir.x * Mathf.Cos(rad) - baseDir.y * Mathf.Sin(rad),
                    baseDir.x * Mathf.Sin(rad) + baseDir.y * Mathf.Cos(rad)
                ).normalized;
                var proj = sys.SpawnProjectile(SkillPrefabs.Get("wind_runner_proj"), player.transform.position, dir, 250f, dmg, 600f);
                if (proj != null)
                {
                    proj.AoeRadius = 60f * player.AoeMult;
                    proj.gameObject.AddComponent<TornadoEffect>().Init(180f);
                }
            }
        }

        public static void VoidReaver(SkillSystem sys, int level)
        {
            var player = P(sys);
            float dmg = 200f * player.DamageMult;
            float radius = 110f * player.AoeMult;

            for (int i = 0; i < 6; i++)
            {
                var projGO = GameObject.Instantiate(SkillPrefabs.Get("void_reaver_proj"), player.transform.position, Quaternion.identity);
                var proj = projGO.GetComponent<Projectile>();
                if (proj == null) proj = projGO.AddComponent<Projectile>();
                proj.Init(Vector2.zero, 0f, dmg, 9999f, player.AoeMult, player.Lifesteal, true, player);

                var orbit = projGO.AddComponent<OrbitingProjectile>();
                orbit.center = player.transform;
                orbit.radius = radius;
                orbit.speed = 220f;
                orbit.angleOffset = (360f / 6f) * i * Mathf.Deg2Rad;

                GameObject.Destroy(projGO, 4f);
            }
        }

        public static void AegisSmash(SkillSystem sys, int level)
        {
            var player = P(sys);
            float dmg = 220f * player.DamageMult;
            float radius = 180f * player.AoeMult;
            var hits = sys.FindEnemiesInRadius(player.transform.position, radius);
            foreach (var e in hits)
            {
                e.TakeDamage(dmg, DamageType.Physical);
                e.ApplyKnockback((e.transform.position - player.transform.position).normalized * 500f);
            }
            SkillVFX.SpawnAoe(player.transform.position, radius, Color.yellow, 0.5f);
        }

        public static void HellfireMeteor(SkillSystem sys, int level)
        {
            sys.StartCoroutine(HellfireMeteorCoroutine(sys, level));
        }

        private static IEnumerator HellfireMeteorCoroutine(SkillSystem sys, int level)
        {
            var player = P(sys);
            float dmg = 300f * player.DamageMult;
            float radius = 140f * player.AoeMult;
            for (int i = 0; i < 3; i++)
            {
                if (player == null) yield break;
                Vector2 pos = (Vector2)player.transform.position + Random.insideUnitCircle * 250f;
                SkillVFX.SpawnWarningCircle(pos, radius, 0.8f);
                yield return new WaitForSeconds(0.8f);
                var hits = sys.FindEnemiesInRadius(pos, radius);
                foreach (var e in hits) e.TakeDamage(dmg, DamageType.Fire);
                SkillVFX.SpawnAoe(pos, radius, new Color(1f, 0.2f, 0f), 0.6f);
                CameraController.Instance?.Shake(12f, 0.3f);
                yield return new WaitForSeconds(0.4f);
            }
        }

        public static void GenesisBloom(SkillSystem sys, int level)
        {
            var player = P(sys);
            float dmg = 140f * player.DamageMult;
            float radius = 160f * player.AoeMult;
            var hits = sys.FindEnemiesInRadius(player.transform.position, radius);
            foreach (var e in hits)
            {
                e.TakeDamage(dmg, DamageType.Poison);
                player.ApplyLifesteal(dmg * 0.4f);
            }
            SkillVFX.SpawnAoe(player.transform.position, radius, new Color(0.9f, 0.1f, 0.3f, 0.6f), 0.6f);
        }

        public static void GrimReaper(SkillSystem sys, int level)
        {
            var player = P(sys);
            float dmg = 250f * player.DamageMult;
            float radius = 220f * player.AoeMult;
            var hits = sys.FindEnemiesInRadius(player.transform.position, radius);
            foreach (var e in hits)
            {
                e.TakeDamage(dmg, DamageType.Dark);
                e.ApplyKnockback((e.transform.position - player.transform.position).normalized * 300f);
            }
            SkillVFX.SpawnAoe(player.transform.position, radius, Color.black, 0.5f);
        }

        public static void ArchangelLight(SkillSystem sys, int level)
        {
            sys.StartCoroutine(ArchangelLightCoroutine(sys, level));
        }

        private static IEnumerator ArchangelLightCoroutine(SkillSystem sys, int level)
        {
            var player = P(sys);
            float dmg = 180f * player.DamageMult;
            float radius = 200f * player.AoeMult;
            float duration = 3f;
            float elapsed = 0f;
            while (elapsed < duration && player != null)
            {
                player.TakeDamage(-player.MaxHp * 0.03f * Time.deltaTime);
                var hits = sys.FindEnemiesInRadius(player.transform.position, radius);
                foreach (var e in hits) e.TakeDamage(dmg * Time.deltaTime, DamageType.Holy);

                SkillVFX.SpawnAoe(player.transform.position, radius, new Color(1f, 0.95f, 0.6f, 0.2f), 0.1f);
                elapsed += 0.2f;
                yield return new WaitForSeconds(0.2f);
            }
        }

        public static void VoidDemon(SkillSystem sys, int level)
        {
            var player = P(sys);
            player.StartCoroutine(VoidDemonCoroutine(player, 10f));
        }

        private static IEnumerator VoidDemonCoroutine(PlayerController player, float duration)
        {
            float originalScale = player.transform.localScale.x;
            player.transform.localScale = Vector3.one * (originalScale * 1.8f);
            float originalDmg = player.DamageMult;
            player.DamageMult *= 1.5f;
            yield return new WaitForSeconds(duration);
            if (player != null)
            {
                player.transform.localScale = Vector3.one * originalScale;
                player.DamageMult = originalDmg;
            }
        }

        public static void GlacialSanctum(SkillSystem sys, int level)
        {
            sys.StartCoroutine(GlacialSanctumCoroutine(sys, level));
        }

        private static IEnumerator GlacialSanctumCoroutine(SkillSystem sys, int level)
        {
            var player = P(sys);
            float dmg = 120f * player.DamageMult;
            float radius = 220f * player.AoeMult;
            float duration = 4f;
            float elapsed = 0f;
            while (elapsed < duration && player != null)
            {
                player.TakeDamage(-player.MaxHp * 0.05f * Time.deltaTime);
                var hits = sys.FindEnemiesInRadius(player.transform.position, radius);
                foreach (var e in hits)
                {
                    e.TakeDamage(dmg * Time.deltaTime, DamageType.Ice);
                    e.ApplySlow(1f, 1f);
                }
                SkillVFX.SpawnAoe(player.transform.position, radius, new Color(0.6f, 0.9f, 1f, 0.25f), 0.2f);
                elapsed += 0.2f;
                yield return new WaitForSeconds(0.2f);
            }
        }

        // ════════════════════════════════════════════════════════
        // LEGENDARY COMBOS
        // ════════════════════════════════════════════════════════

        public static void SpecterStorm(SkillSystem sys, int level)
        {
            sys.StartCoroutine(SpecterStormCoroutine(sys, level));
        }

        private static IEnumerator SpecterStormCoroutine(SkillSystem sys, int level)
        {
            var player = P(sys);
            float dmg = 200f * player.DamageMult;
            for (int i = 0; i < 6; i++)
            {
                if (player == null) yield break;
                float angle = (360f / 6f) * i * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 spawnPos = (Vector2)player.transform.position - dir * 200f;
                SkillVFX.SpawnDashTrail(spawnPos, dir, Color.magenta, 0.4f);
                var hits = sys.FindEnemiesInRadius(spawnPos + dir * 200f, 120f * player.AoeMult);
                foreach (var e in hits) e.TakeDamage(dmg, DamageType.Physical);
                yield return new WaitForSeconds(0.12f);
            }
        }

        public static void CelestialNova(SkillSystem sys, int level)
        {
            sys.StartCoroutine(CelestialNovaCoroutine(sys, level));
        }

        private static IEnumerator CelestialNovaCoroutine(SkillSystem sys, int level)
        {
            var player = P(sys);
            float dmg = 250f * player.DamageMult;
            float radius = 300f * player.AoeMult;

            var hits = sys.FindEnemiesInRadius(player.transform.position, radius);
            foreach (var e in hits)
            {
                Vector2 pullDir = ((Vector2)player.transform.position - (Vector2)e.transform.position).normalized;
                e.ApplyKnockback(pullDir * 600f);
            }
            yield return new WaitForSeconds(0.25f);

            hits = sys.FindEnemiesInRadius(player.transform.position, radius);
            foreach (var e in hits)
            {
                e.TakeDamage(dmg, DamageType.Ice);
                e.ApplySlow(1f, 2.5f);
            }
            SkillVFX.SpawnAoe(player.transform.position, radius, new Color(0.2f, 0.7f, 1f, 0.7f), 0.6f);
            CameraController.Instance?.Shake(20f, 0.5f);
        }

        public static void TitanFortress(SkillSystem sys, int level)
        {
            sys.StartCoroutine(TitanFortressCoroutine(sys, level));
        }

        private static IEnumerator TitanFortressCoroutine(SkillSystem sys, int level)
        {
            var player = P(sys);
            float duration = 3f;
            float dmg = 180f * player.DamageMult;
            float radius = 250f * player.AoeMult;

            SkillVFX.SpawnShieldEffect(player.transform, duration);
            yield return new WaitForSeconds(duration);

            var hits = sys.FindEnemiesInRadius(player.transform.position, radius);
            foreach (var e in hits)
            {
                e.TakeDamage(dmg, DamageType.Physical);
                e.ApplyKnockback((e.transform.position - player.transform.position).normalized * 600f);
            }
            SkillVFX.SpawnAoe(player.transform.position, radius, Color.red, 0.5f);
            CameraController.Instance?.Shake(25f, 0.5f);
        }

        public static void HurricaneBarrage(SkillSystem sys, int level)
        {
            sys.StartCoroutine(HurricaneBarrageCoroutine(sys, level));
        }

        private static IEnumerator HurricaneBarrageCoroutine(SkillSystem sys, int level)
        {
            var player = P(sys);
            float dmg = 160f * player.DamageMult;
            for (int step = 0; step < 15; step++)
            {
                if (player == null) yield break;
                for (int i = 0; i < 8; i++)
                {
                    float angle = (step * 8f + (360f / 8f) * i) * Mathf.Deg2Rad;
                    Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    sys.SpawnProjectile(SkillPrefabs.Get("arrow_proj"), player.transform.position, dir, 520f, dmg, 600f);
                }
                yield return new WaitForSeconds(0.18f);
            }
        }

        public static void JudgmentDay(SkillSystem sys, int level)
        {
            sys.StartCoroutine(JudgmentDayCoroutine(sys, level));
        }

        private static IEnumerator JudgmentDayCoroutine(SkillSystem sys, int level)
        {
            var player = P(sys);
            float dmg = 300f * player.DamageMult;
            float radius = 350f * player.AoeMult;

            for (int i = 0; i < 8; i++)
            {
                if (player == null) yield break;
                Vector2 pos = (Vector2)player.transform.position + Random.insideUnitCircle * 400f;
                SkillVFX.SpawnWarningCircle(pos, 70f, 0.4f);
                yield return new WaitForSeconds(0.4f);
                var hits = sys.FindEnemiesInRadius(pos, 70f);
                foreach (var e in hits) e.TakeDamage(dmg * 0.5f, DamageType.Holy);
                SkillVFX.SpawnAoe(pos, 70f, Color.yellow, 0.3f);
            }

            var allHits = sys.FindEnemiesInRadius(player.transform.position, radius);
            foreach (var e in allHits) e.TakeDamage(dmg, DamageType.Holy);
            SkillVFX.SpawnAoe(player.transform.position, radius, Color.white, 0.6f);
            CameraController.Instance?.Shake(30f, 0.6f);
        }

        public static void LichArmy(SkillSystem sys, int level)
        {
            var player = P(sys);
            float dmg = 220f * player.DamageMult;
            for (int i = 0; i < 3; i++)
            {
                Vector2 pos = (Vector2)player.transform.position + Random.insideUnitCircle * 70f;
                var go = GameObject.Instantiate(SkillPrefabs.Get("skeleton_prefab"), pos, Quaternion.identity);
                var summon = go.GetComponent<SummonController>();
                if (summon == null) summon = go.AddComponent<SummonController>();
                summon.Init(player, 400f, dmg, 80f, sys);
                sys.Summons.Add(summon);

                go.AddComponent<LichLaserShooter>().Init(sys, dmg);
                SkillVFX.SpawnSummonEffect(pos, Color.blue);
            }
        }

        public static void GraveWrath(SkillSystem sys, int level)
        {
            var player = P(sys);
            float dmg = 240f * player.DamageMult;
            var enemies = sys.FindEnemiesInRadius(player.transform.position, 600f * player.AoeMult);
            foreach (var e in enemies)
            {
                e.TakeDamage(dmg, DamageType.Poison);
                e.ApplySlow(0.9f, 3.5f);
                SkillVFX.SpawnImpact(e.transform.position, Color.green, 0.3f);
            }
            CameraController.Instance?.Shake(15f, 0.4f);
        }
    }

    // ════════════════════════════════════════════════════════
    // HELPER BEHAVIOR COMPONENTS
    // ════════════════════════════════════════════════════════

    public class OrbitingProjectile : MonoBehaviour
    {
        public Transform center;
        public float radius = 80f;
        public float speed = 180f;
        public float angleOffset;

        private void Update()
        {
            if (center == null) { Destroy(gameObject); return; }
            float angle = Time.time * speed * Mathf.Deg2Rad + angleOffset;
            transform.position = (Vector2)center.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
    }

    public class TornadoEffect : MonoBehaviour
    {
        private float _kb = 120f;
        public void Init(float kb) { _kb = kb; }
        private void OnTriggerStay2D(Collider2D other)
        {
            if (other.CompareTag("Enemy"))
            {
                var ec = other.GetComponent<EnemyController>();
                if (ec != null)
                {
                    Vector2 dir = (other.transform.position - transform.position).normalized;
                    ec.ApplyKnockback(dir * _kb);
                }
            }
        }
    }

    public class PoisonEffect : MonoBehaviour
    {
        private float _dot;
        private float _dur;
        public void Init(float dot, float dur) { _dot = dot; _dur = dur; }
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Enemy"))
            {
                var ec = other.GetComponent<EnemyController>();
                if (ec != null) ec.StartCoroutine(ApplyPoison(ec));
            }
        }
        private IEnumerator ApplyPoison(EnemyController enemy)
        {
            float elapsed = 0;
            while (elapsed < _dur && enemy != null && enemy.IsAlive)
            {
                enemy.TakeDamage(_dot * 0.5f, DamageType.Poison);
                enemy.ApplySlow(0.2f, 0.5f);
                elapsed += 0.5f;
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    public class LichLaserShooter : MonoBehaviour
    {
        private SkillSystem _sys;
        private float _dmg;
        private float _cooldown = 1.5f;
        private float _timer;

        public void Init(SkillSystem sys, float dmg) { _sys = sys; _dmg = dmg; }
        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0 && _sys != null)
            {
                _timer = _cooldown;
                var target = _sys.FindNearestEnemy(400f);
                if (target != null)
                {
                    Vector2 dir = (target.transform.position - transform.position).normalized;
                    _sys.SpawnProjectile(SkillPrefabs.Get("magic_missile_proj"), transform.position, dir, 480f, _dmg, 400f);
                    SkillVFX.SpawnImpact(target.transform.position, Color.black, 0.2f);
                }
            }
        }
    }

    public enum DamageType { Physical, Shadow, Fire, Ice, Holy, Poison, Lightning, Dark, True }
}
