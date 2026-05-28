using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using AngelArena.Core;

namespace AngelArena.Graphics
{
    /// <summary>
    /// Beautiful HUD overlay: HP bar, XP bar, timer, kill counter,
    /// level indicator, minimap hint, boss health bar.
    /// Styled with dark glassmorphism theme.
    /// </summary>
    public class HUDOverlay : MonoBehaviour
    {
        public static HUDOverlay Instance { get; private set; }

        // ── UI Elements ──────────────────────────────────────────
        private RectTransform _hpFill;
        private RectTransform _xpFill;
        private Text          _hpText;
        private Text          _levelText;
        private Text          _timerText;
        private Text          _killText;
        private Text          _goldText;
        private Text          _streakText;
        private CanvasGroup   _streakGroup;

        private GameObject    _bossBar;
        private RectTransform _bossFill;
        private Text          _bossNameText;
        private Text          _bossHpText;

        private Image         _vignetteImg;
        private float         _vignetteTimer;

        private GameObject    _levelUpPanel;
        private Text          _levelUpText;
        private float         _levelUpTimer;

        // Active Skills & Passives HUD elements
        private SkillSlotUI[] _skillSlots = new SkillSlotUI[6];
        private Image[]        _passiveSlots = new Image[6];
        private Text[]         _passiveLevelTexts = new Text[6];

        // ── Layout Constants ─────────────────────────────────────
        private const float BAR_W = 320f;
        private const float BAR_H = 22f;
        private const float PAD   = 16f;

        // ═════════════════════════════════════════════════════════
        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            BuildHUD();
            SubscribeEvents();
        }

        private void BuildHUD()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                gameObject.AddComponent<CanvasScaler>().uiScaleMode =
                    CanvasScaler.ScaleMode.ScaleWithScreenSize;
                gameObject.AddComponent<GraphicRaycaster>();
            }

            // ── HP Bar (top-left) ─────────────────────────────────
            var hpPanel = MakePanel(canvas.transform, new Vector2(PAD + BAR_W/2, -PAD - BAR_H/2),
                new Vector2(BAR_W + 8, BAR_H + 24), new Color(0,0,0,0.55f));
            hpPanel.pivot = new Vector2(0, 1);
            hpPanel.anchorMin = hpPanel.anchorMax = new Vector2(0, 1);

            MakeLabel(hpPanel, "HP", new Vector2(0, 10), 10, new Color(1f,0.4f,0.4f));
            var hpBG = MakeBarBG(hpPanel, Vector2.zero, BAR_W, BAR_H - 6, new Color(0.2f,0.05f,0.05f));
            _hpFill  = MakeBarFill(hpBG, new Color(0.9f,0.2f,0.2f), new Color(1f,0.5f,0.5f));
            _hpText  = MakeLabel(hpPanel, "500/500", new Vector2(0, -(BAR_H - 6)/2f - 8), 9, Color.white);

            // ── XP Bar (below HP) ─────────────────────────────────
            var xpPanel = MakePanel(canvas.transform, new Vector2(PAD + BAR_W/2, -PAD - BAR_H/2 - 44),
                new Vector2(BAR_W + 8, BAR_H - 8), new Color(0,0,0,0.45f));
            xpPanel.pivot = new Vector2(0, 1);
            xpPanel.anchorMin = xpPanel.anchorMax = new Vector2(0, 1);

            var xpBG = MakeBarBG(xpPanel, Vector2.zero, BAR_W, BAR_H - 10, new Color(0.05f,0.05f,0.2f));
            _xpFill  = MakeBarFill(xpBG, new Color(0.3f,0.5f,1f), new Color(0.6f,0.8f,1f));

            // ── Level Badge ───────────────────────────────────────
            var lvlPanel = MakePanel(canvas.transform, new Vector2(PAD + 28, -PAD - 84),
                new Vector2(56, 56), new Color(0.1f,0.1f,0.3f,0.85f));
            lvlPanel.pivot = new Vector2(0, 1);
            lvlPanel.anchorMin = lvlPanel.anchorMax = new Vector2(0, 1);
            _levelText = MakeLabel(lvlPanel, "Lv1", Vector2.zero, 14, new Color(0.8f,0.9f,1f));
            _levelText.fontStyle = FontStyle.Bold;

            // ── Timer (top-center) ────────────────────────────────
            var timerPanel = MakePanel(canvas.transform, new Vector2(0, -PAD - 16),
                new Vector2(140, 40), new Color(0,0,0,0.5f));
            timerPanel.pivot = new Vector2(0.5f, 1);
            timerPanel.anchorMin = timerPanel.anchorMax = new Vector2(0.5f, 1);
            _timerText = MakeLabel(timerPanel, "00:00", Vector2.zero, 20, new Color(0.9f,0.9f,1f));
            _timerText.fontStyle = FontStyle.Bold;

            // ── Kill counter (top-right) ──────────────────────────
            var killPanel = MakePanel(canvas.transform, new Vector2(-PAD - 80, -PAD - 16),
                new Vector2(140, 36), new Color(0,0,0,0.5f));
            killPanel.pivot = new Vector2(1, 1);
            killPanel.anchorMin = killPanel.anchorMax = new Vector2(1, 1);
            _killText = MakeLabel(killPanel, "☠ 0", Vector2.zero, 14, new Color(0.9f,0.5f,0.3f));

            // ── Gold display (top-right, next to kills) ───────────
            var goldPanel = MakePanel(canvas.transform, new Vector2(-PAD - 240, -PAD - 16),
                new Vector2(140, 36), new Color(0,0,0,0.5f));
            goldPanel.pivot = new Vector2(1, 1);
            goldPanel.anchorMin = goldPanel.anchorMax = new Vector2(1, 1);
            _goldText = MakeLabel(goldPanel, "🪙 0", Vector2.zero, 14, new Color(0.97f,0.8f,0.2f));

            // ── Kill streak ───────────────────────────────────────
            var streakGO = new GameObject("StreakPanel");
            streakGO.transform.SetParent(canvas.transform, false);
            _streakGroup = streakGO.AddComponent<CanvasGroup>();
            _streakGroup.alpha = 0f;
            var streakRT = streakGO.AddComponent<RectTransform>();
            streakRT.anchorMin = streakRT.anchorMax = new Vector2(1, 0.5f);
            streakRT.pivot     = new Vector2(1, 0.5f);
            streakRT.anchoredPosition = new Vector2(-PAD, 0);
            streakRT.sizeDelta = new Vector2(160, 60);
            _streakText = MakeLabel(streakRT, "🔥 x3 STREAK!", Vector2.zero, 16, new Color(1f,0.7f,0.2f));
            _streakText.fontStyle = FontStyle.Bold;

            // ── Boss Health Bar (top-center, hidden) ──────────────
            _bossBar = new GameObject("BossBar");
            _bossBar.transform.SetParent(canvas.transform, false);
            _bossBar.SetActive(false);
            var bossRT = _bossBar.AddComponent<RectTransform>();
            bossRT.anchorMin = bossRT.anchorMax = new Vector2(0.5f, 1);
            bossRT.pivot     = new Vector2(0.5f, 1);
            bossRT.anchoredPosition = new Vector2(0, -52);
            bossRT.sizeDelta = new Vector2(500, 48);
            var bossBG = _bossBar.AddComponent<Image>();
            bossBG.color = new Color(0,0,0,0.7f);

            _bossNameText = MakeLabel(bossRT, "BOSS", new Vector2(0, 14), 14, new Color(1f,0.3f,0.3f));
            _bossNameText.fontStyle = FontStyle.Bold;
            var bossFillBG = MakeBarBG(bossRT, new Vector2(0, -8), 480, 16, new Color(0.3f,0,0,1f));
            _bossFill = MakeBarFill(bossFillBG, new Color(1f,0.2f,0.1f), new Color(1f,0.5f,0.5f));
            _bossHpText = MakeLabel(bossRT, "", new Vector2(0, -26), 9, Color.white);

            // ── Vignette overlay ──────────────────────────────────
            var vigGO = new GameObject("Vignette");
            vigGO.transform.SetParent(canvas.transform, false);
            var vigRT = vigGO.AddComponent<RectTransform>();
            vigRT.anchorMin = Vector2.zero; vigRT.anchorMax = Vector2.one;
            vigRT.sizeDelta = Vector2.zero;
            _vignetteImg = vigGO.AddComponent<Image>();
            _vignetteImg.color = new Color(0.5f,0,0,0);
            _vignetteImg.raycastTarget = false;

            // ── Level-up flash ────────────────────────────────────
            _levelUpPanel = new GameObject("LevelUpFlash");
            _levelUpPanel.transform.SetParent(canvas.transform, false);
            var luRT = _levelUpPanel.AddComponent<RectTransform>();
            luRT.anchorMin = luRT.anchorMax = new Vector2(0.5f, 0.5f);
            luRT.pivot     = new Vector2(0.5f, 0.5f);
            luRT.anchoredPosition = new Vector2(0, 80);
            luRT.sizeDelta = new Vector2(400, 80);
            _levelUpText  = MakeLabel(luRT, "LEVEL UP!", Vector2.zero, 36, new Color(1f,0.9f,0.3f));
            _levelUpText.fontStyle = FontStyle.Bold;
            _levelUpPanel.SetActive(false);

            // ── Active Skills Bar (bottom center) ──────────────────
            var skillBarPanel = MakePanel(canvas.transform, new Vector2(0f, 76f), new Vector2(400f, 60f), new Color(0f,0f,0f,0.3f));
            skillBarPanel.pivot = new Vector2(0.5f, 0f);
            skillBarPanel.anchorMin = skillBarPanel.anchorMax = new Vector2(0.5f, 0f);

            for (int i = 0; i < 6; i++)
            {
                var slotGO = new GameObject($"SkillSlot_{i}");
                slotGO.transform.SetParent(skillBarPanel, false);
                var slotRT = slotGO.AddComponent<RectTransform>();
                slotRT.anchorMin = slotRT.anchorMax = new Vector2(0.5f, 0.5f);
                slotRT.pivot = new Vector2(0.5f, 0.5f);
                slotRT.sizeDelta = new Vector2(60f, 60f);
                slotRT.anchoredPosition = new Vector2(-170f + i * 68f, 0f);

                var slotImg = slotGO.AddComponent<Image>();
                slotImg.color = new Color(0.06f, 0.07f, 0.14f, 0.9f);
                var outline = slotGO.AddComponent<Outline>();
                outline.effectColor = new Color(0.65f, 0.55f, 0.98f, 0.3f);
                outline.effectDistance = new Vector2(2f, 2f);

                // Icon image child
                var iconGO = new GameObject("Icon");
                iconGO.transform.SetParent(slotGO.transform, false);
                var iconRT = iconGO.AddComponent<RectTransform>();
                iconRT.anchorMin = Vector2.zero; iconRT.anchorMax = Vector2.one;
                iconRT.offsetMin = iconRT.offsetMax = new Vector2(4f, 4f);
                var iconImg = iconGO.AddComponent<Image>();

                // Cooldown overlay child
                var cdGO = new GameObject("CooldownOverlay");
                cdGO.transform.SetParent(slotGO.transform, false);
                var cdRT = cdGO.AddComponent<RectTransform>();
                cdRT.anchorMin = Vector2.zero; cdRT.anchorMax = Vector2.one;
                cdRT.offsetMin = cdRT.offsetMax = Vector2.zero;
                var cdImg = cdGO.AddComponent<Image>();
                cdImg.color = new Color(0f, 0f, 0f, 0.72f);
                cdImg.type = Image.Type.Filled;
                cdImg.fillMethod = Image.FillMethod.Radial360;
                cdImg.fillAmount = 0f;

                // Level text child
                var lvGO = new GameObject("LevelText");
                lvGO.transform.SetParent(slotGO.transform, false);
                var lvRT = lvGO.AddComponent<RectTransform>();
                lvRT.anchorMin = new Vector2(0f, 0f); lvRT.anchorMax = new Vector2(0f, 0f);
                lvRT.pivot = new Vector2(0f, 0f);
                lvRT.anchoredPosition = new Vector2(4f, 4f);
                lvRT.sizeDelta = new Vector2(40f, 15f);
                var lvTxt = lvGO.AddComponent<Text>();
                lvTxt.fontSize = 9;
                lvTxt.color = new Color(0.65f, 0.55f, 0.98f, 0.9f);
                lvTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                lvTxt.alignment = TextAnchor.LowerLeft;

                var slotComp = slotGO.AddComponent<SkillSlotUI>();
                slotComp.iconImage = iconImg;
                slotComp.cdOverlay = cdImg;
                slotComp.levelText = lvTxt;
                _skillSlots[i] = slotComp;
            }

            // ── Passive Items Bar (bottom center, under skills) ────
            var passiveBarPanel = MakePanel(canvas.transform, new Vector2(0f, 24f), new Vector2(260f, 38f), new Color(0f,0f,0f,0.3f));
            passiveBarPanel.pivot = new Vector2(0.5f, 0f);
            passiveBarPanel.anchorMin = passiveBarPanel.anchorMax = new Vector2(0.5f, 0f);

            for (int i = 0; i < 6; i++)
            {
                var slotGO = new GameObject($"PassiveSlot_{i}");
                slotGO.transform.SetParent(passiveBarPanel, false);
                var slotRT = slotGO.AddComponent<RectTransform>();
                slotRT.anchorMin = slotRT.anchorMax = new Vector2(0.5f, 0.5f);
                slotRT.pivot = new Vector2(0.5f, 0.5f);
                slotRT.sizeDelta = new Vector2(38f, 38f);
                slotRT.anchoredPosition = new Vector2(-110f + i * 44f, 0f);

                var slotImg = slotGO.AddComponent<Image>();
                slotImg.color = new Color(0.06f, 0.07f, 0.14f, 0.8f);
                var outline = slotGO.AddComponent<Outline>();
                outline.effectColor = new Color(0.2f, 0.83f, 0.6f, 0.2f);
                outline.effectDistance = new Vector2(1.5f, 1.5f);

                // Icon image child
                var iconGO = new GameObject("Icon");
                iconGO.transform.SetParent(slotGO.transform, false);
                var iconRT = iconGO.AddComponent<RectTransform>();
                iconRT.anchorMin = Vector2.zero; iconRT.anchorMax = Vector2.one;
                iconRT.offsetMin = iconRT.offsetMax = new Vector2(3f, 3f);
                var iconImg = iconGO.AddComponent<Image>();
                _passiveSlots[i] = iconImg;

                // Level text child
                var lvGO = new GameObject("LevelText");
                lvGO.transform.SetParent(slotGO.transform, false);
                var lvRT = lvGO.AddComponent<RectTransform>();
                lvRT.anchorMin = new Vector2(0f, 0f); lvRT.anchorMax = new Vector2(0f, 0f);
                lvRT.pivot = new Vector2(0f, 0f);
                lvRT.anchoredPosition = new Vector2(2f, 2f);
                lvRT.sizeDelta = new Vector2(30f, 12f);
                var lvTxt = lvGO.AddComponent<Text>();
                lvTxt.fontSize = 8;
                lvTxt.color = new Color(0.2f, 0.83f, 0.6f, 0.9f);
                lvTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                lvTxt.alignment = TextAnchor.LowerLeft;
                _passiveLevelTexts[i] = lvTxt;
            }
        }

        // ── Event Subscriptions ───────────────────────────────────
        private void SubscribeEvents()
        {
            PlayerController.OnHpChanged  += OnHpChanged;
            PlayerController.OnXpChanged  += OnXpChanged;
            GameManager.OnTimerUpdate     += OnTimer;
            GameManager.OnKillStreakUpdate += OnStreak;
            GameManager.OnLevelUp         += OnLevelUp;
        }

        private void OnDestroy()
        {
            PlayerController.OnHpChanged  -= OnHpChanged;
            PlayerController.OnXpChanged  -= OnXpChanged;
            GameManager.OnTimerUpdate     -= OnTimer;
            GameManager.OnKillStreakUpdate -= OnStreak;
            GameManager.OnLevelUp         -= OnLevelUp;
        }

        // ── Update ────────────────────────────────────────────────
        private void Update()
        {
            // Vignette fade
            if (_vignetteTimer > 0)
            {
                _vignetteTimer -= Time.deltaTime;
                float a = Mathf.Clamp01(_vignetteTimer / 0.3f) * 0.45f;
                if (_vignetteImg) _vignetteImg.color = new Color(0.5f, 0, 0, a);
            }

            // Level-up fade
            if (_levelUpTimer > 0)
            {
                _levelUpTimer -= Time.deltaTime;
                if (_levelUpTimer <= 0 && _levelUpPanel) _levelUpPanel.SetActive(false);
            }

            // Sync with PlayerController skills/passives/gold
            var pc = GameManager.Instance?.playerController;
            if (pc != null)
            {
                // Update active skills slots
                var sys = pc.skillSystem;
                if (sys != null)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        if (i < sys.Skills.Count)
                        {
                            _skillSlots[i].SetSkill(sys.Skills[i]);
                            _skillSlots[i].UpdateCooldown(sys.Skills[i], pc.CdMult);
                        }
                        else
                        {
                            _skillSlots[i].SetEmpty();
                        }
                    }
                }

                // Update passive items slots
                for (int i = 0; i < 6; i++)
                {
                    if (i < pc.ownedPassives.Count)
                    {
                        var p = pc.ownedPassives[i];
                        _passiveSlots[i].gameObject.SetActive(true);
                        _passiveSlots[i].sprite = p.icon;
                        _passiveSlots[i].color = p.iconColor;
                        _passiveLevelTexts[i].text = $"Lv{p.level}";
                    }
                    else
                    {
                        _passiveSlots[i].gameObject.SetActive(false);
                        _passiveLevelTexts[i].text = "";
                    }
                }

                // Update Gold
                if (_goldText)
                    _goldText.text = $"🪙 {GameManager.Instance.TotalGold}";
            }

            // Kill count
            if (_killText && GameManager.Instance)
                _killText.text = $"☠ {GameManager.Instance.TotalKills}";

            // Boss health (nearest boss)
            UpdateBossBar();
        }

        // ── Event Handlers ────────────────────────────────────────
        private void OnHpChanged(float hp, float maxHp)
        {
            if (_hpFill) _hpFill.localScale = new Vector3(Mathf.Clamp01(hp / maxHp), 1, 1);
            if (_hpText) _hpText.text = $"{Mathf.CeilToInt(hp)}/{Mathf.CeilToInt(maxHp)}";
        }

        private void OnXpChanged(int xp, int xpToNext)
        {
            if (_xpFill) _xpFill.localScale = new Vector3(Mathf.Clamp01((float)xp / xpToNext), 1, 1);
        }

        private void OnTimer(float elapsed)
        {
            int m = (int)(elapsed / 60), s = (int)(elapsed % 60);
            if (_timerText) _timerText.text = $"{m:D2}:{s:D2}";
        }

        private void OnStreak(int streak)
        {
            if (_streakText) _streakText.text = $"🔥 x{streak} STREAK!";
            if (_streakGroup)
            {
                StopAllCoroutines();
                StartCoroutine(FadeStreak(streak));
            }
        }

        private void OnLevelUp(int level)
        {
            if (_levelText) _levelText.text = $"Lv{level}";
            if (_levelUpPanel)
            {
                _levelUpText.text = $"⬆ LEVEL {level}!";
                _levelUpPanel.SetActive(true);
                _levelUpTimer = 2.5f;
            }
        }

        public void TriggerDamageVignette()
        {
            _vignetteTimer = 0.3f;
            CameraFX.Instance?.FlashDamage();
        }

        public void ShowBossWarning(string bossId)
        {
            StartCoroutine(BossWarningCo(bossId));
        }

        private IEnumerator BossWarningCo(string bossId)
        {
            if (_levelUpPanel)
            {
                _levelUpText.text = $"⚠ {bossId.ToUpper()} INCOMING!";
                _levelUpText.color = new Color(1f, 0.3f, 0.3f);
                _levelUpPanel.SetActive(true);
                yield return new WaitForSeconds(3f);
                _levelUpPanel.SetActive(false);
                _levelUpText.color = new Color(1f, 0.9f, 0.3f);
            }
        }

        public void ShowLegendaryUnlockBanner(string skillId)
        {
            StartCoroutine(LegendaryUnlockCo(skillId));
        }

        private IEnumerator LegendaryUnlockCo(string skillId)
        {
            if (_levelUpPanel)
            {
                string displayName = skillId.Replace("_", " ").ToUpper();
                if (UpgradeDatabase.Instance != null && UpgradeDatabase.Instance.allSkills != null)
                {
                    foreach (var sd in UpgradeDatabase.Instance.allSkills)
                    {
                        if (sd != null && sd.skillId == skillId)
                        {
                            displayName = sd.skillName.ToUpper();
                            break;
                        }
                    }
                }

                _levelUpText.text = $"🌟 COMBO: {displayName}! 🌟";
                _levelUpText.color = new Color(1f, 0.85f, 0.1f);
                _levelUpPanel.SetActive(true);
                yield return new WaitForSeconds(3.5f);
                _levelUpPanel.SetActive(false);
                _levelUpText.color = new Color(1f, 0.9f, 0.3f);
            }
        }

        private void UpdateBossBar()
        {
            if (_bossBar == null) return;
            foreach (var e in EnemySpawner.AllEnemies)
            {
                if (e == null || !e.isBoss || !e.IsAlive) continue;
                _bossBar.SetActive(true);
                if (_bossNameText) _bossNameText.text = e.EnemyName?.ToUpper() ?? "BOSS";
                if (_bossFill) _bossFill.localScale = new Vector3(Mathf.Clamp01(e.Hp / e.MaxHp), 1, 1);
                if (_bossHpText) _bossHpText.text = $"{Mathf.CeilToInt(e.Hp):N0} / {Mathf.CeilToInt(e.MaxHp):N0}";
                return;
            }
            _bossBar.SetActive(false);
        }

        private IEnumerator FadeStreak(int streak)
        {
            if (_streakGroup == null) yield break;
            if (streak < 3) { _streakGroup.alpha = 0f; yield break; }
            _streakGroup.alpha = 1f;
            yield return new WaitForSeconds(2f);
            float t = 0;
            while (t < 0.8f) { t += Time.deltaTime; _streakGroup.alpha = 1f - t / 0.8f; yield return null; }
            _streakGroup.alpha = 0f;
        }

        // ── UI Builders ───────────────────────────────────────────
        private static RectTransform MakePanel(Transform parent, Vector2 pos, Vector2 size, Color bg)
        {
            var go = new GameObject("Panel");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = bg;
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        private static RectTransform MakeBarBG(RectTransform parent, Vector2 pos, float w, float h, Color col)
        {
            var go = new GameObject("BarBG");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = col;
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        private static RectTransform MakeBarFill(RectTransform parent, Color colA, Color colB)
        {
            var go = new GameObject("Fill");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = colA;
            var rt  = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot     = new Vector2(0, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            // Set pivot to left for scaling
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f);
            rt.sizeDelta = new Vector2(parent.sizeDelta.x, parent.sizeDelta.y);
            rt.anchoredPosition = new Vector2(-parent.sizeDelta.x / 2f, 0);
            return rt;
        }

        private static Text MakeLabel(RectTransform parent, string text, Vector2 pos, int fontSize, Color col)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.text      = text;
            t.fontSize  = fontSize;
            t.color     = col;
            t.alignment = TextAnchor.MiddleCenter;
            t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(200, 30);
            return t;
        }
    }
}
