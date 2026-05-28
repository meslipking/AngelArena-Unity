using UnityEngine;

namespace AngelArena.Data
{
    [CreateAssetMenu(fileName = "New Skill", menuName = "AngelArena/Skill Data")]
    public class SkillData : ScriptableObject
    {
        [Header("Identity")]
        public string skillName;
        public string skillId;        // e.g. "shadow_blades", "magic_missile"
        public string branchId;       // e.g. "assassin", "mage"
        [TextArea(2, 4)]
        public string description;

        [Header("Visuals")]
        public Sprite icon;
        public Color   skillColor = Color.white;
        public GameObject vfxPrefab;       // Spawned on cast
        public GameObject projectilePrefab; // If projectile-based

        [Header("Stats")]
        [Range(0.5f, 20f)] public float cooldown   = 3.0f;
        [Range(0f,   500f)] public float baseDamage = 30f;
        [Range(1f,   500f)] public float baseRange  = 200f;
        [Range(1f,   500f)] public float baseAoeRadius = 80f;

        [Header("Scaling per Level")]
        public AnimationCurve damageScaling;  // Y = multiplier at level X
        public AnimationCurve cooldownScaling; // Y = cd reduction at level X

        [Header("Flags")]
        public bool isLegendary;
        public bool isSummon;
        public bool isAoe;
        public bool isProjectile;
        public bool isHeal;
        [Range(1, 8)] public int maxLevel = 8;

        // ── Derived values at a given level ────────────────────
        public float GetDamageAtLevel(int level)
        {
            float mult = (damageScaling != null && damageScaling.length > 0)
                ? damageScaling.Evaluate(level)
                : 1f + (level - 1) * 0.15f;
            return baseDamage * mult;
        }

        public float GetCooldownAtLevel(int level)
        {
            float mult = (cooldownScaling != null && cooldownScaling.length > 0)
                ? cooldownScaling.Evaluate(level)
                : Mathf.Max(0.3f, 1f - (level - 1) * 0.05f);
            return cooldown * mult;
        }
    }
}
