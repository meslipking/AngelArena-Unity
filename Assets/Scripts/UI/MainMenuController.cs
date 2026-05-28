using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using AngelArena.Data;
using AngelArena.Core;

namespace AngelArena.UI
{
    /// <summary>
    /// Main Menu: character selection, start, settings.
    /// Uses UnityEngine.UI.Text (no TMPro needed).
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Character Selection")]
        public CharacterData[]  characters;
        public Image            portraitImage;
        public Text             charNameText;
        public Text             charDescText;
        public Text             statHpText;
        public Text             statSpdText;
        public Text             statAtkText;
        public Text             statDefText;
        public Image            statHpFill;
        public Image            statSpdFill;
        public Image            statAtkFill;
        public Image            statDefFill;
        public Button[]         charButtons;
        public Image            selectedHighlight;

        [Header("Buttons")]
        public Button playButton;
        public Button settingsButton;
        public Button quitButton;

        [Header("Panels")]
        public GameObject settingsPanel;

        // ── State ────────────────────────────────────────────────
        private int _selectedIndex = 0;

        // ─────────────────────────────────────────────────────────
        private void Start()
        {
            playButton?.onClick.AddListener(StartGame);
            settingsButton?.onClick.AddListener(OpenSettings);
            quitButton?.onClick.AddListener(Application.Quit);

            for (int i = 0; i < charButtons.Length; i++)
            {
                int idx = i;
                charButtons[i]?.onClick.AddListener(() => SelectCharacter(idx));
            }

            if (characters != null && characters.Length > 0)
                SelectCharacter(0);
        }

        public void SelectCharacter(int index)
        {
            if (characters == null || index < 0 || index >= characters.Length) return;
            _selectedIndex = index;
            var data = characters[index];

            if (portraitImage)
            {
                portraitImage.sprite = data.portrait;
                portraitImage.color  = data.characterColor;
            }

            if (charNameText) charNameText.text = data.characterName;
            if (charDescText) charDescText.text = data.description;

            UpdateStat(statHpFill,  statHpText,  data.statPreview.hp,  "HP");
            UpdateStat(statSpdFill, statSpdText, data.statPreview.spd, "SPD");
            UpdateStat(statAtkFill, statAtkText, data.statPreview.atk, "ATK");
            UpdateStat(statDefFill, statDefText, data.statPreview.def, "DEF");

            if (selectedHighlight && index < charButtons.Length)
                selectedHighlight.transform.position = charButtons[index].transform.position;
        }

        private void UpdateStat(Image bar, Text label, int value, string name)
        {
            if (bar)   bar.fillAmount = value / 100f;
            if (label) label.text    = $"{name}: {value}";
        }

        public void StartGame()
        {
            if (characters == null || characters.Length == 0) return;
            GameManager.Instance?.StartGame(characters[_selectedIndex]);
            SceneManager.LoadScene("GameScene");
        }

        public void OpenSettings()
        {
            if (settingsPanel) settingsPanel.SetActive(!settingsPanel.activeSelf);
        }
    }
}
