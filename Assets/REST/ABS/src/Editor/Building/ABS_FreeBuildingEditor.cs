//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEditor;

//  Dependencies: REST
using REST.AdvancedBuildSystem;

//*********************************************************************

namespace REST.AdvancedBuildSystem.Editor
{
    [CustomEditor(typeof(ABS_FreeBuilding))]
    [CanEditMultipleObjects]
    internal class ABS_FreeBuildingEditor : ABS_BuildingEditor
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
            p_Title = "Free Building";
        }
    }
}
