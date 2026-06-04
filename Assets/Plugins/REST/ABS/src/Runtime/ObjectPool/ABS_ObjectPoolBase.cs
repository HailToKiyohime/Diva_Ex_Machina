//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public abstract class ABS_ObjectPoolBase : MonoBehaviour
    {
        public abstract ABS_BuildingElement Get(ABS_BuildingElement p_Element);
        public abstract void GiveBack(ABS_BuildingElement p_Element);
        public abstract void Release(ABS_BuildingElement p_Element);
    }
}