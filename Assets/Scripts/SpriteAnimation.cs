using UnityEngine;
using System.Collections;

namespace VirulentVentures
{
    public class SpriteAnimation : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void TiltForward(bool isHero)
        {
            StartCoroutine(TiltForwardCoroutine(isHero));
        }

        public void Jiggle()
        {
            StartCoroutine(JiggleCoroutine());
        }

        private IEnumerator TiltForwardCoroutine(bool isHero)
        {
            // Capture starting rotation
            Quaternion startRotation = transform.rotation;
            Quaternion targetRotation = Quaternion.Euler(0, 0, isHero ? -30f : 30f); // Heroes tilt left (-30), monsters tilt right (+30)
            float duration = 0.2f; // Fast tilt
            float holdTime = 0.1f; // Brief hold before reverting
            float elapsed = 0f;

            // Tilt forward
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.rotation = Quaternion.Lerp(startRotation, targetRotation, t);
                yield return null;
            }

            // Hold briefly
            yield return new WaitForSeconds(holdTime);

            // Revert to original rotation
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.rotation = Quaternion.Lerp(targetRotation, startRotation, t);
                yield return null;
            }

            // Ensure exact restoration
            transform.rotation = startRotation;
        }

        private IEnumerator JiggleCoroutine()
        {
            // Capture current values at the start of animation
            Vector3 startScale = transform.localScale;
            Vector3 startPosition = transform.localPosition;

            float duration = 0.3f;
            float elapsed = 0f;
            float scaleAmount = 0.15f; // Smaller scale for hit reaction
            float hopAmount = 0.3f; // Smaller hop for hit reaction

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scale = 1f + scaleAmount * Mathf.Sin(t * Mathf.PI * 4f);
                transform.localScale = startScale * scale;
                transform.localPosition = startPosition + Vector3.up * hopAmount * Mathf.Sin(t * Mathf.PI * 2f);
                yield return null;
            }

            // Restore to captured values at end
            transform.localScale = startScale;
            transform.localPosition = startPosition;
        }
    }
}