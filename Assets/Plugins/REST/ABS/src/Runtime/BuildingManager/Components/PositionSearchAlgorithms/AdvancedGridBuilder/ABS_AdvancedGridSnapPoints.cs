//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using System;
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public enum ABS_AdvancedGridType : ushort
    {
        Floor = 0,
        Wall = 1,
        EdgeHorizontal = 2,
        EdgeVertical = 3,
        Corner = 4,
        Center = 5
    }

    public enum ABS_AdvancedGridAxisType : ushort
    {
        X = 0,
        Z = 1,
        Both = 2
    }
    public enum ABS_AdvancedGridStabilityConnectionType : ushort
    {
        None = 0,
        Stable = 1,
        Unstable = 2
    }

    [Serializable]
    public class ABS_AdvancedGridSnapPoint
    {
        public Vector3 m_Position;
        public ABS_AdvancedGridStabilityConnectionType m_StabilityConnection;

        public ABS_AdvancedGridSnapPoint(Vector3 p_Position, ABS_AdvancedGridStabilityConnectionType p_Stability)
        {
            m_Position = p_Position;
            m_StabilityConnection = p_Stability;
        }

        public ABS_AdvancedGridSnapPoint(ABS_AdvancedGridSnapPoint p_Copytarget)
        {
            m_Position = p_Copytarget.m_Position;
            m_StabilityConnection = p_Copytarget.m_StabilityConnection;
        }
    }

    [Serializable]
    public class ABS_AdvancedGridSnapPointCollection
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region SnapPoints
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Floor
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_Floor_Floor = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 (  1f, 0,    0), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( -1f, 0,    0), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (   0, 0,   1f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (   0, 0,  -1f), ABS_AdvancedGridStabilityConnectionType.Unstable),
        };
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_Floor_Wall = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f,  0.5f,     0), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f,  0.5f,     0), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (    0,  0.5f,  0.5f), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (    0,  0.5f, -0.5f), ABS_AdvancedGridStabilityConnectionType.Stable),

            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f, -0.5f,     0), ABS_AdvancedGridStabilityConnectionType.None),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f, -0.5f,     0), ABS_AdvancedGridStabilityConnectionType.None),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (    0, -0.5f,  0.5f), ABS_AdvancedGridStabilityConnectionType.None),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (    0, -0.5f, -0.5f), ABS_AdvancedGridStabilityConnectionType.None),
        };
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_Floor_EdgeHorizontal = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f, 0,     0), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f, 0,     0), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (    0, 0,  0.5f), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (    0, 0, -0.5f), ABS_AdvancedGridStabilityConnectionType.Stable),
        };
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_Floor_EdgeVertical = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f,  0.5f,  0.5f), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f,  0.5f, -0.5f), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f,  0.5f,  0.5f), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f,  0.5f, -0.5f), ABS_AdvancedGridStabilityConnectionType.Stable),

            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f, -0.5f,  0.5f), ABS_AdvancedGridStabilityConnectionType.None),
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f, -0.5f, -0.5f), ABS_AdvancedGridStabilityConnectionType.None),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f, -0.5f,  0.5f), ABS_AdvancedGridStabilityConnectionType.None),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f, -0.5f, -0.5f), ABS_AdvancedGridStabilityConnectionType.None),
        };
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_Floor_Corner = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f, 0,  0.5f), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f, 0, -0.5f), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f, 0,  0.5f), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f, 0, -0.5f), ABS_AdvancedGridStabilityConnectionType.Stable),
        };
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_Floor_Center = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 (0,  0.5f , 0), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (0, -0.5f , 0), ABS_AdvancedGridStabilityConnectionType.None),
        };

        private static readonly ABS_AdvancedGridSnapPoint[][] s_SnapPoints_Floor = new ABS_AdvancedGridSnapPoint[][]
        {
            s_SnapPoints_Floor_Floor,
            s_SnapPoints_Floor_Wall,
            s_SnapPoints_Floor_EdgeHorizontal,
            s_SnapPoints_Floor_EdgeVertical,
            s_SnapPoints_Floor_Corner,
            s_SnapPoints_Floor_Center
        };

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion Floor
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Wall
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_Wall_Floor = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 (0,  0.5f,  0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (0,  0.5f, -0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (0, -0.5f,  0.5f), ABS_AdvancedGridStabilityConnectionType.None),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (0, -0.5f, -0.5f), ABS_AdvancedGridStabilityConnectionType.None),
        };
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_Wall_Wall = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 (    0,  1f,     0), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (    0, -1f,     0), ABS_AdvancedGridStabilityConnectionType.None),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (   1f,   0,     0), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (  -1f,   0,     0), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f,   0, -0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f,   0,  0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f,   0, -0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f,   0,  0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
        };
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_Wall_EdgeHorizontal = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 (0,  0.5f, 0), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (0, -0.5f, 0), ABS_AdvancedGridStabilityConnectionType.None),
        };
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_Wall_EdgeVertical = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f,   0, 0), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f,   0, 0), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f,  1f, 0), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f,  1f, 0), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f, -1f, 0), ABS_AdvancedGridStabilityConnectionType.None),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f, -1f, 0), ABS_AdvancedGridStabilityConnectionType.None),
        };
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_Wall_Corner = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f,  0.5f, 0), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f,  0.5f, 0), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f, -0.5f, 0), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f, -0.5f, 0), ABS_AdvancedGridStabilityConnectionType.Stable),
        };
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_Wall_Center = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 (0, 0,  0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (0, 0, -0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
        };

        private static readonly ABS_AdvancedGridSnapPoint[][] s_SnapPoints_Wall = new ABS_AdvancedGridSnapPoint[][]
        {
            s_SnapPoints_Wall_Floor,
            s_SnapPoints_Wall_Wall,
            s_SnapPoints_Wall_EdgeHorizontal,
            s_SnapPoints_Wall_EdgeVertical,
            s_SnapPoints_Wall_Corner,
            s_SnapPoints_Wall_Center
        };

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion Wall
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region EdgeHorizontal
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_EdgeHorizontal_Floor = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 (0, 0,  0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (0, 0, -0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
        };
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_EdgeHorizontal_Wall = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 (0,  0.5f, 0), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (0, -0.5f, 0), ABS_AdvancedGridStabilityConnectionType.None),
        };
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_EdgeHorizontal_EdgeHorizontal = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 (   1f, 0,     0), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (  -1f, 0,     0), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f, 0,  0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f, 0,  0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f, 0, -0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f, 0, -0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
        };
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_EdgeHorizontal_EdgeVertical = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f,  0.5f, 0), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f, -0.5f, 0), ABS_AdvancedGridStabilityConnectionType.None),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f,  0.5f, 0), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f, -0.5f, 0), ABS_AdvancedGridStabilityConnectionType.None),
        };
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_EdgeHorizontal_Corner = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f, 0, 0), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f, 0, 0), ABS_AdvancedGridStabilityConnectionType.Stable),
        };
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_EdgeHorizontal_Center = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 (0,  0.5f,  0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (0,  0.5f, -0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (0, -0.5f,  0.5f), ABS_AdvancedGridStabilityConnectionType.None),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (0, -0.5f, -0.5f), ABS_AdvancedGridStabilityConnectionType.None),
        };

        private static readonly ABS_AdvancedGridSnapPoint[][] s_SnapPoints_EdgeHorizontal = new ABS_AdvancedGridSnapPoint[][]
        {
            s_SnapPoints_EdgeHorizontal_Floor,
            s_SnapPoints_EdgeHorizontal_Wall,
            s_SnapPoints_EdgeHorizontal_EdgeHorizontal,
            s_SnapPoints_EdgeHorizontal_EdgeVertical,
            s_SnapPoints_EdgeHorizontal_Corner,
            s_SnapPoints_EdgeHorizontal_Center
        };

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion EdgeHorizontal
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region EdgeVertical
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_EdgeVertical_Floor = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f,  0.5f,  0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f,  0.5f, -0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f,  0.5f,  0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f,  0.5f, -0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f, -0.5f,  0.5f), ABS_AdvancedGridStabilityConnectionType.None),
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f, -0.5f, -0.5f), ABS_AdvancedGridStabilityConnectionType.None),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f, -0.5f,  0.5f), ABS_AdvancedGridStabilityConnectionType.None),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f, -0.5f, -0.5f), ABS_AdvancedGridStabilityConnectionType.None),
        };
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_EdgeVertical_Wall = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f, 0,     0), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f, 0,     0), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (    0, 0,  0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (    0, 0, -0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
        };
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_EdgeVertical_EdgeHorizontal = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f,  0.5f,     0), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f,  0.5f,     0), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (    0,  0.5f,  0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (    0,  0.5f, -0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f, -0.5f,     0), ABS_AdvancedGridStabilityConnectionType.None),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f, -0.5f,     0), ABS_AdvancedGridStabilityConnectionType.None),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (    0, -0.5f,  0.5f), ABS_AdvancedGridStabilityConnectionType.None),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (    0, -0.5f, -0.5f), ABS_AdvancedGridStabilityConnectionType.None),
        };
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_EdgeVertical_EdgeVertical = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 (0,  1f, 0), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (0, -1f, 0), ABS_AdvancedGridStabilityConnectionType.None),
        };
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_EdgeVertical_Corner = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 (0,  0.5f, 0), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (0, -0.5f, 0), ABS_AdvancedGridStabilityConnectionType.Stable),
        };
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_EdgeVertical_Center = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f, 0,  0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f, 0, -0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f, 0,  0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f, 0, -0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
        };

        private static readonly ABS_AdvancedGridSnapPoint[][] s_SnapPoints_EdgeVertical = new ABS_AdvancedGridSnapPoint[][]
        {
            s_SnapPoints_EdgeVertical_Floor,
            s_SnapPoints_EdgeVertical_Wall,
            s_SnapPoints_EdgeVertical_EdgeHorizontal,
            s_SnapPoints_EdgeVertical_EdgeVertical,
            s_SnapPoints_EdgeVertical_Corner,
            s_SnapPoints_EdgeVertical_Center
        };

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion EdgeVertical
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Corner
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_Corner_Floor = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f, 0,  0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f, 0, -0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f, 0,  0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f, 0, -0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
        };
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_Corner_Wall = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f,  0.5f,     0), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f,  0.5f,     0), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (    0,  0.5f,  0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (    0,  0.5f, -0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f, -0.5f,     0), ABS_AdvancedGridStabilityConnectionType.None),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f, -0.5f,     0), ABS_AdvancedGridStabilityConnectionType.None),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (    0, -0.5f,  0.5f), ABS_AdvancedGridStabilityConnectionType.None),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (    0, -0.5f, -0.5f), ABS_AdvancedGridStabilityConnectionType.None),
        };
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_Corner_EdgeHorizontal = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f, 0,     0), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f, 0,     0), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (    0, 0,  0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (    0, 0, -0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
        };
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_Corner_EdgeVertical = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 (0,  0.5f, 0), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (0, -0.5f, 0), ABS_AdvancedGridStabilityConnectionType.None),
        };
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_Corner_Corner = new ABS_AdvancedGridSnapPoint[]
        {
        };
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_Corner_Center = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f,  0.5f ,  0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f,  0.5f , -0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f,  0.5f ,  0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f,  0.5f , -0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f, -0.5f ,  0.5f), ABS_AdvancedGridStabilityConnectionType.None),
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f, -0.5f , -0.5f), ABS_AdvancedGridStabilityConnectionType.None),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f, -0.5f ,  0.5f), ABS_AdvancedGridStabilityConnectionType.None),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f, -0.5f , -0.5f), ABS_AdvancedGridStabilityConnectionType.None),
        };

        private static readonly ABS_AdvancedGridSnapPoint[][] s_SnapPoints_Corner = new ABS_AdvancedGridSnapPoint[][]
        {
            s_SnapPoints_Corner_Floor,
            s_SnapPoints_Corner_Wall,
            s_SnapPoints_Corner_EdgeHorizontal,
            s_SnapPoints_Corner_EdgeVertical,
            s_SnapPoints_Corner_Corner,
            s_SnapPoints_Corner_Center
        };

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion Corner
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Center
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_Center_Floor = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 (0,  0.5f, 0), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (0, -0.5f, 0), ABS_AdvancedGridStabilityConnectionType.None),
        };
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_Center_Wall = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f, 0,     0), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f, 0,     0), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (    0, 0, -0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (    0, 0,  0.5f), ABS_AdvancedGridStabilityConnectionType.Unstable),
        };
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_Center_EdgeHorizontal = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f,  0.5f,     0), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f,  0.5f,     0), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (    0,  0.5f,  0.5f), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (    0,  0.5f, -0.5f), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f, -0.5f,     0), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f, -0.5f,     0), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (    0, -0.5f,  0.5f), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (    0, -0.5f, -0.5f), ABS_AdvancedGridStabilityConnectionType.Stable),
        };
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_Center_EdgeVertical = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f, 0,  0.5f), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f, 0, -0.5f), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f, 0,  0.5f), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f, 0, -0.5f), ABS_AdvancedGridStabilityConnectionType.Stable),
        };
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_Center_Corner = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f,  0.5f,  0.5f), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f,  0.5f, -0.5f), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f,  0.5f,  0.5f), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f,  0.5f, -0.5f), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f, -0.5f,  0.5f), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 0.5f, -0.5f, -0.5f), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f, -0.5f,  0.5f), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-0.5f, -0.5f, -0.5f), ABS_AdvancedGridStabilityConnectionType.Stable),
        };
        private static readonly ABS_AdvancedGridSnapPoint[] s_SnapPoints_Center_Center = new ABS_AdvancedGridSnapPoint[]
        {
            new ABS_AdvancedGridSnapPoint ( new Vector3 ( 1f,   0,   0), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (-1f,   0,   0), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (  0,   0,  1f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (  0,   0, -1f), ABS_AdvancedGridStabilityConnectionType.Unstable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (  0,  1f,   0), ABS_AdvancedGridStabilityConnectionType.Stable),
            new ABS_AdvancedGridSnapPoint ( new Vector3 (  0, -1f,   0), ABS_AdvancedGridStabilityConnectionType.None),
        };

        private static readonly ABS_AdvancedGridSnapPoint[][] s_SnapPoints_Center = new ABS_AdvancedGridSnapPoint[][]
        {
            s_SnapPoints_Center_Floor,
            s_SnapPoints_Center_Wall,
            s_SnapPoints_Center_EdgeHorizontal,
            s_SnapPoints_Center_EdgeVertical,
            s_SnapPoints_Center_Corner,
            s_SnapPoints_Center_Center
        };

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion Center
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        private static readonly ABS_AdvancedGridSnapPoint[][][] s_SnapPoints = new ABS_AdvancedGridSnapPoint[][][]
        {
            s_SnapPoints_Floor,
            s_SnapPoints_Wall,
            s_SnapPoints_EdgeHorizontal,
            s_SnapPoints_EdgeVertical,
            s_SnapPoints_Corner,
            s_SnapPoints_Center
        };

        public static ABS_AdvancedGridSnapPoint[] GetSnapPointsForElements(in ABS_AdvancedGridType p_ActualBuildingElementType, in ABS_AdvancedGridType p_CheckedType)
        {
            return s_SnapPoints[(ushort)p_ActualBuildingElementType][(ushort)p_CheckedType];
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion SnapPoints
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}