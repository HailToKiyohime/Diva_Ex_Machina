//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public class ABS_DestroyActionElementData : ABS_ActionElementDataBase<ABS_DestroyActionBuildingData>
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        //The destroyed elemnt was connected to these elements
        private List<ABS_ActionElementConnectionData> m_LostConnectionsConnectionTarget = null;

        //This elements was connect to the destroyed element
        private List<ABS_ActionElementConnectionData> m_LostConnectionsConnected = null;
        //This elements was connect to the destroyed element and they were destroyed as well becasue of the connection type
        private Dictionary<ABS_DestroyActionElementData, ABS_BuildingElementConnectionType> m_DestroyedConenctedElementData = null;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Getter / Setter
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++


        public Dictionary<ABS_DestroyActionElementData, ABS_BuildingElementConnectionType> DestroyedConenctedElementData => m_DestroyedConenctedElementData;
        public List<ABS_ActionElementConnectionData> LostConnectionsConnectionTarget => m_LostConnectionsConnectionTarget;
        public List<ABS_ActionElementConnectionData> LostConnectionsConnected => m_LostConnectionsConnected;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public override void AddBuildingElement(ABS_BuildingElement p_BuildingElement)
        {
            base.AddBuildingElement(p_BuildingElement);
        }

        public void AddDestroyedConenctedElementData(Dictionary<ABS_DestroyActionElementData, ABS_BuildingElementConnectionType> p_Data)
        {
            m_DestroyedConenctedElementData = p_Data;
        }

        public void AddConenctionTargetData(List<ABS_ActionElementConnectionData> p_Data)
        {
            m_LostConnectionsConnectionTarget = p_Data;
        }

        public void AddConnectedData(List<ABS_ActionElementConnectionData> p_Data)
        {
            m_LostConnectionsConnected = p_Data;
        }
    }
}