using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using AngelArena.Core;

namespace AngelArena.Core
{
    /// <summary>
    /// In-game HUD: survival timer, HP/XP bars, skill bar, boss bar, minimap, kill streak.
    /// Uses UnityEngine.UI.Text (no TMPro package needed).
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        public static HUDManager Instance { get; private set; }

        [Header("Bars")]
        public Slider hpBar;
        public Slider xpBar;
        public Text   hpText;
        public Text   levelText;

        [Header("Timer")]
        public Text survivalTimerText;

        [Header("Skill Bar")]
        public SkillSlotUI[] skillSlots;   // 6 slots

        [Header("Boss Bar")]
        public GameObject bossBarRoot;
        public Slider     bossHpBar;
        public Text       bossNameText;
        public Text       bossPhaseText;

        [Header("Kill Streak")]
        public GameObject killStreakRoot;
        public Text       killStreakText;

        [Header("Vignette")]
        public Image vignetteImage;

        [Header("Boss Warning")]
        public GameObject bossWarningRoot;
        public Text       bossWarningText;

        // ─────────────────────────────────────────────────────────
        private void Awake() { Instance = this; }

        private void OnEnable()
        {
            GameManager.OnTimerUpdate       += UpdateTimer;
            PlayerController.OnHpChanged    += UpdateHpBar;
            PlayerController.OnXpChanged    += UpdateXpBar;
            GameManager.OnLevelUp           += UpdateLevel;
            GameManager.OnKillStreakUpdate  += UpdateKillStreak;
        }

        private void OnDisable()
        {
            GameManager.OnTimerUpdate       -= UpdateTimer;
            PlayerController.OnHpChanged    -= UpdateHpBar;
            PlayerController.OnXpChanged    -= UpdateXpBar;
            GameManager.OnLevelUp           -= UpdateLevel;
            GameManager.OnKillStreakUpdate  -= UpdateKillStreak;
        }

        private void Update() => UpdateBossBar();

        // ── Timer ─────────────────────────────────────────────────
        private void UpdateTimer(float elapsed)
        {
            int min = (int)(elapsed / 60);
            int sec = (int)(elapsed % 60);
            if (survivalTimerText) survivalTimerText.text = $"{min}:{sec:00}";
        }

        // ── HP Bar ───────────────────────────────────────────────
        private void UpdateHpBar(float hp, float maxHp)
        {
            if (hpBar)  hpBar.value  = hp / maxHp;
            if (hpText) hpText.text  = $"{(int)hp} / {(int)maxHp}";
            float pct = hp / maxHp;
            if (pct < 0.3f) ShowLowHpVignette(1f - pct / 0.3f);
            else            HideLowHpVignette();
        }

        // ── XP Bar ───────────────────────────────────────────────
        private void UpdateXpBar(int xp, int toNext)
        {
            if (xpBar) xpBar.value = (float)xp / toNext;
        }

        private void UpdateLevel(int level)
        {
            if (levelText) levelText.text = $"LV.{level}";
        }

        // ── Skill Bar ─────────────────────────────────────────────
        public void RefreshSkillBar(System.Collections.Generic.List<SkillSystem.ActiveSkill> skills)
        {
            for (int i = 0; i < skillSlots.Length; i++)
            {
                if (i < skills.Count) skillSlots[i].SetSkill(skills[i]);
                else                  skillSlots[i].SetEmpty();
            }
        }

        // ── Boss Bar ─────────────────────────────────────────────
        private void UpdateBossBar()
        {
            var boss = EnemySpawner.AllEnemies?.Find(e => e != null && e.isBoss && e.IsAlive);
            if (bossBarRoot) bossBarRoot.SetActive(boss != null);

            if (boss != null)
            {
                if (bossHpBar)    bossHpBar.value   = boss.Hp / boss.MaxHp;
                if (bossNameText) bossNameText.text  = $"BOSS: {boss.EnemyName}  {(int)(boss.Hp / boss.MaxHp * 100)}%";
                if (bossPhaseText) bossPhaseText.text = boss.Phase switch
                {
                    3 => "PHASE 3 - ENRAGED",
                    2 => "PHASE 2",
                    _ => "PHASE 1"
                };
            }
        }

        // ── Kill Streak ──────────────────────────────────────────
        private void UpdateKillStreak(int streak)
        {
            if (killStreakRoot) killStreakRoot.SetActive(streak >= 5);
            if (killStreakText && streak >= 5) killStreakText.text = $"{streak}x KILL STREAK!";
        }

        // ── Vignette ─────────────────────────────────────────────
        public void TriggerDamageVignette()
        {
            // Delegate to new HUDOverlay if available
            AngelArena.Graphics.HUDOverlay.Instance?.TriggerDamageVignette();
            if (vignetteImage == null) return;
            if (_vignetteCoroutine != null) StopCoroutine(_vignetteCoroutine);
            _vignetteCoroutine = StartCoroutine(DamageVignetteAnim());
        }

        private Coroutine _vignetteCoroutine;

        private IEnumerator DamageVignetteAnim()
        {
            if (vignetteImage == null) yield break;
            vignetteImage.color = new Color(0.8f, 0f, 0f, 0.6f);
            float t = 0;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                vignetteImage.color = new Color(0.8f, 0f, 0f, Mathf.Lerp(0.6f, 0f, t / 0.5f));
                yield return null;
            }
            vignetteImage.color = Color.clear;
        }

        private void ShowLowHpVignette(float alpha)
        {
            if (vignetteImage) vignetteImage.color = new Color(0.5f, 0f, 0f, alpha * 0.4f);
        }

        private void HideLowHpVignette()
        {
            if (vignetteImage) vignetteImage.color = Color.clear;
        }

        // ── Boss Warning ─────────────────────────────────────────
        public void ShowBossWarning(string bossId)
        {
            AngelArena.Graphics.HUDOverlay.Instance?.ShowBossWarning(bossId);
            if (bossWarningRoot == null) return;
            if (bossWarningText) bossWarningText.text = $"BOSS APPROACHING: {bossId.ToUpper()}";
            StartCoroutine(BossWarningAnim());
        }

        private IEnumerator BossWarningAnim()
        {
            if (bossWarningRoot) bossWarningRoot.SetActive(true);
            yield return new WaitForSeconds(3f);
            if (bossWarningRoot) bossWarningRoot.SetActive(false);
        }
    }
}
