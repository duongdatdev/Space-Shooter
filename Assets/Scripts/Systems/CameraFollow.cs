using UnityEngine;

namespace MeteorShooter
{
    [DefaultExecutionOrder(100)]
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private float followSpeed = 6f;

        private Transform target;
        private CameraShake cameraShake;
        private Vector3 basePosition;

        public void SetTarget(Transform value) => target = value;

        private void Awake()
        {
            cameraShake = GetComponent<CameraShake>();
            basePosition = transform.position;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                PlayerController player = FindAnyObjectByType<PlayerController>();
                if (player != null) target = player.transform;
            }
            if (target != null) FollowTarget(Time.deltaTime);
        }

        public void FollowTarget(float deltaTime)
        {
            if (target == null) return;
            if (cameraShake == null) cameraShake = GetComponent<CameraShake>();
            Vector3 destination = new Vector3(target.position.x, target.position.y, basePosition.z);
            float blend = 1f - Mathf.Exp(-followSpeed * Mathf.Max(0f, deltaTime));
            Vector3 shakeOffset = cameraShake != null ? cameraShake.Offset : Vector3.zero;
            basePosition = Vector3.Lerp(basePosition, destination, blend);
            transform.position = basePosition + shakeOffset;
        }
    }
}
