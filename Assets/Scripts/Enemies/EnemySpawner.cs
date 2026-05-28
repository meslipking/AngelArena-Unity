using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngelArena.Data;

namespace AngelArena.Core
{
    /// <summary>
    /// Spawns enemies in batches based on WaveScaling.
    /// Manages the full enemy list for targeting queries.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        public static EnemySpawner Instance { get; private set; }
        public static List<EnemyController> AllEnemies { get; } = new();

        [Header("Enemy Prefabs — assign in Inspector")]
        public EnemyPool[] enemyPools;  // Each pool: data + prefab

        [Header("Boss Prefabs")]
        public BossPool[] bossPools;

        [Header("Spawn Config")]
        [Tooltip("Minimum distance from player to spawn")]
        public float spawnDistMin = 500f;
        public float spawnDistMax = 700f;
        public int   maxActiveEnemies = 800;

        // ── Internal ─────────────────────────────────────────────
        private Transform _playerTr;
        private float     _batchTimer;
        private WaveParams _currentWave;
        private bool      _bossSpawned;

        // ── Boss schedule ─────────────────────────────────────────
        private HashSet<string> _spawnedBossIds = new();

        // ─────────────────────────────────────────────────────────
        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            _playerTr    = GameObject.FindWithTag("Player")?.transform;
            _currentWave = WaveScaling.Get(0f);
            _batchTimer  = 0f; // Spawn immediately

            // Initial batch
            SpawnBatch();
        }

        private void Update()
        {
            if (!GameManager.Instance.GameRunning) return;

            float elapsed = GameManager.Instance.ElapsedSeconds;
            _currentWave  = WaveScaling.Get(elapsed);

            // Batch timer
            _batchTimer -= Time.deltaTime;
            if (_batchTimer <= 0)
            {
                _batchTimer = _currentWave.batchInterval;
                SpawnBatch();
            }

            // Boss spawn check
            if (WaveScaling.IsBossTime(elapsed, out string bossId) && !_spawnedBossIds.Contains(bossId))
            {
                _spawnedBossIds.Add(bossId);
                StartCoroutine(SpawnBossCoroutine(bossId));
            }
        }

        private void SpawnBatch()
        {
            if (_playerTr == null) return;
            if (AllEnemies.Count >= maxActiveEnemies) return;

            int count = Mathf.Min(_currentWave.countPerBatch,
                                   maxActiveEnemies - AllEnemies.Count);

            for (int i = 0; i < count; i++)
            {
                bool isElite = Random.value < _currentWave.eliteChance;
                EnemyPool pool = GetRandomPool(isElite);
                if (pool == null) continue;

                Vector2 spawnPos = GetSpawnPosition();
                SpawnEnemy(pool, spawnPos, _currentWave.hpMult, _currentWave.dmgMult, isElite, false);
            }
        }

        private void SpawnEnemy(EnemyPool pool, Vector2 pos, float hpM, float dmgM,
                                 bool elite, bool boss)
        {
            if (pool.prefab == null || pool.data == null) return;

            var go   = Instantiate(pool.prefab, (Vector3)pos, Quaternion.identity, transform);
            var ctrl = go.GetComponent<EnemyController>();
            if (ctrl == null) return;

            float hp  = pool.data.GetScaledHp(hpM)  * (elite ? 3f  : 1f) * (boss ? 20f : 1f);
            float dmg = pool.data.GetScaledDmg(dmgM) * (elite ? 1.5f: 1f) * (boss ? 5f  : 1f);

            ctrl.Init(pool.data, hp, dmg, elite, boss);
            AllEnemies.Add(ctrl);
        }

        private IEnumerator SpawnBossCoroutine(string bossId)
        {
            // Warning UI
            HUDManager.Instance?.ShowBossWarning(bossId);
            yield return new WaitForSeconds(3f);

            BossPool bPool = System.Array.Find(bossPools, b => b.bossId == bossId);
            if (bPool != null)
            {
                Vector2 spawnPos = GetSpawnPosition();
                SpawnEnemy(new EnemyPool { data = bPool.data, prefab = bPool.prefab },
                    spawnPos, _currentWave.hpMult, _currentWave.dmgMult, false, true);
            }
        }

        public void OnEnemyDied(EnemyController e) => AllEnemies.Remove(e);

        // ── Helpers ──────────────────────────────────────────────
        private Vector2 GetSpawnPosition()
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist  = Random.Range(spawnDistMin, spawnDistMax);
            Vector2 pos = (Vector2)_playerTr.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;

            // Clamp to world
            pos.x = Mathf.Clamp(pos.x, -GameConstants.WORLD_W / 2f, GameConstants.WORLD_W / 2f);
            pos.y = Mathf.Clamp(pos.y, -GameConstants.WORLD_H / 2f, GameConstants.WORLD_H / 2f);
            return pos;
        }

        private EnemyPool GetRandomPool(bool preferElite)
        {
            if (enemyPools == null || enemyPools.Length == 0) return null;
            // For now: random pick, can be refined with wave-based weights
            return enemyPools[Random.Range(0, enemyPools.Length)];
        }

        private void OnDestroy() => AllEnemies.Clear();
    }

    [System.Serializable]
    public class EnemyPool
    {
        public EnemyData data;
        public GameObject prefab;
    }

    [System.Serializable]
    public class BossPool
    {
        public string    bossId;
        public EnemyData data;
        public GameObject prefab;
    }
}
