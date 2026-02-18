//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public class ABS_ProjectSettings : ScriptableObject
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Debug
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        [SerializeField] private bool m_DragBuilding_AdvancedGridValidationProcess = false;
        [SerializeField] private bool m_PositionSearchProcess_Result = false;

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion // Debug
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Gizmos
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //Raycast
        [SerializeField] private bool m_Raycast_Line = true;
        [SerializeField] private UnityEngine.Color m_Raycast_LineColor = UnityEngine.Color.green;
        [SerializeField] private bool m_Raycast_Hitpoint = true;
        [SerializeField] private UnityEngine.Color m_Raycast_HitpointColor = UnityEngine.Color.green;

        //Position Search
        [SerializeField] private bool m_PositionSearch_SearchCollider = true;
        [SerializeField] private UnityEngine.Color m_PositionSearch_SearchColliderColor = UnityEngine.Color.blue;
        [SerializeField] private bool m_PositionSearch_BuildCollider = true;
        [SerializeField] private UnityEngine.Color m_PositionSearch_BuildColliderColor = UnityEngine.Color.green;
        [SerializeField] private bool m_PositionSearch_CheckedBESnapPoints = false;
        [SerializeField] private UnityEngine.Color m_PositionSearch_CheckedBESnapPointsColor = UnityEngine.Color.red;
        [SerializeField][Range(100f, 1000f)] private float m_PositionSearch_SnapPointsArea = 200f;

        //Position Validation
        [SerializeField] private bool m_PositionValidation_AirBuildingMaximumRange = false;
        [SerializeField] private bool m_PositionValidation_BuildableGround = false;

        //Drag Building
        [SerializeField] private bool m_DragBuilding_Index = false;

        //Building Element
        [SerializeField] private bool m_BuildingElement_Stability = false;
        [SerializeField] private bool m_BuildingElement_StabilityWhenSelected = false;

        //BuildingArea
        [SerializeField] private bool m_BuildingArea_AreaCollider = true;
        [SerializeField] private UnityEngine.Color m_BuildingArea_AreaColliderColor = UnityEngine.Color.blue;

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion // Gizmos
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Getters
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public bool DragBuilding_AdvancedGridValidationProcess => m_DragBuilding_AdvancedGridValidationProcess;
        public bool PositionSearchProcess_Result => m_PositionSearchProcess_Result;

        //Raycast
        public bool Raycast_Line => m_Raycast_Line;
        public UnityEngine.Color Raycast_LineColor => m_Raycast_LineColor;
        public bool Raycast_Hitpoint => m_Raycast_Hitpoint;
        public UnityEngine.Color Raycast_HitpointColor => m_Raycast_HitpointColor;

        //Position Search
        public bool PositionSearch_SearchCollider => m_PositionSearch_SearchCollider;
        public UnityEngine.Color PositionSearch_SearchColliderColor => m_PositionSearch_SearchColliderColor;
        public bool PositionSearch_BuildCollider => m_PositionSearch_BuildCollider;
        public UnityEngine.Color PositionSearch_BuildColliderColor => m_PositionSearch_BuildColliderColor;
        public bool PositionSearch_CheckedBESnapPoints => m_PositionSearch_CheckedBESnapPoints;
        public UnityEngine.Color PositionSearch_CheckedBESnapPointsColor => m_PositionSearch_CheckedBESnapPointsColor;
        public float PositionSearch_SnapPointsArea => m_PositionSearch_SnapPointsArea;

        //Position Validation
        public bool PositionValidation_AirBuildingMaximumRange => m_PositionValidation_AirBuildingMaximumRange;
        public bool PositionValidation_BuildableGround => m_PositionValidation_BuildableGround;

        //Drag Building
        public bool DragBuilding_Index => m_DragBuilding_Index;

        //Building Element
        public bool BuildingElement_Stability => m_BuildingElement_Stability;
        public bool BuildingElement_StabilityWhenSelected => m_BuildingElement_StabilityWhenSelected;

        //BuildingArea
        public bool BuildingArea_AreaCollider => m_BuildingArea_AreaCollider;
        public UnityEngine.Color BuildingArea_AreaColliderColor => m_BuildingArea_AreaColliderColor;


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion // Getters
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}