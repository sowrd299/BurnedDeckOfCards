using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ashworld {

    [Serializable]
    public class QuestChapterDefinition {
        public List<CardDefinitionAsset> cards;
        public DeckDefinition treasureDeck;
    }

    [Serializable]
    public class QuestDefinition
    {
        [SerializeField] private List<QuestChapterDefinition> chapters;
        public int ChapterCount => chapters.Count;

        public List<Card> GetCardsForChapter(int chapterInd, string ownerId = "") {

            List<Card> cards = new List<Card>();

            if (chapterInd < chapters.Count) {
                foreach (CardDefinitionAsset cardDefinition in chapters[chapterInd].cards) {
                    cards.Add(new Card(cardDefinition.Definition, ownerId));
                }
            }

            return cards;
        }

        public Deck GetTreasureDeckForChapter(int chapterInd, string ownerId = "") {
            if (chapterInd < chapters.Count && chapters[chapterInd].treasureDeck != null) {
                return chapters[chapterInd].treasureDeck.CreateDeck(ownerId);
            }
            return new Deck();
        }

        public string GetChapterName(int index) {
            if (index < 0 || index >= chapters.Count) index = chapters.Count - 1;

            if (chapters[index].cards == null || chapters[index].cards.Count == 0) return "Empty";
            
            var def = chapters[index].cards[0].Definition;
            string name = def.CardName;
            if (!string.IsNullOrEmpty(def.Subtitle)) {
                name += ", " + def.Subtitle;
            }
            return name;
        }
    }

}