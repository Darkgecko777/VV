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
            public Sprite combatSprite; // CombatScene
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
            public string nodeType; // e.g., "Combat", "NonCombat", "Temple"
            public Color highlightColor;
        }

        public List<CharacterVisuals> characterVisuals = new List<CharacterVisuals>
        {
            new CharacterVisuals { characterID = "Fighter", portrait = null, combatSprite = null },
            new CharacterVisuals { characterID = "Healer", portrait = null, combatSprite = null },
            new CharacterVisuals { characterID = "Ranger", portrait = null, combatSprite = null },
            new CharacterVisuals { characterID = "TreasureHunter", portrait = null, combatSprite = null },
            new CharacterVisuals { characterID = "Monk", portrait = null, combatSprite = null },
            new CharacterVisuals { characterID = "Assassin", portrait = null, combatSprite = null },
            new CharacterVisuals { characterID = "Bard", portrait = null, combatSprite = null },
            new CharacterVisuals { characterID = "Barbarian", portrait = null, combatSprite = null }
        };

        public List<EnemyVisuals> enemyVisuals;
        public List<NodeVisuals> nodeVisuals = new List<NodeVisuals>
        {
            new NodeVisuals { nodeType = "Combat", highlightColor = new Color(0.8f, 0.2f, 0.2f) }, // Reddish for Combat
            new NodeVisuals { nodeType = "NonCombat", highlightColor = new Color(0.2f, 0.4f, 0.8f) }, // Bluish for NonCombat
            new NodeVisuals { nodeType = "Temple", highlightColor = Color.white } // White for Temple
        };
        [SerializeField] private Sprite combatBackground; // 512x512 background for Combat scene

        public Sprite GetPortrait(string characterID)
        {
            var visual = characterVisuals.Find(v => v.characterID == characterID);
            if (visual.portrait == null)
            {
                Debug.LogWarning($"VisualConfig.GetPortrait: No portrait found for {characterID}");
            }
            return visual.portrait;
        }

        public Sprite GetCombatSprite(string characterID)
        {
            var visual = characterVisuals.Find(v => v.characterID == characterID);
            if (visual.combatSprite == null)
            {
                Debug.LogWarning($"VisualConfig.GetCombatSprite: No combat sprite found for {characterID}");
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
            if (visual.highlightColor == default)
            {
                Debug.LogWarning($"VisualConfig.GetNodeColor: No color found for nodeType {nodeType}, returning white");
                return Color.white;
            }
            return visual.highlightColor;
        }

        public Sprite GetCombatBackground()
        {
            if (combatBackground == null)
            {
                Debug.LogWarning("VisualConfig.GetCombatBackground: No combat background sprite assigned!");
                // Fallback: Try loading from Resources for prototype
                combatBackground = Resources.Load<Sprite>("CombatBackground");
                if (combatBackground == null)
                {
                    Debug.LogWarning("VisualConfig.GetCombatBackground: Failed to load CombatBackground sprite from Resources/CombatBackground!");
                }
            }
            return combatBackground;
        }
    }
}