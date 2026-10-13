using System.Collections;
using UnityEngine;

namespace MeteorShooter
{
    public sealed class AsteroidSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject[] asteroidPrefabs;
        [SerializeField] private GameObject starHazardPrefab;
        [SerializeField, Range(0f, 1f)] private float starSpawnChance = 0.24f;
        [SerializeField] private float initialInterval = 1.05f;
        [SerializeField] private float minimumInterval = 0.38f;
        [SerializeField] private float spawnMargin = 1.35f;
        [SerializeField] private float minimumPlayerDistance = 4f;
        [SerializeField] private float targetJitter = 1.6f;
        private Coroutine routine;
        private Camera mainCamera;
        private Transform player;

        private void Awake() => mainCamera = Camera.main;

        private void Start() => routine = StartCoroutine(SpawnRoutine());

        public void StopSpawning()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
        }

        private IEnumerator SpawnRoutine()
        {
            yield return new WaitForSeconds(0.7f);
            while (GameManager.Instance != null && !GameManager.Instance.IsGameOver)
            {
                if (mainCamera == null) mainCamera = Camera.main;
                if (player == null)
                {
                    PlayerController playerController = FindAnyObjectByType<PlayerController>();
                    if (playerController != null) player = playerController.transform;
                }
                if (mainCamera == null || player == null) yield break;

                Rect viewBounds = GetViewBounds(mainCamera);
                Vector2 playerPosition = player.position;
                Vector2 spawnPosition = ChooseSpawnPosition(viewBounds, playerPosition, spawnMargin, minimumPlayerDistance);
                Vector2 target = playerPosition + Random.insideUnitCircle * targetJitter;
                bool spawnStar = starHazardPrefab != null && Random.value < starSpawnChance;
                if (spawnStar)
                {
                    StarHazard star = Instantiate(starHazardPrefab, spawnPosition, Quaternion.identity).GetComponent<StarHazard>();
                    star.SetTravelDirection(target - spawnPosition);
                }
                else if (asteroidPrefabs != null && asteroidPrefabs.Length > 0)
                {
                    GameObject prefab = asteroidPrefabs[Random.Range(0, asteroidPrefabs.Length)];
                    Asteroid asteroid = Instantiate(prefab, spawnPosition, Quaternion.identity).GetComponent<Asteroid>();
                    asteroid.SetTravelDirection(target - spawnPosition);
                }
                else if (starHazardPrefab != null)
                {
                    StarHazard star = Instantiate(starHazardPrefab, spawnPosition, Quaternion.identity).GetComponent<StarHazard>();
                    star.SetTravelDirection(target - spawnPosition);
                }
                else yield break;
                float difficulty = GameManager.Instance.Difficulty;
                float interval = Mathf.Lerp(initialInterval, minimumInterval, difficulty) * Random.Range(0.9f, 1.12f);
                yield return new WaitForSeconds(interval);
            }
        }

        public static Rect GetViewBounds(Camera camera)
        {
            float height = camera.orthographicSize * 2f;
            float width = height * camera.aspect;
            Vector2 center = camera.transform.position;
            return new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height);
        }

        public static Vector2 GetSpawnPosition(Rect viewBounds, int edge, float edgePosition, float margin)
        {
            float t = Mathf.Clamp01(edgePosition);
            switch (edge)
            {
                case 0: return new Vector2(Mathf.Lerp(viewBounds.xMin, viewBounds.xMax, t), viewBounds.yMax + margin);
                case 1: return new Vector2(viewBounds.xMax + margin, Mathf.Lerp(viewBounds.yMin, viewBounds.yMax, t));
                case 2: return new Vector2(Mathf.Lerp(viewBounds.xMin, viewBounds.xMax, t), viewBounds.yMin - margin);
                default: return new Vector2(viewBounds.xMin - margin, Mathf.Lerp(viewBounds.yMin, viewBounds.yMax, t));
            }
        }

        public static Vector2 ChooseSpawnPosition(Rect viewBounds, Vector2 playerPosition, float margin, float minimumDistance)
        {
            for (int attempt = 0; attempt < 16; attempt++)
            {
                Vector2 candidate = GetSpawnPosition(viewBounds, Random.Range(0, 4), Random.value, margin);
                if (Vector2.Distance(candidate, playerPosition) >= minimumDistance) return candidate;
            }

            int fallbackEdge = 0;
            Vector2 fallback = GetSpawnPosition(viewBounds, fallbackEdge, 0.5f, margin);
            for (int edge = 1; edge < 4; edge++)
            {
                Vector2 candidate = GetSpawnPosition(viewBounds, edge, 0.5f, margin);
                if (Vector2.Distance(candidate, playerPosition) > Vector2.Distance(fallback, playerPosition)) fallback = candidate;
            }
            return fallback;
        }
    }
}
