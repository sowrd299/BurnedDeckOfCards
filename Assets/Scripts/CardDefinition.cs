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

    public enum AbilityTrigger
    {
        None,
        WhenPlayed,
    }

    public enum AbilityEffect
    {
        None,
        Draw1,
        Draw2,
    }

    [Serializable]
    public class TriggeredAbility
    {
        public AbilityTrigger trigger;
        public AbilityEffect effect;
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
        [SerializeField] private List<TriggeredAbility> triggeredAbilities;
        [SerializeField] private List<Requirement> lockRequirements;
        [SerializeField] private List<Requirement> holdRequirements;
        [SerializeField] private List<string> subtypes;
        [SerializeField] private string flavorText;
        [SerializeField] private Sprite illustration;

        // Properties
        public string CardName => cardName;
        public string Subtitle => subtitle;
        public List<string> Subtypes => subtypes;
        public int Rank => rank;
        public List<Suit> Suits => suits;
        public int HistoryCost => historyCost;
        public List<SpecialAbility> Abilities => abilities;
        public List<TriggeredAbility> TriggeredAbilities => triggeredAbilities;
        public List<Requirement> LockRequirements => lockRequirements;
        public List<Requirement> HoldRequirements => holdRequirements;
        public Sprite Illustration => illustration;

        // Constructor
        public CardDefinition() {}

        public CardDefinition(string name, int rank, List<Suit> suits, int historyCost = 0,
                    List<SpecialAbility> abilities = null,
                    List<Requirement> lockReqs = null,
                    List<Requirement> holdReqs = null,
                    List<TriggeredAbility> triggeredAbilities = null)
        {
            this.cardName = name;
            this.rank = rank;
            this.suits = suits ?? new List<Suit>();
            this.historyCost = historyCost;
            this.abilities = abilities ?? new List<SpecialAbility>();
            this.triggeredAbilities = triggeredAbilities ?? new List<TriggeredAbility>();
            this.lockRequirements = lockReqs ?? new List<Requirement>();
            this.holdRequirements = holdReqs ?? new List<Requirement>();
        }

        public bool HasAbility(SpecialAbility ability) => abilities.Contains(ability);

        public bool CanBoonApply(CardDefinition boon)
        {
            if (boon == null || !boon.HasAbility(SpecialAbility.Boon)) return false;
            foreach (var s in boon.Suits)
            {
                if (s == Suit.None) continue;
                if (!this.suits.Contains(s)) return false;
            }
            return true;
        }

        public static bool MeetsRequirement(Requirement requirement, List<CardDefinition> party, List<CardDefinition> opposingParty = null, bool heroismAvailable = true)
        {
            if (party.Count == 0 && requirement != Requirement.None) return false;

            switch (requirement)
            {
                case Requirement.Harmony:
                case Requirement.Hierarchy:
                    // Boons can boost non-boons. We need to find if any assignment of boons to non-boons satisfies the requirement.
                    List<CardDefinition> nonBoons = party.FindAll(c => !c.HasAbility(SpecialAbility.Boon));
                    List<CardDefinition> boons = party.FindAll(c => c.HasAbility(SpecialAbility.Boon));

                    if (nonBoons.Count == 0) return false; // Requirements apply to cards (non-boons)

                    return CanSatisfyAssignment(requirement, nonBoons, boons);

                case Requirement.Might:
                    if (opposingParty == null) return false;
                    return GetZoneSum(party) >= GetZoneSum(opposingParty);

                case Requirement.Fealty:
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

        private static bool CanSatisfyAssignment(Requirement req, List<CardDefinition> nonBoons, List<CardDefinition> boons, int[] boosts = null, int boonIndex = 0)
        {
            if (boosts == null) boosts = new int[nonBoons.Count];

            if (boonIndex >= boons.Count)
            {
                // Evaluation
                List<int> ranks = new List<int>();
                for (int i = 0; i < nonBoons.Count; i++)
                {
                    ranks.Add(nonBoons[i].Rank + boosts[i]);
                }

                if (req == Requirement.Harmony)
                {
                    int first = ranks[0];
                    return ranks.TrueForAll(r => r == first);
                }
                else if (req == Requirement.Hierarchy)
                {
                    ranks.Sort();
                    for (int i = 1; i < ranks.Count; i++)
                    {
                        if (ranks[i] != ranks[i - 1] + 1) return false;
                    }
                    return true;
                }
                return false;
            }

            CardDefinition currentBoon = boons[boonIndex];
            bool canApply = false;

            // Try assigning this boon to each applicable non-boon
            for (int i = 0; i < nonBoons.Count; i++)
            {
                if (nonBoons[i].CanBoonApply(currentBoon))
                {
                    canApply = true;
                    boosts[i] += currentBoon.Rank;
                    if (CanSatisfyAssignment(req, nonBoons, boons, boosts, boonIndex + 1)) return true;
                    boosts[i] -= currentBoon.Rank;
                }
            }
            
            if (!canApply) return CanSatisfyAssignment(req, nonBoons, boons, boosts, boonIndex + 1);

            return false;
        }

        private static int GetZoneSum(List<CardDefinition> zone)
        {
            int sum = 0;
            List<CardDefinition> nonBoons = zone.FindAll(c => !c.HasAbility(SpecialAbility.Boon));
            List<CardDefinition> boons = zone.FindAll(c => c.HasAbility(SpecialAbility.Boon));

            foreach (var nb in nonBoons) sum += nb.Rank;
            foreach (var b in boons)
            {
                if (nonBoons.Exists(nb => nb.CanBoonApply(b)))
                {
                    sum += b.Rank;
                }
            }
            return sum;
        }
    }
}