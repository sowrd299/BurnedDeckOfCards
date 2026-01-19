using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ashworld
{
    public class Card
    {
        private CardDefinition definition;
        private bool exhausted;
        private string ownerId;  // optional: track which player owns this instance

        // ---- QOL passthroughs to Definition ----
        public string CardName => definition.CardName;
        public int Rank => definition.Rank;
        public List<Suit> Suits => definition.Suits;
        public int HistoryCost => definition.HistoryCost;
        public List<SpecialAbility> Abilities => definition.Abilities;
        public List<Requirement> LockRequirements => definition.LockRequirements;
        public List<Requirement> HoldRequirements => definition.HoldRequirements;

        // Properties
        public CardDefinition Definition => definition;
        public bool IsExhausted => exhausted;
        public string OwnerId => ownerId;

        // Constructor
        public Card(CardDefinition definition, string ownerId = null)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.exhausted = false;
            this.ownerId = ownerId;
        }

        // Methods
        public void Exhaust() => exhausted = true;
        public void Refresh() => exhausted = false;

        public bool HasAbility(SpecialAbility ability) => definition.Abilities.Contains(ability);

        public override string ToString()
        {
            return $"{definition.CardName} (Rank {definition.Rank}, Suits: {string.Join(", ", definition.Suits)})";
        }
        
        public bool IsSameCard(Card other) {
            return this.definition == other.definition;
        }
    }
}
