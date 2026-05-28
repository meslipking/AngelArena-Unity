#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using AngelArena.Core;
using AngelArena.Data;

namespace AngelArena.Editor
{
    /// <summary>
    /// Runs FULL setup automatically:
    ///   1. Register Tags
    ///   2. Create enemy prefabs (if missing)
    ///   3. Build scene (if GameScene missing)
    ///   4. Link enemy prefabs to spawner
    /// Menu: AngelArena → AUTO SETUP (Run All Steps)
    /// </summary>
    [InitializeOnLoad]
    public static class AutoSetup
    {
        // Key to avoid running on every domain reload
        private const string SETUP_DONE_KEY = "AngelArena_AutoSetup_Done";

        static AutoSetup()
        {
            // Run once after each compilation only if not yet done
            if (!SessionState.GetBool(SETUP_DONE_KEY, false))
                EditorApplication.delayCall += RunIfNeeded;
        }

        private static void RunIfNeeded()
        {
            // Only run if GameScene doesn't exist yet
            if (!System.IO.File.Exists("Assets/Scenes/GameScene.unity")) return;

            // Already set up — just ensure enemy prefabs are linked
            var spawnerInScene = Object.FindAnyObjectByType<EnemySpawner>();
            if (spawnerInScene != null && (spawnerInScene.enemyPools == null || spawnerInScene.enemyPools.Length == 0))
            {
                EnemyLinker.LinkEnemyPrefabs();
                EditorSceneManager.SaveOpenScenes();
            }

            SessionState.SetBool(SETUP_DONE_KEY, true);
        }

        [MenuItem("AngelArena/AUTO SETUP (Run All Steps)")]
        public static void RunFullSetup()
        {
            EditorUtility.DisplayProgressBar("AngelArena Auto Setup", "Registering tags...", 0.1f);
            try
            {
                // Step 1: Register tags
                RegisterTag("Player");
                RegisterTag("Enemy");

                // Step 2: Fix Input Handler
                var so   = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
                var prop = so.FindProperty("activeInputHandler");
                if (prop != null) { prop.intValue = 0; so.ApplyModifiedProperties(); }

                // Step 3: Project settings
                PlayerSettings.companyName = "Angel Arena Studio";
                PlayerSettings.productName = "Angel Arena";
#if UNITY_6000_0_OR_NEWER
                PlayerSettings.SetApplicationIdentifier(
                    UnityEditor.Build.NamedBuildTarget.Standalone,
                    "com.angelarenastudio.angelarena");
#endif
                PlayerSettings.defaultScreenWidth  = 1920;
                PlayerSettings.defaultScreenHeight = 1080;

                EditorUtility.DisplayProgressBar("AngelArena Auto Setup", "Creating enemy prefabs...", 0.3f);

                // Step 4: Create enemy data & prefabs (recreate to fix PPU)
                EnemySetupCreator.CreateEnemyAssetsAndPrefabs();

                EditorUtility.DisplayProgressBar("AngelArena Auto Setup", "Building scene...", 0.55f);

                // Step 5: Build PVE scene
                SceneBuilder.BuildPVESceneSilent();

                EditorUtility.DisplayProgressBar("AngelArena Auto Setup", "Linking enemy prefabs...", 0.8f);

                // Step 6: Link enemy prefabs
                EnemyLinker.LinkEnemyPrefabs();

                EditorUtility.DisplayProgressBar("AngelArena Auto Setup", "Saving...", 0.95f);

                // Save scene
                EditorSceneManager.SaveOpenScenes();
                AssetDatabase.SaveAssets();

                SessionState.SetBool(SETUP_DONE_KEY, true);

                EditorUtility.DisplayDialog("✅ Auto Setup Complete!",
                    "Angel Arena is ready!\n\n" +
                    "• Tags registered\n" +
                    "• Enemy prefabs created (correct PPU)\n" +
                    "• GameScene built\n" +
                    "• Enemy spawner linked\n\n" +
                    "Press PLAY to start the game!", "Play Now!");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void RegisterTag(string tagName)
        {
            var tagManager   = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tagsProperty = tagManager.FindProperty("tags");
            for (int i = 0; i < tagsProperty.arraySize; i++)
                if (tagsProperty.GetArrayElementAtIndex(i).stringValue == tagName) return;
            tagsProperty.InsertArrayElementAtIndex(tagsProperty.arraySize);
            tagsProperty.GetArrayElementAtIndex(tagsProperty.arraySize - 1).stringValue = tagName;
            tagManager.ApplyModifiedProperties();
        }
    }
}
#endif
