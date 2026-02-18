//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.Utils
{
    public class REST_Raycaster
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Variables
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private RaycastHit m_Hit;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Getters / Setters
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public RaycastHit Hit
        {
            get { return m_Hit; }
        }

        public Vector3 HitPoint
        {
            get { return m_Hit.point; }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  static Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public static Vector3 CalcualteRaycastEndPosition(in Transform p_Transform, in float p_RaycastDistance, in float p_RaycastOffset)
        {
            return p_Transform.position + p_Transform.forward * (p_RaycastDistance + p_RaycastOffset);
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public bool RaycastFromTransform(in Transform p_Transform, in float p_RaycastDistance, in LayerMask p_LayerToHit)
        {
            return RaycastFromPosition(p_Transform.position, p_Transform.forward, p_RaycastDistance, p_LayerToHit);
        }

        public bool RaycastFromPosition(in Vector3 p_Position, in Vector3 p_Forward, in float p_RaycastDistance, in LayerMask p_LayerToHit)
        {
            return Physics.Raycast(p_Position, p_Forward, out m_Hit, p_RaycastDistance, p_LayerToHit);
        }

        public bool RaycastFromCursor(in float p_RaycastDistance, in float p_RaycastOffset, in LayerMask p_LayerToHit)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            float length = p_RaycastDistance + p_RaycastOffset;
            Vector3 startPoint = ray.origin + ray.direction * p_RaycastOffset;
            return Physics.Raycast(new Ray(startPoint, ray.direction), out m_Hit, p_RaycastDistance, p_LayerToHit);
        }

        public Vector3 GetRaycastHitOrEndPosition(in Transform p_Transform, in float p_RaycastDistance, in float p_RaycastOffset)
        {
            return Hit.transform == null ? CalcualteRaycastEndPosition(p_Transform, p_RaycastDistance, p_RaycastOffset) : HitPoint;
        }
    }
}
