using UnityEngine;
using System.Collections.Generic;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "PlayerProgress", menuName = "VirulentVentures/PlayerProgress", order = 15)]
    public class PlayerProgress : ScriptableObject
    {
        [SerializeField] private List<string> unlockedHeroes = new List<string> { "Fighter", "Healer", "Scout", "Monk" };
        [SerializeField] private int templeRank = 1;
        [SerializeField] private List<string> virusTokens = new List<string>();
        [SerializeField] private List<string> relics = new List<string>();
        [SerializeField] private List<string> artifacts = new List<string>();
        [SerializeField] private int favour = 0;
        [SerializeField] private int totalFavourEarned = 0; // Tracks lifetime favour for meta-progression

        public List<string> UnlockedHeroes => unlockedHeroes;
        public int TempleRank => templeRank;
        public List<string> VirusTokens => virusTokens;
        public List<string> Relics => relics;
        public List<string> Artifacts => artifacts;
        public int Favour => favour;
        public int TotalFavourEarned => totalFavourEarned;

        public void AddFavour(int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"PlayerProgress: Attempted to add negative favour ({amount}), ignoring.");
                return;
            }
            favour += amount;
            totalFavourEarned += amount;
            // Check for temple rank upgrade (every 100 total favour)
            int newRank = 1 + (totalFavourEarned / 100);
            if (newRank > templeRank)
            {
                templeRank = newRank;
                Debug.Log($"PlayerProgress: Temple rank increased to {templeRank}");
            }
            Debug.Log($"PlayerProgress: Added {amount} favour, total: {favour}, lifetime: {totalFavourEarned}");
        }

        public bool SpendFavour(int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"PlayerProgress: Attempted to spend negative favour ({amount}), ignoring.");
                return false;
            }
            if (favour >= amount)
            {
                favour -= amount;
                Debug.Log($"PlayerProgress: Spent {amount} favour, remaining: {favour}");
                return true;
            }
            Debug.LogWarning($"PlayerProgress: Insufficient favour to spend {amount}, current: {favour}");
            return false;
        }

        public void UnlockHero(string heroId)
        {
            if (!unlockedHeroes.Contains(heroId))
            {
                unlockedHeroes.Add(heroId);
                Debug.Log($"PlayerProgress: Unlocked hero {heroId}");
            }
        }

        public void Reset()
        {
            unlockedHeroes.Clear();
            unlockedHeroes.AddRange(new List<string> { "Fighter", "Healer", "Scout", "Monk" });
            templeRank = 1;
            virusTokens.Clear();
            relics.Clear();
            artifacts.Clear();
            favour = 0;
            totalFavourEarned = 0;
            Debug.Log("PlayerProgress: Reset to demo defaults");
        }
    }
}