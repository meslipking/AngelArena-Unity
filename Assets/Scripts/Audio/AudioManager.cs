using UnityEngine;
using UnityEngine.Audio;

namespace AngelArena.Audio
{
    /// <summary>
    /// AudioManager: BGM cross-fade, SFX pooling, volume control.
    /// No external package dependencies.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Mixer (optional)")]
        public AudioMixer masterMixer;

        [Header("BGM Sources")]
        public AudioSource bgmA;
        public AudioSource bgmB;
        private int _activeBgm = 0;

        [Header("BGM Tracks — 0=calm, 1=battle, 2=boss, 3=victory")]
        public AudioClip[] bgmTracks;

        [Header("SFX Pool")]
        [Range(5, 20)] public int sfxPoolSize = 10;
        private AudioSource[] _sfxPool;
        private int           _sfxIndex;

        [Header("SFX Clips")]
        public AudioClip sfxLevelUp;
        public AudioClip sfxEnemyDie;
        public AudioClip sfxBossRoar;
        public AudioClip sfxPlayerHurt;
        public AudioClip sfxKillStreak;
        public AudioClip sfxVictory;
        public AudioClip sfxGameOver;

        private float _masterVol = 1f;
        private float _sfxVol    = 1f;
        private float _musicVol  = 0.8f;

        // ─────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Auto-create BGM sources if not assigned in Inspector
            if (bgmA == null) bgmA = CreateBGMSource("BGM_A");
            if (bgmB == null) bgmB = CreateBGMSource("BGM_B");

            BuildSfxPool();
        }

        private AudioSource CreateBGMSource(string goName)
        {
            var go  = new GameObject(goName);
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop        = true;
            src.volume      = 0f;
            return src;
        }

        private void Start()
        {
            Core.GameManager.OnGameOver         += () => { PlaySFX(sfxGameOver); StopBGM(); };
            Core.GameManager.OnVictory          += () => { PlaySFX(sfxVictory);  StopBGM(); };
            Core.GameManager.OnLevelUp          += (_) => PlaySFX(sfxLevelUp);
            Core.GameManager.OnKillStreakUpdate += (n)  => { if (n == 10 || n == 25 || n == 50) PlaySFX(sfxKillStreak); };

            var save = Save.SaveSystem.Instance?.CurrentSave;
            if (save != null) SetVolumes(save.masterVolume / 100f, save.sfxVolume / 100f, save.musicVolume / 100f);
            PlayBGM(0);
        }

        // ── SFX ──────────────────────────────────────────────────
        private void BuildSfxPool()
        {
            _sfxPool = new AudioSource[sfxPoolSize];
            for (int i = 0; i < sfxPoolSize; i++)
            {
                var go = new GameObject($"SFX_{i}");
                go.transform.SetParent(transform);
                _sfxPool[i] = go.AddComponent<AudioSource>();
                _sfxPool[i].playOnAwake = false;
            }
        }

        public void PlaySFX(AudioClip clip, float vol = 1f)
        {
            if (clip == null) return;
            var src   = _sfxPool[_sfxIndex++ % sfxPoolSize];
            src.clip  = clip;
            src.volume= _sfxVol * _masterVol * vol;
            src.pitch = Random.Range(0.95f, 1.05f);
            src.Play();
        }

        // ── BGM ──────────────────────────────────────────────────
        public void PlayBGM(int index)
        {
            if (bgmTracks == null || index >= bgmTracks.Length || bgmTracks[index] == null) return;
            var active   = _activeBgm == 0 ? bgmA : bgmB;
            var inactive = _activeBgm == 0 ? bgmB : bgmA;
            if (active == null || inactive == null) return;
            inactive.clip  = bgmTracks[index];
            inactive.volume= 0;
            inactive.loop  = true;
            inactive.Play();
            StartCoroutine(CrossFade(active, inactive, 1.5f));
            _activeBgm = _activeBgm == 0 ? 1 : 0;
        }

        public void SetBossBGM()   => PlayBGM(2);
        public void SetBattleBGM() => PlayBGM(1);
        public void SetCalmBGM()   => PlayBGM(0);

        private System.Collections.IEnumerator CrossFade(AudioSource from, AudioSource to, float dur)
        {
            float start = from.volume, t = 0;
            while (t < dur)
            {
                t += Time.deltaTime;
                float p   = t / dur;
                from.volume = Mathf.Lerp(start, 0, p);
                to.volume   = Mathf.Lerp(0, _musicVol * _masterVol, p);
                yield return null;
            }
            from.Stop();
        }

        private void StopBGM() { bgmA?.Stop(); bgmB?.Stop(); }

        public void SetVolumes(float master, float sfx, float music)
        {
            _masterVol = master; _sfxVol = sfx; _musicVol = music;
            if (masterMixer != null)
            {
                masterMixer.SetFloat("MasterVolume", VolumeToDb(master));
                masterMixer.SetFloat("SFXVolume",    VolumeToDb(sfx));
                masterMixer.SetFloat("MusicVolume",  VolumeToDb(music));
            }
            var active = _activeBgm == 0 ? bgmA : bgmB;
            if (active) active.volume = _musicVol * _masterVol;
        }

        private static float VolumeToDb(float v) =>
            Mathf.Log10(Mathf.Max(0.0001f, v)) * 20f;
    }
}
