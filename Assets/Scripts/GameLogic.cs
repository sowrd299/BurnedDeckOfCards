using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ashworld {

    public class GameLogic : MonoBehaviour
    {
        private const int MAX_PARTY_SIZE = 5;

        [Header("Config")]
        [SerializeField] private DeckDefinitionAsset playerDeckDefinition;
        [SerializeField] private QuestDefinitionAsset playerQuestDefintiion;
        
        [Header("UI")]
        [SerializeField] private InputManager input;
        [SerializeField] private PlayerUIView playerUIView;
        [SerializeField] private CardZoneView playerHandView;
        [SerializeField] private CardZoneView playerPartyView;
        [SerializeField] private CardZoneView playerDefenseView;

        [SerializeField] private GameObject advanceButtonRoot;
        [SerializeField] private Button advanceButton;
        [SerializeField] private Button endTurnButton;


        private Player player;

        private int currentTurnActions;
        private bool isPlayerTurn => true; // Single player always active for now? Or state machine?

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            SetUpPlayer();
            SetUpInput();
            StartTurn();
        }

        private void SetUpInput() {
            input.RegisterZoneCallback(playerPartyView, OnCardDroppedInParty);
            input.RegisterZoneCallback(playerHandView, OnCardDroppedInHand); // Pick Card Up (Drag)
            
            input.OnCardDroppedOnCard += OnCardAttack;
            input.OnCardRightClicked += OnCardPickUpRequest;

            advanceButton.onClick.AddListener(OnAdvanceButtonPressed);
            if (endTurnButton != null) endTurnButton.onClick.AddListener(OnEndTurnPressed);
        }

        private readonly Dictionary<Card, CardView> cardViewCache = new Dictionary<Card, CardView>();

        private void SetUpPlayer() {
            player = new Player("Player", playerDeckDefinition.Definition, playerQuestDefintiion.Definition);

            player.Shuffle();
            player.Draw(5);

            player.AddToParty(player.GetHeroCard());
            player.AddCardForQuestChapterToDefense();

            UpdateCardViews();
        }

        private void UpdateCardViews() {
            // 1. Identify valid cards
            HashSet<Card> validCards = new HashSet<Card>();
            validCards.UnionWith(player.hand);
            validCards.UnionWith(player.party);
            validCards.UnionWith(player.defense);

            // 2. Cleanup Stale Views
            List<Card> toRemove = new List<Card>();
            foreach(var kvp in cardViewCache) {
                 if (!validCards.Contains(kvp.Key)) {
                      if (kvp.Value != null) Destroy(kvp.Value.gameObject);
                      toRemove.Add(kvp.Key);
                 }
            }
            foreach(var c in toRemove) cardViewCache.Remove(c);

            // 3. Sync Zones
            playerHandView.SyncCards(player.hand, cardViewCache);
            playerPartyView.SyncCards(player.party, cardViewCache);
            playerDefenseView.SyncCards(player.defense, cardViewCache);
        }

        private void StartTurn() {

            if (isPlayerTurn) {
                player.Draw();
            }

            currentTurnActions = 3;
            UpdateUI();
        }

        private void OnEndTurnPressed() {
             if (!isPlayerTurn) return; // Should likely be player turn to end it

             StartTurn();

             UpdateCardViews(); // Show new card
        }

        // --- Actions ---

        // Play Card
        private bool OnCardDroppedInParty(CardView cardView) {
            if (currentTurnActions <= 0) return false;

            Card card = cardView.Card;
            
            if (!isPlayerTurn || player.party.Count >= MAX_PARTY_SIZE) return false;
            
            // 1. Must be in hand
            if (!player.hand.Contains(card)) return false;

            // 2. Constraints (Location, Unique)
            if (card.HasAbility(SpecialAbility.Location)) {
                if (player.party.Exists(c => c.HasAbility(SpecialAbility.Location))) {
                    Debug.Log("Cannot play Location: Party already has one.");
                    return false;
                }
            }
            if (card.HasAbility(SpecialAbility.Unique)) {
                // Check party and defense (rules: "in or defending against any party")
                bool partyHas = player.party.Exists(c => c.CardName == card.CardName);
                bool defenseHas = player.defense.Exists(c => c.CardName == card.CardName);
                if (partyHas || defenseHas) {
                    Debug.Log("Cannot play Unique: Card with same name already in play.");
                    return false;
                }
            }

            // 3. History Cost
            if (!player.CanPayHistoryCost(card.HistoryCost, card)) {
                Debug.Log($"Cannot pay History Cost of {card.HistoryCost}.");
                return false;
            }

            // Execute Play
            player.PayHistoryCost(card.HistoryCost, card);

            player.hand.Remove(card);
            player.party.Add(card);

            // Rebuild all views (since deck/history might have changed during payment)
            UpdateCardViews();
            DecrementActions();

            return true;
        }

        // Pick Up (Drag)
        private bool OnCardDroppedInHand(CardView cardView) {
             return TryPickUp(cardView.Card);
        }

        // Pick Up (Right Click)
        private void OnCardPickUpRequest(CardView cardView) {
            TryPickUp(cardView.Card);
        }

        private bool TryPickUp(Card card) {
            if (currentTurnActions <= 0) return false;
            if (!isPlayerTurn) return false;

            // Can only pick up from Party (Rules: "in your party or defending against another player’s party")
            if (player.party.Contains(card)) {
                
                player.party.Remove(card);
                player.hand.Add(card);

                UpdateCardViews();
                DecrementActions();
                return true;
            }
            return false;
        }

        // Attack
        private bool CanCardAttack(CardView attackerMember, CardView targetMember) {
            if (currentTurnActions <= 0) return false;
            if (!isPlayerTurn) return false;

            Card attacker = attackerMember.Card;
            Card defender = targetMember.Card;

            // Validate Context
            if (!player.party.Contains(attacker)) return false; // Must attack with party card
            if (!player.defense.Contains(defender)) return false; // Must attack defense card (for now)

            // Validate State
            if (attacker.IsExhausted) {
                Debug.Log("Attacker is exhausted!");
                return false;
            }

            // Compare Ranks
            int attackRank = GetEffectiveRank(attacker, player.party);
            int defenseRank = GetEffectiveRank(defender, player.defense); // Defenders might have boons too?

            Debug.Log($"Attack: {attacker.CardName}({attackRank}) vs {defender.CardName}({defenseRank})");

            return attackRank > defenseRank;
        }

        private bool OnCardAttack(CardView attackerMember, CardView targetMember) {
            if (CanCardAttack(attackerMember, targetMember)) {
                // Success
                player.defense.Remove(targetMember.Card);
                
                // Where does defender go?
                // "sent to its owner’s ash (if its part of that player’s quest and is still defending against their party) or their history otherwise."
                // Since it's from the Quest (defending against player), it goes to Ash.
                // Note: The player object holds the Ash/History.
                // The logical owner of the Quest cards is the "Quest", but functionally in single player, they go to the player's piles upon defeat? 
                // "By default, cards from your quest will be discarded into your ashes". Yes.
                player.MoveToAsh(targetMember.Card);

                attackerMember.Card.Exhaust();
                
                UpdateCardViews();
                DecrementActions();
                return true;
            } else {
                Debug.Log("Attack failed: Rank too low.");
                return false;
            }
        }

        private int GetEffectiveRank(Card card, List<Card> contextParty) {
            if (card.HasAbility(SpecialAbility.Boon)) return 0; // Boons don't have rank (conceptually)
            
            int rank = card.Rank;

            // Add Boons from same party
            foreach(var other in contextParty) {
                if (other != card && other.HasAbility(SpecialAbility.Boon)) {
                    // "add their rank to any other card... that has all the suits the boon has"
                    // Does 'card' have all suits of 'Boon'?
                    // card.Suits must be superset of boon.Suits?
                    // "has all the suits the boon has" -> Boon {A, B}, Card {A, B, C} -> Yes.
                    
                    bool receivesBoon = true;

                    foreach(var s in other.Suits) {
                        if (!card.Suits.Contains(s)) {
                            receivesBoon = false;
                            break;
                        }
                    }

                    if (receivesBoon) {
                         rank += other.Rank;
                    }
                }
            }
            return rank;
        }

        private void OnAdvanceButtonPressed() {
            if (isPlayerTurn) {
                if (currentTurnActions <= 0) {
                    Debug.Log("Cannot advance: No actions left.");
                    return;
                }

                player.StartNextQuestChapter();

                UpdateCardViews();
                DecrementActions();
            }
        }

        private void DecrementActions() {
            currentTurnActions--;
            UpdateUI();

            if (currentTurnActions <= 0) {
                OnEndTurnPressed();
            }
        }

        private void UpdateUI() {
            playerUIView.SetTurnInfo(currentTurnActions, isPlayerTurn);
            advanceButtonRoot.SetActive(isPlayerTurn && player.CanAdvance());
        }

        // Update is called once per frame
        private void Update()
        {
            
        }
    }
}
