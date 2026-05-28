using UnityEngine;

namespace AngelArena.Core
{
    /// <summary>
    /// Smooth camera follow with screen shake support.
    /// Attach to Main Camera.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance { get; private set; }

        [Header("Follow")]
        public Transform target;
        [Range(1f, 20f)] public float followSpeed = 8f;
        public Vector2 deadzone = new Vector2(0.5f, 0.5f);

        [Header("Bounds")]
        public bool clampToBounds = true;
        private float _halfCamW, _halfCamH;

        [Header("Shake")]
        private float _shakeIntensity;
        private float _shakeUntil;
        private Vector3 _basePos;

        // ─────────────────────────────────────────────────────────
        private void Awake()
        {
            Instance  = this;
            var cam   = GetComponent<Camera>();
            _halfCamH = cam.orthographicSize;
            _halfCamW = _halfCamH * cam.aspect;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Smooth follow
            Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);

            // Clamp to world bounds
            if (clampToBounds)
            {
                float hw = GameConstants.WORLD_W / 2f;
                float hh = GameConstants.WORLD_H / 2f;
                transform.position = new Vector3(
                    Mathf.Clamp(transform.position.x, -hw + _halfCamW, hw - _halfCamW),
                    Mathf.Clamp(transform.position.y, -hh + _halfCamH, hh - _halfCamH),
                    transform.position.z);
            }

            _basePos = transform.position;

            // Screen shake
            if (Time.time < _shakeUntil)
            {
                transform.position = _basePos + (Vector3)Random.insideUnitCircle * _shakeIntensity;
            }
        }

        public void Shake(float intensity, float duration)
        {
            _shakeIntensity = intensity;
            _shakeUntil     = Time.time + duration;
        }
    }
}
