//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.Utils
{
    public class REST_QuaternionEqualityComparer : IEqualityComparer<UnityEngine.Quaternion>
    {
        private static readonly float c_Epsilon = 0.0001f;

        public bool Equals(UnityEngine.Quaternion p_Q1, UnityEngine.Quaternion p_Q2)
        {
            return Static_Equals(p_Q1, p_Q2);
        }

        public static bool Static_Equals(UnityEngine.Quaternion p_Q1, UnityEngine.Quaternion p_Q2)
        {
            return Mathf.Abs(p_Q1.x - p_Q2.x) < c_Epsilon &&
                   Mathf.Abs(p_Q1.y - p_Q2.y) < c_Epsilon &&
                   Mathf.Abs(p_Q1.z - p_Q2.z) < c_Epsilon &&
                   Mathf.Abs(p_Q1.w - p_Q2.w) < c_Epsilon;
        }

        public int GetHashCode(UnityEngine.Quaternion p_Q)
        {
            int x = Mathf.RoundToInt(p_Q.x * 1000);
            int y = Mathf.RoundToInt(p_Q.y * 1000);
            int z = Mathf.RoundToInt(p_Q.z * 1000);
            int w = Mathf.RoundToInt(p_Q.w * 1000);

            return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode() ^ w.GetHashCode();
        }
    }
}
