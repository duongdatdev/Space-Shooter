using System.Collections;
using UnityEngine;

namespace MeteorShooter
{
    public sealed class CameraShake : MonoBehaviour
    {
        private Coroutine routine;
        public Vector3 Offset { get; private set; }

        public void Shake(float duration, float magnitude)
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                Offset = (Vector3)(Random.insideUnitCircle * magnitude);
                yield return null;
            }
            Offset = Vector3.zero;
            routine = null;
        }
    }
}
