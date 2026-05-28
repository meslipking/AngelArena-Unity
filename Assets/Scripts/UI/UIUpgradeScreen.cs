using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using AngelArena.Core;

namespace AngelArena.Core
{
    /// <summary>
    /// Upgrade level-up screen. Uses UnityEngine.UI.Text (no TMPro needed).
    /// </summary>
    public class UIUpgradeScreen : MonoBehaviour
    {
        public static UIUpgradeScreen Instance { get; private set; }

        [Header("UI References")]
        public GameObject  screenRoot;
        public UpgradeCardUI[] cards; // 3 cards

        private System.Action[] _pendingActions = new System.Action[3];
        private SkillSystem     _skillSystem;

        private void Awake()
        {
            Instance = this;
            if (screenRoot == null) screenRoot = gameObject;
            BuildCardsProcedurally();
        }

        private void BuildCardsProcedurally()
        {
            // Title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(transform, false);
            var titleTxt = titleGO.AddComponent<Text>();
            titleTxt.text = "NÂNG CẤP NHÂN VẬT";
            titleTxt.fontSize = 32;
            titleTxt.color = new Color(0.65f, 0.55f, 0.98f);
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.fontStyle = FontStyle.Bold;
            var titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = titleRT.anchorMax = new Vector2(0.5f, 0.5f);
            titleRT.anchoredPosition = new Vector2(0f, 220f);
            titleRT.sizeDelta = new Vector2(600f, 50f);

            // Subtitle
            var subGO = new GameObject("Subtitle");
            subGO.transform.SetParent(transform, false);
            var subTxt = subGO.AddComponent<Text>();
            subTxt.text = "Chọn 1 nâng cấp để tiếp tục trận chiến";
            subTxt.fontSize = 14;
            subTxt.color = new Color(0.7f, 0.7f, 0.8f);
            subTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subTxt.alignment = TextAnchor.MiddleCenter;
            var subRT = subGO.GetComponent<RectTransform>();
            subRT.anchorMin = subRT.anchorMax = new Vector2(0.5f, 0.5f);
            subRT.anchoredPosition = new Vector2(0f, 170f);
            subRT.sizeDelta = new Vector2(600f, 30f);

            // Container for cards
            var containerGO = new GameObject("CardsContainer");
            containerGO.transform.SetParent(transform, false);
            var containerRT = containerGO.AddComponent<RectTransform>();
            containerRT.anchorMin = containerRT.anchorMax = new Vector2(0.5f, 0.5f);
            containerRT.pivot = new Vector2(0.5f, 0.5f);
            containerRT.anchoredPosition = new Vector2(0f, -30f);
            containerRT.sizeDelta = new Vector2(760f, 320f);

            cards = new UpgradeCardUI[3];
            for (int i = 0; i < 3; i++)
            {
                var cardGO = new GameObject($"UpgradeCard_{i}");
                cardGO.transform.SetParent(containerRT, false);
                var cardRT = cardGO.AddComponent<RectTransform>();
                cardRT.anchorMin = cardRT.anchorMax = new Vector2(0.5f, 0.5f);
                cardRT.pivot = new Vector2(0.5f, 0.5f);
                cardRT.sizeDelta = new Vector2(220f, 290f);
                cardRT.anchoredPosition = new Vector2(-240f + i * 240f, 0f);

                // Background
                var bgImg = cardGO.AddComponent<Image>();
                bgImg.color = new Color(0.05f, 0.06f, 0.12f, 0.9f);
                var outline = cardGO.AddComponent<Outline>();
                outline.effectColor = new Color(0.65f, 0.55f, 0.98f, 0.25f);
                outline.effectDistance = new Vector2(2f, 2f);

                // Icon / Emoji text
                var iconGO = new GameObject("Icon");
                iconGO.transform.SetParent(cardGO.transform, false);
                var iconTxt = iconGO.AddComponent<Text>();
                iconTxt.text = "🔮";
                iconTxt.fontSize = 54;
                iconTxt.alignment = TextAnchor.MiddleCenter;
                iconTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                var iconRT = iconGO.GetComponent<RectTransform>();
                iconRT.anchorMin = iconRT.anchorMax = new Vector2(0.5f, 0.5f);
                iconRT.anchoredPosition = new Vector2(0f, 80f);
                iconRT.sizeDelta = new Vector2(100f, 80f);

                // Title Label
                var labelGO = new GameObject("Title");
                labelGO.transform.SetParent(cardGO.transform, false);
                var labelTxt = labelGO.AddComponent<Text>();
                labelTxt.fontSize = 15;
                labelTxt.color = Color.white;
                labelTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                labelTxt.alignment = TextAnchor.MiddleCenter;
                labelTxt.fontStyle = FontStyle.Bold;
                var labelRT = labelGO.GetComponent<RectTransform>();
                labelRT.anchorMin = labelRT.anchorMax = new Vector2(0.5f, 0.5f);
                labelRT.anchoredPosition = new Vector2(0f, 20f);
                labelRT.sizeDelta = new Vector2(200f, 30f);

                // Type Label
                var typeGO = new GameObject("Type");
                typeGO.transform.SetParent(cardGO.transform, false);
                var typeTxt = typeGO.AddComponent<Text>();
                typeTxt.fontSize = 9;
                typeTxt.color = new Color(0.65f, 0.55f, 0.98f);
                typeTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                typeTxt.alignment = TextAnchor.MiddleCenter;
                var typeRT = typeGO.GetComponent<RectTransform>();
                typeRT.anchorMin = typeRT.anchorMax = new Vector2(0.5f, 0.5f);
                typeRT.anchoredPosition = new Vector2(0f, -15f);
                typeRT.sizeDelta = new Vector2(180f, 20f);

                // Description Label
                var descGO = new GameObject("Description");
                descGO.transform.SetParent(cardGO.transform, false);
                var descTxt = descGO.AddComponent<Text>();
                descTxt.fontSize = 11;
                descTxt.color = new Color(0.7f, 0.7f, 0.8f);
                descTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                descTxt.alignment = TextAnchor.MiddleCenter;
                var descRT = descGO.GetComponent<RectTransform>();
                descRT.anchorMin = descRT.anchorMax = new Vector2(0.5f, 0.5f);
                descRT.anchoredPosition = new Vector2(0f, -70f);
                descRT.sizeDelta = new Vector2(180f, 65f);

                // Button listener
                var btn = cardGO.AddComponent<Button>();
                int index = i;
                btn.onClick.AddListener(() => OnCardSelected(index));

                // Hover effects
                cardGO.AddComponent<HoverCardFX>();

                var cardComp = cardGO.AddComponent<UpgradeCardUI>();
                cardComp.titleText = labelTxt;
                cardComp.descText = descTxt;
                cardComp.typeText = typeTxt;
                cardComp.iconText = iconTxt;
                cardComp.cardOutline = outline;
                cards[i] = cardComp;
            }
        }

        public static void Show(int level, SkillSystem skillSystem)
        {
            if (Instance == null) return;
            Instance._skillSystem = skillSystem;
            Instance.BuildChoices(level);
            Instance.screenRoot?.SetActive(true);
            Time.timeScale = 0f;
        }

        private void BuildChoices(int level)
        {
            var player = GameManager.Instance?.playerController;
            var db     = UI.UpgradeDatabase.Instance;
            if (db == null || player == null) return;

            var skills  = db.GetSkillChoices(_skillSystem, player);
            var passives= db.GetPassiveChoices(player);
            var stats   = db.GetStatBoosts(player);

            var selected = new System.Collections.Generic.List<UpgradeChoice>
            {
                PickRandom(skills)  ?? PickRandom(stats),
                PickRandom(passives)?? PickRandom(stats),
                null
            };

            var remaining = new System.Collections.Generic.List<UpgradeChoice>();
            remaining.AddRange(skills);
            remaining.AddRange(passives);
            remaining.RemoveAll(c => selected.Contains(c));
            selected[2] = PickRandom(remaining) ?? PickRandom(stats);

            for (int i = 0; i < cards.Length && i < selected.Count; i++)
            {
                if (selected[i] == null) continue;
                cards[i]?.SetChoice(selected[i]);
                int idx = i;
                _pendingActions[idx] = selected[i].apply;
            }
        }

        public void OnCardSelected(int index)
        {
            if (index < 0 || index >= _pendingActions.Length) return;
            _pendingActions[index]?.Invoke();
            screenRoot?.SetActive(false);
            Time.timeScale = 1f;
        }

        private UpgradeChoice PickRandom(System.Collections.Generic.List<UpgradeChoice> pool)
        {
            if (pool == null || pool.Count == 0) return null;
            float total = 0;
            foreach (var c in pool) total += c.weight;
            float r = Random.value * total;
            foreach (var c in pool) { r -= c.weight; if (r <= 0) return c; }
            return pool[pool.Count - 1];
        }
    }

    public class HoverCardFX : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
    {
        private Vector3 _originalScale;
        private Outline _outline;
        private Color   _origColor;

        private void Start()
        {
            _originalScale = transform.localScale;
            _outline = GetComponent<Outline>();
            if (_outline) _origColor = _outline.effectColor;
        }

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
        {
            transform.localScale = _originalScale * 1.05f;
            if (_outline) _outline.effectColor = new Color(_origColor.r, _origColor.g, _origColor.b, 0.9f);
        }

        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            transform.localScale = _originalScale;
            if (_outline) _outline.effectColor = _origColor;
        }
    }

    public enum UpgradeChoiceType { NewSkill, UpgradeSkill, NewPassive, UpgradePassive, StatBoost }

    [System.Serializable]
    public class UpgradeChoice
    {
        public string        label;
        public string        description;
        public Sprite        icon;
        public Color         color;
        public float         weight = 1f;
        public UpgradeChoiceType type;
        public System.Action apply;
    }
}
