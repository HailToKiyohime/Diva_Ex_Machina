//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{

    public class ABS_BuildAction : ABS_ActionBase
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private List<ABS_BuildActionElementData> m_Data = null;

        private ABS_BuildingElement m_BuildingElementPrefab = null;

        private ABS_BuildActionBuildingData m_BuildingData = null;

        //The building has been created by this build action
        //The undo of this action will destroy the Building too
        private bool m_NewBuilding = false;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Getter / Setter
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public List<ABS_BuildActionElementData> Data => m_Data;
        public ABS_BuildingElement BuildingElementPrefab => m_BuildingElementPrefab;

        public ABS_BuildActionBuildingData BuildingData => m_BuildingData;
        public bool NewBuilding => m_NewBuilding;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Initialization
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_BuildAction(ABS_BuildingManagerTracker p_Tracker, ABS_BuildingElement p_BuildingElementPrefab, ABS_BuildActionBuildingData p_BuildingData, bool p_NewParentCreated) 
            : base (ABS_ActionTypes.Build, p_Tracker)
        {
            m_BuildingElementPrefab = p_BuildingElementPrefab;
            m_Data = new List<ABS_BuildActionElementData>();
            m_BuildingData = p_BuildingData;
            m_NewBuilding = p_NewParentCreated;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public void AddData (ABS_BuildActionElementData p_NewData)
        {
            m_Data.Insert(0, p_NewData);
        }
    }
}

