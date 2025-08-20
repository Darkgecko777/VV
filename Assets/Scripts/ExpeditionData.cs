using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "ExpeditionData", menuName = "VirulentVentures/ExpeditionData", order = 11)]
    public class ExpeditionData : ScriptableObject
    {
        [SerializeField] private List<NodeData> nodeData = new List<NodeData>();
        [SerializeField] private int currentNodeIndex = 0;

        public List<NodeData> NodeData => nodeData;
        public int CurrentNodeIndex { get => currentNodeIndex; set => currentNodeIndex = value; }

        public void Reset()
        {
            nodeData.Clear();
            currentNodeIndex = 0;
        }
    }
}