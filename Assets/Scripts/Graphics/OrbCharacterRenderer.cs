using UnityEngine;

namespace AngelArena.Graphics
{
    /// <summary>
    /// Premium procedural orb-chibi renderer matching the reference screenshot style.
    /// Creates glossy spherical characters with large expressive eyes, thick outlines,
    /// neon glow, and cute aggressive expressions - as seen in the reference image.
    /// 
    /// Each character is a:
    ///   - Glossy sphere body (radial gradient + 2 specular highlights)
    ///   - Thick dark outline (3px)
    ///   - Large oval eyes with pupils + angry/cute brow
    ///   - Blush cheeks (soft pink)
    ///   - Unique accessories: crown, ears, horns, hat, etc.
    ///   - Neon rim light at base
    /// </summary>
    public static class OrbCharacterRenderer
    {
        // ─────────────────────────────────────────────────
        //  PUBLIC API — call these to generate sprites
        // ─────────────────────────────────────────────────

        public static Sprite GeneratePlayerOrb(string charId, int level, bool angry, bool blinking, bool isDead)
        {
            var def = GetPlayerDef(charId, level);
            return DrawOrbFull(def, 256, angry, blinking, isDead);
        }

        public static Sprite GenerateEnemyOrb(string enemyType, bool isElite, bool isBoss, bool angry, bool blinking)
        {
            var def = GetEnemyDef(enemyType, isElite, isBoss);
            return DrawOrbFull(def, isBoss ? 256 : 192, angry, blinking, false);
        }

        // ─────────────────────────────────────────────────
        //  CORE ORB DRAWING
        // ─────────────────────────────────────────────────
        static Sprite DrawOrbFull(OrbDef def, int sz, bool angry, bool blinking, bool dead)
        {
            var tex = NewTex(sz);
            float c = sz / 2f;
            float r = sz * 0.42f;       // orb radius (leaving room for outline + glow)
            float outlineW = Mathf.Max(3f, sz * 0.025f);

            // ── Step 1: Outer glow / neon ring ─────────────────────
            DrawNeonGlow(tex, c, c, r, def.glowColor, sz);

            // ── Step 2: Orb body (dark outline ring then fill) ──────
            DrawFilledCircle(tex, c, c, r + outlineW, def.outlineColor);
            DrawGlossySphere(tex, c, c, r, def.bodyColor, def.bodyColorDark, def.bodyColorHL, def.outlineColor);

            // ── Step 3: Accessories behind face ─────────────────────
            DrawAccessory(tex, def, c, c, r, sz, false);

            // ── Step 4: Face (eyes, cheeks, mouth) ──────────────────
            DrawFace(tex, def, c, c, r, angry, blinking, dead);

            // ── Step 5: Accessories on top of face ──────────────────
            DrawAccessory(tex, def, c, c, r, sz, true);

            // ── Step 6: Double specular highlights ──────────────────
            // Primary large specular (top-left)
            DrawSpecular(tex, c - r * 0.3f, c + r * 0.35f, r * 0.28f, 0.92f);
            // Secondary small specular (right of primary)
            DrawSpecular(tex, c - r * 0.05f, c + r * 0.52f, r * 0.10f, 0.65f);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, sz);
        }

        // ─────────────────────────────────────────────────
        //  SPHERE GRADIENT
        // ─────────────────────────────────────────────────
        static void DrawGlossySphere(Texture2D tex, float cx, float cy, float r,
                                      Color bodyMid, Color bodyDark, Color bodyHL, Color outline)
        {
            int sz = tex.width;
            // Light source from top-left at 35% from left, 65% from bottom
            Vector2 lightPos = new Vector2(cx - r * 0.35f, cy + r * 0.45f);

            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float dx = x - cx, dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > r) continue;

                // Normalized distance [0,1]
                float t = dist / r;

                // Distance from light source
                float lDist = Vector2.Distance(new Vector2(x, y), lightPos);
                float lFactor = Mathf.Clamp01(lDist / (r * 1.4f));

                // Base: lerp from highlight(center near light) -> mid -> dark(edges)
                Color fill = Color.Lerp(bodyHL, bodyMid, lFactor * 0.6f + t * 0.4f);
                fill = Color.Lerp(fill, bodyDark, Mathf.Pow(t, 2.0f) * 0.7f);

                // Rim light (opposite side of specular — bottom right)
                float rimDx = x - (cx + r * 0.55f), rimDy = y - (cy - r * 0.55f);
                float rimDist = Mathf.Sqrt(rimDx * rimDx + rimDy * rimDy);
                if (rimDist < r * 0.5f && t > 0.6f)
                {
                    float rimT = 1f - (rimDist / (r * 0.5f));
                    Color rimColor = new Color(
                        Mathf.Clamp01(bodyMid.r * 1.4f),
                        Mathf.Clamp01(bodyMid.g * 1.4f),
                        Mathf.Clamp01(bodyMid.b * 1.8f),
                        1f);
                    fill = Color.Lerp(fill, rimColor, rimT * 0.3f);
                }

                tex.SetPixel(x, y, fill);
            }
        }

        // ─────────────────────────────────────────────────
        //  SPECULAR
        // ─────────────────────────────────────────────────
        static void DrawSpecular(Texture2D tex, float cx, float cy, float r, float intensity)
        {
            int sz = tex.width;
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                if (d >= r) continue;
                float t = 1f - (d / r);
                // Soft gaussian falloff
                float alpha = t * t * intensity;
                Color existing = tex.GetPixel(x, y);
                if (existing.a < 0.01f) continue;
                tex.SetPixel(x, y, Color.Lerp(existing, Color.white, alpha));
            }
        }

        // ─────────────────────────────────────────────────
        //  NEON GLOW
        // ─────────────────────────────────────────────────
        static void DrawNeonGlow(Texture2D tex, float cx, float cy, float orbR, Color glowColor, int sz)
        {
            float glowR  = orbR * 1.38f;
            float coreR  = orbR * 1.01f;
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                if (d < coreR || d > glowR) continue;
                float t = 1f - (d - coreR) / (glowR - coreR);
                float alpha = t * t * 0.55f;
                Color gc = glowColor;
                gc.a = alpha;
                tex.SetPixel(x, y, gc);
            }
        }

        // ─────────────────────────────────────────────────
        //  FACE
        // ─────────────────────────────────────────────────
        static void DrawFace(Texture2D tex, OrbDef def, float cx, float cy, float r,
                              bool angry, bool blinking, bool dead)
        {
            // Face center is slightly upper half of orb
            float faceY = cy + r * 0.08f;

            // Blush cheeks (always visible, behind eyes)
            DrawBlushCheek(tex, cx - r * 0.52f, faceY - r * 0.22f, r * 0.19f, r * 0.10f);
            DrawBlushCheek(tex, cx + r * 0.52f, faceY - r * 0.22f, r * 0.19f, r * 0.10f);

            // Eye positions
            float eyeSpread = r * 0.32f;
            float eyeY      = faceY + r * 0.08f;
            float eyeRX     = r * 0.19f;   // eye half-width
            float eyeRY     = r * 0.22f;   // eye half-height (tall oval)

            if (dead)
            {
                DrawDeadEye(tex, cx - eyeSpread, eyeY, eyeRX, def.outlineColor);
                DrawDeadEye(tex, cx + eyeSpread, eyeY, eyeRX, def.outlineColor);
            }
            else if (blinking)
            {
                DrawBlinkEye(tex, cx - eyeSpread, eyeY, eyeRX, def.outlineColor);
                DrawBlinkEye(tex, cx + eyeSpread, eyeY, eyeRX, def.outlineColor);
            }
            else
            {
                DrawOrbEye(tex, cx - eyeSpread, eyeY, eyeRX, eyeRY, def.eyeColor, def.eyePupilColor, def.outlineColor, angry, true);
                DrawOrbEye(tex, cx + eyeSpread, eyeY, eyeRX, eyeRY, def.eyeColor, def.eyePupilColor, def.outlineColor, angry, false);
            }

            // Mouth
            DrawMouth(tex, cx, faceY - r * 0.32f, r, def.outlineColor, angry);
        }

        // Premium oval eye with specular dot
        static void DrawOrbEye(Texture2D tex, float ex, float ey,
                                 float rx, float ry,
                                 Color eyeWhite, Color pupilColor, Color outline,
                                 bool angry, bool leftEye)
        {
            float outlineRX = rx + 2.5f;
            float outlineRY = ry + 2.5f;

            // Thick outline oval
            DrawOval(tex, ex, ey, outlineRX, outlineRY, outline);

            // Angry brow (before eye fill so eye overlaps)
            if (angry)
            {
                // Diagonal angry brow
                float browLen = rx * 1.5f;
                float browY   = ey + ry * 0.75f;
                if (leftEye)
                    DrawThickLine(tex, ex - browLen * 0.4f, browY + rx * 0.5f,
                                       ex + browLen * 0.6f, browY, outline, 2.5f);
                else
                    DrawThickLine(tex, ex - browLen * 0.6f, browY,
                                       ex + browLen * 0.4f, browY + rx * 0.5f, outline, 2.5f);
            }
            else
            {
                // Soft curved brow (just a slight arc line above eye)
                float browY = ey + ry * 0.75f;
                DrawThickLine(tex, ex - rx * 0.8f, browY + 1f,
                                   ex + rx * 0.8f, browY + 1f, outline, 1.5f);
            }

            // White eye fill
            DrawOval(tex, ex, ey, rx, ry, eyeWhite);

            // Pupil (oval, slightly shifted inward)
            float pShiftX = leftEye ? rx * 0.12f : -rx * 0.12f;
            float pShiftY = angry ? -ry * 0.05f : ry * 0.05f;
            float pRX = rx * 0.48f;
            float pRY = ry * 0.52f;
            DrawOval(tex, ex + pShiftX, ey + pShiftY, pRX, pRY, outline);
            DrawOval(tex, ex + pShiftX, ey + pShiftY, pRX * 0.65f, pRY * 0.65f, pupilColor);

            // Specular dot in pupil (top of pupil)
            float specX = ex + pShiftX - pRX * 0.3f;
            float specY = ey + pShiftY + pRY * 0.35f;
            DrawFilledCircle(tex, specX, specY, pRX * 0.28f, new Color(1f, 1f, 1f, 0.95f));
        }

        static void DrawBlinkEye(Texture2D tex, float ex, float ey, float er, Color col)
        {
            // Upward curved arc = closed eye
            DrawThickLine(tex, ex - er, ey, ex, ey + er * 0.6f, col, 2.5f);
            DrawThickLine(tex, ex, ey + er * 0.6f, ex + er, ey, col, 2.5f);
            // Lashes
            DrawThickLine(tex, ex - er * 0.7f, ey - er * 0.3f, ex - er * 0.7f, ey - er * 0.6f, col, 1.5f);
            DrawThickLine(tex, ex, ey - er * 0.15f, ex, ey - er * 0.5f, col, 1.5f);
            DrawThickLine(tex, ex + er * 0.7f, ey - er * 0.3f, ex + er * 0.7f, ey - er * 0.6f, col, 1.5f);
        }

        static void DrawDeadEye(Texture2D tex, float ex, float ey, float er, Color col)
        {
            DrawThickLine(tex, ex - er, ey - er, ex + er, ey + er, col, 2.5f);
            DrawThickLine(tex, ex - er, ey + er, ex + er, ey - er, col, 2.5f);
        }

        static void DrawMouth(Texture2D tex, float mx, float my, float r, Color col, bool angry)
        {
            if (angry)
            {
                // Open zigzag angry mouth
                float mw = r * 0.38f;
                float mh = r * 0.12f;
                DrawThickLine(tex, mx - mw, my, mx - mw * 0.33f, my - mh, col, 2f);
                DrawThickLine(tex, mx - mw * 0.33f, my - mh, mx, my, col, 2f);
                DrawThickLine(tex, mx, my, mx + mw * 0.33f, my - mh, col, 2f);
                DrawThickLine(tex, mx + mw * 0.33f, my - mh, mx + mw, my, col, 2f);
                // Teeth (white inside)
                DrawRect(tex, mx - mw * 0.7f, my - mh * 0.8f, mw * 1.4f, mh * 0.6f, Color.white);
            }
            else
            {
                // Simple smile ˘‿˘
                float mw = r * 0.25f;
                DrawThickLine(tex, mx - mw, my + r * 0.04f, mx, my - r * 0.04f, col, 2f);
                DrawThickLine(tex, mx, my - r * 0.04f, mx + mw, my + r * 0.04f, col, 2f);
            }
        }

        static void DrawBlushCheek(Texture2D tex, float cx, float cy, float rx, float ry)
        {
            Color cheek = new Color(0.98f, 0.62f, 0.72f, 0.38f);
            DrawOvalAlpha(tex, cx, cy, rx, ry, cheek);
        }

        // ─────────────────────────────────────────────────
        //  ACCESSORIES (per character type)
        // ─────────────────────────────────────────────────
        static void DrawAccessory(Texture2D tex, OrbDef def, float cx, float cy, float r, int sz, bool topLayer)
        {
            switch (def.accessory)
            {
                // ── Player characters ─────────────────────────────
                case "crown":
                    if (topLayer) DrawCrown(tex, cx, cy, r, def.outlineColor);
                    break;
                case "wizard_hat":
                    if (!topLayer) DrawWizardHat(tex, cx, cy, r, def.outlineColor);
                    break;
                case "neko_ears":
                    if (!topLayer) DrawNekoEars(tex, cx, cy, r, def.bodyColor, def.outlineColor);
                    break;
                case "ninja_mask":
                    if (topLayer) DrawNinjaMask(tex, cx, cy, r, def.outlineColor);
                    break;
                case "antlers":
                    if (!topLayer) DrawAntlers(tex, cx, cy, r, def.outlineColor);
                    break;
                case "demon_horns":
                    if (!topLayer) DrawDemonHorns(tex, cx, cy, r, new Color(0.85f, 0.1f, 0.1f), def.outlineColor);
                    break;
                case "golden_halo":
                    if (topLayer) DrawHalo(tex, cx, cy, r, new Color(1f, 0.9f, 0.2f, 0.9f), def.outlineColor);
                    break;
                case "skull_horns":
                    if (!topLayer) DrawSkullHorns(tex, cx, cy, r, def.outlineColor);
                    break;

                // ── Enemy accessories ──────────────────────────────
                case "slime_blob":
                    if (!topLayer) DrawSlimeBlobs(tex, cx, cy, r, def.bodyColor);
                    break;
                case "goblin_ears":
                    if (!topLayer) DrawGoblinEars(tex, cx, cy, r, def.bodyColor, def.outlineColor);
                    break;
                case "skeleton_skull":
                    // Already handled by body color/style
                    break;
                case "orc_tusks":
                    if (topLayer) DrawTusks(tex, cx, cy, r, def.outlineColor);
                    break;
                case "demon_wings":
                    if (!topLayer) DrawDemonWingBumps(tex, cx, cy, r, def.outlineColor);
                    break;
                case "ghost_wisp":
                    if (!topLayer) DrawGhostWisp(tex, cx, cy, r, def.glowColor);
                    break;
                case "boss_crown_fire":
                    if (topLayer) DrawFireCrown(tex, cx, cy, r, def.outlineColor, def.glowColor);
                    break;
                case "vampire_collar":
                    if (!topLayer) DrawVampireCollar(tex, cx, cy, r, new Color(0.6f, 0.05f, 0.1f), def.outlineColor);
                    break;
                case "witch_hat":
                    if (!topLayer) DrawWitchHat(tex, cx, cy, r, def.outlineColor);
                    break;
                case "giant_horns":
                    if (!topLayer) DrawGiantHorns(tex, cx, cy, r, def.bodyColorDark, def.outlineColor);
                    break;
            }
        }

        // ─── Crown (Golden boss) ───────────────────────────────────
        static void DrawCrown(Texture2D tex, float cx, float cy, float r, Color outline)
        {
            Color gold  = new Color(1f, 0.82f, 0.1f);
            Color goldD = new Color(0.8f, 0.6f, 0.05f);
            float baseY = cy + r * 0.72f;
            float baseH = r * 0.22f;
            float baseW = r * 0.9f;

            // Crown band
            DrawRect(tex, cx - baseW, baseY - baseH * 0.5f, baseW * 2f, baseH, gold);
            DrawRect(tex, cx - baseW, baseY - baseH * 0.5f, baseW * 2f, 2f, outline);
            DrawRect(tex, cx - baseW, baseY + baseH * 0.5f - 2f, baseW * 2f, 2f, outline);

            // Three spike points
            float[] spikeXs = { cx - baseW * 0.55f, cx, cx + baseW * 0.55f };
            float[] spikeHs = { r * 0.28f, r * 0.38f, r * 0.28f };

            for (int i = 0; i < 3; i++)
            {
                float sx = spikeXs[i];
                float sh = spikeHs[i];
                DrawTriangle(tex,
                    new Vector2(sx - r * 0.12f, baseY),
                    new Vector2(sx, baseY + sh),
                    new Vector2(sx + r * 0.12f, baseY), gold);
                // Gem on spike
                DrawFilledCircle(tex, sx, baseY + sh * 0.6f, r * 0.045f, i == 1 ? Color.red : Color.cyan);
                DrawFilledCircle(tex, sx, baseY + sh * 0.6f, r * 0.025f, Color.white);
            }
        }

        // ─── Wizard Hat ────────────────────────────────────────────
        static void DrawWizardHat(Texture2D tex, float cx, float cy, float r, Color outline)
        {
            Color hat  = new Color(0.18f, 0.12f, 0.55f);
            Color brim = new Color(0.12f, 0.08f, 0.4f);

            float topY  = cy + r * 1.55f;
            float baseY = cy + r * 0.75f;
            float brimW = r * 1.1f;

            // Hat cone
            DrawTriangle(tex,
                new Vector2(cx - r * 0.38f, baseY + r * 0.05f),
                new Vector2(cx, topY),
                new Vector2(cx + r * 0.38f, baseY + r * 0.05f), hat);

            // Brim
            DrawRect(tex, cx - brimW, baseY - r * 0.07f, brimW * 2f, r * 0.18f, brim);
            DrawRect(tex, cx - brimW, baseY - r * 0.07f, brimW * 2f, 2f, outline);

            // Gold buckle
            DrawRect(tex, cx - r * 0.1f, baseY, r * 0.2f, r * 0.16f, new Color(1f, 0.85f, 0.15f));

            // Stars on hat
            DrawFilledCircle(tex, cx - r * 0.15f, baseY + r * 0.55f, r * 0.04f, Color.yellow);
            DrawFilledCircle(tex, cx + r * 0.1f, baseY + r * 0.85f, r * 0.03f, Color.cyan);
        }

        // ─── Neko Ears ─────────────────────────────────────────────
        static void DrawNekoEars(Texture2D tex, float cx, float cy, float r, Color bodyCol, Color outline)
        {
            Color inner = new Color(1f, 0.72f, 0.82f);
            float earW = r * 0.2f;
            float earH = r * 0.38f;
            float earBaseY = cy + r * 0.72f;

            // Left ear
            DrawOval(tex, cx - r * 0.52f, earBaseY + earH * 0.5f, earW, earH, outline);
            DrawOval(tex, cx - r * 0.52f, earBaseY + earH * 0.5f, earW * 0.65f, earH * 0.72f, bodyCol);
            DrawOval(tex, cx - r * 0.52f, earBaseY + earH * 0.5f, earW * 0.35f, earH * 0.45f, inner);
            // Right ear
            DrawOval(tex, cx + r * 0.52f, earBaseY + earH * 0.5f, earW, earH, outline);
            DrawOval(tex, cx + r * 0.52f, earBaseY + earH * 0.5f, earW * 0.65f, earH * 0.72f, bodyCol);
            DrawOval(tex, cx + r * 0.52f, earBaseY + earH * 0.5f, earW * 0.35f, earH * 0.45f, inner);
        }

        // ─── Ninja Mask ─────────────────────────────────────────────
        static void DrawNinjaMask(Texture2D tex, float cx, float cy, float r, Color outline)
        {
            Color maskCol = new Color(0.08f, 0.05f, 0.18f, 0.82f);
            float maskY   = cy - r * 0.1f;
            DrawOval(tex, cx, maskY, r * 0.75f, r * 0.28f, maskCol);
            DrawOval(tex, cx, maskY, r * 0.75f, r * 0.28f, outline);
            // Eye slit
            DrawRect(tex, cx - r * 0.55f, maskY - r * 0.06f, r * 1.1f, r * 0.12f, new Color(0f, 0f, 0f, 0f));
        }

        // ─── Antlers ────────────────────────────────────────────────
        static void DrawAntlers(Texture2D tex, float cx, float cy, float r, Color outline)
        {
            Color antler = new Color(0.55f, 0.38f, 0.18f);
            float baseY  = cy + r * 0.72f;
            // Left antler
            DrawThickLine(tex, cx - r * 0.35f, baseY, cx - r * 0.75f, baseY + r * 0.62f, antler, 4f);
            DrawThickLine(tex, cx - r * 0.75f, baseY + r * 0.62f, cx - r * 1.0f, baseY + r * 0.44f, antler, 3f);
            DrawThickLine(tex, cx - r * 0.75f, baseY + r * 0.62f, cx - r * 0.62f, baseY + r * 0.85f, antler, 3f);
            // Right antler
            DrawThickLine(tex, cx + r * 0.35f, baseY, cx + r * 0.75f, baseY + r * 0.62f, antler, 4f);
            DrawThickLine(tex, cx + r * 0.75f, baseY + r * 0.62f, cx + r * 1.0f, baseY + r * 0.44f, antler, 3f);
            DrawThickLine(tex, cx + r * 0.75f, baseY + r * 0.62f, cx + r * 0.62f, baseY + r * 0.85f, antler, 3f);
        }

        // ─── Demon Horns ────────────────────────────────────────────
        static void DrawDemonHorns(Texture2D tex, float cx, float cy, float r, Color hornColor, Color outline)
        {
            float baseY = cy + r * 0.72f;
            float hw = r * 0.15f;
            float hh = r * 0.42f;
            // Left horn
            DrawTriangle(tex,
                new Vector2(cx - r * 0.55f - hw, baseY),
                new Vector2(cx - r * 0.62f, baseY + hh),
                new Vector2(cx - r * 0.55f + hw, baseY), hornColor);
            // Right horn
            DrawTriangle(tex,
                new Vector2(cx + r * 0.55f - hw, baseY),
                new Vector2(cx + r * 0.62f, baseY + hh),
                new Vector2(cx + r * 0.55f + hw, baseY), hornColor);
        }

        // ─── Golden Halo ────────────────────────────────────────────
        static void DrawHalo(Texture2D tex, float cx, float cy, float r, Color haloColor, Color outline)
        {
            float haloY = cy + r * 0.88f;
            float haloRX = r * 0.62f;
            float haloRY = r * 0.14f;
            DrawOvalAlpha(tex, cx, haloY, haloRX + 3f, haloRY + 3f, outline);
            DrawOvalRing(tex, cx, haloY, haloRX, haloRY, r * 0.07f, haloColor);
        }

        // ─── Skull Horns (Necromancer) ──────────────────────────────
        static void DrawSkullHorns(Texture2D tex, float cx, float cy, float r, Color outline)
        {
            Color skullCol = new Color(0.92f, 0.92f, 0.88f);
            Color darkCol  = new Color(0.1f, 0.08f, 0.15f);
            float baseY    = cy + r * 0.72f;

            // Left twisted horn
            DrawThickLine(tex, cx - r * 0.45f, baseY, cx - r * 0.55f, baseY + r * 0.35f, skullCol, 4f);
            DrawThickLine(tex, cx - r * 0.55f, baseY + r * 0.35f, cx - r * 0.4f, baseY + r * 0.55f, skullCol, 3.5f);
            DrawThickLine(tex, cx - r * 0.4f, baseY + r * 0.55f, cx - r * 0.52f, baseY + r * 0.7f, skullCol, 3f);

            // Right twisted horn
            DrawThickLine(tex, cx + r * 0.45f, baseY, cx + r * 0.55f, baseY + r * 0.35f, skullCol, 4f);
            DrawThickLine(tex, cx + r * 0.55f, baseY + r * 0.35f, cx + r * 0.4f, baseY + r * 0.55f, skullCol, 3.5f);
            DrawThickLine(tex, cx + r * 0.4f, baseY + r * 0.55f, cx + r * 0.52f, baseY + r * 0.7f, skullCol, 3f);
        }

        // ─── Enemy Accessories ──────────────────────────────────────
        static void DrawSlimeBlobs(Texture2D tex, float cx, float cy, float r, Color bodyCol)
        {
            Color blobCol = new Color(bodyCol.r * 0.8f, bodyCol.g * 0.8f, bodyCol.b * 0.8f);
            DrawFilledCircle(tex, cx - r * 0.62f, cy + r * 0.52f, r * 0.2f, blobCol);
            DrawFilledCircle(tex, cx + r * 0.72f, cy + r * 0.42f, r * 0.15f, blobCol);
            DrawFilledCircle(tex, cx + r * 0.22f, cy - r * 0.68f, r * 0.18f, blobCol);
        }

        static void DrawGoblinEars(Texture2D tex, float cx, float cy, float r, Color bodyCol, Color outline)
        {
            float earW = r * 0.14f;
            float earH = r * 0.3f;
            float earY = cy + r * 0.35f;
            // Left
            DrawTriangle(tex, new Vector2(cx - r, earY + earH * 0.3f),
                              new Vector2(cx - r * 1.45f, earY + earH),
                              new Vector2(cx - r * 0.85f, earY - earH * 0.1f), outline);
            DrawTriangle(tex, new Vector2(cx - r + 2, earY + earH * 0.25f),
                              new Vector2(cx - r * 1.35f, earY + earH * 0.85f),
                              new Vector2(cx - r * 0.9f, earY), bodyCol);
            // Right
            DrawTriangle(tex, new Vector2(cx + r, earY + earH * 0.3f),
                              new Vector2(cx + r * 1.45f, earY + earH),
                              new Vector2(cx + r * 0.85f, earY - earH * 0.1f), outline);
            DrawTriangle(tex, new Vector2(cx + r - 2, earY + earH * 0.25f),
                              new Vector2(cx + r * 1.35f, earY + earH * 0.85f),
                              new Vector2(cx + r * 0.9f, earY), bodyCol);
        }

        static void DrawTusks(Texture2D tex, float cx, float cy, float r, Color outline)
        {
            Color tusk = Color.white;
            float tuskY = cy - r * 0.32f;
            DrawTriangle(tex, new Vector2(cx - r * 0.3f, tuskY),
                              new Vector2(cx - r * 0.22f, tuskY - r * 0.32f),
                              new Vector2(cx - r * 0.14f, tuskY), tusk);
            DrawTriangle(tex, new Vector2(cx + r * 0.14f, tuskY),
                              new Vector2(cx + r * 0.22f, tuskY - r * 0.32f),
                              new Vector2(cx + r * 0.30f, tuskY), tusk);
        }

        static void DrawDemonWingBumps(Texture2D tex, float cx, float cy, float r, Color outline)
        {
            Color wing = new Color(0.4f, 0.05f, 0.08f, 0.8f);
            // Two wing bumps on sides
            DrawFilledCircle(tex, cx - r * 0.9f, cy + r * 0.2f, r * 0.3f, wing);
            DrawFilledCircle(tex, cx + r * 0.9f, cy + r * 0.2f, r * 0.3f, wing);
        }

        static void DrawGhostWisp(Texture2D tex, float cx, float cy, float r, Color glowColor)
        {
            Color wisp = new Color(glowColor.r, glowColor.g, glowColor.b, 0.4f);
            DrawFilledCircle(tex, cx - r * 0.88f, cy + r * 0.0f, r * 0.22f, wisp);
            DrawFilledCircle(tex, cx + r * 0.88f, cy - r * 0.15f, r * 0.18f, wisp);
        }

        static void DrawFireCrown(Texture2D tex, float cx, float cy, float r, Color outline, Color fireColor)
        {
            // First draw base crown
            DrawCrown(tex, cx, cy, r, outline);

            // Then add fire effect above crown points
            Color fireMid  = new Color(1f, 0.55f, 0.05f, 0.8f);
            Color fireTip  = new Color(1f, 0.9f, 0.2f, 0.5f);
            float crownTop = cy + r * 0.72f;

            for (int i = 0; i < 5; i++)
            {
                float fx = cx + (i - 2) * r * 0.22f;
                float fh = r * (0.2f + Mathf.Abs(Mathf.Sin(i * 1.5f)) * 0.18f);
                DrawOvalAlpha(tex, fx, crownTop + r * 0.22f + fh * 0.5f, r * 0.08f, fh, fireMid);
                DrawOvalAlpha(tex, fx, crownTop + r * 0.22f + fh * 0.85f, r * 0.05f, fh * 0.5f, fireTip);
            }
        }

        static void DrawVampireCollar(Texture2D tex, float cx, float cy, float r, Color capeColor, Color outline)
        {
            // Two pointed collar edges at bottom of face
            float capeY = cy - r * 0.35f;
            DrawTriangle(tex, new Vector2(cx - r * 0.7f, capeY),
                              new Vector2(cx - r * 0.32f, capeY - r * 0.35f),
                              new Vector2(cx, capeY), capeColor);
            DrawTriangle(tex, new Vector2(cx, capeY),
                              new Vector2(cx + r * 0.32f, capeY - r * 0.35f),
                              new Vector2(cx + r * 0.7f, capeY), capeColor);
        }

        static void DrawWitchHat(Texture2D tex, float cx, float cy, float r, Color outline)
        {
            Color hat  = new Color(0.28f, 0.08f, 0.52f);
            Color brim = new Color(0.18f, 0.05f, 0.35f);
            float baseY = cy + r * 0.72f;
            float topY  = cy + r * 1.65f;

            // Slight tilt for personality
            DrawTriangle(tex,
                new Vector2(cx - r * 0.4f + r * 0.08f, baseY),
                new Vector2(cx + r * 0.12f, topY),
                new Vector2(cx + r * 0.48f + r * 0.08f, baseY), hat);
            // Brim
            DrawRect(tex, cx - r * 1.05f + r * 0.04f, baseY - r * 0.08f, r * 2.1f, r * 0.18f, brim);
            DrawRect(tex, cx - r * 1.05f + r * 0.04f, baseY + r * 0.08f, r * 2.1f, 2f, outline);
            // Moon buckle
            DrawFilledCircle(tex, cx + r * 0.15f, baseY, r * 0.08f, new Color(1f, 0.85f, 0.1f));
        }

        static void DrawGiantHorns(Texture2D tex, float cx, float cy, float r, Color hornColor, Color outline)
        {
            float baseY = cy + r * 0.72f;
            // Wide curved horns
            DrawThickLine(tex, cx - r * 0.4f, baseY, cx - r * 1.1f, baseY + r * 0.4f, hornColor, 6f);
            DrawThickLine(tex, cx - r * 1.1f, baseY + r * 0.4f, cx - r * 1.25f, baseY + r * 0.62f, hornColor, 4f);
            DrawThickLine(tex, cx + r * 0.4f, baseY, cx + r * 1.1f, baseY + r * 0.4f, hornColor, 6f);
            DrawThickLine(tex, cx + r * 1.1f, baseY + r * 0.4f, cx + r * 1.25f, baseY + r * 0.62f, hornColor, 4f);
        }

        // ─────────────────────────────────────────────────
        //  CHARACTER DEFINITIONS
        // ─────────────────────────────────────────────────
        struct OrbDef
        {
            public Color bodyColor;
            public Color bodyColorDark;
            public Color bodyColorHL;
            public Color outlineColor;
            public Color eyeColor;
            public Color eyePupilColor;
            public Color glowColor;
            public string accessory;
        }

        static OrbDef GetPlayerDef(string charId, int level)
        {
            switch (charId?.ToLower())
            {
                case "fighter":
                    return new OrbDef {
                        bodyColor    = new Color(0.96f, 0.38f, 0.22f),
                        bodyColorDark= new Color(0.62f, 0.14f, 0.06f),
                        bodyColorHL  = new Color(1.00f, 0.78f, 0.65f),
                        outlineColor = new Color(0.12f, 0.05f, 0.02f),
                        eyeColor     = new Color(1.00f, 1.00f, 1.00f),
                        eyePupilColor= new Color(0.15f, 0.08f, 0.35f),
                        glowColor    = new Color(1.00f, 0.55f, 0.15f, 1f),
                        accessory    = level >= 6 ? "crown" : "neko_ears"
                    };

                case "mage":
                    return new OrbDef {
                        bodyColor    = new Color(0.48f, 0.28f, 0.90f),
                        bodyColorDark= new Color(0.22f, 0.10f, 0.52f),
                        bodyColorHL  = new Color(0.82f, 0.72f, 1.00f),
                        outlineColor = new Color(0.08f, 0.04f, 0.20f),
                        eyeColor     = Color.white,
                        eyePupilColor= new Color(0.10f, 0.85f, 1.00f),
                        glowColor    = new Color(0.55f, 0.22f, 1.00f, 1f),
                        accessory    = "wizard_hat"
                    };

                case "assassin":
                    return new OrbDef {
                        bodyColor    = new Color(0.12f, 0.62f, 0.45f),
                        bodyColorDark= new Color(0.04f, 0.28f, 0.20f),
                        bodyColorHL  = new Color(0.62f, 1.00f, 0.82f),
                        outlineColor = new Color(0.02f, 0.08f, 0.06f),
                        eyeColor     = Color.white,
                        eyePupilColor= new Color(0.80f, 0.15f, 0.80f),
                        glowColor    = new Color(0.10f, 0.90f, 0.60f, 1f),
                        accessory    = "ninja_mask"
                    };

                case "ranger":
                    return new OrbDef {
                        bodyColor    = new Color(0.22f, 0.78f, 0.32f),
                        bodyColorDark= new Color(0.06f, 0.38f, 0.14f),
                        bodyColorHL  = new Color(0.72f, 1.00f, 0.78f),
                        outlineColor = new Color(0.02f, 0.10f, 0.04f),
                        eyeColor     = Color.white,
                        eyePupilColor= new Color(0.08f, 0.45f, 0.15f),
                        glowColor    = new Color(0.20f, 1.00f, 0.35f, 1f),
                        accessory    = "antlers"
                    };

                case "paladin":
                    return new OrbDef {
                        bodyColor    = new Color(1.00f, 0.82f, 0.18f),
                        bodyColorDark= new Color(0.70f, 0.48f, 0.05f),
                        bodyColorHL  = new Color(1.00f, 0.98f, 0.72f),
                        outlineColor = new Color(0.20f, 0.12f, 0.02f),
                        eyeColor     = Color.white,
                        eyePupilColor= new Color(0.90f, 0.55f, 0.10f),
                        glowColor    = new Color(1.00f, 0.90f, 0.25f, 1f),
                        accessory    = "golden_halo"
                    };

                case "necromancer":
                    return new OrbDef {
                        bodyColor    = new Color(0.22f, 0.18f, 0.38f),
                        bodyColorDark= new Color(0.06f, 0.04f, 0.15f),
                        bodyColorHL  = new Color(0.58f, 0.48f, 0.85f),
                        outlineColor = new Color(0.00f, 0.00f, 0.00f),
                        eyeColor     = Color.white,
                        eyePupilColor= new Color(0.55f, 0.10f, 0.85f),
                        glowColor    = new Color(0.45f, 0.08f, 0.85f, 1f),
                        accessory    = "skull_horns"
                    };

                case "druid":
                    return new OrbDef {
                        bodyColor    = new Color(0.35f, 0.68f, 0.28f),
                        bodyColorDark= new Color(0.12f, 0.32f, 0.08f),
                        bodyColorHL  = new Color(0.75f, 1.00f, 0.68f),
                        outlineColor = new Color(0.04f, 0.12f, 0.02f),
                        eyeColor     = Color.white,
                        eyePupilColor= new Color(0.62f, 0.88f, 0.25f),
                        glowColor    = new Color(0.25f, 0.85f, 0.22f, 1f),
                        accessory    = "antlers"
                    };

                default:
                    return new OrbDef {
                        bodyColor    = new Color(0.40f, 0.72f, 1.00f),
                        bodyColorDark= new Color(0.12f, 0.35f, 0.70f),
                        bodyColorHL  = new Color(0.80f, 0.92f, 1.00f),
                        outlineColor = new Color(0.04f, 0.08f, 0.20f),
                        eyeColor     = Color.white,
                        eyePupilColor= new Color(0.10f, 0.45f, 0.90f),
                        glowColor    = new Color(0.22f, 0.65f, 1.00f, 1f),
                        accessory    = "neko_ears"
                    };
            }
        }

        static OrbDef GetEnemyDef(string enemyType, bool isElite, bool isBoss)
        {
            OrbDef def;
            switch (enemyType?.ToLower())
            {
                case "slime":
                    def = new OrbDef {
                        bodyColor    = new Color(0.32f, 0.88f, 0.38f),
                        bodyColorDark= new Color(0.10f, 0.48f, 0.16f),
                        bodyColorHL  = new Color(0.72f, 1.00f, 0.75f),
                        outlineColor = new Color(0.04f, 0.18f, 0.05f),
                        eyeColor     = Color.white,
                        eyePupilColor= new Color(0.08f, 0.55f, 0.18f),
                        glowColor    = new Color(0.25f, 0.92f, 0.32f, 1f),
                        accessory    = "slime_blob"
                    }; break;

                case "goblin":
                    def = new OrbDef {
                        bodyColor    = new Color(0.48f, 0.72f, 0.22f),
                        bodyColorDark= new Color(0.22f, 0.40f, 0.06f),
                        bodyColorHL  = new Color(0.78f, 0.95f, 0.58f),
                        outlineColor = new Color(0.08f, 0.14f, 0.02f),
                        eyeColor     = Color.white,
                        eyePupilColor= new Color(0.85f, 0.12f, 0.12f),
                        glowColor    = new Color(0.52f, 0.85f, 0.12f, 1f),
                        accessory    = "goblin_ears"
                    }; break;

                case "skeleton":
                    def = new OrbDef {
                        bodyColor    = new Color(0.92f, 0.92f, 0.86f),
                        bodyColorDark= new Color(0.65f, 0.65f, 0.58f),
                        bodyColorHL  = new Color(1.00f, 1.00f, 0.96f),
                        outlineColor = new Color(0.15f, 0.15f, 0.12f),
                        eyeColor     = Color.white,
                        eyePupilColor= new Color(0.90f, 0.12f, 0.12f),
                        glowColor    = new Color(0.85f, 0.85f, 0.75f, 1f),
                        accessory    = "skeleton_skull"
                    }; break;

                case "orc":
                case "elite_orc":
                    def = new OrbDef {
                        bodyColor    = new Color(0.38f, 0.58f, 0.18f),
                        bodyColorDark= new Color(0.16f, 0.30f, 0.05f),
                        bodyColorHL  = new Color(0.68f, 0.90f, 0.45f),
                        outlineColor = new Color(0.06f, 0.10f, 0.02f),
                        eyeColor     = Color.white,
                        eyePupilColor= new Color(0.85f, 0.08f, 0.08f),
                        glowColor    = new Color(0.42f, 0.72f, 0.12f, 1f),
                        accessory    = "orc_tusks"
                    }; break;

                case "demon":
                case "elite_demon":
                    def = new OrbDef {
                        bodyColor    = new Color(0.82f, 0.18f, 0.18f),
                        bodyColorDark= new Color(0.42f, 0.04f, 0.04f),
                        bodyColorHL  = new Color(1.00f, 0.62f, 0.62f),
                        outlineColor = new Color(0.15f, 0.02f, 0.02f),
                        eyeColor     = Color.white,
                        eyePupilColor= new Color(1.00f, 0.62f, 0.05f),
                        glowColor    = new Color(1.00f, 0.18f, 0.08f, 1f),
                        accessory    = "demon_horns"
                    }; break;

                case "wraith":
                    def = new OrbDef {
                        bodyColor    = new Color(0.52f, 0.32f, 0.85f),
                        bodyColorDark= new Color(0.22f, 0.10f, 0.45f),
                        bodyColorHL  = new Color(0.82f, 0.72f, 1.00f),
                        outlineColor = new Color(0.08f, 0.04f, 0.18f),
                        eyeColor     = Color.white,
                        eyePupilColor= new Color(0.15f, 0.95f, 0.95f),
                        glowColor    = new Color(0.52f, 0.25f, 0.95f, 1f),
                        accessory    = "ghost_wisp"
                    }; break;

                case "golem":
                    def = new OrbDef {
                        bodyColor    = new Color(0.58f, 0.58f, 0.68f),
                        bodyColorDark= new Color(0.28f, 0.28f, 0.38f),
                        bodyColorHL  = new Color(0.88f, 0.88f, 0.95f),
                        outlineColor = new Color(0.10f, 0.10f, 0.15f),
                        eyeColor     = Color.white,
                        eyePupilColor= new Color(0.95f, 0.62f, 0.05f),
                        glowColor    = new Color(0.62f, 0.62f, 0.88f, 1f),
                        accessory    = "giant_horns"
                    }; break;

                case "vampire":
                    def = new OrbDef {
                        bodyColor    = new Color(0.88f, 0.88f, 0.95f),
                        bodyColorDark= new Color(0.55f, 0.55f, 0.72f),
                        bodyColorHL  = new Color(1.00f, 1.00f, 1.00f),
                        outlineColor = new Color(0.10f, 0.05f, 0.18f),
                        eyeColor     = Color.white,
                        eyePupilColor= new Color(0.92f, 0.08f, 0.08f),
                        glowColor    = new Color(0.72f, 0.08f, 0.20f, 1f),
                        accessory    = "vampire_collar"
                    }; break;

                case "witch":
                    def = new OrbDef {
                        bodyColor    = new Color(0.45f, 0.82f, 0.28f),
                        bodyColorDark= new Color(0.18f, 0.45f, 0.08f),
                        bodyColorHL  = new Color(0.78f, 1.00f, 0.65f),
                        outlineColor = new Color(0.06f, 0.14f, 0.02f),
                        eyeColor     = Color.white,
                        eyePupilColor= new Color(0.58f, 0.12f, 0.85f),
                        glowColor    = new Color(0.38f, 0.95f, 0.22f, 1f),
                        accessory    = "witch_hat"
                    }; break;

                case "giant":
                    def = new OrbDef {
                        bodyColor    = new Color(0.68f, 0.48f, 0.28f),
                        bodyColorDark= new Color(0.38f, 0.22f, 0.08f),
                        bodyColorHL  = new Color(0.95f, 0.78f, 0.58f),
                        outlineColor = new Color(0.14f, 0.08f, 0.02f),
                        eyeColor     = Color.white,
                        eyePupilColor= new Color(0.85f, 0.10f, 0.10f),
                        glowColor    = new Color(0.72f, 0.52f, 0.22f, 1f),
                        accessory    = "giant_horns"
                    }; break;

                case "boss_dragon":
                    def = new OrbDef {
                        bodyColor    = new Color(1.00f, 0.42f, 0.08f),
                        bodyColorDark= new Color(0.58f, 0.10f, 0.02f),
                        bodyColorHL  = new Color(1.00f, 0.82f, 0.52f),
                        outlineColor = new Color(0.18f, 0.04f, 0.00f),
                        eyeColor     = Color.white,
                        eyePupilColor= new Color(1.00f, 0.88f, 0.05f),
                        glowColor    = new Color(1.00f, 0.45f, 0.05f, 1f),
                        accessory    = "boss_crown_fire"
                    }; break;

                case "boss_lich":
                    def = new OrbDef {
                        bodyColor    = new Color(0.52f, 0.72f, 0.95f),
                        bodyColorDark= new Color(0.18f, 0.35f, 0.72f),
                        bodyColorHL  = new Color(0.85f, 0.92f, 1.00f),
                        outlineColor = new Color(0.05f, 0.08f, 0.20f),
                        eyeColor     = Color.white,
                        eyePupilColor= new Color(0.12f, 0.92f, 0.92f),
                        glowColor    = new Color(0.35f, 0.62f, 1.00f, 1f),
                        accessory    = "skull_horns"
                    }; break;

                case "boss_demon":
                    def = new OrbDef {
                        bodyColor    = new Color(0.92f, 0.12f, 0.12f),
                        bodyColorDark= new Color(0.48f, 0.02f, 0.02f),
                        bodyColorHL  = new Color(1.00f, 0.62f, 0.55f),
                        outlineColor = new Color(0.18f, 0.00f, 0.00f),
                        eyeColor     = Color.white,
                        eyePupilColor= new Color(1.00f, 0.72f, 0.05f),
                        glowColor    = new Color(1.00f, 0.08f, 0.08f, 1f),
                        accessory    = "demon_horns"
                    }; break;

                case "boss_vampire":
                    def = new OrbDef {
                        bodyColor    = new Color(0.78f, 0.10f, 0.38f),
                        bodyColorDark= new Color(0.38f, 0.02f, 0.14f),
                        bodyColorHL  = new Color(1.00f, 0.62f, 0.78f),
                        outlineColor = new Color(0.12f, 0.00f, 0.06f),
                        eyeColor     = Color.white,
                        eyePupilColor= new Color(0.92f, 0.08f, 0.08f),
                        glowColor    = new Color(0.88f, 0.08f, 0.42f, 1f),
                        accessory    = "crown"
                    }; break;

                case "boss_golem":
                    def = new OrbDef {
                        bodyColor    = new Color(0.72f, 0.72f, 0.45f),
                        bodyColorDark= new Color(0.38f, 0.38f, 0.18f),
                        bodyColorHL  = new Color(0.98f, 0.98f, 0.82f),
                        outlineColor = new Color(0.12f, 0.12f, 0.05f),
                        eyeColor     = Color.white,
                        eyePupilColor= new Color(0.92f, 0.65f, 0.05f),
                        glowColor    = new Color(0.88f, 0.88f, 0.42f, 1f),
                        accessory    = "giant_horns"
                    }; break;

                case "boss_witch":
                    def = new OrbDef {
                        bodyColor    = new Color(0.55f, 0.18f, 0.88f),
                        bodyColorDark= new Color(0.25f, 0.05f, 0.48f),
                        bodyColorHL  = new Color(0.88f, 0.72f, 1.00f),
                        outlineColor = new Color(0.10f, 0.02f, 0.18f),
                        eyeColor     = Color.white,
                        eyePupilColor= new Color(0.22f, 0.95f, 0.52f),
                        glowColor    = new Color(0.62f, 0.15f, 1.00f, 1f),
                        accessory    = "witch_hat"
                    }; break;

                default:
                    def = new OrbDef {
                        bodyColor    = new Color(0.72f, 0.35f, 0.15f),
                        bodyColorDark= new Color(0.38f, 0.12f, 0.04f),
                        bodyColorHL  = new Color(1.00f, 0.72f, 0.52f),
                        outlineColor = new Color(0.14f, 0.05f, 0.02f),
                        eyeColor     = Color.white,
                        eyePupilColor= new Color(0.85f, 0.12f, 0.12f),
                        glowColor    = new Color(0.85f, 0.42f, 0.12f, 1f),
                        accessory    = "orc_tusks"
                    }; break;
            }

            // Elite tint: brighter/gold shift
            if (isElite && !isBoss)
            {
                def.bodyColor     = Color.Lerp(def.bodyColor, new Color(1f, 0.85f, 0.2f), 0.35f);
                def.bodyColorHL   = Color.Lerp(def.bodyColorHL, Color.white, 0.3f);
                def.glowColor     = new Color(1f, 0.88f, 0.2f, 1f);
            }

            return def;
        }

        // ─────────────────────────────────────────────────
        //  PRIMITIVE DRAWING HELPERS
        // ─────────────────────────────────────────────────
        static void DrawOval(Texture2D tex, float cx, float cy, float rx, float ry, Color col)
        {
            int x0 = Mathf.Clamp((int)(cx - rx) - 1, 0, tex.width  - 1);
            int x1 = Mathf.Clamp((int)(cx + rx) + 1, 0, tex.width  - 1);
            int y0 = Mathf.Clamp((int)(cy - ry) - 1, 0, tex.height - 1);
            int y1 = Mathf.Clamp((int)(cy + ry) + 1, 0, tex.height - 1);

            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = (x - cx) / rx;
                float dy = (y - cy) / ry;
                if (dx * dx + dy * dy <= 1.02f)
                    tex.SetPixel(x, y, col);
            }
        }

        static void DrawOvalAlpha(Texture2D tex, float cx, float cy, float rx, float ry, Color col)
        {
            int x0 = Mathf.Clamp((int)(cx - rx) - 1, 0, tex.width  - 1);
            int x1 = Mathf.Clamp((int)(cx + rx) + 1, 0, tex.width  - 1);
            int y0 = Mathf.Clamp((int)(cy - ry) - 1, 0, tex.height - 1);
            int y1 = Mathf.Clamp((int)(cy + ry) + 1, 0, tex.height - 1);

            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = (x - cx) / rx;
                float dy = (y - cy) / ry;
                if (dx * dx + dy * dy <= 1.02f)
                {
                    Color existing = tex.GetPixel(x, y);
                    tex.SetPixel(x, y, Color.Lerp(existing, col, col.a));
                }
            }
        }

        static void DrawOvalRing(Texture2D tex, float cx, float cy, float rx, float ry, float ringW, Color col)
        {
            float innerRX = rx - ringW, innerRY = ry - ringW;
            int x0 = Mathf.Clamp((int)(cx - rx) - 1, 0, tex.width  - 1);
            int x1 = Mathf.Clamp((int)(cx + rx) + 1, 0, tex.width  - 1);
            int y0 = Mathf.Clamp((int)(cy - ry) - 1, 0, tex.height - 1);
            int y1 = Mathf.Clamp((int)(cy + ry) + 1, 0, tex.height - 1);

            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float outerV = (x - cx) * (x - cx) / (rx * rx) + (y - cy) * (y - cy) / (ry * ry);
                float innerV = (innerRX > 0 && innerRY > 0)
                             ? (x - cx) * (x - cx) / (innerRX * innerRX) + (y - cy) * (y - cy) / (innerRY * innerRY)
                             : float.MaxValue;
                if (outerV <= 1.02f && innerV >= 1f)
                {
                    Color existing = tex.GetPixel(x, y);
                    tex.SetPixel(x, y, Color.Lerp(existing, col, col.a));
                }
            }
        }

        static void DrawFilledCircle(Texture2D tex, float cx, float cy, float r, Color col)
        {
            int x0 = Mathf.Clamp((int)(cx - r) - 1, 0, tex.width  - 1);
            int x1 = Mathf.Clamp((int)(cx + r) + 1, 0, tex.width  - 1);
            int y0 = Mathf.Clamp((int)(cy - r) - 1, 0, tex.height - 1);
            int y1 = Mathf.Clamp((int)(cy + r) + 1, 0, tex.height - 1);
            float r2 = r * r;

            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = x - cx, dy = y - cy;
                if (dx * dx + dy * dy <= r2)
                    tex.SetPixel(x, y, col);
            }
        }

        static void DrawThickLine(Texture2D tex, float x0, float y0, float x1, float y1, Color col, float thickness)
        {
            int steps = (int)(Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0)) * 2f) + 2;
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float px = Mathf.Lerp(x0, x1, t);
                float py = Mathf.Lerp(y0, y1, t);
                DrawFilledCircle(tex, px, py, thickness * 0.5f, col);
            }
        }

        static void DrawRect(Texture2D tex, float x, float y, float w, float h, Color col)
        {
            int x0 = Mathf.Clamp((int)x,       0, tex.width  - 1);
            int x1 = Mathf.Clamp((int)(x + w), 0, tex.width  - 1);
            int y0 = Mathf.Clamp((int)y,       0, tex.height - 1);
            int y1 = Mathf.Clamp((int)(y + h), 0, tex.height - 1);
            for (int j = y0; j <= y1; j++)
            for (int i = x0; i <= x1; i++)
                tex.SetPixel(i, j, col);
        }

        static void DrawTriangle(Texture2D tex, Vector2 p0, Vector2 p1, Vector2 p2, Color col)
        {
            int minX = Mathf.Clamp((int)Mathf.Min(p0.x, Mathf.Min(p1.x, p2.x)) - 1, 0, tex.width  - 1);
            int maxX = Mathf.Clamp((int)Mathf.Max(p0.x, Mathf.Max(p1.x, p2.x)) + 1, 0, tex.width  - 1);
            int minY = Mathf.Clamp((int)Mathf.Min(p0.y, Mathf.Min(p1.y, p2.y)) - 1, 0, tex.height - 1);
            int maxY = Mathf.Clamp((int)Mathf.Max(p0.y, Mathf.Max(p1.y, p2.y)) + 1, 0, tex.height - 1);

            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 p = new Vector2(x, y);
                float d0 = Cross2D(p1 - p0, p - p0);
                float d1 = Cross2D(p2 - p1, p - p1);
                float d2 = Cross2D(p0 - p2, p - p2);
                bool hasNeg = (d0 < 0) || (d1 < 0) || (d2 < 0);
                bool hasPos = (d0 > 0) || (d1 > 0) || (d2 > 0);
                if (!(hasNeg && hasPos))
                    tex.SetPixel(x, y, col);
            }
        }

        static float Cross2D(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        static Texture2D NewTex(int sz)
        {
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color clear = Color.clear;
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
                tex.SetPixel(x, y, clear);
            return tex;
        }
    }
}
