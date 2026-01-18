using System.Collections.Generic;
using UnityEngine;

namespace Ashworld {

    public class Player {

        private string id;
        private DeckDefinition deckDefinition;
        private QuestDefinition questDefintion;

        private List<Card> deck;
        public List<Card> hand { get; private set; }
        public List<Card> party { get; private set; }
        public List<Card> defense { get; private set; } // The defense AGAINST the player's party

        public int chapterInd { get; private set; } = 0;

        public Player (string id, DeckDefinition deckDefinition, QuestDefinition questDefintion) {
            this.id = id;
            this.deckDefinition = deckDefinition;
            this.deck = deckDefinition.GetCards(id);
            this.questDefintion = questDefintion;
            this.hand = new List<Card>();
            this.party = new List<Card>();
            this.defense = new List<Card>();
        }

        public void Shuffle () {
            for (int n = deck.Count - 1; n > 1; n--) {  
                int k = Random.Range(0, n);  
                Card value = deck[k];  
                deck[k] = deck[n];  
                deck[n] = value;  
            }  
        }

        public void Draw() {

            if (deck.Count <= 0) {
                return;
            }

            hand.Add(deck[0]);
            deck.RemoveAt(0);
        }

        public void Draw(int n) {
            for (int i = 0; i < n; i++) {
                Draw();
            }
        }

        public Card GetHeroCard() {
            return this.deckDefinition.GetHeroCard(this.id);
        }

        public void AddToParty(Card card) {
            this.party.Add(card);
        }

        public void AddToDefense(Card card) {
            this.defense.Add(card);
        }

        public void StartNextQuestChapter() {
            // TODO: Check for holding
            this.party.Clear(); 
            this.defense.Clear();

            chapterInd++;

            AddCardForQuestChapterToDefense();
        }

        public void AddCardForQuestChapterToDefense() {
            foreach (Card card in questDefintion.GetCardsForChapter(chapterInd)) {
                AddToDefense(card);
            }
        }

        public bool CanAdvance() {
            // TODO: Check locks
            return this.party.Count >= this.defense.Count;
        }
    }

}