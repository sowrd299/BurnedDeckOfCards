using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ashworld
{
    public enum Suit
    {
        None,
        
        // Factions
        Cast,
        Dragonic,
        Imperial,
        Vernal,
        Dahannut,

        // Classes
        Knight,
        Ranger,
        Rogue,
        Mystic,
    }

    public enum SpecialAbility
    {
        None,
        Location,
        Unique,
        Boon,
    }

    public enum Requirement
    {
        None,
        Harmony,   // all cards have same rank
        Hierarchy, // sequential ranks
        Might,     // sum of ranks >= opposing
        Fealty,    // all cards same suit
        Heroism,   // free pass for one requirement
    }

    [Serializable]
    public class CardDefinition
    {
        [SerializeField] private string cardName;
        [SerializeField] private string subtitle;
        [SerializeField] private int rank;
        [SerializeField] private List<Suit> suits;
        [SerializeField] private int historyCost;
        [SerializeField] private List<SpecialAbility> abilities;
        [SerializeField] private List<Requirement> lockRequirements;
        [SerializeField] private List<Requirement> holdRequirements;
        [SerializeField] private List<string> subtypes;
        [SerializeField] private string flavorText;
        [SerializeField] private Sprite illustration;

        // Properties
        public string CardName => cardName;
        public string Subtitle => subtitle;
        public int Rank => rank;
        public List<Suit> Suits => suits;
        public int HistoryCost => historyCost;
        public List<SpecialAbility> Abilities => abilities;
        public List<Requirement> LockRequirements => lockRequirements;
        public List<Requirement> HoldRequirements => holdRequirements;
        public Sprite Illustration => illustration;

        // Constructor
        public CardDefinition() {}

        public CardDefinition(string name, int rank, List<Suit> suits, int historyCost = 0,
                    List<SpecialAbility> abilities = null,
                    List<Requirement> lockReqs = null,
                    List<Requirement> holdReqs = null)
        {
            this.cardName = name;
            this.rank = rank;
            this.suits = suits ?? new List<Suit>();
            this.historyCost = historyCost;
            this.abilities = abilities ?? new List<SpecialAbility>();
            this.lockRequirements = lockReqs ?? new List<Requirement>();
            this.holdRequirements = holdReqs ?? new List<Requirement>();
        }

        public bool HasAbility(SpecialAbility ability) => abilities.Contains(ability);

        public static bool MeetsRequirement(Requirement requirement, List<CardDefinition> party, List<CardDefinition> opposingParty = null, bool heroismAvailable = true)
        {
            switch (requirement)
            {
                case Requirement.Harmony:
                    int firstRank = party[0].Rank;
                    return party.TrueForAll(c => c.Rank == firstRank);

                case Requirement.Hierarchy:
                    party.Sort((a, b) => a.Rank.CompareTo(b.Rank));
                    for (int i = 1; i < party.Count; i++)
                        if (party[i].Rank != party[i - 1].Rank + 1) return false;
                    return true;

                case Requirement.Might:
                    if (opposingParty == null) return false;
                    int partySum = 0, oppSum = 0;
                    foreach (var c in party) partySum += c.Rank;
                    foreach (var c in opposingParty) oppSum += c.Rank;
                    return partySum >= oppSum;

                case Requirement.Fealty:
                    if (party.Count == 0) return false;
                    
                    // Fealty: All cards must share at least one suit.
                    // Start with the suits of the first card, and intersect with every other card.
                    HashSet<Suit> commonSuits = new HashSet<Suit>(party[0].Suits);
                    
                    for (int i = 1; i < party.Count; i++) {
                        // IntersectWith modifies the set to contain only elements present in both
                        // We need to convert card.Suits list to IEnumerable for IntersectWith
                        // Note: List<T> implements IEnumerable<T>
                        commonSuits.IntersectWith(party[i].Suits);
                    }
                    
                    return commonSuits.Count > 0;

                case Requirement.Heroism:
                    return heroismAvailable;

                default:
                    return true;
            }
        }
    }
}