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


        private Player player;

        private int currentTurnActions;
        private bool isPlayerTurn => true;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            SetUpPlayer();
            SetUpInput();
            playerHandView.SetUpForCards(player.hand);
            playerPartyView.SetUpForCards(player.party);
            playerDefenseView.SetUpForCards(player.defense);
            StartTurn();
        }

        private void SetUpInput() {
            input.RegisterZoneCallback(playerPartyView, OnCardDroppedInParty);
            advanceButton.onClick.AddListener(OnAdvanceButtonPressed);
        }

        private void SetUpPlayer() {
            player = new Player("Player", playerDeckDefinition.Definition, playerQuestDefintiion.Definition);

            player.Shuffle();
            player.Draw(5);

            player.AddToParty(player.GetHeroCard());
            player.AddCardForQuestChapterToDefense();

            ForceUpdatePlayerZoneViews();
        }

        private void ForceUpdatePlayerZoneViews() {
            playerHandView.SetUpForCards(player.hand);
            playerPartyView.SetUpForCards(player.party);
            playerDefenseView.SetUpForCards(player.defense);
        }

        private void StartTurn() {
            currentTurnActions = 3;
            UpdateUI();
        }

        private bool OnCardDroppedInParty(CardView cardView) {
            if (isPlayerTurn && player.party.Count < MAX_PARTY_SIZE && cardView.Card.HistoryCost == 0 && player.hand.Contains(cardView.Card)) {

                player.hand.Remove(cardView.Card);
                player.party.Add(cardView.Card);

                playerHandView.RemoveCardView(cardView);
                playerPartyView.AddCardView(cardView);

                DecrementActions();

                return true;
            }

            return false;
        }

        private void OnAdvanceButtonPressed() {
            if (isPlayerTurn) {

                player.StartNextQuestChapter();

                ForceUpdatePlayerZoneViews();
                DecrementActions();
            }
        }

        private void DecrementActions() {
            currentTurnActions--;
            UpdateUI();
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
