using UnityEngine;
using System.Collections;

public class SpriteAnimation : MonoBehaviour
{
    private Vector3 originalScale;
    private Vector3 originalPosition;

    void Awake()
    {
        originalScale = transform.localScale;
        originalPosition = transform.localPosition;
    }

    public void Jiggle()
    {
        StartCoroutine(JiggleCoroutine());
    }

    private IEnumerator JiggleCoroutine()
    {
        float duration = 0.3f;
        float elapsed = 0f;
        float scaleAmount = 0.2f;
        float hopAmount = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = 1f + scaleAmount * Mathf.Sin(t * Mathf.PI * 4f); // Jiggle effect
            transform.localScale = originalScale * scale;
            transform.localPosition = originalPosition + Vector3.up * hopAmount * Mathf.Sin(t * Mathf.PI * 2f); // Hop effect
            yield return null;
        }
        transform.localScale = originalScale;
        transform.localPosition = originalPosition;
    }
}