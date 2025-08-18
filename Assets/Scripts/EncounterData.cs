using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EncounterData", menuName = "VirulentVentures/EncounterData", order = 10)]
public class EncounterData : ScriptableObject
{
    [SerializeField] private MonsterSO ghoulSO; // Single GhoulSO reference
    [SerializeField] private MonsterSO wraithSO; // Single WraithSO reference
    [SerializeField] private bool isCombatNode = true; // Always true for prototype
    [SerializeField] private CharacterPositions positions = CharacterPositions.Default();

    public bool IsCombatNode => isCombatNode;

    public List<CharacterRuntimeStats> SpawnMonsters()
    {
        List<CharacterRuntimeStats> monsterStats = new List<CharacterRuntimeStats>();
        if (ghoulSO == null || wraithSO == null)
        {
            return monsterStats;
        }
        if (positions.monsterPositions.Length != 4)
        {
            return monsterStats;
        }

        // Fixed composition: 2 Ghouls, 2 Wraiths
        MonsterSO[] monsterComposition = new MonsterSO[] { ghoulSO, ghoulSO, wraithSO, wraithSO };

        for (int i = 0; i < monsterComposition.Length; i++)
        {
            if (monsterComposition[i] != null)
            {
                GameObject monsterObj = new GameObject(monsterComposition[i].Stats.characterType.ToString());
                monsterObj.transform.position = positions.monsterPositions[i];
                var runtimeStats = monsterObj.AddComponent<CharacterRuntimeStats>();
                var renderer = monsterObj.AddComponent<SpriteRenderer>();
                renderer.sprite = monsterComposition[i].Sprite;
                renderer.sortingLayerName = "Characters";
                renderer.transform.localScale = new Vector3(2f, 2f, 1f);
                runtimeStats.SetCharacterSO(monsterComposition[i]);
                runtimeStats.Initialize();
                monsterStats.Add(runtimeStats);
            }
        }

        return monsterStats;
    }

    // Initialize encounter with fixed 2 Ghouls, 2 Wraiths (for testing)
    public void InitializeRandomEncounter(MonsterSO ghoulSO, MonsterSO wraithSO)
    {
        this.ghoulSO = ghoulSO;
        this.wraithSO = wraithSO;
    }
}