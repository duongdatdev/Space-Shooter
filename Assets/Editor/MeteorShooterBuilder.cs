using System;
using System.Collections.Generic;
using System.IO;
using MeteorShooter;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MeteorShooterBuilder
{
    private const string PrefabFolder = "Assets/Prefabs";
    private const string AnimationFolder = "Assets/Animations";
    private const string SceneFolder = "Assets/Scenes";
    private const string StarArtFolder = "Assets/Art/StarHazard";
    private const string StarVariantsFolder = "Assets/Art/StarHazard/Variants";
    private const string PlayerIdlePrefix = "Assets/Art/Player/player_idle_";
    private const string ProjectileSpritePath = "Assets/Art/Projectiles/laser.png";
    private static readonly string[] StarVariantNames = { "pink", "blue", "purple", "lime", "gold", "mint", "orange", "lavender" };
    private static Font font;

    [MenuItem("Meteor Shooter/Build Complete Game")]
    public static void Build()
    {
        ValidateStarVariantFiles();
        EnsureFolders();
        Directory.CreateDirectory(StarArtFolder);
        ConfigureTextures();
        AssetDatabase.Refresh();
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        AnimationClip playerIdle = CreateClip("PlayerIdle", "Assets/Art/Player/player_idle_", 4, 8f, true);
        AnimationClip asteroidExplosion = CreateClip("AsteroidExplosion", "Assets/Art/FX/asteroid_explosion_", 6, 14f, false);
        AnimationClip playerExplosion = CreateClip("PlayerExplosion", "Assets/Art/FX/player_explosion_", 6, 12f, false);

        AnimatorController playerController = CreateController("PlayerIdleController", playerIdle);
        AnimatorController asteroidExplosionController = CreateController("AsteroidExplosionController", asteroidExplosion);
        AnimatorController playerExplosionController = CreateController("PlayerExplosionController", playerExplosion);

        GameObject asteroidFx = CreateFxPrefab("AsteroidExplosion", asteroidExplosionController, 0.55f, 4);
        GameObject playerFx = CreateFxPrefab("PlayerExplosion", playerExplosionController, 0.68f, 5);
        GameObject bullet = CreateBulletPrefab();
        GameObject[] asteroids =
        {
            CreateAsteroidPrefab("AsteroidSmall", "Assets/Art/Asteroids/asteroid_small.png", 2.45f, 0.34f),
            CreateAsteroidPrefab("AsteroidMedium", "Assets/Art/Asteroids/asteroid_medium.png", 2.15f, 0.42f),
            CreateAsteroidPrefab("AsteroidLarge", "Assets/Art/Asteroids/asteroid_large.png", 1.85f, 0.48f)
        };
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        GameObject starHazard = CreateStarHazardPrefab(LoadStarVariantSprites());
        GameObject player = CreatePlayerPrefab(playerController, bullet);

        BuildMainMenuScene();
        BuildGameplayScene(player, asteroids, starHazard, asteroidFx, playerFx);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Gameplay.unity", true)
        };
        PlayerSettings.defaultScreenWidth = 720;
        PlayerSettings.defaultScreenHeight = 1280;
        PlayerSettings.productName = "Meteor Shooter";
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        Debug.Log("Meteor Shooter build complete: scenes, prefabs, animations, UI, and build settings created.");
    }

    [MenuItem("Meteor Shooter/Upgrade Star Hazard and Main Menu High Score")]
    public static void UpgradeStarHazardAndMainMenuHighScore()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before running this scoped upgrade.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save all open scenes before running this scoped upgrade.");
        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        Directory.CreateDirectory(PrefabFolder);
        Directory.CreateDirectory(StarVariantsFolder);
        ValidateStarVariantFiles();
        GameObject starPrefab = CreateStarHazardPrefab(LoadStarVariantSprites());

        Scene gameplay = EditorSceneManager.OpenScene($"{SceneFolder}/Gameplay.unity", OpenSceneMode.Single);
        AsteroidSpawner spawner = UnityEngine.Object.FindAnyObjectByType<AsteroidSpawner>();
        if (spawner == null) throw new InvalidOperationException("Gameplay scene has no AsteroidSpawner.");
        SetObject(spawner, "starHazardPrefab", starPrefab);
        SetFloat(spawner, "starSpawnChance", 0.24f);
        EditorSceneManager.MarkSceneDirty(gameplay);
        EditorSceneManager.SaveScene(gameplay);

        Scene menu = EditorSceneManager.OpenScene($"{SceneFolder}/MainMenu.unity", OpenSceneMode.Single);
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        MainMenuController controller = UnityEngine.Object.FindAnyObjectByType<MainMenuController>();
        Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        if (controller == null || canvas == null) throw new InvalidOperationException("MainMenu scene is missing its controller or Canvas.");
        Text highScore = null;
        foreach (Text candidate in canvas.GetComponentsInChildren<Text>(true))
            if (candidate.gameObject.name == "MainMenuHighScore") { highScore = candidate; break; }
        if (highScore == null)
        {
            highScore = CreateText(canvas.transform, "HIGH SCORE  0000", 21, new Vector2(0.08f, 0.48f), new Vector2(0.92f, 0.55f), new Color(1f, 0.72f, 0.27f), TextAnchor.MiddleCenter, FontStyle.Bold);
            highScore.gameObject.name = "MainMenuHighScore";
        }
        SetObject(controller, "highScoreText", highScore);
        EditorSceneManager.MarkSceneDirty(menu);
        EditorSceneManager.SaveScene(menu);
        AssetDatabase.SaveAssets();
        EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
        Debug.Log("Star hazards and persistent MainMenu high score are wired into the existing scenes.");
    }

    [MenuItem("Meteor Shooter/Refresh Pixel Star Hazard Variants")]
    public static void RefreshPixelStarHazardVariants()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before refreshing the pixel star hazard variants.");
        Directory.CreateDirectory(StarVariantsFolder);
        ValidateStarVariantFiles();
        Directory.CreateDirectory(PrefabFolder);
        CreateStarHazardPrefab(LoadStarVariantSprites());
        AssetDatabase.SaveAssets();
        Debug.Log("Eight pixel star variants and the existing hazard prefab were refreshed; scene references are preserved.");
    }

    [MenuItem("Meteor Shooter/Refresh Player Ship Art")]
    public static void RefreshPlayerShipArt()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before refreshing the player ship art.");

        Sprite[] frames = new Sprite[4];
        for (int i = 0; i < frames.Length; i++)
        {
            string path = $"{PlayerIdlePrefix}{i + 1:00}.png";
            if (!File.Exists(path)) throw new InvalidOperationException($"Player ship frame is missing: {path}");
        }
        string playerPrefabPath = $"{PrefabFolder}/Player.prefab";
        string projectilePrefabPath = $"{PrefabFolder}/Bullet.prefab";
        if (!File.Exists(ProjectileSpritePath)) throw new InvalidOperationException($"Projectile image is missing: {ProjectileSpritePath}");
        if (!File.Exists(playerPrefabPath)) throw new InvalidOperationException($"Player prefab is missing: {playerPrefabPath}");
        if (!File.Exists(projectilePrefabPath)) throw new InvalidOperationException($"Projectile prefab is missing: {projectilePrefabPath}");

        for (int i = 0; i < frames.Length; i++)
        {
            string path = $"{PlayerIdlePrefix}{i + 1:00}.png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            ConfigurePlayerShipSprite(path);
            frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (frames[i] == null) throw new InvalidOperationException($"Player ship frame failed to import as a Sprite: {path}");
        }
        AssetDatabase.ImportAsset(ProjectileSpritePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        ConfigureProjectileSprite(ProjectileSpritePath);
        Sprite projectileSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ProjectileSpritePath);
        if (projectileSprite == null) throw new InvalidOperationException($"Projectile image failed to import as a Sprite: {ProjectileSpritePath}");

        SetPrefabSprite(playerPrefabPath, frames[0], "Player");
        SetPrefabSprite(projectilePrefabPath, projectileSprite, "Bullet");

        AssetDatabase.SaveAssets();
        Debug.Log("Four player frames and the cyan projectile sprite were refreshed; existing prefab and scene references are preserved.");
    }

    private static void SetPrefabSprite(string prefabPath, Sprite sprite, string objectName)
    {
        GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            SpriteRenderer renderer = prefabContents.GetComponent<SpriteRenderer>();
            if (renderer == null) throw new InvalidOperationException($"{objectName} prefab has no root SpriteRenderer.");
            renderer.sprite = sprite;
            PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabContents);
        }
    }

    private static void ConfigurePlayerShipSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException($"No texture importer found for player ship frame: {path}");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = 128f;
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.spritePivot = new Vector2(0.5f, 0.5f);
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    private static void ConfigureProjectileSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException($"No texture importer found for projectile: {path}");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = 80f;
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.spritePivot = new Vector2(0.5f, 0.5f);
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    private static void ValidateStarVariantFiles()
    {
        foreach (string name in StarVariantNames)
        {
            string path = $"{StarVariantsFolder}/star_{name}.png";
            if (!File.Exists(path)) throw new InvalidOperationException($"Pixel star hazard variant is missing: {path}");
        }
    }

    private static Sprite[] LoadStarVariantSprites()
    {
        Sprite[] sprites = new Sprite[StarVariantNames.Length];
        for (int i = 0; i < StarVariantNames.Length; i++)
        {
            string path = $"{StarVariantsFolder}/star_{StarVariantNames[i]}.png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            ConfigurePixelStarSprite(path);
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprites[i] == null) throw new InvalidOperationException($"Pixel star variant failed to import as a Sprite: {path}");
        }
        return sprites;
    }

    private static void ConfigurePixelStarSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException($"No texture importer found for pixel star variant: {path}");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = GetStarSpritePixelsPerUnit(path);
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    private static float GetStarSpritePixelsPerUnit(string path)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        return texture != null ? Mathf.Max(texture.width, texture.height) / 1.8f : 128f / 1.8f;
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory(PrefabFolder);
        Directory.CreateDirectory(AnimationFolder);
        Directory.CreateDirectory(SceneFolder);
    }

    private static void ConfigureTextures()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            bool pixelStarVariant = path.StartsWith(StarVariantsFolder + "/", StringComparison.Ordinal);
            bool playerIdleFrame = path.StartsWith(PlayerIdlePrefix, StringComparison.Ordinal);
            bool projectileSprite = string.Equals(path, ProjectileSpritePath, StringComparison.Ordinal);
            importer.filterMode = pixelStarVariant ? FilterMode.Point : FilterMode.Bilinear;
            if (path.Contains("Background") || pixelStarVariant || playerIdleFrame || projectileSprite)
            {
                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                if (playerIdleFrame || projectileSprite)
                {
                    settings.spriteAlignment = (int)SpriteAlignment.Center;
                    settings.spritePivot = new Vector2(0.5f, 0.5f);
                }
                importer.SetTextureSettings(settings);
            }
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = pixelStarVariant
                ? GetStarSpritePixelsPerUnit(path)
                : path.Contains("Background") ? 155f : projectileSprite ? 80f : 128f;
            importer.SaveAndReimport();
        }
    }

    private static AnimationClip CreateClip(string name, string prefix, int count, float fps, bool loop)
    {
        string path = $"{AnimationFolder}/{name}.anim";
        AssetDatabase.DeleteAsset(path);
        AnimationClip clip = new AnimationClip { name = name, frameRate = fps };
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[count + (loop ? 1 : 0)];
        for (int i = 0; i < count; i++)
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time = i / fps,
                value = AssetDatabase.LoadAssetAtPath<Sprite>($"{prefix}{i + 1:00}.png")
            };
        }
        if (loop) keys[count] = new ObjectReferenceKeyframe { time = count / fps, value = keys[0].value };
        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = string.Empty,
            propertyName = "m_Sprite"
        };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static AnimatorController CreateController(string name, AnimationClip clip)
    {
        string path = $"{AnimationFolder}/{name}.controller";
        AssetDatabase.DeleteAsset(path);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        AnimatorState state = controller.layers[0].stateMachine.AddState(clip.name);
        state.motion = clip;
        controller.layers[0].stateMachine.defaultState = state;
        return controller;
    }

    private static GameObject CreateFxPrefab(string name, AnimatorController controller, float lifetime, int order)
    {
        GameObject go = new GameObject(name);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = order;
        Animator animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        FxLifetime fx = go.AddComponent<FxLifetime>();
        SetFloat(fx, "lifetime", lifetime);
        string path = $"{PrefabFolder}/{name}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        UnityEngine.Object.DestroyImmediate(go);
        return prefab;
    }

    private static GameObject CreateBulletPrefab()
    {
        GameObject go = new GameObject("Bullet");
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ProjectileSpritePath);
        renderer.sortingOrder = 3;
        go.transform.localScale = new Vector3(0.45f, 0.45f, 1f);
        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        CapsuleCollider2D collider = go.AddComponent<CapsuleCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.25f, 0.85f);
        go.AddComponent<Projectile>();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabFolder}/Bullet.prefab");
        UnityEngine.Object.DestroyImmediate(go);
        return prefab;
    }

    private static GameObject CreateAsteroidPrefab(string name, string spritePath, float speed, float radius)
    {
        GameObject go = new GameObject(name);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        renderer.sortingOrder = 2;
        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.constraints = RigidbodyConstraints2D.None;
        CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = radius;
        Asteroid asteroid = go.AddComponent<Asteroid>();
        SetFloat(asteroid, "baseSpeed", speed);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabFolder}/{name}.prefab");
        UnityEngine.Object.DestroyImmediate(go);
        return prefab;
    }

    private static GameObject CreateStarHazardPrefab(Sprite[] appearanceVariants)
    {
        if (appearanceVariants == null || appearanceVariants.Length != StarVariantNames.Length)
            throw new InvalidOperationException("The pixel star hazard requires exactly eight appearance variants.");
        for (int i = 0; i < appearanceVariants.Length; i++)
            if (appearanceVariants[i] == null) throw new InvalidOperationException($"Pixel star variant {StarVariantNames[i]} is missing.");
        GameObject go = new GameObject("StarHazard");
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = appearanceVariants[0];
        renderer.color = Color.white;
        renderer.sortingOrder = 2;
        go.transform.localScale = new Vector3(0.92f, 0.92f, 1f);
        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.constraints = RigidbodyConstraints2D.None;
        PolygonCollider2D collider = go.AddComponent<PolygonCollider2D>();
        collider.isTrigger = true;
        collider.points = new[]
        {
            new Vector2(0f, 0.675f), new Vector2(0.21f, 0.267f), new Vector2(0.675f, 0.267f),
            new Vector2(0.38f, -0.07f), new Vector2(0.55f, -0.65f), new Vector2(0f, -0.38f),
            new Vector2(-0.55f, -0.65f), new Vector2(-0.38f, -0.07f), new Vector2(-0.675f, 0.267f),
            new Vector2(-0.21f, 0.267f)
        };
        StarHazard hazard = go.AddComponent<StarHazard>();
        SetFloat(hazard, "baseSpeed", 2.6f);
        SetFloat(hazard, "minimumRotationSpeed", 35f);
        SetFloat(hazard, "maximumRotationSpeed", 80f);
        SetObjectArray(hazard, "appearanceVariants", appearanceVariants);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabFolder}/StarHazard.prefab");
        UnityEngine.Object.DestroyImmediate(go);
        return prefab;
    }

    private static GameObject CreatePlayerPrefab(AnimatorController controller, GameObject bulletPrefab)
    {
        GameObject go = new GameObject("Player");
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Player/player_idle_01.png");
        renderer.sortingOrder = 3;
        Animator animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        CapsuleCollider2D collider = go.AddComponent<CapsuleCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.82f, 1.12f);
        collider.offset = new Vector2(0f, 0.05f);
        PlayerController player = go.AddComponent<PlayerController>();
        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(go.transform, false);
        firePoint.transform.localPosition = new Vector3(0f, 0.88f, 0f);
        SetObject(player, "projectilePrefab", bulletPrefab);
        SetObject(player, "firePoint", firePoint.transform);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabFolder}/Player.prefab");
        UnityEngine.Object.DestroyImmediate(go);
        return prefab;
    }

    private static void BuildMainMenuScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera camera = CreateCamera();
        CreateBackground(camera);
        MainMenuController controller = new GameObject("MainMenuController").AddComponent<MainMenuController>();
        Canvas canvas = CreateCanvas();
        CreateText(canvas.transform, "METEOR\nSHOOTER", 58, new Vector2(0.12f, 0.62f), new Vector2(0.88f, 0.9f), new Color(0.86f, 0.97f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold);
        CreateText(canvas.transform, "DEFEND THE LAST STARLANE", 17, new Vector2(0.08f, 0.56f), new Vector2(0.92f, 0.63f), new Color(0.34f, 0.88f, 1f), TextAnchor.MiddleCenter, FontStyle.Normal);
        Text highScore = CreateText(canvas.transform, "HIGH SCORE  0000", 21, new Vector2(0.08f, 0.48f), new Vector2(0.92f, 0.55f), new Color(1f, 0.72f, 0.27f), TextAnchor.MiddleCenter, FontStyle.Bold);
        highScore.gameObject.name = "MainMenuHighScore";
        SetObject(controller, "highScoreText", highScore);
        Button play = CreateButton(canvas.transform, "PLAY", new Vector2(0.2f, 0.38f), new Vector2(0.8f, 0.47f));
        Button quit = CreateButton(canvas.transform, "QUIT", new Vector2(0.2f, 0.27f), new Vector2(0.8f, 0.36f));
        UnityEventTools.AddPersistentListener(play.onClick, controller.Play);
        UnityEventTools.AddPersistentListener(quit.onClick, controller.Quit);
        CreateText(canvas.transform, "WASD / ARROWS MOVE     LEFT CLICK AIM / FIRE     ESC PAUSE", 14, new Vector2(0.05f, 0.07f), new Vector2(0.95f, 0.14f), new Color(0.6f, 0.75f, 0.86f), TextAnchor.MiddleCenter, FontStyle.Normal);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
    }

    private static void BuildGameplayScene(GameObject playerPrefab, GameObject[] asteroids, GameObject starHazard, GameObject asteroidFx, GameObject playerFx)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera camera = CreateCamera();
        CameraShake shake = camera.gameObject.AddComponent<CameraShake>();
        CreateBackground(camera);
        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.position = new Vector3(0f, -3.85f, 0f);

        AsteroidSpawner spawner = new GameObject("AsteroidSpawner").AddComponent<AsteroidSpawner>();
        SetObjectArray(spawner, "asteroidPrefabs", asteroids);
        SetObject(spawner, "starHazardPrefab", starHazard);
        SetFloat(spawner, "starSpawnChance", 0.24f);
        SetFloat(spawner, "spawnMargin", 1.35f);
        SetFloat(spawner, "minimumPlayerDistance", 4f);
        SetFloat(spawner, "targetJitter", 1.6f);
        GameManager manager = new GameObject("GameManager").AddComponent<GameManager>();

        Canvas canvas = CreateCanvas();
        Text score = CreateText(canvas.transform, "SCORE  0000", 24, new Vector2(0.04f, 0.92f), new Vector2(0.54f, 0.985f), Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
        Text lives = CreateText(canvas.transform, "LIVES  ◆◆◆", 22, new Vector2(0.48f, 0.92f), new Vector2(0.96f, 0.985f), new Color(0.35f, 0.92f, 1f), TextAnchor.MiddleRight, FontStyle.Bold);

        GameObject pausePanel = CreateOverlay(canvas.transform, "PausePanel", new Color(0.015f, 0.04f, 0.11f, 0.92f));
        CreateText(pausePanel.transform, "PAUSED", 46, new Vector2(0.1f, 0.65f), new Vector2(0.9f, 0.78f), Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        Button resume = CreateButton(pausePanel.transform, "RESUME", new Vector2(0.2f, 0.50f), new Vector2(0.8f, 0.58f));
        Button restart = CreateButton(pausePanel.transform, "RESTART", new Vector2(0.2f, 0.39f), new Vector2(0.8f, 0.47f));
        Button pauseMenu = CreateButton(pausePanel.transform, "MAIN MENU", new Vector2(0.2f, 0.28f), new Vector2(0.8f, 0.36f));
        UnityEventTools.AddPersistentListener(resume.onClick, manager.Resume);
        UnityEventTools.AddPersistentListener(restart.onClick, manager.Restart);
        UnityEventTools.AddPersistentListener(pauseMenu.onClick, manager.MainMenu);

        GameObject gameOverPanel = CreateOverlay(canvas.transform, "GameOverPanel", new Color(0.015f, 0.03f, 0.08f, 0.94f));
        CreateText(gameOverPanel.transform, "GAME OVER", 48, new Vector2(0.08f, 0.68f), new Vector2(0.92f, 0.8f), new Color(1f, 0.48f, 0.2f), TextAnchor.MiddleCenter, FontStyle.Bold);
        Text finalScore = CreateText(gameOverPanel.transform, "SCORE  0000", 25, new Vector2(0.1f, 0.58f), new Vector2(0.9f, 0.65f), Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        Text highScore = CreateText(gameOverPanel.transform, "HIGH SCORE  0000", 21, new Vector2(0.1f, 0.51f), new Vector2(0.9f, 0.58f), new Color(0.35f, 0.92f, 1f), TextAnchor.MiddleCenter, FontStyle.Normal);
        Button again = CreateButton(gameOverPanel.transform, "PLAY AGAIN", new Vector2(0.2f, 0.36f), new Vector2(0.8f, 0.44f));
        Button gameOverMenu = CreateButton(gameOverPanel.transform, "MAIN MENU", new Vector2(0.2f, 0.25f), new Vector2(0.8f, 0.33f));
        UnityEventTools.AddPersistentListener(again.onClick, manager.Restart);
        UnityEventTools.AddPersistentListener(gameOverMenu.onClick, manager.MainMenu);

        SetObject(manager, "scoreText", score);
        SetObject(manager, "livesText", lives);
        SetObject(manager, "finalScoreText", finalScore);
        SetObject(manager, "highScoreText", highScore);
        SetObject(manager, "pausePanel", pausePanel);
        SetObject(manager, "gameOverPanel", gameOverPanel);
        SetObject(manager, "spawner", spawner);
        SetObject(manager, "asteroidExplosionPrefab", asteroidFx);
        SetObject(manager, "playerExplosionPrefab", playerFx);
        SetObject(manager, "cameraShake", shake);
        pausePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Gameplay.unity");
    }

    private static Camera CreateCamera()
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        Camera camera = go.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.4f;
        camera.backgroundColor = new Color(0.005f, 0.012f, 0.035f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        go.AddComponent<AudioListener>();
        go.AddComponent<CameraFollow>();
        return camera;
    }

    private static void CreateBackground(Camera camera)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Background/space_background.png");
        GameObject root = new GameObject("ScrollingBackground");
        Transform[] panels = new Transform[1];
        for (int i = 0; i < panels.Length; i++)
        {
            GameObject panel = new GameObject($"SpacePanel_{i + 1}");
            panel.transform.SetParent(root.transform);
            panel.transform.position = new Vector3(camera.transform.position.x, camera.transform.position.y, 2f);
            SpriteRenderer renderer = panel.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = -10;
            float height = camera.orthographicSize * 2f;
            float width = height * camera.aspect;
            float scale = Mathf.Max(width / sprite.bounds.size.x, height / sprite.bounds.size.y);
            panel.transform.localScale = new Vector3(scale, scale, 1f);
            panels[i] = panel.transform;
        }
        BackgroundScroller scroller = root.AddComponent<BackgroundScroller>();
        SetObjectArray(scroller, "panels", panels);
        SetFloat(scroller, "parallaxFactor", 0.24f);
        SetFloat(scroller, "starParallaxFactor", 0.82f);
    }

    private static Canvas CreateCanvas()
    {
        GameObject go = new GameObject("Canvas");
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(720, 1280);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        return canvas;
    }

    private static GameObject CreateOverlay(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static Text CreateText(Transform parent, string value, int size, Vector2 min, Vector2 max, Color color, TextAnchor anchor, FontStyle style)
    {
        GameObject go = new GameObject("Text_" + value.Replace("\n", "_"), typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Text text = go.GetComponent<Text>();
        text.text = value;
        text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = anchor;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Math.Max(10, size / 2);
        text.resizeTextMaxSize = size;
        return text;
    }

    private static Button CreateButton(Transform parent, string label, Vector2 min, Vector2 max)
    {
        GameObject go = new GameObject("Button_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Image image = go.GetComponent<Image>();
        image.color = new Color(0.035f, 0.22f, 0.39f, 0.94f);
        Button button = go.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.08f, 0.55f, 0.75f, 1f);
        colors.pressedColor = new Color(0.02f, 0.72f, 0.88f, 1f);
        button.colors = colors;
        CreateText(go.transform, label, 24, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f), Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        return button;
    }

    private static void SetObject(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObjectArray<T>(UnityEngine.Object target, string field, T[] values) where T : UnityEngine.Object
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(field);
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(UnityEngine.Object target, string field, float value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(field).floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
