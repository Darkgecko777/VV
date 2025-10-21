using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace VirulentVentures
{
    public class SpriteAnimation : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        [SerializeField] private CombatConfig combatConfig;
        [SerializeField] private EventBusSO eventBus;
        private bool isAnimating = false;
        private bool isPaused;
        private Queue<IEnumerator> animationQueue = new Queue<IEnumerator>();
        public bool IsAnimating => isAnimating;

        public void Init(CombatConfig config, EventBusSO bus)
        {
            combatConfig = config;
            eventBus = bus;
            if (combatConfig == null)
            {
                Debug.LogError("SpriteAnimation: CombatConfig is null in Init.");
                return;
            }
            if (eventBus == null)
            {
                Debug.LogError("SpriteAnimation: EventBus is null in Init.");
                return;
            }
            eventBus.OnCombatPaused += () => isPaused = true;
            eventBus.OnCombatPlayed += () => isPaused = false;
        }

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogError("SpriteAnimation: SpriteRenderer is missing.");
            }
        }

        void OnDestroy()
        {
            if (eventBus != null)
            {
                eventBus.OnCombatPaused -= () => isPaused = true;
                eventBus.OnCombatPlayed -= () => isPaused = false;
            }
        }

        public void TiltForward(bool isHero)
        {
            EnqueueAnimation(TiltForwardCoroutine(isHero));
        }

        public void Jiggle()
        {
            EnqueueAnimation(JiggleCoroutine());
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

        private IEnumerator TiltForwardCoroutine(bool isHero)
        {
            if (!gameObject.activeSelf || combatConfig == null) yield break;

            Quaternion startRotation = transform.rotation;
            Quaternion targetRotation = Quaternion.Euler(0, 0, isHero ? -30f : 30f);
            float baseDuration = 0.4f;
            float holdTime = 0.15f;
            float elapsed = 0f;

            // Tilt forward
            while (elapsed < baseDuration)
            {
                if (isPaused || !gameObject.activeSelf) yield return null;
                elapsed += Time.deltaTime * combatConfig.CombatSpeed;
                float t = Mathf.Clamp01(elapsed / baseDuration);
                transform.rotation = Quaternion.Lerp(startRotation, targetRotation, t);
                yield return null;
            }

            // Hold briefly
            elapsed = 0f;
            while (elapsed < holdTime)
            {
                if (isPaused || !gameObject.activeSelf) yield return null;
                elapsed += Time.deltaTime * combatConfig.CombatSpeed;
                yield return null;
            }

            // Revert to original rotation
            elapsed = 0f;
            while (elapsed < baseDuration)
            {
                if (isPaused || !gameObject.activeSelf) yield return null;
                elapsed += Time.deltaTime * combatConfig.CombatSpeed;
                float t = Mathf.Clamp01(elapsed / baseDuration);
                transform.rotation = Quaternion.Lerp(targetRotation, startRotation, t);
                yield return null;
            }

            // Ensure exact restoration
            transform.rotation = startRotation;
        }

        private IEnumerator JiggleCoroutine()
        {
            if (!gameObject.activeSelf || combatConfig == null) yield break;

            Vector3 startScale = transform.localScale;
            Vector3 startPosition = transform.localPosition;
            float baseDuration = 0.3f;
            float elapsed = 0f;
            float scaleAmount = 0.15f;
            float hopAmount = 0.3f;

            while (elapsed < baseDuration)
            {
                if (isPaused || !gameObject.activeSelf) yield return null;
                elapsed += Time.deltaTime * combatConfig.CombatSpeed;
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