// Steamworks.NET — only active after package is installed
// Install via: Window > Package Manager > Add from git URL
// https://github.com/rlabrecque/Steamworks.NET.git?path=/com.rlabrecque.steamworks.net

using UnityEngine;

namespace AngelArena.Steam
{
    /// <summary>
    /// Steam integration: achievements, leaderboards, cloud save.
    /// Steamworks.NET must be installed for full functionality.
    /// Without the package this class compiles as stubs.
    /// </summary>
    public class SteamManager : MonoBehaviour
    {
        public static SteamManager Instance { get; private set; }

        [Header("App ID (replace with your Steam App ID)")]
        public uint appId = 480; // 480 = Spacewar (test), replace after Steam Direct

        private bool _steamInitialized = false;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            TryInitSteam();
        }

        private void TryInitSteam()
        {
            // Steamworks.NET will be initialized here once package is installed
            // For now: stub that compiles without the package
            Debug.Log("[Steam] SteamManager ready. Install Steamworks.NET package to enable Steam features.");
        }

        private void Update()
        {
            // SteamAPI.RunCallbacks() — called once Steamworks.NET is installed
        }

        private void OnDestroy()
        {
            // SteamAPI.Shutdown()
        }

        // ── Achievements ─────────────────────────────────────────
        public void UnlockAchievement(string achievementId)
        {
            if (!_steamInitialized) { Debug.Log($"[Steam] Achievement (stub): {achievementId}"); return; }
            // SteamUserStats.SetAchievement(achievementId);
            // SteamUserStats.StoreStats();
        }

        public void UnlockIfQualified(int kills, int level, float survivalSeconds)
        {
            if (kills >= 1)    UnlockAchievement("ACH_FIRST_BLOOD");
            if (kills >= 100)  UnlockAchievement("ACH_100_KILLS");
            if (kills >= 1000) UnlockAchievement("ACH_1000_KILLS");
            if (survivalSeconds >= 300) UnlockAchievement("ACH_SURVIVOR_5MIN");
        }

        public void OnVictory(float survivalSeconds, int kills)
        {
            UnlockAchievement("ACH_30MIN_VICTORY");
            UnlockIfQualified(kills, 1, survivalSeconds);
            SubmitLeaderboardScore("LB_SURVIVAL_TIME", (int)survivalSeconds);
            SubmitLeaderboardScore("LB_TOTAL_KILLS", kills);
        }

        public void OnBossKilled(string bossId)    => UnlockAchievement("ACH_FIRST_BOSS");
        public void OnLegendaryUnlocked()          => UnlockAchievement("ACH_LEGENDARY_SKILL");

        // ── Leaderboards ─────────────────────────────────────────
        public void SubmitLeaderboardScore(string name, int score)
            => Debug.Log($"[Steam] Score (stub): {score} → {name}");

        // ── Cloud Save ────────────────────────────────────────────
        public void CloudSave(string key, string json)
            => Debug.Log($"[Steam] CloudSave (stub): {key}");

        public string CloudLoad(string key) => null; // Returns null = use local save
    }
}
