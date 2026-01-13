using UnityEngine;
using UnityEngine.UI;

namespace IsaacLike.Net
{
    public class HealthBar : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image fillImage;
        [SerializeField] private Image backgroundImage;

        [Header("Settings")]
        [SerializeField] private Vector3 offset = new Vector3(0, 1f, 0);
        [SerializeField] private bool followTarget = true;
        [SerializeField] private bool alwaysVisible = false;

        [Header("Colors")]
        [SerializeField] private Color highHealthColor = Color.green;
        [SerializeField] private Color mediumHealthColor = Color.yellow;
        [SerializeField] private Color lowHealthColor = Color.red;

        private Transform _target;
        private NetworkHealth _health;
        private Canvas _canvas;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
        }

        private void Start()
        {
            if (!alwaysVisible)
            {
                SetVisible(false);
            }
        }

        public void Initialize(Transform target, NetworkHealth health)
        {
            _target = target;
            _health = health;

            if (_health != null)
            {
                _health.CurrentHp.OnValueChanged += OnHealthChanged;
                UpdateHealthBar(_health.CurrentHp.Value, _health.CurrentHp.Value);
            }
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.CurrentHp.OnValueChanged -= OnHealthChanged;
            }
        }

        private void OnHealthChanged(int previousValue, int newValue)
        {
            UpdateHealthBar(previousValue, newValue);
        }

        private void UpdateHealthBar(int previousValue, int newValue)
        {
            if (_health == null || fillImage == null) return;

            float maxHp = _health.GetMaxHp();
            float fillAmount = newValue / maxHp;

            fillImage.fillAmount = fillAmount;

            if (fillAmount > 0.6f)
            {
                fillImage.color = highHealthColor;
            }
            else if (fillAmount > 0.3f)
            {
                fillImage.color = mediumHealthColor;
            }
            else
            {
                fillImage.color = lowHealthColor;
            }

            if (!alwaysVisible)
            {
                SetVisible(true);
                CancelInvoke(nameof(HideHealthBar));
                Invoke(nameof(HideHealthBar), 2f);
            }
        }

        private void HideHealthBar()
        {
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (fillImage != null)
            {
                fillImage.gameObject.SetActive(visible);
            }

            if (backgroundImage != null)
            {
                backgroundImage.gameObject.SetActive(visible);
            }
        }

        private void LateUpdate()
        {
            if (followTarget && _target != null && _canvas != null)
            {
                Vector3 worldPosition = _target.position + offset;
                transform.position = worldPosition;
            }
        }
    }
}
