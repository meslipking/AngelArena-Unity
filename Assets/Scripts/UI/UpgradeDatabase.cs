using System.Collections.Generic;
using UnityEngine;
using AngelArena.Data;
using AngelArena.Core;

namespace AngelArena.UI
{
    /// <summary>
    /// Upgrade choice database — generates weighted level-up options.
    /// </summary>
    public class UpgradeDatabase : MonoBehaviour
    {
        public static UpgradeDatabase Instance { get; private set; }

        [Header("All Skill Data (assign in Inspector)")]
        public SkillData[] allSkills;

        [Header("All Passive Items (assign in Inspector)")]
        public PassiveItemData[] allPassives;

        [Header("Stat Boost Templates")]
        public StatBoostTemplate[] statBoosts;

        private void Awake() { Instance = this; }

        // ── Skills ───────────────────────────────────────────────
        public List<UpgradeChoice> GetSkillChoices(SkillSystem sys, PlayerController player)
        {
            var result = new List<UpgradeChoice>();
            if (allSkills == null || sys == null) return result;

            string branch = GameManager.Instance?.selectedCharacter?.characterId ?? "";

            foreach (var sd in allSkills)
            {
                if (sd == null) continue;
                if (!string.IsNullOrEmpty(sd.branchId) && sd.branchId != branch) continue;

                var existing = sys.Skills.Find(s => s.data.skillId == sd.skillId);
                bool owned   = existing != null;
                if (owned && existing.level >= sd.maxLevel) continue;

                float weight = owned ? 1.5f : 1.0f;
                if (sd.isLegendary) weight = 0.3f;

                var sd_captured = sd;
                result.Add(new UpgradeChoice
                {
                    label       = owned ? $"UPGRADE: {sd.skillName} Lv{existing.level+1}" : $"NEW: {sd.skillName}",
                    description = sd.description,
                    icon        = sd.icon,
                    color       = sd.skillColor,
                    weight      = weight,
                    type        = owned ? UpgradeChoiceType.UpgradeSkill : UpgradeChoiceType.NewSkill,
                    apply       = () =>
                    {
                        if (owned) sys.UpgradeSkill(sd_captured.skillId);
                        else       sys.AddSkill(sd_captured.skillId);
                    }
                });
            }
            return result;
        }

        // ── Passives ─────────────────────────────────────────────
        public List<UpgradeChoice> GetPassiveChoices(PlayerController player)
        {
            var result = new List<UpgradeChoice>();
            if (allPassives == null) return result;

            foreach (var p in allPassives)
            {
                if (p == null) continue;
                var p_captured = p;
                result.Add(new UpgradeChoice
                {
                    label       = p.itemName,
                    description = p.description,
                    icon        = p.icon,
                    color       = p.iconColor,
                    weight      = 1.0f,
                    type        = UpgradeChoiceType.NewPassive,
                    apply       = () => player.AddPassiveItem(p_captured.itemId, p_captured.itemName, p_captured.icon, p_captured.iconColor, p_captured.effect, p_captured.effectPerLevel)
                });
            }
            return result;
        }

        // ── Stat Boosts ──────────────────────────────────────────
        public List<UpgradeChoice> GetStatBoosts(PlayerController player)
        {
            var result = new List<UpgradeChoice>();
            if (statBoosts == null) return result;

            foreach (var s in statBoosts)
            {
                if (s == null) continue;
                var s_captured = s;
                result.Add(new UpgradeChoice
                {
                    label       = s.label,
                    description = s.description,
                    color       = s.color,
                    weight      = 0.8f,
                    type        = UpgradeChoiceType.StatBoost,
                    apply       = () => player.ApplyPassiveEffect(s_captured.effect, s_captured.value, 1)
                });
            }
            return result;
        }
    }

    [System.Serializable]
    public class StatBoostTemplate
    {
        public string      label;
        public string      description;
        public Color       color;
        public PassiveEffect effect;
        public float       value;
    }
}
