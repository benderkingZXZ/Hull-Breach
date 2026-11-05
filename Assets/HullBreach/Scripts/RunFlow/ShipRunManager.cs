using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HullBreach.Combat;
using HullBreach.Core;
using HullBreach.Data;
using HullBreach.Enemies;
using HullBreach.Player;
using HullBreach.Rooms;

namespace HullBreach.RunFlow
{
    public class ShipRunManager : MonoBehaviour
    {
        [SerializeField] private RoomGenerator roomGenerator;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private PlayerController player;
        [Tooltip("Пауза между касанием выхода и появлением в следующей комнате (для UI-затемнения).")]
        [SerializeField] private float roomTransitionDelay = 0.6f;
        [Tooltip("Пауза между уничтожением мини-босса/реактора и завершением корабля.")]
        [SerializeField] private float shipCompleteDelay = 1.5f;
        [Tooltip("0 — случайный seed на каждый корабль; иначе фиксированный (для отладки).")]
        [SerializeField] private int fixedSeed = 0;

        private readonly List<RoomController> rooms = new List<RoomController>();
        private System.Random rng;
        private Coroutine routine;
        private EntryPointProfile profile;
        private bool transitioning;
        private bool completing;
        private bool playerHooked;
        private float startTime;
        private int roomsCleared;
        private int enemiesKilled;

        public event Action<ShipDefinition, EntryPointType> ShipStarted;
        public event Action<int, int, RoomController> RoomEntered;
        public event Action<int> RoomCleared;
        public event Action<EnemyController> EnemyKilled;
        public event Action<int, Vector3> ResourcesEarned;
        public event Action<ShipCompletionResult> ShipCompleted;
        public event Action<ShipFailureInfo> ShipFailed;

        public bool IsRunning { get; private set; }
        public ShipDefinition CurrentShip { get; private set; }
        public EntryPointType CurrentEntry { get; private set; }
        public int CurrentRoomIndex { get; private set; }
        public int TotalRooms { get { return rooms.Count; } }
        public int LastSeed { get; private set; }
        public RoomController CurrentRoom
        {
            get { return (CurrentRoomIndex >= 0 && CurrentRoomIndex < rooms.Count) ? rooms[CurrentRoomIndex] : null; }
        }
        public int EnemiesRemaining { get { return CurrentRoom != null ? CurrentRoom.EnemiesAlive : 0; } }

        public void Configure(RoomGenerator generator, EnemySpawner spawner, PlayerController playerController)
        {
            roomGenerator = generator;
            enemySpawner = spawner;
            player = playerController;
        }

        public bool StartShip(ShipDefinition ship, EntryPointType entry)
        {
            if (ship == null || roomGenerator == null || enemySpawner == null || player == null)
            {
                Debug.LogError("[HullBreach] ShipRunManager: не заданы ship/roomGenerator/enemySpawner/player.", this);
                return false;
            }
            if (IsRunning || rooms.Count > 0) AbortShip();

            int seed = fixedSeed != 0 ? fixedSeed : Environment.TickCount;
            RoomLayout layout = roomGenerator.GenerateLayout(ship, entry, seed);
            string error;
            if (!roomGenerator.ValidateLayout(layout, out error))
            {
                Debug.LogError("[HullBreach] Генерация корабля не прошла проверку: " + error, this);
                return false;
            }

            rooms.AddRange(roomGenerator.BuildRooms(layout));
            profile = EntryPointCatalog.Get(entry);
            rng = new System.Random(seed);
            LastSeed = seed;
            CurrentShip = ship;
            CurrentEntry = entry;
            CurrentRoomIndex = -1;
            roomsCleared = 0;
            enemiesKilled = 0;
            transitioning = false;
            completing = false;
            startTime = Time.time;
            IsRunning = true;

            player.Health.Died += OnPlayerDied;
            playerHooked = true;
            player.SetInputEnabled(true);

            ShipStarted?.Invoke(ship, entry);
            EnterRoom(0);
            return true;
        }

        public void AbortShip()
        {
            IsRunning = false;
            Cleanup();
        }

        public void Cleanup()
        {
            if (routine != null) { StopCoroutine(routine); routine = null; }
            transitioning = false;
            completing = false;
            UnhookPlayer();
            for (int i = 0; i < rooms.Count; i++) SubscribeRoom(rooms[i], false);
            rooms.Clear();
            CurrentRoomIndex = -1;
            if (roomGenerator != null) roomGenerator.ClearRooms();
        }

        private void UnhookPlayer()
        {
            if (playerHooked && player != null) player.Health.Died -= OnPlayerDied;
            playerHooked = false;
        }

        private void SubscribeRoom(RoomController room, bool subscribe)
        {
            if (room == null) return;
            if (subscribe)
            {
                room.Cleared += HandleRoomCleared;
                room.ExitReached += HandleExitReached;
                room.EnemyKilled += HandleEnemyKilled;
            }
            else
            {
                room.Cleared -= HandleRoomCleared;
                room.ExitReached -= HandleExitReached;
                room.EnemyKilled -= HandleEnemyKilled;
            }
        }

        private void EnterRoom(int index)
        {
            if (index < 0 || index >= rooms.Count)
            {
                Debug.LogError("[HullBreach] ShipRunManager: комнаты с индексом " + index + " нет.", this);
                return;
            }

            CurrentRoomIndex = index;
            RoomController room = rooms[index];
            room.gameObject.SetActive(true);
            player.TeleportTo(room.PlayerSpawnPosition);
            SubscribeRoom(room, true);

            room.Enter();
            RoomEntered?.Invoke(index, rooms.Count, room);

            RoomSpawnContext ctx = new RoomSpawnContext(CurrentShip, profile, index, rooms.Count, room.Role == RoomRole.Reactor);
            int spawned = enemySpawner.SpawnForRoom(room, ctx, player.transform, rng);
            if (spawned == 0) Debug.LogWarning("[HullBreach] В комнате " + index + " не создано ни одного врага (проверьте префабы EnemySpawner).", this);
            room.BeginCombat();
        }

        private void HandleEnemyKilled(EnemyController enemy)
        {
            if (!IsRunning) return;
            enemiesKilled++;
            if (enemy.ResourceReward > 0) ResourcesEarned?.Invoke(enemy.ResourceReward, enemy.transform.position);
            EnemyKilled?.Invoke(enemy);
        }

        private void HandleRoomCleared(RoomController room)
        {
            if (!IsRunning || room != CurrentRoom) return;
            roomsCleared++;
            RoomCleared?.Invoke(room.Index);

            if (room.Role == RoomRole.Reactor && !completing)
            {
                completing = true;
                routine = StartCoroutine(CompleteShipRoutine());
            }
        }

        private void HandleExitReached(RoomController room)
        {
            if (!IsRunning || transitioning || completing || room != CurrentRoom) return;
            if (CurrentRoomIndex >= rooms.Count - 1) return;
            routine = StartCoroutine(AdvanceRoutine(CurrentRoomIndex + 1));
        }

        private IEnumerator AdvanceRoutine(int nextIndex)
        {
            transitioning = true;
            player.SetInputEnabled(false);
            if (roomTransitionDelay > 0f) yield return new WaitForSeconds(roomTransitionDelay);

            RoomController previous = CurrentRoom;
            SubscribeRoom(previous, false);
            EnterRoom(nextIndex);
            if (previous != null) previous.gameObject.SetActive(false);

            player.SetInputEnabled(true);
            transitioning = false;
            routine = null;
        }

        private IEnumerator CompleteShipRoutine()
        {
            if (shipCompleteDelay > 0f) yield return new WaitForSeconds(shipCompleteDelay);
            routine = null;
            FinishShip();
        }

        private void FinishShip()
        {
            if (!IsRunning) return;
            IsRunning = false;
            UnhookPlayer();
            SubscribeRoom(CurrentRoom, false);
            player.SetInputEnabled(false);

            int reward = Mathf.RoundToInt(CurrentShip.BaseResourceReward * profile.ResourceMultiplier);
            ShipCompletionResult result = new ShipCompletionResult(CurrentShip, CurrentEntry, reward,
                roomsCleared, enemiesKilled, Time.time - startTime);
            ShipCompleted?.Invoke(result);
        }

        private void OnPlayerDied(Health h, DamageInfo info)
        {
            if (!IsRunning || completing) return;
            IsRunning = false;
            if (routine != null) { StopCoroutine(routine); routine = null; }
            transitioning = false;
            UnhookPlayer();
            SubscribeRoom(CurrentRoom, false);
            player.SetInputEnabled(false);

            ShipFailureInfo failure = new ShipFailureInfo(CurrentShip, CurrentEntry, CurrentRoomIndex, roomsCleared, enemiesKilled);
            ShipFailed?.Invoke(failure);
        }
    }
}
