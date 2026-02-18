//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public enum ABS_RaycastMode
    {
        Camera,
        Cursor
    }

    public class ABS_Raycaster : ABS_BuildingManagerComponentBase
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Enum
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private REST_Raycaster m_Raycaster = null;

        private ABS_RaycastMode m_RaycastMode = ABS_RaycastMode.Camera;
        private Camera m_Camera = null;
        private Transform m_CameraTransform = null;

        private ABS_BuildingElement m_BuildingElement = null;
        private Transform m_HitTransform = null;

#if UNITY_EDITOR
        private uint m_StatisticsRaycastCounter = 0;
#endif
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Initialization
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_Raycaster(ABS_IBuildingManagerInternalInterface p_Manager,
                            ABS_BuildingManagerTracker p_Tracker,
                            Camera p_Camera,
                            ABS_RaycastMode p_RaycastMode)
            : base(p_Manager, p_Tracker)
        {
            m_Raycaster = new REST_Raycaster();
;
            m_RaycastMode = p_RaycastMode;
            m_Camera = p_Camera;
            m_CameraTransform = m_Camera.transform;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Getters
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public Vector3 HitPoint
        {
            get { return m_Raycaster.HitPoint; }
        }

        public RaycastHit Hit
        {
            get { return m_Raycaster.Hit; }
        }

        public Transform HitTransform
        {
            get { return m_HitTransform; }
        }

        public ABS_BuildingElement BuildingElement
        {
            get { return m_BuildingElement; }
        }

        public ABS_RaycastMode RaycastMode
        {
            get { return m_RaycastMode; }
            set { m_RaycastMode = value; }
        }

        public Camera Camera
        {
            get { return m_Camera; }
            set
            {
                m_Camera = value;
                m_CameraTransform = m_Camera.transform;
            }
        }

#if UNITY_EDITOR
        public uint StatisticsRaycastCounter
        {
            get { return m_StatisticsRaycastCounter; }
        }
#endif

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Public Function
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public void PerformRaycast()
        {
#if UNITY_EDITOR
            ++m_StatisticsRaycastCounter;
#endif
            bool raycastResult = false;
            if (m_RaycastMode == ABS_RaycastMode.Camera)
            {
                Vector3 forward = m_Camera.transform.TransformDirection(Vector3.forward) * m_Manager.RaycastDistance;
                Vector3 startingPosition = m_Camera.transform.position;
                startingPosition += forward.normalized * m_Manager.RaycastOffset;

                raycastResult = m_Raycaster.RaycastFromPosition(startingPosition, forward, m_Manager.RaycastDistance, m_Manager.LayerCollection.RaycastHitLayers);
            }
            else
            {
                raycastResult = m_Raycaster.RaycastFromCursor(m_Manager.RaycastDistance, m_Manager.RaycastOffset, m_Manager.LayerCollection.RaycastHitLayers);
            }

            //clear the last hit
            m_HitTransform = null;
            m_BuildingElement = null;

            //Check the new hit
            m_HitTransform = m_Raycaster.Hit.transform;
            if (raycastResult && m_HitTransform != null)
            {
                m_BuildingElement = ABS_BuildingElementLink.FindElement(m_HitTransform);
            }


            m_Tracker.RaycastOnHit(raycastResult, raycastResult ? m_HitTransform.gameObject : null);

        }

        public bool RaycastFromTransform(in Transform p_Transform, in float p_RaycastDistance, in LayerMask p_LayerToHit)
        {
            return m_Raycaster.RaycastFromTransform(p_Transform, p_RaycastDistance, p_LayerToHit);
        }
        public bool RaycastFromTransform(in Vector3 p_Position, in Vector3 p_Forward, in float p_RaycastDistance, in LayerMask p_LayerToHit)
        {
            return m_Raycaster.RaycastFromPosition(p_Position, p_Forward, p_RaycastDistance, p_LayerToHit);
        }

        public bool RaycastFromCursor(in float p_RaycastDistance, in float p_RaycastOffset, in LayerMask p_LayerToHit)
        {
            return m_Raycaster.RaycastFromCursor(p_RaycastDistance, p_RaycastOffset, p_LayerToHit);
        }

        public Vector3 CalcualteRaycastEndPosition(in float p_RaycastDistance, in float p_RaycastOffset)
        {
            return REST_Raycaster.CalcualteRaycastEndPosition(m_CameraTransform, p_RaycastDistance, p_RaycastOffset);
        }

        public Vector3 GetRaycastHitOrEndPosition(in float p_RaycastDistance, in float p_RaycastOffset)
        {
            return m_Raycaster.GetRaycastHitOrEndPosition(m_CameraTransform, p_RaycastDistance, p_RaycastOffset);
        }

#if UNITY_EDITOR
        public void StatisticsReset()
        {
            m_StatisticsRaycastCounter = 0;
        }
#endif

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Static Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public static bool IsUnderground(
            in Vector3 p_TargetPosition, 
            ref Vector3 p_RaycastStartPosition, 
            ref RaycastHit p_Hit, 
            in LayerMask p_GroundLayer)
        {
            Vector3 startingPosition = p_RaycastStartPosition;
            Vector3 direction = p_TargetPosition - startingPosition;
            startingPosition -= direction.normalized * 0.1f;

            Ray ray = new Ray(startingPosition, direction);
            p_RaycastStartPosition = startingPosition;
            return Physics.Raycast(ray, out p_Hit, direction.magnitude, p_GroundLayer);
        }

        public static bool GetGroundPosition(
            in Vector3 p_CheckedPosition,
            out Vector3 p_GroundPosition, 
            in LayerMask p_GroundLayer,
            out RaycastHit p_Hit,
            in float p_Range = float.MaxValue)
        {
            Ray ray = new Ray(p_CheckedPosition, Vector3.down);

            bool result = Physics.Raycast(ray, out p_Hit, p_Range, p_GroundLayer);
            if (result)
            {
                p_GroundPosition = p_Hit.point;
            }
            else
            {
                p_GroundPosition = Vector3.zero;
            }
            return result;
        }
    }
}