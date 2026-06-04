//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.Utils
{
    public class REST_QuaternionHelper : MonoBehaviour
    {
        public static Quaternion ConvertGlobalRotationIntoLocal (in Transform p_Target, Quaternion p_Rotation)
        {
            return Quaternion.Inverse(p_Target.rotation) * p_Rotation;
        }

        public static Vector3 ConvertGlobalRotationIntoLocal(in Transform p_Target, Vector3 p_EulerAngles)
        {
            return (Quaternion.Inverse(p_Target.rotation) * Quaternion.Euler(p_EulerAngles)).eulerAngles;
        }
    }
}
