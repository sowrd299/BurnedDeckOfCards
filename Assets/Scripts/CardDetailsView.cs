using UnityEngine;

namespace Ashworld
{
    public class CardDetailsView : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private Vector3 viewOffset = new Vector3(3f, 3f, 0f);
        
        [Header("References")]
        [SerializeField] private CardView cardView;
        [SerializeField] private GameObject root;


        private float originalZ;

        private void Awake()
        {
            originalZ = cardView.transform.position.z;
            Hide();
        }

        public void ShowCard(Card card, Vector3 cardTargetPos)
        {
            if (card == null || cardView == null)
            {
                Hide();
                return;
            }

            root.SetActive(true);
            cardView.SetUpForCard(card);

            // Determine quadrant based on card target position relative to world space (0,0)
            float signX = cardTargetPos.x > 0 ? -1f : 1f;
            float signY = cardTargetPos.y > 0 ? -1f : 1f;

            Vector3 finalOffset = new Vector3(viewOffset.x * signX, viewOffset.y * signY, 0f);
            Vector3 targetPos = cardTargetPos + finalOffset;
            
            // Keep original Z
            targetPos.z = originalZ;
            
            // Ensure card view also snaps to position if it uses the same positioning logic
            cardView.SetTargetPosition(targetPos, true);
        }

        public void Hide()
        {
            root.SetActive(false);
        }
    }
}
