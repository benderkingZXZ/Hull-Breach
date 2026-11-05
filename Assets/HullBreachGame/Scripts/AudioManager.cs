using System.Collections.Generic;
using HullBreach.Combat;
using HullBreach.Core;
using HullBreach.Data;
using HullBreach.Player;
using HullBreach.RunFlow;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HullBreach.Game
{
    public sealed class AudioManager : MonoBehaviour
    {
        private const int VoiceCount = 8;
        private const float SfxVolume = 0.9f;
        private const float MusicVolume = 0.45f;

        private readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();
        private readonly List<RaycastResult> raycastResults = new List<RaycastResult>(8);
        private AudioSource music;
        private AudioSource[] voices;
        private int nextVoice;
        private string currentMusic;
        private GameStateManager state;
        private ShipRunManager shipRun;
        private PlayerController player;
        private PlayerCombat combat;

        public static AudioManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.volume = MusicVolume;
            voices = new AudioSource[VoiceCount];
            for (int i = 0; i < VoiceCount; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
                voices[i].spatialBlend = 0f;
            }
        }

        private void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    if (touch.phase == TouchPhase.Began) CheckClickSound(touch.position);
                }
            }
            else if (Input.GetMouseButtonDown(0))
            {
                CheckClickSound(Input.mousePosition);
            }
#endif
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Unbind();
        }

        public static void PlaySfx(string key, float volume = 1f, float pitch = 1f)
        {
            if (Instance != null) Instance.PlaySfxInternal(key, volume, pitch);
        }

        public void Bind(GameStateManager gameState, ShipRunManager run, PlayerController playerController, PlayerCombat playerCombat)
        {
            Unbind();
            state = gameState;
            shipRun = run;
            player = playerController;
            combat = playerCombat;

            if (state != null)
            {
                state.StateChanged += OnStateChanged;
                state.SectorVictory += OnSectorVictory;
            }
            if (shipRun != null) shipRun.RoomCleared += OnRoomCleared;
            if (combat != null)
            {
                combat.Attacked += OnAttacked;
                combat.SkillUsed += OnSkillUsed;
            }
            if (player != null && player.Health != null) player.Health.Damaged += OnPlayerDamaged;

            SetMusicInternal(state != null && state.CurrentState == GameState.Combat ? "combat" : "menu");
        }

        private void Unbind()
        {
            if (state != null)
            {
                state.StateChanged -= OnStateChanged;
                state.SectorVictory -= OnSectorVictory;
            }
            if (shipRun != null) shipRun.RoomCleared -= OnRoomCleared;
            if (combat != null)
            {
                combat.Attacked -= OnAttacked;
                combat.SkillUsed -= OnSkillUsed;
            }
            if (player != null && player.Health != null) player.Health.Damaged -= OnPlayerDamaged;
            state = null;
            shipRun = null;
            player = null;
            combat = null;
        }

        private void OnStateChanged(GameState previous, GameState next)
        {
            SetMusicInternal(next == GameState.Combat ? "combat" : "menu");
        }

        private void OnSectorVictory(RunSummary summary)
        {
            PlaySfxInternal("boss_victory", 1f, 1f);
        }

        private void OnRoomCleared(int index)
        {
            if (shipRun == null) return;
            if (index < shipRun.TotalRooms - 1)
            {
                PlaySfxInternal("room_cleared", 1f, 1f);
                return;
            }
            if (shipRun.CurrentShip != null && !shipRun.CurrentShip.IsBossShip) PlaySfxInternal("boss_victory", 1f, 1f);
        }

        private void OnAttacked(Vector2 origin, Vector2 direction, RobotType robot)
        {
            if (robot == RobotType.Techno) PlaySfxInternal("strel_shot", 0.8f, 1f);
            else PlaySfxInternal("samurai_attack", 0.9f, Random.Range(0.95f, 1.05f));
        }

        private void OnSkillUsed(RobotType robot, Vector2 origin)
        {
            if (robot == RobotType.Techno) PlaySfxInternal("strel_shot", 1f, 0.7f);
            else PlaySfxInternal("samurai_attack", 1f, 0.8f);
        }

        private void OnPlayerDamaged(DamageInfo info, float dealt)
        {
            if (dealt > 0f) PlaySfxInternal("player_hurt", 1f, 1f);
        }

        private void CheckClickSound(Vector2 screenPosition)
        {
            EventSystem system = EventSystem.current;
            if (system == null) return;
            PointerEventData data = new PointerEventData(system);
            data.position = screenPosition;
            raycastResults.Clear();
            system.RaycastAll(data, raycastResults);
            if (raycastResults.Count == 0) return;
            if (ExecuteEvents.GetEventHandler<IPointerClickHandler>(raycastResults[0].gameObject) != null)
                PlaySfxInternal("ui_click", 0.9f, 1f);
        }

        private void PlaySfxInternal(string key, float volume, float pitch)
        {
            if (voices == null || voices.Length == 0) return;
            AudioClip clip = LoadClip("Audio/Sfx/" + key);
            if (clip == null) return;
            AudioSource voice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            voice.clip = clip;
            voice.volume = SfxVolume * Mathf.Clamp01(volume);
            voice.pitch = pitch;
            voice.Play();
        }

        private void SetMusicInternal(string key)
        {
            if (music == null || currentMusic == key) return;
            currentMusic = key;
            AudioClip clip = LoadClip("Audio/Music/" + key);
            music.Stop();
            music.clip = clip;
            if (clip != null) music.Play();
        }

        private AudioClip LoadClip(string path)
        {
            AudioClip clip;
            if (clipCache.TryGetValue(path, out clip)) return clip;
            clip = Resources.Load<AudioClip>(path);
            if (clip == null) Debug.LogWarning("[HullBreach] Нет звука: Resources/" + path);
            clipCache[path] = clip;
            return clip;
        }
    }
}
