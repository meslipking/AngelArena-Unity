using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using AngelArena.Core;

namespace AngelArena.UI
{
    /// <summary>
    /// Game Over / Victory result screen.
    /// Uses UnityEngine.UI.Text (no TMPro needed).
    /// </summary>
    public class GameResultScreen : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject gameOverPanel;
        public GameObject victoryPanel;

        [Header("Game Over Stats")]
        public Text goTimeText;
        public Text goKillsText;
        public Text goLevelText;
        public Text goGoldText;

        [Header("Victory Stats")]
        public Text vicTimeText;
        public Text vicKillsText;
        public Text vicLevelText;
        public Text vicGoldText;

        [Header("Buttons")]
        public Button goRetryButton;
        public Button goMenuButton;
        public Button vicMenuButton;

        [Header("VFX")]
        public ParticleSystem victoryConfetti;
        public AudioClip      gameOverSFX;
        public AudioClip      victorySFX;
        private AudioSource   _audio;

        // ─────────────────────────────────────────────────────────
        private void Awake()
        {
            _audio = GetComponent<AudioSource>();
            if (gameOverPanel) gameOverPanel.SetActive(false);
            if (victoryPanel)  victoryPanel.SetActive(false);

            GameManager.OnGameOver += ShowGameOver;
            GameManager.OnVictory  += ShowVictory;

            goRetryButton?.onClick.AddListener(Retry);
            goMenuButton?.onClick.AddListener(ReturnToMenu);
            vicMenuButton?.onClick.AddListener(ReturnToMenu);
        }

        private void OnDestroy()
        {
            GameManager.OnGameOver -= ShowGameOver;
            GameManager.OnVictory  -= ShowVictory;
        }

        private void ShowGameOver()
        {
            if (gameOverPanel) gameOverPanel.SetActive(true);
            StartCoroutine(PopulateStats(goTimeText, goKillsText, goLevelText, goGoldText));
            if (gameOverSFX && _audio) _audio.PlayOneShot(gameOverSFX);
            CameraController.Instance?.Shake(20f, 0.8f);
        }

        private void ShowVictory()
        {
            if (victoryPanel) victoryPanel.SetActive(true);
            StartCoroutine(PopulateStats(vicTimeText, vicKillsText, vicLevelText, vicGoldText));
            if (victoryConfetti) victoryConfetti.Play();
            if (victorySFX && _audio) _audio.PlayOneShot(victorySFX);
        }

        private IEnumerator PopulateStats(Text timeT, Text killT, Text levelT, Text goldT)
        {
            yield return new WaitForSecondsRealtime(0.5f);

            var gm  = GameManager.Instance;
            var pc  = gm?.playerController;
            float e = gm?.ElapsedSeconds ?? 0;
            int min = (int)(e / 60), sec = (int)(e % 60);

            if (timeT)  timeT.text  = $"Time: {min}:{sec:00}";
            if (killT)  killT.text  = $"Kills: {gm?.TotalKills ?? 0}";
            if (levelT) levelT.text = $"Level: {pc?.Level ?? 1}";
            if (goldT)  goldT.text  = $"Gold: {gm?.TotalGold ?? 0}";
        }

        private void Retry()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
        }

        private void ReturnToMenu()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}
