//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.Utils
{
#if UNITY_EDITOR
    public class REST_GizmosUtils
    {
        public static void DrawMesh(Mesh p_Mesh, Vector3 p_Position, Vector3 p_Rotation, UnityEngine.Color p_Color)
        {
            Gizmos.color = p_Color;
            Gizmos.DrawWireMesh(p_Mesh, 0, p_Position, Quaternion.Euler(p_Rotation), Vector3.one);
        }

        public static void DrawWireCube(Vector3 p_Dimension, Vector3 p_Position, Vector3 p_Rotation, UnityEngine.Color p_Color)
        {
            Gizmos.color = p_Color;

            Matrix4x4 oldmatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(
                p_Position,
                Quaternion.Euler(p_Rotation),
                p_Dimension);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            Gizmos.matrix = oldmatrix;
        }

        public static void DrawLine (Vector3 p_Position, Vector3 p_Direction, float p_Length, UnityEngine.Color p_Color)
        {
            Gizmos.color = p_Color;
            Gizmos.DrawLine(p_Position, p_Position + p_Direction * p_Length);
        }

        public static void DrawLine(Vector3 p_From, Vector3 p_To, UnityEngine.Color p_Color)
        {
            Gizmos.color = p_Color;
            Gizmos.DrawLine(p_From, p_To);
        }

        public static void DrawArrow(Vector3 p_Position, Vector3 p_Direction, float p_Length, Quaternion p_Rotation, UnityEngine.Color p_Color)
        {
            Gizmos.color = p_Color;

            Vector3 startPoint = p_Position;
            Vector3 endPoint = startPoint + p_Rotation * p_Direction * p_Length;

            DrawArrow(p_Position, endPoint, p_Direction);
        }

        public static void DrawArrow(Vector3 p_StartPoint, Vector3 p_EndPoint, in UnityEngine.Color p_Color)
        {
            Gizmos.color = p_Color;

            DrawArrow(p_StartPoint, p_EndPoint, Vector3.forward);
        }

        private static void DrawArrow(Vector3 p_StartPoint, Vector3 p_EndPoint, Vector3 p_Direction)
        {
            float arrowHeadLength = Vector3.Distance(p_StartPoint, p_EndPoint) / 3f;
            float arrowHeadAngle = 30f;

            Vector3 arrowHeadDirection = -(p_EndPoint - p_StartPoint);
            Quaternion lookRotation = Quaternion.LookRotation(arrowHeadDirection, Vector3.up);

            Gizmos.DrawLine(p_StartPoint, p_EndPoint);
            Gizmos.DrawLine(p_EndPoint, p_EndPoint + lookRotation * Quaternion.Euler(0, -arrowHeadAngle, 0) * p_Direction * arrowHeadLength);
            Gizmos.DrawLine(p_EndPoint, p_EndPoint + lookRotation * Quaternion.Euler(0, arrowHeadAngle, 0) * p_Direction * arrowHeadLength);
        }


        public static void DrawWireSphere(in Vector3 p_Position, in float p_Radius, in UnityEngine.Color p_Color)
        {
            Gizmos.color = p_Color;
            Gizmos.DrawWireSphere(p_Position, p_Radius);
        }

        public static void DrawSphere(in Vector3 p_Position, in float p_Radius, in UnityEngine.Color p_Color)
        {
            Gizmos.color = p_Color;
            Gizmos.DrawSphere(p_Position, p_Radius);
        }

        public static void DrawText (in Vector3 p_Position, in string p_Msg, in UnityEngine.Color p_Color)
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = p_Color;
            style.fontSize = 16;
            style.alignment = TextAnchor.LowerCenter;
            style.fontStyle = FontStyle.Bold;

            UnityEditor.Handles.Label(p_Position, p_Msg, style);
        }
    }
#endif
}