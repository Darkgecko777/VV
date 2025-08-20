using UnityEngine;
using System.Collections.Generic;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "VisualConfig", menuName = "VirulentVentures/VisualConfig", order = 14)]
    public class VisualConfig : ScriptableObject
    {
        [System.Serializable]
        public struct CharacterVisuals
        {
            public string characterID; // e.g., "Fighter", "Healer" from HeroSO.Stats.Type.Id
            public Sprite portrait; // Temple/Expedition
            public Sprite combatSprite; // BattleScene
        }

        [System.Serializable]
        public struct EnemyVisuals
        {
            public string enemyID; // e.g., "Ghoul", "Wraith"
            public Sprite combatSprite;
        }

        [System.Serializable]
        public struct NodeVisuals
        {
            public string nodeType; // e.g., "Combat", "NonCombat", "Active", "Passed"
            public Color highlightColor;
        }

        public List<CharacterVisuals> characterVisuals;
        public List<EnemyVisuals> enemyVisuals;
        public List<NodeVisuals> nodeVisuals;

        public Sprite GetPortrait(string characterID, int rank)
        {
            string tieredID = $"{characterID}_{rank}";
            var visual = characterVisuals.Find(v => v.characterID == tieredID || v.characterID == characterID); // Fallback to base if tiered not found
            if (visual.portrait == null)
            {
                Debug.LogWarning($"VisualConfig.GetPortrait: No portrait found for {tieredID} (fallback {characterID})");
            }
            return visual.portrait;
        }

        public Sprite GetCombatSprite(string characterID, int rank)
        {
            string tieredID = $"{characterID}_{rank}";
            var visual = characterVisuals.Find(v => v.characterID == tieredID || v.characterID == characterID); // Fallback to base if tiered not found
            if (visual.combatSprite == null)
            {
                Debug.LogWarning($"VisualConfig.GetCombatSprite: No combat sprite found for {tieredID} (fallback {characterID})");
            }
            return visual.combatSprite;
        }

        public Sprite GetEnemySprite(string enemyID)
        {
            var visual = enemyVisuals.Find(v => v.enemyID == enemyID);
            return visual.combatSprite != null ? visual.combatSprite : null;
        }

        public Color GetNodeColor(string nodeType)
        {
            var visual = nodeVisuals.Find(v => v.nodeType == nodeType);
            return visual.highlightColor != default ? visual.highlightColor : Color.white;
        }
    }
}