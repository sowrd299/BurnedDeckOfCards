using TMPro;
using UnityEngine;
using System.Collections.Generic;

namespace Ashworld
{
    public class CardView : MonoBehaviour
    {
        [System.Serializable]
        public struct IconSlot {
            public SpriteRenderer mainIcon;
            public SpriteRenderer typeIcon;
        }
        
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

        [Header("Icon Pool")]
        [SerializeField] private List<IconSlot> iconSlots;

        [Header("State UI")]
        [SerializeField] private GameObject faceDownRoot;
        [SerializeField] private GameObject faceUpRoot; // Optional wrapper for content
        [SerializeField] private SpriteRenderer background; // Reference to background sprite
        [SerializeField] private GameObject exhaustedRoot; // Object to enable when exhausted
        [SerializeField] private SpriteRenderer vignette;
        [SerializeField] private Color exhaustedBackgroundColor = new Color(0.85f, 0.85f, 0.85f, 1f);

        [Header("Vignette Colors")]
        [SerializeField] private Color canUseColor = new Color(1f, 1f, 0.8f, 0.3f);
        [SerializeField] private Color canBeAttackedColor = new Color(0.8f, 0.1f, 0.1f, 0.6f);
        [SerializeField] private Color exhaustedColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        [SerializeField] private Color canUseExhaustedColor = new Color(0.5f, 0.5f, 0.3f, 0.4f);
        [SerializeField] private Color defaultColor = new Color(1f, 1f, 1f, 0f);
        [SerializeField] private float vignetteLerpSpeed = 15f;

        [Header("Animations")]
        [SerializeField] private AttackAnimationView attackAnim;
        [SerializeField] private FireEffectView fireEffect;

        public AttackAnimationView AttackAnim => attackAnim;
        public FireEffectView FireEffect => fireEffect;

        private Card currentCard;
        public Card Card => currentCard;

        private bool canUse;
        private bool canBeAttacked;
        private Color targetVignetteColor;

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

            canUse = false;
            canBeAttacked = false;
            targetVignetteColor = defaultColor;
            if (vignette != null) vignette.color = defaultColor;

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

            // Icon Pool Update (Prioritized: Hold > Lock > Ability > Cost)
            UpdateIconSlots(card);

            // Illustration
            if (illustration != null) {
                illustration.sprite = card.Definition.Illustration;
            }

            // Exhaustion State
            UpdateExhaustedStatus();
        }

        public void UpdateExhaustedStatus() {
            if (currentCard == null) return;
            if (background != null) background.color = currentCard.IsExhausted ? exhaustedBackgroundColor : Color.white;
            if (exhaustedRoot != null) exhaustedRoot.SetActive(currentCard.IsExhausted);
            RefreshVignette();
        }

        public void SetCanUse(bool canUse) {
            this.canUse = canUse;
            RefreshVignette();
        }

        public void SetCanBeAttacked(bool canBeAttacked) {
            this.canBeAttacked = canBeAttacked;
            RefreshVignette();
        }

        private void RefreshVignette() {
            if (vignette == null) return;

            if (canBeAttacked) {
                targetVignetteColor = canBeAttackedColor;
            } else if (canUse && currentCard != null && currentCard.IsExhausted) {
                targetVignetteColor = canUseExhaustedColor;
            } else if (canUse) {
                targetVignetteColor = canUseColor;
            } else if (currentCard != null && currentCard.IsExhausted) {
                targetVignetteColor = exhaustedColor;
            } else {
                targetVignetteColor = defaultColor;
            }
        }

        private void Update() {
            if (vignette != null) {
                vignette.color = Color.Lerp(vignette.color, targetVignetteColor, Time.deltaTime * vignetteLerpSpeed);
            }
        }

        private void ClearView()
        {
            if (nameText != null) nameText.text = string.Empty;
            if (rankText != null) rankText.text = string.Empty;
            if (abilitiesText != null) abilitiesText.text = string.Empty;
            if (lockRequirementsText != null) lockRequirementsText.text = string.Empty;
            if (holdRequirementsText != null) holdRequirementsText.text = string.Empty;

            if (iconSlots != null)
            {
                foreach (var slot in iconSlots)
                {
                    if (slot.mainIcon != null) { slot.mainIcon.sprite = null; slot.mainIcon.enabled = false; }
                    if (slot.typeIcon != null) { slot.typeIcon.sprite = null; slot.typeIcon.enabled = false; }
                }
            }

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

        private void UpdateIconSlots(Card card)
        {
            if (iconSlots == null || uiDefinitions == null) return;

            // 1. Reset all slots
            foreach (var slot in iconSlots)
            {
                if (slot.mainIcon != null) { slot.mainIcon.sprite = null; slot.mainIcon.enabled = false; }
                if (slot.typeIcon != null) { slot.typeIcon.sprite = null; slot.typeIcon.enabled = false; }
            }

            int currentSlot = 0;

            // 2. Priority 1: Hold Requirements
            foreach (var req in card.HoldRequirements)
            {
                if (currentSlot >= iconSlots.Count) break;
                AssignIconSlot(currentSlot, uiDefinitions.GetSpriteForRequirement(req), uiDefinitions.holdTypeSprite);
                currentSlot++;
            }

            // 3. Priority 2: Lock Requirements
            foreach (var req in card.LockRequirements)
            {
                if (currentSlot >= iconSlots.Count) break;
                AssignIconSlot(currentSlot, uiDefinitions.GetSpriteForRequirement(req), uiDefinitions.lockTypeSprite);
                currentSlot++;
            }

            // 4. Priority 3: Abilities
            foreach (var ability in card.Abilities)
            {
                if (currentSlot >= iconSlots.Count) break;
                AssignIconSlot(currentSlot, uiDefinitions.GetSpriteForAbility(ability), null);
                currentSlot++;
            }

            // 5. Priority 4: History Cost
            if (card.HistoryCost > 0)
            {
                if (currentSlot < iconSlots.Count)
                {
                    Sprite typeSprite = (card.HistoryCost > 1) ? uiDefinitions.historySprite : null;
                    AssignIconSlot(currentSlot, uiDefinitions.historySprite, typeSprite);
                    currentSlot++;
                }
            }
        }

        private void AssignIconSlot(int index, Sprite main, Sprite type)
        {
            if (index < 0 || index >= iconSlots.Count) return;
            var slot = iconSlots[index];
            
            if (slot.mainIcon != null && main != null)
            {
                slot.mainIcon.sprite = main;
                slot.mainIcon.enabled = true;
            }

            if (slot.typeIcon != null)
            {
                if (type != null)
                {
                    slot.typeIcon.sprite = type;
                    slot.typeIcon.enabled = true;
                }
                else
                {
                    slot.typeIcon.enabled = false;
                }
            }
        }
    }
}