// URP 2D Renderer Data settings for Angel Arena
// Apply this via: Edit > Project Settings > Graphics > Default Renderer

// ── Key URP Settings for 2D top-down game ──────────────────────────────
// Renderer Type: Renderer 2D
// HDR: Enabled (for bloom/glow effects)
// Post Processing: Enabled
//
// Post Processing Stack setup (add via Volume in scene):
//   - Bloom: Intensity 0.8, Threshold 0.9, Scatter 0.7
//   - Color Grading: Mode LDR, Contrast +15, Saturation +10
//   - Vignette: Intensity 0.25, Smoothness 0.5
//   - Chromatic Aberration: Intensity 0.15 (on hit flash)

// ── 2D Lights Setup ──────────────────────────────────────────────────── 
// Global Light 2D: Color #1A1A2E, Intensity 0.4 (dark dungeon ambient)
// Point Light 2D on Player: Color #7C3AED (purple), Range 300, Intensity 0.8
// Skills: Spawn temporary Point Lights on AoE for dramatic effect
//
// Sorting Layers (already defined in TagManager.asset):
//   Ground → Objects → Characters → Projectiles → VFX → HUD

// ── Input Actions (create via: Assets > Create > Input Actions) ───────
// Action Map: "Gameplay"
//   - Move: Composite 2D Vector (WASD + Arrow Keys + Left Stick)
//   - Pause: Button (Escape + Start)
//   - UseItem: Button (Space + South Face Button)
//
// Action Map: "UI"  
//   - Navigate, Submit, Cancel (auto-configured by Unity)

// ── Physics 2D Layer Matrix ────────────────────────────────────────────
// Player    ↔ Enemy:     Enabled (contact damage)
// Player    ↔ Pickable:  Enabled (gold/gem pickup)
// Projectile ↔ Enemy:   Enabled (skill damage)
// Projectile ↔ Player:  Disabled
// Enemy     ↔ Enemy:    Disabled (no collision between enemies = better perf)
// Summon    ↔ Enemy:    Enabled (summon melee)
// Ground    ↔ All:      Disabled (no physics ground in top-down)
