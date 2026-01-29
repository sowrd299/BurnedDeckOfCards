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

        public DialogLineDefinition GetNextDialogLine(Player player, Player opponent)
        {
            if (definitions == null || definitions.Lines == null) return null;

            foreach (var line in definitions.Lines)
            {
                if (seenLines.Contains(line)) continue;

                bool allConditionsMet = true;
                foreach (var condition in line.AllConditions)
                {
                    if (!IsConditionMet(condition, player, opponent))
                    {
                        allConditionsMet = false;
                        break;
                    }
                }

                if (allConditionsMet) {
                    Debug.Log($"Found dialog line: {line.Speaker.Card.CardName} - {line.Text}");
                    return line;
                }
            }

            return null;
        }

        private bool IsConditionMet(DialogCondition condition, Player player, Player opponent)
        {
            if (condition.Card == null) return false;

            foreach (var zone in condition.AllowedZones)
            {
                List<Card> cardsInZone = GetCardsInZone(zone, player, opponent);
                if (cardsInZone != null && cardsInZone.Exists(c => c.Definition == condition.Card))
                {
                    return true;
                }
            }

            return false;
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
