using System.Collections.Generic;
using UnityEngine;

namespace Ashworld {

    public class Player {

        private string id;
        public string Id => id;
        private DeckDefinition deckDefinition;
        private QuestDefinition questDefintion;

        private List<Card> deck;
        public List<Card> hand { get; private set; }
        public List<Card> party { get; private set; }
        public List<Card> defense { get; private set; } // The defense AGAINST the player's party
        public List<Card> ashCards { get; private set; }
        public List<Card> historyCards { get; private set; }

        public int chapterInd { get; private set; } = 0;

        public Player (string id, DeckDefinition deckDefinition, QuestDefinition questDefintion) {
            this.id = id;
            this.deckDefinition = deckDefinition;
            this.deck = deckDefinition.GetCards(id);
            this.questDefintion = questDefintion;
            this.hand = new List<Card>();
            this.party = new List<Card>();
            this.defense = new List<Card>();
            this.ashCards = new List<Card>();
            this.historyCards = new List<Card>();
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

        public void MoveToAsh(Card card) {
            this.ashCards.Add(card);
        }

        public void MoveToHistory(Card card) {
            this.historyCards.Add(card);
        }

        public bool CanPayHistoryCost(int cost, Card cardToPlay) {
            // Need enough cards in hand + history to pay logic?
            // "for each point of its story cost, either discard a card from your hand into your history, 
            // or put a card with the same name from your history onto the bottom of your deck."
            
            // Simplified check: Do we have enough potential resources?
            // Check matches in history
            int matchesInHistory = 0;
            foreach(var hCard in historyCards) {
                if(hCard.CardName == cardToPlay.CardName) matchesInHistory++;
            }

            // Available cards in hand (excluding the card itself if needed, though usually you play then pay? Or pay to play?)
            // Assuming cost is paid UPON playing.
            // If paying by discarding hand, we need (HandCount - 1) >= (Cost - matchesInHistoryUsed)
            
            // Ideally we can mix and match. 
            // Max payment possible = matchesInHistory + (hand.Count - (hand.Contains(cardToPlay) ? 1 : 0));
            int cardsInHandAvailable = 0;
            foreach (var hCard in hand) {
                if (hCard == cardToPlay) continue;
                cardsInHandAvailable += (hCard.CardName == cardToPlay.CardName) ? 2 : 1;
            }

            return (matchesInHistory + cardsInHandAvailable) >= cost;
        }

        public void PayHistoryCost(int cost, Card cardToPlay) {
            int remainingCost = cost;

            // 1. Swap from History (Priority)
            // Find cards in history with same name
            for (int i = 0; i < cost; i++) {
                int historyIdx = historyCards.FindLastIndex(c => c.CardName == cardToPlay.CardName);
                if (historyIdx != -1) {
                    Card match = historyCards[historyIdx];
                    historyCards.RemoveAt(historyIdx);
                    deck.Add(match);
                } else {
                    List<Card> candidates = hand.FindAll(c => c != cardToPlay);
                    if (candidates.Count > 0) {
                        Card discard = candidates[Random.Range(0, candidates.Count)];
                        hand.Remove(discard);
                        historyCards.Add(discard);
                    }
                }
            }
        }

        public void StartNextQuestChapter() {
            // Process Party: Check Hold Requirements
            // "Return all cards you own from your party... that you meet the hold requirements of, to your hand."
            // "Discard all ... other cards ... to their owner’s histories."
            
            // Usage of .ToList() to iterate safely while modifying collections
            List<Card> currentParty = new List<Card>(party);
            List<CardDefinition> partyDefs = party.ConvertAll(c => c.Definition);
            List<CardDefinition> defenseDefs = defense.ConvertAll(c => c.Definition);

            this.party.Clear(); // We will re-distribute them

            foreach (var card in currentParty) {
                 bool meetsHold = true;
                 foreach(var req in card.HoldRequirements) {
                     if (!CardDefinition.MeetsRequirement(req, partyDefs, defenseDefs)) {
                         meetsHold = false;
                         break;
                     }
                 }

                 if (card.HoldRequirements.Count > 0 && meetsHold) {
                     this.hand.Add(card);
                 } else {
                     this.historyCards.Add(card);
                 }
            }

            // Process Defense: Ash
            this.ClearDefenseToAsh();

            chapterInd++;

            AddCardForQuestChapterToDefense();
        }

        public void AddCardForQuestChapterToDefense() {
            foreach (Card card in questDefintion.GetCardsForChapter(chapterInd)) {
                AddToDefense(card);
            }
        }

        public bool CanAdvance() {
            // Must equal or exceed number of defenders
            if (this.party.Count < this.defense.Count) return false;

            // Check Locks on Defending Cards
            // "meet all the lock requirements of cards defending against it"
            
            List<CardDefinition> partyDefs = party.ConvertAll(c => c.Definition);
            List<CardDefinition> defenseDefs = defense.ConvertAll(c => c.Definition);

            foreach (var defCard in defense) {
                foreach (var lockReq in defCard.LockRequirements) {
                     if (!CardDefinition.MeetsRequirement(lockReq, partyDefs, defenseDefs)) {
                         return false;
                     }
                }
            }

            return true;
        }

        // Removed ClearPartyToHistory as it's now integrated into StartNextQuestChapter logic
        private void ClearDefenseToAsh() {
            foreach(var card in defense) {
                ashCards.Add(card);
            }
            defense.Clear();
        }
    }

}