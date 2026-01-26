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

        [Header("Highlighting")]
        [SerializeField] private SpriteRenderer background;
        [SerializeField] private Color highlightColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private float highlightLerpSpeed = 10f;

        private readonly Dictionary<Card, CardView> activeCardViews = new Dictionary<Card, CardView>();
        private readonly Dictionary<Card, Vector3> activeCardPositions = new Dictionary<Card, Vector3>();

        private Color targetColor;
        private Color originalColor;

        private void Awake()
        {
            if (background != null)
            {
                originalColor = background.color;
                targetColor = originalColor;
            }
        }

        private void Update()
        {
            if (background != null)
            {
                background.color = Color.Lerp(background.color, targetColor, Time.deltaTime * highlightLerpSpeed);
            }
        }

        public void SyncCards(List<Card> cards, Dictionary<Card, CardView> globalCache, bool faceDown = false, bool keepPositions = true)
        {
            if (cards == null) return;

            if (!keepPositions) {
                activeCardPositions.Clear();
                activeCardViews.Clear();
            }

            // 1. Identify and Remove gone cards
            List<Card> removedCards = new List<Card>();
            foreach (var card in activeCardViews.Keys)
            {
                if (!cards.Contains(card)) removedCards.Add(card);
            }

            foreach (var card in removedCards)
            {
                activeCardViews.Remove(card);
                activeCardPositions.Remove(card);
            }

            // 2. Resolve or Update cards
            foreach (Card card in cards)
            {
                if (!globalCache.TryGetValue(card, out CardView view) || view == null)
                {
                    // Create new if doesn't exist globally
                    view = Instantiate(cardViewPrefab, cardsRoot);
                    view.SetUpForCard(card);
                    globalCache[card] = view;
                }
                
                view.SetFaceDown(faceDown);

                if (!activeCardViews.ContainsKey(card)) {
                    AddCardView(view);
                }
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

        private Quaternion GetTargetRotation(int index) {
            if (cardPositions == null) return Quaternion.identity;

            if (index < cardPositions.Count) {
                return cardPositions[index].rotation;
            }

            // Overflow logic
            Quaternion lastRot = cardPositions[cardPositions.Count - 1].rotation;
            return lastRot;
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
            Quaternion targetRotation = GetTargetRotation(targetIndex);
            activeCardViews[cardView.Card] = cardView;
            activeCardPositions[cardView.Card] = targetPos;

            cardView.SetTargetPosition(targetPos);
            cardView.SetTargetRotation(targetRotation);
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

        public void SetHighlighted(bool highlighted)
        {
            targetColor = highlighted ? highlightColor : originalColor;
        }
    }
}
