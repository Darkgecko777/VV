using UnityEngine;
using System.Collections.Generic;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "PlayerProgress", menuName = "VirulentVentures/PlayerProgress", order = 15)]
    public class PlayerProgress : ScriptableObject
    {
        [SerializeField] private List<string> unlockedHeroes = new List<string> { "Fighter", "Healer", "Scout", "Monk" }; // Demo defaults
        [SerializeField] private int templeRank = 1; // Placeholder for meta-progression
        [SerializeField] private List<string> virusTokens = new List<string>(); // Placeholder for virus crafting
        [SerializeField] private List<string> relics = new List<string>(); // Placeholder for relics
        [SerializeField] private List<string> artifacts = new List<string>(); // Placeholder for artifacts
        [SerializeField] private int favour = 0; // Placeholder for currency

        public List<string> UnlockedHeroes => unlockedHeroes;
        public int TempleRank => templeRank;
        public List<string> VirusTokens => virusTokens;
        public List<string> Relics => relics;
        public List<string> Artifacts => artifacts;
        public int Favour => favour;

        // For full release: Add hero unlock method
        public void UnlockHero(string heroId)
        {
            if (!unlockedHeroes.Contains(heroId))
            {
                unlockedHeroes.Add(heroId);
                Debug.Log($"PlayerProgress: Unlocked hero {heroId}");
            }
        }

        // Reset for demo or new game
        public void Reset()
        {
            unlockedHeroes.Clear();
            unlockedHeroes.AddRange(new List<string> { "Fighter", "Healer", "Scout", "Monk" });
            templeRank = 1;
            virusTokens.Clear();
            relics.Clear();
            artifacts.Clear();
            favour = 0;
            Debug.Log("PlayerProgress: Reset to demo defaults");
        }
    }
}