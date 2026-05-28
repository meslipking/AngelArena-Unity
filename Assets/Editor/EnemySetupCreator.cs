#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using AngelArena.Data;
using AngelArena.Core;

namespace AngelArena.Editor
{
    /// <summary>
    /// Creates enemy prefabs and EnemyData assets automatically.
    /// Menu: AngelArena → Setup → 6. Create Enemy Data & Prefabs
    /// </summary>
    public static class EnemySetupCreator
    {
        private struct EnemyDef
        {
            public string id, name;
            public float  hp, dmg, spd, xp, gold;
            public float  size;
            public Color  color;
            public bool   isBoss;
            public float  aggro;
        }

        private static readonly EnemyDef[] Enemies = new[]
        {
            // ── Normal enemies ────────────────────────────────────
            new EnemyDef { id="slime",     name="Slime",       hp=60,   dmg=8,   spd=80,  xp=5,   gold=1,  size=22, color=new Color(0.3f,0.9f,0.3f), aggro=250 },
            new EnemyDef { id="goblin",    name="Goblin",      hp=80,   dmg=12,  spd=105, xp=8,   gold=2,  size=24, color=new Color(0.5f,0.7f,0.2f), aggro=300 },
            new EnemyDef { id="skeleton",  name="Skeleton",    hp=100,  dmg=15,  spd=90,  xp=10,  gold=2,  size=26, color=new Color(0.9f,0.9f,0.8f), aggro=320 },
            new EnemyDef { id="orc",       name="Orc",         hp=180,  dmg=22,  spd=75,  xp=18,  gold=4,  size=34, color=new Color(0.4f,0.6f,0.2f), aggro=280 },
            new EnemyDef { id="demon",     name="Demon",       hp=220,  dmg=28,  spd=100, xp=25,  gold=5,  size=30, color=new Color(0.8f,0.2f,0.2f), aggro=350 },
            new EnemyDef { id="wraith",    name="Wraith",      hp=140,  dmg=20,  spd=120, xp=20,  gold=4,  size=28, color=new Color(0.5f,0.3f,0.8f), aggro=400 },
            new EnemyDef { id="golem",     name="Golem",       hp=400,  dmg=35,  spd=55,  xp=40,  gold=8,  size=45, color=new Color(0.6f,0.6f,0.7f), aggro=220 },
            new EnemyDef { id="vampire",   name="Vampire",     hp=260,  dmg=30,  spd=110, xp=30,  gold=6,  size=30, color=new Color(0.7f,0.1f,0.3f), aggro=350 },
            new EnemyDef { id="witch",     name="Witch",       hp=160,  dmg=38,  spd=88,  xp=28,  gold=6,  size=28, color=new Color(0.5f,0.2f,0.8f), aggro=500 },
            new EnemyDef { id="giant",     name="Giant",       hp=600,  dmg=50,  spd=50,  xp=55,  gold=12, size=58, color=new Color(0.7f,0.5f,0.3f), aggro=200 },
            // ── Elite variants ────────────────────────────────────
            new EnemyDef { id="elite_orc", name="Elite Orc",  hp=540,  dmg=66,  spd=75,  xp=54,  gold=12, size=40, color=new Color(0.2f,0.9f,0.3f), aggro=300 },
            new EnemyDef { id="elite_demon",name="Elite Demon",hp=660, dmg=84,  spd=100, xp=75,  gold=15, size=36, color=new Color(1.0f,0.4f,0.1f), aggro=380 },
            // ── Bosses ───────────────────────────────────────────
            new EnemyDef { id="boss_dragon",  name="Ancient Dragon",    hp=8000, dmg=80, spd=70, xp=500,gold=100,size=80,color=new Color(1f,0.3f,0.1f),isBoss=true,aggro=800 },
            new EnemyDef { id="boss_lich",    name="Lich King",         hp=6000, dmg=70, spd=60, xp=500,gold=100,size=70,color=new Color(0.4f,0.7f,1f),isBoss=true,aggro=700 },
            new EnemyDef { id="boss_golem",   name="Titan Golem",       hp=10000,dmg=90, spd=50, xp=500,gold=100,size=90,color=new Color(0.8f,0.8f,0.5f),isBoss=true,aggro=600},
            new EnemyDef { id="boss_demon",   name="Demon Lord",        hp=9000, dmg=85, spd=80, xp=500,gold=100,size=85,color=new Color(1f,0.1f,0.1f),isBoss=true,aggro=900 },
            new EnemyDef { id="boss_vampire", name="Vampire Overlord",  hp=7000, dmg=75, spd=90, xp=500,gold=100,size=75,color=new Color(0.8f,0.1f,0.4f),isBoss=true,aggro=800},
            new EnemyDef { id="boss_witch",   name="Grand Witch",       hp=6500, dmg=95, spd=65, xp=500,gold=100,size=72,color=new Color(0.6f,0.2f,0.9f),isBoss=true,aggro=700},
        };

        [MenuItem("AngelArena/Setup/6. Create Enemy Data & Prefabs")]
        public static void CreateEnemyAssetsAndPrefabs()
        {
            // Register tags first so prefabs can use them
            RegisterTag("Enemy");
            RegisterTag("Player");

            string dataFolder   = "Assets/ScriptableObjects/Enemies";
            string prefabFolder = "Assets/Resources/Prefabs/Enemies";
            System.IO.Directory.CreateDirectory(dataFolder);
            System.IO.Directory.CreateDirectory(prefabFolder);

            int created = 0;

            foreach (var e in Enemies)
            {
                // ── ScriptableObject Data ─────────────────────────
                string dataPath = $"{dataFolder}/{e.id}.asset";
                EnemyData data;
                if (AssetDatabase.LoadAssetAtPath<EnemyData>(dataPath) == null)
                {
                    data = ScriptableObject.CreateInstance<EnemyData>();
                    data.enemyName    = e.name;
                    data.enemyId      = e.id;
                    data.baseHp       = e.hp;
                    data.baseDamage   = e.dmg;
                    data.moveSpeed    = e.spd;
                    data.baseXp       = (int)e.xp;
                    data.baseGold     = (int)e.gold;
                    data.aggroRange   = e.aggro;
                    data.isBoss       = e.isBoss;
                    data.radius       = e.size;
                    data.color        = e.color;
                    AssetDatabase.CreateAsset(data, dataPath);
                    created++;
                }
                else
                {
                    data = AssetDatabase.LoadAssetAtPath<EnemyData>(dataPath);
                }

                // ── Prefab ────────────────────────────────────────
                string prefabPath = $"{prefabFolder}/{e.id}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) continue;

                var go  = new GameObject(e.name);

                // Sprite renderer (colored circle)
                var sr  = go.AddComponent<SpriteRenderer>();
                sr.sprite       = CreateCircle(e.color, (int)(e.size * 2));
                sr.sortingOrder = 2;

                // Rigidbody2D
                var rb  = go.AddComponent<Rigidbody2D>();
                rb.gravityScale   = 0;
                rb.freezeRotation = true;

                // Collider
                var col = go.AddComponent<CircleCollider2D>();
                col.radius    = e.size;
                col.isTrigger = false;

                // EnemyController
                var ctrl = go.AddComponent<EnemyController>();
                // We can't set data here in Editor script easily without SaveAsPrefab,
                // but we set the tag and layer
                go.tag  = "Enemy";

                // Health bar child
                var hpBarGO = BuildEnemyHealthBar(go);

                // Save as prefab
                var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                Object.DestroyImmediate(go);

                created++;
                Debug.Log($"[AngelArena] Created enemy: {prefabPath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Enemy Setup",
                $"Created {created} enemy assets/prefabs!\n\n" +
                "Assign them in the EnemySpawner component\n(enemyPools and bossPools arrays)", "OK");
        }

        private static GameObject BuildEnemyHealthBar(GameObject parent)
        {
            var hpRoot = new GameObject("HP Bar");
            hpRoot.transform.SetParent(parent.transform);
            hpRoot.transform.localPosition = new Vector3(0, 35f, 0);

            var bg = new GameObject("BG");
            bg.transform.SetParent(hpRoot.transform);
            var bgSr = bg.AddComponent<SpriteRenderer>();
            bgSr.sprite       = CreateSquare(new Color(0.1f, 0.1f, 0.1f));
            bgSr.sortingOrder = 3;
            bg.transform.localScale = new Vector3(40, 5, 1);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(hpRoot.transform);
            var fillSr = fill.AddComponent<SpriteRenderer>();
            fillSr.sprite       = CreateSquare(new Color(0.9f, 0.2f, 0.2f));
            fillSr.sortingOrder = 4;
            fill.transform.localScale = new Vector3(38, 4, 1);

            return hpRoot;
        }

        // ── Tag Registration Helper ───────────────────────────────
        private static void RegisterTag(string tagName)
        {
            var tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tagsProperty = tagManager.FindProperty("tags");
            for (int i = 0; i < tagsProperty.arraySize; i++)
                if (tagsProperty.GetArrayElementAtIndex(i).stringValue == tagName) return;
            tagsProperty.InsertArrayElementAtIndex(tagsProperty.arraySize);
            tagsProperty.GetArrayElementAtIndex(tagsProperty.arraySize - 1).stringValue = tagName;
            tagManager.ApplyModifiedProperties();
            Debug.Log($"[AngelArena] Registered tag: '{tagName}'");
        }
        private static Sprite CreateCircle(Color col, int size)
        {
            int sz  = Mathf.Max(16, size);
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            float c = sz / 2f;
            for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), Vector2.one * c);
                    tex.SetPixel(x, y, d < c - 1 ? col : (d < c ? new Color(col.r * 0.7f, col.g * 0.7f, col.b * 0.7f) : Color.clear));
                }
            tex.Apply();
            // PPU = sz so sprite = 1 unit at scale=1; enemy world size = size units
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), (float)sz);
        }

        private static Sprite CreateSquare(Color col)
        {
            var tex = new Texture2D(4, 4);
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++) tex.SetPixel(x, y, col);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }
    }
}
#endif
