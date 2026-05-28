using System.Collections.Generic;
using UnityEngine;
using AngelArena.Data;

namespace AngelArena.Core
{
    /// <summary>
    /// Manages the player's active skills: holds skill states (level, last fired),
    /// auto-fires skills when cooldown expires, delegates execution to individual skill handlers.
    /// </summary>
    public class SkillSystem : MonoBehaviour
    {
        [System.Serializable]
        public class ActiveSkill
        {
            public SkillData data;
            public int   level    = 1;
            public float lastFired = -999f;
            public bool  IsLegendary => data != null && data.isLegendary;
            public float GetEffectiveCd(float cdMult) => data.GetCooldownAtLevel(level) * cdMult;
        }

        [Header("References")]
        public PlayerController player;
        public Transform        projectileParent; // For spawned projectiles

        [Header("Skill Database")]
        public SkillData[]      allSkillDatabase; // Assign all skill SOs in inspector

        // ── Runtime ──────────────────────────────────────────────
        public List<ActiveSkill> Skills { get; } = new();
        private int _maxActiveSkills = 6;

        // ── Summons ──────────────────────────────────────────────
        public List<SummonController> Summons { get; } = new();

        // ─────────────────────────────────────────────────────────
        private void Start()
        {
            // Add starting skill from character data
            if (GameManager.Instance.selectedCharacter != null)
                AddSkill(GameManager.Instance.selectedCharacter.startSkillId);
        }

        private void Update()
        {
            if (!GameManager.Instance.GameRunning) return;

            float cdMult = player != null ? player.CdMult : 1f;
            float now    = Time.time;

            foreach (var skill in Skills)
            {
                if (skill.data == null) continue;
                float elapsed = now - skill.lastFired;
                float cd      = skill.GetEffectiveCd(cdMult);

                if (elapsed >= cd)
                {
                    skill.lastFired = now;
                    FireSkill(skill);
                }
            }

            // Update minimap via HUD
            // Prune dead summons
            Summons.RemoveAll(s => s == null || !s.gameObject.activeInHierarchy);
        }

        // ── Skill Management ─────────────────────────────────────
        public bool AddSkill(string skillId)
        {
            SkillData def = FindSkillData(skillId);
            if (def == null) { Debug.LogWarning($"SkillData not found: {skillId}"); return false; }

            // Check if already owned — upgrade instead
            var existing = Skills.Find(s => s.data.skillId == skillId);
            if (existing != null) { existing.level = Mathf.Min(existing.level + 1, def.maxLevel); return true; }

            // Check max slots
            int nonLegendary = Skills.FindAll(s => !s.IsLegendary).Count;
            if (!def.isLegendary && nonLegendary >= _maxActiveSkills) return false;

            Skills.Add(new ActiveSkill { data = def, level = 1, lastFired = Time.time });
            GameManager.Instance.hudManager?.RefreshSkillBar(Skills);
            CheckLegendary();
            return true;
        }

        public void UpgradeSkill(string skillId)
        {
            var sk = Skills.Find(s => s.data.skillId == skillId);
            if (sk != null) { sk.level = Mathf.Min(sk.level + 1, sk.data.maxLevel); CheckLegendary(); }
        }

        private bool HasSkillOrEvo(string id)
        {
            if (Skills.Exists(s => s.data != null && s.data.skillId == id)) return true;

            // Check if they have the evolution of this skill
            switch (id)
            {
                case "shadow_blades":   return Skills.Exists(s => s.data != null && s.data.skillId == "void_reaver");
                case "shadow_clone":    return Skills.Exists(s => s.data != null && s.data.skillId == "void_demon");
                case "magic_missile":   return Skills.Exists(s => s.data != null && s.data.skillId == "spell_storm");
                case "shield_bash":     return Skills.Exists(s => s.data != null && s.data.skillId == "aegis_smash");
                case "holy_light":      return Skills.Exists(s => s.data != null && s.data.skillId == "archangel_light");
                case "summon_skeleton": return Skills.Exists(s => s.data != null && s.data.skillId == "grim_reaper");
                case "briar_patch":     return Skills.Exists(s => s.data != null && s.data.skillId == "genesis_bloom");
                case "frost_aura":      return Skills.Exists(s => s.data != null && s.data.skillId == "glacial_sanctum");
                case "consecration":    return Skills.Exists(s => s.data != null && s.data.skillId == "glacial_sanctum");
            }
            return false;
        }

        private void CheckLegendary()
        {
            if (player == null) return;
            bool updated = false;

            // 1. Check UNION first
            // UNION: frost_aura + consecration -> glacial_sanctum
            var s1 = Skills.Find(s => s.data != null && s.data.skillId == "frost_aura");
            var s2 = Skills.Find(s => s.data != null && s.data.skillId == "consecration");
            if (s1 != null && s1.level >= 8 && s2 != null && s2.level >= 8)
            {
                if (!Skills.Exists(s => s.data != null && s.data.skillId == "glacial_sanctum"))
                {
                    Skills.Remove(s1);
                    Skills.Remove(s2);
                    var evoData = FindSkillData("glacial_sanctum");
                    if (evoData != null)
                    {
                        Skills.Add(new ActiveSkill { data = evoData, level = 1, lastFired = Time.time });
                        updated = true;
                        Debug.Log("[AngelArena] Evolved Union: glacial_sanctum");
                        HUDOverlay.Instance?.ShowLegendaryUnlockBanner("glacial_sanctum");
                    }
                }
            }

            // 2. Check standard EVOS, GIFTS, and MORPHS
            var standardEvos = new (string baseSkill, string passiveItem, string evolvedSkill)[]
            {
                ("magic_missile", "empty_tome", "spell_storm"),
                ("piercing_arrow", "wings", "wind_runner"),
                ("shadow_blades", "clover", "void_reaver"),
                ("shield_bash", "armor_plate", "aegis_smash"),
                ("fireball", "spinach", "hellfire_meteor"),
                ("briar_patch", "hollow_heart", "genesis_bloom"),
                ("summon_skeleton", "attractorb", "grim_reaper"),
                ("holy_light", "empty_tome", "archangel_light"),
                ("shadow_clone", "clover", "void_demon")
            };

            foreach (var combo in standardEvos)
            {
                var baseSk = Skills.Find(s => s.data != null && s.data.skillId == combo.baseSkill);
                if (baseSk != null && baseSk.level >= 8)
                {
                    bool hasPassive = player.ownedPassives.Exists(p => p.itemId == combo.passiveItem);
                    if (hasPassive)
                    {
                        if (!Skills.Exists(s => s.data != null && s.data.skillId == combo.evolvedSkill))
                        {
                            int index = Skills.IndexOf(baseSk);
                            var evoData = FindSkillData(combo.evolvedSkill);
                            if (evoData != null && index != -1)
                            {
                                Skills[index] = new ActiveSkill { data = evoData, level = 1, lastFired = Time.time };
                                updated = true;
                                Debug.Log($"[AngelArena] Evolved active skill: {combo.evolvedSkill}");
                                HUDOverlay.Instance?.ShowLegendaryUnlockBanner(combo.evolvedSkill);
                            }
                        }
                    }
                }
            }

            // 3. Check LEGENDARY_COMBOS
            var legendaryCombos = new (string branch, string[] needs, string unlocks)[]
            {
                ("assassin", new[] { "shadow_blades", "shadow_clone", "void_pulse", "poison_dart" }, "specter_storm"),
                ("mage", new[] { "frost_aura", "magic_missile", "thunder_ring", "meteor" }, "celestial_nova"),
                ("fighter", new[] { "shield_bash", "earthquake", "iron_wall", "rally" }, "titan_fortress"),
                ("ranger", new[] { "piercing_arrow", "arrow_rain", "spirit_wolf" }, "hurricane_barrage"),
                ("paladin", new[] { "holy_light", "divine_shield", "consecration" }, "judgment_day"),
                ("necromancer", new[] { "summon_skeleton", "death_grasp", "soul_drain" }, "lich_army"),
                ("druid", new[] { "briar_patch", "nature_regrowth", "vine_whip" }, "grave_wrath")
            };

            foreach (var combo in legendaryCombos)
            {
                if (Skills.Exists(s => s.data != null && s.data.skillId == combo.unlocks)) continue;

                bool hasAll = true;
                foreach (var needId in combo.needs)
                {
                    if (!HasSkillOrEvo(needId))
                    {
                        hasAll = false;
                        break;
                    }
                }

                if (hasAll)
                {
                    var evoData = FindSkillData(combo.unlocks);
                    if (evoData != null)
                    {
                        Skills.Add(new ActiveSkill { data = evoData, level = 1, lastFired = Time.time });
                        updated = true;
                        Debug.Log($"[AngelArena] Unlocked Legendary Combo: {combo.unlocks}");
                        HUDOverlay.Instance?.ShowLegendaryUnlockBanner(combo.unlocks);
                    }
                }
            }

            if (updated)
            {
                GameManager.Instance.hudManager?.RefreshSkillBar(Skills);
            }
        }

        private SkillData FindSkillData(string id)
        {
            foreach (var sd in allSkillDatabase)
                if (sd != null && sd.skillId == id) return sd;
            return null;
        }

        // ── Skill Execution ──────────────────────────────────────
        private void FireSkill(ActiveSkill skill)
        {
            string id    = skill.data.skillId;
            int    level = skill.level;

            switch (id)
            {
                // ── ASSASSIN ──
                case "shadow_blades":  SkillHandlers.ShadowBlades(this, level);  break;
                case "void_pulse":     SkillHandlers.VoidPulse(this, level);     break;
                case "phantom_dash":   SkillHandlers.PhantomDash(this, level);   break;
                case "dark_mark":      SkillHandlers.DarkMark(this, level);      break;

                // ── MAGE ──
                case "magic_missile":  SkillHandlers.MagicMissile(this, level);  break;
                case "frost_aura":     SkillHandlers.FrostAura(this, level);     break;
                case "thunder_ring":   SkillHandlers.ThunderRing(this, level);   break;
                case "meteor":         SkillHandlers.Meteor(this, level);        break;
                case "fireball":       SkillHandlers.Fireball(this, level);      break;

                // ── RANGER ──
                case "piercing_arrow": SkillHandlers.PiercingArrow(this, level); break;
                case "arrow_rain":     SkillHandlers.ArrowRain(this, level);     break;
                case "spirit_wolf":    SkillHandlers.SpiritWolf(this, level);    break;

                // ── PALADIN ──
                case "holy_light":     SkillHandlers.HolyLight(this, level);     break;
                case "divine_shield":  SkillHandlers.DivineShield(this, level);  break;
                case "consecration":   SkillHandlers.Consecration(this, level);  break;

                // ── NECROMANCER ──
                case "summon_skeleton":SkillHandlers.SummonSkeleton(this, level);break;
                case "death_grasp":    SkillHandlers.DeathGrasp(this, level);    break;
                case "soul_drain":     SkillHandlers.SoulDrain(this, level);     break;

                // ── FIGHTER ──
                case "battle_cry":     SkillHandlers.BattleCry(this, level);     break;
                case "iron_wall":      SkillHandlers.IronWall(this, level);      break;
                case "earthquake":     SkillHandlers.Earthquake(this, level);    break;

                // ── DRUID ──
                case "briar_patch":    SkillHandlers.BriarPatch(this, level);    break;
                case "nature_regrowth":SkillHandlers.NatureRegrowth(this, level);break;
                case "call_of_wild":   SkillHandlers.CallOfWild(this, level);    break;

                default:
                    Debug.LogWarning($"No handler for skill: {id}");
                    break;
            }
        }

        // ── Helper: find nearest enemy ───────────────────────────
        public EnemyController FindNearestEnemy(float maxRange = float.MaxValue)
        {
            EnemyController nearest = null;
            float minDist = maxRange;
            var enemies = EnemySpawner.AllEnemies;

            foreach (var e in enemies)
            {
                if (e == null || !e.IsAlive) continue;
                float d = Vector2.Distance(transform.position, e.transform.position);
                if (d < minDist) { minDist = d; nearest = e; }
            }
            return nearest;
        }

        public List<EnemyController> FindEnemiesInRadius(Vector2 center, float radius)
        {
            var result = new List<EnemyController>();
            foreach (var e in EnemySpawner.AllEnemies)
            {
                if (e == null || !e.IsAlive) continue;
                if (Vector2.Distance(center, e.transform.position) <= radius)
                    result.Add(e);
            }
            return result;
        }

        /// <summary>Spawn a projectile prefab and return its Projectile component.</summary>
        public Projectile SpawnProjectile(GameObject prefab, Vector2 pos, Vector2 dir,
                                          float speed, float damage, float range, bool piercing = false)
        {
            if (prefab == null) return null;
            var go  = Instantiate(prefab, pos, Quaternion.identity, projectileParent);
            var proj = go.GetComponent<Projectile>();
            proj?.Init(dir, speed, damage * player.DamageMult, range, player.AoeMult,
                       player.Lifesteal, piercing, player);
            return proj;
        }
    }
}
