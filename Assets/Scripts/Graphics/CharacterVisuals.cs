using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngelArena.Core;
using AngelArena.Data;

namespace AngelArena.Graphics
{
    /// <summary>
    /// Upgraded 2.5D procedural renderer for player characters and PVE enemies.
    /// Draws structured heads, class-specific hair/hats/helmets, customized garments/torso,
    /// dynamic eyes (blinking & angry attack expressions), cheeks, and hands/feet matching class style.
    /// </summary>
    public class CharacterVisuals : MonoBehaviour
    {
        [Header("Entity References")]
        public PlayerController playerCtrl;
        public EnemyController  enemyCtrl;

        [Header("Animation Configuration")]
        public float walkSpeed   = 12f;
        public float bobAmount    = 3.5f;
        public float tiltAmount   = 9f;     // degrees
        public float handSwing    = 7f;
        public float footSwing    = 9f;
        public float footLift     = 5f;
        public float breatheSpeed = 2.2f;
        public float breatheScale = 0.04f;

        // Limbs & Attachments
        private GameObject _bodyGO;
        private GameObject _handL;
        private GameObject _handR;
        private GameObject _footL;
        private GameObject _footR;
        private GameObject _weaponGO;
        private GameObject _shieldGO;
        private GameObject _wingL;
        private GameObject _wingR;
        private GameObject _auraGO;
        private GameObject _haloGO;
        private GameObject _shadowGO;

        // Renderers
        private SpriteRenderer _bodySR;
        private SpriteRenderer _handLSR;
        private SpriteRenderer _handRSR;
        private SpriteRenderer _footLSR;
        private SpriteRenderer _footRSR;
        private SpriteRenderer _weaponSR;
        private SpriteRenderer _shieldSR;
        private SpriteRenderer _wingLSR;
        private SpriteRenderer _wingRSR;
        private SpriteRenderer _auraSR;
        private SpriteRenderer _haloSR;
        private SpriteRenderer _shadowSR;

        // Custom Face Layer GOs
        private GameObject     _eyesGO;
        private SpriteRenderer _eyesSR;

        // Face & Anim States
        private bool           _isHit;
        private bool           _isDead;
        private float          _hitTimer;
        private float          _lastHp = -1f;
        private Vector2        _eyeLookDir;
        private Transform      _playerTr;

        // State caching
        private string _lastBranch;
        private int    _lastLevel = -1;
        private bool   _lastIsBoss;
        private bool   _lastIsElite;
        private bool   _lastIsMorphed;
        private bool   _isPlayer;
        private float  _scaleSize = 55f;

        private float _breatheTimer;
        private float _walkTimer;
        private float _hurtFlashTimer;
        private float _blinkTimer;
        private bool  _isBlinking;

        // Attack animation state
        private bool  _isAttacking;
        private float _attackProgress;

        private void Start()
        {
            _isPlayer = (playerCtrl != null);
            DetermineScale();
            RebuildVisualHierarchy();
            UpdateVisualSkin();

            var pGO = GameObject.FindWithTag("Player");
            if (pGO != null) _playerTr = pGO.transform;

            // Disable parent SpriteRenderer to avoid blocking/clashing with 2.5D visual components
            var parentSR = GetComponent<SpriteRenderer>();
            if (parentSR != null)
            {
                parentSR.enabled = false;
            }
        }

        private void DetermineScale()
        {
            if (_isPlayer)
            {
                _scaleSize = 55f;
            }
            else if (enemyCtrl != null)
            {
                var def = EnemyDefs.Get(enemyCtrl.EnemyName);
                _scaleSize = enemyCtrl.isBoss ? def.radius * 2.2f
                           : enemyCtrl.isElite ? def.radius * 1.6f
                           : def.radius * 2f;
            }
        }

        private void RebuildVisualHierarchy()
        {
            // Clear existing kids
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            // Shadow
            _shadowGO = CreateChild("Shadow", 1);
            _shadowSR = _shadowGO.AddComponent<SpriteRenderer>();
            _shadowSR.sprite = SpriteFactory.SolidCircle(new Color(0f, 0f, 0f, 0.35f));
            _shadowGO.transform.localScale = new Vector3(_scaleSize * 1.1f, _scaleSize * 0.35f, 1f);
            _shadowGO.transform.localPosition = new Vector3(0f, -_scaleSize * 0.45f, 0.05f);

            // Aura Ring
            _auraGO = CreateChild("AuraRing", 2);
            _auraSR = _auraGO.AddComponent<SpriteRenderer>();

            // Wings (Behind body)
            _wingL = CreateChild("WingLeft", 3);
            _wingLSR = _wingL.AddComponent<SpriteRenderer>();
            _wingR = CreateChild("WingRight", 3);
            _wingRSR = _wingR.AddComponent<SpriteRenderer>();

            // Feet
            _footL = CreateChild("FootLeft", 4);
            _footLSR = _footL.AddComponent<SpriteRenderer>();
            _footL.transform.localScale = Vector3.one * (_scaleSize * 0.35f);

            _footR = CreateChild("FootRight", 4);
            _footRSR = _footR.AddComponent<SpriteRenderer>();
            _footR.transform.localScale = Vector3.one * (_scaleSize * 0.35f);

            // Body
            _bodyGO = CreateChild("Body", 10);
            _bodySR = _bodyGO.AddComponent<SpriteRenderer>();
            _bodyGO.transform.localScale = Vector3.one * _scaleSize;

            // Face Layer - Eyes GameObject (child of body)
            _eyesGO = new GameObject("Eyes");
            _eyesGO.transform.SetParent(_bodyGO.transform, false);
            _eyesSR = _eyesGO.AddComponent<SpriteRenderer>();
            _eyesSR.sortingOrder = 11; // draw above body

            // Halo (above head)
            _haloGO = CreateChild("Halo", 11);
            _haloSR = _haloGO.AddComponent<SpriteRenderer>();

            // Hands
            _handL = CreateChild("HandLeft", 12);
            _handLSR = _handL.AddComponent<SpriteRenderer>();
            _handL.transform.localScale = Vector3.one * (_scaleSize * 0.22f);

            _handR = CreateChild("HandRight", 12);
            _handRSR = _handR.AddComponent<SpriteRenderer>();
            _handR.transform.localScale = Vector3.one * (_scaleSize * 0.22f);

            // Equipment
            _weaponGO = CreateChild("Weapon", 13);
            _weaponSR = _weaponGO.AddComponent<SpriteRenderer>();

            _shieldGO = CreateChild("Shield", 14);
            _shieldSR = _shieldGO.AddComponent<SpriteRenderer>();
        }

        private GameObject CreateChild(string name, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go;
        }

        private void Update()
        {
            // Sync status variables
            int currentLevel = 1;
            string branch = "fighter";
            bool isBoss = false;
            bool isElite = false;
            bool isMorphed = false;
            bool moving = false;
            Vector2 movement = Vector2.zero;

            if (_isPlayer)
            {
                currentLevel = playerCtrl.Level;
                branch = playerCtrl.characterData?.characterId?.ToLower() ?? "fighter";
                isMorphed = playerCtrl.characterData?.characterId?.ToLower() == "necromancer" && playerCtrl.Hp < playerCtrl.MaxHp * 0.3f;
                // Check if player is moving
                float h = Input.GetAxisRaw("Horizontal");
                float v = Input.GetAxisRaw("Vertical");
                movement = new Vector2(h, v);
                moving = movement.sqrMagnitude > 0.01f;
                _isDead = !playerCtrl.IsAlive;
            }
            else if (enemyCtrl != null)
            {
                isBoss = enemyCtrl.isBoss;
                isElite = enemyCtrl.isElite;
                moving = enemyCtrl.IsMoving;
                movement = enemyCtrl.Velocity;
                _isDead = !enemyCtrl.IsAlive;

                // Map enemy types to visual levels / branches
                string enName = enemyCtrl.EnemyName?.ToLower() ?? "";
                if (enName.Contains("slime")) { currentLevel = 1; branch = "slime"; }
                else if (enName.Contains("goblin")) { currentLevel = 2; branch = "goblin"; }
                else if (enName.Contains("skeleton")) { currentLevel = 3; branch = "skeleton"; }
                else if (enName.Contains("orc")) { currentLevel = 4; branch = "orc"; }
                else if (enName.Contains("demon")) { currentLevel = 5; branch = "demon"; }
                else if (enName.Contains("wraith")) { currentLevel = 6; branch = "wraith"; }
                else if (enName.Contains("golem")) { currentLevel = 7; branch = "golem"; }
                else if (enName.Contains("vampire")) { currentLevel = 8; branch = "vampire"; }
                else if (enName.Contains("witch")) { currentLevel = 9; branch = "witch"; }
                else if (enName.Contains("giant")) { currentLevel = 10; branch = "giant"; }
                else { currentLevel = 3; branch = "orc"; }
            }

            // Sync visual trigger if level/skin changed
            if (currentLevel != _lastLevel || branch != _lastBranch || isBoss != _lastIsBoss || isElite != _lastIsElite || isMorphed != _lastIsMorphed)
            {
                _lastLevel = currentLevel;
                _lastBranch = branch;
                _lastIsBoss = isBoss;
                _lastIsElite = isElite;
                _lastIsMorphed = isMorphed;
                UpdateVisualSkin();
            }

            // Detect hit via HP drops
            float curHp = _isPlayer ? (playerCtrl != null ? playerCtrl.Hp : 0f) : (enemyCtrl != null ? enemyCtrl.Hp : 0f);
            if (_lastHp < 0f) _lastHp = curHp;
            if (curHp < _lastHp && curHp > 0 && _lastHp > 0)
            {
                _isHit = true;
                _hitTimer = 0.15f;
            }
            _lastHp = curHp;

            if (_hitTimer > 0)
            {
                _hitTimer -= Time.deltaTime;
                if (_hitTimer <= 0) _isHit = false;
            }

            // Pupil tracking toward movement or nearest enemy
            Vector2 targetLook = Vector2.zero;
            if (_isPlayer && playerCtrl != null)
            {
                if (moving)
                {
                    targetLook = movement.normalized;
                }
                else
                {
                    // Find nearest enemy
                    float minD = 300f;
                    EnemyController nearest = null;
                    if (EnemySpawner.AllEnemies != null)
                    {
                        foreach (var en in EnemySpawner.AllEnemies)
                        {
                            if (en == null || !en.IsAlive) continue;
                            float d = Vector2.Distance(transform.position, en.transform.position);
                            if (d < minD) { minD = d; nearest = en; }
                        }
                    }
                    if (nearest != null)
                        targetLook = ((Vector2)(nearest.transform.position - transform.position)).normalized;
                }
            }
            else if (enemyCtrl != null && _playerTr != null)
            {
                targetLook = ((Vector2)(_playerTr.position - transform.position)).normalized;
            }

            _eyeLookDir = Vector2.Lerp(_eyeLookDir, targetLook, Time.deltaTime * 8f);
            if (_eyesGO != null)
            {
                // Account for flipping (local X scale determines direction)
                float flipX = transform.localScale.x < 0 ? -1f : 1f;
                _eyesGO.transform.localPosition = new Vector3(_eyeLookDir.x * 1.5f * flipX, _eyeLookDir.y * 1.0f, -0.01f);
            }

            // Hurt flash
            if (_isPlayer && playerCtrl.TakeDamage(0) > 0)
            {
                _hurtFlashTimer = 0.15f;
            }
            if (_hurtFlashTimer > 0)
            {
                _hurtFlashTimer -= Time.deltaTime;
                SetColorOverride(new Color(1f, 0.25f, 0.25f, 1f));
            }
            else
            {
                ResetColorOverride();
            }

            // Handle Blink Timer
            _blinkTimer -= Time.deltaTime;
            if (_blinkTimer <= 0f)
            {
                if (_isBlinking)
                {
                    _isBlinking = false;
                    _blinkTimer = Random.Range(3f, 6f);
                }
                else
                {
                    _isBlinking = true;
                    _blinkTimer = 0.12f;
                }
                UpdateVisualSkin(); // redraw eyes
            }

            // Handle Attack animations
            UpdateAttackState();

            // Limb Positioning & Floating Animations (Sin/Cos waves)
            AnimateLimbs(moving, movement);
        }

        private void UpdateAttackState()
        {
            bool wasAttacking = _isAttacking;
            _isAttacking = false;
            _attackProgress = 0f;

            if (_isPlayer)
            {
                var skills = playerCtrl.skillSystem?.Skills;
                if (skills != null)
                {
                    foreach (var s in skills)
                    {
                        if (s.data == null) continue;
                        float cd = s.GetEffectiveCd(playerCtrl.CdMult);
                        float elapsed = Time.time - s.lastFired;
                        float animDur = Mathf.Min(0.38f, cd * 0.45f);
                        if (elapsed < animDur)
                        {
                            _isAttacking = true;
                            _attackProgress = elapsed / animDur;
                            break;
                        }
                    }
                }
            }

            if (_isAttacking != wasAttacking)
            {
                UpdateVisualSkin(); // redraw mouth state
            }
        }

        private void SetColorOverride(Color c)
        {
            if (_bodySR) _bodySR.color = c;
            if (_handLSR) _handLSR.color = c;
            if (_handRSR) _handRSR.color = c;
            if (_footLSR) _footLSR.color = c;
            if (_footRSR) _footRSR.color = c;
            if (_weaponSR) _weaponSR.color = c;
            if (_shieldSR) _shieldSR.color = c;
        }

        private void ResetColorOverride()
        {
            if (_bodySR) _bodySR.color = Color.white;
            if (_handLSR) _handLSR.color = Color.white;
            if (_handRSR) _handRSR.color = Color.white;
            if (_footLSR) _footLSR.color = Color.white;
            if (_footRSR) _footRSR.color = Color.white;
            if (_weaponSR) _weaponSR.color = Color.white;
            if (_shieldSR) _shieldSR.color = Color.white;
        }

        private void UpdateVisualSkin()
        {
            // Retrieve skin matching level & branch
            var skin = GetSkin(_lastLevel, _lastBranch, _lastIsMorphed);
            Color bodyColor = ParseHex(skin.body1);
            Color outlineColor = ParseHex(skin.outline);

            // Re-generate procedurally body sprite (Removed blink/attack from body cache to avoid cache overflow)
            _bodySR.sprite = SpriteFactory.GetOrCreate($"body_{_lastBranch}_{_lastLevel}_{_lastIsMorphed}", () => GenerateBodyTexture(skin));

            // Update procedural Eyes sprite based on expressiveness states
            string eyeStateKey = "idle";
            if (_isDead) eyeStateKey = "dead";
            else if (_isHit) eyeStateKey = "hit";
            else if (_isBlinking) eyeStateKey = "blink";
            else if (_isAttacking) eyeStateKey = "angry";

            if (_eyesSR != null)
            {
                _eyesSR.sprite = SpriteFactory.GetOrCreate($"eyes_{eyeStateKey}_{skin.eye}_{skin.outline}", () => GenerateEyesSprite(eyeStateKey, skin));
            }

            // Generate customized hand & foot sprites (Gauntlets/Tabi shoes/sandals)
            string classId = _isPlayer ? _lastBranch : "enemy";
            _handLSR.sprite = SpriteFactory.GetOrCreate($"hand_l_{classId}_{skin.body1}", () => GenerateHandTexture(classId, bodyColor, outlineColor));
            _handRSR.sprite = SpriteFactory.GetOrCreate($"hand_r_{classId}_{skin.body1}", () => GenerateHandTexture(classId, bodyColor, outlineColor));
            _footLSR.sprite = SpriteFactory.GetOrCreate($"foot_l_{classId}_{skin.body1}", () => GenerateFootTexture(classId, bodyColor, outlineColor));
            _footRSR.sprite = SpriteFactory.GetOrCreate($"foot_r_{classId}_{skin.body1}", () => GenerateFootTexture(classId, bodyColor, outlineColor));

            // Generate aura ring
            if (_lastLevel >= 2 && !string.IsNullOrEmpty(skin.aura))
            {
                _auraGO.SetActive(true);
                _auraSR.color = ParseHex(skin.aura);
                _auraSR.sprite = SpriteFactory.GetOrCreate($"aura_{skin.aura}", () => GenerateAuraSprite(ParseHex(skin.aura)));
                _auraGO.transform.localScale = Vector3.one * (_scaleSize * 1.8f);
                _auraGO.transform.localPosition = new Vector3(0f, -_scaleSize * 0.4f, 0.04f);
            }
            else
            {
                _auraGO.SetActive(false);
            }

            // Generate wings
            bool hasWings = skin.skinType == "demon" || skin.skinType == "dragon" || _lastLevel == 5 || _lastLevel == 7 || _lastBranch == "necromancer" && _lastLevel >= 8;
            if (hasWings)
            {
                _wingL.SetActive(true);
                _wingR.SetActive(true);
                Color wingCol = skin.skinType == "dragon" ? new Color(0.6f, 0.08f, 0.08f) : new Color(0.1f, 0.05f, 0.15f);
                _wingLSR.sprite = SpriteFactory.GetOrCreate($"wing_l_{skin.skinType}", () => GenerateWingSprite(wingCol, true));
                _wingRSR.sprite = SpriteFactory.GetOrCreate($"wing_r_{skin.skinType}", () => GenerateWingSprite(wingCol, false));
                _wingL.transform.localScale = Vector3.one * (_scaleSize * 0.8f);
                _wingR.transform.localScale = Vector3.one * (_scaleSize * 0.8f);
            }
            else
            {
                _wingL.SetActive(false);
                _wingR.SetActive(false);
            }

            // Generate Halo
            if (skin.skinType == "celestial" || _lastLevel == 9)
            {
                _haloGO.SetActive(true);
                _haloSR.sprite = SpriteFactory.GetOrCreate("halo_sprite", () => GenerateHaloSprite());
                _haloGO.transform.localScale = Vector3.one * (_scaleSize * 0.7f);
                _haloGO.transform.localPosition = new Vector3(0f, _scaleSize * 0.65f, -0.05f);
            }
            else
            {
                _haloGO.SetActive(false);
            }

            // Generate Weapons & Shields (if player)
            if (_isPlayer)
            {
                _weaponGO.SetActive(true);
                _weaponSR.sprite = SpriteFactory.GetOrCreate($"wp_{_lastBranch}", () => GenerateWeaponSprite(_lastBranch));
                _weaponGO.transform.localScale = Vector3.one * (_scaleSize * 1.1f);

                if (_lastBranch == "fighter" || _lastBranch == "paladin")
                {
                    _shieldGO.SetActive(true);
                    _shieldSR.sprite = SpriteFactory.GetOrCreate($"shield_{_lastBranch}", () => GenerateShieldSprite(_lastBranch));
                    _shieldGO.transform.localScale = Vector3.one * (_scaleSize * 0.8f);
                }
                else
                {
                    _shieldGO.SetActive(false);
                }
            }
            else
            {
                _weaponGO.SetActive(false);
                _shieldGO.SetActive(false);
            }
        }

        private void AnimateLimbs(bool moving, Vector2 dir)
        {
            _breatheTimer += Time.deltaTime * breatheSpeed;
            float breathe = Mathf.Sin(_breatheTimer) * breatheScale;

            // Flip character based on facing direction
            float faceScaleX = 1f;
            if (_isPlayer)
            {
                float h = Input.GetAxisRaw("Horizontal");
                if (h < 0) faceScaleX = -1f;
                else if (h > 0) faceScaleX = 1f;
                else if (transform.localScale.x < 0) faceScaleX = -1f; // keep last flip
            }
            else if (dir.x < 0)
            {
                faceScaleX = -1f;
            }

            transform.localScale = new Vector3(faceScaleX, 1f, 1f);

            // Waddle bounce when moving
            float waddleRotation = 0f;
            float waddleOffset = 0f;
            if (moving)
            {
                _walkTimer += Time.deltaTime * walkSpeed;
                waddleOffset = Mathf.Abs(Mathf.Sin(_walkTimer)) * bobAmount;
                waddleRotation = Mathf.Sin(_walkTimer) * tiltAmount;
            }
            else
            {
                _walkTimer = 0f;
            }

            // Apply breathe scaling & walking bob to body
            _bodyGO.transform.localScale = new Vector3(1f + breathe, 1f - breathe, 1f) * _scaleSize;
            _bodyGO.transform.localPosition = new Vector3(0f, waddleOffset, 0f);
            _bodyGO.transform.localRotation = Quaternion.Euler(0f, 0f, waddleRotation);

            // Animate Hands
            Vector3 defaultHandL = new Vector3(-_scaleSize * 0.42f, -_scaleSize * 0.1f, -0.05f);
            Vector3 defaultHandR = new Vector3(_scaleSize * 0.42f, -_scaleSize * 0.1f, -0.05f);

            if (moving)
            {
                float swing = Mathf.Sin(_walkTimer) * handSwing;
                _handL.transform.localPosition = defaultHandL + new Vector3(0f, swing, 0f);
                _handR.transform.localPosition = defaultHandR + new Vector3(0f, -swing, 0f);
            }
            else
            {
                float handFloat = Mathf.Sin(_breatheTimer) * 1.5f;
                _handL.transform.localPosition = defaultHandL + new Vector3(0f, handFloat, 0f);
                _handR.transform.localPosition = defaultHandR + new Vector3(0f, -handFloat, 0f);
            }

            // Animate Feet
            Vector3 defaultFootL = new Vector3(-_scaleSize * 0.25f, -_scaleSize * 0.42f, 0.02f);
            Vector3 defaultFootR = new Vector3(_scaleSize * 0.25f, -_scaleSize * 0.42f, 0.02f);

            if (moving)
            {
                float swingL = Mathf.Sin(_walkTimer) * footSwing;
                float liftL = Mathf.Max(0f, Mathf.Cos(_walkTimer)) * footLift;
                _footL.transform.localPosition = defaultFootL + new Vector3(swingL, liftL, 0f);

                float swingR = -Mathf.Sin(_walkTimer) * footSwing;
                float liftR = Mathf.Max(0f, -Mathf.Cos(_walkTimer)) * footLift;
                _footR.transform.localPosition = defaultFootR + new Vector3(swingR, liftR, 0f);
            }
            else
            {
                _footL.transform.localPosition = defaultFootL;
                _footR.transform.localPosition = defaultFootR;
            }

            // Animate Wings
            if (_wingL.activeSelf)
            {
                float flap = Mathf.Sin(Time.time * 6f) * 12f;
                _wingL.transform.localPosition = new Vector3(-_scaleSize * 0.35f, -_scaleSize * 0.08f, 0.1f);
                _wingR.transform.localPosition = new Vector3(_scaleSize * 0.35f, -_scaleSize * 0.08f, 0.1f);

                _wingL.transform.localRotation = Quaternion.Euler(0f, 0f, -22f + flap);
                _wingR.transform.localRotation = Quaternion.Euler(0f, 0f, 22f - flap);
            }

            // Animate Weapon / Attack Swing
            if (_isPlayer)
            {
                AnimateWeapons();
            }

            // Animate Aura
            if (_auraGO.activeSelf)
            {
                _auraGO.transform.Rotate(0f, 0f, Time.deltaTime * 35f);
            }
        }

        private void AnimateWeapons()
        {
            Vector3 defaultWeaponPos = new Vector3(_scaleSize * 0.45f, -_scaleSize * 0.08f, -0.1f);
            float defaultWeaponRot = -15f;

            if (_lastBranch == "assassin")
            {
                _weaponGO.transform.localPosition = defaultWeaponPos;
                _weaponGO.transform.Rotate(0f, 0f, Time.deltaTime * (_isAttacking ? 850f : 250f));
            }
            else if (_isAttacking)
            {
                if (_lastBranch == "fighter" || _lastBranch == "paladin")
                {
                    float swing = Mathf.Lerp(50f, -100f, _attackProgress);
                    _weaponGO.transform.localPosition = defaultWeaponPos + new Vector3(Mathf.Sin(_attackProgress * Mathf.PI) * _scaleSize * 0.25f, 0f, 0f);
                    _weaponGO.transform.localRotation = Quaternion.Euler(0f, 0f, swing);

                    if (_shieldGO.activeSelf)
                    {
                        _shieldGO.transform.localPosition = new Vector3(-_scaleSize * 0.42f - Mathf.Sin(_attackProgress * Mathf.PI) * _scaleSize * 0.18f, 0f, -0.08f);
                    }
                }
                else if (_lastBranch == "mage" || _lastBranch == "necromancer" || _lastBranch == "druid")
                {
                    float thrust = Mathf.Sin(_attackProgress * Mathf.PI) * _scaleSize * 0.3f;
                    _weaponGO.transform.localPosition = defaultWeaponPos + new Vector3(thrust, 0f, 0f);
                    _weaponGO.transform.localRotation = Quaternion.Euler(0f, 0f, defaultWeaponRot + Mathf.Sin(_attackProgress * Mathf.PI) * 20f);
                }
                else if (_lastBranch == "ranger")
                {
                    float pull = Mathf.Sin(_attackProgress * Mathf.PI) * _scaleSize * 0.18f;
                    _weaponGO.transform.localPosition = defaultWeaponPos - new Vector3(pull, 0f, 0f);
                    _weaponGO.transform.localRotation = Quaternion.Euler(0f, 0f, defaultWeaponRot - pull * 2.2f);
                }
            }
            else
            {
                float floatY = Mathf.Sin(_breatheTimer) * 1.5f;
                _weaponGO.transform.localPosition = defaultWeaponPos + new Vector3(0f, floatY, 0f);
                _weaponGO.transform.localRotation = Quaternion.Euler(0f, 0f, defaultWeaponRot);

                if (_shieldGO.activeSelf)
                {
                    _shieldGO.transform.localPosition = new Vector3(-_scaleSize * 0.45f, -_scaleSize * 0.05f, -0.08f);
                    _shieldGO.transform.localRotation = Quaternion.identity;
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  PROCEDURAL SPRITE RENDERER ENGINE (Texture2D Drawing Helpers)
        // ══════════════════════════════════════════════════════════════════
        private Sprite GenerateBodyTexture(SkinDef skin)
        {
            int sz = 128;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            float c = sz / 2f;

            Color cHL = ParseHex(skin.bodyHL);
            Color c1  = ParseHex(skin.body1);
            Color c2  = ParseHex(skin.body2);
            Color cOut = ParseHex(skin.outline);
            Color cEye = ParseHex(skin.eye);

            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++) tex.SetPixel(x, y, Color.clear);

            float r = sz * 0.38f;

            if (_isPlayer)
            {
                // Upgraded 2.5D: Separate Head & Torso structure
                float hx = c;
                float hy = c + r * 0.15f;
                float hr = r * 0.65f;

                // 1. Draw Torso / Clothes (lower layer)
                DrawClassGarment(tex, c, c - r * 0.45f, r, _lastBranch, cOut);

                // 2. Draw Head base (skin/elemental tone)
                DrawGradientCircle(tex, hx, hy, hr, cHL, c1, c2, cOut, 2.5f);

                // 3. Draw Hair / Cowl / Helmets
                DrawClassHeadwear(tex, hx, hy, hr, _lastBranch, c1, cOut);

                // 4. Custom headwear accessories
                if (skin.skinType == "leaf") DrawLeaf(tex, hx, hy - hr * 0.95f, hr * 0.2f);
                else if (skin.skinType == "horns") DrawHorns(tex, hx, hy - hr * 0.85f, hr);
                else if (skin.skinType == "neko") DrawNekoEars(tex, hx, hy - hr * 0.85f, hr, c1, cOut);
                else if (skin.skinType == "panda") DrawPandaEars(tex, hx, hy - hr * 0.85f, hr, cOut);
                else if (skin.skinType == "demon") DrawDemonHorns(tex, hx, hy - hr * 0.9f, hr);
                else if (skin.skinType == "boss") DrawCrown(tex, hx, hy - hr * 0.95f, hr);
                else if (skin.skinType == "dragon") DrawDragonHorns(tex, hx, hy - hr * 0.9f, hr);
                else if (skin.skinType == "shadow") DrawShadowMask(tex, hx, hy, hr);
                else if (skin.skinType == "celestial") DrawStar(tex, hx, hy - hr * 0.3f, 4f, Color.white);
                else if (skin.skinType == "omega") DrawOmegaSymbol(tex, hx, hy - hr * 0.3f, 5f, Color.yellow);

                // 5. Eyes & Expression (Blinking / Attack angry) - Handled dynamically by custom Face Layer _eyesSR
                // DrawEyes(tex, hx, hy, hr, cEye, cOut, _isAttacking, _isBlinking);

                // 6. Blushing cheeks & Cute Mouth
                DrawCheeks(tex, hx, hy, hr);
                DrawMouth(tex, hx, hy, hr, _isAttacking);
            }
            else
            {
                // Enemy Procedural Rendering
                GenerateEnemyTexture(tex, _lastBranch, c, r, c1, c2, cOut);
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, sz);
        }

        private Sprite GenerateHandTexture(string branch, Color bodyCol, Color outline)
        {
            int sz = 32;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            float c = sz / 2f;
            float r = sz * 0.35f;

            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++) tex.SetPixel(x, y, Color.clear);

            if (branch == "fighter" || branch == "paladin")
            {
                Color metalCol = (branch == "paladin") ? new Color(0.99f, 0.88f, 0.2f) : Color.gray;
                for (int y = (int)(c - r); y <= (int)(c + r); y++)
                for (int x = (int)(c - r * 0.7f); x <= (int)(c + r * 0.7f); x++)
                {
                    float dx = x - c, dy = y - c;
                    bool border = Mathf.Abs(dx) > r * 0.58f || Mathf.Abs(dy) > r * 0.85f;
                    tex.SetPixel(x, y, border ? outline : Color.Lerp(metalCol, Color.white, (x - c + r) / (r * 2f)));
                }
            }
            else if (branch == "assassin")
            {
                for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                    if (d < r)
                    {
                        tex.SetPixel(x, y, d > r - 2f ? outline : new Color(0.12f, 0.08f, 0.2f));
                    }
                }
                DrawTriangle(tex, new Vector2(c - 4, c + r), new Vector2(c - 2, c + r + 5), new Vector2(c, c + r), Color.red);
                DrawTriangle(tex, new Vector2(c, c + r), new Vector2(c + 2, c + r + 5), new Vector2(c + 4, c + r), Color.red);
            }
            else if (branch == "mage" || branch == "necromancer")
            {
                Color glowCol = (branch == "mage") ? new Color(0.22f, 0.56f, 0.97f) : new Color(0.35f, 0.2f, 0.55f);
                for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                    if (d < r)
                    {
                        float factor = d / r;
                        tex.SetPixel(x, y, Color.Lerp(Color.white, glowCol, factor));
                    }
                }
            }
            else
            {
                Color cuffCol = (branch == "ranger") ? new Color(0.2f, 0.78f, 0.35f) : new Color(0.25f, 0.65f, 0.2f);
                for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                    if (d < r)
                    {
                        if (y < c)
                            tex.SetPixel(x, y, d > r - 2f ? outline : cuffCol);
                        else
                            tex.SetPixel(x, y, d > r - 2f ? outline : bodyCol);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, sz);
        }

        private Sprite GenerateFootTexture(string branch, Color bodyCol, Color outline)
        {
            int sz = 32;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            float c = sz / 2f;
            float r = sz * 0.38f;

            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++) tex.SetPixel(x, y, Color.clear);

            if (branch == "fighter" || branch == "paladin")
            {
                Color metalCol = (branch == "paladin") ? new Color(0.99f, 0.88f, 0.2f) : Color.gray;
                DrawCheekOval(tex, c, c, r * 1.1f, r * 0.7f, outline);
                DrawCheekOval(tex, c, c, r * 1.0f, r * 0.6f, metalCol);
                DrawCheekOval(tex, c + 3f, c, r * 0.5f, r * 0.5f, Color.white);
            }
            else if (branch == "assassin")
            {
                DrawCheekOval(tex, c, c, r * 1.1f, r * 0.6f, outline);
                DrawCheekOval(tex, c, c, r * 1.0f, r * 0.5f, new Color(0.12f, 0.1f, 0.2f));
                DrawLine(tex, c + 2f, c, c + 7f, c, outline);
            }
            else
            {
                Color bootCol = new Color(0.35f, 0.22f, 0.12f);
                DrawCheekOval(tex, c, c, r * 1.1f, r * 0.6f, outline);
                DrawCheekOval(tex, c, c, r * 1.0f, r * 0.5f, bootCol);
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, sz);
        }

        private Sprite GenerateAuraSprite(Color c)
        {
            int sz = 128;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            float ctr = sz / 2f;
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), Vector2.one * ctr);
                if (d > ctr * 0.9f || d < ctr * 0.72f)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else
                {
                    float factor = 1f - Mathf.Abs(d - ctr * 0.81f) / (ctr * 0.09f);
                    tex.SetPixel(x, y, new Color(c.r, c.g, c.b, factor * 0.6f));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, sz);
        }

        private Sprite GenerateWingSprite(Color c, bool left)
        {
            int sz = 64;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float nx = left ? (sz - x) / (float)sz : x / (float)sz;
                float ny = y / (float)sz;
                bool wing = ny > 0.1f && ny < 0.8f && nx > 0.1f && nx < 0.85f - (ny - 0.4f) * (ny - 0.4f) * 0.6f;
                if (wing)
                {
                    tex.SetPixel(x, y, Color.Lerp(c, new Color(0.05f, 0.03f, 0.1f), nx));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, sz);
        }

        private Sprite GenerateHaloSprite()
        {
            int sz = 64;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            float c = sz / 2f;
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float dx = (x - c) / (sz * 0.4f);
                float dy = (y - c) / (sz * 0.15f);
                float dist = dx * dx + dy * dy;
                if (dist > 0.8f && dist < 1.25f)
                {
                    tex.SetPixel(x, y, new Color(1f, 0.92f, 0.4f, 0.8f));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, sz);
        }

        private Sprite GenerateWeaponSprite(string branch)
        {
            int sz = 64;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++) tex.SetPixel(x, y, Color.clear);

            float c = sz / 2f;

            if (branch == "assassin")
            {
                // Shuriken: 4-pointed glowing star
                for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    float dx = x - c, dy = y - c;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);
                    float r = c * 0.8f * (0.3f + 0.7f * Mathf.Abs(Mathf.Cos(4f * angle)));
                    if (dist < r)
                    {
                        tex.SetPixel(x, y, Color.Lerp(Color.white, new Color(0.5f, 0.1f, 0.7f), dist / c));
                    }
                }
            }
            else if (branch == "fighter")
            {
                // Detailed Runic Sword
                int cx = sz / 2;
                for (int y = 6; y < sz - 6; y++)
                {
                    for (int x = cx - 4; x <= cx + 4; x++)
                    {
                        if (y > sz - 15) // Hilt (Brown)
                            tex.SetPixel(x, y, new Color(0.4f, 0.2f, 0.1f));
                        else if (y > sz - 19) // Guard (Gold)
                            tex.SetPixel(x, y, new Color(0.99f, 0.88f, 0.2f));
                        else // Blade (Steel grey + center glowing red rune line)
                        {
                            Color col = Color.Lerp(Color.white, Color.gray, Mathf.Abs(x - cx) / 4f);
                            if (x == cx && y > 22 && y < sz - 22) col = new Color(1f, 0.3f, 0.3f); // rune glow
                            tex.SetPixel(x, y, col);
                        }
                    }
                }
            }
            else if (branch == "ranger")
            {
                // Detailed Bow: green structure with arrow
                for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    float dx = x - c * 0.4f, dy = y - c;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > c * 0.75f && dist < c * 0.9f && x > c * 0.4f)
                    {
                        tex.SetPixel(x, y, new Color(0.1f, 0.6f, 0.2f)); // Forest green bow wood
                    }
                    else if (x == (int)(sz * 0.35f) && y > c * 0.15f && y < sz - c * 0.15f)
                    {
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.5f)); // silver string
                    }
                }
                // Draw arrow notched
                for (int x = (int)(c * 0.2f); x < (int)(sz * 0.8f); x++)
                {
                    tex.SetPixel(x, (int)c, new Color(0.47f, 0.25f, 0.15f));
                }
                tex.SetPixel((int)(sz * 0.8f), (int)c, Color.red); // tip
            }
            else if (branch == "paladin")
            {
                // Gold Trimmed Warhammer
                int cx = sz / 2;
                for (int y = 4; y < sz - 4; y++)
                {
                    for (int x = cx - 10; x <= cx + 10; x++)
                    {
                        bool shaft = y > 22 && x >= cx - 2 && x <= cx + 2;
                        bool head = y <= 22 && y >= 6;
                        if (head)
                        {
                            bool goldTrim = x <= cx - 8 || x >= cx + 8 || y <= 10 || y >= 18;
                            tex.SetPixel(x, y, goldTrim ? new Color(0.99f, 0.88f, 0.2f) : Color.gray);
                        }
                        else if (shaft)
                        {
                            tex.SetPixel(x, y, new Color(0.47f, 0.25f, 0.15f));
                        }
                    }
                }
            }
            else if (branch == "mage")
            {
                // Staff with large sapphire gem
                int cx = sz / 2;
                for (int y = 4; y < sz - 4; y++)
                {
                    for (int x = cx - 5; x <= cx + 5; x++)
                    {
                        bool crystal = y < 15 && x >= cx - 4 && x <= cx + 4;
                        bool shaft = y >= 15 && x >= cx - 1 && x <= cx + 1;
                        if (crystal)
                        {
                            float factor = Vector2.Distance(new Vector2(x, y), new Vector2(cx, 8f)) / 5f;
                            tex.SetPixel(x, y, Color.Lerp(Color.white, new Color(0.1f, 0.85f, 1f), factor));
                        }
                        else if (shaft)
                        {
                            tex.SetPixel(x, y, new Color(0.5f, 0.3f, 0.15f));
                        }
                    }
                }
            }
            else if (branch == "necromancer")
            {
                // Staff topped with detailed skull
                int cx = sz / 2;
                for (int y = 4; y < sz - 4; y++)
                {
                    for (int x = cx - 6; x <= cx + 6; x++)
                    {
                        bool skull = y < 20 && x >= cx - 5 && x <= cx + 5;
                        bool shaft = y >= 20 && x >= cx - 1 && x <= cx + 1;
                        if (skull)
                        {
                            // draw eyes in skull
                            bool isEye = (y == 12 || y == 13) && (x == cx - 2 || x == cx + 2);
                            tex.SetPixel(x, y, isEye ? new Color(0.2f, 0.9f, 0.3f) : new Color(0.92f, 0.92f, 0.88f));
                        }
                        else if (shaft)
                        {
                            tex.SetPixel(x, y, Color.gray);
                        }
                    }
                }
            }
            else // druid
            {
                // Nature branch staff with emerald
                int cx = sz / 2;
                for (int y = 4; y < sz - 4; y++)
                {
                    for (int x = cx - 5; x <= cx + 5; x++)
                    {
                        bool gem = y < 16 && x >= cx - 4 && x <= cx + 4;
                        bool shaft = y >= 16 && x >= cx - 2 && x <= cx + 2;
                        if (gem)
                        {
                            tex.SetPixel(x, y, new Color(0.12f, 0.85f, 0.22f));
                        }
                        else if (shaft)
                        {
                            tex.SetPixel(x, y, new Color(0.4f, 0.25f, 0.1f));
                        }
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, sz);
        }

        private Sprite GenerateShieldSprite(string branch)
        {
            int sz = 48;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            float c = sz / 2f;
            Color gold = new Color(0.99f, 0.88f, 0.2f);
            Color iron = Color.gray;

            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float dx = x - c, dy = y - c;
                bool outer = dx * dx + dy * dy < c * c * 0.9f;
                bool inner = dx * dx + dy * dy < c * c * 0.5f;
                if (inner)
                {
                    tex.SetPixel(x, y, branch == "paladin" ? gold : Color.white);
                }
                else if (outer)
                {
                    tex.SetPixel(x, y, branch == "paladin" ? iron : gold);
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, sz);
        }

        // ── Drawing Helpers for Head & Body ──────────────────────────────
        private void DrawClassHeadwear(Texture2D tex, float hx, float hy, float hr, string branch, Color baseCol, Color outline)
        {
            if (branch == "fighter")
            {
                // Spiky Orange Hair
                Color hairCol = new Color(0.95f, 0.45f, 0.2f);
                for (int i = -3; i <= 3; i++)
                {
                    float angle = i * 22f * Mathf.Deg2Rad;
                    Vector2 p0 = new Vector2(hx + Mathf.Sin(angle) * hr * 0.8f, hy + Mathf.Cos(angle) * hr * 0.8f);
                    Vector2 p1 = new Vector2(hx + Mathf.Sin(angle + 0.15f) * hr * 1.3f, hy + Mathf.Cos(angle + 0.15f) * hr * 1.3f);
                    Vector2 p2 = new Vector2(hx + Mathf.Sin(angle + 0.3f) * hr * 0.8f, hy + Mathf.Cos(angle + 0.3f) * hr * 0.8f);
                    DrawTriangle(tex, p0, p1, p2, hairCol);
                }
                // Red headband
                DrawRect(tex, hx - hr, hy + hr * 0.1f, hr * 2f, hr * 0.22f, Color.red);
            }
            else if (branch == "mage")
            {
                // Wizard Hat (Pointy Blue Triangle)
                Color hatCol = new Color(0.15f, 0.2f, 0.6f);
                Vector2 p0 = new Vector2(hx - hr * 1.1f, hy + hr * 0.2f);
                Vector2 p1 = new Vector2(hx, hy + hr * 2.2f);
                Vector2 p2 = new Vector2(hx + hr * 1.1f, hy + hr * 0.2f);
                DrawTriangle(tex, p0, p1, p2, hatCol);
                // Hat gold brim & buckle
                DrawRect(tex, hx - hr * 0.8f, hy + hr * 0.2f, hr * 1.6f, hr * 0.15f, Color.yellow);
            }
            else if (branch == "assassin")
            {
                // Ninja Cowl: dark purple overlay on head except slit
                for (int y = (int)(hy - hr); y <= (int)(hy + hr); y++)
                for (int x = (int)(hx - hr); x <= (int)(hx + hr); x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(hx, hy));
                    if (d < hr - 2f)
                    {
                        // slit for eyes
                        bool isSlit = y >= hy - hr * 0.1f && y <= hy + hr * 0.3f && x >= hx - hr * 0.6f && x <= hx + hr * 0.6f;
                        if (!isSlit)
                        {
                            tex.SetPixel(x, y, new Color(0.12f, 0.08f, 0.22f));
                        }
                    }
                }
            }
            else if (branch == "ranger")
            {
                // Green Hood surrounding head
                for (int y = (int)(hy - hr); y <= (int)(hy + hr * 1.2f); y++)
                for (int x = (int)(hx - hr * 1.2f); x <= (int)(hx + hr * 1.2f); x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(hx, hy));
                    if (d > hr - 2.5f && d < hr * 1.25f && y > hy - hr * 0.5f)
                    {
                        tex.SetPixel(x, y, new Color(0.1f, 0.5f, 0.2f));
                    }
                }
                // Blonde bangs
                DrawCheekOval(tex, hx, hy + hr * 0.6f, hr * 0.5f, hr * 0.2f, new Color(0.99f, 0.88f, 0.2f));
            }
            else if (branch == "paladin")
            {
                // Golden helmet dome & visor
                Color gold = new Color(0.99f, 0.88f, 0.2f);
                for (int y = (int)(hy); y <= (int)(hy + hr * 1.1f); y++)
                for (int x = (int)(hx - hr * 1.05f); x <= (int)(hx + hr * 1.05f); x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(hx, hy));
                    if (d < hr)
                    {
                        tex.SetPixel(x, y, gold);
                    }
                }
                // visor bar
                DrawRect(tex, hx - hr * 0.6f, hy + hr * 0.1f, hr * 1.2f, hr * 0.25f, Color.gray);
                // red plume
                DrawTriangle(tex, new Vector2(hx - 3, hy + hr), new Vector2(hx, hy + hr * 1.7f), new Vector2(hx + 3, hy + hr), Color.red);
            }
            else if (branch == "necromancer")
            {
                // Skull Cowl & pale hair
                for (int y = (int)(hy - hr); y <= (int)(hy + hr * 1.1f); y++)
                for (int x = (int)(hx - hr * 1.1f); x <= (int)(hx + hr * 1.1f); x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(hx, hy));
                    if (d > hr - 3f && d < hr * 1.15f && y > hy)
                    {
                        tex.SetPixel(x, y, Color.black);
                    }
                }
                // white bangs
                DrawCheekOval(tex, hx, hy + hr * 0.55f, hr * 0.5f, hr * 0.18f, Color.white);
            }
            else if (branch == "druid")
            {
                // Deer Antlers
                Color antlerCol = new Color(0.48f, 0.35f, 0.2f);
                DrawLine(tex, hx - hr * 0.6f, hy + hr * 0.7f, hx - hr * 1.1f, hy + hr * 1.3f, antlerCol);
                DrawLine(tex, hx - hr * 1.1f, hy + hr * 1.3f, hx - hr * 1.3f, hy + hr * 1.1f, antlerCol);
                DrawLine(tex, hx + hr * 0.6f, hy + hr * 0.7f, hx + hr * 1.1f, hy + hr * 1.3f, antlerCol);
                DrawLine(tex, hx + hr * 1.1f, hy + hr * 1.3f, hx + hr * 1.3f, hy + hr * 1.1f, antlerCol);

                // Leaf green hair
                Color leafCol = new Color(0.12f, 0.65f, 0.22f);
                DrawCheekOval(tex, hx - hr * 0.7f, hy + hr * 0.3f, hr * 0.3f, hr * 0.6f, leafCol);
                DrawCheekOval(tex, hx + hr * 0.7f, hy + hr * 0.3f, hr * 0.3f, hr * 0.6f, leafCol);
            }
        }

        private void DrawClassGarment(Texture2D tex, float cx, float cy, float r, string branch, Color outline)
        {
            Color clothCol = Color.clear;
            if (branch == "fighter") clothCol = new Color(0.4f, 0.45f, 0.55f);
            else if (branch == "mage") clothCol = new Color(0.3f, 0.15f, 0.6f);
            else if (branch == "assassin") clothCol = new Color(0.2f, 0.08f, 0.3f);
            else if (branch == "ranger") clothCol = new Color(0.1f, 0.5f, 0.2f);
            else if (branch == "paladin") clothCol = new Color(0.85f, 0.7f, 0.2f);
            else if (branch == "necromancer") clothCol = new Color(0.1f, 0.1f, 0.15f);
            else clothCol = new Color(0.25f, 0.45f, 0.2f); // druid

            // Render torso oval
            DrawCheekOval(tex, cx, cy, r * 0.78f, r * 0.5f, outline);
            DrawCheekOval(tex, cx, cy, r * 0.72f, r * 0.42f, clothCol);

            // Garment details
            if (branch == "fighter" || branch == "paladin")
            {
                // metal trim strap
                DrawRect(tex, cx - 2, cy - r * 0.4f, 4, r * 0.8f, Color.yellow);
            }
            else if (branch == "ranger")
            {
                // diagonal quiver strap
                DrawLine(tex, cx - r * 0.5f, cy + r * 0.3f, cx + r * 0.5f, cy - r * 0.3f, new Color(0.38f, 0.2f, 0.1f));
            }
            else if (branch == "necromancer")
            {
                // Green ribs
                DrawLine(tex, cx - 6, cy + 4, cx - 2, cy + 4, Color.green);
                DrawLine(tex, cx + 2, cy + 4, cx + 6, cy + 4, Color.green);
                DrawLine(tex, cx - 8, cy - 2, cx - 2, cy - 2, Color.green);
                DrawLine(tex, cx + 2, cy - 2, cx + 8, cy - 2, Color.green);
            }
        }

        private void DrawGradientCircle(Texture2D tex, float cx, float cy, float r, Color cHL, Color c1, Color c2, Color cOut, float border)
        {
            for (int y = 0; y < tex.height; y++)
            for (int x = 0; x < tex.width; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                if (d > r) continue;
                if (d > r - border)
                {
                    tex.SetPixel(x, y, cOut);
                }
                else
                {
                    float factor = d / r;
                    float hD = Vector2.Distance(new Vector2(x, y), new Vector2(cx - r * 0.3f, cy + r * 0.35f));
                    float hFactor = Mathf.Clamp01(hD / (r * 1.4f));
                    
                    // Radial gradient sphere background
                    Color fill = Color.Lerp(cHL, Color.Lerp(c1, c2, factor * 0.8f), hFactor);
                    
                    // Add glossy specular toy-like highlight (white circle in top-left)
                    if (hD < r * 0.22f)
                    {
                        float specAlpha = 1f - (hD / (r * 0.22f));
                        fill = Color.Lerp(fill, Color.white, specAlpha * 0.85f);
                    }
                    
                    tex.SetPixel(x, y, fill);
                }
            }
        }

        private Sprite GenerateEyesSprite(string state, SkinDef skin)
        {
            int sz = 128;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            float c = sz / 2f;
            float r = sz * 0.38f;
            float hr = r * 0.65f;
            float hx = c;
            float hy = c + r * 0.15f;

            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++) tex.SetPixel(x, y, Color.clear);

            Color cEye = ParseHex(skin.eye);
            Color cOut = ParseHex(skin.outline);

            float eyeSpread = hr * 0.35f;
            float eyeY = hy + hr * 0.15f;
            float eyeR = hr * 0.16f;
            float pupR = hr * 0.08f;

            if (state == "blink")
            {
                DrawLine(tex, hx - eyeSpread - eyeR, eyeY, hx - eyeSpread + eyeR, eyeY, cOut);
                DrawLine(tex, hx + eyeSpread - eyeR, eyeY, hx + eyeSpread + eyeR, eyeY, cOut);
            }
            else if (state == "dead")
            {
                DrawXEye(tex, hx - eyeSpread, eyeY, eyeR, cOut);
                DrawXEye(tex, hx + eyeSpread, eyeY, eyeR, cOut);
            }
            else if (state == "hit")
            {
                DrawSquashedEye(tex, hx - eyeSpread, eyeY, eyeR, cOut, true);
                DrawSquashedEye(tex, hx + eyeSpread, eyeY, eyeR, cOut, false);
            }
            else
            {
                bool angry = (state == "angry");
                DrawSingleEye(tex, hx - eyeSpread, eyeY, eyeR, pupR, cEye, cOut, angry, true);
                DrawSingleEye(tex, hx + eyeSpread, eyeY, eyeR, pupR, cEye, cOut, angry, false);
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, sz);
        }

        private void DrawXEye(Texture2D tex, float ex, float ey, float er, Color col)
        {
            float len = er * 0.9f;
            DrawLine(tex, ex - len, ey - len, ex + len, ey + len, col);
            DrawLine(tex, ex - len, ey + len, ex + len, ey - len, col);
        }

        private void DrawSquashedEye(Texture2D tex, float ex, float ey, float er, Color col, bool left)
        {
            float len = er * 0.8f;
            if (left)
            {
                // Shape: >
                DrawLine(tex, ex - len, ey + len * 0.7f, ex + len * 0.3f, ey, col);
                DrawLine(tex, ex + len * 0.3f, ey, ex - len, ey - len * 0.7f, col);
            }
            else
            {
                // Shape: <
                DrawLine(tex, ex + len, ey + len * 0.7f, ex - len * 0.3f, ey, col);
                DrawLine(tex, ex - len * 0.3f, ey, ex + len, ey - len * 0.7f, col);
            }
        }

        private void DrawEyes(Texture2D tex, float cx, float cy, float r, Color eyeCol, Color outline, bool angry, bool blinking)
        {
            float eyeSpread = r * 0.35f;
            float eyeY = cy + r * 0.15f;
            float eyeR = r * 0.16f;
            float pupR = r * 0.08f;

            if (blinking)
            {
                // Draw blinking lines
                DrawLine(tex, cx - eyeSpread - eyeR, eyeY, cx - eyeSpread + eyeR, eyeY, outline);
                DrawLine(tex, cx + eyeSpread - eyeR, eyeY, cx + eyeSpread + eyeR, eyeY, outline);
            }
            else
            {
                DrawSingleEye(tex, cx - eyeSpread, eyeY, eyeR, pupR, eyeCol, outline, angry, true);
                DrawSingleEye(tex, cx + eyeSpread, eyeY, eyeR, pupR, eyeCol, outline, angry, false);
            }
        }

        private void DrawSingleEye(Texture2D tex, float ex, float ey, float er, float pr, Color eyeCol, Color outline, bool angry, bool left)
        {
            for (int y = 0; y < tex.height; y++)
            for (int x = 0; x < tex.width; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(ex, ey));
                if (d > er) continue;

                if (d > er - 1.5f)
                {
                    tex.SetPixel(x, y, outline);
                }
                else
                {
                    float pd = Vector2.Distance(new Vector2(x, y), new Vector2(ex + (left ? 1.5f : -1.5f), ey - 1f));
                    if (pd < pr)
                    {
                        tex.SetPixel(x, y, eyeCol);
                    }
                    else if (pd < pr + 1f)
                    {
                        tex.SetPixel(x, y, outline);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                }
            }

            if (angry)
            {
                float sx = ex - er * 1.2f, ex_pt = ex + er * 1.2f;
                float sy = ey + er * 0.8f + (left ? 1.5f : -1.5f);
                float ey_pt = ey + er * 0.8f + (left ? -1.5f : 1.5f);
                DrawLine(tex, sx, sy + 3f, ex_pt, ey_pt + 3f, outline);
            }
        }

        private void DrawCheeks(Texture2D tex, float cx, float cy, float r)
        {
            Color cheekCol = new Color(0.97f, 0.65f, 0.8f, 0.45f);
            float spread = r * 0.52f;
            float cheekY = cy - r * 0.2f;
            DrawCheekOval(tex, cx - spread, cheekY, r * 0.22f, r * 0.12f, cheekCol);
            DrawCheekOval(tex, cx + spread, cheekY, r * 0.22f, r * 0.12f, cheekCol);
        }

        private void DrawMouth(Texture2D tex, float cx, float cy, float r, bool angry)
        {
            float mouthY = cy - r * 0.15f;
            Color col = new Color(0.08f, 0.05f, 0.12f);
            if (angry)
            {
                // draw open mouth "o"
                DrawCheekOval(tex, cx, mouthY, 4f, 5f, col);
            }
            else
            {
                // draw smile line
                DrawLine(tex, cx - 3f, mouthY, cx, mouthY - 2f, col);
                DrawLine(tex, cx, mouthY - 2f, cx + 3f, mouthY, col);
            }
        }

        private void DrawCheekOval(Texture2D tex, float ex, float ey, float rx, float ry, Color c)
        {
            for (int y = (int)(ey - ry); y <= (int)(ey + ry); y++)
            for (int x = (int)(ex - rx); x <= (int)(ex + rx); x++)
            {
                if (x < 0 || y < 0 || x >= tex.width || y >= tex.height) continue;
                float dx = (x - ex) / rx, dy = (y - ey) / ry;
                if (dx * dx + dy * dy < 1f)
                {
                    Color orig = tex.GetPixel(x, y);
                    tex.SetPixel(x, y, Color.Lerp(orig, c, c.a));
                }
            }
        }

        // ── Custom head decor shapes (leaves, horns, crown) ────────
        private void DrawLeaf(Texture2D tex, float cx, float cy, float r)
        {
            DrawCheekOval(tex, cx - 4f, cy, r, r * 0.5f, new Color(0.1f, 0.75f, 0.3f));
            DrawCheekOval(tex, cx + 4f, cy, r, r * 0.5f, new Color(0.1f, 0.75f, 0.3f));
        }

        private void DrawHorns(Texture2D tex, float cx, float cy, float r)
        {
            DrawCheekOval(tex, cx - r * 0.55f, cy, 6f, 16f, Color.white);
            DrawCheekOval(tex, cx + r * 0.55f, cy, 6f, 16f, Color.white);
        }

        private void DrawNekoEars(Texture2D tex, float cx, float cy, float r, Color body, Color outline)
        {
            DrawCheekOval(tex, cx - r * 0.6f, cy - 2f, 8f, 12f, body);
            DrawCheekOval(tex, cx + r * 0.6f, cy - 2f, 8f, 12f, body);
            DrawCheekOval(tex, cx - r * 0.6f, cy, 4f, 8f, new Color(1f, 0.7f, 0.8f));
            DrawCheekOval(tex, cx + r * 0.6f, cy, 4f, 8f, new Color(1f, 0.7f, 0.8f));
        }

        private void DrawPandaEars(Texture2D tex, float cx, float cy, float r, Color outline)
        {
            DrawCheekOval(tex, cx - r * 0.6f, cy, 10f, 10f, outline);
            DrawCheekOval(tex, cx + r * 0.6f, cy, 10f, 10f, outline);
        }

        private void DrawDemonHorns(Texture2D tex, float cx, float cy, float r)
        {
            DrawCheekOval(tex, cx - r * 0.45f, cy, 6f, 14f, Color.red);
            DrawCheekOval(tex, cx + r * 0.45f, cy, 6f, 14f, Color.red);
        }

        private void DrawCrown(Texture2D tex, float cx, float cy, float r)
        {
            for (int y = (int)(cy - 2); y <= (int)(cy + 5); y++)
            for (int x = (int)(cx - r * 0.65f); x <= (int)(cx + r * 0.65f); x++)
            {
                if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                {
                    tex.SetPixel(x, y, new Color(0.99f, 0.88f, 0.2f));
                }
            }
            // crowns spikes
            DrawTriangle(tex, new Vector2(cx - r * 0.5f, cy + 5), new Vector2(cx - r * 0.5f, cy + 12), new Vector2(cx - r * 0.2f, cy + 5), new Color(0.99f, 0.88f, 0.2f));
            DrawTriangle(tex, new Vector2(cx - 3, cy + 5), new Vector2(cx, cy + 15), new Vector2(cx + 3, cy + 5), new Color(0.99f, 0.88f, 0.2f));
            DrawTriangle(tex, new Vector2(cx + r * 0.2f, cy + 5), new Vector2(cx + r * 0.5f, cy + 12), new Vector2(cx + r * 0.5f, cy + 5), new Color(0.99f, 0.88f, 0.2f));
        }

        private void DrawDragonHorns(Texture2D tex, float cx, float cy, float r)
        {
            DrawCheekOval(tex, cx - r * 0.45f, cy, 7f, 15f, new Color(0.85f, 0.15f, 0.15f));
            DrawCheekOval(tex, cx + r * 0.45f, cy, 7f, 15f, new Color(0.85f, 0.15f, 0.15f));
        }

        private void DrawShadowMask(Texture2D tex, float cx, float cy, float r)
        {
            for (int y = (int)(cy - r * 0.2f); y <= (int)(cy + r * 0.3f); y++)
            for (int x = (int)(cx - r * 0.7f); x <= (int)(cx + r * 0.7f); x++)
            {
                if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                {
                    Color orig = tex.GetPixel(x, y);
                    tex.SetPixel(x, y, Color.Lerp(orig, new Color(0.08f, 0.05f, 0.15f), 0.7f));
                }
            }
        }

        private void DrawStar(Texture2D tex, float cx, float cy, float r, Color col)
        {
            DrawCheekOval(tex, cx, cy, r, r, col);
            DrawCheekOval(tex, cx, cy, r * 0.3f, r * 2.5f, col);
            DrawCheekOval(tex, cx, cy, r * 2.5f, r * 0.3f, col);
        }

        private void DrawOmegaSymbol(Texture2D tex, float cx, float cy, float r, Color col)
        {
            DrawCheekOval(tex, cx, cy, r, r, col);
        }

        private void DrawLine(Texture2D tex, float x0, float y0, float x1, float y1, Color col)
        {
            int w = tex.width;
            int h = tex.height;
            int x = (int)x0;
            int y = (int)y0;
            int dx = (int)Mathf.Abs(x1 - x0);
            int dy = (int)Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                if (x >= 0 && x < w && y >= 0 && y < h)
                    tex.SetPixel(x, y, col);

                if (x == (int)x1 && y == (int)y1) break;
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }
        }

        private void DrawTriangle(Texture2D tex, Vector2 p0, Vector2 p1, Vector2 p2, Color col)
        {
            int minX = (int)Mathf.Min(p0.x, Mathf.Min(p1.x, p2.x));
            int maxX = (int)Mathf.Max(p0.x, Mathf.Max(p1.x, p2.x));
            int minY = (int)Mathf.Min(p0.y, Mathf.Min(p1.y, p2.y));
            int maxY = (int)Mathf.Max(p0.y, Mathf.Max(p1.y, p2.y));

            minX = Mathf.Clamp(minX, 0, tex.width - 1);
            maxX = Mathf.Clamp(maxX, 0, tex.width - 1);
            minY = Mathf.Clamp(minY, 0, tex.height - 1);
            maxY = Mathf.Clamp(maxY, 0, tex.height - 1);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (PointInTriangle(new Vector2(x, y), p0, p1, p2))
                    {
                        tex.SetPixel(x, y, col);
                    }
                }
            }
        }

        private bool PointInTriangle(Vector2 p, Vector2 p0, Vector2 p1, Vector2 p2)
        {
            float s = p0.y * p2.x - p0.x * p2.y + (p2.y - p0.y) * p.x + (p0.x - p2.x) * p.y;
            float t = p0.x * p1.y - p0.y * p1.x + (p0.y - p1.y) * p.x + (p1.x - p0.x) * p.y;

            if ((s < 0) != (t < 0))
                return false;

            float a = -p1.y * p2.x + p0.y * (p2.x - p1.x) + p0.x * (p1.y - p2.y) + p1.x * p2.y;
            return a < 0 ?
                    (s <= 0 && s + t >= a) :
                    (s >= 0 && s + t <= a);
        }

        private void DrawRect(Texture2D tex, float x, float y, float w, float h, Color col)
        {
            int x0 = (int)x;
            int y0 = (int)y;
            int x1 = (int)(x + w);
            int y1 = (int)(y + h);

            x0 = Mathf.Clamp(x0, 0, tex.width - 1);
            x1 = Mathf.Clamp(x1, 0, tex.width - 1);
            y0 = Mathf.Clamp(y0, 0, tex.height - 1);
            y1 = Mathf.Clamp(y1, 0, tex.height - 1);

            for (int j = y0; j <= y1; j++)
            for (int i = x0; i <= x1; i++)
            {
                tex.SetPixel(i, j, col);
            }
        }

        // ── PVE Enemy Shapes Rendering ───────────────────────────
        private void GenerateEnemyTexture(Texture2D tex, string type, float c, float r, Color c1, Color c2, Color cOut)
        {
            if (type == "slime")
            {
                // Squished wavy blob
                for (int y = 0; y < tex.height; y++)
                for (int x = 0; x < tex.width; x++)
                {
                    float dx = (x - c)/c, dy = (y - c)/c;
                    float blob = dx*dx + dy*dy * 1.4f + 0.12f * Mathf.Sin(dx * 8f) + 0.08f * Mathf.Cos(dy * 6f);
                    if (blob < 0.78f)
                    {
                        Color col = Color.Lerp(c1, c2, blob / 0.78f);
                        tex.SetPixel(x, y, blob > 0.70f ? cOut : col);
                    }
                }
                // Slime face
                DrawCircle(tex, (int)(c - r * 0.3f), (int)(c + r * 0.1f), (int)(r * 0.16f), cOut);
                DrawCircle(tex, (int)(c + r * 0.3f), (int)(c + r * 0.1f), (int)(r * 0.16f), cOut);
            }
            else if (type == "goblin")
            {
                // Green pointed diamond with floppy ears
                for (int y = 0; y < tex.height; y++)
                for (int x = 0; x < tex.width; x++)
                {
                    float nx = Mathf.Abs(x - c)/c, ny = Mathf.Abs(y - c)/c;
                    float dist = nx + ny * 1.1f;
                    if (dist < 0.8f)
                    {
                        tex.SetPixel(x, y, dist > 0.72f ? cOut : c1);
                    }
                }
                // Pointy Ears
                DrawTriangle(tex, new Vector2(c - r * 0.7f, c + 4), new Vector2(c - r * 1.3f, c + 15), new Vector2(c - r * 0.5f, c - 2), c1);
                DrawTriangle(tex, new Vector2(c + r * 0.7f, c + 4), new Vector2(c + r * 1.3f, c + 15), new Vector2(c + r * 0.5f, c - 2), c1);
                // Eyes
                DrawCircle(tex, (int)(c - r * 0.25f), (int)(c + r * 0.15f), (int)(r * 0.12f), Color.red);
                DrawCircle(tex, (int)(c + r * 0.25f), (int)(c + r * 0.15f), (int)(r * 0.12f), Color.red);
            }
            else if (type == "skeleton")
            {
                // Skull head
                DrawCheekOval(tex, c, c, r * 0.8f, r * 0.75f, cOut);
                DrawCheekOval(tex, c, c, r * 0.7f, r * 0.65f, new Color(0.9f, 0.9f, 0.85f));
                // Jaw teeth
                DrawRect(tex, c - r * 0.35f, c - r * 0.6f, r * 0.7f, r * 0.2f, new Color(0.9f, 0.9f, 0.85f));
                // Hollow black eyes with red glow
                DrawSingleSkeletonEye(tex, c - r * 0.25f, c + r * 0.1f, r * 0.18f);
                DrawSingleEyeRed(tex, c - r * 0.22f, c + r * 0.08f, 2.5f);
                DrawSingleSkeletonEye(tex, c + r * 0.25f, c + r * 0.1f, r * 0.18f);
                DrawSingleEyeRed(tex, c + r * 0.22f, c + r * 0.08f, 2.5f);
            }
            else if (type == "demon")
            {
                // Spiky star red body
                for (int y = 0; y < tex.height; y++)
                for (int x = 0; x < tex.width; x++)
                {
                    float dx = (x - c)/c, dy = (y - c)/c;
                    float angle = Mathf.Atan2(dy, dx);
                    float r2    = Mathf.Sqrt(dx*dx + dy*dy);
                    float star  = r2 * (0.6f + 0.4f * Mathf.Abs(Mathf.Cos(5f * angle)));
                    if (star < 0.75f)
                    {
                        tex.SetPixel(x, y, star > 0.68f ? cOut : c1);
                    }
                }
                // demon horns
                DrawCheekOval(tex, c - r * 0.45f, c + r * 0.7f, 5f, 12f, Color.black);
                DrawCheekOval(tex, c + r * 0.45f, c + r * 0.7f, 5f, 12f, Color.black);
            }
            else if (type == "wraith")
            {
                // Purple ghost shape fading at bottom
                for (int y = 0; y < tex.height; y++)
                for (int x = 0; x < tex.width; x++)
                {
                    float dx = (x - c)/c, dy = (y - c)/c;
                    float dist = Mathf.Sqrt(dx*dx + (dy - 0.1f)*(dy - 0.1f));
                    float wave = y > c ? 0.18f * Mathf.Sin((x - c)*0.3f) : 0f;
                    if (dist + wave < 0.8f)
                    {
                        float alpha = 1f - (c * 1.1f - y) / (c * 1.3f);
                        Color fill = Color.Lerp(c1, c2, dist);
                        fill.a = Mathf.Clamp01(alpha * 0.8f);
                        tex.SetPixel(x, y, fill);
                    }
                }
                // Glowing eyes
                DrawCircle(tex, (int)(c - r * 0.25f), (int)(c + r * 0.1f), 3, new Color(0.2f, 0.9f, 1f));
                DrawCircle(tex, (int)(c + r * 0.25f), (int)(c + r * 0.1f), 3, new Color(0.2f, 0.9f, 1f));
            }
            else if (type == "golem")
            {
                // Hexagonal stone with cracked lines
                for (int y = 0; y < tex.height; y++)
                for (int x = 0; x < tex.width; x++)
                {
                    float dx = Mathf.Abs(x - c)/c, dy = Mathf.Abs(y - c)/c;
                    float hex = Mathf.Max(dx * 1.0f, 0.577f * dx + dy * 0.866f);
                    if (hex < 0.8f)
                    {
                        bool isCrack = Mathf.Abs(Mathf.Sin((x + y) * 0.4f)) < 0.1f;
                        tex.SetPixel(x, y, hex > 0.72f ? cOut : (isCrack ? Color.yellow : c1));
                    }
                }
            }
            else if (type == "vampire")
            {
                // Pale face, red cape
                DrawCheekOval(tex, c, c - r * 0.2f, r * 0.9f, r * 0.6f, new Color(0.6f, 0.05f, 0.12f)); // cape collar
                DrawGradientCircle(tex, c, c + r * 0.15f, r * 0.65f, Color.white, new Color(0.9f,0.9f,0.9f), Color.lightGray, cOut, 2f);
                // red eyes
                DrawCircle(tex, (int)(c - r * 0.22f), (int)(c + r * 0.2f), 3, Color.red);
                DrawCircle(tex, (int)(c + r * 0.22f), (int)(c + r * 0.2f), 3, Color.red);
            }
            else if (type == "witch")
            {
                // Green face, pointy purple witch hat
                DrawGradientCircle(tex, c, c, r * 0.65f, new Color(0.5f, 0.9f, 0.3f), new Color(0.3f, 0.7f, 0.2f), new Color(0.15f, 0.5f, 0.1f), cOut, 2f);
                // pointy hat
                DrawTriangle(tex, new Vector2(c - r * 0.9f, c + 3f), new Vector2(c, c + r * 1.6f), new Vector2(c + r * 0.9f, c + 3f), new Color(0.3f, 0.1f, 0.5f));
            }
            else
            {
                // Giant/Orc: huge solid elemental gradient circle
                DrawGradientCircle(tex, c, c, r, c1 + new Color(0.2f,0.2f,0.2f), c1, c2, cOut, 3.5f);
                // glowing red eyes
                DrawCircle(tex, (int)(c - r * 0.28f), (int)(c + r * 0.12f), 4, Color.red);
                DrawCircle(tex, (int)(c + r * 0.28f), (int)(c + r * 0.12f), 4, Color.red);
            }
        }

        private static void DrawCircle(Texture2D tex, int cx, int cy, int r, Color col)
        {
            for (int y = cy - r; y <= cy + r; y++)
            for (int x = cx - r; x <= cx + r; x++)
            {
                if (x < 0 || y < 0 || x >= tex.width || y >= tex.height) continue;
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) < r * r) tex.SetPixel(x, y, col);
            }
        }

        private void DrawSingleSkeletonEye(Texture2D tex, float ex, float ey, float er)
        {
            for (int y = (int)(ey - er); y <= (int)(ey + er); y++)
            for (int x = (int)(ex - er); x <= (int)(ex + er); x++)
            {
                if (x < 0 || y < 0 || x >= tex.width || y >= tex.height) continue;
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(ex, ey));
                if (d < er) tex.SetPixel(x, y, Color.black);
            }
        }

        private void DrawSingleEyeRed(Texture2D tex, float ex, float ey, float er)
        {
            for (int y = (int)(ey - er); y <= (int)(ey + er); y++)
            for (int x = (int)(ex - er); x <= (int)(ex + er); x++)
            {
                if (x < 0 || y < 0 || x >= tex.width || y >= tex.height) continue;
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(ex, ey));
                if (d < er) tex.SetPixel(x, y, Color.red);
            }
        }

        // ── CSS Skins Data Synced ──
        public struct SkinDef
        {
            public string body1;
            public string body2;
            public string bodyHL;
            public string eye;
            public string outline;
            public string aura;
            public string skinType;
        }

        private static readonly Dictionary<int, SkinDef> BaseSkins = new()
        {
            { 1, new SkinDef { body1="#4ade80", body2="#15803d", bodyHL="#bbf7d0", eye="#166534", outline="#052e16", aura=null,    skinType="leaf" } },
            { 2, new SkinDef { body1="#38bdf8", body2="#0369a1", bodyHL="#bae6fd", eye="#075985", outline="#082f49", aura="#22d3ee",skinType="horns" } },
            { 3, new SkinDef { body1="#f472b6", body2="#9d174d", bodyHL="#fbcfe8", eye="#831843", outline="#500724", aura="#38bdf8",skinType="neko" } },
            { 4, new SkinDef { body1="#ef4444", body2="#7f1d1d", bodyHL="#fecaca", eye="#450a0a", outline="#1c0303", aura="#f97316",skinType="panda" } },
            { 5, new SkinDef { body1="#a855f7", body2="#3b0764", bodyHL="#d8b4fe", eye="#c026d3", outline="#1a0030", aura="#7c3aed",skinType="demon" } },
            { 6, new SkinDef { body1="#fbbf24", body2="#92400e", bodyHL="#fef08a", eye="#451a03", outline="#1c0a00", aura="#f97316",skinType="boss" } },
            { 7, new SkinDef { body1="#f97316", body2="#431407", bodyHL="#fed7aa", eye="#7c2d12", outline="#1c0a00", aura="#dc2626",skinType="dragon" } },
            { 8, new SkinDef { body1="#1e1b4b", body2="#0f0e26", bodyHL="#4c1d95", eye="#c084fc", outline="#000000", aura="#6d28d9",skinType="shadow" } },
            { 9, new SkinDef { body1="#e0e7ff", body2="#6366f1", bodyHL="#ffffff", eye="#312e81", outline="#1e1b4b", aura="#818cf8",skinType="celestial" } },
            { 10, new SkinDef { body1="#ffffff", body2="#d1d5db", bodyHL="#ffffff", eye="#000000", outline="#1f2937", aura="#fbbf24",skinType="omega" } }
        };

        private static readonly Dictionary<string, Dictionary<int, SkinDef>> BranchSkins = new()
        {
            { "assassin", new Dictionary<int, SkinDef> {
                { 5, new SkinDef { body1="#059669", body2="#047857", bodyHL="#a7f3d0", eye="#c084fc", outline="#064e3b", aura="#a855f7", skinType="leaf" } },
                { 6, new SkinDef { body1="#8b5cf6", body2="#5b21b6", bodyHL="#ddd6fe", eye="#22d3ee", outline="#2e1065", aura="#a855f7", skinType="horns" } },
                { 7, new SkinDef { body1="#312e81", body2="#1e1b4b", bodyHL="#c7d2fe", eye="#4ade80", outline="#000000", aura="#10b981", skinType="demon" } },
                { 8, new SkinDef { body1="#0f172a", body2="#020617", bodyHL="#334155", eye="#a855f7", outline="#000000", aura="#6d28d9", skinType="shadow" } },
                { 9, new SkinDef { body1="#1e1b4b", body2="#090514", bodyHL="#3b0764", eye="#ec4899", outline="#000000", aura="#db2777", skinType="celestial" } },
                { 10, new SkinDef { body1="#00f2fe", body2="#4facfe", bodyHL="#e0f2fe", eye="#ffffff", outline="#000000", aura="#a855f7", skinType="omega" } }
            }},
            { "fighter", new Dictionary<int, SkinDef> {
                { 5, new SkinDef { body1="#cbd5e1", body2="#64748b", bodyHL="#f1f5f9", eye="#38bdf8", outline="#334155", aura="#38bdf8", skinType="neko" } },
                { 6, new SkinDef { body1="#ef4444", body2="#7f1d1d", bodyHL="#fecaca", eye="#fbbf24", outline="#450a0a", aura="#f97316", skinType="horns" } },
                { 7, new SkinDef { body1="#4b5563", body2="#1f2937", bodyHL="#9ca3af", eye="#fbbf24", outline="#111827", aura="#fbbf24", skinType="demon" } },
                { 8, new SkinDef { body1="#fbbf24", body2="#92400e", bodyHL="#fef08a", eye="#ef4444", outline="#451a03", aura="#fbbf24", skinType="boss" } },
                { 9, new SkinDef { body1="#0f172a", body2="#1a0d00", bodyHL="#334155", eye="#f97316", outline="#000000", aura="#f97316", skinType="shadow" } },
                { 10, new SkinDef { body1="#fffbeb", body2="#fbbf24", bodyHL="#ffffff", eye="#ffffff", outline="#78350f", aura="#fbbf24", skinType="omega" } }
            }},
            { "mage", new Dictionary<int, SkinDef> {
                { 5, new SkinDef { body1="#c084fc", body2="#6b21a8", bodyHL="#e9d5ff", eye="#e9d5ff", outline="#3b0764", aura="#8b5cf6", skinType="leaf" } },
                { 6, new SkinDef { body1="#f97316", body2="#9a3412", bodyHL="#fed7aa", eye="#fde047", outline="#431407", aura="#ef4444", skinType="horns" } },
                { 7, new SkinDef { body1="#93c5fd", body2="#1d4ed8", bodyHL="#dbeafe", eye="#e0f2fe", outline="#1e3a8a", aura="#38bdf8", skinType="neko" } },
                { 8, new SkinDef { body1="#fef08a", body2="#ca8a04", bodyHL="#fef9c3", eye="#38bdf8", outline="#422006", aura="#eab308", skinType="demon" } },
                { 9, new SkinDef { body1="#818cf8", body2="#3730a3", bodyHL="#c7d2fe", eye="#ffffff", outline="#1e1b4b", aura="#a5b4fc", skinType="celestial" } },
                { 10, new SkinDef { body1="#fef08a", body2="#854d0e", bodyHL="#fef9c3", eye="#000000", outline="#422006", aura="#fbbf24", skinType="omega" } }
            }},
            { "ranger", new Dictionary<int, SkinDef> {
                { 5, new SkinDef { body1="#4ade80", body2="#166534", bodyHL="#bbf7d0", eye="#166534", outline="#052e16", aura="#22c55e", skinType="leaf" } },
                { 6, new SkinDef { body1="#86efac", body2="#15803d", bodyHL="#bbf7d0", eye="#15803d", outline="#052e16", aura="#4ade80", skinType="horns" } },
                { 7, new SkinDef { body1="#059669", body2="#064e3b", bodyHL="#a7f3d0", eye="#059669", outline="#000000", aura="#10b981", skinType="neko" } },
                { 8, new SkinDef { body1="#34d399", body2="#065f46", bodyHL="#a7f3d0", eye="#c084fc", outline="#000000", aura="#059669", skinType="demon" } },
                { 9, new SkinDef { body1="#00f2fe", body2="#1d4ed8", bodyHL="#e0f2fe", eye="#22d3ee", outline="#000000", aura="#38bdf8", skinType="celestial" } },
                { 10, new SkinDef { body1="#ffffff", body2="#047857", bodyHL="#e8f5e9", eye="#000000", outline="#000000", aura="#10b981", skinType="omega" } }
            }},
            { "paladin", new Dictionary<int, SkinDef> {
                { 5, new SkinDef { body1="#fef08a", body2="#ca8a04", bodyHL="#fef9c3", eye="#ca8a04", outline="#422006", aura="#eab308", skinType="leaf" } },
                { 6, new SkinDef { body1="#fde68a", body2="#b45309", bodyHL="#fef3c7", eye="#b45309", outline="#451a03", aura="#f59e0b", skinType="horns" } },
                { 7, new SkinDef { body1="#fbbf24", body2="#78350f", bodyHL="#fef3c7", eye="#78350f", outline="#451a03", aura="#d97706", skinType="neko" } },
                { 8, new SkinDef { body1="#fffbeb", body2="#d97706", bodyHL="#ffffff", eye="#fbbf24", outline="#000000", aura="#fbbf24", skinType="demon" } },
                { 9, new SkinDef { body1="#ffffff", body2="#eab308", bodyHL="#ffffff", eye="#ffffff", outline="#000000", aura="#fbbf24", skinType="celestial" } },
                { 10, new SkinDef { body1="#ffffff", body2="#fbbf24", bodyHL="#ffffff", eye="#ffffff", outline="#78350f", aura="#fbbf24", skinType="omega" } }
            }},
            { "necromancer", new Dictionary<int, SkinDef> {
                { 5, new SkinDef { body1="#475569", body2="#1e293b", bodyHL="#64748b", eye="#a855f7", outline="#0f172a", aura="#475569", skinType="leaf" } },
                { 6, new SkinDef { body1="#334155", body2="#0f172a", bodyHL="#475569", eye="#22d3ee", outline="#000000", aura="#334155", skinType="horns" } },
                { 7, new SkinDef { body1="#1e1b4b", body2="#0f0e26", bodyHL="#312e81", eye="#ef4444", outline="#000000", aura="#4f46e5", skinType="demon" } },
                { 8, new SkinDef { body1="#581c87", body2="#2e1065", bodyHL="#7e22ce", eye="#a855f7", outline="#000000", aura="#a855f7", skinType="shadow" } },
                { 9, new SkinDef { body1="#020617", body2="#000000", bodyHL="#1e293b", eye="#c084fc", outline="#000000", aura="#6d28d9", skinType="celestial" } },
                { 10, new SkinDef { body1="#000000", body2="#1e1b4b", bodyHL="#4c1d95", eye="#ffffff", outline="#000000", aura="#a855f7", skinType="omega" } }
            }},
            { "druid", new Dictionary<int, SkinDef> {
                { 5, new SkinDef { body1="#86efac", body2="#166534", bodyHL="#bbf7d0", eye="#166534", outline="#052e16", aura="#22c55e", skinType="leaf" } },
                { 6, new SkinDef { body1="#4ade80", body2="#15803d", bodyHL="#bbf7d0", eye="#15803d", outline="#052e16", aura="#4ade80", skinType="horns" } },
                { 7, new SkinDef { body1="#b45309", body2="#78350f", bodyHL="#d97706", eye="#78350f", outline="#451a03", aura="#f59e0b", skinType="panda" } },
                { 8, new SkinDef { body1="#059669", body2="#064e3b", bodyHL="#a7f3d0", eye="#4ade80", outline="#000000", aura="#10b981", skinType="demon" } },
                { 9, new SkinDef { body1="#10b981", body2="#047857", bodyHL="#6ee7b7", eye="#a7f3d0", outline="#000000", aura="#10b981", skinType="celestial" } },
                { 10, new SkinDef { body1="#ffffff", body2="#065f46", bodyHL="#a7f3d0", eye="#ffffff", outline="#000000", aura="#10b981", skinType="omega" } }
            }}
        };

        private SkinDef GetSkin(int level, string branch, bool morphed)
        {
            if (morphed)
            {
                return new SkinDef { bodyHL = "#ef4444", body1 = "#7f1d1d", body2 = "#450a0a", outline = "#f87171", aura = "#ef4444", skinType = "demon", eye = "#ff0000" };
            }

            int clLvl = Mathf.Clamp(level, 1, 10);
            if (clLvl >= 5 && !string.IsNullOrEmpty(branch) && BranchSkins.TryGetValue(branch, out var list))
            {
                if (list.TryGetValue(clLvl, out var sk)) return sk;
            }

            if (BaseSkins.TryGetValue(clLvl, out var baseSk)) return baseSk;
            return BaseSkins[1];
        }

        private Color ParseHex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Color.white;
            if (ColorUtility.TryParseHtmlString(hex, out var col)) return col;
            return Color.white;
        }
    }
}
