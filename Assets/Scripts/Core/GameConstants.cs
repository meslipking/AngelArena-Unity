using UnityEngine;

namespace AngelArena.Core
{
    /// <summary>Global game constants.</summary>
    public static class GameConstants
    {
        public const float WORLD_W = 3840f;  // World width in Unity units
        public const float WORLD_H = 2160f;  // World height in Unity units
        public const int   MAX_ACTIVE_SKILLS = 6;
        public const int   MAX_PASSIVE_ITEMS = 6;
        public const float GAME_DURATION     = 1800f; // 30 minutes
    }
}
