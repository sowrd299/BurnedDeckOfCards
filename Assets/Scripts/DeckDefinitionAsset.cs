using System.Collections.Generic;
using UnityEngine;


namespace Ashworld {

    [CreateAssetMenu( fileName = "NewDeckDefinition", menuName = "Ashworld/Deck Definition", order = 1)]
    public class DeckDefinitionAsset : ScriptableObject
    {
        [SerializeField] private DeckDefinition deckDefinition = new DeckDefinition();

        public DeckDefinition Definition => deckDefinition;

        /// <summary>
        /// Convenience wrapper to get runtime card list from this asset.
        /// </summary>
        public List<Card> GetCards(string ownerId = null)
        {
            return deckDefinition.GetCards(ownerId);
        }
    }

}