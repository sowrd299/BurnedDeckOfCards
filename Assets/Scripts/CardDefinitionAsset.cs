using UnityEngine;

namespace Ashworld
{
    [CreateAssetMenu( fileName = "NewCardDefinition", menuName = "Ashworld/Card Definition", order = 0)]
    public class CardDefinitionAsset : ScriptableObject
    {
        [SerializeField] private CardDefinition definition = new CardDefinition();

        public CardDefinition Definition => definition;
    }
}
