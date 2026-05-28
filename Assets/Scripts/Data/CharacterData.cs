using UnityEngine;

namespace AngelArena.Data
{
    public enum CharacterWeaponType { Daggers, Sword, Wand, Bow, Hammer, SkullStaff, NatureStaff }

    [System.Serializable]
    public struct StatPreview
    {
        [Range(0, 100)] public int hp;
        [Range(0, 100)] public int spd;
        [Range(0, 100)] public int atk;
        [Range(0, 100)] public int def;
    }

    [CreateAssetMenu(fileName = "New Character", menuName = "AngelArena/Character Data")]
    public class CharacterData : ScriptableObject
    {
        [Header("Identity")]
        public string characterName;
        public string characterId;   // e.g. "assassin", "mage", "necromancer"
        [TextArea(2, 4)]
        public string description;
        public string startSkillId;  // e.g. "shadow_blades"

        [Header("Visuals")]
        public Sprite characterSprite;
        public Sprite portrait;
        public Color  characterColor = Color.white;

        [Header("Base Stats")]
        [Range(150f, 400f)] public float maxHp     = 200f;
        [Range(100f, 250f)] public float moveSpeed  = 170f;
        [Range(0.6f, 1.6f)] public float atkMult    = 1.0f;
        [Range(0.5f, 1.6f)] public float defMult    = 1.0f;
        [Range(0f,   0.1f)] public float baseLifesteal = 0f;

        [Header("Weapon")]
        public CharacterWeaponType weaponType;

        [Header("Stat Preview (0-100)")]
        public StatPreview statPreview;

        // ── Helper: resolved start HP ──────────────────────────
        public float GetStartHp()   => maxHp;
        public float GetMoveSpeed() => moveSpeed;
    }
}
