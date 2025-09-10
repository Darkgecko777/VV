using System.Collections.Generic;
using UnityEngine;

namespace VirulentVentures
{
    public static class CharacterLibrary
    {
        public struct CharacterData
        {
            public string Id;
            public int Health;
            public int MaxHealth;
            public int Attack;
            public int Defense;
            public int Speed;
            public int Evasion;
            public int Morale;
            public int MaxMorale;
            public int Infectivity;
            public List<string> AbilityIds;
            public bool CanBeCultist;
            public int PartyPosition;

            public CharacterStats.DisplayStats GetDisplayStats(bool isHero)
            {
                return new CharacterStats.DisplayStats(
                    name: Id,
                    health: isHero ? Health : MaxHealth,
                    maxHealth: MaxHealth,
                    attack: Attack,
                    defense: Defense,
                    speed: Mathf.Clamp(Speed, 1, 8),
                    evasion: Mathf.Clamp(Evasion, 0, 100),
                    morale: isHero ? Morale : 0,
                    maxMorale: isHero ? MaxMorale : 0,
                    infectivity: Infectivity,
                    isHero: isHero,
                    isInfected: false
                );
            }
        }

        private static readonly Dictionary<string, CharacterData> HeroData = new Dictionary<string, CharacterData>
        {
            {
                "Fighter", new CharacterData
                {
                    Id = "Fighter",
                    Health = 80,
                    MaxHealth = 100,
                    Attack = 14,
                    Defense = 8,
                    Speed = 3,
                    Evasion = 0,
                    Morale = 100,
                    MaxMorale = 100,
                    Infectivity = 40,
                    AbilityIds = new List<string> { "BasicAttack", "ShieldBash", "IronResolve" },
                    CanBeCultist = true,
                    PartyPosition = 1
                }
            },
            {
                "Healer", new CharacterData
                {
                    Id = "Healer",
                    Health = 60,
                    MaxHealth = 60,
                    Attack = 8,
                    Defense = 3,
                    Speed = 4,
                    Evasion = 15,
                    Morale = 100,
                    MaxMorale = 100,
                    Infectivity = 50,
                    AbilityIds = new List<string> { "BasicAttack", "HealerHeal" },
                    CanBeCultist = false,
                    PartyPosition = 4
                }
            },
            {
                "Scout", new CharacterData
                {
                    Id = "Scout",
                    Health = 65,
                    MaxHealth = 75,
                    Attack = 15,
                    Defense = 2,
                    Speed = 5,
                    Evasion = 25,
                    Morale = 100,
                    MaxMorale = 100,
                    Infectivity = 40,
                    AbilityIds = new List<string> { "BasicAttack", "SniperShot" },
                    CanBeCultist = true,
                    PartyPosition = 3
                }
            },
            {
                "Monk", new CharacterData
                {
                    Id = "Monk",
                    Health = 75,
                    MaxHealth = 90,
                    Attack = 18,
                    Defense = 4,
                    Speed = 5,
                    Evasion = 35,
                    Morale = 100,
                    MaxMorale = 100,
                    Infectivity = 70,
                    AbilityIds = new List<string> { "BasicAttack", "ChiStrike", "InnerFocus" },
                    CanBeCultist = false,
                    PartyPosition = 2
                }
            }
        };

        private static readonly Dictionary<string, CharacterData> MonsterData = new Dictionary<string, CharacterData>
        {
            {
                "Bog Fiend", new CharacterData
                {
                    Id = "Bog Fiend",
                    MaxHealth = 80,
                    Attack = 15,
                    Defense = 7,
                    Speed = 4,
                    Evasion = 0,
                    Infectivity = 80,
                    AbilityIds = new List<string> { "BasicAttack", "SludgeSlam", "MireGrasp" },
                    CanBeCultist = false,
                    PartyPosition = 1
                }
            },
            {
                "Wraith", new CharacterData
                {
                    Id = "Wraith",
                    MaxHealth = 50,
                    Attack = 10,
                    Defense = 4,
                    Speed = 6,
                    Evasion = 50,
                    Infectivity = 50,
                    AbilityIds = new List<string> { "TrueStrike", "SpectralDrain", "EtherealWail" },
                    CanBeCultist = false,
                    PartyPosition = 4
                }
            },
            {
                "Mire Shambler", new CharacterData
                {
                    Id = "Mire Shambler",
                    MaxHealth = 75,
                    Attack = 12,
                    Defense = 13,
                    Speed = 3,
                    Evasion = 10,
                    Infectivity = 60,
                    AbilityIds = new List<string> { "BasicAttack", "ThornNeedle", "Entangle", "ViralSpikes" },
                    CanBeCultist = false,
                    PartyPosition = 2
                }
            },
            {
                "Umbral Corvax", new CharacterData
                {
                    Id = "Umbral Corvax",
                    MaxHealth = 60,
                    Attack = 22,
                    Defense = 8,
                    Speed = 5,
                    Evasion = 30,
                    Infectivity = 55,
                    AbilityIds = new List<string> { "BasicAttack", "ShriekOfDespair", "FlocksVigor" },
                    CanBeCultist = false,
                    PartyPosition = 3
                }
            }
        };

        public static CharacterData GetHeroData(string id)
        {
            if (HeroData.TryGetValue(id, out var data))
            {
                return data;
            }
            Debug.LogWarning($"CharacterLibrary: Hero ID {id} not found, returning default");
            return new CharacterData
            {
                Id = id,
                Health = 50,
                MaxHealth = 50,
                Attack = 10,
                Defense = 5,
                Speed = 3,
                Evasion = 10,
                Morale = 100,
                MaxMorale = 100,
                Infectivity = 20,
                AbilityIds = new List<string> { "BasicAttack" },
                CanBeCultist = false,
                PartyPosition = 1
            };
        }

        public static CharacterData GetMonsterData(string id)
        {
            if (MonsterData.TryGetValue(id, out var data))
            {
                return data;
            }
            Debug.LogWarning($"CharacterLibrary: Monster ID {id} not found, returning default");
            return new CharacterData
            {
                Id = id,
                MaxHealth = 50,
                Attack = 10,
                Defense = 5,
                Speed = 3,
                Evasion = 10,
                Infectivity = 0,
                AbilityIds = new List<string> { "BasicAttack" },
                CanBeCultist = false,
                PartyPosition = 0
            };
        }

        public static List<string> GetMonsterIds()
        {
            return new List<string>(MonsterData.Keys);
        }
    }
}