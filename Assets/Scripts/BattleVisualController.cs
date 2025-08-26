using System.Collections.Generic;
using UnityEngine;

namespace VirulentVentures
{
    public class BattleVisualController : MonoBehaviour
    {
        [SerializeField] private VisualConfig visualConfig;
        [SerializeField] private CharacterPositions characterPositions;
        [SerializeField] private Sprite backgroundSprite; // 512x512 at 64 PPU
        [SerializeField] private Camera mainCamera; // Reference to the main camera

        private List<(ICombatUnit unit, GameObject go, SpriteAnimation animator)> units;
        private bool isInitialized;
        private GameObject backgroundObject;

        void Awake()
        {
            if (!ValidateReferences())
            {
                isInitialized = false;
                return;
            }

            // Ensure camera is orthographic and not rotated for 2D setup
            mainCamera.orthographic = true;
            mainCamera.transform.rotation = Quaternion.identity;
            mainCamera.transform.position = new Vector3(0f, 0f, -8f); // Maintain Z=-8

            units = new List<(ICombatUnit, GameObject, SpriteAnimation)>();
            isInitialized = true;

            // Log CharacterPositions arrays for debugging
            Debug.Log($"BattleVisualController: heroPositions.Length = {characterPositions.heroPositions.Length}");
            for (int i = 0; i < characterPositions.heroPositions.Length; i++)
            {
                Debug.Log($"BattleVisualController: heroPositions[{i}] = {characterPositions.heroPositions[i]}");
            }
            Debug.Log($"BattleVisualController: monsterPositions.Length = {characterPositions.monsterPositions.Length}");
            for (int i = 0; i < characterPositions.monsterPositions.Length; i++)
            {
                Debug.Log($"BattleVisualController: monsterPositions[{i}] = {characterPositions.monsterPositions[i]}");
            }

            // Create background programmatically
            if (backgroundSprite != null)
            {
                backgroundObject = new GameObject("BattleBackground");
                var renderer = backgroundObject.AddComponent<SpriteRenderer>();
                renderer.sprite = backgroundSprite;
                renderer.sortingLayerName = "Background";
                renderer.sortingOrder = -10; // Behind all other elements

                // Set hard-coded scale and position
                backgroundObject.transform.localScale = new Vector3(2.4f, 1f, 1f); // Scale: 2.4X, 1Y
                backgroundObject.transform.position = new Vector3(0f, 1f, 0f); // Position: Y=1, Z=0 for 2D
            }
            else
            {
                Debug.LogWarning("BattleVisualController: No backgroundSprite assigned!");
            }
        }

        public void InitializeUnits(List<(ICombatUnit unit, GameObject go)> combatUnits)
        {
            if (!isInitialized) return;

            units.Clear();
            var heroPositions = characterPositions.heroPositions;
            var monsterPositions = characterPositions.monsterPositions;

            int heroIndex = 0;
            int monsterIndex = 0;

            foreach (var (unit, _) in combatUnits)
            {
                if (unit.Health <= 0) continue;

                GameObject unitObj = new GameObject(unit.Type.Id);
                var renderer = unitObj.AddComponent<SpriteRenderer>();
                var animator = unitObj.AddComponent<SpriteAnimation>();
                renderer.sortingLayerName = "Characters";
                renderer.sortingOrder = unit is HeroStats ? heroIndex : monsterIndex + 10;
                renderer.transform.localScale = new Vector3(2f, 2f, 1f);

                if (unit is HeroStats heroStats && heroStats.SO is HeroSO heroSO)
                {
                    Sprite sprite = visualConfig.GetCombatSprite(heroSO.Stats.Type.Id);
                    if (sprite != null)
                    {
                        renderer.sprite = sprite;
                    }
                    Vector3 position = heroIndex < heroPositions.Length ? heroPositions[heroIndex] : Vector3.zero;
                    unitObj.transform.position = position;
                    Debug.Log($"BattleVisualController: Placing hero {unit.Type.Id} at index {heroIndex} with position {position}");
                    heroIndex++;
                }
                else if (unit is MonsterStats monsterStats && monsterStats.SO is MonsterSO monsterSO)
                {
                    Sprite sprite = visualConfig.GetEnemySprite(monsterSO.Stats.Type.Id);
                    if (sprite != null)
                    {
                        renderer.sprite = sprite;
                    }
                    Vector3 position = monsterIndex < monsterPositions.Length ? monsterPositions[monsterIndex] : Vector3.zero;
                    unitObj.transform.position = position;
                    Debug.Log($"BattleVisualController: Placing monster {unit.Type.Id} at index {monsterIndex} with position {position}");
                    monsterIndex++;
                }

                units.Add((unit, unitObj, animator));
            }
        }

        public void SubscribeToModel(CombatModel model)
        {
            if (!isInitialized) return;
            model.OnUnitUpdated += UpdateUnitVisual;
            model.OnDamagePopup += TriggerUnitAnimation;
        }

        public void UpdateUnitVisual(ICombatUnit unit)
        {
            if (!isInitialized) return;
            var unitEntry = units.Find(u => u.unit == unit);
            if (unitEntry.go != null && unit.Health <= 0)
            {
                unitEntry.animator.Jiggle(false);
                Destroy(unitEntry.go, 0.5f);
                units.Remove(unitEntry);
            }
        }

        public void TriggerUnitAnimation(ICombatUnit unit, string message)
        {
            if (!isInitialized) return;
            var unitEntry = units.Find(u => u.unit == unit);
            if (unitEntry.animator != null)
            {
                unitEntry.animator.Jiggle(unit is HeroStats);
            }
        }

        void OnDestroy()
        {
            if (backgroundObject != null)
            {
                Destroy(backgroundObject);
            }
        }

        private bool ValidateReferences()
        {
            if (visualConfig == null || characterPositions == null || mainCamera == null || backgroundSprite == null)
            {
                Debug.LogError($"BattleVisualController: Missing references! VisualConfig: {visualConfig != null}, CharacterPositions: {characterPositions != null}, MainCamera: {mainCamera != null}, BackgroundSprite: {backgroundSprite != null}");
                return false;
            }
            if (!mainCamera.orthographic)
            {
                Debug.LogWarning("BattleVisualController: MainCamera is not orthographic, forcing orthographic for 2D setup.");
                mainCamera.orthographic = true;
            }
            return true;
        }
    }
}