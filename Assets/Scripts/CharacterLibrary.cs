using System.Collections.Generic;
using UnityEngine;

namespace VirulentVentures
{
    public static class CharacterLibrary
    {
        public struct CharacterData
        {
            public string Id;
            public int Health; // Only used for heroes
            public int MaxHealth;
            public int Attack;
            public int Defense;
            public int Speed;
            public int Evasion;
            public int Morale; // Only used for heroes
            public int MaxMorale;
            public int Immunity; // New immunity stat
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
                    morale: isHero ? Morale : MaxMorale,
                    maxMorale: MaxMorale,
                    immunity: Immunity, // Add immunity to display stats
                    isHero: isHero
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
                    Attack = 25,
                    Defense = 15,
                    Speed = 5,
                    Evasion = 20,
                    Morale = 100,
                    MaxMorale = 100,
                    Immunity = 20, // Default for Fighter
                    AbilityIds = new List<string> { "BasicAttack", "FighterAttack" },
                    CanBeCultist = true,
                    PartyPosition = 1
                }
            },
            {
                "Healer", new CharacterData
                {
                    Id = "Healer",
                    Health = 60,
                    MaxHealth = 80,
                    Attack = 10,
                    Defense = 5,
                    Speed = 3,
                    Evasion = 30,
                    Morale = 100,
                    MaxMorale = 100,
                    Immunity = 50, // Higher for Healer
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
                    MaxHealth = 85,
                    Attack = 15,
                    Defense = 10,
                    Speed = 6,
                    Evasion = 40,
                    Morale = 100,
                    MaxMorale = 100,
                    Immunity = 30, // Default for Scout
                    AbilityIds = new List<string> { "BasicAttack", "ScoutDefend" },
                    CanBeCultist = true,
                    PartyPosition = 3
                }
            },
            {
                "TreasureHunter", new CharacterData
                {
                    Id = "TreasureHunter",
                    Health = 70,
                    MaxHealth = 90,
                    Attack = 20,
                    Defense = 12,
                    Speed = 4,
                    Evasion = 35,
                    Morale = 100,
                    MaxMorale = 100,
                    Immunity = 25, // Default for TreasureHunter
                    AbilityIds = new List<string> { "BasicAttack", "TreasureFind" },
                    CanBeCultist = false,
                    PartyPosition = 3
                }
            },
            {
                "Monk", new CharacterData
                {
                    Id = "Monk",
                    Health = 75,
                    MaxHealth = 95,
                    Attack = 18,
                    Defense = 14,
                    Speed = 4,
                    Evasion = 25,
                    Morale = 100,
                    MaxMorale = 100,
                    Immunity = 35, // Default for Monk
                    AbilityIds = new List<string> { "BasicAttack" },
                    CanBeCultist = false,
                    PartyPosition = 2
                }
            },
            {
                "Assassin", new CharacterData
                {
                    Id = "Assassin",
                    Health = 60,
                    MaxHealth = 80,
                    Attack = 22,
                    Defense = 8,
                    Speed = 7,
                    Evasion = 45,
                    Morale = 100,
                    MaxMorale = 100,
                    Immunity = 15, // Lower for Assassin
                    AbilityIds = new List<string> { "BasicAttack" },
                    CanBeCultist = true,
                    PartyPosition = 2
                }
            },
            {
                "Bard", new CharacterData
                {
                    Id = "Bard",
                    Health = 55,
                    MaxHealth = 75,
                    Attack = 12,
                    Defense = 6,
                    Speed = 5,
                    Evasion = 30,
                    Morale = 100,
                    MaxMorale = 100,
                    Immunity = 25, // Default for Bard
                    AbilityIds = new List<string> { "BasicAttack" },
                    CanBeCultist = false,
                    PartyPosition = 4
                }
            },
            {
                "Barbarian", new CharacterData
                {
                    Id = "Barbarian",
                    Health = 85,
                    MaxHealth = 105,
                    Attack = 28,
                    Defense = 10,
                    Speed = 4,
                    Evasion = 15,
                    Morale = 100,
                    MaxMorale = 100,
                    Immunity = 10, // Lower for Barbarian
                    AbilityIds = new List<string> { "BasicAttack" },
                    CanBeCultist = true,
                    PartyPosition = 1
                }
            }
        };

        private static readonly Dictionary<string, CharacterData> MonsterData = new Dictionary<string, CharacterData>
        {
            {
                "Bog Fiend", new CharacterData
                {
                    Id = "Bog Fiend",
                    MaxHealth = 70,
                    Attack = 15,
                    Defense = 10,
                    Speed = 4,
                    Evasion = 25,
                    MaxMorale = 80,
                    Immunity = 0, // Monsters default to 0
                    AbilityIds = new List<string> { "BasicAttack", "Bog FiendClaw" },
                    CanBeCultist = false,
                    PartyPosition = 1
                }
            },
            {
                "Wraith", new CharacterData
                {
                    Id = "Wraith",
                    MaxHealth = 80,
                    Attack = 18,
                    Defense = 5,
                    Speed = 6,
                    Evasion = 40,
                    MaxMorale = 90,
                    Immunity = 0, // Monsters default to 0
                    AbilityIds = new List<string> { "BasicAttack", "WraithStrike" },
                    CanBeCultist = false,
                    PartyPosition = 4
                }
            },
            {
                "Skeleton", new CharacterData
                {
                    Id = "Skeleton",
                    MaxHealth = 75,
                    Attack = 12,
                    Defense = 15,
                    Speed = 3,
                    Evasion = 10,
                    MaxMorale = 70,
                    Immunity = 0, // Monsters default to 0
                    AbilityIds = new List<string> { "BasicAttack", "SkeletonSlash" },
                    CanBeCultist = false,
                    PartyPosition = 2
                }
            },
            {
                "Vampire", new CharacterData
                {
                    Id = "Vampire",
                    MaxHealth = 85,
                    Attack = 22,
                    Defense = 8,
                    Speed = 5,
                    Evasion = 30,
                    MaxMorale = 85,
                    Immunity = 0, // Monsters default to 0
                    AbilityIds = new List<string> { "BasicAttack", "VampireBite" },
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
                Immunity = 20, // Default immunity for hero
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
                MaxMorale = 80,
                Immunity = 0, // Monsters default to 0
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