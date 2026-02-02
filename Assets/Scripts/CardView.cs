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
            public TextMeshPro displayText;
            public GameObject unmetRoot;
            public GameObject metRoot;
        }
        
        [Header("Definitions")]
        [SerializeField] private UIDefinitions uiDefinitions;

        [Header("UI References")]
        [SerializeField] private TextMeshPro nameText;
        [SerializeField] private TextMeshPro subtitleText;
        [SerializeField] private TextMeshPro subtypesText;
        [SerializeField] private TextMeshPro rankText;
        [SerializeField] private TextMeshPro boonRankText;
        [SerializeField] private List<SpriteRenderer> suitIcons; // slots for suits
        [SerializeField] private SpriteRenderer illustration;

        [Header("Requirement UI")]
        [SerializeField] private string allTextPrefix = "<line-height=65%>";
        [SerializeField] private string lockPrefix = "Lock: ";
        [SerializeField] private string holdPrefix = "Hold: ";

        [Header("Icon Pool")]
        [SerializeField] private List<IconSlot> iconSlots;

        [Header("Dialog UI")]
        [SerializeField] private GameObject dialogRoot;
        [SerializeField] private TextMeshPro dialogText;

        [Header("State UI")]
        [SerializeField] private GameObject faceDownRoot;
        [SerializeField] private GameObject faceUpRoot; // Optional wrapper for content
        [SerializeField] private SpriteRenderer background; // Reference to background sprite
        [SerializeField] private GameObject exhaustedRoot; // Object to enable when exhausted
        [SerializeField] private GameObject shadowRoot;
        [SerializeField] private SpriteRenderer vignette;
        [SerializeField] private Color exhaustedBackgroundColor = new Color(0.85f, 0.85f, 0.85f, 1f);

        [Header("Vignette Colors")]
        [SerializeField] private Color canUseColor = new Color(1f, 1f, 0.8f, 0.3f);
        [SerializeField] private Color canBeAttackedColor = new Color(0.8f, 0.1f, 0.1f, 0.6f);
        [SerializeField] private Color exhaustedColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        [SerializeField] private Color canUseExhaustedColor = new Color(0.5f, 0.5f, 0.3f, 0.4f);
        [SerializeField] private Color defaultColor = new Color(1f, 1f, 1f, 0f);

        [Header("Hover Colors")]
        [SerializeField] private Color canUseHoverColor = new Color(1f, 1f, 1f, 0.4f);
        [SerializeField] private Color canUseExhaustedHoverColor = new Color(0.7f, 0.7f, 0.5f, 0.5f);
        [SerializeField] private Color canBeAttackedHoverColor = new Color(1f, 0.3f, 0.3f, 0.7f);

        [SerializeField] private float vignetteLerpSpeed = 15f;

        [Header("Animations")]
        [SerializeField] private AttackAnimationView attackAnim;
        [SerializeField] private FireEffectView fireEffect;
        [SerializeField] private float movementLerpSpeed = 10f;
        [SerializeField] private float rotationLerpSpeed = 10f;

        public AttackAnimationView AttackAnim => attackAnim;
        public FireEffectView FireEffect => fireEffect;

        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private bool isPositionInitialized;
        private bool isRotationInitialized;

        private Card currentCard;
        public Card Card => currentCard;

        private bool canUse;
        private bool canBeAttacked;
        private bool isHovered;
        private Suit highlightedSuit = Suit.None;
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
            isHovered = false;
            highlightedSuit = Suit.None;
            targetVignetteColor = defaultColor;
            if (vignette != null) vignette.color = defaultColor;

            isPositionInitialized = false;

            // Name & Subtitle & Subtypes
            if (nameText != null)
                nameText.text = allTextPrefix + (card.CardName ?? "Unnamed");

            if (subtitleText != null)
                subtitleText.text = allTextPrefix + (card.Definition.Subtitle ?? string.Empty);

            if (subtypesText != null && card.Definition.Subtypes != null)
            {
                if (card.Definition.Subtypes.Count > 0)
                    subtypesText.text = allTextPrefix + string.Join(". ", card.Definition.Subtypes) + ".";
                else
                    subtypesText.text = string.Empty;
            }

            // Rank
            if (rankText != null)
            {
                if (!card.HasAbility(SpecialAbility.Boon))
                    rankText.text = card.Rank > 0 ? card.Rank.ToString() : "-";
                else
                    rankText.text = string.Empty;
            }

            if (boonRankText != null) {
                if (card.HasAbility(SpecialAbility.Boon))
                    boonRankText.text = card.Rank > 0 ? "+" + card.Rank.ToString() : "-";
                else
                    boonRankText.text = string.Empty;
            }

            // Suits
            UpdateSuits(card);

            // Icon Pool Update (Prioritized: Hold > Lock > Ability > Cost)
            UpdateIconSlots(card);

            // Illustration
            if (illustration != null) {
                illustration.sprite = card.Definition.Illustration;
            }

            // Exhaustion State
            UpdateExhaustedStatus();

            // Dialog
            HideDialog();
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

        public void SetHovered(bool hovered) {
            this.isHovered = hovered;
            RefreshVignette();
        }

        public void SetHighlightedSuit(Suit suit) {
            this.highlightedSuit = suit;
            RefreshVignette();
        }

        public void SetTargetPosition(Vector3 position, bool setImmediate = false) {
            targetPosition = position;
            if (!isPositionInitialized || setImmediate) {
                transform.position = position;
                isPositionInitialized = true;
            }
        }

        public void SetTargetRotation(Quaternion rotation, bool setImmediate = false) {
            targetRotation = rotation;
            if (!isRotationInitialized || setImmediate) {
                transform.rotation = rotation;
                isRotationInitialized = true;
            }
        }

        public Vector3 GetTargetPosition() => targetPosition;

        private void RefreshVignette() {
            if (vignette == null) return;

            if (highlightedSuit != Suit.None && uiDefinitions != null) {
                targetVignetteColor = uiDefinitions.GetColorForSuit(highlightedSuit);
            } else if (canBeAttacked) {
                targetVignetteColor = isHovered ? canBeAttackedHoverColor : canBeAttackedColor;
            } else if (canUse && currentCard != null && currentCard.IsExhausted) {
                targetVignetteColor = isHovered ? canUseExhaustedHoverColor : canUseExhaustedColor;
            } else if (canUse) {
                targetVignetteColor = isHovered ? canUseHoverColor : canUseColor;
            } else if (currentCard != null && currentCard.IsExhausted) {
                targetVignetteColor = exhaustedColor;
            } else {
                targetVignetteColor = defaultColor;
            }

            if (shadowRoot != null) shadowRoot.SetActive(isHovered && canUse);
        }

        private void Update() {
            if (vignette != null) {
                vignette.color = Color.Lerp(vignette.color, targetVignetteColor, Time.deltaTime * vignetteLerpSpeed);
            }

            if (isPositionInitialized) {
                Vector3 lerpPos = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * movementLerpSpeed);
                lerpPos.z = targetPosition.z;
                transform.position = lerpPos;

                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationLerpSpeed);
            }
        }

        public void ShowDialog(string text)
        {
            if (dialogRoot != null) dialogRoot.SetActive(true);
            if (dialogText != null) dialogText.text = text;
        }

        public void HideDialog()
        {
            if (dialogRoot != null) dialogRoot.SetActive(false);
        }

        private void ClearView()
        {
            if (nameText != null) nameText.text = string.Empty;
            if (subtitleText != null) subtitleText.text = string.Empty;
            if (subtypesText != null) subtypesText.text = string.Empty;
            if (rankText != null) rankText.text = string.Empty;
            if (boonRankText != null) boonRankText.text = string.Empty;

            if (iconSlots != null)
            {
                foreach (var slot in iconSlots)
                {
                    if (slot.mainIcon != null) { slot.mainIcon.sprite = null; slot.mainIcon.enabled = false; }
                    if (slot.typeIcon != null) { slot.typeIcon.sprite = null; slot.typeIcon.enabled = false; }
                    if (slot.displayText != null) slot.displayText.text = string.Empty;
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

            ClearRequirements();
            isPositionInitialized = false;
        }

        public void ClearRequirements() 
        {
            if (iconSlots == null) return;
            foreach (var slot in iconSlots)
            {
                if (slot.unmetRoot != null) slot.unmetRoot.SetActive(false);
                if (slot.metRoot != null) slot.metRoot.SetActive(false);
            }
        }

        public void UpdateRequirements(Player zoneOwner)
        {
            if (currentCard == null || iconSlots == null || uiDefinitions == null || zoneOwner == null) return;

            bool inParty = zoneOwner.party.Contains(currentCard);
            bool inDefense = zoneOwner.defense.Contains(currentCard);
            bool isOwnedByZoneOwner = currentCard.OwnerId == zoneOwner.Id;

            if (!inParty && !inDefense)
            {
                ClearRequirements();
                return;
            }

            List<CardDefinition> partyDefs = zoneOwner.party.ConvertAll(c => c.Definition);
            List<CardDefinition> defenseDefs = zoneOwner.defense.ConvertAll(c => c.Definition);
            bool heroismAvailable = zoneOwner.HeroismAvailable;

            int currentSlot = 0;

            // 1. Hold Requirements (Priority 1)
            foreach (var req in currentCard.HoldRequirements)
            {
                if (currentSlot >= iconSlots.Count) break;
                var slot = iconSlots[currentSlot];
                
                if (isOwnedByZoneOwner)
                {
                    bool isMet = CardDefinition.MeetsRequirement(req, partyDefs, defenseDefs, heroismAvailable);
                    if (slot.metRoot != null) slot.metRoot.SetActive(isMet);
                    if (slot.unmetRoot != null) slot.unmetRoot.SetActive(false);
                }
                else
                {
                    if (slot.metRoot != null) slot.metRoot.SetActive(false);
                    if (slot.unmetRoot != null) slot.unmetRoot.SetActive(false);
                }
                currentSlot++;
            }

            // 2. Lock Requirements (Priority 2)
            foreach (var req in currentCard.LockRequirements)
            {
                if (currentSlot >= iconSlots.Count) break;
                var slot = iconSlots[currentSlot];

                if (inDefense)
                {
                    bool isMet = CardDefinition.MeetsRequirement(req, partyDefs, defenseDefs, heroismAvailable);
                    if (slot.unmetRoot != null) slot.unmetRoot.SetActive(!isMet);
                    if (slot.metRoot != null) slot.metRoot.SetActive(false);
                }
                else
                {
                    if (slot.metRoot != null) slot.metRoot.SetActive(false);
                    if (slot.unmetRoot != null) slot.unmetRoot.SetActive(false);
                }
                currentSlot++;
            }

            // 3. Clear remaining slots (Abilities/Cost don't have req visual state yet)
            for (int i = currentSlot; i < iconSlots.Count; i++)
            {
                var slot = iconSlots[i];
                if (slot.unmetRoot != null) slot.unmetRoot.SetActive(false);
                if (slot.metRoot != null) slot.metRoot.SetActive(false);
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

        public bool IsFaceDown { get; private set; }

        public void SetFaceDown(bool isFaceDown) {
            IsFaceDown = isFaceDown;
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
                if (slot.displayText != null) slot.displayText.text = string.Empty;
            }

            int currentSlot = 0;

            // 2. Priority 1: Hold Requirements
            foreach (var req in card.HoldRequirements)
            {
                if (currentSlot >= iconSlots.Count) break;
                string label = holdPrefix + uiDefinitions.GetStringForRequirement(req);
                AssignIconSlot(currentSlot, uiDefinitions.GetSpriteForRequirement(req), uiDefinitions.holdTypeSprite, label);
                currentSlot++;
            }

            // 3. Priority 2: Lock Requirements
            foreach (var req in card.LockRequirements)
            {
                if (currentSlot >= iconSlots.Count) break;
                string label = lockPrefix + uiDefinitions.GetStringForRequirement(req);
                AssignIconSlot(currentSlot, uiDefinitions.GetSpriteForRequirement(req), uiDefinitions.lockTypeSprite, label);
                currentSlot++;
            }

            // 4. Priority 3: Triggered Abilities
            foreach (var triggered in card.TriggeredAbilities)
            {
                if (currentSlot >= iconSlots.Count) break;
                string triggerText = uiDefinitions.GetStringForTrigger(triggered.trigger);
                string effectText = uiDefinitions.GetStringForEffect(triggered.effect);
                string label = $"{triggerText}, {effectText}";
                AssignIconSlot(currentSlot, uiDefinitions.GetSpriteForTrigger(triggered.trigger), null, label);
                currentSlot++;
            }

            // 5. Priority 4: Abilities
            foreach (var ability in card.Abilities)
            {
                if (currentSlot >= iconSlots.Count) break;
                string label = uiDefinitions.GetStringForAbility(ability);
                AssignIconSlot(currentSlot, uiDefinitions.GetSpriteForAbility(ability), null, label);
                currentSlot++;
            }

            // 6. Priority 5: History Cost
            if (card.HistoryCost > 0)
            {
                if (currentSlot < iconSlots.Count)
                {
                    Sprite typeSprite = (card.HistoryCost > 1) ? uiDefinitions.historySprite : null;
                    string label = card.HistoryCost == 1 ? "Costs 1 History or Card" : $"Costs {card.HistoryCost} History or Cards";
                    AssignIconSlot(currentSlot, uiDefinitions.historySprite, typeSprite, label);
                    currentSlot++;
                }
            }
        }

        private void AssignIconSlot(int index, Sprite main, Sprite type, string text)
        {
            if (index < 0 || index >= iconSlots.Count) return;
            var slot = iconSlots[index];
            
            if (slot.mainIcon != null && main != null)
            {
                slot.mainIcon.sprite = main;
                slot.mainIcon.enabled = true;
            }

            if (slot.displayText != null)
            {
                slot.displayText.text = allTextPrefix + text;
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