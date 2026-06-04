//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.Utils
{
    public class REST_Vector3EqualityComparer : IEqualityComparer<Vector3>
    {
        private static readonly float c_Epsilon = 0.0001f;
        private bool m_CheckX = true;
        private bool m_CheckY = true;
        private bool m_CheckZ = true;

        public REST_Vector3EqualityComparer() { }
        public REST_Vector3EqualityComparer(in bool p_CheckX, in bool p_CheckY, in bool p_CheckZ)
        {
            m_CheckX = p_CheckX;
            m_CheckY = p_CheckY;
            m_CheckZ = p_CheckZ;
        }

        public bool Equals(Vector3 p_V1, Vector3 p_V2)
        {
            return Static_Equals(p_V1, p_V2, m_CheckX, m_CheckY, m_CheckZ);
        }

        public static bool Static_Equals(in Vector3 p_V1, in Vector3 p_V2)
        {
            return Static_Equals(p_V1, p_V2, true, true, true);
        }

        public static bool Static_Equals(in Vector3 p_V1, in Vector3 p_V2, in bool p_CheckX, in bool p_CheckY, in bool p_CheckZ)
        {
            return (!p_CheckX || Mathf.Abs(p_V1.x - p_V2.x) < c_Epsilon) 
                    && (!p_CheckY || Mathf.Abs(p_V1.y - p_V2.y) < c_Epsilon)
                    && (!p_CheckZ || Mathf.Abs(p_V1.z - p_V2.z) < c_Epsilon);
        }

        public int GetHashCode(Vector3 p_V)
        {
            int x = m_CheckX ? Mathf.RoundToInt(p_V.x * 1000) : 1;
            int y = m_CheckY ? Mathf.RoundToInt(p_V.y * 1000) : 1;
            int z = m_CheckZ ? Mathf.RoundToInt(p_V.z * 1000) : 1;

            return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode();
        }
    }
}
