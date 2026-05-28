#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using AngelArena.Core;
using AngelArena.Data;
using AngelArena.UI;

namespace AngelArena.Editor
{
    /// <summary>
    /// Auto-links all enemy prefabs into EnemySpawner in the current scene.
    /// Menu: AngelArena → Setup → 7. Link Enemy Prefabs to Spawner
    /// </summary>
    public static class EnemyLinker
    {
        private static readonly string[] BossIds = {
            "boss_dragon", "boss_lich", "boss_golem",
            "boss_demon",  "boss_vampire", "boss_witch"
        };

        [MenuItem("AngelArena/Setup/7. Link Enemy Prefabs to Spawner")]
        public static void LinkEnemyPrefabs()
        {
            // ── Find EnemySpawner in scene ──────────────────────────
            var spawner = Object.FindAnyObjectByType<EnemySpawner>();
            if (spawner == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "No EnemySpawner found in scene!\nRun 'Build PVE GameScene' first.", "OK");
                return;
            }

            string prefabRoot = "Assets/Resources/Prefabs/Enemies";
            string dataRoot   = "Assets/ScriptableObjects/Enemies";

            // ── Normal enemy pools ──────────────────────────────────
            var normalIds = new[]
            {
                "slime","goblin","skeleton","orc","demon",
                "wraith","golem","vampire","witch","giant",
                "elite_orc","elite_demon"
            };

            var pools = new System.Collections.Generic.List<EnemyPool>();
            foreach (var id in normalIds)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{prefabRoot}/{id}.prefab");
                var data   = AssetDatabase.LoadAssetAtPath<EnemyData>($"{dataRoot}/{id}.asset");
                if (prefab == null || data == null)
                {
                    Debug.LogWarning($"[AngelArena] Missing: {id} prefab or data — run Setup 6 first!");
                    continue;
                }
                pools.Add(new EnemyPool { data = data, prefab = prefab });
                Debug.Log($"[AngelArena] Linked enemy: {id}");
            }

            // ── Boss pools ──────────────────────────────────────────
            var bossPools = new System.Collections.Generic.List<BossPool>();
            foreach (var id in BossIds)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{prefabRoot}/{id}.prefab");
                var data   = AssetDatabase.LoadAssetAtPath<EnemyData>($"{dataRoot}/{id}.asset");
                if (prefab == null || data == null)
                {
                    Debug.LogWarning($"[AngelArena] Missing boss: {id} — run Setup 6 first!");
                    continue;
                }
                bossPools.Add(new BossPool { bossId = id, data = data, prefab = prefab });
                Debug.Log($"[AngelArena] Linked boss: {id}");
            }

            // ── Apply to spawner ────────────────────────────────────
            var so = new SerializedObject(spawner);

            var poolsProp = so.FindProperty("enemyPools");
            poolsProp.arraySize = pools.Count;
            for (int i = 0; i < pools.Count; i++)
            {
                var elem = poolsProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("data").objectReferenceValue   = pools[i].data;
                elem.FindPropertyRelative("prefab").objectReferenceValue = pools[i].prefab;
            }

            var bossesProp = so.FindProperty("bossPools");
            bossesProp.arraySize = bossPools.Count;
            for (int i = 0; i < bossPools.Count; i++)
            {
                var elem = bossesProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("bossId").stringValue          = bossPools[i].bossId;
                elem.FindPropertyRelative("data").objectReferenceValue   = bossPools[i].data;
                elem.FindPropertyRelative("prefab").objectReferenceValue = bossPools[i].prefab;
            }

            so.ApplyModifiedProperties();

            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Done!",
                $"Linked {pools.Count} enemy pools and {bossPools.Count} boss pools to EnemySpawner!\n\nPress PLAY to start game!", "OK");

            Debug.Log($"[AngelArena] EnemySpawner linked: {pools.Count} enemies, {bossPools.Count} bosses");
        }

        /// <summary>
        /// Assigns CharacterData assets to the MainMenuController or GameManager.
        /// </summary>
        [MenuItem("AngelArena/Setup/8. Link Character Data to GameManager")]
        public static void LinkCharacterData()
        {
            string folder = "Assets/ScriptableObjects/Characters";
            var ids = new[] { "assassin","fighter","mage","ranger","paladin","necromancer","druid" };

            var chars = new System.Collections.Generic.List<CharacterData>();
            foreach (var id in ids)
            {
                var c = AssetDatabase.LoadAssetAtPath<CharacterData>($"{folder}/{id}.asset");
                if (c != null) chars.Add(c);
                else Debug.LogWarning($"[AngelArena] Missing character data: {id}");
            }

            // Try to find MainMenuController
            var menu = Object.FindAnyObjectByType<MainMenuController>();
            if (menu != null)
            {
                var so = new SerializedObject(menu);
                var prop = so.FindProperty("characters");
                if (prop != null)
                {
                    prop.arraySize = chars.Count;
                    for (int i = 0; i < chars.Count; i++)
                        prop.GetArrayElementAtIndex(i).objectReferenceValue = chars[i];
                    so.ApplyModifiedProperties();
                    Debug.Log($"[AngelArena] Linked {chars.Count} characters to MainMenuController");
                }
            }

            EditorUtility.DisplayDialog("Done!",
                $"Found {chars.Count} CharacterData assets.\n\nAssign them to MainMenuController.characters in Inspector if not auto-linked.", "OK");
        }
    }
}
#endif
