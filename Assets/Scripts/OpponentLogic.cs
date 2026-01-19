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
            
            // PRIORITY 1: Halt Player (Attack)
            // Rule: Find Opponent Party Card > Human Party Card (rank).
            // Tie-breaker: Lowest rank that wins.
            OpponentAction attackAction = GetBestAttack(opponentPlayer, humanPlayer);
            if (attackAction != null) return attackAction;

            // PRIORITY 2: Defend against Player (Strategic)
            // Play into Human.Defense
            // Heuristic: Lock requirement NOT already present AND NOT met by Human.
            OpponentAction strategicDefend = GetStrategicDefend(opponentPlayer, humanPlayer);
            if (strategicDefend != null) return strategicDefend;

            // PRIORITY 3: Advance Own Quest (Button)
            // If we have enough dominance to advance, do it. 
            // This is "Advancing Quest" in the literal sense.
            if (opponentPlayer.CanAdvance()) {
                return new OpponentAction { Type = OpponentAction.ActionType.Advance };
            }

            // PRIORITY 4: Advance Own Quest (Strategic Play)
            // Play into AI.Party
            // Heuristic: Hold Requirement Met > Hold Requirement Unmet
            OpponentAction strategicAdvance = GetStrategicPlayToParty(opponentPlayer);
            if (strategicAdvance != null) return strategicAdvance;

            // PRIORITY 5: Defend against Player (Fallback)
            // Play *any* valid card into Human.Defense. Tie-break: High Rank.
            OpponentAction fallbackDefend = GetFallbackDefend(opponentPlayer, humanPlayer);
            if (fallbackDefend != null) return fallbackDefend;

            // PRIORITY 6: Advance Own Quest (Fallback)
            // Play *any* valid card into AI.Party. Tie-break: High Rank.
            OpponentAction fallbackAdvance = GetFallbackPlayToParty(opponentPlayer);
            if (fallbackAdvance != null) return fallbackAdvance;

            // No valid moves
            return null;
        }

        private OpponentAction GetBestAttack(Player ai, Player human) {
            // Context: AI Party attacking Human Cards (in AI Defense) logic.
            // Using GameLogic.CanCardAttack.

            int bestRankDiff = int.MaxValue; 
            Card bestAttacker = null;
            Card bestTarget = null;
            Player bestTargetPlayer = null;

            foreach (var attacker in human.defense) {
                if (attacker.IsExhausted) continue;

                // Check against Human Party
                foreach (var target in human.party) {
                     if (CanCardAttack(ai, human, attacker, target)) {
                         // We prefer successful attacks (Rank > Rank) which CanCardAttack checks?
                         // Wait, CanCardAttack returns bool based on Rank > Rank.
                         // So if it returns true, it's a win.
                         
                         int diff = attacker.Rank - target.Rank; // Rough estimate, technically we should use EffectiveRank but simple rank diff is ok heuristic
                         if (diff < bestRankDiff) {
                             bestRankDiff = diff;
                             bestAttacker = attacker;
                             bestTarget = target;
                             bestTargetPlayer = ai;
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

        private OpponentAction GetStrategicDefend(Player ai, Player human) {
            if (human.defense.Count >= MAX_DEFENSE_SIZE) return null;

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
            if (ai.party.Count >= MAX_PARTY_SIZE) return null;

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
        
        private OpponentAction GetFallbackDefend(Player ai, Player human) {
            if (human.defense.Count >= MAX_DEFENSE_SIZE) return null;

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
            if (ai.party.Count >= MAX_PARTY_SIZE) return null;

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
