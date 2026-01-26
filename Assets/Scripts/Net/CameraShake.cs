using UnityEngine;

namespace IsaacLike.Net
{
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float maxShakeDuration = 1f;
        [SerializeField] private float maxShakeMagnitude = 0.5f;

        private Vector3 _originalPosition;
        private float _shakeTimeRemaining;
        private float _shakeMagnitude;
        private Camera _camera;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _camera = GetComponent<Camera>();
        }

        private void Start()
        {
            _originalPosition = transform.localPosition;
        }

        private void LateUpdate()
        {
            if (_shakeTimeRemaining > 0)
            {
                transform.localPosition = _originalPosition + Random.insideUnitSphere * _shakeMagnitude;
                _shakeTimeRemaining -= Time.deltaTime;

                if (_shakeTimeRemaining <= 0)
                {
                    _shakeTimeRemaining = 0;
                    transform.localPosition = _originalPosition;
                }
            }
        }

        public void Shake(float duration, float magnitude)
        {
            _shakeTimeRemaining = Mathf.Min(duration, maxShakeDuration);
            _shakeMagnitude = Mathf.Min(magnitude, maxShakeMagnitude);
        }

        public void ShakeSmall()
        {
            Shake(0.1f, 0.05f);
        }

        public void ShakeMedium()
        {
            Shake(0.2f, 0.15f);
        }

        public void ShakeLarge()
        {
            Shake(0.3f, 0.3f);
        }
    }
}
