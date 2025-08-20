using System.Collections.Generic;
using UnityEngine;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "EncounterData", menuName = "VirulentVentures/EncounterData", order = 10)]
    public class EncounterData : ScriptableObject
    {
        [SerializeField] private List<MonsterSO> monsterSOs = new List<MonsterSO>();
        [SerializeField] private bool isCombatNode = true; // Always true for prototype
        [SerializeField] private CharacterPositions positions = CharacterPositions.Default();

        public bool IsCombatNode => isCombatNode;

        public List<MonsterStats> SpawnMonsters()
        {
            List<MonsterStats> monsters = new List<MonsterStats>();

            if (monsterSOs == null || monsterSOs.Count == 0 || monsterSOs.Count > 4)
            {
                Debug.LogError($"EncounterData: Invalid monster setup! MonsterSOs count: {monsterSOs?.Count ?? 0}, must be 1-4");
                return monsters;
            }

            if (positions.monsterPositions == null || positions.monsterPositions.Length < monsterSOs.Count)
            {
                Debug.LogError($"EncounterData: Invalid monster positions! Length: {positions.monsterPositions?.Length ?? 0}, Required: {monsterSOs.Count}");
                return monsters;
            }

            for (int i = 0; i < monsterSOs.Count; i++)
            {
                if (monsterSOs[i] == null)
                {
                    Debug.LogWarning($"EncounterData: Null MonsterSO at index {i}");
                    continue;
                }

                GameObject monsterObj = new GameObject($"Monster{i + 1}_{monsterSOs[i].Stats.Type.Id}");
                monsterObj.transform.position = positions.monsterPositions[i];
                var renderer = monsterObj.AddComponent<SpriteRenderer>();
                //renderer.sprite = monsterSOs[i].Sprite;
                renderer.sortingLayerName = "Characters";
                renderer.transform.localScale = new Vector3(2f, 2f, 1f);
                var monsterStats = new MonsterStats(monsterSOs[i], positions.monsterPositions[i]);
                monsterSOs[i].ApplyStats(monsterStats);
                monsters.Add(monsterStats);
            }

            return monsters;
        }

        public void InitializeEncounter(List<MonsterSO> monsterSOs)
        {
            if (monsterSOs == null || monsterSOs.Count < 1 || monsterSOs.Count > 4)
            {
                Debug.LogError($"EncounterData: Invalid monsterSOs count for initialization: {monsterSOs?.Count ?? 0}, must be 1-4");
                return;
            }
            this.monsterSOs = monsterSOs;
        }
    }
}