//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using REST.Utils;
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public class ABS_PositionSearchResult
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Nested Class
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public enum ResultType
        {
            Unkown,
            Success,
            SuccessBlockNeeded,
            FallbackIsNeeded,
            Failed
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private ResultType m_Result = ResultType.Unkown;
        private bool m_IsFallbackResult = false;

        private Vector3 m_WorldPosition = Vector3.zero;
        private Quaternion m_Rotation = Quaternion.identity;
        private bool m_RotatedByPosition = false;
        private bool m_IsAlignedToGround = false;

        //Snapping details
        private ABS_Building m_TargetBuilding = null;
        private ABS_BuildingElement m_TargetPreBuiltElement = null;
        private ABS_BuildingElement m_TargetOverrideElement = null;
        private bool m_IsPreBuiltSnapping = false;
        private bool m_IsOverrideSnapping = false;

        private ABS_PositionValidationData m_ValidationResult = null;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Getter/Setter
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ResultType Result { get { return m_Result; } set { m_Result = value; } }
        public bool IsFallbackResult { get { return m_IsFallbackResult; } set { m_IsFallbackResult = value; } }


        public Vector3 WorldPosition { get { return m_WorldPosition; } set { m_WorldPosition = value; } }
        public Quaternion Rotation { get { return m_Rotation; } set { m_Rotation = value; } }
        public bool RotatedByPosition { get { return m_RotatedByPosition; } set { m_RotatedByPosition = value; } }
        public bool IsAlignedToGround { get { return m_IsAlignedToGround; } set { m_IsAlignedToGround = value; } }
        public ref bool IsAlignedToGroundRef { get { return ref m_IsAlignedToGround; } }


        public ABS_Building TargetBuilding { get { return m_TargetBuilding; } set { m_TargetBuilding = value; } }
        public ABS_BuildingElement TargetPreBuiltElement { get { return m_TargetPreBuiltElement; } set { m_TargetPreBuiltElement = value; } }
        public ABS_BuildingElement TargetOverrideElement { get { return m_TargetOverrideElement; } set { m_TargetOverrideElement = value; } }
        public bool IsPreBuiltSnapping { get { return m_IsPreBuiltSnapping; } set { m_IsPreBuiltSnapping = value; } }
        public bool IsOverrideSnapping { get { return m_IsOverrideSnapping; } set { m_IsOverrideSnapping = value; } }

        public ABS_PositionValidationData ValidationResult { get { return m_ValidationResult; } set { m_ValidationResult = value; } }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_PositionSearchResult Clone()
        {
            ABS_PositionSearchResult res = new ABS_PositionSearchResult();
            res.m_Result = m_Result;
            res.m_IsFallbackResult = m_IsFallbackResult;
            res.m_WorldPosition = m_WorldPosition;
            res.m_Rotation = m_Rotation;
            res.m_RotatedByPosition = m_RotatedByPosition;
            res.m_IsAlignedToGround = m_IsAlignedToGround;
            res.m_TargetBuilding = m_TargetBuilding;
            res.m_TargetOverrideElement = m_TargetOverrideElement;
            res.m_TargetOverrideElement = m_TargetOverrideElement;
            res.m_IsPreBuiltSnapping = m_IsPreBuiltSnapping;
            res.m_ValidationResult = m_ValidationResult;
            return res;
        }

        public override string ToString()
        {
            string result = REST_Logging.ColorizeString(
                                m_Result.ToString(),
                                m_Result == ResultType.Success
                                    ? REST_Logging.Colors.Green
                                    : REST_Logging.Colors.Red);

            return $"Result : {result}" +
                    $"\nIsFallbackResult : {REST_Logging.ColorizeBlooean(m_IsFallbackResult)}" +
                    $"\nRotation : {m_Rotation} ({REST_Logging.ColorizeString(m_Rotation.eulerAngles.ToString(), REST_Logging.Colors.White)})" +
                    $"\nRotatedByPosition : {REST_Logging.ColorizeBlooean(m_RotatedByPosition)}" +
                    $"\nTargetBuilding : {(m_TargetBuilding == null ? REST_Logging.s_Literal_Null : m_TargetBuilding.name)}" +
                    $"\nIsPreBuiltSnapping : {REST_Logging.ColorizeBlooean(m_IsPreBuiltSnapping)}" +
                    $"  |  TargetPreBuiltElement : {(m_TargetPreBuiltElement == null ? REST_Logging.s_Literal_Null : m_TargetPreBuiltElement.name)}" +
                    $"\nIsOverrideSnapping : {REST_Logging.ColorizeBlooean(m_IsPreBuiltSnapping)}" +
                    $"  | TargetOverrideElement : {(m_TargetOverrideElement == null ? REST_Logging.s_Literal_Null : m_TargetOverrideElement.name)}" +
                    "\n**********************************************************************" + 
                    $"\nValidationResult : {(m_ValidationResult == null ? REST_Logging.s_Literal_Null : "\n\n"+m_ValidationResult)}";
        }
    }
}