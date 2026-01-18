using System.Collections.Generic;
using UnityEngine;


namespace Ashworld {

    [CreateAssetMenu( fileName = "NewQuestDefinition", menuName = "Ashworld/Quest Definition", order = 1)]
    public class QuestDefinitionAsset : ScriptableObject
    {
        [SerializeField] private QuestDefinition questDefinition = new QuestDefinition();

        public QuestDefinition Definition => questDefinition;

        /// <summary>
        /// Convenience wrapper to get runtime card list from this asset.
        /// </summary>
        public List<Card> GetCardsForChapter(int chapterInd, string ownerId = null)
        {
            return questDefinition.GetCardsForChapter(chapterInd, ownerId);
        }
    }

}