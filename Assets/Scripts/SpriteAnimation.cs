using UnityEngine;
using System.Collections;

namespace VirulentVentures
{
    public class SpriteAnimation : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Vector3 originalScale;
        private Vector3 originalPosition;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            originalScale = transform.localScale;
            originalPosition = transform.localPosition;
        }

        public void Jiggle(bool isAttacker)
        {
            StartCoroutine(JiggleCoroutine(isAttacker));
        }

        private IEnumerator JiggleCoroutine(bool isAttacker)
        {
            float duration = 0.3f;
            float elapsed = 0f;
            float scaleAmount = isAttacker ? 0.3f : 0.15f;
            float hopAmount = isAttacker ? 0.7f : 0.3f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scale = 1f + scaleAmount * Mathf.Sin(t * Mathf.PI * 4f);
                transform.localScale = originalScale * scale;
                transform.localPosition = originalPosition + Vector3.up * hopAmount * Mathf.Sin(t * Mathf.PI * 2f);
                yield return null;
            }
            transform.localScale = originalScale;
            transform.localPosition = originalPosition;
        }
    }
}