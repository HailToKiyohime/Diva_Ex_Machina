//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.UIElements;

//  Dependencies: REST

//*********************************************************************

namespace REST.Utils
{
    public class REST_CollisionChecker : MonoBehaviour
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public static Collider[] OverlapBox(in Vector3 p_Position, in Quaternion p_Rotation, in LayerMask p_Layer, in Vector3 p_Size)
        {
            return Physics.OverlapBox(p_Position, p_Size, p_Rotation, p_Layer);
        }

        public static Collider[] OverlapSphere(in Vector3 p_Position, in float p_Radius, in LayerMask p_Layer)
        {
            return Physics.OverlapSphere(p_Position, p_Radius, p_Layer);
        }
    }
}
