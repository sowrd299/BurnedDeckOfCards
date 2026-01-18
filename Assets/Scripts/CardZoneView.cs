using System.Collections.Generic;
using UnityEngine;

namespace Ashworld
{
    public class CardZoneView : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private List<Transform> cardPositions;
        [SerializeField] private Transform cardsRoot;
        [SerializeField] private CardView cardViewPrefab;

        private readonly Dictionary<Card, CardView> activeCardViews = new Dictionary<Card, CardView>();
        private readonly Dictionary<Card, Transform> activeCardPositions = new Dictionary<Card, Transform>();

        /// <summary>
        /// Sets up this zone to display the given cards.
        /// </summary>
        public void SetUpForCards(List<Card> cards)
        {
            if (cards == null) return;

            // Keep track of which cards should remain
            HashSet<Card> incomingCards = new HashSet<Card>(cards);

            // Remove views for cards no longer present
            List<Card> toRemove = new List<Card>();
            foreach (var kvp in activeCardViews)
            {
                if (!incomingCards.Contains(kvp.Key))
                {
                    if (kvp.Value != null)
                        Destroy(kvp.Value.gameObject);

                    toRemove.Add(kvp.Key);
                }
            }
            foreach (var card in toRemove) {
                activeCardViews.Remove(card);
                activeCardPositions.Remove(card);
            }

            // Create or update views for incoming cards
            for (int i = 0; i < cards.Count; i++)
            {
                Card card = cards[i];
                Transform targetPosition = (i < cardPositions.Count) ? cardPositions[i] : null;

                if (!activeCardViews.TryGetValue(card, out var view) || view == null)
                {
                    // Instantiate new view
                    var instance = Instantiate(cardViewPrefab, cardsRoot);
                    view = instance;
                    activeCardViews[card] = view;

                    // Update view content
                    view.SetUpForCard(card);
                }

                // Update position if slots are available
                if (targetPosition != null)
                {
                    view.transform.position = targetPosition.position;
                    view.transform.rotation = targetPosition.rotation;

                    activeCardPositions[card] = targetPosition;
                }
            }
        }

        public void AddCardView(CardView cardView) {

            cardView.transform.SetParent(cardsRoot);

            foreach (Transform pos in cardPositions) {
                if (!activeCardPositions.ContainsValue(pos)) {
                    activeCardViews[cardView.Card] = cardView;
                    activeCardPositions[cardView.Card] = pos;

                    break;
                }
            }

            // TODO: For now this assumes the actual position moving will be handled by the InputManager
        }

        public void RemoveCardView(CardView cardView) {

            Card card = cardView.Card;

            activeCardViews.Remove(card);
            activeCardPositions.Remove(card);
        }

        public Vector3 GetDropPosition(Card card) {
            if (activeCardPositions.ContainsKey(card)) {
                return activeCardPositions[card].position;
            }

            return Vector3.zero;
        }

        /// <summary>
        /// Clears all card views in this zone.
        /// </summary>
        public void Clear()
        {
            foreach (var kvp in activeCardViews)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value.gameObject);
            }
            activeCardViews.Clear();
        }
    }
}
