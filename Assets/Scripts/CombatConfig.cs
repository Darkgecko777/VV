using UnityEngine;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "CombatConfig", menuName = "VirulentVentures/CombatConfig")]
    public class CombatConfig : ScriptableObject
    {
        public float CombatSpeed = 1f;
        public float MinCombatSpeed = 0.5f;
        public float MaxCombatSpeed = 4f;
        public int RetreatMoraleThreshold = 20;
    }
}