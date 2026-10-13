using MeteorShooter;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

public class MeteorShooterTests
{
    [Test]
    public void DifficultyStartsAtZero() => Assert.AreEqual(0f, GameManager.CalculateDifficulty(0, 0f));

    [Test]
    public void DifficultyIncreasesWithScoreAndTime()
    {
        float early = GameManager.CalculateDifficulty(50, 10f);
        float later = GameManager.CalculateDifficulty(300, 60f);
        Assert.Greater(later, early);
    }

    [Test]
    public void DifficultyIsClamped() => Assert.AreEqual(1f, GameManager.CalculateDifficulty(5000, 500f));

    [Test]
    public void NewSessionStartsWithThreeLivesAndZeroScore()
    {
        GameObject go = new GameObject("GameManagerTest");
        GameManager manager = go.AddComponent<GameManager>();
        manager.InitializeSession();
        Assert.AreEqual(GameManager.StartingLives, manager.Lives);
        Assert.AreEqual(0, manager.Score);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void ScoreIncrementsOncePerAward()
    {
        const string key = "MeteorShooter.HighScore";
        bool hadValue = PlayerPrefs.HasKey(key);
        int oldValue = PlayerPrefs.GetInt(key, 0);
        GameObject go = null;
        try
        {
            PlayerPrefs.SetInt(key, 0);
            go = new GameObject("GameManagerTest");
            GameManager manager = go.AddComponent<GameManager>();
            manager.AddAsteroidScore();
            Assert.AreEqual(GameManager.PointsPerAsteroid, manager.Score);
        }
        finally
        {
            if (go != null) Object.DestroyImmediate(go);
            if (hadValue) PlayerPrefs.SetInt(key, oldValue); else PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }

    [Test]
    public void StarHazardAwardsSamePointsAndUpdatesPersistentBest()
    {
        const string key = "MeteorShooter.HighScore";
        bool hadValue = PlayerPrefs.HasKey(key);
        int oldValue = PlayerPrefs.GetInt(key, 0);
        GameObject managerObject = null;
        try
        {
            PlayerPrefs.SetInt(key, 0);
            managerObject = new GameObject("StarScoreManagerTest");
            GameManager manager = managerObject.AddComponent<GameManager>();
            manager.AddStarScore();
            Assert.AreEqual(GameManager.PointsPerAsteroid, manager.Score);
            Assert.AreEqual(GameManager.PointsPerAsteroid, GameManager.HighScore);
        }
        finally
        {
            if (managerObject != null) Object.DestroyImmediate(managerObject);
            if (hadValue) PlayerPrefs.SetInt(key, oldValue); else PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }

    [Test]
    public void MainMenuDisplaysSavedHighScoreAndTestRestoresPlayerRecord()
    {
        const string key = "MeteorShooter.HighScore";
        bool hadValue = PlayerPrefs.HasKey(key);
        int oldValue = PlayerPrefs.GetInt(key, 0);
        GameObject menuObject = null;
        GameObject textObject = null;
        try
        {
            PlayerPrefs.SetInt(key, 340);
            menuObject = new GameObject("MainMenuControllerTest");
            MainMenuController controller = menuObject.AddComponent<MainMenuController>();
            textObject = new GameObject("HighScoreText", typeof(Text));
            Text label = textObject.GetComponent<Text>();
            typeof(MainMenuController).GetField("highScoreText", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, label);
            controller.RefreshHighScore();
            Assert.AreEqual("HIGH SCORE  0340", label.text);
        }
        finally
        {
            if (textObject != null) Object.DestroyImmediate(textObject);
            if (menuObject != null) Object.DestroyImmediate(menuObject);
            if (hadValue) PlayerPrefs.SetInt(key, oldValue); else PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }

    [Test]
    public void MovementIsFreeInAllDirectionsAndNormalizesDiagonals()
    {
        Assert.AreEqual(Vector2.left, PlayerController.NormalizeMovement(Vector2.left));
        Assert.AreEqual(Vector2.up, PlayerController.NormalizeMovement(Vector2.up));
        Assert.AreEqual(Vector2.one.normalized, PlayerController.NormalizeMovement(Vector2.one));
    }

    [TestCase(0f, 1f, 0f)]
    [TestCase(1f, 0f, -90f)]
    [TestCase(0f, -1f, 180f)]
    [TestCase(-1f, 0f, 90f)]
    public void AimRotationPointsTheLocalUpNoseTowardMouse(float x, float y, float expectedAngle)
    {
        Assert.IsTrue(PlayerController.TryGetAimRotation(new Vector2(x, y), out Quaternion rotation));
        Assert.That(Mathf.DeltaAngle(0f, rotation.eulerAngles.z), Is.EqualTo(expectedAngle).Within(0.01f));
    }

    [Test]
    public void ZeroAimDirectionKeepsTheCurrentShipRotation()
    {
        Assert.IsFalse(PlayerController.TryGetAimRotation(Vector2.zero, out _));
    }

    [Test]
    public void CameraFollowTracksDistantPlayerPositionsAndKeepsDepth()
    {
        GameObject cameraObject = new GameObject("CameraFollowTest");
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        CameraFollow follow = cameraObject.AddComponent<CameraFollow>();
        typeof(CameraFollow).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(follow, null);
        GameObject target = new GameObject("CameraTargetTest");
        target.transform.position = new Vector3(125f, -83f, 4f);
        follow.SetTarget(target.transform);

        follow.FollowTarget(10f);

        Assert.That(cameraObject.transform.position.x, Is.EqualTo(125f).Within(0.01f));
        Assert.That(cameraObject.transform.position.y, Is.EqualTo(-83f).Within(0.01f));
        Assert.AreEqual(-10f, cameraObject.transform.position.z);
        Object.DestroyImmediate(target);
        Object.DestroyImmediate(cameraObject);
    }

    [TestCase(16f / 9f)]
    [TestCase(9f / 16f)]
    public void AsteroidsSpawnOutsideEveryViewEdgeAndAwayFromPlayer(float aspect)
    {
        Rect bounds = new Rect(-5f * aspect, -5f, 10f * aspect, 10f);
        Vector2 player = Vector2.zero;
        for (int edge = 0; edge < 4; edge++)
        {
            Vector2 point = AsteroidSpawner.GetSpawnPosition(bounds, edge, 0.5f, 1.4f);
            Assert.IsFalse(bounds.Contains(point));
        }

        for (int i = 0; i < 30; i++)
        {
            Vector2 point = AsteroidSpawner.ChooseSpawnPosition(bounds, player, 1.4f, 4f);
            Assert.IsFalse(bounds.Contains(point));
            Assert.GreaterOrEqual(Vector2.Distance(player, point), 4f);
        }
    }
}
