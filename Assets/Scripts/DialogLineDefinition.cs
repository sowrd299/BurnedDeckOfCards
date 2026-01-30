using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ashworld
{
    public enum DialogZone
    {
        PlayerHand,
        PlayerParty,
        PlayerDefense,
        OpponentHand,
        OpponentParty,
        OpponentDefense
    }

    [Serializable]
    public class DialogCondition
    {
        [SerializeField] private CardDefinitionAsset cardAsset;
        [SerializeField] private List<DialogZone> allowedZones;

        public CardDefinition Card => cardAsset != null ? cardAsset.Definition : null;
        public List<DialogZone> AllowedZones => allowedZones;
    }

    [Serializable]
    public class DialogLineDefinition
    {
        [HideInInspector] public string title;

        [SerializeField] private DialogCondition speaker;
        [SerializeField] private List<DialogCondition> otherConditions;
        [TextArea(3, 10)]
        [SerializeField] private string text;

        public DialogCondition Speaker => speaker;
        public List<DialogCondition> OtherConditions => otherConditions;
        public string Text => text;

        public List<DialogCondition> AllConditions
        {
            get
            {
                var list = new List<DialogCondition> { speaker };
                if (otherConditions != null)
                {
                    list.AddRange(otherConditions);
                }
                return list;
            }
        }

        public void DoOnValidate() {
            title = (Speaker.Card?.CardName ?? "") + " - " + Text;
        }
    }
}
