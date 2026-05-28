using System.Collections.Generic;
using UnityEngine;
using AngelArena.Core;
using AngelArena.Data;

namespace AngelArena.Graphics
{
    /// <summary>
    /// 2.5D Chibi Orb character renderer.
    /// Replaces old flat procedural art with premium glossy-sphere chibi style
    /// matching the reference art direction: glossy orb body, large expressive eyes,
    /// thick outline, neon glow, unique per-character accessories, walk/attack animations.
    /// </summary>
    public class CharacterVisuals : MonoBehaviour
    {
        [Header("Entity References")]
        public PlayerController playerCtrl;
        public EnemyController  enemyCtrl;

        [Header("Animation Configuration")]
        public float walkSpeed    = 10f;
        public float bobAmount    = 4f;
        public float tiltAmount   = 8f;
        public float breatheSpeed = 2.2f;
        public float breatheScale = 0.038f;

        // ── Scene hierarchy ──────────────────────────────────────────
        private GameObject     _shadowGO;
        private SpriteRenderer _shadowSR;

        private GameObject     _glowGO;
        private SpriteRenderer _glowSR;

        private GameObject     _bodyGO;
        private SpriteRenderer _bodySR;

        private GameObject     _eyesGO;    // overlay layer for dynamic eye changes only
        private SpriteRenderer _eyesSR;

        private GameObject     _handL, _handR;
        private SpriteRenderer _handLSR, _handRSR;

        private GameObject     _footL, _footR;
        private SpriteRenderer _footLSR, _footRSR;

        private GameObject     _weaponGO;
        private SpriteRenderer _weaponSR;

        private GameObject     _shieldGO;
        private SpriteRenderer _shieldSR;

        private GameObject     _auraGO;
        private SpriteRenderer _auraSR;

        // ── State ────────────────────────────────────────────────────
        private bool   _isPlayer;
        private float  _scaleSize = 55f;

        private string _lastBranch  = "fighter";
        private int    _lastLevel   = -1;
        private bool   _lastIsBoss;
        private bool   _lastIsElite;
        private bool   _lastIsMorphed;

        private float  _breatheTimer;
        private float  _walkTimer;
        private bool   _isBlinking;
        private float  _blinkTimer;

        private bool   _isDead;
        private bool   _isHit;
        private float  _hitTimer;
        private bool   _isAttacking;
        private float  _attackProgress;
        private float  _lastHp = -1f;

        private Vector2   _eyeLookDir;
        private Transform _playerTr;

        // ── Sprite caches ─────────────────────────────────────────────
        //  Key → Sprite  (avoid regenerating every frame)
        private static readonly Dictionary<string, Sprite> _spriteCache = new();

        // ─────────────────────────────────────────────────────────────
        private void Start()
        {
            _isPlayer = (playerCtrl != null);
            DetermineScale();
            RebuildHierarchy();
            RefreshSkin();

            var pGO = GameObject.FindWithTag("Player");
            if (pGO != null) _playerTr = pGO.transform;

            // Disable parent SpriteRenderer (it may already have a placeholder sprite)
            var parentSR = GetComponent<SpriteRenderer>();
            if (parentSR != null) parentSR.enabled = false;
        }

        // ─────────────────────────────────────────────────────────────
        private void DetermineScale()
        {
            if (_isPlayer)
            {
                _scaleSize = 58f;
            }
            else if (enemyCtrl != null)
            {
                var def = EnemyDefs.Get(enemyCtrl.EnemyName);
                if (enemyCtrl.isBoss)       _scaleSize = def.radius * 2.8f;
                else if (enemyCtrl.isElite) _scaleSize = def.radius * 2.0f;
                else                        _scaleSize = def.radius * 1.8f;
            }
        }

        // ─────────────────────────────────────────────────────────────
        private void RebuildHierarchy()
        {
            // Clear old children
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            float s = _scaleSize;

            // Shadow (flat ellipse under orb)
            _shadowGO = MakeChild("Shadow", 0);
            _shadowSR = _shadowGO.AddComponent<SpriteRenderer>();
            _shadowSR.sprite = GetOrCreate("shadow", () => MakeShadowSprite());
            _shadowGO.transform.localScale    = new Vector3(s * 1.2f, s * 0.28f, 1f);
            _shadowGO.transform.localPosition = new Vector3(0f, -s * 0.48f, 0.06f);

            // Neon glow ring (behind body)
            _glowGO = MakeChild("Glow", 1);
            _glowSR = _glowGO.AddComponent<SpriteRenderer>();
            _glowGO.transform.localScale = Vector3.one * (s * 1.0f);

            // Feet (behind body)
            _footL = MakeChild("FootL", 2);
            _footLSR = _footL.AddComponent<SpriteRenderer>();
            _footL.transform.localScale = Vector3.one * (s * 0.3f);

            _footR = MakeChild("FootR", 2);
            _footRSR = _footR.AddComponent<SpriteRenderer>();
            _footR.transform.localScale = Vector3.one * (s * 0.3f);

            // Body (main orb)
            _bodyGO = MakeChild("Body", 10);
            _bodySR = _bodyGO.AddComponent<SpriteRenderer>();
            _bodyGO.transform.localScale = Vector3.one * s;

            // Aura ring
            _auraGO = MakeChild("Aura", 9);
            _auraSR = _auraGO.AddComponent<SpriteRenderer>();
            _auraGO.transform.localScale = Vector3.one * (s * 1.85f);

            // Hands
            _handL = MakeChild("HandL", 12);
            _handLSR = _handL.AddComponent<SpriteRenderer>();
            _handL.transform.localScale = Vector3.one * (s * 0.26f);

            _handR = MakeChild("HandR", 12);
            _handRSR = _handR.AddComponent<SpriteRenderer>();
            _handR.transform.localScale = Vector3.one * (s * 0.26f);

            // Weapon / Shield
            _weaponGO = MakeChild("Weapon", 13);
            _weaponSR = _weaponGO.AddComponent<SpriteRenderer>();

            _shieldGO = MakeChild("Shield", 8);
            _shieldSR = _shieldGO.AddComponent<SpriteRenderer>();
        }

        private GameObject MakeChild(string name, int sortOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go;
        }

        // ─────────────────────────────────────────────────────────────
        private void Update()
        {
            ResolveCharacterState(out int level, out string branch, out bool isBoss,
                                  out bool isElite, out bool isMorphed,
                                  out bool moving, out Vector2 movDir);

            // Rebuild skin when key state changes
            if (level != _lastLevel || branch != _lastBranch
                || isBoss != _lastIsBoss || isElite != _lastIsElite
                || isMorphed != _lastIsMorphed)
            {
                _lastLevel    = level;
                _lastBranch   = branch;
                _lastIsBoss   = isBoss;
                _lastIsElite  = isElite;
                _lastIsMorphed= isMorphed;
                RefreshSkin();
            }

            // Hit detection via HP drop
            float curHp = _isPlayer
                ? (playerCtrl != null ? playerCtrl.Hp : 0f)
                : (enemyCtrl  != null ? enemyCtrl.Hp  : 0f);
            if (_lastHp < 0f) _lastHp = curHp;
            if (curHp < _lastHp && curHp > 0 && _lastHp > 0)
            {
                _isHit    = true;
                _hitTimer = 0.18f;
            }
            _lastHp = curHp;

            if (_hitTimer > 0)
            {
                _hitTimer -= Time.deltaTime;
                if (_hitTimer <= 0) { _isHit = false; RefreshEyes(); }
                SetFlashColor(new Color(1f, 0.25f, 0.25f, 1f));
            }
            else
            {
                ResetColor();
            }

            // Blink
            _blinkTimer -= Time.deltaTime;
            if (_blinkTimer <= 0f)
            {
                _isBlinking = !_isBlinking;
                _blinkTimer = _isBlinking ? 0.11f : Random.Range(2.8f, 5.5f);
                RefreshEyes();
            }

            // Attack
            UpdateAttackState();

            // Pupil look direction
            UpdateEyeLook(moving, movDir);

            // Animate
            AnimateLimbs(moving, movDir);
        }

        private void ResolveCharacterState(out int level, out string branch, out bool isBoss,
                                           out bool isElite, out bool isMorphed,
                                           out bool moving, out Vector2 movDir)
        {
            level   = 1;
            branch  = "fighter";
            isBoss  = false;
            isElite = false;
            isMorphed = false;
            moving  = false;
            movDir  = Vector2.zero;

            if (_isPlayer)
            {
                level  = playerCtrl != null ? playerCtrl.Level : 1;
                branch = playerCtrl?.characterData?.characterId?.ToLower() ?? "fighter";
                isMorphed = (branch == "necromancer")
                         && playerCtrl != null
                         && playerCtrl.Hp < playerCtrl.MaxHp * 0.3f;

                float h = Input.GetAxisRaw("Horizontal");
                float v = Input.GetAxisRaw("Vertical");
                movDir  = new Vector2(h, v);
                moving  = movDir.sqrMagnitude > 0.01f;
                _isDead = playerCtrl != null && !playerCtrl.IsAlive;
            }
            else if (enemyCtrl != null)
            {
                isBoss  = enemyCtrl.isBoss;
                isElite = enemyCtrl.isElite;
                moving  = enemyCtrl.IsMoving;
                movDir  = enemyCtrl.Velocity;
                _isDead = !enemyCtrl.IsAlive;

                string en = enemyCtrl.EnemyName?.ToLower() ?? "";
                if      (en.Contains("slime"))    { level = 1; branch = "slime"; }
                else if (en.Contains("goblin"))   { level = 2; branch = "goblin"; }
                else if (en.Contains("skeleton")) { level = 3; branch = "skeleton"; }
                else if (en.Contains("orc"))      { level = 4; branch = "orc"; }
                else if (en.Contains("demon"))    { level = 5; branch = isBoss ? "boss_demon" : "demon"; }
                else if (en.Contains("wraith"))   { level = 6; branch = "wraith"; }
                else if (en.Contains("golem"))    { level = 7; branch = isBoss ? "boss_golem" : "golem"; }
                else if (en.Contains("vampire"))  { level = 8; branch = isBoss ? "boss_vampire" : "vampire"; }
                else if (en.Contains("witch"))    { level = 9; branch = isBoss ? "boss_witch" : "witch"; }
                else if (en.Contains("giant"))    { level = 10; branch = "giant"; }
                else if (en.Contains("dragon"))   { level = 10; branch = "boss_dragon"; }
                else if (en.Contains("lich"))     { level = 10; branch = "boss_lich"; }
                else                              { level = 4; branch = "orc"; }
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  SKIN REFRESH
        // ─────────────────────────────────────────────────────────────
        private void RefreshSkin()
        {
            bool angry = _isAttacking && !_isDead;

            // ── Body orb ──────────────────────────────────────────
            string bodyKey = $"orb_body_{_lastBranch}_{_lastLevel}_{_lastIsMorphed}_{_lastIsBoss}_{_lastIsElite}";
            _bodySR.sprite = GetOrCreate(bodyKey, () =>
            {
                if (_isPlayer)
                {
                    string charId = _lastIsMorphed ? "necro_morphed" : _lastBranch;
                    return OrbCharacterRenderer.GeneratePlayerOrb(charId, _lastLevel, false, false, false);
                }
                else
                {
                    return OrbCharacterRenderer.GenerateEnemyOrb(_lastBranch, _lastIsElite, _lastIsBoss, false, false);
                }
            });

            // ── Eyes (separate layer for dynamic state) ────────────
            RefreshEyes();

            // ── Feet ──────────────────────────────────────────────
            string footKey = $"foot_{_lastBranch}";
            var footSprite  = GetOrCreate(footKey, () => MakeFootSprite(_lastBranch));
            _footLSR.sprite = footSprite;
            _footRSR.sprite = footSprite;

            // ── Hands ─────────────────────────────────────────────
            string handKey = $"hand_{_lastBranch}";
            var handSprite  = GetOrCreate(handKey, () => MakeHandSprite(_lastBranch));
            _handLSR.sprite = handSprite;
            _handRSR.sprite = handSprite;

            // ── Aura ──────────────────────────────────────────────
            if (_lastIsBoss || _lastIsElite || _lastLevel >= 5)
            {
                _auraGO.SetActive(true);
                Color auraColor = GetAuraColor(_lastBranch, _lastIsBoss);
                string auraKey  = $"aura_{ColorUtility.ToHtmlStringRGB(auraColor)}";
                _auraSR.sprite  = GetOrCreate(auraKey, () => MakeAuraSprite(auraColor));
                _auraSR.color   = auraColor;
            }
            else
            {
                _auraGO.SetActive(false);
            }

            // ── Weapon / Shield ───────────────────────────────────
            if (_isPlayer)
            {
                _weaponGO.SetActive(true);
                _weaponSR.sprite = GetOrCreate($"wp2_{_lastBranch}", () => MakeWeaponSprite(_lastBranch));
                _weaponGO.transform.localScale = Vector3.one * (_scaleSize * 1.05f);

                bool hasShield = (_lastBranch == "fighter" || _lastBranch == "paladin");
                _shieldGO.SetActive(hasShield);
                if (hasShield)
                {
                    _shieldSR.sprite = GetOrCreate($"sh2_{_lastBranch}", () => MakeShieldSprite(_lastBranch));
                    _shieldGO.transform.localScale = Vector3.one * (_scaleSize * 0.75f);
                }
            }
            else
            {
                _weaponGO.SetActive(false);
                _shieldGO.SetActive(false);
            }
        }

        private void RefreshEyes()
        {
            // eyes are baked into body sprite (OrbCharacterRenderer draws them inline)
            // For dynamic eye states we re-generate body sprite with the correct state
            bool angry   = _isAttacking && !_isDead;
            bool blinking = _isBlinking && !_isDead && !_isHit;

            string eyeState = _isDead ? "dead" : _isHit ? "hit" : _isBlinking ? "blink" : _isAttacking ? "angry" : "idle";
            string bodyKey2 = $"orb_body_{_lastBranch}_{_lastLevel}_{_lastIsMorphed}_{_lastIsBoss}_{_lastIsElite}_{eyeState}";

            _bodySR.sprite = GetOrCreate(bodyKey2, () =>
            {
                if (_isPlayer)
                {
                    string charId = _lastIsMorphed ? "necro_morphed" : _lastBranch;
                    return OrbCharacterRenderer.GeneratePlayerOrb(charId, _lastLevel, angry, blinking, _isDead);
                }
                else
                {
                    return OrbCharacterRenderer.GenerateEnemyOrb(_lastBranch, _lastIsElite, _lastIsBoss, angry, blinking);
                }
            });
        }

        // ─────────────────────────────────────────────────────────────
        //  ATTACK STATE
        // ─────────────────────────────────────────────────────────────
        private void UpdateAttackState()
        {
            bool wasAttacking = _isAttacking;
            _isAttacking  = false;
            _attackProgress = 0f;

            if (_isPlayer && playerCtrl?.skillSystem?.Skills != null)
            {
                foreach (var s in playerCtrl.skillSystem.Skills)
                {
                    if (s.data == null) continue;
                    float cd      = s.GetEffectiveCd(playerCtrl.CdMult);
                    float elapsed = Time.time - s.lastFired;
                    float animDur = Mathf.Min(0.35f, cd * 0.42f);
                    if (elapsed < animDur)
                    {
                        _isAttacking    = true;
                        _attackProgress = elapsed / animDur;
                        break;
                    }
                }
            }

            if (_isAttacking != wasAttacking) RefreshEyes();
        }

        // ─────────────────────────────────────────────────────────────
        //  EYE LOOK (just offset the body sprite slightly — low cost)
        // ─────────────────────────────────────────────────────────────
        private void UpdateEyeLook(bool moving, Vector2 movDir)
        {
            Vector2 target = Vector2.zero;

            if (_isPlayer)
            {
                if (moving)
                {
                    target = movDir.normalized;
                }
                else if (EnemySpawner.AllEnemies != null)
                {
                    float minD = 400f;
                    foreach (var en in EnemySpawner.AllEnemies)
                    {
                        if (en == null || !en.IsAlive) continue;
                        float d = Vector2.Distance(transform.position, en.transform.position);
                        if (d < minD) { minD = d; target = ((Vector2)(en.transform.position - transform.position)).normalized; }
                    }
                }
            }
            else if (_playerTr != null)
            {
                target = ((Vector2)(_playerTr.position - transform.position)).normalized;
            }

            _eyeLookDir = Vector2.Lerp(_eyeLookDir, target, Time.deltaTime * 7f);
        }

        // ─────────────────────────────────────────────────────────────
        //  ANIMATION
        // ─────────────────────────────────────────────────────────────
        private void AnimateLimbs(bool moving, Vector2 dir)
        {
            _breatheTimer += Time.deltaTime * breatheSpeed;
            float breathe = Mathf.Sin(_breatheTimer) * breatheScale;

            // Flip
            float fX = 1f;
            if (_isPlayer)
            {
                float h = Input.GetAxisRaw("Horizontal");
                if      (h < -0.01f) fX = -1f;
                else if (h >  0.01f) fX =  1f;
                else fX = transform.localScale.x < 0 ? -1f : 1f;
            }
            else if (dir.x < -0.01f) fX = -1f;

            transform.localScale = new Vector3(fX, 1f, 1f);

            // Walk bob
            float bob  = 0f;
            float tilt = 0f;
            if (moving)
            {
                _walkTimer += Time.deltaTime * walkSpeed;
                bob  = Mathf.Abs(Mathf.Sin(_walkTimer)) * bobAmount;
                tilt = Mathf.Sin(_walkTimer) * tiltAmount;
            }
            else
            {
                _walkTimer = 0f;
            }

            // Body breathe + bob
            float bScale = 1f + breathe;
            _bodyGO.transform.localScale    = new Vector3(bScale, bScale, 1f) * _scaleSize;
            _bodyGO.transform.localPosition = new Vector3(_eyeLookDir.x * 2f, bob, 0f);
            _bodyGO.transform.localRotation = Quaternion.Euler(0f, 0f, tilt);

            // Shadow squish
            float shadowScale = 1f + bob / (_scaleSize * 2f);
            _shadowGO.transform.localScale = new Vector3(_scaleSize * 1.2f * shadowScale, _scaleSize * 0.28f, 1f);

            // Hands
            float s = _scaleSize;
            Vector3 lhDefault = new Vector3(-s * 0.46f, -s * 0.12f, -0.05f);
            Vector3 rhDefault = new Vector3( s * 0.46f, -s * 0.12f, -0.05f);

            if (moving)
            {
                float sw = Mathf.Sin(_walkTimer) * 6f;
                _handL.transform.localPosition = lhDefault + new Vector3(0f,  sw, 0f);
                _handR.transform.localPosition = rhDefault + new Vector3(0f, -sw, 0f);
            }
            else
            {
                float hf = Mathf.Sin(_breatheTimer) * 2f;
                _handL.transform.localPosition = lhDefault + new Vector3(0f, hf, 0f);
                _handR.transform.localPosition = rhDefault + new Vector3(0f, -hf, 0f);
            }

            // Feet
            Vector3 lfDefault = new Vector3(-s * 0.28f, -s * 0.46f, 0.02f);
            Vector3 rfDefault = new Vector3( s * 0.28f, -s * 0.46f, 0.02f);

            if (moving)
            {
                float liftL = Mathf.Max(0f,  Mathf.Sin(_walkTimer)) * 7f;
                float liftR = Mathf.Max(0f, -Mathf.Sin(_walkTimer)) * 7f;
                float swF   = Mathf.Sin(_walkTimer) * 8f;
                _footL.transform.localPosition = lfDefault + new Vector3(swF,  liftL, 0f);
                _footR.transform.localPosition = rfDefault + new Vector3(-swF, liftR, 0f);
            }
            else
            {
                _footL.transform.localPosition = lfDefault;
                _footR.transform.localPosition = rfDefault;
            }

            // Aura spin
            if (_auraGO.activeSelf)
                _auraGO.transform.Rotate(0f, 0f, Time.deltaTime * 30f);

            // Weapon animation
            if (_isPlayer) AnimateWeapon();
        }

        private void AnimateWeapon()
        {
            float s = _scaleSize;
            Vector3 wpDefault   = new Vector3(s * 0.5f, -s * 0.06f, -0.1f);
            float   wpRotDefault = -20f;

            if (_lastBranch == "assassin")
            {
                _weaponGO.transform.localPosition = wpDefault;
                _weaponGO.transform.Rotate(0f, 0f, Time.deltaTime * (_isAttacking ? 900f : 260f));
            }
            else if (_isAttacking)
            {
                if (_lastBranch == "fighter" || _lastBranch == "paladin")
                {
                    float swing = Mathf.Lerp(55f, -105f, _attackProgress);
                    _weaponGO.transform.localPosition = wpDefault
                        + new Vector3(Mathf.Sin(_attackProgress * Mathf.PI) * s * 0.22f, 0f, 0f);
                    _weaponGO.transform.localRotation = Quaternion.Euler(0f, 0f, swing);

                    if (_shieldGO.activeSelf)
                        _shieldGO.transform.localPosition = new Vector3(-s * 0.48f
                            - Mathf.Sin(_attackProgress * Mathf.PI) * s * 0.15f, 0f, -0.08f);
                }
                else if (_lastBranch == "mage" || _lastBranch == "necromancer" || _lastBranch == "druid")
                {
                    float thrust = Mathf.Sin(_attackProgress * Mathf.PI) * s * 0.28f;
                    _weaponGO.transform.localPosition = wpDefault + new Vector3(thrust, 0f, 0f);
                    _weaponGO.transform.localRotation = Quaternion.Euler(0f, 0f,
                        wpRotDefault + Mathf.Sin(_attackProgress * Mathf.PI) * 22f);
                }
                else if (_lastBranch == "ranger")
                {
                    float pull = Mathf.Sin(_attackProgress * Mathf.PI) * s * 0.16f;
                    _weaponGO.transform.localPosition = wpDefault - new Vector3(pull, 0f, 0f);
                    _weaponGO.transform.localRotation = Quaternion.Euler(0f, 0f, wpRotDefault - pull * 2.5f);
                }
            }
            else
            {
                float fY = Mathf.Sin(_breatheTimer) * 1.8f;
                _weaponGO.transform.localPosition = wpDefault + new Vector3(0f, fY, 0f);
                _weaponGO.transform.localRotation = Quaternion.Euler(0f, 0f, wpRotDefault);

                if (_shieldGO.activeSelf)
                {
                    _shieldGO.transform.localPosition = new Vector3(-_scaleSize * 0.48f, -_scaleSize * 0.06f, -0.08f);
                    _shieldGO.transform.localRotation = Quaternion.identity;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  COLOR HELPERS
        // ─────────────────────────────────────────────────────────────
        private void SetFlashColor(Color c)
        {
            if (_bodySR)   _bodySR.color   = c;
            if (_handLSR)  _handLSR.color  = c;
            if (_handRSR)  _handRSR.color  = c;
            if (_footLSR)  _footLSR.color  = c;
            if (_footRSR)  _footRSR.color  = c;
            if (_weaponSR) _weaponSR.color = c;
        }

        private void ResetColor()
        {
            if (_bodySR)   _bodySR.color   = Color.white;
            if (_handLSR)  _handLSR.color  = Color.white;
            if (_handRSR)  _handRSR.color  = Color.white;
            if (_footLSR)  _footLSR.color  = Color.white;
            if (_footRSR)  _footRSR.color  = Color.white;
            if (_weaponSR) _weaponSR.color = Color.white;
        }

        // ─────────────────────────────────────────────────────────────
        //  HAND / FOOT / WEAPON / SHIELD SPRITES  (small procedural)
        // ─────────────────────────────────────────────────────────────
        private static Sprite MakeHandSprite(string branch)
        {
            int sz = 32;
            var tex = NewTex(sz);
            float c = sz / 2f;
            float r = sz * 0.36f;

            Color main, hl, outline;
            GetHandColors(branch, out main, out hl, out outline);

            // Round fist
            DrawCircle(tex, c, c, r, outline);
            DrawCircle(tex, c, c, r - 2f, main);
            DrawCircle(tex, c - r * 0.28f, c + r * 0.32f, r * 0.28f, hl);

            tex.Apply();
            return ToSprite(tex);
        }

        private static Sprite MakeFootSprite(string branch)
        {
            int sz = 32;
            var tex = NewTex(sz);
            float c  = sz / 2f;
            float rx = sz * 0.42f;
            float ry = sz * 0.28f;

            Color main, hl, outline;
            GetHandColors(branch, out main, out hl, out outline);

            DrawOval(tex, c, c, rx, ry, outline);
            DrawOval(tex, c, c, rx - 2f, ry - 2f, main);

            tex.Apply();
            return ToSprite(tex);
        }

        private static void GetHandColors(string branch, out Color main, out Color hl, out Color outline)
        {
            switch (branch)
            {
                case "fighter":
                    main = new Color(0.62f, 0.62f, 0.68f);
                    hl   = Color.white;
                    outline = new Color(0.15f, 0.15f, 0.18f);
                    break;
                case "paladin":
                    main = new Color(1f, 0.82f, 0.18f);
                    hl   = new Color(1f, 0.98f, 0.72f);
                    outline = new Color(0.20f, 0.12f, 0.02f);
                    break;
                case "mage":
                    main = new Color(0.48f, 0.28f, 0.90f);
                    hl   = new Color(0.82f, 0.72f, 1.00f);
                    outline = new Color(0.08f, 0.04f, 0.20f);
                    break;
                case "assassin":
                    main = new Color(0.12f, 0.62f, 0.45f);
                    hl   = new Color(0.62f, 1.00f, 0.82f);
                    outline = new Color(0.02f, 0.08f, 0.06f);
                    break;
                case "ranger":
                    main = new Color(0.22f, 0.78f, 0.32f);
                    hl   = new Color(0.72f, 1.00f, 0.78f);
                    outline = new Color(0.02f, 0.10f, 0.04f);
                    break;
                case "necromancer":
                    main = new Color(0.22f, 0.18f, 0.38f);
                    hl   = new Color(0.58f, 0.48f, 0.85f);
                    outline = Color.black;
                    break;
                default: // druid + fallback
                    main = new Color(0.35f, 0.68f, 0.28f);
                    hl   = new Color(0.75f, 1.00f, 0.68f);
                    outline = new Color(0.04f, 0.12f, 0.02f);
                    break;
            }
        }

        private static Sprite MakeWeaponSprite(string branch)
        {
            int sz = 64;
            var tex = NewTex(sz);
            float c = sz / 2f;

            switch (branch)
            {
                case "fighter":   DrawSword(tex, c, sz, new Color(0.78f, 0.78f, 0.85f), new Color(1f, 0.25f, 0.25f)); break;
                case "paladin":   DrawHammer(tex, c, sz, new Color(1f, 0.85f, 0.18f));   break;
                case "mage":      DrawStaff(tex, c, sz, new Color(0.22f, 0.72f, 1f));    break;
                case "assassin":  DrawShuriken(tex, c, sz, new Color(0.38f, 0.85f, 0.65f)); break;
                case "ranger":    DrawBow(tex, c, sz, new Color(0.22f, 0.72f, 0.32f));   break;
                case "necromancer": DrawSkullStaff(tex, c, sz);                           break;
                default:          DrawStaff(tex, c, sz, new Color(0.35f, 0.85f, 0.32f)); break;
            }

            tex.Apply();
            return ToSprite(tex);
        }

        private static Sprite MakeShieldSprite(string branch)
        {
            int sz = 48;
            var tex = NewTex(sz);
            float c = sz / 2f;
            float r = sz * 0.42f;

            Color outer = branch == "paladin" ? new Color(1f, 0.82f, 0.18f) : new Color(0.62f, 0.62f, 0.68f);
            Color inner = branch == "paladin" ? new Color(0.85f, 0.55f, 0.08f) : Color.white;
            Color emb   = branch == "paladin" ? new Color(0.88f, 0.12f, 0.12f) : new Color(1f, 0.82f, 0.18f);

            DrawCircle(tex, c, c, r, new Color(0.12f, 0.08f, 0.02f));
            DrawCircle(tex, c, c, r - 2.5f, outer);
            DrawCircle(tex, c, c, r * 0.62f, inner);
            DrawCircle(tex, c, c, r * 0.22f, emb);

            tex.Apply();
            return ToSprite(tex);
        }

        // ─── Weapon shapes ────────────────────────────────────────────
        static void DrawSword(Texture2D tex, float c, int sz, Color blade, Color rune)
        {
            int cx = (int)c;
            for (int y = 6; y < sz - 6; y++)
            {
                for (int x = cx - 3; x <= cx + 3; x++)
                {
                    if (y > sz - 14) tex.SetPixel(x, y, new Color(0.4f, 0.2f, 0.1f));          // hilt
                    else if (y > sz - 18) tex.SetPixel(x, y, new Color(1f, 0.85f, 0.18f));      // guard
                    else
                    {
                        Color col = Color.Lerp(Color.white, blade, Mathf.Abs(x - cx) / 3f);
                        if (x == cx && y > 20 && y < sz - 20) col = rune;
                        tex.SetPixel(x, y, col);
                    }
                }
            }
        }

        static void DrawHammer(Texture2D tex, float c, int sz, Color gold)
        {
            int cx = (int)c;
            for (int y = 4; y < sz - 4; y++)
            for (int x = cx - 9; x <= cx + 9; x++)
            {
                bool shaft = y > 22 && x >= cx - 2 && x <= cx + 2;
                bool head  = y <= 22 && y >= 5;
                if (head)   tex.SetPixel(x, y, Mathf.Abs(x - cx) > 7 || y <= 9 || y >= 18
                                ? new Color(0.88f, 0.65f, 0.08f) : gold);
                else if (shaft) tex.SetPixel(x, y, new Color(0.45f, 0.22f, 0.10f));
            }
        }

        static void DrawStaff(Texture2D tex, float c, int sz, Color gemColor)
        {
            int cx = (int)c;
            for (int y = 4; y < sz - 4; y++)
            for (int x = cx - 4; x <= cx + 4; x++)
            {
                bool gem  = y < 16 && x >= cx - 4 && x <= cx + 4;
                bool shaft= y >= 16 && x >= cx - 1 && x <= cx + 1;
                if (gem)
                {
                    float f = Vector2.Distance(new Vector2(x, y), new Vector2(cx, 8f)) / 5f;
                    tex.SetPixel(x, y, Color.Lerp(Color.white, gemColor, f));
                }
                else if (shaft) tex.SetPixel(x, y, new Color(0.5f, 0.3f, 0.12f));
            }
        }

        static void DrawShuriken(Texture2D tex, float c, int sz, Color col)
        {
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float dx = x - c, dy = y - c;
                float dist  = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx);
                float r     = c * 0.78f * (0.3f + 0.7f * Mathf.Abs(Mathf.Cos(4f * angle)));
                if (dist < r) tex.SetPixel(x, y, Color.Lerp(Color.white, col, dist / c));
            }
        }

        static void DrawBow(Texture2D tex, float c, int sz, Color bowColor)
        {
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float dx = x - c * 0.4f, dy = y - c;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > c * 0.72f && dist < c * 0.88f && x > c * 0.35f)
                    tex.SetPixel(x, y, bowColor);
                if (x == (int)(sz * 0.34f) && y > c * 0.12f && y < sz - c * 0.12f)
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.55f));
            }
            for (int x = (int)(c * 0.18f); x < (int)(sz * 0.82f); x++)
                tex.SetPixel(x, (int)c, new Color(0.48f, 0.28f, 0.15f));
            tex.SetPixel((int)(sz * 0.82f), (int)c, Color.red);
        }

        static void DrawSkullStaff(Texture2D tex, float c, int sz)
        {
            int cx = (int)c;
            for (int y = 4; y < sz - 4; y++)
            for (int x = cx - 5; x <= cx + 5; x++)
            {
                bool skull = y < 20 && x >= cx - 4 && x <= cx + 4;
                bool shaft = y >= 20 && x >= cx - 1 && x <= cx + 1;
                if (skull)
                {
                    bool eye = (y == 11 || y == 12) && (x == cx - 2 || x == cx + 2);
                    tex.SetPixel(x, y, eye ? new Color(0.2f, 0.95f, 0.38f) : new Color(0.92f, 0.92f, 0.88f));
                }
                else if (shaft) tex.SetPixel(x, y, Color.gray);
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  SHADOW / AURA
        // ─────────────────────────────────────────────────────────────
        private static Sprite MakeShadowSprite()
        {
            int sz = 64;
            var tex = NewTex(sz);
            float c = sz / 2f;
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float dx = (x - c) / (sz * 0.46f);
                float dy = (y - c) / (sz * 0.44f);
                float d  = dx * dx + dy * dy;
                if (d < 1f) tex.SetPixel(x, y, new Color(0f, 0f, 0f, (1f - d) * 0.42f));
            }
            tex.Apply();
            return ToSprite(tex);
        }

        private static Sprite MakeAuraSprite(Color c)
        {
            int sz = 128;
            var tex = NewTex(sz);
            float ctr = sz / 2f;
            float outerR = ctr * 0.92f;
            float innerR = ctr * 0.72f;

            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float d = Mathf.Sqrt((x - ctr) * (x - ctr) + (y - ctr) * (y - ctr));
                if (d > innerR && d < outerR)
                {
                    float mid = (innerR + outerR) * 0.5f;
                    float t   = 1f - Mathf.Abs(d - mid) / ((outerR - innerR) * 0.5f);
                    // Rotating 8-point star pattern
                    float angle = Mathf.Atan2(y - ctr, x - ctr);
                    float star  = 0.6f + 0.4f * Mathf.Abs(Mathf.Cos(4f * angle));
                    tex.SetPixel(x, y, new Color(c.r, c.g, c.b, t * star * 0.7f));
                }
            }
            tex.Apply();
            return ToSprite(tex);
        }

        private static Color GetAuraColor(string branch, bool boss)
        {
            if (boss) return new Color(1f, 0.62f, 0.08f);
            switch (branch)
            {
                case "fighter":     return new Color(1.00f, 0.42f, 0.12f);
                case "mage":        return new Color(0.55f, 0.22f, 1.00f);
                case "assassin":    return new Color(0.12f, 0.88f, 0.60f);
                case "ranger":      return new Color(0.20f, 0.90f, 0.32f);
                case "paladin":     return new Color(1.00f, 0.88f, 0.18f);
                case "necromancer": return new Color(0.48f, 0.08f, 0.88f);
                case "druid":       return new Color(0.25f, 0.82f, 0.22f);
                case "demon":
                case "boss_demon":  return new Color(1.00f, 0.08f, 0.08f);
                case "slime":       return new Color(0.22f, 0.85f, 0.28f);
                case "wraith":      return new Color(0.55f, 0.22f, 0.95f);
                case "vampire":
                case "boss_vampire":return new Color(0.88f, 0.08f, 0.42f);
                case "boss_dragon": return new Color(1.00f, 0.42f, 0.05f);
                case "boss_lich":   return new Color(0.35f, 0.65f, 1.00f);
                default:            return new Color(0.85f, 0.85f, 0.28f);
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  SHARED DRAWING PRIMITIVES
        // ─────────────────────────────────────────────────────────────
        static void DrawCircle(Texture2D tex, float cx, float cy, float r, Color col)
        {
            int x0 = Mathf.Max(0, (int)(cx - r) - 1);
            int x1 = Mathf.Min(tex.width  - 1, (int)(cx + r) + 1);
            int y0 = Mathf.Max(0, (int)(cy - r) - 1);
            int y1 = Mathf.Min(tex.height - 1, (int)(cy + r) + 1);
            float r2 = r * r;
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = x - cx, dy = y - cy;
                if (dx * dx + dy * dy <= r2) tex.SetPixel(x, y, col);
            }
        }

        static void DrawOval(Texture2D tex, float cx, float cy, float rx, float ry, Color col)
        {
            int x0 = Mathf.Max(0, (int)(cx - rx) - 1);
            int x1 = Mathf.Min(tex.width  - 1, (int)(cx + rx) + 1);
            int y0 = Mathf.Max(0, (int)(cy - ry) - 1);
            int y1 = Mathf.Min(tex.height - 1, (int)(cy + ry) + 1);
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = (x - cx) / rx, dy = (y - cy) / ry;
                if (dx * dx + dy * dy <= 1.02f) tex.SetPixel(x, y, col);
            }
        }

        static Texture2D NewTex(int sz)
        {
            var t = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Bilinear;
            Color cl = Color.clear;
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
                t.SetPixel(x, y, cl);
            return t;
        }

        static Sprite ToSprite(Texture2D tex)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f, tex.width);
        }

        // ─────────────────────────────────────────────────────────────
        //  SPRITE CACHE
        // ─────────────────────────────────────────────────────────────
        private static Sprite GetOrCreate(string key, System.Func<Sprite> factory)
        {
            if (_spriteCache.TryGetValue(key, out var s) && s != null) return s;
            s = factory();
            _spriteCache[key] = s;
            return s;
        }

        // Keep API compat with SpriteFactory if it's referenced elsewhere
        public static void ClearCache() => _spriteCache.Clear();
    }
}
