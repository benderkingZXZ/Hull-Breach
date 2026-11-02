using HullBreach.Rooms;
using UnityEngine;

namespace HullBreach.Game
{
    /// <summary>Реактор в финальной комнате: пульсирует, пока мини-босс жив, и гаснет после зачистки комнаты.</summary>
    public sealed class ReactorVisual : MonoBehaviour
    {
        private RoomController room;
        private SpriteRenderer core;
        private SpriteRenderer halo;
        private bool destroyed;
        private Vector3 baseScale = Vector3.one;

        public static ReactorVisual Attach(GameObject reactorObject, SpriteRenderer coreRenderer, SpriteRenderer haloRenderer)
        {
            ReactorVisual v = reactorObject.AddComponent<ReactorVisual>();
            v.core = coreRenderer;
            v.halo = haloRenderer;
            v.baseScale = reactorObject.transform.localScale;
            return v;
        }

        private void OnEnable()
        {
            room = GetComponentInParent<RoomController>();
            if (room != null) room.Cleared += OnRoomCleared;
            destroyed = false;
        }

        private void OnDisable()
        {
            if (room != null) room.Cleared -= OnRoomCleared;
        }

        private void OnRoomCleared(RoomController cleared)
        {
            destroyed = true;
            if (core != null) core.color = new Color(0.2f, 0.2f, 0.24f, 1f);
            if (halo != null) halo.color = new Color(0.2f, 0.2f, 0.24f, 0.2f);
            OneShotFx.Spawn(RuntimeSprites.Ring, transform.position, 0f, new Color(0.4f, 1f, 0.8f, 0.9f),
                new Vector2(2f, 2f), new Vector2(14f, 14f), 0.8f);
        }

        private void Update()
        {
            if (destroyed) return;
            float pulse = 1f + 0.08f * Mathf.Sin(Time.time * 5f);
            transform.localScale = baseScale * pulse;
        }
    }
}
