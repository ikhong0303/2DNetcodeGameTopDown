using UnityEngine;
using TMPro;

namespace IsaacLike.Net
{
    public class DamageNumber : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float lifetime = 1f;
        [SerializeField] private float floatSpeed = 2f;
        [SerializeField] private float fadeSpeed = 1f;

        private TMP_Text _text;
        private float _timer;
        private Color _originalColor;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            if (_text != null)
            {
                _originalColor = _text.color;
            }
        }

        public void Initialize(int damage, Color color)
        {
            if (_text != null)
            {
                _text.text = damage.ToString();
                _text.color = color;
                _originalColor = color;
            }
        }

        private void Update()
        {
            _timer += Time.deltaTime;

            transform.position += Vector3.up * floatSpeed * Time.deltaTime;

            if (_text != null)
            {
                float alpha = Mathf.Lerp(_originalColor.a, 0, _timer / lifetime);
                _text.color = new Color(_originalColor.r, _originalColor.g, _originalColor.b, alpha);
            }

            if (_timer >= lifetime)
            {
                Destroy(gameObject);
            }
        }
    }

    public class DamageNumberManager : MonoBehaviour
    {
        public static DamageNumberManager Instance { get; private set; }

        [Header("Prefab")]
        [SerializeField] private GameObject damageNumberPrefab;

        [Header("Colors")]
        [SerializeField] private Color playerDamageColor = Color.red;
        [SerializeField] private Color enemyDamageColor = Color.yellow;
        [SerializeField] private Color criticalDamageColor = new Color(1f, 0.5f, 0f);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void ShowDamage(Vector3 position, int damage, bool isPlayer = false, bool isCritical = false)
        {
            if (damageNumberPrefab == null)
            {
                return;
            }

            GameObject numObj = Instantiate(damageNumberPrefab, position, Quaternion.identity);
            DamageNumber damageNum = numObj.GetComponent<DamageNumber>();

            if (damageNum != null)
            {
                Color color = isCritical ? criticalDamageColor : (isPlayer ? playerDamageColor : enemyDamageColor);
                damageNum.Initialize(damage, color);
            }
        }
    }
}
