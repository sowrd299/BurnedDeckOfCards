using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ashworld {

    public partial class GameLogic : MonoBehaviour
    {
        private const int MAX_PARTY_SIZE = 5;
        private const int MAX_DEFENSE_SIZE = 5;

        [Header("Config")]
        [SerializeField] private DeckDefinitionAsset playerDeckDefinition;
        [SerializeField] private QuestDefinitionAsset playerQuestDefintiion;
        
        [Header("UI")]
        [SerializeField] private InputManager input;
        [SerializeField] private PlayerUIView playerUIView;
        [SerializeField] private CardZoneView playerHandView;
        [SerializeField] private CardZoneView playerPartyView;
        [SerializeField] private CardZoneView playerDefenseView;
        [SerializeField] private PlayerUIView opponentUIView;
        [SerializeField] private CardZoneView opponentHandView;
        [SerializeField] private CardZoneView opponentPartyView;
        [SerializeField] private CardZoneView opponentDefenseView;

        [SerializeField] private GameObject advanceButtonRoot;
        [SerializeField] private Button advanceButton;
        [SerializeField] private Button endTurnButton;

        [Header("AI Opponent")]
        [SerializeField] private DeckDefinitionAsset opponentDeckDefinition;
        [SerializeField] private QuestDefinitionAsset opponentQuestDefinition;
        
        private Player player;
        private Player opponent;
        private List<Player> allPlayers = new List<Player>();

        private Player currentTurnPlayer;

        private int currentTurnActions;
        private bool isPlayerTurn => currentTurnPlayer == player; 

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            SetUpPlayer();
            SetUpInput();
            StartTurn(player);
        }

        private void SetUpInput() {
            input.RegisterZoneCallback(playerPartyView, OnCardDroppedInParty);
            input.RegisterZoneCallback(playerHandView, OnCardDroppedInHand); // Pick Card Up (Drag)
            if (opponentDefenseView != null) input.RegisterZoneCallback(opponentDefenseView, OnCardDroppedInOpponentDefense);
            
            input.OnCardDroppedOnCard += OnCardAttack;
            input.OnCardRightClicked += OnCardPickUpRequest;

            advanceButton.onClick.AddListener(OnAdvanceButtonPressed);
            if (endTurnButton != null) endTurnButton.onClick.AddListener(OnEndTurnPressed);
        }

        private readonly Dictionary<Card, CardView> cardViewCache = new Dictionary<Card, CardView>();

        private void SetUpPlayer() {
            player = new Player("Player", playerDeckDefinition.Definition, playerQuestDefintiion.Definition);

            var aiDeck = (opponentDeckDefinition != null ? opponentDeckDefinition : playerDeckDefinition).Definition;
            var aiQuest = (opponentQuestDefinition != null ? opponentQuestDefinition : playerQuestDefintiion).Definition;
            
            opponent = new Player("Opponent", aiDeck, aiQuest);
            
            allPlayers.Clear();
            allPlayers.Add(player);
            allPlayers.Add(opponent);

            foreach (var p in allPlayers) 
            {
                p.Shuffle();
                p.Draw(5);
                p.AddToParty(p.GetHeroCard());
                p.AddCardForQuestChapterToDefense();
            }

            UpdateCardViews();
        }

        private void UpdateCardViews() {
            // 1. Identify valid cards
            HashSet<Card> validCards = new HashSet<Card>();
            
            foreach(var p in allPlayers) {
                validCards.UnionWith(p.hand);
                validCards.UnionWith(p.party);
                validCards.UnionWith(p.defense);
            }

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

            if (opponentHandView != null) opponentHandView.SyncCards(opponent.hand, cardViewCache, true);
            if (opponentPartyView != null) opponentPartyView.SyncCards(opponent.party, cardViewCache);
            if (opponentDefenseView != null) opponentDefenseView.SyncCards(opponent.defense, cardViewCache);
        }

        private void StartTurn(Player actingPlayer) {
            currentTurnPlayer = actingPlayer;
            actingPlayer.Draw();
            currentTurnActions = 3;
            UpdateUI();
        }

        private void OnEndTurnPressed() {
             if (isPlayerTurn) {
                StartOpponentTurn();
             }
        }
        
        private void StartOpponentTurn() {
            StartTurn(opponent);
            StartCoroutine(OpponentTurnCoroutine());
        }

        private System.Collections.IEnumerator OpponentTurnCoroutine() {
            Debug.Log("AI Turn Start");
            
            // AI takes 3 actions
            while (currentTurnActions > 0) {
                yield return new WaitForSeconds(1.5f); // Simulated thinking

                OpponentAction action = GetBestAction(opponent, player);

                if (action == null) {
                    Debug.Log("AI has no moves. Ending turn.");
                    break;
                }

                bool success = false;
                switch(action.Type) {
                    case OpponentAction.ActionType.Attack:
                        // Determine target player based on defender location
                        Player target = opponent;
                        if (player.party.Contains(action.Defender)) {
                            target = player;
                        }
                        success = TryAttack(opponent, target, action.Attacker, action.Defender);
                        break;
                    case OpponentAction.ActionType.Play:
                        if (action.PlayToDefense) {
                             success = TryPlayCardToDefense(opponent, player, action.CardToPlay);
                        } else {
                             success = TryPlayCardToParty(opponent, action.CardToPlay);
                        }
                        break;
                }

                if (!success) {
                    Debug.LogWarning("AI Attempted invalid move! Skipping turn to prevent loop.");
                    break;
                }
                
                UpdateCardViews();
            }

            // End AI Turn
            yield return new WaitForSeconds(1.0f);
            Debug.Log("AI Turn End. Player Start.");
            
            StartTurn(player);
            UpdateCardViews();
        }

        // --- Actions ---

        // Play Card (to Party)
        private bool OnCardDroppedInParty(CardView cardView) {
            return TryPlayCardToParty(player, cardView.Card);
        }

        private bool OnCardDroppedInOpponentDefense(CardView cardView) {
            return TryPlayCardToDefense(player, opponent, cardView.Card);
        }

        public bool CanPlayCard(Player actingPlayer, Card card, Player targetPlayer) {
            if (currentTurnActions <= 0) return false;
            if (!actingPlayer.hand.Contains(card)) return false;

            // Global Unique
            if (card.HasAbility(SpecialAbility.Unique)) {
                // Check ALL players zones
                foreach(var p in allPlayers) {
                    if (p.party.Exists(c => c.CardName == card.CardName) || 
                        p.defense.Exists(c => c.CardName == card.CardName)) return false;
                }
            }

            // Location (Target Context)
            if (card.HasAbility(SpecialAbility.Location)) {
                if (targetPlayer.party.Exists(c => c.HasAbility(SpecialAbility.Location)) || 
                    targetPlayer.defense.Exists(c => c.HasAbility(SpecialAbility.Location))) return false;
            }

            // History Cost
            if (!actingPlayer.CanPayHistoryCost(card.HistoryCost, card)) {
                Debug.Log($"Cannot pay History Cost of {card.HistoryCost}.");
                return false;
            }

            return true;
        }

        public bool TryPlayCardToParty(Player actingPlayer, Card card) {
            if (!CanPlayCard(actingPlayer, card, actingPlayer)) return false;
            
            if (actingPlayer.party.Count >= MAX_PARTY_SIZE) return false;

            actingPlayer.PayHistoryCost(card.HistoryCost, card);

            actingPlayer.hand.Remove(card);
            actingPlayer.party.Add(card);

            UpdateCardViews();
            DecrementActions();

            return true;
        }

        public bool TryPlayCardToDefense(Player actingPlayer, Player targetPlayer, Card card) {
             if (!CanPlayCard(actingPlayer, card, targetPlayer)) return false;
             
             if (targetPlayer.defense.Count >= MAX_DEFENSE_SIZE) return false;

             actingPlayer.PayHistoryCost(card.HistoryCost, card);
             actingPlayer.hand.Remove(card);
             
             targetPlayer.defense.Add(card); 

             UpdateCardViews();
             DecrementActions();

             return true;
        }

        // Pick Up (Drag)
        private bool OnCardDroppedInHand(CardView cardView) {
             return TryPickUp(player, cardView.Card);
        }

        // Pick Up (Right Click)
        private void OnCardPickUpRequest(CardView cardView) {
            TryPickUp(player, cardView.Card);
        }

        public bool TryPickUp(Player actingPlayer, Card card) {
            if (currentTurnActions <= 0) return false;

            // Can only pick up from Party
            if (actingPlayer.party.Contains(card)) {
                
                actingPlayer.party.Remove(card);
                actingPlayer.hand.Add(card);

                UpdateCardViews();
                DecrementActions();
                return true;
            }
            return false;
        }

        // Attack
        public bool CanCardAttack(Player actingPlayer, Player targetPlayer, Card attacker, Card defender) {
            if (currentTurnActions <= 0) return false;

            // Validate Ownership: You can only attack with cards you own.
            if (attacker.OwnerId != actingPlayer.Id) return false;

            // Context: Attack happens on targetPlayer's board
            bool attackerInParty = targetPlayer.party.Contains(attacker);
            bool attackerInDefense = targetPlayer.defense.Contains(attacker);
            
            bool defenderInParty = targetPlayer.party.Contains(defender);
            bool defenderInDefense = targetPlayer.defense.Contains(defender);

            if (!attackerInParty && !attackerInDefense) return false;
            if (!defenderInParty && !defenderInDefense) return false;

            // Control Check
            if (actingPlayer == targetPlayer) {
                // Attacking on own board.
                // Must act with Party cards against Defense cards.
                if (!attackerInParty) return false;
                if (!defenderInDefense) return false;
            } else {
                // Attacking on opponent's board.
                // Must act with Defense cards against Party cards.
                if (!attackerInDefense) return false;
                if (!defenderInParty) return false;
            }
            
            // Validate State
            if (attacker.IsExhausted) {
                Debug.Log("Attacker is exhausted!");
                return false;
            }

            // Compare Ranks
            int attackRank = GetEffectiveRank(attacker, attackerInParty ? targetPlayer.party : targetPlayer.defense);
            int defenseRank = GetEffectiveRank(defender, defenderInParty ? targetPlayer.party : targetPlayer.defense); 

            Debug.Log($"Attack: {attacker.CardName}({attackRank}) vs {defender.CardName}({defenseRank})");

            return attackRank > defenseRank;
        }

        private bool OnCardAttack(CardView attackerMember, CardView targetMember) {
             Player target = allPlayers.Find(p => p.party.Contains(targetMember.Card) || p.defense.Contains(targetMember.Card));
             if (target == null) return false;
             return TryAttack(player, target, attackerMember.Card, targetMember.Card);
        }

        public bool TryAttack(Player actingPlayer, Player targetPlayer, Card attacker, Card defender) {
            
            if (CanCardAttack(actingPlayer, targetPlayer, attacker, defender)) {
                
                if (targetPlayer.defense.Contains(defender)) {
                    targetPlayer.defense.Remove(defender);
                    targetPlayer.MoveToAsh(defender);
                } else if (targetPlayer.party.Contains(defender)) {
                    targetPlayer.party.Remove(defender);
                    targetPlayer.MoveToHistory(defender);
                }

                attacker.Exhaust();
                
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
