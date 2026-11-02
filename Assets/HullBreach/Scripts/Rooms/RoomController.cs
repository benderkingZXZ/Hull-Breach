using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using HullBreach.Core;
using HullBreach.Enemies;

namespace HullBreach.Rooms
{
    /// <summary>
    /// Компонент корня префаба-шаблона комнаты. Хранит точки спавна, двери и выход, отслеживает зачистку.
    /// Жизненный цикл: Initialize → Enter (двери закрыты) → RegisterEnemy... → BeginCombat → Cleared (двери открыты).
    /// </summary>
    public class RoomController : MonoBehaviour
    {
        [SerializeField] private RoomRole role = RoomRole.Normal;
        [Tooltip("Куда ставится игрок при входе в комнату (обязательно).")]
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private Transform[] enemySpawnPoints;
        [Tooltip("Только для комнаты реактора: где появляется мини-босс (иначе первая точка спавна).")]
        [SerializeField] private Transform bossSpawnPoint;
        [Tooltip("Объекты-преграды (двери/щиты). Активны, пока в комнате есть враги; отключаются при зачистке.")]
        [SerializeField] private GameObject[] doorBlockers;
        [Tooltip("Триггер выхода в следующую комнату (обязателен для Normal, не нужен для Reactor).")]
        [SerializeField] private RoomExitTrigger exitTrigger;
        [Tooltip("Реактор (визуал). Для художников: вызывается, когда мини-босс повержен.")]
        [SerializeField] private GameObject reactorObject;
        [SerializeField] private UnityEvent reactorDestroyed;
        [Header("Границы (для камеры UI/интеграции)")]
        [SerializeField] private Vector2 boundsSize = new Vector2(20f, 12f);
        [SerializeField] private Vector2 boundsOffset = Vector2.zero;

        private bool combatBegun;
        private bool exitNotified;

        public event Action<RoomController> Entered;
        public event Action<RoomController> Cleared;
        public event Action<RoomController> ExitReached;
        public event Action<EnemyController> EnemyKilled;
        /// <summary>true — двери открыты.</summary>
        public event Action<bool> DoorsStateChanged;

        public int Index { get; private set; }
        public RoomRole Role { get { return role; } }
        public RoomState State { get; private set; } = RoomState.Inactive;
        public int EnemiesAlive { get; private set; }
        public GameObject ReactorObject { get { return reactorObject; } }
        public IReadOnlyList<Transform> EnemySpawnPoints { get { return enemySpawnPoints; } }
        public Vector3 PlayerSpawnPosition { get { return playerSpawnPoint != null ? playerSpawnPoint.position : transform.position; } }
        public Vector3 BossSpawnPosition
        {
            get
            {
                if (bossSpawnPoint != null) return bossSpawnPoint.position;
                if (enemySpawnPoints != null && enemySpawnPoints.Length > 0 && enemySpawnPoints[0] != null) return enemySpawnPoints[0].position;
                return transform.position;
            }
        }
        public Bounds WorldBounds
        {
            get { return new Bounds(transform.position + (Vector3)boundsOffset, new Vector3(boundsSize.x, boundsSize.y, 1f)); }
        }

        /// <summary>Программная настройка вместо инспектора (процедурные шаблоны комнат).</summary>
        public void Configure(RoomRole newRole, Transform newPlayerSpawn, Transform[] newEnemySpawns, Transform newBossSpawn,
            GameObject[] newDoorBlockers, RoomExitTrigger newExitTrigger, GameObject newReactor, Vector2 newBoundsSize)
        {
            role = newRole;
            playerSpawnPoint = newPlayerSpawn;
            enemySpawnPoints = newEnemySpawns;
            bossSpawnPoint = newBossSpawn;
            doorBlockers = newDoorBlockers;
            exitTrigger = newExitTrigger;
            reactorObject = newReactor;
            boundsSize = newBoundsSize;
        }

        /// <summary>Вызывается генератором сразу после создания комнаты.</summary>
        public void Initialize(int index)
        {
            Index = index;
            State = RoomState.Inactive;
            EnemiesAlive = 0;
            combatBegun = false;
            exitNotified = false;
            SetDoorsOpen(false);
        }

        /// <summary>Игрок вошёл: комната становится активной, двери закрываются.</summary>
        public void Enter()
        {
            if (State != RoomState.Inactive) return;
            State = RoomState.Active;
            SetDoorsOpen(false);
            Entered?.Invoke(this);
        }

        public void RegisterEnemy(EnemyController enemy)
        {
            if (enemy == null) return;
            EnemiesAlive++;
            enemy.Died += HandleEnemyDied;
        }

        /// <summary>Вызывается после спавна всех врагов. Если врагов нет — комната сразу зачищена.</summary>
        public void BeginCombat()
        {
            combatBegun = true;
            if (State == RoomState.Active && EnemiesAlive <= 0) Clear();
        }

        private void HandleEnemyDied(EnemyController enemy)
        {
            enemy.Died -= HandleEnemyDied;
            EnemiesAlive = Mathf.Max(0, EnemiesAlive - 1);
            EnemyKilled?.Invoke(enemy);
            if (combatBegun && State == RoomState.Active && EnemiesAlive <= 0) Clear();
        }

        private void Clear()
        {
            if (State == RoomState.Cleared) return;
            State = RoomState.Cleared;
            SetDoorsOpen(true);
            if (role == RoomRole.Reactor && reactorDestroyed != null) reactorDestroyed.Invoke();
            Cleared?.Invoke(this);
        }

        private void SetDoorsOpen(bool open)
        {
            if (doorBlockers != null)
            {
                for (int i = 0; i < doorBlockers.Length; i++)
                    if (doorBlockers[i] != null) doorBlockers[i].SetActive(!open);
            }
            if (exitTrigger != null) exitTrigger.SetArmed(open && role == RoomRole.Normal);
            DoorsStateChanged?.Invoke(open);
        }

        /// <summary>Вызывается RoomExitTrigger, когда игрок вошёл в открытый выход.</summary>
        public void NotifyExitReached()
        {
            if (State != RoomState.Cleared || exitNotified || role != RoomRole.Normal) return;
            exitNotified = true;
            ExitReached?.Invoke(this);
        }

        /// <summary>Проверка корректности настройки префаба (используется генератором и из контекстного меню).</summary>
        public bool Validate(out string error)
        {
            if (playerSpawnPoint == null) { error = name + ": не задан playerSpawnPoint"; return false; }
            if (role == RoomRole.Normal)
            {
                if (exitTrigger == null) { error = name + ": у обычной комнаты нет exitTrigger"; return false; }
                if (enemySpawnPoints == null || enemySpawnPoints.Length == 0) { error = name + ": нет точек спавна врагов"; return false; }
            }
            else
            {
                bool hasBossPoint = bossSpawnPoint != null || (enemySpawnPoints != null && enemySpawnPoints.Length > 0);
                if (!hasBossPoint) { error = name + ": у комнаты реактора нет bossSpawnPoint/точек спавна"; return false; }
            }
            error = null;
            return true;
        }

        [ContextMenu("Validate Room Setup")]
        private void ValidateFromMenu()
        {
            string error;
            if (Validate(out error)) Debug.Log("[HullBreach] " + name + ": настройка комнаты корректна.", this);
            else Debug.LogError("[HullBreach] " + error, this);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position + (Vector3)boundsOffset, new Vector3(boundsSize.x, boundsSize.y, 0.1f));
        }
    }
}
