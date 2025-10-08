using System.Collections.Generic;
using UnityEngine;

namespace VirulentVentures
{
    public static class VirusLibrary
    {
        private static Dictionary<string, VirusSO> virusCache;

        static VirusLibrary()
        {
            virusCache = new Dictionary<string, VirusSO>();
            var allViruses = Resources.LoadAll<VirusSO>("Viruses");
            foreach (var virus in allViruses)
            {
                if (virus == null) continue;
                virusCache[virus.VirusID] = virus;
            }
            if (virusCache.Count == 0) Debug.LogWarning("VirusLibrary: No VirusSOs found in Resources/Viruses.");
        }

        public static VirusSO GetVirusData(string id)
        {
            if (virusCache.TryGetValue(id, out var data))
            {
                return data;
            }
            Debug.LogWarning($"VirusLibrary: Virus ID {id} not found, returning default");
            var defaultSO = ScriptableObject.CreateInstance<VirusSO>();
            defaultSO.name = "DefaultVirus";
            defaultSO.SetDefaultStats(id, TransmissionVector.Health, "None", -0.1f, 0.05f, "Common");
            return defaultSO;
        }

        public static List<string> GetVirusIds()
        {
            return new List<string>(virusCache.Keys);
        }
    }

    public static class VirusSOExtensions
    {
        public static void SetDefaultStats(this VirusSO so, string id, TransmissionVector vector, string effect, float baseInfectionChance, float effectStrength, string rarity)
        {
            var idField = typeof(VirusSO).GetField("id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var vectorField = typeof(VirusSO).GetField("transmissionVector", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var effectField = typeof(VirusSO).GetField("effect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var chanceField = typeof(VirusSO).GetField("baseInfectionChance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var strengthField = typeof(VirusSO).GetField("effectStrength", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var rarityField = typeof(VirusSO).GetField("rarity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            idField.SetValue(so, id);
            vectorField.SetValue(so, vector);
            effectField.SetValue(so, effect);
            chanceField.SetValue(so, baseInfectionChance);
            strengthField.SetValue(so, effectStrength);
            rarityField.SetValue(so, rarity);
        }
    }
}