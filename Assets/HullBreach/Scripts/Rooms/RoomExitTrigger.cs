using UnityEngine;
using HullBreach.Player;

namespace HullBreach.Rooms
{
    [RequireComponent(typeof(Collider2D))]
    public class RoomExitTrigger : MonoBehaviour
    {
        [SerializeField] private RoomController room;

        private Collider2D trigger;
        private bool armed;

        private void Reset()
        {
            Collider2D c = GetComponent<Collider2D>();
            if (c != null) c.isTrigger = true;
        }

        public void Configure(RoomController owner)
        {
            room = owner;
        }

        private void Awake()
        {
            trigger = GetComponent<Collider2D>();
            trigger.isTrigger = true;
            if (room == null) room = GetComponentInParent<RoomController>();
            trigger.enabled = armed;
        }

        public void SetArmed(bool value)
        {
            armed = value;
            if (trigger == null) trigger = GetComponent<Collider2D>();
            if (trigger != null) trigger.enabled = value;
        }

        private void OnTriggerEnter2D(Collider2D other) { TryNotify(other); }
        private void OnTriggerStay2D(Collider2D other) { TryNotify(other); }

        private void TryNotify(Collider2D other)
        {
            if (!armed || room == null) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;
            room.NotifyExitReached();
        }
    }
}
