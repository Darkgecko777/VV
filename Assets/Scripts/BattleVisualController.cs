using System.Collections.Generic;
using UnityEngine;

namespace VirulentVentures
{
    public class BattleVisualController : MonoBehaviour
    {
        [SerializeField] private VisualConfig visualConfig;
        [SerializeField] private CharacterPositions characterPositions;
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Camera mainCamera;

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

            mainCamera.orthographic = true;
            mainCamera.transform.rotation = Quaternion.identity;
            mainCamera.transform.position = new Vector3(0f, 0f, -8f);

            units = new List<(ICombatUnit, GameObject, SpriteAnimation)>();
            isInitialized = true;

            if (backgroundSprite != null)
            {
                backgroundObject = new GameObject("BattleBackground");
                var renderer = backgroundObject.AddComponent<SpriteRenderer>();
                renderer.sprite = backgroundSprite;
                renderer.sortingLayerName = "Background";
                renderer.sortingOrder = -10;

                backgroundObject.transform.localScale = new Vector3(2.25f, 0.625f, 1f); // Fit top half (18x5 units, 512x512px at 64 PPU)
                backgroundObject.transform.position = new Vector3(0f, 0f, 0f); // Center at ground level
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

                GameObject unitObj = new GameObject(unit.Id);
                var renderer = unitObj.AddComponent<SpriteRenderer>();
                var animator = unitObj.AddComponent<SpriteAnimation>();
                renderer.sortingLayerName = "Characters";
                renderer.sortingOrder = unit is HeroStats ? heroIndex : monsterIndex + 10;
                renderer.transform.localScale = new Vector3(2f, 2f, 1f);

                if (unit is HeroStats)
                {
                    Sprite sprite = visualConfig.GetCombatSprite(unit.Id);
                    if (sprite != null)
                    {
                        renderer.sprite = sprite;
                    }
                    Vector3 position = heroIndex < heroPositions.Length ? heroPositions[heroIndex] : Vector3.zero;
                    unitObj.transform.position = position;
                    heroIndex++;
                }
                else if (unit is MonsterStats)
                {
                    Sprite sprite = visualConfig.GetEnemySprite(unit.Id);
                    if (sprite != null)
                    {
                        renderer.sprite = sprite;
                    }
                    Vector3 position = monsterIndex < monsterPositions.Length ? monsterPositions[monsterIndex] : Vector3.zero;
                    unitObj.transform.position = position;
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

        public void UpdateUnitVisual(ICombatUnit unit, DisplayStats displayStats)
        {
            if (!isInitialized) return;
            var unitEntry = units.Find(u => u.unit == unit);
            if (unitEntry.go != null)
            {
                if (unit.Health <= 0)
                {
                    unitEntry.animator.Jiggle(false);
                    Destroy(unitEntry.go, 0.5f);
                    units.Remove(unitEntry);
                }
                else if (unit is HeroStats hero && hero.Morale <= 20) // Visual cue for low Morale
                {
                    unitEntry.animator.Jiggle(true); // Shaky sprite for retreat risk
                }
            }
        }

        public void TriggerUnitAnimation(ICombatUnit unit, string message)
        {
            if (!isInitialized) return;
            var unitEntry = units.Find(u => u.unit == unit);
            if (unitEntry.animator != null)
            {
                unitEntry.animator.Jiggle(unit is HeroStats); // Jiggle on damage
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
            if (visualConfig == null || characterPositions == null || mainCamera == null)
            {
                Debug.LogError($"BattleVisualController: Missing references! VisualConfig: {visualConfig != null}, CharacterPositions: {characterPositions != null}, MainCamera: {mainCamera != null}");
                return false;
            }
            if (!mainCamera.orthographic)
            {
                Debug.LogWarning("BattleVisualController: MainCamera is not orthographic, forcing orthographic for 2D setup.");
                mainCamera.orthographic = true;
            }
            return true;
        }

        public List<(ICombatUnit unit, GameObject go, SpriteAnimation animator)> GetUnits()
        {
            return units;
        }
    }
}