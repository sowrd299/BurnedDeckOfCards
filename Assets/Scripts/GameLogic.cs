using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ashworld {

    public partial class GameLogic : MonoBehaviour
    {
        private const int MAX_PARTY_SIZE = 5;
        private const int MAX_DEFENSE_SIZE = 5;
        private const int ACTIONS_PER_TURN = 3;

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

        [Header("Animations")]
        [SerializeField] private ChapterAnimationView playerChapterAnim;
        [SerializeField] private ChapterAnimationView opponentChapterAnim;

        [Header("Card Details")]
        [SerializeField] private CardDetailsView cardDetailsView;

        [Header("AI Opponent")]
        [SerializeField] private DeckDefinitionAsset opponentDeckDefinition;
        [SerializeField] private QuestDefinitionAsset opponentQuestDefinition;
        
        private Player player;
        private Player opponent;
        private List<Player> allPlayers = new List<Player>();

        private bool isGameInProgress = true;

        private Player currentTurnPlayer;

        private int currentTurnActions;
        private bool isPlayerTurn => currentTurnPlayer == player; 

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            SetUpPlayer();
            SetUpInput();
            StartTurn(player);
            UpdateCardViews();
        }

        private void SetUpInput() {
            input.CanStartDrag = (view) => isGameInProgress && isPlayerTurn && CanUse(view.Card, player);
            input.RegisterZoneCallback(playerPartyView, OnCardDroppedInParty);
            input.RegisterZoneCallback(playerHandView, OnCardDroppedInHand); // Pick Card Up (Drag)
            if (opponentDefenseView != null) input.RegisterZoneCallback(opponentDefenseView, OnCardDroppedInOpponentDefense);
            
            input.OnCardDroppedOnCard += OnCardAttack;
            input.OnCardRightClicked += OnCardPickUpRequest;
            input.OnCardDragBegan += HandleCardDragBegan;
            input.OnCardDragEnded += HandleCardDragEnded;
            input.OnCardHoverChanged += HandleCardHoverChanged;

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
                p.Draw(4);
                p.AddToHand(p.GetHeroCard());
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
            playerHandView.SyncCards(player.hand, cardViewCache, false, currentTurnActions < ACTIONS_PER_TURN);
            playerPartyView.SyncCards(player.party, cardViewCache);
            playerDefenseView.SyncCards(player.defense, cardViewCache);

            if (opponentHandView != null) opponentHandView.SyncCards(opponent.hand, cardViewCache, true, currentTurnActions < ACTIONS_PER_TURN);
            if (opponentPartyView != null) opponentPartyView.SyncCards(opponent.party, cardViewCache);
            if (opponentDefenseView != null) opponentDefenseView.SyncCards(opponent.defense, cardViewCache);

            // 4. Update Statuses (Exhausted & CanUse & Requirements)
            foreach(var kvp in cardViewCache) {
                if (kvp.Value != null) {
                    kvp.Value.UpdateExhaustedStatus();
                    kvp.Value.SetCanUse(isPlayerTurn && CanUse(kvp.Key, player));

                    Player zoneOwner = GetOwnerOfZoneForCard(kvp.Key);
                    if (zoneOwner != null) {
                        kvp.Value.UpdateRequirements(zoneOwner);
                    } else {
                        kvp.Value.ClearRequirements();
                    }
                 }
            }

            // 5. Default History Feedback
            if (playerUIView != null) playerUIView.UpdateHistoryFeedback(null, player);
        }

        private void HandleCardHoverChanged(CardView hoveredView) {
            if (playerUIView != null) {
                playerUIView.UpdateHistoryFeedback(hoveredView != null ? hoveredView.Card : null, player);
            }

            // Clear previous hover/highlights
            foreach (CardView v in cardViewCache.Values) {
                v.SetHovered(v == hoveredView);
                v.SetHighlightedSuit(Suit.None);
            }

            if (cardDetailsView != null) {
                if (hoveredView != null && !hoveredView.IsFaceDown) {
                    cardDetailsView.ShowCard(hoveredView.Card, hoveredView.GetTargetPosition());
                } else {
                    cardDetailsView.Hide();
                }
            }

            if (hoveredView != null) {
                Card card = hoveredView.Card;
                List<Card> zone = GetZoneForCard(card);
                
                // Only highlight boons for cards in Play (Party/Defense)
                if (zone != null && zone != player.hand && zone != opponent.hand) {
                    List<Card> boons = GetApplyingBoons(card, zone);
                    foreach (Card boon in boons) {
                        if (cardViewCache.TryGetValue(boon, out CardView boonView)) {
                            // Find first matching suit between boon and card
                            Suit match = Suit.None;
                            foreach (Suit s in boon.Suits) {
                                if (s != Suit.None && card.Suits.Contains(s)) {
                                    match = s;
                                    break;
                                }
                            }
                            boonView.SetHighlightedSuit(match);
                        }
                    }
                }
            }
        }

        private List<Card> GetApplyingBoons(Card card, List<Card> contextParty) {
            List<Card> applyingBoons = new List<Card>();
            if (card.HasAbility(SpecialAbility.Boon)) return applyingBoons; 
            
            foreach(var other in contextParty) {
                if (other != card && other.HasAbility(SpecialAbility.Boon)) {
                    if (card.Definition.CanBoonApply(other.Definition)) {
                        applyingBoons.Add(other);
                    }
                }
            }
            return applyingBoons;
        }

        private void StartTurn(Player actingPlayer) {
            currentTurnPlayer = actingPlayer;
            actingPlayer.ResetHeroism();
            actingPlayer.Draw();
            currentTurnActions = ACTIONS_PER_TURN;
            UpdateUI();
        }

        private void OnEndTurnPressed() {
             if (isGameInProgress && isPlayerTurn) {
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
            while (currentTurnActions > 0 && isGameInProgress) {
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
                        yield return StartCoroutine(AttackCoroutine(opponent, target, action.Attacker, action.Defender));
                        success = true; // AttackCoroutine internally decrements actions and returns
                        break;
                    case OpponentAction.ActionType.Play:
                        if (action.PlayToDefense) {
                             success = TryPlayCardToDefense(opponent, player, action.CardToPlay);
                        } else {
                             success = TryPlayCardToParty(opponent, action.CardToPlay);
                        }
                        break;
                    case OpponentAction.ActionType.Advance:
                        // Perform Advance
                        yield return StartCoroutine(HandleQuestAdvancement(opponent));
                        success = true;
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
            
            if (isGameInProgress) {
                StartTurn(player);
            }

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

            List<Card> discards = actingPlayer.PayHistoryCost(card.HistoryCost, card);
            foreach(var d in discards) {
                if(cardViewCache.TryGetValue(d, out CardView dv)) {
                    cardViewCache.Remove(d);
                    StartCoroutine(PlayDiscardAnimationThenDestroy(dv));
                }
            }

            actingPlayer.hand.Remove(card);
            actingPlayer.party.Add(card);

            DecrementActions();
            UpdateCardViews();

            return true;
        }

        private System.Collections.IEnumerator PlayDiscardAnimationThenDestroy(CardView view) {
            if (view == null) yield break;
            
            if (view.FireEffect != null) {
                yield return view.FireEffect.PlayFire(null);
            } else {
                yield return new WaitForSeconds(0.5f);
            }
            
            if (view != null) Destroy(view.gameObject);
        }

        public bool TryPlayCardToDefense(Player actingPlayer, Player targetPlayer, Card card) {
             if (!CanPlayCard(actingPlayer, card, targetPlayer)) return false;
             
             if (targetPlayer.defense.Count >= MAX_DEFENSE_SIZE) return false;

             List<Card> discards = actingPlayer.PayHistoryCost(card.HistoryCost, card);
             foreach(var d in discards) {
                 if(cardViewCache.TryGetValue(d, out CardView dv)) {
                     cardViewCache.Remove(d);
                     StartCoroutine(PlayDiscardAnimationThenDestroy(dv));
                 }
             }

             actingPlayer.hand.Remove(card);
             
             targetPlayer.defense.Add(card); 

             DecrementActions();
             UpdateCardViews();

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

        public bool CanPickUp(Player actingPlayer, Card card) {
            if (currentTurnActions <= 0) return false;

            // Can only pick up from Party
            if (actingPlayer.party.Contains(card)) return true;

            // Or from another player's defense if you own it (e.g. played it there)
            foreach (var p in allPlayers) {
                if (p != actingPlayer && p.defense.Contains(card) && card.OwnerId == actingPlayer.Id) {
                    return true;
                }
            }

            return false;
        }

        public bool TryPickUp(Player actingPlayer, Card card) {
            if (!CanPickUp(actingPlayer, card)) return false;

            // Remove from wherever it is
            if (actingPlayer.party.Contains(card)) {
                actingPlayer.party.Remove(card);
            } else {
                foreach (var p in allPlayers) {
                    if (p.defense.Contains(card)) {
                        p.defense.Remove(card);
                        break;
                    }
                }
            }

            actingPlayer.AddToHand(card);

            UpdateCardViews();
            DecrementActions();
            return true;
        }

        public bool CanUse(Card card, Player actingPlayer) {
            if (currentTurnActions <= 0) return false;
            if (currentTurnPlayer == null || card == null) return false;

            // 1. Can Pick Up?
            if (CanPickUp(actingPlayer, card)) return true;

            // 2. Can Play? (From Hand)
            if (actingPlayer.hand.Contains(card)) {
                foreach (var p in allPlayers) {
                    if (CanPlayCard(actingPlayer, card, p)) return true;
                }
            }

            // 3. Can Attack? (From Party or Defense)
            // If card is in a zone, see if it can attack anything
            // Optimization: Only check if it's the acting player's turn (handled above)
            
            // Collect all possible targets
            List<Card> allPotentialTargets = new List<Card>();
            foreach (var p in allPlayers) {
                allPotentialTargets.AddRange(p.party);
                allPotentialTargets.AddRange(p.defense);
            }

            foreach (var target in allPotentialTargets) {
                if (target == card) continue;
                Player targetPlayer = GetOwnerOfZoneForCard(target);
                if (targetPlayer == null) continue;

                if (CanCardAttack(actingPlayer, targetPlayer, card, target)) return true;
            }

            return false;
        }

        private void HandleCardDragBegan(CardView draggedView) {
            if (draggedView == null || draggedView.Card == null) return;
            
            foreach (var kvp in cardViewCache) {
                Card targetCard = kvp.Key;
                CardView targetView = kvp.Value;
                if (targetView == null || targetCard == draggedView.Card) continue;

                Player targetPlayer = GetOwnerOfZoneForCard(targetCard);
                if (targetPlayer != null) {
                    bool canBeAttacked = CanCardAttack(player, targetPlayer, draggedView.Card, targetCard);
                    targetView.SetCanBeAttacked(canBeAttacked);
                }
            }
        }

        private void HandleCardDragEnded(CardView draggedView) {
            foreach (var kvp in cardViewCache) {
                if (kvp.Value != null) {
                    kvp.Value.SetCanBeAttacked(false);
                }
            }
        }

        // Attack
        public bool CanCardAttack(Player actingPlayer, Player targetPlayer, Card attacker, Card defender) {
            if (currentTurnActions <= 0) return false;

            // Validate Ownership: You can only attack with cards you own.
            if (attacker.OwnerId != actingPlayer.Id) return false;

            // Boons cannot attack
            if (attacker.HasAbility(SpecialAbility.Boon)) return false;

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
                StartCoroutine(AttackCoroutine(actingPlayer, targetPlayer, attacker, defender));
                return true;
            } else {
                return false;
            }
        }

        private System.Collections.IEnumerator AttackCoroutine(Player actingPlayer, Player targetPlayer, Card attacker, Card defender) {
            
            // 1. Gather Views
            cardViewCache.TryGetValue(attacker, out CardView attackerView);
            cardViewCache.TryGetValue(defender, out CardView defenderView);

            if (defenderView != null) defenderView.SetCanBeAttacked(true);

            // 2. Immediate Model Update (Apply logic changes)
            if (targetPlayer.defense.Contains(defender)) {
                targetPlayer.defense.Remove(defender);

                // This is bit of a heuristic; we should be checking if started in the acting player's quest, not just if its theirs.
                if (defender.OwnerId == actingPlayer.Id) {
                    actingPlayer.MoveToAsh(defender);
                } else {
                    foreach (Player player in allPlayers) {
                        if (player.Id == defender.OwnerId) {
                            player.MoveToHistory(defender);
                        }
                    }
                }
            } else if (targetPlayer.party.Contains(defender)) {
                targetPlayer.party.Remove(defender);
                targetPlayer.MoveToHistory(defender);
            }

            attacker.Exhaust();
            DecrementActions();

            // 3. Play Visuals
            if (attackerView != null && defenderView != null && attackerView.AttackAnim != null) {
                
                // Snap attacker to its proper slot first (in case it was being dragged)
                CardZoneView zone = GetZoneViewForCard(attacker);
                if (zone != null && input != null) {
                    input.CancelAnimationAndSnap(attackerView, zone.GetDropPosition(attacker));
                }

                bool effectDone = false;
                yield return attackerView.AttackAnim.PlayAttackerAnim(
                    defenderView.transform,
                    () => {
                        // On Hit
                        if (defenderView.FireEffect != null) {
                            StartCoroutine(defenderView.FireEffect.PlayFire(() => {
                                // On Fire Complete
                                effectDone = true;
                                UpdateCardViews();
                            }));
                        } else {
                            effectDone = true;
                            UpdateCardViews();
                        }
                    },
                    () => {
                        // On Attacker Anim Complete
                    }
                );

                // Wait for fire effect to complete if it's still running
                while (!effectDone) {
                    yield return null;
                }
            } else {
                // Fallback
                UpdateCardViews();
            }
        }

        private int GetEffectiveRank(Card card, List<Card> containingZone) {
            int rank = card.Rank;

            // Add Boons from same party
            foreach(var other in GetApplyingBoons(card, containingZone)) {
                rank += other.Rank;
            }

            return rank;
        }

        private void OnAdvanceButtonPressed() {
            if (isGameInProgress && isPlayerTurn) {
                if (currentTurnActions <= 0) {
                    Debug.Log("Cannot advance: No actions left.");
                    return;
                }

                StartCoroutine(HandleQuestAdvancement(player));
            }
        }

        private System.Collections.IEnumerator HandleQuestAdvancement(Player p) {
            
            // 1. Update Model
            p.StartNextQuestChapter(allPlayers);
            DecrementActions();

            // 2. Check Win Condition
            var questDef = p.questDefinition;
            string chapterName = questDef.GetChapterName(p.chapterInd);

            if (p.chapterInd >= questDef.ChapterCount) {
                isGameInProgress = false;
                UpdateUI(); // Enable/Disable buttons based on state
                UpdateCardViews(); // Refresh statuses

                ChapterAnimationView victoryAnim = (p == player) ? playerChapterAnim : opponentChapterAnim;
                if (victoryAnim != null) {
                    yield return victoryAnim.PlayVictoryTransition(chapterName);
                }
                yield break;
            }

            // 3. Identify Animation & Data
            ChapterAnimationView anim = (p == player) ? playerChapterAnim : opponentChapterAnim;

            // 4. Play Animation
            if (anim != null) {
                yield return anim.PlayTransition(p.chapterInd, chapterName, () => {
                    UpdateCardViews();
                });
            } else {
                // Fallback if no animation object
                UpdateCardViews();
            }
        }

        private void DecrementActions() {
            if (!isGameInProgress) return;
            
            currentTurnActions--;
            UpdateUI();

            if (currentTurnActions <= 0) {
                OnEndTurnPressed();
            }
        }

        private void UpdateUI() {
            playerUIView.SetTurnInfo(currentTurnActions, isGameInProgress && isPlayerTurn);
            if (opponentUIView != null) opponentUIView.SetTurnInfo(currentTurnActions, isGameInProgress && !isPlayerTurn);
            advanceButtonRoot.SetActive(isGameInProgress && isPlayerTurn && player.CanAdvance());
            if (endTurnButton != null) endTurnButton.gameObject.SetActive(isGameInProgress && isPlayerTurn);
        }

        private CardZoneView GetZoneViewForCard(Card c) {
            if (player.hand.Contains(c)) return playerHandView;
            if (opponent.hand.Contains(c)) return opponentHandView;
            if (player.party.Contains(c)) return playerPartyView;
            if (opponent.party.Contains(c)) return opponentPartyView;
            if (player.defense.Contains(c)) return playerDefenseView;
            if (opponent.defense.Contains(c)) return opponentDefenseView;
            
            return null;
        }

        private List<Card> GetZoneForCard(Card card) {
            foreach (var p in allPlayers) {
                if (p.hand.Contains(card)) return p.hand;
                if (p.party.Contains(card)) return p.party;
                if (p.defense.Contains(card)) return p.defense;
            }
            return null;
        }

        private Player GetOwnerOfZoneForCard(Card card) {
            foreach (var p in allPlayers) {
                if (p.party.Contains(card) || p.defense.Contains(card)) return p;
            }
            return null;
        }

        // Update is called once per frame
        private void Update()
        {
            
        }
    }
}
