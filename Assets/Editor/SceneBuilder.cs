#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using AngelArena.Core;
using AngelArena.Audio;
using AngelArena.Save;

namespace AngelArena.Editor
{
    /// <summary>
    /// One-click PVE scene builder.
    /// Menu: AngelArena → Setup → Build PVE Scene
    /// </summary>
    public static class SceneBuilder
    {
        [MenuItem("AngelArena/Setup/1. Build PVE GameScene")]
        public static void BuildPVEScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildCore(showDialog: true);
        }

        /// <summary>Silent version for AutoSetup — no Save dialog, no success dialog.</summary>
        public static void BuildPVESceneSilent()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildCore(showDialog: false);
        }

        private static void BuildCore(bool showDialog)
        {
            // ── Register Tags ──────────────────────────────────────
            RegisterTag("Player");
            RegisterTag("Enemy");

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

            // ── 1. Camera ─────────────────────────────────────────
            var camGO    = new GameObject("Main Camera");
            var cam      = camGO.AddComponent<Camera>();
            cam.orthographic     = true;
            cam.orthographicSize = 250f;   // Synced: shows 500 units height (HTML viewport)
            cam.clearFlags       = CameraClearFlags.SolidColor;
            cam.backgroundColor  = new Color(0.035f, 0.045f, 0.08f);
            cam.depth            = -1;
            camGO.tag            = "MainCamera";
            camGO.AddComponent<AudioListener>();
            camGO.AddComponent<CameraController>();
            camGO.AddComponent<AngelArena.Graphics.CameraFX>(); // damage flash
            SetPos(camGO, new Vector3(0, 0, -10));

            // ── 2. Directional Light (ambient) ─────────────────────
            var lightGO  = new GameObject("Ambient Light");
            var light    = lightGO.AddComponent<Light>();
            light.type   = LightType.Directional;
            light.color  = new Color(0.3f, 0.3f, 0.5f);
            light.intensity = 0.4f;

            // ── 3. GameManager ────────────────────────────────────
            var gmGO = new GameObject("GameManager");
            var gm   = gmGO.AddComponent<GameManager>();

            // ── 4. SkillPrefabs Registry ─────────────────────────
            var spGO = new GameObject("SkillPrefabs");
            spGO.AddComponent<SkillPrefabs>();

            // ── 5. Player ─────────────────────────────────────────
            var playerGO = new GameObject("Player");
            playerGO.tag  = "Player";
            // Add PlayerController first — [RequireComponent] auto-adds Rigidbody2D + CircleCollider2D
            var pc    = playerGO.AddComponent<PlayerController>();
            // Now GetComponent (already added by RequireComponent)
            var rb2d  = playerGO.GetComponent<Rigidbody2D>();
            if (rb2d == null) rb2d = playerGO.AddComponent<Rigidbody2D>();
            rb2d.gravityScale   = 0;
            rb2d.freezeRotation = true;
            rb2d.linearDamping  = 5f;
            var col   = playerGO.GetComponent<CircleCollider2D>();
            if (col == null) col = playerGO.AddComponent<CircleCollider2D>();
            col.radius    = 16f;
            col.isTrigger = false;
            var sys = playerGO.AddComponent<AngelArena.Core.SkillSystem>();
            pc.skillSystem = sys;

            // Player visual
            var playerSprite = CreateChildSprite(playerGO, "Sprite", Color.cyan, 28f);

            // Link camera to player
            var camCtrl = camGO.GetComponent<CameraController>();
            camCtrl.target = playerGO.transform;

            // ── 6. EnemySpawner ───────────────────────────────────
            var spawnerGO = new GameObject("EnemySpawner");
            spawnerGO.AddComponent<EnemySpawner>();

            // ── 7. Singletons (Audio, Save) ───────────────────────
            var audioGO = new GameObject("AudioManager");
            audioGO.AddComponent<AudioManager>();

            var saveGO  = new GameObject("SaveSystem");
            saveGO.AddComponent<SaveSystem>();

            var steamGO = new GameObject("SteamManager");
            steamGO.AddComponent<AngelArena.Steam.SteamManager>();

            var upgradeGO = new GameObject("UpgradeDatabase");
            upgradeGO.AddComponent<AngelArena.UI.UpgradeDatabase>();

            // ── 8. HUD Canvas ─────────────────────────────────────
            var canvasGO = BuildHUDCanvas(playerGO, gm);

            // ── 9. Link GameManager refs ──────────────────────────
            gm.playerController = pc;
            gm.enemySpawner     = spawnerGO.GetComponent<EnemySpawner>();
            gm.hudManager       = canvasGO.GetComponent<HUDManager>();

            // ── 10. GameVisualManager (handles all graphics, arena, HUD) ─────
            var visGO = new GameObject("GameVisualManager");
            visGO.AddComponent<AngelArena.Graphics.GameVisualManager>();

            // ── 11. HUD Canvas (GameVisualManager also creates its own) ─────
            var hudOverlayGO = new GameObject("HUDCanvas");
            var hudCanvas    = hudOverlayGO.AddComponent<Canvas>();
            hudCanvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            hudCanvas.sortingOrder = 100;
            var scaler = hudOverlayGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0.5f;
            hudOverlayGO.AddComponent<GraphicRaycaster>();
            hudOverlayGO.AddComponent<AngelArena.Graphics.HUDOverlay>();

            // ── 12. Basic arena placeholder (GameVisualManager builds the real one) ──
            BuildArenaGround();

            // ── Save Scene ────────────────────────────────────────
            string path = "Assets/Scenes/GameScene.unity";
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene(), path);
            AssetDatabase.Refresh();
            Debug.Log("[AngelArena] PVE GameScene built and saved to: " + path);
            if (showDialog)
                EditorUtility.DisplayDialog("Done!", "GameScene built!\nPath: " + path + "\n\nNext: Run Step 7 (Link Enemy Prefabs) then PLAY!", "OK");
        }

        [MenuItem("AngelArena/Setup/2. Fix Input Handler (Old System)")]
        public static void FixInputHandler()
        {
            var so = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var prop = so.FindProperty("activeInputHandler");
            if (prop != null) { prop.intValue = 0; so.ApplyModifiedProperties(); }
            PlayerSettings.allowedAutorotateToPortrait = PlayerSettings.allowedAutorotateToPortrait; // force save
            Debug.Log("[AngelArena] Input Handler set to: Old Input System (0)");
            EditorUtility.DisplayDialog("Fixed!", "Input Handler set to Old Input System.\nNo InputSystem package needed.", "OK");
        }

        [MenuItem("AngelArena/Setup/3. Set Project Settings")]
        public static void SetProjectSettings()
        {
            PlayerSettings.companyName = "Angel Arena Studio";
            PlayerSettings.productName = "Angel Arena";
#if UNITY_6000_0_OR_NEWER
            PlayerSettings.SetApplicationIdentifier(
                UnityEditor.Build.NamedBuildTarget.Standalone,
                "com.angelarenastudio.angelarena");
#else
            PlayerSettings.SetApplicationIdentifier(
                BuildTargetGroup.Standalone,
                "com.angelarenastudio.angelarena");
#endif
            PlayerSettings.defaultScreenWidth  = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            Debug.Log("[AngelArena] Project Settings applied!");
            EditorUtility.DisplayDialog("Done!", "Project settings applied:\n- Company: Angel Arena Studio\n- Product: Angel Arena\n- Resolution: 1920x1080", "OK");
        }

        // ── Helpers ───────────────────────────────────────────────
        private static GameObject BuildHUDCanvas(GameObject player, GameManager gm)
        {
            var canvasGO  = new GameObject("HUD Canvas");
            var canvas    = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler    = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();
            var hud = canvasGO.AddComponent<HUDManager>();

            // HP Bar
            hud.hpBar  = CreateSlider(canvasGO, "HP Bar",
                new Vector2(-750, 500), new Vector2(400, 30), new Color(0.9f, 0.2f, 0.2f));

            // XP Bar
            hud.xpBar  = CreateSlider(canvasGO, "XP Bar",
                new Vector2(0, -525), new Vector2(900, 20), new Color(0.3f, 0.8f, 1f));

            // Level Text
            hud.levelText = CreateText(canvasGO, "Level Text", "LV.1",
                new Vector2(-880, 500), new Vector2(80, 40), 22, Color.yellow);

            // Timer
            hud.survivalTimerText = CreateText(canvasGO, "Timer", "0:00",
                new Vector2(0, 520), new Vector2(200, 50), 32, Color.white);

            // Boss Bar (hidden by default)
            var bossRoot = new GameObject("Boss Bar Root");
            bossRoot.transform.SetParent(canvasGO.transform, false);
            var bossRectTr = bossRoot.AddComponent<RectTransform>();
            SetAnchors(bossRectTr, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(700, 50));
            bossRoot.SetActive(false);
            hud.bossBarRoot = bossRoot;
            hud.bossHpBar   = CreateSlider(bossRoot, "Boss HP", Vector2.zero, new Vector2(680, 35), new Color(1f, 0.4f, 0f));
            hud.bossNameText= CreateText(bossRoot, "Boss Name", "BOSS", Vector2.zero, new Vector2(680, 35), 18, Color.white);

            // Kill Streak (hidden)
            var streakRoot = new GameObject("Kill Streak Root");
            streakRoot.transform.SetParent(canvasGO.transform, false);
            var sRt = streakRoot.AddComponent<RectTransform>();
            SetAnchors(sRt, new Vector2(1,0.5f), new Vector2(1,0.5f), new Vector2(-120, 0), new Vector2(220, 60));
            streakRoot.SetActive(false);
            hud.killStreakRoot = streakRoot;
            hud.killStreakText = CreateText(streakRoot, "Streak Text", "5x STREAK!", Vector2.zero, new Vector2(220,60), 20, Color.yellow);

            // Vignette (full screen, transparent)
            var vignGO = new GameObject("Vignette");
            vignGO.transform.SetParent(canvasGO.transform, false);
            var vignImg = vignGO.AddComponent<Image>();
            vignImg.color = Color.clear;
            var vignRt  = vignGO.GetComponent<RectTransform>();
            vignRt.anchorMin = Vector2.zero;
            vignRt.anchorMax = Vector2.one;
            vignRt.offsetMin = Vector2.zero;
            vignRt.offsetMax = Vector2.zero;
            vignGO.GetComponent<Image>().raycastTarget = false;
            hud.vignetteImage = vignImg;

            // Boss Warning (hidden)
            var warnRoot = new GameObject("Boss Warning Root");
            warnRoot.transform.SetParent(canvasGO.transform, false);
            var wRt = warnRoot.AddComponent<RectTransform>();
            SetAnchors(wRt, new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), Vector2.zero, new Vector2(600, 80));
            warnRoot.AddComponent<Image>().color = new Color(0.8f, 0.2f, 0.1f, 0.85f);
            warnRoot.SetActive(false);
            hud.bossWarningRoot  = warnRoot;
            hud.bossWarningText  = CreateText(warnRoot, "Warning Text", "BOSS INCOMING!", Vector2.zero, new Vector2(600,80), 24, Color.white);

            // Upgrade Screen (hidden)
            var upgradeScreenGO = new GameObject("Upgrade Screen");
            upgradeScreenGO.transform.SetParent(canvasGO.transform, false);
            upgradeScreenGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.85f);
            var upgradeRt = upgradeScreenGO.GetComponent<RectTransform>();
            upgradeRt.anchorMin = Vector2.zero;
            upgradeRt.anchorMax = Vector2.one;
            upgradeRt.offsetMin = Vector2.zero;
            upgradeRt.offsetMax = Vector2.zero;
            var upgradeComp = upgradeScreenGO.AddComponent<AngelArena.Core.UIUpgradeScreen>();
            upgradeComp.screenRoot = upgradeScreenGO;
            upgradeScreenGO.SetActive(false);

            return canvasGO;
        }

        private static void BuildArenaGround()
        {
            var groundGO = new GameObject("Arena Ground");
            var sr = groundGO.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite(new Color(0.12f, 0.15f, 0.20f));
            sr.sortingOrder = -10;
            groundGO.transform.localScale = new Vector3(3840f, 2160f, 1f);

            // Arena border visual
            for (int i = 0; i < 4; i++)
            {
                var border   = new GameObject($"Border_{i}");
                var bsr      = border.AddComponent<SpriteRenderer>();
                bsr.sprite   = CreateSquareSprite(new Color(0.3f, 0.5f, 1f, 0.5f));
                bsr.sortingOrder = -5;
                float w = 3840f, h = 2160f, t = 20f;
                switch (i)
                {
                    case 0: border.transform.position = new Vector3(0, h/2, 0);  border.transform.localScale = new Vector3(w+t, t, 1); break;
                    case 1: border.transform.position = new Vector3(0, -h/2, 0); border.transform.localScale = new Vector3(w+t, t, 1); break;
                    case 2: border.transform.position = new Vector3(w/2, 0, 0);  border.transform.localScale = new Vector3(t, h, 1); break;
                    case 3: border.transform.position = new Vector3(-w/2, 0, 0); border.transform.localScale = new Vector3(t, h, 1); break;
                }
            }
        }

        // ── Utility helpers ───────────────────────────────────────
        private static void SetPos(GameObject go, Vector3 pos) => go.transform.position = pos;

        private static GameObject CreateChildSprite(GameObject parent, string name, Color col, float size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite(col);
            go.transform.localScale = Vector3.one * size;
            return go;
        }

        private static Slider CreateSlider(GameObject parent, string name, Vector2 pos, Vector2 size, Color fillColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            SetAnchors(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            // Fill area
            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(go.transform, false);
            var faRt = fillArea.AddComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero; faRt.anchorMax = Vector2.one;
            faRt.offsetMin = Vector2.zero; faRt.offsetMax = Vector2.zero;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = fillColor;
            var fillRt  = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;

            var slider = go.AddComponent<Slider>();
            slider.fillRect       = fillRt;
            slider.targetGraphic  = bg;
            slider.direction      = Slider.Direction.LeftToRight;
            slider.value          = 1f;
            slider.interactable   = false;

            return slider;
        }

        private static Text CreateText(GameObject parent, string name, string content,
            Vector2 pos, Vector2 size, int fontSize, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            SetAnchors(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
            var txt = go.AddComponent<Text>();
            txt.text      = content;
            txt.fontSize  = fontSize;
            txt.color     = color;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return txt;
        }

        private static void SetAnchors(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
        {
            rt.anchorMin        = aMin;
            rt.anchorMax        = aMax;
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
        }

        private static Sprite CreateCircleSprite(Color color)
        {
            int sz = 64;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            float c = sz / 2f;
            for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                    tex.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), Vector2.one * c) < c - 1 ? color : Color.clear);
            tex.Apply();
            // PPU = sz → sprite is exactly 1 unit wide at scale=1
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), (float)sz);
        }

        private static Sprite CreateSquareSprite(Color color)
        {
            var tex = new Texture2D(4, 4);
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    tex.SetPixel(x, y, color);
            tex.Apply();
            // PPU = 4 → sprite is exactly 1 unit wide at scale=1
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }
        // ── Tag Registration Helper ───────────────────────────────
        private static void RegisterTag(string tagName)
        {
            var tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tagsProperty = tagManager.FindProperty("tags");
            for (int i = 0; i < tagsProperty.arraySize; i++)
                if (tagsProperty.GetArrayElementAtIndex(i).stringValue == tagName) return; // already exists

            tagsProperty.InsertArrayElementAtIndex(tagsProperty.arraySize);
            tagsProperty.GetArrayElementAtIndex(tagsProperty.arraySize - 1).stringValue = tagName;
            tagManager.ApplyModifiedProperties();
            Debug.Log($"[AngelArena] Registered tag: '{tagName}'");
        }
    }
}
#endif
