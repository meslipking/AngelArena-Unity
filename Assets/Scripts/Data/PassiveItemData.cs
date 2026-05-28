using UnityEngine;

namespace AngelArena.Data
{
    public enum PassiveEffect
    {
        DamageMult, DefMult, MaxHpMult, SpeedMult, CooldownMult,
        AoeMult, LifeSteal, XpGainMult, GoldMult, CritChance, HpRegen
    }

    [CreateAssetMenu(fileName = "New Passive Item", menuName = "AngelArena/Passive Item")]
    public class PassiveItemData : ScriptableObject
    {
        [Header("Identity")]
        public string itemName;
        public string itemId;   // e.g. "spinach", "armor_plate", "hollow_heart"
        [TextArea(2, 3)]
        public string description;

        [Header("Visual")]
        public Sprite icon;
        public Color  iconColor = Color.white;

        [Header("Effect")]
        public PassiveEffect effect;
        [Tooltip("Added per level (e.g. 0.10 = 10% per level)")]
        public float effectPerLevel;
        [Range(1, 5)]
        public int maxLevel = 5;

        public float GetTotalEffect(int level) => effectPerLevel * Mathf.Min(level, maxLevel);
    }
}
