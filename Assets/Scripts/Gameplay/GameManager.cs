using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MeteorShooter
{
    public sealed class GameManager : MonoBehaviour
    {
        public const int StartingLives = 3;
        public const int PointsPerAsteroid = 10;
        private const string HighScoreKey = "MeteorShooter.HighScore";

        [SerializeField] private Text scoreText;
        [SerializeField] private Text livesText;
        [SerializeField] private Text finalScoreText;
        [SerializeField] private Text highScoreText;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private AsteroidSpawner spawner;
        [SerializeField] private GameObject asteroidExplosionPrefab;
        [SerializeField] private GameObject playerExplosionPrefab;
        [SerializeField] private CameraShake cameraShake;

        public static GameManager Instance { get; private set; }
        public int Score { get; private set; }
        public int Lives { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsPaused { get; private set; }
        public float ElapsedTime { get; private set; }
        public float Difficulty => CalculateDifficulty(Score, ElapsedTime);
        public static int HighScore => PlayerPrefs.GetInt(HighScoreKey, 0);

        private void Awake() => InitializeSession();

        public void InitializeSession()
        {
            Instance = this;
            Time.timeScale = 1f;
            Score = 0;
            Lives = StartingLives;
            IsGameOver = false;
            IsPaused = false;
            UpdateHud();
            if (pausePanel != null) pausePanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
        }

        private void Update()
        {
            if (!IsGameOver && !IsPaused) ElapsedTime += Time.deltaTime;
            if (!IsGameOver && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (IsPaused) Resume(); else Pause();
            }
        }

        public static float CalculateDifficulty(int score, float elapsedSeconds)
        {
            return Mathf.Clamp01((score / 650f) + (elapsedSeconds / 120f));
        }

        public void AddAsteroidScore()
        {
            if (IsGameOver) return;
            AddScore(PointsPerAsteroid);
        }

        public void AddStarScore()
        {
            if (IsGameOver) return;
            AddScore(PointsPerAsteroid);
        }

        public bool LoseLife()
        {
            if (IsGameOver) return false;
            Lives = Mathf.Max(0, Lives - 1);
            UpdateHud();
            cameraShake?.Shake(0.14f, 0.11f);
            return Lives == 0;
        }

        public void TriggerGameOver(Vector3 playerPosition)
        {
            if (IsGameOver) return;
            IsGameOver = true;
            spawner?.StopSpawning();
            foreach (Asteroid asteroid in FindObjectsByType<Asteroid>()) Destroy(asteroid.gameObject);
            foreach (StarHazard star in FindObjectsByType<StarHazard>()) Destroy(star.gameObject);
            foreach (Projectile projectile in FindObjectsByType<Projectile>()) Destroy(projectile.gameObject);
            cameraShake?.Shake(0.35f, 0.2f);
            if (playerExplosionPrefab != null) Instantiate(playerExplosionPrefab, playerPosition, Quaternion.identity);

            SaveHighScore();
            int highScore = HighScore;
            PlayerPrefs.Save();
            if (finalScoreText != null) finalScoreText.text = $"SCORE  {Score:0000}";
            if (highScoreText != null) highScoreText.text = $"HIGH SCORE  {highScore:0000}";
            StartCoroutine(ShowGameOver());
        }

        public void SpawnAsteroidExplosion(Vector3 position)
        {
            if (asteroidExplosionPrefab != null) Instantiate(asteroidExplosionPrefab, position, Quaternion.identity);
        }

        public void Pause()
        {
            if (IsPaused || IsGameOver) return;
            IsPaused = true;
            if (pausePanel != null) pausePanel.SetActive(true);
            Time.timeScale = 0f;
        }

        public void Resume()
        {
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = 1f;
            if (pausePanel != null) pausePanel.SetActive(false);
        }

        public void Restart()
        {
            PersistHighScore();
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void MainMenu()
        {
            PersistHighScore();
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        private IEnumerator ShowGameOver()
        {
            yield return new WaitForSecondsRealtime(0.75f);
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
        }

        private void UpdateHud()
        {
            if (scoreText != null) scoreText.text = $"SCORE  {Score:0000}";
            if (livesText != null) livesText.text = "LIVES  " + new string('◆', Lives);
        }

        private void AddScore(int points)
        {
            Score += points;
            UpdateHud();
            SaveHighScore();
        }

        private void SaveHighScore()
        {
            if (Score <= HighScore) return;
            PlayerPrefs.SetInt(HighScoreKey, Score);
        }

        private void PersistHighScore() => PlayerPrefs.Save();

        private void OnApplicationQuit() => PersistHighScore();

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }
    }
}
