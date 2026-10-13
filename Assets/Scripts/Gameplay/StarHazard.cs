using UnityEngine;

namespace MeteorShooter
{
    [RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
    public sealed class StarHazard : MonoBehaviour
    {
        [SerializeField] private float baseSpeed = 2.6f;
        [SerializeField] private float speedJitter = 0.4f;
        [SerializeField] private float minimumRotationSpeed = 35f;
        [SerializeField] private float maximumRotationSpeed = 80f;
        [SerializeField] private float lifetime = 22f;
        [SerializeField] private Sprite[] appearanceVariants;

        private float speed;
        private bool resolved;
        private Rigidbody2D body;
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            float difficulty = GameManager.Instance != null ? GameManager.Instance.Difficulty : 0f;
            speed = baseSpeed + Random.Range(-speedJitter, speedJitter) + difficulty * 1.8f;
            if (appearanceVariants != null && appearanceVariants.Length > 0)
                spriteRenderer.sprite = appearanceVariants[Random.Range(0, appearanceVariants.Length)];
            body.linearVelocity = Vector2.down * speed;
            float rotationSpeed = Random.Range(minimumRotationSpeed, maximumRotationSpeed);
            body.angularVelocity = Random.value < 0.5f ? -rotationSpeed : rotationSpeed;
            Destroy(gameObject, lifetime);
        }

        public void SetTravelDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.0001f) body.linearVelocity = direction.normalized * speed;
        }

        public bool Hit()
        {
            if (resolved) return false;
            resolved = true;
            GameManager.Instance?.AddStarScore();
            GameManager.Instance?.SpawnAsteroidExplosion(transform.position);
            Destroy(gameObject);
            return true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (resolved) return;
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && player.TryTakeHit())
            {
                resolved = true;
                GameManager.Instance?.SpawnAsteroidExplosion(transform.position);
                Destroy(gameObject);
            }
        }
    }
}
