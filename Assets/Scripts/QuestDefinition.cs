using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ashworld {

    [Serializable]
    public class QuestChapterDefinition {
        public List<CardDefinitionAsset> cards;
    }

    [Serializable]
    public class QuestDefinition
    {
        [SerializeField] private List<QuestChapterDefinition> chapters;

        public List<Card> GetCardsForChapter(int chapterInd, string ownerId = "") {

            List<Card> cards = new List<Card>();

            if (chapterInd < chapters.Count) {
                foreach (CardDefinitionAsset cardDefinition in chapters[chapterInd].cards) {
                    cards.Add(new Card(cardDefinition.Definition, ownerId));
                }
            }

            return cards;
        }
    }

}