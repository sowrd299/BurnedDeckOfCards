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

        [Header("Suit Sprites")]
        [SerializeField] private List<SuitSpriteMapping> suitSprites;

        [Header("Ability Strings")]
        [SerializeField] private List<AbilityStringMapping> abilityStrings;

        [Header("Requirement Strings")]
        [SerializeField] private List<RequirementStringMapping> requirementStrings;

        // --- Public Lookup Methods ---
        public Sprite GetSpriteForSuit(Suit suit)
        {
            var mapping = suitSprites.Find(m => m.suit == suit);
            return mapping != null ? mapping.sprite : null;
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
    }
}

