//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEditor;
using UnityEngine;

//  Dependencies: REST
using REST.AdvancedBuildSystem;

//*********************************************************************

namespace REST.AdvancedBuildSystem.Editor
{
    [CustomEditor(typeof(ABS_BasicGridBuilding))]
    [CanEditMultipleObjects]
    internal class ABS_BasicGridBuildingEditor : ABS_BuildingEditor
    {
        protected override void OnEnableImpl()
        {
            base.OnEnableImpl();
        }

        protected override void OnInspectorGUIImpl()
        {
            base.OnInspectorGUIImpl();
        }

        protected override void OnHeaderGUIImpl(out string p_Title)
        {
            p_Title = "Basic Grid Building";
        }

    }
}
