using System.Collections.Generic;
using UnityEngine;

namespace Ashworld
{
    [CreateAssetMenu(fileName = "DialogDefinitions", menuName = "Ashworld/Dialog Definitions", order = 2)]
    public class DialogDefinitions : ScriptableObject
    {
        [SerializeField] private List<DialogLineDefinition> lines = new List<DialogLineDefinition>();

        public IReadOnlyList<DialogLineDefinition> Lines => lines;
    }
}
