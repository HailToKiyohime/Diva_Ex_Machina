//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{

    [CreateAssetMenu(fileName = "NewLayerCollection", menuName = "AdvancedBuildingSystem/BuildingManager Settings/New Layer Collection")]
    public class ABS_LayerCollection : ScriptableObject
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        [SerializeField] private LayerMask m_LayerOfBuildingElement;
        [SerializeField] private LayerMask m_LayerOfPlayer;
        [SerializeField] private LayerMask m_LayerOfGround;

        [SerializeField] private LayerMask m_BlockingLayers;
        [SerializeField] private LayerMask m_RaycastHitLayers;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Getters / Setters
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public LayerMask LayerOfBuildingElement { get { return m_LayerOfBuildingElement; } }
        public LayerMask LayerOfPlayer { get { return m_LayerOfPlayer; } }
        public LayerMask LayerOfGround { get { return m_LayerOfGround; } }
        public LayerMask BlockingLayers { get { return m_BlockingLayers; } }
        public LayerMask RaycastHitLayers { get { return m_RaycastHitLayers; } }
    }
}