using System;

namespace VirulentVentures
{
    [Serializable]
    public struct DisplayStats
    {
        public string name;
        public int health;
        public int maxHealth;
        public int attack;
        public int defense;
        public int speed;
        public int evasion;
        public int? morale;
        public int? maxMorale;
        public bool isHero;

        public DisplayStats(string name, int health, int maxHealth, int attack, int defense, int speed, int evasion, int? morale, int? maxMorale, bool isHero)
        {
            this.name = name;
            this.health = health;
            this.maxHealth = maxHealth;
            this.attack = attack;
            this.defense = defense;
            this.speed = speed;
            this.evasion = evasion;
            this.morale = morale;
            this.maxMorale = maxMorale;
            this.isHero = isHero;
        }
    }
}