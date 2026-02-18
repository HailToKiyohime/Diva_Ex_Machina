//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public class ABS_BuildingElementLink : MonoBehaviour
    {
        public ABS_BuildingElement m_Element;

        public static ABS_BuildingElement FindElement (in Transform p_Transform)
        {
            ABS_BuildingElement element = p_Transform.GetComponent<ABS_BuildingElement>();

            if (element == null)
            {
                ABS_BuildingElementLink link = p_Transform.GetComponent<ABS_BuildingElementLink>();
                if (link != null)
                {
                    element = link.m_Element;
                }
            }

            return element;
        }
    }
}