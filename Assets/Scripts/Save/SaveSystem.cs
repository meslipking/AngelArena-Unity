using System;
using System.IO;
using UnityEngine;

namespace AngelArena.Save
{
    /// <summary>
    /// Handles local save/load using JSON files.
    /// Also syncs to Steam Cloud if available.
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; private set; }

        private const string SAVE_FILE = "angelarena_save.json";
        private string SavePath => Path.Combine(Application.persistentDataPath, SAVE_FILE);

        [System.Serializable]
        public class SaveData
        {
            public int    totalPlaytimeSec;
            public int    bestSurvivalSec;
            public int    totalKillsAllTime;
            public int    highestLevel;
            public bool[] unlockedCharacters = new bool[7];
            public int[]  characterPlayCount  = new int[7];
            public int    steamAchievementFlags;  // Bitfield of unlocked achievements
            public string lastPlayedCharId;
            public int    masterVolume = 100;
            public int    sfxVolume    = 100;
            public int    musicVolume  = 80;
            public bool   fullscreen   = true;
        }

        public SaveData CurrentSave { get; private set; } = new();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        // ── Save ─────────────────────────────────────────────────
        public void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(CurrentSave, prettyPrint: true);
                File.WriteAllText(SavePath, json);

                // Sync to Steam Cloud
                Steam.SteamManager.Instance?.CloudSave(SAVE_FILE, json);

                Debug.Log($"[Save] Saved to {SavePath}");
            }
            catch (Exception e) { Debug.LogError($"[Save] Error: {e.Message}"); }
        }

        // ── Load ─────────────────────────────────────────────────
        public void Load()
        {
            try
            {
                // Try Steam Cloud first
                string cloudJson = Steam.SteamManager.Instance?.CloudLoad(SAVE_FILE);
                if (!string.IsNullOrEmpty(cloudJson))
                {
                    CurrentSave = JsonUtility.FromJson<SaveData>(cloudJson);
                    Debug.Log("[Save] Loaded from Steam Cloud");
                    return;
                }

                // Fallback: local file
                if (File.Exists(SavePath))
                {
                    string json = File.ReadAllText(SavePath);
                    CurrentSave = JsonUtility.FromJson<SaveData>(json);
                    Debug.Log("[Save] Loaded from local file");
                }
                else
                {
                    CurrentSave = new SaveData();
                    CurrentSave.unlockedCharacters[0] = true; // Assassin unlocked by default
                    Debug.Log("[Save] New save created");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Load error: {e.Message}");
                CurrentSave = new SaveData();
            }
        }

        // ── Update after session ─────────────────────────────────
        public void RecordSession(int survivalSec, int kills, int level, string charId, int charIndex)
        {
            CurrentSave.totalPlaytimeSec  += survivalSec;
            CurrentSave.totalKillsAllTime += kills;
            CurrentSave.bestSurvivalSec    = Math.Max(CurrentSave.bestSurvivalSec, survivalSec);
            CurrentSave.highestLevel       = Math.Max(CurrentSave.highestLevel, level);
            CurrentSave.lastPlayedCharId   = charId;

            if (charIndex >= 0 && charIndex < CurrentSave.characterPlayCount.Length)
                CurrentSave.characterPlayCount[charIndex]++;

            Save();
        }

        public void SetVolume(int master, int sfx, int music)
        {
            CurrentSave.masterVolume = master;
            CurrentSave.sfxVolume    = sfx;
            CurrentSave.musicVolume  = music;
            Save();
        }
    }
}
