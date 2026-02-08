using System.Collections.Generic;
using UnityEngine;

namespace Ashworld
{
    public class Deck
    {
        private List<Card> cards;

        public int Count => cards.Count;
        public IReadOnlyList<Card> Cards => cards;

        public Deck()
        {
            cards = new List<Card>();
        }

        public Deck(List<Card> initialCards)
        {
            cards = initialCards ?? new List<Card>();
        }

        public void Shuffle()
        {
            for (int n = cards.Count - 1; n > 1; n--)
            {
                int k = Random.Range(0, n + 1);
                Card value = cards[k];
                cards[k] = cards[n];
                cards[n] = value;
            }
        }

        public Card Draw()
        {
            if (cards.Count == 0) return null;

            Card card = cards[0];
            cards.RemoveAt(0);
            return card;
        }

        public void Add(Card card)
        {
            if (card == null) return;
            cards.Add(card);
        }

        public void Clear()
        {
            cards.Clear();
        }

        public bool Contains(Card card)
        {
            return cards.Contains(card);
        }
    }
}
