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

    public void Jiggle(bool isAttacker)
    {
        StartCoroutine(JiggleCoroutine(isAttacker));
    }

    private IEnumerator JiggleCoroutine(bool isAttacker)
    {
        float duration = 0.3f;
        float elapsed = 0f;
        float scaleAmount = isAttacker ? 0.3f : 0.15f; // Bigger scale for attacker
        float hopAmount = isAttacker ? 0.7f : 0.3f;   // Bigger hop for attacker

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