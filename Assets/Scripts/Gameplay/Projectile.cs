using UnityEngine;

namespace MeteorShooter
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed = 11f;
        [SerializeField] private float lifetime = 2f;

        private Rigidbody2D body;

        private void Awake() => body = GetComponent<Rigidbody2D>();

        private void OnEnable()
        {
            body.linearVelocity = (Vector2)transform.up * speed;
            Destroy(gameObject, lifetime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Asteroid asteroid = other.GetComponent<Asteroid>();
            if (asteroid != null && asteroid.Hit()) Destroy(gameObject);
            StarHazard star = other.GetComponent<StarHazard>();
            if (star != null && star.Hit()) Destroy(gameObject);
        }
    }
}
