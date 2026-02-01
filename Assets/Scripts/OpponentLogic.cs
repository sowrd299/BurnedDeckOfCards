using System.Collections.Generic;
using UnityEngine;

namespace Ashworld {

    // Simple data structure for an AI action
    public class OpponentAction {
        public enum ActionType { Play, Attack, Advance }
        
        public ActionType Type;
        public Card CardToPlay;      // For Play
        public bool PlayToDefense;   // For Play: True = Play to opponent's defense (Human.Defense), False = Play to own party (AI.Party)
        public Card Attacker;        // For Attack
        public Card Defender;        // For Attack
    }

    public partial class GameLogic
    {
        private OpponentAction GetBestAction(Player opponentPlayer, Player humanPlayer) {
            
            // PRIORITY 1: Defensive Attack (AI Defense -> Human Party)
            OpponentAction defensiveAttackAction = GetBestDefenseAttack(opponentPlayer, humanPlayer);
            if (defensiveAttackAction != null) return defensiveAttackAction;

            // PRIORITY 2: Strategic Defend (ONLY if Human has <= Party + 2 defenders)
            // Play into Human.Defense
            // Heuristic: Lock requirement NOT already present AND NOT met by Human.
            if (humanPlayer.defense.Count <= humanPlayer.party.Count + 2) {
                OpponentAction strategicDefend = GetStrategicDefend(opponentPlayer, humanPlayer);
                if (strategicDefend != null) return strategicDefend;
            }

            // PRIORITY 3: Advance Own Quest (Button)
            // If we have enough dominance to advance, do it. 
            // This is "Advancing Quest" in the literal sense.
            if (opponentPlayer.CanAdvance()) {
                return new OpponentAction { Type = OpponentAction.ActionType.Advance };
            }

            // PRIORITY 4: Rank Advantage Defend
            OpponentAction rankAdvantageDefend = GetRankAdvantageDefend(opponentPlayer, humanPlayer);
            if (rankAdvantageDefend != null) return rankAdvantageDefend;

            // PRIORITY 5: Party Attack (AI Party -> AI Defense blocker)
            OpponentAction partyAttackAction = GetBestPartyAttack(opponentPlayer);
            if (partyAttackAction != null) return partyAttackAction;

            // PRIORITY 6: Strategic Play to Party
            // Play into AI.Party
            // Heuristic: Hold Requirement Met > Hold Requirement Unmet
            OpponentAction strategicPlayToPartyAction = GetStrategicPlayToParty(opponentPlayer);
            if (strategicPlayToPartyAction != null) return strategicPlayToPartyAction;

            // PRIORITY 7: Fallback Play to Party
            // Play *any* valid card into AI.Party. Tie-break: High Rank.
            OpponentAction fallbackPlayToPartyAction = GetFallbackPlayToParty(opponentPlayer);
            if (fallbackPlayToPartyAction != null) return fallbackPlayToPartyAction;

            // PRIORITY 8: Fallback Defend
            // Play *any* valid card into Human.Defense. Tie-break: High Rank.
            OpponentAction fallbackDefendAction = GetFallbackDefend(opponentPlayer, humanPlayer);
            if (fallbackDefendAction != null) return fallbackDefendAction;

            // No valid moves
            return null;
        }

        private OpponentAction GetBestAttack(Player ai, Player targetPlayer, List<Card> attackerZone, List<Card> targetZone) {
            int bestRankDiff = int.MaxValue; 
            Card bestAttacker = null;
            Card bestTarget = null;

            foreach (var attacker in attackerZone) {
                if (attacker.IsExhausted) continue;
                if (attacker.OwnerId != ai.Id) continue; // Safety check

                foreach (var target in targetZone) {
                     if (CanCardAttack(ai, targetPlayer, attacker, target)) {
                         int diff = attacker.Rank - target.Rank; 
                         if (diff < bestRankDiff) {
                             bestRankDiff = diff;
                             bestAttacker = attacker;
                             bestTarget = target;
                         }
                     }
                }
            }

            if (bestAttacker != null) {
                return new OpponentAction { 
                    Type = OpponentAction.ActionType.Attack,
                    Attacker = bestAttacker,
                    Defender = bestTarget
                };
            }
            return null;
        }

        private OpponentAction GetBestDefenseAttack(Player ai, Player human) {
            // AI Defense cards (belonging to AI) attacking Human Party
            return GetBestAttack(ai, human, human.defense, human.party);
        }

        private OpponentAction GetBestPartyAttack(Player ai) {
            // AI Party cards attacking AI Defense blockers
            return GetBestAttack(ai, ai, ai.party, ai.defense);
        }

        private OpponentAction GetStrategicDefend(Player ai, Player human) {
            // Play into Human.Defense
            foreach (var card in ai.hand) {
                if (!CanPlayCard(ai, card, human)) continue;

                if (card.LockRequirements.Count > 0) {
                    List<CardDefinition> humanParty = human.party.ConvertAll(c => c.Definition);
                    List<CardDefinition> humanDefense = human.defense.ConvertAll(c => c.Definition);

                    foreach (var req in card.LockRequirements) {
                        bool alreadyImposed = human.defense.Exists(d => d.LockRequirements.Contains(req));
                        if (!alreadyImposed && !CardDefinition.MeetsRequirement(req, humanParty, humanDefense)) {
                            return new OpponentAction { Type = OpponentAction.ActionType.Play, CardToPlay = card, PlayToDefense = true };
                        }
                    }    
                }
            }
            return null;
        }

        private OpponentAction GetStrategicPlayToParty(Player ai) {
            // Play into AI.Party
            Card bestCard = null;
            int bestScore = -1; // 2 = Met, 1 = Unmet, 0 = No Hold

            foreach (var card in ai.hand) {
                if (!CanPlayCard(ai, card, ai)) continue;

                int score = 0;
                if (card.HoldRequirements.Count > 0) {
                    bool met = true;
                    List<CardDefinition> partyDefs = ai.party.ConvertAll(c => c.Definition);
                    List<CardDefinition> defenseDefs = ai.defense.ConvertAll(c => c.Definition);

                    foreach (var req in card.HoldRequirements) {
                        if (!CardDefinition.MeetsRequirement(req, partyDefs, defenseDefs)) {
                            met = false; 
                            break;
                        }
                    }
                    score = met ? 2 : 1;
                }

                if (score > bestScore) {
                    bestScore = score;
                    bestCard = card;
                }
            }

            if (bestCard != null && bestScore > 0) {
                return new OpponentAction { Type = OpponentAction.ActionType.Play, CardToPlay = bestCard, PlayToDefense = false }; 
            }
            return null;
        }

        private OpponentAction GetRankAdvantageDefend(Player ai, Player human) {
            foreach (var card in ai.hand) {
                if (!CanPlayCard(ai, card, human)) continue;
                if (card.HasAbility(SpecialAbility.Boon)) continue;

                // Simulate play to human's defense
                List<Card> simulatedDefense = new List<Card>(human.defense);
                simulatedDefense.Add(card);

                foreach (var partyCard in human.party) {
                    if (CanCardAttack(ai, card, partyCard, human.party, simulatedDefense)) {
                        return new OpponentAction { Type = OpponentAction.ActionType.Play, CardToPlay = card, PlayToDefense = true };
                    }
                }
            }
            return null;
        }
        
        private OpponentAction GetFallbackDefend(Player ai, Player human) {
            Card bestCard = null;
            foreach (var card in ai.hand) {
                 if (!CanPlayCard(ai, card, human)) continue;

                 if (bestCard == null || card.Rank > bestCard.Rank) {
                     bestCard = card;
                 }
            }
             if (bestCard != null) {
                return new OpponentAction { Type = OpponentAction.ActionType.Play, CardToPlay = bestCard, PlayToDefense = true };
            }
            return null;
        }

        private OpponentAction GetFallbackPlayToParty(Player ai) {
            Card bestCard = null;
            foreach (var card in ai.hand) {
                 if (!CanPlayCard(ai, card, ai)) continue;
                 
                 if (bestCard == null || card.Rank > bestCard.Rank) {
                     bestCard = card;
                 }
            }
             if (bestCard != null) {
                return new OpponentAction { Type = OpponentAction.ActionType.Play, CardToPlay = bestCard, PlayToDefense = false };
            }
            return null;
        }
    }
}
