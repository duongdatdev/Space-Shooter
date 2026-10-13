using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MeteorShooter
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Text highScoreText;

        private void Start() => RefreshHighScore();

        public void RefreshHighScore()
        {
            if (highScoreText != null) highScoreText.text = $"HIGH SCORE  {GameManager.HighScore:0000}";
        }

        public void Play()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Gameplay");
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
