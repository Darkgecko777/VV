using UnityEngine;

[System.Serializable]
public struct CharacterStatsData
{
    public enum CharacterType { Fighter, Healer, TreasureHunter, Scout, Mage, Ghoul, Wraith }
    public enum Speed { VerySlow = 5, Slow = 4, Normal = 3, Fast = 2, VeryFast = 1 }

    public CharacterType characterType;
    public Speed speed;
    public int minHealth;
    public int maxHealth;
    public int health;
    public int minAttack;
    public int maxAttack;
    public int attack;
    public int minDefense;
    public int maxDefense;
    public int defense;
    public int morale;
    public int sanity;
    public bool isInfected;
    public bool isCultist;
    public int slowTickDelay;
    public int rank; // 1-3 for stat scaling
}