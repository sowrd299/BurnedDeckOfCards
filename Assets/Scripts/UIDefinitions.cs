using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ashworld
{
    [CreateAssetMenu(fileName = "UIDefinitions", menuName = "Ashworld/UIDefinitions", order = 0)]
    public class UIDefinitions : ScriptableObject
    {
        [Serializable]
        public class SuitSpriteMapping
        {
            public Suit suit;
            public Sprite sprite;
            public Color color = Color.white;
        }

        [Serializable]
        public class AbilityStringMapping
        {
            public SpecialAbility ability;
            [TextArea] public string displayText;
            public Sprite sprite;
        }

        [Serializable]
        public class RequirementStringMapping
        {
            public Requirement requirement;
            [TextArea] public string displayText;
            public Sprite sprite;
        }

        [Serializable]
        public class TriggerStringMapping
        {
            public AbilityTrigger trigger;
            [TextArea] public string displayText;
            public Sprite sprite;
        }

        [Serializable]
        public class EffectStringMapping
        {
            public AbilityEffect effect;
            [TextArea] public string displayText;
        }

        [Header("Suit Sprites")]
        [SerializeField] private List<SuitSpriteMapping> suitSprites;

        [Header("Ability Strings")]
        [SerializeField] private List<AbilityStringMapping> abilityStrings;
        [SerializeField] public Sprite historySprite;

        [Header("Requirement Strings")]
        [SerializeField] private List<RequirementStringMapping> requirementStrings;
        public Sprite holdTypeSprite;
        public Sprite lockTypeSprite;

        [Header("Trigger and Effect Strings")]
        [SerializeField] private List<TriggerStringMapping> triggerStrings;
        [SerializeField] private List<EffectStringMapping> effectStrings;

        // --- Public Lookup Methods ---
        public Sprite GetSpriteForSuit(Suit suit)
        {
            var mapping = suitSprites.Find(m => m.suit == suit);
            return mapping != null ? mapping.sprite : null;
        }

        public Color GetColorForSuit(Suit suit)
        {
            var mapping = suitSprites.Find(m => m.suit == suit);
            return mapping != null ? mapping.color : Color.clear;
        }

        public string GetStringForAbility(SpecialAbility ability)
        {
            var mapping = abilityStrings.Find(m => m.ability == ability);
            return mapping != null ? mapping.displayText : ability.ToString();
        }
        
        public Sprite GetSpriteForAbility(SpecialAbility ability)
        {
            var mapping = abilityStrings.Find(m => m.ability == ability);
            return mapping != null ? mapping.sprite : null;
        }

        public string GetStringForRequirement(Requirement requirement)
        {
            var mapping = requirementStrings.Find(m => m.requirement == requirement);
            return mapping != null ? mapping.displayText : requirement.ToString();
        }
        
        public Sprite GetSpriteForRequirement(Requirement requirement)
        {
            var mapping = requirementStrings.Find(m => m.requirement == requirement);
            return mapping != null ? mapping.sprite : null;
        }

        public string GetStringForTrigger(AbilityTrigger trigger)
        {
            var mapping = triggerStrings.Find(m => m.trigger == trigger);
            return mapping != null ? mapping.displayText : trigger.ToString();
        }

        public Sprite GetSpriteForTrigger(AbilityTrigger trigger)
        {
            var mapping = triggerStrings.Find(m => m.trigger == trigger);
            return mapping != null ? mapping.sprite : null;
        }

        public string GetStringForEffect(AbilityEffect effect)
        {
            var mapping = effectStrings.Find(m => m.effect == effect);
            return mapping != null ? mapping.displayText : effect.ToString();
        }
    }
}

