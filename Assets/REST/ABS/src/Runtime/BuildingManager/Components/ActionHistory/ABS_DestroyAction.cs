//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{

    public class ABS_DestroyAction : ABS_ActionBase
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private List<ABS_DestroyActionElementData> m_Data = null;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Initialization
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_DestroyAction(ABS_BuildingManagerTracker p_Tracker) : base (ABS_ActionTypes.Destroy, p_Tracker)
        {
            m_Data = new List<ABS_DestroyActionElementData>();
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        public void AddData (ABS_DestroyActionElementData p_NewData) 
        {
            m_Data.Insert(0, p_NewData);
        }

        public List<ABS_DestroyActionElementData> Data { get { return m_Data; } }
    }
}

