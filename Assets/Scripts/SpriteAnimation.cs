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

        public void Jiggle(bool isAttacker)
        {
            StartCoroutine(JiggleCoroutine(isAttacker));
        }

        private IEnumerator JiggleCoroutine(bool isAttacker)
        {
            // Capture current values at the start of animation
            Vector3 startScale = transform.localScale;
            Vector3 startPosition = transform.localPosition;

            float duration = 0.3f;
            float elapsed = 0f;
            float scaleAmount = isAttacker ? 0.3f : 0.15f;
            float hopAmount = isAttacker ? 0.7f : 0.3f;

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