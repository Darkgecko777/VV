using System;
using UnityEngine;

namespace VirulentVentures
{
    [Serializable]
    public struct TargetingRule
    {
        public enum RuleType
        {
            Random, // Select randomly from valid targets
            LowestHealth, // Select unit with lowest health
            HighestHealth, // Select unit with highest health
            LowestMorale, // Select unit with lowest morale
            HighestMorale, // Select unit with highest morale
            LowestAttack, // Select unit with lowest attack
            HighestAttack, // Select unit with highest attack
            AllAllies // Select all valid allies (including self)
        }

        public RuleType Type;
        public ConditionTarget Target; // User, Ally, Enemy
        public bool MustBeInfected; // Optional: Target must be infected
        public bool MustNotBeInfected; // Optional: Target must not be infected
        public bool MeleeOnly; // Optional: Restrict to CombatPosition 1-2 (overrides Effect.Melee)

        public void Validate()
        {
            if (MustBeInfected && MustNotBeInfected)
            {
                Debug.LogWarning("TargetingRule: MustBeInfected and MustNotBeInfected cannot both be true.");
                MustBeInfected = false;
                MustNotBeInfected = false;
            }
            if (Type == RuleType.AllAllies && Target != ConditionTarget.Ally)
            {
                Debug.LogWarning("TargetingRule: AllAllies rule requires Target = Ally.");
                Target = ConditionTarget.Ally;
            }
        }
    }
}