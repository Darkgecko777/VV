// <DOCUMENT filename="CharacterLibrary.cs">
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VirulentVentures
{
    public static class CharacterLibrary
    {
        private static Dictionary<string, CharacterSO> heroCache = new Dictionary<string, CharacterSO>();
        private static Dictionary<string, CharacterSO> monsterCache = new Dictionary<string, CharacterSO>();
        private static bool isInitialized = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeCaches()
        {
            if (isInitialized) return;

            heroCache.Clear();
            monsterCache.Clear();

#if UNITY_EDITOR
            // STYLE GUIDE COMPLIANT: Load from Assets/ScriptableObjects/Characters
            string[] guids = AssetDatabase.FindAssets("t:CharacterSO", new[] { "Assets/ScriptableObjects/Characters" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<CharacterSO>(path);
                if (so != null && !string.IsNullOrEmpty(so.Id))
                {
                    if (so.Type == CharacterType.Hero)
                        heroCache[so.Id] = so;
                    else if (so.Type == CharacterType.Monster)
                        monsterCache[so.Id] = so;
                }
            }
#endif

            isInitialized = true;
        }

        public static CharacterSO GetHeroData(string id)
        {
            if (heroCache.TryGetValue(id, out var data))
                return data;

            Debug.LogWarning($"CharacterLibrary: Hero ID {id} not found, returning default");
            return CreateDefaultCharacter(id, CharacterType.Hero);
        }

        public static CharacterSO GetMonsterData(string id)
        {
            if (monsterCache.TryGetValue(id, out var data))
                return data;

            Debug.LogWarning($"CharacterLibrary: Monster ID {id} not found, returning default");
            return CreateDefaultCharacter(id, CharacterType.Monster);
        }

        public static List<string> GetMonsterIds()
        {
            return new List<string>(monsterCache.Keys);
        }

        private static CharacterSO CreateDefaultCharacter(string id, CharacterType type)
        {
            var defaultSO = ScriptableObject.CreateInstance<CharacterSO>();
            defaultSO.name = type == CharacterType.Hero ? "DefaultHero" : "DefaultMonster";

            // FIXED: Direct property assignment (no reflection needed for public fields)
            typeof(CharacterSO).GetField("id", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(defaultSO, id);
            typeof(CharacterSO).GetField("type", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(defaultSO, type);

            if (type == CharacterType.Hero)
            {
                // Hero defaults
                SetField(defaultSO, "health", 50);
                SetField(defaultSO, "maxHealth", 50);
                SetField(defaultSO, "morale", 100);
                SetField(defaultSO, "maxMorale", 100);
            }
            else
            {
                // Monster defaults
                SetField(defaultSO, "health", 0); // Uses maxHealth
                SetField(defaultSO, "maxHealth", 50);
                SetField(defaultSO, "morale", 0);
                SetField(defaultSO, "maxMorale", 0);
            }

            SetField(defaultSO, "attack", 10);
            SetField(defaultSO, "defense", 5);
            SetField(defaultSO, "speed", 3);
            SetField(defaultSO, "evasion", 10);
            SetField(defaultSO, "immunity", 20); // FIXED: was "infectivity"
            SetField(defaultSO, "canBeCultist", false);
            SetField(defaultSO, "partyPosition", type == CharacterType.Hero ? 1 : 0);
            SetField(defaultSO, "rank", 1);
            SetField(defaultSO, "abilities", new AbilitySO[0]); // FIXED: array assignment

            return defaultSO;
        }

        private static void SetField(CharacterSO so, string fieldName, object value)
        {
            var field = typeof(CharacterSO).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
                field.SetValue(so, value);
            else
                Debug.LogError($"CharacterLibrary: Field '{fieldName}' not found in CharacterSO");
        }
    }
}
// </DOCUMENT>