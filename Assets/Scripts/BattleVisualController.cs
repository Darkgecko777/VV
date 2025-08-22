using System.Collections.Generic;
using UnityEngine;

namespace VirulentVentures
{
    public class BattleVisualController : MonoBehaviour
    {
        [SerializeField] private VisualConfig visualConfig;
        [SerializeField] private CharacterPositions characterPositions;

        private List<(ICombatUnit unit, GameObject go, SpriteAnimation animator)> units;
        private bool isInitialized;

        void Awake()
        {
            if (visualConfig is null || characterPositions is null)
            {
                isInitialized = false;
                return;
            }
            units = new List<(ICombatUnit, GameObject, SpriteAnimation)>();
            isInitialized = true;
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
                    Sprite sprite = visualConfig.GetCombatSprite(heroSO.Stats.Type.Id); // Removed Rank param; use base sprite
                    if (sprite != null)
                    {
                        renderer.sprite = sprite;
                    }
                    unitObj.transform.position = heroIndex < heroPositions.Length ? heroPositions[heroIndex] : Vector3.zero;
                    heroIndex++;
                }
                else if (unit is MonsterStats monsterStats && monsterStats.SO is MonsterSO monsterSO)
                {
                    Sprite sprite = visualConfig.GetEnemySprite(monsterSO.Stats.Type.Id);
                    if (sprite != null)
                    {
                        renderer.sprite = sprite;
                    }
                    unitObj.transform.position = monsterIndex < monsterPositions.Length ? monsterPositions[monsterIndex] : Vector3.zero;
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
    }
}