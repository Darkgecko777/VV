using System.Collections.Generic;
using UnityEngine;

namespace VirulentVentures
{
    public class BattleVisualController : MonoBehaviour
    {
        [SerializeField] private VisualConfig visualConfig;
        private List<(ICombatUnit unit, GameObject go)> units;

        void Awake()
        {
            if (visualConfig == null)
            {
                Debug.LogError("BattleVisualController: Missing VisualConfig!");
            }
        }

        public void InitializeUnits(List<HeroStats> heroStats, List<MonsterStats> monsterStats)
        {
            units = new List<(ICombatUnit, GameObject)>();

            // Create hero GameObjects
            for (int i = 0; i < heroStats.Count; i++)
            {
                if (heroStats[i].Health <= 0) continue;
                GameObject heroObj = new GameObject(heroStats[i].Type.Id);
                heroObj.transform.position = heroStats[i].Position;
                var renderer = heroObj.AddComponent<SpriteRenderer>();
                if (heroStats[i].SO is HeroSO heroSO)
                {
                    Sprite sprite = visualConfig.GetCombatSprite(heroSO.Stats.Type.Id, heroStats[i].Rank);
                    if (sprite != null)
                    {
                        renderer.sprite = sprite;
                    }
                    else
                    {
                        Debug.LogWarning($"BattleVisualController: No combat sprite for {heroSO.Stats.Type.Id}_Rank{heroStats[i].Rank}");
                    }
                }
                renderer.sortingLayerName = "Characters";
                renderer.transform.localScale = new Vector3(2f, 2f, 1f);
                units.Add((heroStats[i], heroObj));
            }

            // Create monster GameObjects
            for (int i = 0; i < monsterStats.Count; i++)
            {
                if (monsterStats[i].Health <= 0) continue;
                GameObject monsterObj = new GameObject(monsterStats[i].Type.Id);
                monsterObj.transform.position = monsterStats[i].Position;
                var renderer = monsterObj.AddComponent<SpriteRenderer>();
                if (monsterStats[i].SO is MonsterSO monsterSO)
                {
                    Sprite sprite = visualConfig.GetEnemySprite(monsterSO.Stats.Type.Id);
                    if (sprite != null)
                    {
                        renderer.sprite = sprite;
                    }
                    else
                    {
                        Debug.LogWarning($"BattleVisualController: No combat sprite for {monsterSO.Stats.Type.Id}");
                    }
                }
                renderer.sortingLayerName = "Characters";
                renderer.transform.localScale = new Vector3(2f, 2f, 1f);
                units.Add((monsterStats[i], monsterObj));
            }
        }

        public void UpdateUnitVisual(ICombatUnit unit)
        {
            var unitEntry = units.Find(u => u.unit == unit);
            if (unitEntry.go != null && unit.Health <= 0)
            {
                Destroy(unitEntry.go);
                units.Remove(unitEntry);
            }
        }
    }
}