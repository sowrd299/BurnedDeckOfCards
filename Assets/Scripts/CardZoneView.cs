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

        public void SyncCards(List<Card> cards, Dictionary<Card, CardView> globalCache, bool faceDown = false)
        {
            if (cards == null) return;

            // Clear local tracking (we will rebuild it)
            activeCardViews.Clear();
            activeCardPositions.Clear();

            for (int i = 0; i < cards.Count; i++)
            {
                Card card = cards[i];
                Transform targetPosition = (i < cardPositions.Count) ? cardPositions[i] : null;

                // 1. Resolve View
                if (!globalCache.TryGetValue(card, out CardView view) || view == null)
                {
                    // Create new if doesn't exist globally
                    view = Instantiate(cardViewPrefab, cardsRoot);
                    view.SetUpForCard(card);
                    globalCache[card] = view;
                }
                
                view.SetFaceDown(faceDown);

                // 2. Ensure it's in this zone
                if (view.transform.parent != cardsRoot)
                {
                    view.transform.SetParent(cardsRoot);
                }

                // 3. Track locally
                activeCardViews[card] = view;

                // 4. Update Position
                if (targetPosition != null)
                {
                    // We record the target position so other systems (like input) know where it SHOULD be
                    activeCardPositions[card] = targetPosition;

                    // SNAP position for now to ensure layout. 
                    // Note: If InputManager is animating this card, we might fight it.
                    // But usually Sync is called after a logical state change.
                    // If we just dropped it, InputManager might want to Lerp it.
                    // If we snap it here, Lerp might jump.
                    // BUT, fixing the crash is priority.
                    
                    // Optional: Only snap if distance is significant? Or let InputManager handle it?
                    // For "Programmatic" moves (Draw Card), we want it to appear in hand.
                    // For "Drag Drops", InputManager calls SnapCardToZone.
                    
                    // Let's set rotation, but maybe be careful with position?
                    // If we don't set position, newly created cards (Instantiated) will be at (0,0,0) or parent origin.
                    // We must set position for new cards.
                    
                    // Simple approach: Always snap. InputManager's Lerp will just act on the new position (or look odd for a frame).
                    view.transform.position = targetPosition.position;
                    view.transform.rotation = targetPosition.rotation;
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
