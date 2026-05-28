using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AngelArena.Save;
using AngelArena.Audio;

namespace AngelArena.UI
{
    /// <summary>
    /// Permanent powerups upgrade lobby screen.
    /// Built procedurally with beautiful modern styling and sound effects.
    /// </summary>
    public class LobbyPowerupUI : MonoBehaviour
    {
        public static LobbyPowerupUI Instance { get; private set; }

        [System.Serializable]
        private class PowerupDef
        {
            public string fieldName;
            public string displayName;
            public string description;
            public string emoji;
            public int baseCost;
            public int maxLevel;

            public int GetCost(int currentLevel)
            {
                return baseCost * (currentLevel + 1);
            }

            public int GetTotalRefund(int currentLevel)
            {
                // Sum of baseCost * (k + 1) for k from 0 to currentLevel - 1
                // Formula: baseCost * currentLevel * (currentLevel + 1) / 2
                return baseCost * currentLevel * (currentLevel + 1) / 2;
            }
        }

        private readonly List<PowerupDef> _powerups = new()
        {
            new PowerupDef { fieldName = "might", displayName = "Might", description = "+5% sát thương mỗi cấp", emoji = "⚔️", baseCost = 200, maxLevel = 5 },
            new PowerupDef { fieldName = "magnet", displayName = "Magnet", description = "+80px phạm vi hút mỗi cấp", emoji = "🧲", baseCost = 150, maxLevel = 5 },
            new PowerupDef { fieldName = "swiftness", displayName = "Swiftness", description = "+2% tốc độ di chuyển mỗi cấp", emoji = "👟", baseCost = 180, maxLevel = 5 },
            new PowerupDef { fieldName = "recovery", displayName = "Recovery", description = "+0.5 HP/giây hồi phục mỗi cấp", emoji = "🩹", baseCost = 100, maxLevel = 5 },
            new PowerupDef { fieldName = "greed", displayName = "Greed", description = "+15% vàng nhận được mỗi cấp", emoji = "🪙", baseCost = 120, maxLevel = 5 },
            new PowerupDef { fieldName = "luck", displayName = "Luck", description = "+2% tỷ lệ bạo kích mỗi cấp", emoji = "🍀", baseCost = 200, maxLevel = 5 },
            new PowerupDef { fieldName = "cooldown", displayName = "Cooldown", description = "-2.5% thời gian hồi chiêu mỗi cấp", emoji = "⏱️", baseCost = 250, maxLevel = 5 },
            new PowerupDef { fieldName = "vitality", displayName = "Vitality", description = "+5% HP tối đa mỗi cấp", emoji = "💖", baseCost = 150, maxLevel = 5 },
            new PowerupDef { fieldName = "growth", displayName = "Growth", description = "+10% điểm kinh nghiệm mỗi cấp", emoji = "📈", baseCost = 200, maxLevel = 5 },
            new PowerupDef { fieldName = "amount", displayName = "Amount", description = "+1 số lượng đạn bắn ra mỗi cấp", emoji = "🔮", baseCost = 500, maxLevel = 5 }
        };

        [Header("UI Procedural References")]
        private GameObject _screenRoot;
        private Text _goldText;
        private List<Text> _cardLevelTexts = new();
        private List<Text> _cardCostTexts = new();
        private List<Button> _cardButtons = new();
        private List<Outline> _cardOutlines = new();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            _screenRoot = gameObject;
            BuildUIProcedurally();
        }

        public static void Show()
        {
            if (Instance == null)
            {
                // Create instance dynamically inside active Canvas
                var canvas = FindFirstObjectByType<Canvas>();
                if (canvas == null)
                {
                    var canvasGO = new GameObject("Canvas");
                    canvas = canvasGO.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvasGO.AddComponent<CanvasScaler>();
                    canvasGO.AddComponent<GraphicRaycaster>();
                }

                var uiGO = new GameObject("LobbyPowerupUI");
                uiGO.transform.SetParent(canvas.transform, false);
                uiGO.AddComponent<LobbyPowerupUI>();
            }
            else
            {
                Instance._screenRoot.SetActive(true);
                Instance.RefreshAll();
            }
        }

        private void BuildUIProcedurally()
        {
            var rootRT = GetComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.sizeDelta = Vector2.zero;

            // Semi-transparent backdrop
            var bgImg = gameObject.AddComponent<Image>();
            bgImg.color = new Color(0.02f, 0.02f, 0.04f, 0.85f);

            // Center Panel
            var panelGO = new GameObject("CenterPanel");
            panelGO.transform.SetParent(transform, false);
            var panelRT = panelGO.AddComponent<RectTransform>();
            panelRT.anchorMin = panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = new Vector2(820f, 620f);

            var panelImg = panelGO.AddComponent<Image>();
            panelImg.color = new Color(0.04f, 0.04f, 0.08f, 0.98f);
            var panelOutline = panelGO.AddComponent<Outline>();
            panelOutline.effectColor = new Color(0.65f, 0.55f, 0.98f, 0.4f);
            panelOutline.effectDistance = new Vector2(2f, 2f);

            // Title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(panelGO.transform, false);
            var titleTxt = titleGO.AddComponent<Text>();
            titleTxt.text = "⚔️ CƯỜNG HÓA VĨNH VIỄN ⚔️";
            titleTxt.fontSize = 28;
            titleTxt.color = new Color(0.65f, 0.55f, 0.98f);
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.fontStyle = FontStyle.Bold;
            var titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = titleRT.anchorMax = new Vector2(0.5f, 1f);
            titleRT.pivot = new Vector2(0.5f, 1f);
            titleRT.anchoredPosition = new Vector2(0f, -20f);
            titleRT.sizeDelta = new Vector2(600f, 40f);

            // Gold Display
            var goldGO = new GameObject("GoldDisplay");
            goldGO.transform.SetParent(panelGO.transform, false);
            _goldText = goldGO.AddComponent<Text>();
            _goldText.text = "🪙 VÀNG: 0";
            _goldText.fontSize = 20;
            _goldText.color = new Color(1f, 0.77f, 0.2f);
            _goldText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _goldText.alignment = TextAnchor.MiddleCenter;
            _goldText.fontStyle = FontStyle.Bold;
            var goldRT = goldGO.GetComponent<RectTransform>();
            goldRT.anchorMin = goldRT.anchorMax = new Vector2(0.5f, 1f);
            goldRT.pivot = new Vector2(0.5f, 1f);
            goldRT.anchoredPosition = new Vector2(0f, -65f);
            goldRT.sizeDelta = new Vector2(400f, 30f);

            // Cards Container (Scroll View alternative or direct Grid for stability)
            var gridGO = new GameObject("Grid");
            gridGO.transform.SetParent(panelGO.transform, false);
            var gridRT = gridGO.AddComponent<RectTransform>();
            gridRT.anchorMin = new Vector2(0.5f, 0.5f);
            gridRT.anchorMax = new Vector2(0.5f, 0.5f);
            gridRT.pivot = new Vector2(0.5f, 0.5f);
            gridRT.anchoredPosition = new Vector2(0f, -30f);
            gridRT.sizeDelta = new Vector2(780f, 440f);

            // Procedurally build the 10 upgrade cards
            for (int i = 0; i < _powerups.Count; i++)
            {
                int index = i;
                var def = _powerups[index];

                int row = index / 2;
                int col = index % 2;

                var cardGO = new GameObject($"PowerupCard_{def.fieldName}");
                cardGO.transform.SetParent(gridRT, false);
                var cardRT = cardGO.AddComponent<RectTransform>();
                cardRT.anchorMin = cardRT.anchorMax = new Vector2(0.5f, 0.5f);
                cardRT.pivot = new Vector2(0.5f, 0.5f);
                cardRT.sizeDelta = new Vector2(370f, 76f);

                // Grid layout placement
                float x = -195f + col * 390f;
                float y = 160f - row * 82f;
                cardRT.anchoredPosition = new Vector2(x, y);

                // Background
                var cardImg = cardGO.AddComponent<Image>();
                cardImg.color = new Color(0.06f, 0.06f, 0.12f, 0.95f);
                var cardOutline = cardGO.AddComponent<Outline>();
                cardOutline.effectColor = new Color(0.65f, 0.55f, 0.98f, 0.15f);
                cardOutline.effectDistance = new Vector2(1.5f, 1.5f);
                _cardOutlines.Add(cardOutline);

                // Emoji Icon
                var emojiGO = new GameObject("Emoji");
                emojiGO.transform.SetParent(cardGO.transform, false);
                var emojiTxt = emojiGO.AddComponent<Text>();
                emojiTxt.text = def.emoji;
                emojiTxt.fontSize = 28;
                emojiTxt.alignment = TextAnchor.MiddleCenter;
                emojiTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                var emojiRT = emojiGO.GetComponent<RectTransform>();
                emojiRT.anchorMin = emojiRT.anchorMax = new Vector2(0f, 0.5f);
                emojiRT.pivot = new Vector2(0f, 0.5f);
                emojiRT.anchoredPosition = new Vector2(12f, 0f);
                emojiRT.sizeDelta = new Vector2(40f, 40f);

                // Name & Level Text
                var infoGO = new GameObject("InfoText");
                infoGO.transform.SetParent(cardGO.transform, false);
                var nameTxt = infoGO.AddComponent<Text>();
                nameTxt.fontSize = 14;
                nameTxt.color = Color.white;
                nameTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                nameTxt.fontStyle = FontStyle.Bold;
                var infoRT = infoGO.GetComponent<RectTransform>();
                infoRT.anchorMin = new Vector2(0f, 0.5f);
                infoRT.anchorMax = new Vector2(1f, 0.5f);
                infoRT.pivot = new Vector2(0f, 0.5f);
                infoRT.anchoredPosition = new Vector2(60f, 14f);
                infoRT.sizeDelta = new Vector2(-170f, 20f);
                _cardLevelTexts.Add(nameTxt);

                // Description Text
                var descGO = new GameObject("DescriptionText");
                descGO.transform.SetParent(cardGO.transform, false);
                var descTxt = descGO.AddComponent<Text>();
                descTxt.text = def.description;
                descTxt.fontSize = 10;
                descTxt.color = new Color(0.7f, 0.7f, 0.8f);
                descTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                var descRT = descGO.GetComponent<RectTransform>();
                descRT.anchorMin = new Vector2(0f, 0.5f);
                descRT.anchorMax = new Vector2(1f, 0.5f);
                descRT.pivot = new Vector2(0f, 0.5f);
                descRT.anchoredPosition = new Vector2(60f, -12f);
                descRT.sizeDelta = new Vector2(-170f, 24f);

                // Purchase Button
                var btnGO = new GameObject("BuyButton");
                btnGO.transform.SetParent(cardGO.transform, false);
                var btnRT = btnGO.AddComponent<RectTransform>();
                btnRT.anchorMin = btnRT.anchorMax = new Vector2(1f, 0.5f);
                btnRT.pivot = new Vector2(1f, 0.5f);
                btnRT.anchoredPosition = new Vector2(-12f, 0f);
                btnRT.sizeDelta = new Vector2(90f, 42f);

                var btnImg = btnGO.AddComponent<Image>();
                btnImg.color = new Color(0.12f, 0.1f, 0.22f);
                var btnOutline = btnGO.AddComponent<Outline>();
                btnOutline.effectColor = new Color(0.65f, 0.55f, 0.98f, 0.5f);
                btnOutline.effectDistance = new Vector2(1f, 1f);

                var btnTextGO = new GameObject("ButtonText");
                btnTextGO.transform.SetParent(btnGO.transform, false);
                var btnText = btnTextGO.AddComponent<Text>();
                btnText.fontSize = 11;
                btnText.color = new Color(1f, 0.8f, 0.2f);
                btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                btnText.fontStyle = FontStyle.Bold;
                btnText.alignment = TextAnchor.MiddleCenter;
                var btnTextRT = btnTextGO.GetComponent<RectTransform>();
                btnTextRT.anchorMin = Vector2.zero;
                btnTextRT.anchorMax = Vector2.one;
                btnTextRT.sizeDelta = Vector2.zero;
                _cardCostTexts.Add(btnText);

                var btn = btnGO.AddComponent<Button>();
                btn.onClick.AddListener(() => PurchaseUpgrade(index));
                _cardButtons.Add(btn);

                // Interactive Hover
                cardGO.AddComponent<LobbyHoverFX>();
            }

            // Bottom Buttons Container
            var footerGO = new GameObject("Footer");
            footerGO.transform.SetParent(panelGO.transform, false);
            var footerRT = footerGO.AddComponent<RectTransform>();
            footerRT.anchorMin = footerRT.anchorMax = new Vector2(0.5f, 0f);
            footerRT.pivot = new Vector2(0.5f, 0f);
            footerRT.anchoredPosition = new Vector2(0f, 15f);
            footerRT.sizeDelta = new Vector2(780f, 50f);

            // Reset Button
            var resetGO = new GameObject("ResetButton");
            resetGO.transform.SetParent(footerRT, false);
            var resetRT = resetGO.AddComponent<RectTransform>();
            resetRT.anchorMin = resetRT.anchorMax = new Vector2(0f, 0.5f);
            resetRT.pivot = new Vector2(0f, 0.5f);
            resetRT.anchoredPosition = new Vector2(12f, 0f);
            resetRT.sizeDelta = new Vector2(180f, 40f);

            var resetImg = resetGO.AddComponent<Image>();
            resetImg.color = new Color(0.2f, 0.05f, 0.05f);
            var resetOutline = resetGO.AddComponent<Outline>();
            resetOutline.effectColor = new Color(0.9f, 0.2f, 0.2f, 0.5f);

            var resetTextGO = new GameObject("ResetText");
            resetTextGO.transform.SetParent(resetGO.transform, false);
            var resetTxt = resetTextGO.AddComponent<Text>();
            resetTxt.text = "♻️ Tẩy Điểm (Hoàn Vàng)";
            resetTxt.fontSize = 12;
            resetTxt.color = new Color(0.95f, 0.7f, 0.7f);
            resetTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            resetTxt.fontStyle = FontStyle.Bold;
            resetTxt.alignment = TextAnchor.MiddleCenter;
            var resetTextRT = resetTextGO.GetComponent<RectTransform>();
            resetTextRT.anchorMin = Vector2.zero;
            resetTextRT.anchorMax = Vector2.one;
            resetTextRT.sizeDelta = Vector2.zero;

            var resetBtn = resetGO.AddComponent<Button>();
            resetBtn.onClick.AddListener(ResetAllUpgrades);

            // Close Button
            var closeGO = new GameObject("CloseButton");
            closeGO.transform.SetParent(footerRT, false);
            var closeRT = closeGO.AddComponent<RectTransform>();
            closeRT.anchorMin = closeRT.anchorMax = new Vector2(1f, 0.5f);
            closeRT.pivot = new Vector2(1f, 0.5f);
            closeRT.anchoredPosition = new Vector2(-12f, 0f);
            closeRT.sizeDelta = new Vector2(150f, 40f);

            var closeImg = closeGO.AddComponent<Image>();
            closeImg.color = new Color(0.1f, 0.12f, 0.15f);
            var closeOutline = closeGO.AddComponent<Outline>();
            closeOutline.effectColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);

            var closeTextGO = new GameObject("CloseText");
            closeTextGO.transform.SetParent(closeGO.transform, false);
            var closeTxt = closeTextGO.AddComponent<Text>();
            closeTxt.text = "Đóng ✖️";
            closeTxt.fontSize = 13;
            closeTxt.color = Color.white;
            closeTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            closeTxt.fontStyle = FontStyle.Bold;
            closeTxt.alignment = TextAnchor.MiddleCenter;
            var closeTextRT = closeTextGO.GetComponent<RectTransform>();
            closeTextRT.anchorMin = Vector2.zero;
            closeTextRT.anchorMax = Vector2.one;
            closeTextRT.sizeDelta = Vector2.zero;

            var closeBtn = closeGO.AddComponent<Button>();
            closeBtn.onClick.AddListener(CloseScreen);

            // Refresh UI once loaded
            RefreshAll();
        }

        private void RefreshAll()
        {
            var save = SaveSystem.Instance?.CurrentSave;
            if (save == null) return;

            // Update gold text
            _goldText.text = $"🪙 VÀNG HIỆN CÓ: {save.gold:N0}";

            // Update cards
            for (int i = 0; i < _powerups.Count; i++)
            {
                var def = _powerups[i];
                int level = GetPowerupLevel(def.fieldName);

                // Level text
                _cardLevelTexts[i].text = $"{def.displayName} (Cấp {level}/{def.maxLevel})";

                if (level >= def.maxLevel)
                {
                    _cardCostTexts[i].text = "TỐI ĐA";
                    _cardCostTexts[i].color = new Color(0.3f, 0.85f, 0.3f);
                    _cardButtons[i].interactable = false;
                    _cardOutlines[i].effectColor = new Color(0.3f, 0.85f, 0.3f, 0.4f);
                }
                else
                {
                    int cost = def.GetCost(level);
                    _cardCostTexts[i].text = $"🪙 {cost:N0}";

                    if (save.gold >= cost)
                    {
                        _cardCostTexts[i].color = new Color(1f, 0.8f, 0.2f);
                        _cardButtons[i].interactable = true;
                        _cardOutlines[i].effectColor = new Color(0.65f, 0.55f, 0.98f, 0.5f);
                    }
                    else
                    {
                        _cardCostTexts[i].color = new Color(0.8f, 0.3f, 0.3f);
                        _cardButtons[i].interactable = false;
                        _cardOutlines[i].effectColor = new Color(0.8f, 0.3f, 0.3f, 0.2f);
                    }
                }
            }
        }

        private void PurchaseUpgrade(int index)
        {
            if (index < 0 || index >= _powerups.Count) return;

            var save = SaveSystem.Instance?.CurrentSave;
            if (save == null) return;

            var def = _powerups[index];
            int level = GetPowerupLevel(def.fieldName);

            if (level >= def.maxLevel) return;

            int cost = def.GetCost(level);
            if (save.gold >= cost)
            {
                save.gold -= cost;
                SetPowerupLevel(def.fieldName, level + 1);

                // Save immediately
                SaveSystem.Instance.Save();

                // Play Audio feedback
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxLevelUp);
                }

                // Refresh UI
                RefreshAll();
            }
        }

        private void ResetAllUpgrades()
        {
            var save = SaveSystem.Instance?.CurrentSave;
            if (save == null) return;

            int refundAmount = 0;
            foreach (var def in _powerups)
            {
                int level = GetPowerupLevel(def.fieldName);
                refundAmount += def.GetTotalRefund(level);
                SetPowerupLevel(def.fieldName, 0);
            }

            save.gold += refundAmount;
            SaveSystem.Instance.Save();

            // Play audio feedback
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxLevelUp);
            }

            RefreshAll();
            Debug.Log($"[LobbyPowerupUI] Reset all powerups! Refunded {refundAmount:N0} Gold.");
        }

        private void CloseScreen()
        {
            // Play simple click sound if possible
            if (AudioManager.Instance != null && AudioManager.Instance.sfxLevelUp != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxLevelUp, 0.5f);
            }

            gameObject.SetActive(false);
        }

        private int GetPowerupLevel(string fieldName)
        {
            var save = SaveSystem.Instance?.CurrentSave;
            if (save == null) return 0;
            var pu = save.powerups;
            switch (fieldName)
            {
                case "might":     return pu.might;
                case "amount":    return pu.amount;
                case "swiftness": return pu.swiftness;
                case "recovery":  return pu.recovery;
                case "greed":     return pu.greed;
                case "luck":      return pu.luck;
                case "cooldown":  return pu.cooldown;
                case "vitality":  return pu.vitality;
                case "growth":    return pu.growth;
                case "magnet":    return pu.magnet;
            }
            return 0;
        }

        private void SetPowerupLevel(string fieldName, int val)
        {
            var save = SaveSystem.Instance?.CurrentSave;
            if (save == null) return;
            var pu = save.powerups;
            switch (fieldName)
            {
                case "might":     pu.might     = val; break;
                case "amount":    pu.amount    = val; break;
                case "swiftness": pu.swiftness = val; break;
                case "recovery":  pu.recovery  = val; break;
                case "greed":     pu.greed     = val; break;
                case "luck":      pu.luck      = val; break;
                case "cooldown":  pu.cooldown  = val; break;
                case "vitality":  pu.vitality  = val; break;
                case "growth":    pu.growth    = val; break;
                case "magnet":    pu.magnet    = val; break;
            }
        }
    }

    public class LobbyHoverFX : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
    {
        private Vector3 _originalScale;
        private Outline _outline;
        private Color _origColor;

        private void Start()
        {
            _originalScale = transform.localScale;
            _outline = GetComponent<Outline>();
            if (_outline) _origColor = _outline.effectColor;
        }

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
        {
            transform.localScale = _originalScale * 1.02f;
            if (_outline) _outline.effectColor = new Color(_origColor.r, _origColor.g, _origColor.b, 0.8f);
        }

        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            transform.localScale = _originalScale;
            if (_outline) _outline.effectColor = _origColor;
        }
    }
}
