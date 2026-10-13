using System;
using UnityEngine;

namespace MeteorShooter
{
    [DefaultExecutionOrder(200)]
    public sealed class BackgroundScroller : MonoBehaviour
    {
        [SerializeField] private Transform[] panels;
        [SerializeField, Range(0f, 1f)] private float parallaxFactor = 0.24f;
        [SerializeField, Range(0f, 1f)] private float starParallaxFactor = 0.82f;

        private Camera mainCamera;
        private SpriteRenderer backgroundRenderer;
        private SpriteRenderer starsRenderer;
        private Sprite generatedStarSprite;
        private Texture2D generatedStarTexture;
        private Vector2 previousViewportSize;

        private void Awake()
        {
            mainCamera = Camera.main;
            if (mainCamera == null || panels == null || panels.Length == 0 || panels[0] == null) return;

            backgroundRenderer = panels[0].GetComponent<SpriteRenderer>();
            if (backgroundRenderer == null || backgroundRenderer.sprite == null) return;
            ConfigureTiledRenderer(backgroundRenderer);
            backgroundRenderer.sortingOrder = -10;

            for (int i = 1; i < panels.Length; i++)
            {
                if (panels[i] == null) continue;
                SpriteRenderer extraRenderer = panels[i].GetComponent<SpriteRenderer>();
                if (extraRenderer != null) extraRenderer.enabled = false;
            }

            CreateStarLayer();
            UpdateLayerSizes();
            UpdateLayerPositions();
        }

        private void LateUpdate()
        {
            if (mainCamera == null || backgroundRenderer == null) return;

            Vector2 viewportSize = GetViewportSize();
            if (viewportSize != previousViewportSize) UpdateLayerSizes();

            GameManager manager = GameManager.Instance;
            if (manager != null && manager.IsPaused) return;
            UpdateLayerPositions();
        }

        private Vector2 GetViewportSize()
        {
            float height = mainCamera.orthographicSize * 2f;
            return new Vector2(height * mainCamera.aspect, height);
        }

        private void UpdateLayerSizes()
        {
            Vector2 viewport = GetViewportSize();
            Vector2 backgroundTile = backgroundRenderer.sprite.bounds.size;
            backgroundRenderer.drawMode = SpriteDrawMode.Tiled;
            backgroundRenderer.size = viewport + backgroundTile * 2f;

            if (starsRenderer != null)
            {
                Vector2 starTile = starsRenderer.sprite.bounds.size;
                starsRenderer.size = viewport + starTile * 2f;
            }

            previousViewportSize = viewport;
        }

        private void UpdateLayerPositions()
        {
            Vector3 cameraPosition = mainCamera.transform.position;
            PlaceParallaxLayer(backgroundRenderer.transform, cameraPosition, parallaxFactor);
            if (starsRenderer != null) PlaceParallaxLayer(starsRenderer.transform, cameraPosition, starParallaxFactor);
        }

        private static void PlaceParallaxLayer(Transform layer, Vector3 cameraPosition, float factor)
        {
            Vector2 tileSize = layer.GetComponent<SpriteRenderer>().sprite.bounds.size;
            float offsetX = Mathf.Repeat(-cameraPosition.x * factor + tileSize.x * 0.5f, tileSize.x) - tileSize.x * 0.5f;
            float offsetY = Mathf.Repeat(-cameraPosition.y * factor + tileSize.y * 0.5f, tileSize.y) - tileSize.y * 0.5f;
            layer.position = new Vector3(cameraPosition.x + offsetX, cameraPosition.y + offsetY, layer.position.z);
        }

        private void ConfigureTiledRenderer(SpriteRenderer renderer)
        {
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.transform.localPosition = new Vector3(renderer.transform.localPosition.x, renderer.transform.localPosition.y, 2f);
            renderer.transform.localScale = Vector3.one;
            renderer.transform.localRotation = Quaternion.identity;
        }

        private void CreateStarLayer()
        {
            generatedStarTexture = CreateStarTexture();
            generatedStarTexture.hideFlags = HideFlags.DontSave;
            generatedStarSprite = Sprite.Create(
                generatedStarTexture,
                new Rect(0f, 0f, generatedStarTexture.width, generatedStarTexture.height),
                new Vector2(0.5f, 0.5f),
                32f,
                0,
                SpriteMeshType.FullRect);
            generatedStarSprite.name = "RuntimeParallaxStars";
            generatedStarSprite.hideFlags = HideFlags.DontSave;

            GameObject starsObject = new GameObject("ParallaxStars");
            starsObject.transform.SetParent(backgroundRenderer.transform.parent, false);
            starsObject.transform.position = new Vector3(0f, 0f, 1.8f);
            starsRenderer = starsObject.AddComponent<SpriteRenderer>();
            starsRenderer.sprite = generatedStarSprite;
            starsRenderer.drawMode = SpriteDrawMode.Tiled;
            starsRenderer.sortingOrder = backgroundRenderer.sortingOrder + 1;
            starsRenderer.color = new Color(0.84f, 0.96f, 1f, 0.82f);
        }

        private static Texture2D CreateStarTexture()
        {
            const int size = 256;
            var pixels = new Color32[size * size];
            var random = new System.Random(17321);
            Color32[] palette =
            {
                new Color32(154, 228, 255, 255),
                new Color32(194, 177, 255, 255),
                new Color32(223, 248, 255, 255)
            };

            for (int i = 0; i < 42; i++)
            {
                int x = random.Next(size);
                int y = random.Next(size);
                Color32 color = palette[random.Next(palette.Length)];
                byte intensity = (byte)random.Next(115, 235);
                SetPixel(pixels, size, x, y, new Color32(color.r, color.g, color.b, intensity));
                byte halo = (byte)(intensity * 0.22f);
                SetPixel(pixels, size, x - 1, y, new Color32(color.r, color.g, color.b, halo));
                SetPixel(pixels, size, x + 1, y, new Color32(color.r, color.g, color.b, halo));
                SetPixel(pixels, size, x, y - 1, new Color32(color.r, color.g, color.b, halo));
                SetPixel(pixels, size, x, y + 1, new Color32(color.r, color.g, color.b, halo));
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, false)
            {
                name = "RuntimeParallaxStarsTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat,
                anisoLevel = 0
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static void SetPixel(Color32[] pixels, int size, int x, int y, Color32 color)
        {
            x = (x % size + size) % size;
            y = (y % size + size) % size;
            int index = y * size + x;
            if (pixels[index].a < color.a) pixels[index] = color;
        }

        private void OnDestroy()
        {
            if (generatedStarSprite != null) Destroy(generatedStarSprite);
            if (generatedStarTexture != null) Destroy(generatedStarTexture);
        }
    }
}
