// ── Shared UI type aliases ────────────────────────────────────────────────
// This file provides compile-time stubs for TMP_Text and SkillSlotUI
// without requiring TextMeshPro to be installed.
// After installing TextMeshPro package, replace "using TMP_Text = ..." with
// "using TMPro;" in each UI file.

using UnityEngine;
using UnityEngine.UI;

namespace AngelArena.Core
{
    // ── SkillSlotUI stub ────────────────────────────────────────
    public class SkillSlotUI : MonoBehaviour
    {
        public Image  iconImage;
        public Image  cdOverlay;
        public Text   levelText;

        public void SetSkill(SkillSystem.ActiveSkill skill)
        {
            if (skill?.data == null) { SetEmpty(); return; }
            if (iconImage) iconImage.sprite = skill.data.icon;
            if (iconImage) iconImage.color  = skill.data.skillColor;
            if (levelText) levelText.text   = $"Lv{skill.level}";
            gameObject.SetActive(true);
        }

        public void SetEmpty()
        {
            if (iconImage) iconImage.color = new Color(1,1,1,0.2f);
            if (levelText) levelText.text  = "";
            if (cdOverlay) cdOverlay.fillAmount = 0;
        }

        public void UpdateCooldown(SkillSystem.ActiveSkill skill, float cdMult)
        {
            if (cdOverlay == null || skill?.data == null) return;
            float cd      = skill.data.GetCooldownAtLevel(skill.level) * cdMult;
            float elapsed = Time.time - skill.lastFired;
            cdOverlay.fillAmount = Mathf.Clamp01(1f - elapsed / cd);
        }
    }

    // ── UpgradeCardUI stub ──────────────────────────────────────
    public class UpgradeCardUI : MonoBehaviour
    {
        public Image iconImage;
        public Text  iconText;
        public Text  titleText;
        public Text  descText;
        public Text  typeText;
        public Outline cardOutline;

        public void SetChoice(UpgradeChoice choice)
        {
            if (choice == null) return;
            if (iconImage)
            {
                if (choice.icon != null)
                {
                    iconImage.gameObject.SetActive(true);
                    iconImage.sprite = choice.icon;
                    iconImage.color  = choice.color;
                    if (iconText) iconText.gameObject.SetActive(false);
                }
                else
                {
                    iconImage.gameObject.SetActive(false);
                    if (iconText)
                    {
                        iconText.gameObject.SetActive(true);
                        iconText.text = GetFallbackEmoji(choice);
                        iconText.color = choice.color;
                    }
                }
            }
            if (titleText) titleText.text  = choice.label;
            if (descText)  descText.text   = choice.description;
            if (typeText)  typeText.text   = choice.type.ToString().ToUpper();
            if (cardOutline) cardOutline.effectColor = choice.color;
        }

        private string GetFallbackEmoji(UpgradeChoice choice)
        {
            if (choice.type == UpgradeChoiceType.StatBoost)
            {
                if (choice.label.Contains("HP")) return "❤️";
                if (choice.label.Contains("SPEED") || choice.label.Contains("Tốc")) return "👟";
                if (choice.label.Contains("ATK") || choice.label.Contains("Damage")) return "⚔️";
                return "⭐";
            }
            if (choice.type == UpgradeChoiceType.NewPassive || choice.type == UpgradeChoiceType.UpgradePassive) return "💎";
            return "🔮";
        }
    }
}
