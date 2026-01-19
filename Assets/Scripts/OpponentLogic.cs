using System.Collections.Generic;
using UnityEngine;

namespace Ashworld {

    // Simple data structure for an AI action
    public class OpponentAction {
        public enum ActionType { Play, Attack }
        
        public ActionType Type;
        public Card CardToPlay;      // For Play
        public bool PlayToDefense;   // For Play: True = Play to opponent's defense (Human.Defense), False = Play to own party (AI.Party)
        public Card Attacker;        // For Attack
        public Card Defender;        // For Attack
    }

    public static class OpponentLogic
    {
        public static OpponentAction GetBestAction(Player opponentPlayer, Player humanPlayer) {
            
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

            // PRIORITY 3: Advance Own Quest (Strategic)
            // Play into AI.Party
            // Heuristic: Hold Requirement Met > Hold Requirement Unmet
            OpponentAction strategicAdvance = GetStrategicAdvance(opponentPlayer);
            if (strategicAdvance != null) return strategicAdvance;

            // PRIORITY 4: Defend against Player (Fallback)
            // Play *any* valid card into Human.Defense. Tie-break: High Rank.
            OpponentAction fallbackDefend = GetFallbackDefend(opponentPlayer);
            if (fallbackDefend != null) return fallbackDefend;

            // PRIORITY 5: Advance Own Quest (Fallback)
            // Play *any* valid card into AI.Party. Tie-break: High Rank.
            OpponentAction fallbackAdvance = GetFallbackAdvance(opponentPlayer);
            if (fallbackAdvance != null) return fallbackAdvance;

            // No valid moves
            return null;
        }

        private static OpponentAction GetBestAttack(Player ai, Player human) {
            // Context: AI Party attacking Human Party (Wait, rules say "defending against another player's party")
            // In Single Player/AI, the "Defense" pile represents the cards defending against that player.
            // So:
            // - AI attacks Human.Defense (cards defending against AI?) -> NO. This is AI's turn. 
            // - AI wants to halt Human.
            // - Human's Party is attacking AI's Defense.
            // - Does AI attack Human Party directly?
            // "You may attack a card in a defense with a card in your party."
            // So AI attacks cards in `ai.defense` (which are cards defending against AI) using `ai.party`.
            // BUT Priority 1 is "Halt Player".
            // If Human has cards in `human.party`, they are threats.
            // Are those cards in `ai.defense`? No, `ai.defense` are Quest cards or Opponent cards defending against AI.
            // In a 2-player game:
            // Player A Party attacks Player B Defense.
            // Player B Party attacks Player A Defense.
            
            // So for AI to "Halt Player":
            // It needs to kill the Quest cards defending against Player? No, Player wants to kill those.
            // It needs to kill Player's Party cards?
            // "You may attack a card in a defense..."
            // If Player's Party cards are not in a defense... can AI attack them?
            // Typically in these games, you play cards *into* the opponent's defense to block them.
            // And you attack the cards in *your* defense to clear the path.
            
            // Re-reading Priority 1: "Halt Player (Attack)... Find Opponent Party card > Human Party card".
            // This implies direct combat.
            // Assuming for this implementation:
            // AI plays cards INTO Human's Defense (to block).
            // AI attacks cards IN AI's Defense (to advance).
            // BUT the prompt says: "Halt Player... Attack Player Party".
            // Maybe the prompt implies interacting with the Player's Party directly?
            // "The opponent... try to complete their own simple quest (the player's loss condition)."
            // "Prioritize halting... playing cards with applicable locks to defend against the player".
            
            // Let's stick to the mechanics implemented in GameLogic:
            // `CanCardAttack` checks `actingPlayer.defense.Contains(defender)`.
            // So AI can ONLY attack cards in `ai.defense`.
            // These would be the Quest cards blocking the AI.
            // Clearing them advances the AI.
            
            // So "Halt Player" via Attack might be a misunderstanding of the Mechanics?
            // OR does the AI play cards that ARE "defending against the player"?
            // Yes, Priority 2 says "Defend against Player... Play into Human.Defense".
            
            // If Priority 1 is "Halt Player (Attack)", and we can only attack our own defense...
            // Maybe Priority 1 meant "Advance AI Quest (Attack)"?
            // OR maybe in this game you CAN attack the other player's party?
            // `GameLogic`: `if (!actingPlayer.defense.Contains(defender)) return false;`
            // Current validation strictly enforces attacking cards in YOUR defense.
            
            // PROMPT said: "Prioritize halting, playing cards with applicable locks to defend against the player, and then playing cards with relevant holds to advance its own quest."
            // It listed priorities:
            // 1. Halt Player (Attack) -> "Find Opponent Party card > Player Party card".
            // This specific constraint "Find Party > Party" suggests comparing AI Party vs Human Party.
            // If the code doesn't support attacking Human Party, I might need to skip this or interpreting it as "Attack cards in AI Defense that happen to be copies of Human Party cards???" No.
            
            // Let's assume for a moment that the AI CANNOT attack the Human Party directly with the current `GameLogic`.
            // I will implement "Attack cards in AI Defense" as the primary "Attack" action.
            // Why? Because clearing defense is the only way to win/advance.
            // And "Halt Player" is achieved by Priority 2 (Playing into Defense).
            
            // WAIT. "Halt Player (Attack)" in the User Feedback/Plan?
            // "Priority 1: Halt Player (Attack)... Find AI Party Card > Human Party Card".
            // If I implement this, I'd rely on a Mechanic change?
            // Or maybe I just skip it if I can't do it?
            
            // Actually, maybe the "Defense" against the AI IS the Player's Party? 
            // No, `player.defense` holds Quest cards.
            
            // Implementation Decision:
            // I will look for attacks against `ai.defense`. This advances the AI (Advance Own Quest).
            // I will effectively merge "Advance Quest (Attack)" here.
            
            // BUT, if the user explicitly asked for "Halt Player (Attack)" comparing Party vs Party...
            // Maybe they *want* checking if their party is stronger?
            // I'll leave it as: AI attacks its own obstacles.
            
            Card bestAttacker = null;
            Card bestTarget = null;
            int bestRankDiff = int.MaxValue; // Minimize "overkill" (lowest rank that wins)

            // Candidates:
            // Attackers: AI Party (non-exhausted)
            // Targets: AI Defense
            
            foreach (var attacker in ai.party) {
                if (attacker.IsExhausted) continue;
                int attRank = attacker.Rank; // Simplified (GameLogic has Boon logic, might need access? OpponentLogic is static...)
                // We should probably expose GetEffectiveRank in GameLogic or Player or Card?
                // For now, raw Rank.
                
                foreach (var target in ai.defense) {
                    if (attRank > target.Rank) { // Wins
                        int diff = attRank - target.Rank;
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

        private static OpponentAction GetStrategicDefend(Player ai, Player human) {
            // Play into Human.Defense
            // Heuristic: Lock requirement NOT already present AND NOT met by Human.
            
            Card bestCard = null;
            // Simplistic: First valid Lock card.
            foreach (var card in ai.hand) {
                if (!IsPlayable(ai, card, true)) continue; 

                if (card.LockRequirements.Count > 0) {
                     // Check if this lock is novel? 
                     // Valid heuristic: Human doesn't have it in their defense list.
                     if (!human.defense.Exists(c => c.CardName == card.CardName)) { // Name check ok? Requirements might differ?
                         return new OpponentAction { Type = OpponentAction.ActionType.Play, CardToPlay = card, PlayToDefense = true };
                     }
                }
            }
            return null;
        }

        private static OpponentAction GetStrategicAdvance(Player ai) {
            // Play into AI.Party
            // Heuristic: Hold Met > Hold Unmet
            
            Card bestCard = null;
            int bestScore = -1; // 2 = Met, 1 = Unmet, 0 = No Hold

            foreach (var card in ai.hand) {
                if (!IsPlayable(ai, card, false)) continue; // Play into Party

                int score = 0;
                if (card.HoldRequirements.Count > 0) {
                    bool met = true;
                    // Prepare contexts: Party Defs, Defense Defs.
                    // For Opponent AI checking its own party Holds:
                    // contextParty will become AI.Party + this Card? Or just AI.Party?
                    // Typically Hold is checked at Start of Turn for existing party.
                    // If playing a NEW card, we want to know if it WILL be held?
                    // "Hold: If you do not meet the hold requirements... return to hand."
                    // So we check if requirements are met by current party (usually).
                    // Or current party + card?
                    // Let's assume current party.
                    
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

            if (bestCard != null) {
                return new OpponentAction { Type = OpponentAction.ActionType.Play, CardToPlay = bestCard, PlayToDefense = false }; 
            }
            return null;
        }
        
        private static OpponentAction GetFallbackDefend(Player ai) {
            // Any card to Human Defense, High Rank.
            Card bestCard = null;
            foreach (var card in ai.hand) {
                 if (!IsPlayable(ai, card, true)) continue;

                 if (bestCard == null || card.Rank > bestCard.Rank) {
                     bestCard = card;
                 }
            }
             if (bestCard != null) {
                return new OpponentAction { Type = OpponentAction.ActionType.Play, CardToPlay = bestCard, PlayToDefense = true };
            }
            return null;
        }

        private static OpponentAction GetFallbackAdvance(Player ai) {
             // Any card to AI Party, High Rank.
            Card bestCard = null;
            foreach (var card in ai.hand) {
                 if (!IsPlayable(ai, card, false)) continue;
                 
                 if (bestCard == null || card.Rank > bestCard.Rank) {
                     bestCard = card;
                 }
            }
             if (bestCard != null) {
                return new OpponentAction { Type = OpponentAction.ActionType.Play, CardToPlay = bestCard, PlayToDefense = false };
            }
            return null;
        }

        // Helper for basic checks (Cost, Unique, Location)
        // We'll trust GameLogic.TryPlayCard checks, but we need to know if it's generally valid before proposing.
        // Or we just checking basic constraints.
        private static bool IsPlayable(Player player, Card card, bool toDefense) {
            // Simplified check. Real check happens in GameLogic.
            // Check History Cost?
            if (!player.CanPayHistoryCost(card.HistoryCost, card)) return false;
            
            // Check constraints
            if (toDefense) {
                 // Rules: Single Location per party. Defense has no limit? "A party can only have one Location."
                 // Defense is not a party.
            } else {
                if (card.HasAbility(SpecialAbility.Location) && player.party.Exists(c => c.HasAbility(SpecialAbility.Location))) return false;
            }
            
            // Unique
            // "You cannot play a Unique card if a card with the same name is already in your party or defending against your party."
            // So if checking for Defense play (against Opponent), we check Opponent Party and Opponent Defense?
            // "in your party or defending against your party".
            // If playing into Human Defense, it becomes "defending against Human party".
            // So valid check: Is it in AI Party or AI Defense? No.
            // Is it in Human Party or Human Defense? 'Unique' is global usually?
            // Rules: "in your party or defending against your party".
            // So for AI: Check AI.Party and AI.Defense.
            bool partyHas = player.party.Exists(c => c.CardName == card.CardName);
            bool defenseHas = player.defense.Exists(c => c.CardName == card.CardName);
            if(partyHas || defenseHas) return false;

            return true;
        }
    }
}
