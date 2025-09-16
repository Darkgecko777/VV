using System.Collections.Generic;
using UnityEngine;

namespace VirulentVentures
{
    public static class CharacterLibrary
    {
        private static Dictionary<string, CharacterSO> heroCache;
        private static Dictionary<string, CharacterSO> monsterCache;

        static CharacterLibrary()
        {
            heroCache = new Dictionary<string, CharacterSO>();
            monsterCache = new Dictionary<string, CharacterSO>();
            var allCharacters = Resources.LoadAll<CharacterSO>("Characters");
            foreach (var character in allCharacters)
            {
                if (character == null) continue;
                if (character.Type == CharacterType.Hero)
                {
                    heroCache[character.Id] = character;
                }
                else
                {
                    monsterCache[character.Id] = character;
                }
            }
            if (heroCache.Count == 0) Debug.LogWarning("CharacterLibrary: No hero CharacterSOs found in Resources/Characters.");
            if (monsterCache.Count == 0) Debug.LogWarning("CharacterLibrary: No monster CharacterSOs found in Resources/Characters.");
        }

        public static CharacterSO GetHeroData(string id)
        {
            if (heroCache.TryGetValue(id, out var data))
            {
                return data;
            }
            Debug.LogWarning($"CharacterLibrary: Hero ID {id} not found, returning default");
            var defaultSO = ScriptableObject.CreateInstance<CharacterSO>();
            defaultSO.name = "DefaultHero";
            // Set default stats to prevent null refs, including rank: 1
            defaultSO.SetDefaultStats(id, CharacterType.Hero, 50, 50, 10, 5, 3, 10, 100, 100, 20, false, 1, 1, new AbilitySO[0]);
            return defaultSO;
        }

        public static CharacterSO GetMonsterData(string id)
        {
            if (monsterCache.TryGetValue(id, out var data))
            {
                return data;
            }
            Debug.LogWarning($"CharacterLibrary: Monster ID {id} not found, returning default");
            var defaultSO = ScriptableObject.CreateInstance<CharacterSO>();
            defaultSO.name = "DefaultMonster";
            defaultSO.SetDefaultStats(id, CharacterType.Monster, 0, 50, 10, 5, 3, 10, 0, 0, 0, false, 0, 1, new AbilitySO[0]); // Added rank: 1
            return defaultSO;
        }

        public static List<string> GetMonsterIds()
        {
            return new List<string>(monsterCache.Keys);
        }
    }

    // Extension to set default stats via reflection (since fields are private)
    public static class CharacterSOExtensions
    {
        public static void SetDefaultStats(this CharacterSO so, string id, CharacterType type, int health, int maxHealth, int attack, int defense, int speed, int evasion, int morale, int maxMorale, int infectivity, bool canBeCultist, int partyPosition, int rank, AbilitySO[] abilities)
        {
            var idField = typeof(CharacterSO).GetField("id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var typeField = typeof(CharacterSO).GetField("type", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var healthField = typeof(CharacterSO).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxHealthField = typeof(CharacterSO).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var attackField = typeof(CharacterSO).GetField("attack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var defenseField = typeof(CharacterSO).GetField("defense", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var speedField = typeof(CharacterSO).GetField("speed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var evasionField = typeof(CharacterSO).GetField("evasion", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var moraleField = typeof(CharacterSO).GetField("morale", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxMoraleField = typeof(CharacterSO).GetField("maxMorale", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var infectivityField = typeof(CharacterSO).GetField("infectivity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var canBeCultistField = typeof(CharacterSO).GetField("canBeCultist", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var partyPositionField = typeof(CharacterSO).GetField("partyPosition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var rankField = typeof(CharacterSO).GetField("rank", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); // Added rank field reflection
            var abilitiesField = typeof(CharacterSO).GetField("abilities", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            idField.SetValue(so, id);
            typeField.SetValue(so, type);
            healthField.SetValue(so, health);
            maxHealthField.SetValue(so, maxHealth);
            attackField.SetValue(so, attack);
            defenseField.SetValue(so, defense);
            speedField.SetValue(so, speed);
            evasionField.SetValue(so, evasion);
            moraleField.SetValue(so, morale);
            maxMoraleField.SetValue(so, maxMorale);
            infectivityField.SetValue(so, infectivity);
            canBeCultistField.SetValue(so, canBeCultist);
            partyPositionField.SetValue(so, partyPosition);
            rankField.SetValue(so, rank);
            abilitiesField.SetValue(so, abilities);
        }
    }
}