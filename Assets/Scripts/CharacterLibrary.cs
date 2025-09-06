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
            public int Immunity;
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
                    immunity: Immunity,
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
                    Attack = 15,
                    Defense = 15,
                    Speed = 5,
                    Evasion = 20,
                    Morale = 100,
                    MaxMorale = 100,
                    Immunity = 20,
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
                    MaxHealth = 80,
                    Attack = 10,
                    Defense = 5,
                    Speed = 3,
                    Evasion = 30,
                    Morale = 100,
                    MaxMorale = 100,
                    Immunity = 50,
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
                    Immunity = 30,
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
                    MaxHealth = 95,
                    Attack = 18,
                    Defense = 14,
                    Speed = 4,
                    Evasion = 25,
                    Morale = 100,
                    MaxMorale = 100,
                    Immunity = 35,
                    AbilityIds = new List<string> { "BasicAttack", "ChiStrike", "InnerFocus" },
                    CanBeCultist = false,
                    PartyPosition = 2
                }
            }
        };

        private static readonly Dictionary<string, CharacterData> MonsterData = new Dictionary<string, CharacterData>
        {
            {
                "BogFiend", new CharacterData
                {
                    Id = "BogFiend",
                    MaxHealth = 70,
                    Attack = 15,
                    Defense = 10,
                    Speed = 4,
                    Evasion = 25,
                    Immunity = 0,
                    AbilityIds = new List<string> { "BasicAttack", "SludgeSlam", "MireGrasp" },
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
                    Immunity = 0,
                    AbilityIds = new List<string> { "TrueStrike", "SpectralDrain", "EtherealWail" },
                    CanBeCultist = false,
                    PartyPosition = 4
                }
            },
            {
                "MireShambler", new CharacterData
                {
                    Id = "MireShambler",
                    MaxHealth = 75,
                    Attack = 12,
                    Defense = 15,
                    Speed = 3,
                    Evasion = 10,
                    Immunity = 0,
                    AbilityIds = new List<string> { "BasicAttack", "ThornNeedle", "Entangle" },
                    CanBeCultist = false,
                    PartyPosition = 2
                }
            },
            {
                "UmbralCorvax", new CharacterData
                {
                    Id = "UmbralCorvax",
                    MaxHealth = 85,
                    Attack = 22,
                    Defense = 8,
                    Speed = 5,
                    Evasion = 30,
                    Immunity = 0,
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
                Immunity = 20,
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
                Immunity = 0,
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