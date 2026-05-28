using System;
using System.Collections.Generic;
using UnityEngine;
using AngelArena.Data;

namespace AngelArena.Core
{
    /// <summary>
    /// Central game manager: handles game state, timers, references.
    /// Singleton — access via GameManager.Instance.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // ── Events ──────────────────────────────────────────────
        public static Action<int>   OnLevelUp;         // int = new level
        public static Action<float> OnTimerUpdate;     // float = elapsed seconds
        public static Action        OnGameOver;
        public static Action        OnVictory;
        public static Action<int>   OnKillStreakUpdate; // int = streak count

        // ── State ────────────────────────────────────────────────
        public GameState State { get; private set; } = GameState.Menu;
        public float  ElapsedSeconds  { get; private set; }
        public int    TotalKills      { get; private set; }
        public int    TotalGold       { get; private set; }
        public int    KillStreak      { get; private set; }
        public bool   GameRunning     => State == GameState.Playing;

        [Header("References")]
        public PlayerController   playerController;
        public EnemySpawner       enemySpawner;
        public HUDManager         hudManager;

        [Header("Session")]
        public CharacterData      selectedCharacter;

        // ── Kill streak tracking ─────────────────────────────────
        private float _lastKillTime;
        private const float STREAK_WINDOW = 4f;

        // ── Internal ─────────────────────────────────────────────
        private bool _victoryTriggered;

        // ─────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Auto-start game if we're directly in GameScene (no MainMenu flow)
            if (State == GameState.Menu)
            {
                // If playerController is already in scene, start with defaults
                if (playerController != null)
                {
                    if (selectedCharacter != null)
                        playerController.InitFromData(selectedCharacter);
                    else
                        playerController.InitDefaults(); // fallback default stats

                    State = GameState.Playing;
                    Debug.Log("[AngelArena] Auto-started game in GameScene");
                }
            }
        }

        private void Update()
        {
            if (State != GameState.Playing) return;

            ElapsedSeconds += Time.deltaTime;
            OnTimerUpdate?.Invoke(ElapsedSeconds);

            // Victory at 30 minutes
            if (!_victoryTriggered && ElapsedSeconds >= 1800f)
            {
                _victoryTriggered = true;
                TriggerVictory();
            }

            // Expire kill streak
            if (KillStreak > 0 && Time.time - _lastKillTime > STREAK_WINDOW)
            {
                KillStreak = 0;
                OnKillStreakUpdate?.Invoke(0);
            }
        }

        // ── Public API ───────────────────────────────────────────
        public void StartGame(CharacterData character)
        {
            selectedCharacter = character;
            ElapsedSeconds    = 0;
            TotalKills        = 0;
            TotalGold         = 0;
            KillStreak        = 0;
            _victoryTriggered = false;
            State             = GameState.Playing;
        }

        public void PauseGame()
        {
            if (State != GameState.Playing) return;
            State         = GameState.Paused;
            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            if (State != GameState.Paused) return;
            State         = GameState.Playing;
            Time.timeScale = 1f;
        }

        public void RegisterKill(int xp, int gold)
        {
            TotalKills++;
            TotalGold += gold;

            // Kill streak
            float now = Time.time;
            KillStreak = (now - _lastKillTime < STREAK_WINDOW) ? KillStreak + 1 : 1;
            _lastKillTime = now;
            OnKillStreakUpdate?.Invoke(KillStreak);

            // XP to player
            playerController?.GainXp(xp);
        }

        public void TriggerGameOver()
        {
            if (State == GameState.GameOver) return;
            State = GameState.GameOver;
            Time.timeScale = 0f;
            OnGameOver?.Invoke();

            // Steam achievements
            Steam.SteamManager.Instance?.UnlockIfQualified(TotalKills, playerController?.Level ?? 1, ElapsedSeconds);
        }

        public void TriggerVictory()
        {
            State = GameState.Victory;
            OnVictory?.Invoke();
            Steam.SteamManager.Instance?.OnVictory(ElapsedSeconds, TotalKills);
        }

        public void ReturnToMenu()
        {
            Time.timeScale = 1f;
            State = GameState.Menu;
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }

    public enum GameState { Menu, Playing, Paused, GameOver, Victory }
}
