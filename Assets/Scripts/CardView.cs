using TMPro;
using UnityEngine;
using System.Collections.Generic;

namespace Ashworld
{
    public class CardView : MonoBehaviour
    {
        [Header("Definitions")]
        [SerializeField] private UIDefinitions uiDefinitions;

        [Header("UI References")]
        [SerializeField] private TextMeshPro nameText;
        [SerializeField] private TextMeshPro rankText;
        [SerializeField] private List<SpriteRenderer> suitIcons; // slots for suits
        [SerializeField] private TextMeshPro abilitiesText;
        [SerializeField] private SpriteRenderer illustration;

        [Header("Requirement UI")]
        [SerializeField] private TextMeshPro lockRequirementsText;
        [SerializeField] private TextMeshPro holdRequirementsText;
        [SerializeField] private string lockPrefix = "Lock: ";
        [SerializeField] private string holdPrefix = "Hold: ";

        [Header("State UI")]
        [SerializeField] private GameObject faceDownRoot;
        [SerializeField] private GameObject faceUpRoot; // Optional wrapper for content

        private Card currentCard;
        public Card Card => currentCard;

        /// <summary>
        /// Populates the view with data from the given card.
        /// </summary>
        public void SetUpForCard(Card card)
        {
            currentCard = card;

            if (card == null || card.Definition == null)
            {
                ClearView();
                return;
            }

            // Name
            if (nameText != null)
                nameText.text = card.CardName ?? "Unnamed";

            // Rank
            if (rankText != null)
                rankText.text = card.Rank > 0 ? card.Rank.ToString() : "-";

            // Suits
            UpdateSuits(card);

            // Abilities
            if (abilitiesText != null)
            {
                if (uiDefinitions != null && card.Abilities.Count > 0)
                {
                    abilitiesText.text = string.Join(", ",
                        card.Abilities.ConvertAll(a => uiDefinitions.GetStringForAbility(a)));
                }
                else
                {
                    abilitiesText.text = string.Empty;
                }
            }

            // Lock requirements
            if (lockRequirementsText != null)
            {
                if (uiDefinitions != null && card.LockRequirements.Count > 0)
                {
                    var lockReqs = card.LockRequirements.ConvertAll(r => uiDefinitions.GetStringForRequirement(r));
                    lockRequirementsText.text = lockPrefix + string.Join(" or ", lockReqs);
                }
                else
                {
                    lockRequirementsText.text = string.Empty;
                }
            }

            // Hold requirements
            if (holdRequirementsText != null)
            {
                if (uiDefinitions != null && card.HoldRequirements.Count > 0)
                {
                    var holdReqs = card.HoldRequirements.ConvertAll(r => uiDefinitions.GetStringForRequirement(r));
                    holdRequirementsText.text = holdPrefix + string.Join(" or ", holdReqs);
                }
                else
                {
                    holdRequirementsText.text = string.Empty;
                }
            }

            // Illustration
            if (illustration != null) {
                illustration.sprite = card.Definition.Illustration;
            }
        }

        private void ClearView()
        {
            if (nameText != null) nameText.text = string.Empty;
            if (rankText != null) rankText.text = string.Empty;
            if (abilitiesText != null) abilitiesText.text = string.Empty;
            if (lockRequirementsText != null) lockRequirementsText.text = string.Empty;
            if (holdRequirementsText != null) holdRequirementsText.text = string.Empty;

            if (suitIcons != null)
            {
                foreach (var icon in suitIcons)
                {
                    if (icon != null)
                    {
                        icon.sprite = null;
                        icon.enabled = false;
                    }
                }
            }
        }

        private void UpdateSuits(Card card)
        {
            if (suitIcons == null || uiDefinitions == null)
                return;

            // Clear first
            foreach (var icon in suitIcons)
            {
                if (icon != null)
                {
                    icon.sprite = null;
                    icon.enabled = false;
                }
            }

            // Assign suits to slots
            for (int i = 0; i < card.Suits.Count && i < suitIcons.Count; i++)
            {
                var sprite = uiDefinitions.GetSpriteForSuit(card.Suits[i]);
                if (sprite != null && suitIcons[i] != null)
                {
                    suitIcons[i].sprite = sprite;
                    suitIcons[i].enabled = true;
                }
            }
        }

        public void SetFaceDown(bool isFaceDown) {
            if (faceDownRoot != null) faceDownRoot.SetActive(isFaceDown);
            if (faceUpRoot != null) faceUpRoot.SetActive(!isFaceDown);
        }
    }
}