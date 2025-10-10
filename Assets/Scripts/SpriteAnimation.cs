using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace VirulentVentures
{
    public class SpriteAnimation : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private bool isAnimating = false;
        private Queue<IEnumerator> animationQueue = new Queue<IEnumerator>();
        public bool IsAnimating => isAnimating;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void TiltForward(bool isHero, float speed = 1f)
        {
            EnqueueAnimation(TiltForwardCoroutine(isHero, Mathf.Clamp(speed, 0.5f, 1.5f)));
        }

        public void Jiggle(float speed = 1f)
        {
            EnqueueAnimation(JiggleCoroutine(Mathf.Clamp(speed, 0.5f, 1.5f)));
        }

        private void EnqueueAnimation(IEnumerator animation)
        {
            animationQueue.Enqueue(animation);
            if (!isAnimating)
            {
                StartCoroutine(ProcessAnimationQueue());
            }
        }

        private IEnumerator ProcessAnimationQueue()
        {
            while (animationQueue.Count > 0)
            {
                if (!gameObject.activeSelf)
                {
                    animationQueue.Clear();
                    isAnimating = false;
                    yield break;
                }

                isAnimating = true;
                yield return StartCoroutine(animationQueue.Dequeue());
            }
            isAnimating = false;
        }

        private IEnumerator TiltForwardCoroutine(bool isHero, float speed)
        {
            if (!gameObject.activeSelf) yield break;

            Quaternion startRotation = transform.rotation;
            Quaternion targetRotation = Quaternion.Euler(0, 0, isHero ? -30f : 30f);
            float baseDuration = 0.4f; // Increased from 0.3f for more visible animation
            float holdTime = 0.15f / speed;
            float elapsed = 0f;

            // Tilt forward
            while (elapsed < baseDuration)
            {
                if (!gameObject.activeSelf) yield break;
                elapsed += Time.deltaTime * speed;
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
                if (!gameObject.activeSelf) yield break;
                elapsed += Time.deltaTime * speed;
                float t = Mathf.Clamp01(elapsed / baseDuration);
                transform.rotation = Quaternion.Lerp(targetRotation, startRotation, t);
                yield return null;
            }

            // Ensure exact restoration
            transform.rotation = startRotation;
        }

        private IEnumerator JiggleCoroutine(float speed)
        {
            if (!gameObject.activeSelf) yield break;

            Vector3 startScale = transform.localScale;
            Vector3 startPosition = transform.localPosition;
            float baseDuration = 0.3f;
            float elapsed = 0f;
            float scaleAmount = 0.15f;
            float hopAmount = 0.3f;

            while (elapsed < baseDuration)
            {
                if (!gameObject.activeSelf) yield break;
                elapsed += Time.deltaTime * speed;
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