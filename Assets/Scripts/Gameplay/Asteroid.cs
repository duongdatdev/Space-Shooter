using UnityEngine;

namespace MeteorShooter
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Asteroid : MonoBehaviour
    {
        [SerializeField] private float baseSpeed = 2.2f;
        [SerializeField] private float speedJitter = 0.45f;
        [SerializeField] private float rotationRange = 70f;
        [SerializeField] private float lifetime = 22f;

        private float speed;
        private float rotationSpeed;
        private bool resolved;
        private Rigidbody2D body;

        private void Awake() => body = GetComponent<Rigidbody2D>();

        private void OnEnable()
        {
            float difficulty = GameManager.Instance != null ? GameManager.Instance.Difficulty : 0f;
            speed = baseSpeed + Random.Range(-speedJitter, speedJitter) + difficulty * 2.1f;
            rotationSpeed = Random.Range(-rotationRange, rotationRange);
            body.linearVelocity = Vector2.down * speed;
            body.angularVelocity = rotationSpeed;
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
            GameManager.Instance?.AddAsteroidScore();
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
