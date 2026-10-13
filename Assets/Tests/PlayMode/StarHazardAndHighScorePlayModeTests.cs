using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MeteorShooter;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class StarHazardAndHighScorePlayModeTests
{
    private const string HighScoreKey = "MeteorShooter.HighScore";
    private bool hadHighScore;
    private int previousHighScore;

    [UnitySetUp]
    public IEnumerator RememberPlayerRecord()
    {
        hadHighScore = PlayerPrefs.HasKey(HighScoreKey);
        previousHighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        PlayerPrefs.SetInt(HighScoreKey, 0);
        PlayerPrefs.Save();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator RestorePlayerRecord()
    {
        if (hadHighScore) PlayerPrefs.SetInt(HighScoreKey, previousHighScore);
        else PlayerPrefs.DeleteKey(HighScoreKey);
        PlayerPrefs.Save();
        Time.timeScale = 1f;
        if (SceneManager.GetActiveScene().name != "MainMenu")
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
        else yield return null;
    }

    [UnityTest]
    public IEnumerator SpawnerCanSelectStarsAndAsteroidsAtTheConfiguredChance()
    {
        yield return LoadGameplay();
        Random.InitState(318);
        AsteroidSpawner spawner = Object.FindAnyObjectByType<AsteroidSpawner>();
        SetField(spawner, "initialInterval", 0.08f);
        SetField(spawner, "minimumInterval", 0.08f);
        SetField(spawner, "starSpawnChance", 0.24f);
        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        SetField(player, "invulnerable", true);

        float timeout = Time.realtimeSinceStartup + 5f;
        StarHazard[] stars = new StarHazard[0];
        Asteroid[] asteroids = new Asteroid[0];
        while ((stars.Length == 0 || asteroids.Length == 0) && Time.realtimeSinceStartup < timeout)
        {
            yield return null;
            stars = Object.FindObjectsByType<StarHazard>();
            asteroids = Object.FindObjectsByType<Asteroid>();
        }

        Assert.Greater(stars.Length, 0, "A star should be selected by the configured chance.");
        Assert.Greater(asteroids.Length, 0, "Asteroids should continue to spawn alongside stars.");
        Assert.IsNotNull(stars[0].GetComponent<SpriteRenderer>().sprite);
        PolygonCollider2D collider = stars[0].GetComponent<PolygonCollider2D>();
        Assert.IsNotNull(collider);
        Assert.IsTrue(collider.isTrigger);
        Assert.AreEqual(10, collider.points.Length, "The hit shape should follow the five-point pixel-star silhouette.");

        GameObject starPrefab = (GameObject)GetField(spawner, "starHazardPrefab");
        Sprite[] variants = (Sprite[])GetField(starPrefab.GetComponent<StarHazard>(), "appearanceVariants");
        Assert.AreEqual(8, variants.Length, "All eight color variants should be assigned to the shared prefab.");
        HashSet<Sprite> observed = new HashSet<Sprite>();
        List<GameObject> samples = new List<GameObject>();
        Random.InitState(7219);
        for (int i = 0; i < 32; i++)
        {
            GameObject sample = Object.Instantiate(starPrefab, new Vector3(i * 4f, 100f, 0f), Quaternion.identity);
            samples.Add(sample);
            observed.Add(sample.GetComponent<SpriteRenderer>().sprite);
            float angularVelocity = Mathf.Abs(sample.GetComponent<Rigidbody2D>().angularVelocity);
            Assert.That(angularVelocity, Is.InRange(35f, 80f));
        }
        Assert.Greater(observed.Count, 1, "Hazards should randomly select their pixel-star colors on spawn.");
        foreach (GameObject sample in samples) Object.Destroy(sample);
    }

    [UnityTest]
    public IEnumerator ProjectileDestroysStarOnceAndStarContactRemovesOneLife()
    {
        yield return LoadGameplay();
        AsteroidSpawner spawner = Object.FindAnyObjectByType<AsteroidSpawner>();
        spawner.StopSpawning();
        GameManager manager = GameManager.Instance;
        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        GameObject starPrefab = (GameObject)GetField(spawner, "starHazardPrefab");
        GameObject bulletPrefab = (GameObject)GetField(player, "projectilePrefab");
        Assert.IsNotNull(starPrefab);
        Assert.IsNotNull(bulletPrefab);

        StarHazard target = Object.Instantiate(starPrefab, new Vector3(0f, -1.4f, 0f), Quaternion.identity).GetComponent<StarHazard>();
        Rigidbody2D starBody = target.GetComponent<Rigidbody2D>();
        starBody.linearVelocity = Vector2.zero;
        starBody.angularVelocity = 0f;
        Projectile bullet = Object.Instantiate(bulletPrefab, target.transform.position + Vector3.down * 0.8f, Quaternion.identity).GetComponent<Projectile>();
        float timeout = Time.realtimeSinceStartup + 1f;
        while (manager.Score == 0 && Time.realtimeSinceStartup < timeout) yield return new WaitForFixedUpdate();
        Assert.AreEqual(GameManager.PointsPerAsteroid, manager.Score, "A projectile should award one star's points once.");
        yield return null;
        Assert.IsTrue(target == null, "The projectile should destroy the star.");
        Assert.IsTrue(bullet == null, "The projectile should be consumed by the star.");

        SetField(player, "invulnerable", false);
        StarHazard touchingStar = Object.Instantiate(starPrefab, player.transform.position, Quaternion.identity).GetComponent<StarHazard>();
        touchingStar.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
        Assert.AreEqual(GameManager.StartingLives - 1, manager.Lives, "Touching a star should remove one life.");
        yield return null;
        Assert.IsTrue(touchingStar == null, "A damaging star is consumed on contact.");

        StarHazard duringInvulnerability = Object.Instantiate(starPrefab, player.transform.position, Quaternion.identity).GetComponent<StarHazard>();
        duringInvulnerability.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
        Assert.AreEqual(GameManager.StartingLives - 1, manager.Lives, "Invulnerability should prevent repeated life loss.");
    }

    [UnityTest]
    public IEnumerator EarnedBestSurvivesPauseToMenuAndMenuDisplaysIt()
    {
        yield return LoadGameplay();
        AsteroidSpawner spawner = Object.FindAnyObjectByType<AsteroidSpawner>();
        spawner.StopSpawning();
        GameObject starPrefab = (GameObject)GetField(spawner, "starHazardPrefab");
        GameManager manager = GameManager.Instance;
        StarHazard first = Object.Instantiate(starPrefab).GetComponent<StarHazard>();
        StarHazard second = Object.Instantiate(starPrefab).GetComponent<StarHazard>();
        Assert.IsTrue(first.Hit());
        Assert.IsTrue(second.Hit());
        Assert.AreEqual(20, manager.Score);
        Assert.AreEqual(20, GameManager.HighScore);

        manager.Pause();
        manager.MainMenu();
        yield return null;
        Text label = GameObject.Find("MainMenuHighScore")?.GetComponent<Text>();
        Assert.IsNotNull(label);
        Assert.AreEqual("HIGH SCORE  0020", label.text);

        yield return LoadGameplay();
        GameManager nextSession = GameManager.Instance;
        nextSession.AddStarScore();
        Assert.AreEqual(20, GameManager.HighScore, "A later lower score must not replace the best.");
        nextSession.MainMenu();
        yield return null;
        label = GameObject.Find("MainMenuHighScore")?.GetComponent<Text>();
        Assert.AreEqual("HIGH SCORE  0020", label.text);
    }

    private static IEnumerator LoadGameplay()
    {
        yield return SceneManager.LoadSceneAsync("Gameplay", LoadSceneMode.Single);
        yield return null;
    }

    private static void SetField(object target, string name, object value)
    {
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }

    private static object GetField(object target, string name)
    {
        return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    }
}
