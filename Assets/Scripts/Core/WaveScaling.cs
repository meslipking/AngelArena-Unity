using UnityEngine;

namespace AngelArena.Core
{
    /// <summary>
    /// Wave scaling: mirrors JS getWaveScaling(sec) logic — ported to C#.
    /// Returns spawn parameters based on elapsed game seconds.
    /// </summary>
    [System.Serializable]
    public struct WaveParams
    {
        public float hpMult;
        public float dmgMult;
        public int   countPerBatch;
        public float batchInterval;
        public float eliteChance;
    }

    public static class WaveScaling
    {
        public static WaveParams Get(float seconds)
        {
            if (seconds < 120)  return new WaveParams { hpMult = 1.0f,  dmgMult = 1.0f, countPerBatch = 8,   batchInterval = 6f, eliteChance = 0.00f };
            if (seconds < 300)  return new WaveParams { hpMult = 1.2f,  dmgMult = 1.1f, countPerBatch = 14,  batchInterval = 5f, eliteChance = 0.05f };
            if (seconds < 480)  return new WaveParams { hpMult = 1.5f,  dmgMult = 1.2f, countPerBatch = 20,  batchInterval = 5f, eliteChance = 0.08f };
            if (seconds < 720)  return new WaveParams { hpMult = 2.0f,  dmgMult = 1.4f, countPerBatch = 28,  batchInterval = 5f, eliteChance = 0.10f };
            if (seconds < 900)  return new WaveParams { hpMult = 2.8f,  dmgMult = 1.7f, countPerBatch = 38,  batchInterval = 4f, eliteChance = 0.15f };
            if (seconds < 1200) return new WaveParams { hpMult = 4.0f,  dmgMult = 2.2f, countPerBatch = 50,  batchInterval = 4f, eliteChance = 0.20f };
            if (seconds < 1500) return new WaveParams { hpMult = 6.0f,  dmgMult = 3.0f, countPerBatch = 65,  batchInterval = 3f, eliteChance = 0.30f };
            if (seconds < 1680) return new WaveParams { hpMult = 9.0f,  dmgMult = 4.0f, countPerBatch = 85,  batchInterval = 3f, eliteChance = 0.40f };
            if (seconds < 1800) return new WaveParams { hpMult = 14.0f, dmgMult = 5.5f, countPerBatch = 110, batchInterval = 2f, eliteChance = 0.55f };
            return                 new WaveParams { hpMult = 20.0f, dmgMult = 8.0f, countPerBatch = 130, batchInterval = 1f, eliteChance = 0.70f };
        }

        /// <summary>Determines if a boss should spawn at the given elapsed seconds.</summary>
        public static bool IsBossTime(float seconds, out string bossId)
        {
            // Bosses at 5, 10, 15, 20, 25, 30 minutes
            float[] bossTimes = { 300f, 600f, 900f, 1200f, 1500f, 1800f };
            string[] bossIds  = { "goblin_king", "stone_golem", "vampire_lord", "demon_knight", "lich", "death_god" };

            for (int i = 0; i < bossTimes.Length; i++)
            {
                float t = bossTimes[i];
                if (Mathf.Abs(seconds - t) < 0.5f)
                {
                    bossId = bossIds[i];
                    return true;
                }
            }
            bossId = null;
            return false;
        }

        /// <summary>XP required to level up from the given level.</summary>
        public static int GetXpToNext(int level)
        {
            int base_ = 20;
            if (level <= 20)
            {
                base_ = 20 + (level - 1) * 10;
            }
            else if (level <= 40)
            {
                base_ = 20 + 190 + (level - 20) * 13;
            }
            else
            {
                base_ = 20 + 190 + 260 + (level - 40) * 16;
            }

            if (level == 20 || level == 40) base_ = Mathf.RoundToInt(base_ * 3f);
            return base_;
        }
    }
}
