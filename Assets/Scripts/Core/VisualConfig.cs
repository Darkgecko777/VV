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
            public string characterID;
            public Sprite portrait;
            public Sprite combatSprite;
        }

        [System.Serializable]
        public struct EnemyVisuals
        {
            public string enemyID;
            public Sprite combatSprite;
        }

        [System.Serializable]
        public struct NodeVisuals
        {
            public string nodeType;
            public Color highlightColor;
        }

        public List<CharacterVisuals> characterVisuals = new List<CharacterVisuals>
        {
            new CharacterVisuals { characterID = "Fighter", portrait = null, combatSprite = null },
            new CharacterVisuals { characterID = "Healer", portrait = null, combatSprite = null },
            new CharacterVisuals { characterID = "Scout", portrait = null, combatSprite = null },
            new CharacterVisuals { characterID = "Monk", portrait = null, combatSprite = null }
        };

        public List<EnemyVisuals> enemyVisuals = new List<EnemyVisuals>
        {
            new EnemyVisuals { enemyID = "Bog Fiend", combatSprite = null },
            new EnemyVisuals { enemyID = "Mire Shambler", combatSprite = null },
            new EnemyVisuals { enemyID = "Umbral Corvax", combatSprite = null },
            new EnemyVisuals { enemyID = "Wraith", combatSprite = null }
        };

        public List<NodeVisuals> nodeVisuals = new List<NodeVisuals>
        {
            new NodeVisuals { nodeType = "Combat", highlightColor = new Color(0.8f, 0.2f, 0.2f) },
            new NodeVisuals { nodeType = "NonCombat", highlightColor = new Color(0.2f, 0.4f, 0.8f) },
            new NodeVisuals { nodeType = "Temple", highlightColor = Color.white }
        };

        [SerializeField] private Sprite combatBackground;

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
            if (visual.combatSprite == null)
            {
                Debug.LogWarning($"VisualConfig.GetEnemySprite: No combat sprite found for {enemyID}");
            }
            return visual.combatSprite;
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