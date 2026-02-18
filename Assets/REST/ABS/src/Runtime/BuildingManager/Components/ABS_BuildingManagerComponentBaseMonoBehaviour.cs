//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity

//  Dependencies: REST

//*********************************************************************

using UnityEngine;

namespace REST.AdvancedBuildSystem
{
    public class ABS_BuildingManagerComponentBaseMonoBehaviour : MonoBehaviour
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        protected ABS_IBuildingManagerInternalInterface m_Manager = null;
        protected ABS_BuildingManagerTracker m_Tracker = null;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Getters / Setters
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_IBuildingManagerInternalInterface Manager
        {
            set { m_Manager = value; }
        }

        public ABS_BuildingManagerTracker Tracker
        {
            set { m_Tracker = value; }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Initialization
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public virtual void Init(ABS_IBuildingManagerInternalInterface p_Manager, ABS_BuildingManagerTracker p_Tracker)
        {
            m_Manager = p_Manager;
            m_Tracker = p_Tracker;
        }
    }
}
