using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ashworld
{
    [Serializable]
    public class DeckEntry
    {
        [SerializeField] private CardDefinitionAsset cardDefinitionAsset;
        [SerializeField, Min(1)] private int count = 1;

        public CardDefinitionAsset CardDefinitionAsset => cardDefinitionAsset;
        public int Count => count;
    }

    [Serializable]
    public class DeckDefinition
    {
        [SerializeField] private CardDefinitionAsset heroCard;
        [SerializeField] private List<DeckEntry> entries = new List<DeckEntry>();

        public IReadOnlyList<DeckEntry> Entries => entries;

        public Card GetHeroCard(string ownerId = null) {
            return new Card(heroCard.Definition, ownerId);
        }

        /// <summary>
        /// Expands this deck definition into a full list of Card instances.
        /// </summary>
        public List<Card> GetCards(string ownerId = null)
        {
            var cards = new List<Card>();

            foreach (var entry in entries)
            {
                if (entry.CardDefinitionAsset == null || entry.CardDefinitionAsset.Definition == null)
                    continue;

                for (int i = 0; i < entry.Count; i++)
                {
                    var card = new Card(entry.CardDefinitionAsset.Definition, ownerId);
                    cards.Add(card);
                }
            }

            return cards;
        }
    }
}
