using UnityEngine;

namespace HullBreach.Game
{
    /// <summary>Камера следует за игроком и не выходит за границы текущей комнаты (если они заданы).</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private float smoothing = 12f;

        private Camera cam;
        private Transform target;
        private Bounds bounds;
        private bool hasBounds;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void SetBounds(Bounds newBounds)
        {
            bounds = newBounds;
            hasBounds = true;
        }

        public void ClearBounds()
        {
            hasBounds = false;
        }

        /// <summary>Мгновенно перенести камеру к цели (при входе в комнату).</summary>
        public void Snap()
        {
            if (target == null) return;
            transform.position = Clamp(target.position);
        }

        private void LateUpdate()
        {
            if (target == null || !target.gameObject.activeInHierarchy) return;
            Vector3 desired = Clamp(target.position);
            float k = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, k);
        }

        private Vector3 Clamp(Vector3 p)
        {
            if (cam == null) cam = GetComponent<Camera>();
            float z = transform.position.z;
            if (!hasBounds || cam == null) return new Vector3(p.x, p.y, z);

            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            float minX = bounds.min.x + halfW;
            float maxX = bounds.max.x - halfW;
            float minY = bounds.min.y + halfH;
            float maxY = bounds.max.y - halfH;
            float x = minX > maxX ? bounds.center.x : Mathf.Clamp(p.x, minX, maxX);
            float y = minY > maxY ? bounds.center.y : Mathf.Clamp(p.y, minY, maxY);
            return new Vector3(x, y, z);
        }
    }
}
