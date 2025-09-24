using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace VirulentVentures
{
    public class CombatVisualsComponent : MonoBehaviour
    {
        [SerializeField] private VisualConfig visualConfig;
        [SerializeField] private EventBusSO eventBus;
        [SerializeField] private CombatConfig combatConfig;
        [SerializeField] private CharacterPositions characterPositions;
        private Dictionary<ICombatUnit, GameObject> unitGameObjects = new Dictionary<ICombatUnit, GameObject>();
        private GameObject backgroundGameObject;

        void Awake()
        {
            if (!ValidateReferences()) return;
            eventBus.OnCombatInitialized += InitializeCombat;
            eventBus.OnUnitAttacking += HandleUnitAttacking;
            eventBus.OnUnitDamaged += HandleUnitDamaged;
            eventBus.OnUnitDied += HandleUnitDied;
            eventBus.OnUnitRetreated += HandleUnitRetreated;
            SetupBackground();
        }

        void OnDestroy()
        {
            eventBus.OnCombatInitialized -= InitializeCombat;
            eventBus.OnUnitAttacking -= HandleUnitAttacking;
            eventBus.OnUnitDamaged -= HandleUnitDamaged;
            eventBus.OnUnitDied -= HandleUnitDied;
            eventBus.OnUnitRetreated -= HandleUnitRetreated;
            if (backgroundGameObject != null)
                Destroy(backgroundGameObject);
        }

        private void SetupBackground()
        {
            if (backgroundGameObject != null) Destroy(backgroundGameObject);
            var backgroundSprite = visualConfig.GetCombatBackground();
            if (backgroundSprite == null)
            {
                Debug.LogError("CombatVisualsComponent: Background sprite not found.");
                return;
            }
            backgroundGameObject = new GameObject("CombatBackground");
            var sr = backgroundGameObject.AddComponent<SpriteRenderer>();
            sr.sprite = backgroundSprite;
            sr.sortingLayerName = "Background";
            sr.sortingOrder = 0;
            backgroundGameObject.transform.localScale = new Vector3(2.24f, 0.65f, 1f);
        }

        private void InitializeCombat(EventBusSO.CombatInitData data)
        {
            unitGameObjects.Clear();
            var heroes = data.units.Where(u => u.stats.isHero).ToList();
            for (int i = 0; i < heroes.Count && i < characterPositions.heroPositions.Length; i++)
            {
                var unit = heroes[i].unit;
                var stats = heroes[i].stats;
                var go = CreateUnitGameObject(unit, stats, true, characterPositions.heroPositions[i]);
                unitGameObjects[unit] = go;
            }
            for (int i = 0; i < data.units.Count(u => !u.stats.isHero) && i < characterPositions.monsterPositions.Length; i++)
            {
                var unit = data.units.Where(u => !u.stats.isHero).ToList()[i].unit;
                var stats = data.units.Where(u => !u.stats.isHero).ToList()[i].stats;
                var go = CreateUnitGameObject(unit, stats, false, characterPositions.monsterPositions[i]);
                unitGameObjects[unit] = go;
            }
        }

        private void HandleUnitAttacking(EventBusSO.AttackData data)
        {
            if (unitGameObjects.TryGetValue(data.attacker, out GameObject attackerGo))
            {
                if (!attackerGo.activeSelf) return;
                var animator = attackerGo.GetComponent<SpriteAnimation>();
                if (animator != null)
                {
                    bool isHero = data.attacker is CharacterStats charStats && charStats.Type == CharacterType.Hero;
                    animator.TiltForward(isHero, combatConfig.CombatSpeed);
                }
            }
        }

        private void HandleUnitDamaged(EventBusSO.DamagePopupData data)
        {
            if (unitGameObjects.TryGetValue(data.unit, out GameObject targetGo))
            {
                if (!targetGo.activeSelf) return;
                var animator = targetGo.GetComponent<SpriteAnimation>();
                if (animator != null)
                    animator.Jiggle(combatConfig.CombatSpeed);
            }
        }

        private void HandleUnitDied(ICombatUnit unit)
        {
            if (unitGameObjects.TryGetValue(unit, out GameObject go))
            {
                var animator = go.GetComponent<SpriteAnimation>();
                if (animator != null)
                    animator.StopAllCoroutines(); // Cancel ongoing animations
                StartCoroutine(DeactivateAfterJiggle(go));
            }
        }

        private void HandleUnitRetreated(ICombatUnit unit)
        {
            if (unitGameObjects.TryGetValue(unit, out GameObject go))
            {
                var animator = go.GetComponent<SpriteAnimation>();
                if (animator != null)
                    animator.StopAllCoroutines(); // Cancel ongoing animations
                StartCoroutine(DeactivateAfterFade(go));
            }
        }

        private IEnumerator DeactivateAfterJiggle(GameObject go)
        {
            yield return new WaitForSeconds(0.3f / combatConfig.CombatSpeed);
            if (go != null)
                go.SetActive(false);
        }

        private IEnumerator DeactivateAfterFade(GameObject go)
        {
            yield return new WaitForSeconds(0.3f / combatConfig.CombatSpeed);
            if (go != null)
                go.SetActive(false);
        }

        private GameObject CreateUnitGameObject(ICombatUnit unit, CharacterStats.DisplayStats stats, bool isHero, Vector3 position)
        {
            var go = new GameObject(stats.name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = stats.combatSprite;
            if (sr.sprite == null)
            {
                Debug.LogWarning($"CombatVisualsComponent: No combat sprite for {stats.name}.");
            }
            sr.sortingLayerName = "Characters";
            sr.sortingOrder = 1;
            go.transform.position = position;
            go.transform.localScale = new Vector3(2f, 2f, 1f);
            go.AddComponent<SpriteAnimation>();
            return go;
        }

        private bool ValidateReferences()
        {
            if (visualConfig == null || eventBus == null || combatConfig == null || characterPositions == null)
            {
                Debug.LogError($"CombatVisualsComponent: Missing references! VisualConfig: {visualConfig != null}, EventBus: {eventBus != null}, CombatConfig: {combatConfig != null}, CharacterPositions: {characterPositions != null}");
                return false;
            }
            return true;
        }
    }
}