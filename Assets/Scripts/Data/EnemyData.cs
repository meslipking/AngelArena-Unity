using UnityEngine;

namespace AngelArena.Data
{
    public enum EnemyBehaviorType { Chase, Flank, Ranged, Boss, Necromancer }

    [CreateAssetMenu(fileName = "New Enemy", menuName = "AngelArena/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        public string enemyName;
        public string enemyId;   // e.g. "slime", "necromancer", "demon_lord"

        [Header("Visuals")]
        public Sprite   sprite;
        public Color    color = Color.white;
        public float    radius = 20f;

        [Header("Base Stats")]
        public float baseHp     = 80f;
        public float baseDamage = 12f;
        public float moveSpeed  = 100f;

        [Header("Rewards")]
        public int baseXp   = 5;
        public int baseGold = 1;

        [Header("Combat")]
        public float contactDamageCooldown = 1.0f;
        public bool  canRangedAttack;
        public float rangedAttackRange    = 300f;
        public float rangedAttackDamage   = 20f;
        public float rangedAttackCooldown = 2.5f;
        public GameObject rangedProjectilePrefab;

        [Header("Behavior")]
        public EnemyBehaviorType behavior = EnemyBehaviorType.Chase;
        [Range(0f, 1f)] public float flankChance = 0.3f; // Probability of flanking
        public float aggroRange = 350f; // Detection range

        [Header("Boss")]
        public bool  isBoss;
        public float phase2HpThreshold = 0.6f;  // Enter phase 2 at 60% HP
        public float phase3HpThreshold = 0.3f;  // Enter phase 3 at 30% HP
        public bool  canSummonMinions;
        public float summonCooldown = 5f;
        public int   summonCount    = 3;
        public EnemyData[] minionTypes;

        // ── HP scaled by wave time ──────────────────────────────
        public float GetScaledHp(float hpMult)   => baseHp   * hpMult;
        public float GetScaledDmg(float dmgMult)  => baseDamage * dmgMult;
    }
}
