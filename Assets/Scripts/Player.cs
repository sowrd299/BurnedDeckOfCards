using System.Collections.Generic;
using UnityEngine;

namespace Ashworld {

    public class Player {

        private string id;
        public string Id => id;
        private DeckDefinition deckDefinition;
        public QuestDefinition questDefinition { get; private set; }

        private Deck deck;
        public List<Card> hand { get; private set; }
        public List<Card> party { get; private set; }
        public List<Card> defense { get; private set; } // The defense AGAINST the player's party
        public List<Card> ashCards { get; private set; }
        public List<Card> historyCards { get; private set; }
        public Deck chapterTreasureDeck { get; private set; }

        public int chapterInd { get; private set; } = 0;
        public bool HeroismAvailable { get; set; } = true;

        public void ResetHeroism() {
            HeroismAvailable = true;
        }

        public Player (string id, DeckDefinition deckDefinition, QuestDefinition questDefintion) {
            this.id = id;
            this.deckDefinition = deckDefinition;
            this.deck = deckDefinition.CreateDeck(id);
            this.questDefinition = questDefintion;
            this.hand = new List<Card>();
            this.party = new List<Card>();
            this.defense = new List<Card>();
            this.ashCards = new List<Card>();
            this.historyCards = new List<Card>();

            InitializeChapterTreasureDeck();
        }

        private void InitializeChapterTreasureDeck() {
            this.chapterTreasureDeck = questDefinition.GetTreasureDeckForChapter(chapterInd, id);
        }

        public void Shuffle () {
            deck.Shuffle();
        }

        public void Draw() {
            Card card = deck.Draw();
            if (card != null) {
                AddToHand(card);
            }
        }

        public void AddToHand(Card card) {
            card.Refresh();
            if (!hand.Contains(card)) {
                hand.Add(card);
            }
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

        public List<Card> PayHistoryCost(int cost, Card cardToPlay) {
            List<Card> discards = new List<Card>();
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
                        // Priority: Prefer non-hero cards
                        Card heroCard = GetHeroCard();
                        List<Card> nonHeroCandidates = candidates.FindAll(c => !c.IsSameCard(heroCard));
                        
                        Card discard = null;
                        if (nonHeroCandidates.Count > 0) {
                            discard = nonHeroCandidates[Random.Range(0, nonHeroCandidates.Count)];
                        } else {
                            discard = candidates[Random.Range(0, candidates.Count)];
                        }

                        hand.Remove(discard);
                        historyCards.Add(discard);
                        discards.Add(discard);
                    }
                }
            }
            return discards;
        }

        public bool TryHoldCard(Card card, List<CardDefinition> partyContext, List<CardDefinition> defenseContext) {
            if (card.HoldRequirements.Count == 0) return false;

            bool meetsHold = true;
            bool usedHeroism = false;
            foreach (var req in card.HoldRequirements) {
                if (!CardDefinition.MeetsRequirement(req, partyContext, defenseContext, HeroismAvailable)) {
                    meetsHold = false;
                    break;
                }
                if (req == Requirement.Heroism) usedHeroism = true;
            }

            if (meetsHold) {
                if (usedHeroism) HeroismAvailable = false;
                this.AddToHand(card);
                return true;
            }

            return false;
        }

        public void StartNextQuestChapter(List<Player> allPlayers, System.Action<Card> onCardHeldFromDefense = null) {
            List<CardDefinition> partyDefs = party.ConvertAll(c => c.Definition);
            List<CardDefinition> defenseDefs = defense.ConvertAll(c => c.Definition);

            // Did we need heroism to pass the locks?
            foreach (var defCard in defense) {
                foreach (var lockReq in defCard.LockRequirements) {
                     if (lockReq == Requirement.Heroism) {
                         HeroismAvailable = false;
                         break;
                     }
                }
            }

            // Create a list of cards to process with their original zone context
            var partyCopy = new List<Card>(party);
            var defenseCopy = new List<Card>(defense);
            
            this.party.Clear();
            this.defense.Clear();

            List<Card> candidates = new List<Card>(partyCopy);
            candidates.AddRange(defenseCopy);

            // Sort so Hero card comes first (to prioritize for heroism consumption)
            Card heroCard = GetHeroCard();
            candidates.Sort((a, b) => {
                if (a.IsSameCard(heroCard)) return -1;
                if (b.IsSameCard(heroCard)) return 1;
                return 0;
            });

            foreach (var card in candidates) {
                bool isOwnedByThisPlayer = (card.OwnerId == this.Id);

                if (isOwnedByThisPlayer) {
                    if (TryHoldCard(card, partyDefs, defenseDefs)) {
                        // Was it from defense?
                        if (defenseCopy.Contains(card)) {
                            onCardHeldFromDefense?.Invoke(card);
                        }
                    } else {
                        // If not held: Party cards -> History, Defense cards (owned/quest) -> Ash
                        if (partyCopy.Contains(card)) {
                            this.historyCards.Add(card);
                        } else {
                            this.ashCards.Add(card);
                        }
                    }
                } else {
                    if (card.OwnerId == this.Id) {
                        this.ashCards.Add(card);
                    } else {
                        // Owned by another player -> Find owner and move to their history
                        Player owner = allPlayers.Find(p => p.Id == card.OwnerId);
                        if (owner != null) {
                            owner.MoveToHistory(card);
                        } else {
                            // Fallback
                            this.ashCards.Add(card);
                        }
                    }
                }
            }

            chapterInd++;
            InitializeChapterTreasureDeck();
            AddCardForQuestChapterToDefense();
        }

        public void AddCardForQuestChapterToDefense() {
            foreach (Card card in questDefinition.GetCardsForChapter(chapterInd, Id)) {
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

            bool currentHeroism = HeroismAvailable;
            foreach (var defCard in defense) {
                foreach (var lockReq in defCard.LockRequirements) {
                    if (!CardDefinition.MeetsRequirement(lockReq, partyDefs, defenseDefs, currentHeroism)) {
                        return false;
                    }
                    if (lockReq == Requirement.Heroism) currentHeroism = false;
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