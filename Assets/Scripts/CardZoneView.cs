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
        private readonly Dictionary<Card, Vector3> activeCardPositions = new Dictionary<Card, Vector3>();

        public void SyncCards(List<Card> cards, Dictionary<Card, CardView> globalCache, bool faceDown = false)
        {
            if (cards == null) return;

            // Clear local tracking (we will rebuild it)
            activeCardViews.Clear();
            activeCardPositions.Clear();

            for (int i = 0; i < cards.Count; i++)
            {
                Card card = cards[i];
                Vector3 targetPosition = GetTargetPosition(i);
                Quaternion targetRotation = (i < cardPositions.Count) ? cardPositions[i].rotation : cardPositions[cardPositions.Count - 1].rotation;

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
                activeCardPositions[card] = targetPosition;

                // 4. Update Position
                // Simple approach: Always snap.
                view.transform.position = targetPosition;
                view.transform.rotation = targetRotation;
            }
        }

        private Vector3 GetTargetPosition(int index) {
            if (cardPositions == null || cardPositions.Count == 0) return transform.position;

            if (index < cardPositions.Count) {
                return cardPositions[index].position;
            }

            // Overflow logic
            int overflowIndex = index - (cardPositions.Count - 1);
            Vector3 lastPos = cardPositions[cardPositions.Count - 1].position;
            
            // Stagger: slightly right, down, and behind (Z+)
            return lastPos + new Vector3(0.2f * overflowIndex, -0.2f * overflowIndex, 0.1f * overflowIndex);
        }

        public void AddCardView(CardView cardView) {

            cardView.transform.SetParent(cardsRoot);

            int targetIndex = 0;
            while (true)
            {
                Vector3 pos = GetTargetPosition(targetIndex);
                bool occupied = false;
                foreach (var activePos in activeCardPositions.Values)
                {
                    if (Vector3.SqrMagnitude(activePos - pos) < 0.001f)
                    {
                        occupied = true;
                        break;
                    }
                }
                if (!occupied) break;
                targetIndex++;
            }

            Vector3 targetPos = GetTargetPosition(targetIndex);
            activeCardViews[cardView.Card] = cardView;
            activeCardPositions[cardView.Card] = targetPos;
        }

        public void RemoveCardView(CardView cardView) {

            Card card = cardView.Card;

            activeCardViews.Remove(card);
            activeCardPositions.Remove(card);
        }

        public Vector3 GetDropPosition(Card card) {
            if (activeCardPositions.ContainsKey(card)) {
                return activeCardPositions[card];
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
