using System.Collections.Generic;
using UnityEngine;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "ExpeditionData", menuName = "VirulentVentures/ExpeditionData", order = 11)]
    public class ExpeditionData : ScriptableObject
    {
        [SerializeField] private List<NodeData> nodeData = new List<NodeData>();
        [SerializeField] private int currentNodeIndex = 0;
        [SerializeField] private PartyData party;

        public List<NodeData> NodeData => nodeData;
        public int CurrentNodeIndex { get => currentNodeIndex; set => currentNodeIndex = value; }
        public PartyData Party => party;

        public bool IsValid()
        {
            return nodeData != null && nodeData.Count > 0 && party != null && party.HeroStats.Count > 0;
        }

        public void SetNodes(List<NodeData> nodes)
        {
            nodeData.Clear();
            if (nodes != null)
            {
                nodeData.AddRange(nodes);
            }
        }

        public void SetParty(PartyData newParty)
        {
            party = newParty;
        }

        public void Reset()
        {
            nodeData.Clear();
            currentNodeIndex = 0;
            party = null;
        }
    }
}