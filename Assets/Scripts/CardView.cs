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
        [SerializeField] private GameObject lockIconRoot; // Header icon ("Lock")
        [SerializeField] private GameObject holdIconRoot; // Header icon ("Hold")
        [SerializeField] private List<SpriteRenderer> lockRequirementIcons;
        [SerializeField] private List<SpriteRenderer> holdRequirementIcons;
        [SerializeField] private List<SpriteRenderer> abilityIcons;

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

            // Ability Icons
            UpdateIcons(abilityIcons, card.Abilities, (a) => uiDefinitions.GetSpriteForAbility(a));

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

            // Lock Icons
            if (lockIconRoot != null) lockIconRoot.SetActive(card.LockRequirements.Count > 0);
            UpdateIcons(lockRequirementIcons, card.LockRequirements, (r) => uiDefinitions.GetSpriteForRequirement(r));

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

            // Hold Icons
            if (holdIconRoot != null) holdIconRoot.SetActive(card.HoldRequirements.Count > 0);
            UpdateIcons(holdRequirementIcons, card.HoldRequirements, (r) => uiDefinitions.GetSpriteForRequirement(r));

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

        private void UpdateIcons<T>(List<SpriteRenderer> icons, List<T> data, System.Func<T, Sprite> spriteSelector)
        {
            if (icons == null || uiDefinitions == null) return;

            // Clear
            foreach(var icon in icons) {
                if (icon != null) {
                    icon.sprite = null;
                    icon.enabled = false;
                }
            }

            // Assign
            if (data == null) return;

            for (int i = 0; i < data.Count && i < icons.Count; i++) {
                var sprite = spriteSelector(data[i]);
                if (sprite != null && icons[i] != null) {
                    icons[i].sprite = sprite;
                    icons[i].enabled = true;
                }
            }
        }
    }
}