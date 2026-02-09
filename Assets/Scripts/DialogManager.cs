using System.Collections.Generic;
using UnityEngine;

namespace Ashworld
{
    public class DialogManager : MonoBehaviour
    {
        [SerializeField] private DialogDefinitions definitions;
        
        private HashSet<DialogLineDefinition> seenLines = new HashSet<DialogLineDefinition>();

        public void MarkSeen(DialogLineDefinition line)
        {
            if (line != null) seenLines.Add(line);
        }

        public void Reset()
        {
            seenLines.Clear();
        }

        public (Card, DialogLineDefinition) GetNextDialogLine(Player player, Player opponent)
        {
            if (definitions == null || definitions.Lines == null) return (null, null);

            foreach (var line in definitions.Lines)
            {
                if (seenLines.Contains(line)) continue;

                Card speakerCard = FindCardMeetingCondition(line.Speaker, player, opponent);
                if (speakerCard == null) continue;

                bool allConditionsMet = true;
                if (line.OtherConditions != null)
                {
                    foreach (var condition in line.OtherConditions)
                    {
                        if (!IsConditionMet(condition, player, opponent))
                        {
                            allConditionsMet = false;
                            break;
                        }
                    }
                }

                if (allConditionsMet) {
                    Debug.Log($"Found dialog line: {line.Speaker.Card.CardName} - {line.Text}");
                    return (speakerCard, line);
                }
            }

            return (null, null);
        }

        private bool IsConditionMet(DialogCondition condition, Player player, Player opponent)
        {
            return FindCardMeetingCondition(condition, player, opponent) != null;
        }

        private Card FindCardMeetingCondition(DialogCondition condition, Player player, Player opponent)
        {
            if (condition.Card == null) return null;

            foreach (var zone in condition.AllowedZones)
            {
                List<Card> cardsInZone = GetCardsInZone(zone, player, opponent);
                if (cardsInZone != null)
                {
                    Card card = cardsInZone.Find(c => c.Definition == condition.Card);
                    if (card != null) return card;
                }
            }

            return null;
        }

        private List<Card> GetCardsInZone(DialogZone zone, Player player, Player opponent)
        {
            switch (zone)
            {
                case DialogZone.PlayerHand: return player.hand;
                case DialogZone.PlayerParty: return player.party;
                case DialogZone.PlayerDefense: return player.defense;
                case DialogZone.OpponentHand: return opponent.hand;
                case DialogZone.OpponentParty: return opponent.party;
                case DialogZone.OpponentDefense: return opponent.defense;
                default: return null;
            }
        }
    }
}
