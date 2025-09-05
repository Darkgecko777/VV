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

        public void TiltForward(bool isHero, float speed = 1f)
        {
            StartCoroutine(TiltForwardCoroutine(isHero, speed));
        }

        public void Jiggle(float speed = 1f)
        {
            StartCoroutine(JiggleCoroutine(speed));
        }

        private IEnumerator TiltForwardCoroutine(bool isHero, float speed)
        {
            Quaternion startRotation = transform.rotation;
            Quaternion targetRotation = Quaternion.Euler(0, 0, isHero ? -30f : 30f);
            float baseDuration = 0.2f;
            float holdTime = 0.1f / speed; // Scale hold time inversely
            float elapsed = 0f;

            // Tilt forward
            while (elapsed < baseDuration)
            {
                elapsed += Time.deltaTime * speed; // Scale time progression
                float t = Mathf.Clamp01(elapsed / baseDuration);
                transform.rotation = Quaternion.Lerp(startRotation, targetRotation, t);
                yield return null;
            }

            // Hold briefly
            yield return new WaitForSeconds(holdTime);

            // Revert to original rotation
            elapsed = 0f;
            while (elapsed < baseDuration)
            {
                elapsed += Time.deltaTime * speed; // Scale time progression
                float t = Mathf.Clamp01(elapsed / baseDuration);
                transform.rotation = Quaternion.Lerp(targetRotation, startRotation, t);
                yield return null;
            }

            // Ensure exact restoration
            transform.rotation = startRotation;
        }

        private IEnumerator JiggleCoroutine(float speed)
        {
            Vector3 startScale = transform.localScale;
            Vector3 startPosition = transform.localPosition;
            float baseDuration = 0.3f;
            float elapsed = 0f;
            float scaleAmount = 0.15f;
            float hopAmount = 0.3f;

            while (elapsed < baseDuration)
            {
                elapsed += Time.deltaTime * speed; // Scale time progression
                float t = elapsed / baseDuration;
                float scale = 1f + scaleAmount * Mathf.Sin(t * Mathf.PI * 4f);
                transform.localScale = startScale * scale;
                transform.localPosition = startPosition + Vector3.up * hopAmount * Mathf.Sin(t * Mathf.PI * 2f);
                yield return null;
            }

            // Restore to captured values
            transform.localScale = startScale;
            transform.localPosition = startPosition;
        }
    }
}