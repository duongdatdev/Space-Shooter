using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MeteorShooter
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 7.5f;
        [SerializeField] private float fireCooldown = 0.18f;
        [SerializeField] private float invulnerabilitySeconds = 1.35f;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;

        private SpriteRenderer spriteRenderer;
        private Camera mainCamera;
        private float nextShotTime;
        private bool invulnerable;
        private bool dead;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            mainCamera = Camera.main;
        }

        private void Update()
        {
            if (dead || GameManager.Instance == null || GameManager.Instance.IsPaused || GameManager.Instance.IsGameOver) return;
            Vector2 movement = Vector2.zero;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) movement.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) movement.x += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) movement.y -= 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) movement.y += 1f;
            }
            transform.position += (Vector3)(NormalizeMovement(movement) * (moveSpeed * Time.deltaTime));

            Mouse mouse = Mouse.current;
            if (mouse == null) return;
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            Vector3 mouseScreenPosition = mouse.position.ReadValue();
            mouseScreenPosition.z = transform.position.z - mainCamera.transform.position.z;
            Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(mouseScreenPosition);
            Vector2 aimDirection = (Vector2)(mouseWorldPosition - transform.position);
            if (TryGetAimRotation(aimDirection, out Quaternion aimRotation)) transform.rotation = aimRotation;

            if (mouse.leftButton.isPressed && Time.time >= nextShotTime && projectilePrefab != null && firePoint != null)
            {
                nextShotTime = Time.time + fireCooldown;
                Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            }
        }

        public bool TryTakeHit()
        {
            if (dead || invulnerable || GameManager.Instance == null || GameManager.Instance.IsGameOver) return false;
            if (GameManager.Instance.LoseLife())
            {
                dead = true;
                spriteRenderer.enabled = false;
                GameManager.Instance.TriggerGameOver(transform.position);
            }
            else
            {
                StartCoroutine(InvulnerabilityRoutine());
            }
            return true;
        }

        public static Vector2 NormalizeMovement(Vector2 movement) => movement.sqrMagnitude > 1f ? movement.normalized : movement;

        public static bool TryGetAimRotation(Vector2 direction, out Quaternion rotation)
        {
            if (direction.sqrMagnitude <= 0.0001f)
            {
                rotation = Quaternion.identity;
                return false;
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            rotation = Quaternion.Euler(0f, 0f, angle);
            return true;
        }

        private IEnumerator InvulnerabilityRoutine()
        {
            invulnerable = true;
            float elapsed = 0f;
            while (elapsed < invulnerabilitySeconds)
            {
                elapsed += 0.1f;
                spriteRenderer.enabled = !spriteRenderer.enabled;
                yield return new WaitForSeconds(0.1f);
            }
            spriteRenderer.enabled = true;
            invulnerable = false;
        }
    }
}
