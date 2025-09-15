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
        [SerializeField] private int totalFavourEarned = 0;

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
            int newRank = 1 + (totalFavourEarned / 100);
            if (newRank > templeRank)
            {
                templeRank = newRank;
            }
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
            }
        }

        public void CopyFrom(PlayerProgress other)
        {
            if (other == null)
            {
                Debug.LogWarning("PlayerProgress: Cannot copy from null PlayerProgress, resetting.");
                Reset();
                return;
            }
            unlockedHeroes.Clear();
            unlockedHeroes.AddRange(other.unlockedHeroes);
            templeRank = other.templeRank;
            virusTokens.Clear();
            virusTokens.AddRange(other.virusTokens);
            relics.Clear();
            relics.AddRange(other.relics);
            artifacts.Clear();
            artifacts.AddRange(other.artifacts);
            favour = other.favour;
            totalFavourEarned = other.totalFavourEarned;
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
        }
    }
}