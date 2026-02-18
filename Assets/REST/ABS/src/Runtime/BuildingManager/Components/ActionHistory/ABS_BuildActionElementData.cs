//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public class ABS_BuildActionElementData : ABS_ActionElementDataBase<ABS_BuildActionBuildingData>
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        //In case of prebuilt or override features the build action can destroy other elements
        ABS_DestroyActionElementData m_DestroyedElementData = null;

        //The element was conencted to these elements
        private List<ABS_ActionElementConnectionData> m_ConnectionTargets = new List<ABS_ActionElementConnectionData>();

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Getter / Setter
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_DestroyActionElementData DestroyedElementData { get { return m_DestroyedElementData; } }
        public List<ABS_ActionElementConnectionData> ConnectionTargets { get { return m_ConnectionTargets; } }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public override void AddBuildingElement(ABS_BuildingElement p_BuildingElement)
        {
            base.AddBuildingElement(p_BuildingElement);
        }

        public void AddModifiedElement(ABS_DestroyActionElementData p_DestroyedElementData)
        {
            m_DestroyedElementData = p_DestroyedElementData;
        }

        public void AddConnectionTargetData(ABS_ActionElementConnectionData p_Connection)
        {
            m_ConnectionTargets.Add(p_Connection);
        }
    }
}