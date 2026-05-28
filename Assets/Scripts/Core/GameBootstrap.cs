using UnityEngine;
using System.Collections;

namespace AngelArena.Core
{
    /// <summary>
    /// Bootstrap scene loader: initializes singletons in correct order,
    /// then loads the main menu.
    /// Attach this to a GameObject in the "Bootstrap" scene (scene index 0).
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Singleton Prefabs to instantiate at start")]
        public GameObject gameManagerPrefab;
        public GameObject saveSystemPrefab;
        public GameObject audioManagerPrefab;
        public GameObject steamManagerPrefab;
        public GameObject skillPrefabsPrefab;

        [Header("Load After Seconds")]
        public float delayBeforeMenu = 0.5f;

        private IEnumerator Start()
        {
            // ── 1. Save System (first — others depend on it) ──────
            if (saveSystemPrefab && Save.SaveSystem.Instance == null)
                Instantiate(saveSystemPrefab);

            yield return null;

            // ── 2. Steam Manager ─────────────────────────────────
            if (steamManagerPrefab && Steam.SteamManager.Instance == null)
                Instantiate(steamManagerPrefab);

            yield return null;

            // ── 3. Audio Manager ─────────────────────────────────
            if (audioManagerPrefab && Audio.AudioManager.Instance == null)
                Instantiate(audioManagerPrefab);

            yield return null;

            // ── 4. Game Manager ──────────────────────────────────
            if (gameManagerPrefab && GameManager.Instance == null)
                Instantiate(gameManagerPrefab);

            yield return null;

            // ── 5. Skill Prefabs Registry ────────────────────────
            if (skillPrefabsPrefab && SkillPrefabs.Instance == null)
                Instantiate(skillPrefabsPrefab);

            yield return new WaitForSeconds(delayBeforeMenu);

            // ── 6. Load Main Menu ────────────────────────────────
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}
