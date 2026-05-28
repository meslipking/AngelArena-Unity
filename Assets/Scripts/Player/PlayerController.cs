using System;
using System.Collections.Generic;
using UnityEngine;
using AngelArena.Data;

namespace AngelArena.Core
{
    /// <summary>
    /// Player controller: movement, stats, XP/leveling, skill execution.
    /// Uses Unity's classic Input system (no package dependency needed).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        // ── Events ──────────────────────────────────────────────
        public static event Action<float, float> OnHpChanged;
        public static event Action<int, int>     OnXpChanged;

        [Header("Character Data")]
        public CharacterData characterData;

        [Header("Skill System")]
        public SkillSystem skillSystem;

        // ── Runtime Stats ────────────────────────────────────────
        public int   Level       { get; set; } = 1;
        public float Hp          { get; set; }
        public float MaxHp       { get; set; }
        public float MoveSpeed   { get; set; }
        public float DamageMult  { get; set; } = 1f;
        public float DefMult     { get; set; } = 1f;
        public float CdMult      { get; set; } = 1f;
        public float AoeMult     { get; set; } = 1f;
        public float SpeedMult   { get; set; } = 1f;
        public float XpGainMult  { get; set; } = 1f;
        public float Lifesteal   { get; set; }
        public float HpRegen     { get; set; }
        public float MagnetRadius { get; set; } = 100f; // default magnet radius
        public float GoldMult    { get; set; } = 1f;   // gold scaling multiplier
        public float CritChance  { get; set; } = 0.05f; // default crit chance
        public int   ProjectileAmount { get; set; } = 0; // extra projectile amount
        public bool  IsAlive     => Hp > 0;

        private int   _xp;
        private int   _xpToNext;

        // ── Components ───────────────────────────────────────────
        private Rigidbody2D    _rb;
        private Animator       _animator;
        private SpriteRenderer _sr;
        private float          _flashTimer;
        private float          _iFrameTimer;
        private float          _trailTimer;
        private const float    FLASH_DURATION  = 0.12f;
        private const float    IFRAME_DURATION = 0.5f;

        // ── Input ─────────────────────────────────────────────────
        private Vector2 _moveInput;

        // ─────────────────────────────────────────────────────────
        private void Awake()
        {
            _rb       = GetComponent<Rigidbody2D>();
            _sr       = GetComponentInChildren<SpriteRenderer>();
            _animator = GetComponentInChildren<Animator>();
            if (characterData != null) InitFromData(characterData);
        }

        public void InitFromData(CharacterData data)
        {
            characterData = data;
            
            // Read powerups from SaveSystem
            var save = Save.SaveSystem.Instance != null ? Save.SaveSystem.Instance.CurrentSave : null;
            var pu = save != null ? save.powerups : new Save.SaveSystem.PowerupsState();

            // Apply PVE powerups
            float puVitality = pu.vitality * 0.05f; // +5% HP per level
            MaxHp         = data.maxHp * (1f + puVitality);
            Hp            = MaxHp;
            
            float puSwiftness = pu.swiftness * 0.02f; // +2% speed per level
            MoveSpeed     = data.moveSpeed * (1f + puSwiftness);
            
            float puMight = pu.might * 0.05f; // +5% attack mult per level
            DamageMult    = data.atkMult * (1f + puMight);
            
            DefMult       = data.defMult;
            Lifesteal     = data.baseLifesteal;
            
            float puRecovery = pu.recovery * 0.5f; // +0.5 HP/s per level
            HpRegen       = puRecovery;
            
            float puCooldown = pu.cooldown * -0.025f; // -2.5% CD per level
            CdMult        = 1f + puCooldown;
            
            float puGrowth = pu.growth * 0.10f; // +10% XP per level
            XpGainMult    = 1f + puGrowth;
            
            float puGreed = pu.greed * 0.15f; // +15% Gold per level
            GoldMult      = 1f + puGreed;
            
            float puLuck = pu.luck * 0.02f; // +2% luck/crit per level
            CritChance    = 0.05f + puLuck;
            
            MagnetRadius  = 100f + pu.magnet * 80f; // +80px magnet per level
            ProjectileAmount = pu.amount; // +1 projectile count per level

            Level         = 1;
            _xp           = 0;
            _xpToNext     = WaveScaling.GetXpToNext(1);
            OnHpChanged?.Invoke(Hp, MaxHp);
            OnXpChanged?.Invoke(_xp, _xpToNext);
        }

        /// <summary>Fallback init when no CharacterData is assigned (for quick testing).</summary>
        public void InitDefaults()
        {
            // Read powerups from SaveSystem
            var save = Save.SaveSystem.Instance != null ? Save.SaveSystem.Instance.CurrentSave : null;
            var pu = save != null ? save.powerups : new Save.SaveSystem.PowerupsState();

            float puVitality = pu.vitality * 0.05f;
            MaxHp      = 500f * (1f + puVitality);
            Hp         = MaxHp;
            
            float puSwiftness = pu.swiftness * 0.02f;
            MoveSpeed  = 280f * (1f + puSwiftness);
            
            float puMight = pu.might * 0.05f;
            DamageMult = 1f * (1f + puMight);
            
            DefMult    = 1f;
            Lifesteal  = 0f;
            
            float puRecovery = pu.recovery * 0.5f;
            HpRegen    = 1f + puRecovery;
            
            float puCooldown = pu.cooldown * -0.025f;
            CdMult     = 1f + puCooldown;
            
            float puGrowth = pu.growth * 0.10f;
            XpGainMult = 1f + puGrowth;
            
            float puGreed = pu.greed * 0.15f;
            GoldMult   = 1f + puGreed;
            
            float puLuck = pu.luck * 0.02f;
            CritChance = 0.05f + puLuck;
            
            MagnetRadius = 100f + pu.magnet * 80f;
            ProjectileAmount = pu.amount;

            Level      = 1;
            _xp        = 0;
            _xpToNext  = WaveScaling.GetXpToNext(1);
            OnHpChanged?.Invoke(Hp, MaxHp);
            OnXpChanged?.Invoke(_xp, _xpToNext);
            Debug.Log("[AngelArena] Player initialized with DEFAULT stats and PVE powerups");
        }

        private void Update()
        {
            if (!GameManager.Instance || !GameManager.Instance.GameRunning) return;

            // Classic Input System (works without package)
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            _moveInput = new Vector2(h, v).normalized;

            // HP regen
            if (HpRegen > 0 && Hp < MaxHp)
                TakeDamage(-HpRegen * Time.deltaTime);

            // Flash
            if (_flashTimer > 0)
            {
                _flashTimer -= Time.deltaTime;
                if (_sr) _sr.color = _flashTimer > 0 ? new Color(1f, 0.3f, 0.3f) : Color.white;
            }

            // i-frames
            if (_iFrameTimer > 0) _iFrameTimer -= Time.deltaTime;

            // Flip sprite
            if (_sr && _moveInput.x != 0)
                _sr.flipX = _moveInput.x < 0;

            // Animator
            if (_animator) _animator.SetBool("IsMoving", _moveInput.sqrMagnitude > 0.01f);

            // Speed Trail
            if (_moveInput.sqrMagnitude > 0.01f)
            {
                _trailTimer -= Time.deltaTime;
                if (_trailTimer <= 0)
                {
                    _trailTimer = 0.08f;
                    SkillVFX.SpawnSpeedTrail(transform.position, _moveInput, new Color(0.2f, 0.8f, 1f, 0.22f), 0.3f);
                }
            }
        }

        private void FixedUpdate()
        {
            if (!GameManager.Instance || !GameManager.Instance.GameRunning) return;
            _rb.linearVelocity = _moveInput * MoveSpeed * SpeedMult;
        }

        // ── Combat ───────────────────────────────────────────────
        public float TakeDamage(float rawDamage)
        {
            if (!IsAlive) return 0;

            if (rawDamage > 0)
            {
                if (_iFrameTimer > 0) return 0;
                float mitigated = rawDamage / Mathf.Max(0.1f, DefMult);
                Hp = Mathf.Max(0, Hp - mitigated);
                _flashTimer  = FLASH_DURATION;
                _iFrameTimer = IFRAME_DURATION;

                HUDManager.Instance?.TriggerDamageVignette();
                if (Hp <= 0) GameManager.Instance.TriggerGameOver();
                OnHpChanged?.Invoke(Hp, MaxHp);
                return mitigated;
            }
            else
            {
                float heal = Mathf.Abs(rawDamage);
                Hp = Mathf.Min(MaxHp, Hp + heal);
                OnHpChanged?.Invoke(Hp, MaxHp);
                return -heal;
            }
        }

        public void ApplyLifesteal(float damageDealt)
        {
            if (Lifesteal > 0) TakeDamage(-(damageDealt * Lifesteal));
        }

        // ── XP / Level ───────────────────────────────────────────
        public void GainXp(int amount)
        {
            int final = Mathf.RoundToInt(amount * XpGainMult);
            _xp += final;
            while (_xp >= _xpToNext)
            {
                _xp -= _xpToNext;
                Level++;
                _xpToNext = WaveScaling.GetXpToNext(Level);
                OnLevelUpInternal();
            }
            OnXpChanged?.Invoke(_xp, _xpToNext);
        }

        private void OnLevelUpInternal()
        {
            if (Level == 20 || Level == 40) XpGainMult *= 2f;
            GameManager.OnLevelUp?.Invoke(Level);
            UIUpgradeScreen.Show(Level, skillSystem);
        }

        [System.Serializable]
        public class PassiveItemState
        {
            public string itemId;
            public string itemName;
            public Sprite icon;
            public Color iconColor;
            public int level = 1;
        }

        [Header("Passives Tracker")]
        public List<PassiveItemState> ownedPassives = new();

        public void AddPassiveItem(string id, string name, Sprite icon, Color color, Data.PassiveEffect effect, float valuePerLevel)
        {
            var existing = ownedPassives.Find(p => p.itemId == id || p.itemName == name);
            if (existing != null)
            {
                existing.level++;
            }
            else
            {
                ownedPassives.Add(new PassiveItemState { itemId = id, itemName = name, icon = icon, iconColor = color, level = 1 });
            }
            ApplyPassiveEffect(effect, valuePerLevel, 1);
            if (skillSystem != null)
            {
                skillSystem.CheckLegendary();
            }
        }

        // ── Passives ─────────────────────────────────────────────
        public void ApplyPassiveEffect(Data.PassiveEffect effect, float valuePerLevel, int level)
        {
            float total = valuePerLevel * level;
            switch (effect)
            {
                case Data.PassiveEffect.DamageMult:   DamageMult *= 1f + total; break;
                case Data.PassiveEffect.DefMult:      DefMult    *= 1f + total; break;
                case Data.PassiveEffect.MaxHpMult:    MaxHp      *= 1f + total; Hp = Mathf.Min(Hp, MaxHp); break;
                case Data.PassiveEffect.SpeedMult:    SpeedMult  *= 1f + total; break;
                case Data.PassiveEffect.CooldownMult: CdMult     *= 1f - total; break;
                case Data.PassiveEffect.AoeMult:      AoeMult    *= 1f + total; break;
                case Data.PassiveEffect.LifeSteal:    Lifesteal  += total;      break;
                case Data.PassiveEffect.XpGainMult:   XpGainMult *= 1f + total; break;
                case Data.PassiveEffect.HpRegen:      HpRegen    += total;      break;
                case Data.PassiveEffect.GoldMult:     GoldMult   *= 1f + total; break;
                case Data.PassiveEffect.CritChance:   CritChance += total;      break;
                case Data.PassiveEffect.MagnetRadius: MagnetRadius *= 1f + total; break;
            }
            OnHpChanged?.Invoke(Hp, MaxHp);
        }

        // ── World clamp ──────────────────────────────────────────
        private void LateUpdate()
        {
            var pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, -GameConstants.WORLD_W / 2f, GameConstants.WORLD_W / 2f);
            pos.y = Mathf.Clamp(pos.y, -GameConstants.WORLD_H / 2f, GameConstants.WORLD_H / 2f);
            transform.position = pos;
        }
    }
}
