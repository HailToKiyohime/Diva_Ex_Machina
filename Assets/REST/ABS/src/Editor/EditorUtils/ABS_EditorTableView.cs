//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEditor;
using UnityEngine;

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem.Editor
{
    internal class ABS_EditorTableView 
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        // Proeprties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private const int c_COLUMN_WIDTH = 100;
        private const int c_FIRSTCOLUMN_WIDTH = 130;

        private uint m_Column = 0;
        private uint m_Row = 0;
        private string[] m_HeaderValue;

        private uint m_MinWidth = 0;
        private GUILayoutOption[] m_RowOptions = null;
        private GUILayoutOption[] m_RowOptionsFirstColumn = null;

        private AnimationCurve[] m_Curves = null;

        private bool[] m_IsCurveActive;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Init
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_EditorTableView(uint p_Column, uint p_Row, string[] p_HeaderValue)
        {
            if (p_HeaderValue.Length != p_Column)
            {
                REST_Logging.Error("EditorTableView", "The number of HeaderValues are not equal with the column count!");
            }

            m_Column = p_Column;
            m_Row = p_Row;
            m_HeaderValue = p_HeaderValue;

            m_MinWidth = p_Column * c_COLUMN_WIDTH;

            m_RowOptions = new GUILayoutOption[]
            {
                GUILayout.MinWidth(c_COLUMN_WIDTH),
                GUILayout.ExpandWidth(true),
            };

            m_RowOptionsFirstColumn = new GUILayoutOption[]
            {
                GUILayout.MinWidth(c_FIRSTCOLUMN_WIDTH),
                GUILayout.ExpandWidth(true),
            };

            m_IsCurveActive = new bool[m_Row];
            m_Curves = new AnimationCurve[m_Row];
            for (int i = 0; i < m_Row; ++i)
            {
                m_IsCurveActive[i] = false;
                m_Curves[i] = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));
            }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public void CreateTable(ABS_EditorStyleContainer p_EditorStyleContainer, ABS_Statistics.StatisticsData[] p_Data)
        {
            ABS_EditorUtils.BoxStart(p_EditorStyleContainer.DarkBoxStyle);
            {
                CreateRow(p_EditorStyleContainer, m_HeaderValue, false);

                for (int i = 0; i < p_Data.Length; ++i)
                {
                    bool curve = CreateRow(p_EditorStyleContainer, p_Data[i].GetData(), true);
                    if (curve)
                    {
                        m_IsCurveActive[i] = !m_IsCurveActive[i];
                    }

                    if (m_IsCurveActive[i])
                    {
                        CreateCurve(ref m_Curves[i], p_Data[i].ValueBuffer);
                    }
                }
            }
            ABS_EditorUtils.BoxEnd();
        }

        private bool CreateRow(ABS_EditorStyleContainer p_EditorStyleContainer, in string[] p_Values, bool p_AddButtons)
        {
            bool curve = false;
            EditorGUILayout.BeginHorizontal(GUILayout.MinWidth(m_MinWidth));
            GUILayout.FlexibleSpace();
            {
                if (p_AddButtons)
                {
                    curve = GUILayout.Button(
                        "C",
                        p_EditorStyleContainer.SmallDarkButtonStyle,
                        GUILayout.Width(20)
                    );
                }


                for (int i = 0; i < p_Values.Length; ++i)
                {
                    if (i == 0)
                    {
                        EditorGUILayout.LabelField(p_Values[i], m_RowOptionsFirstColumn);
                    }
                    else
                    {
                        EditorGUILayout.LabelField(p_Values[i], m_RowOptions);
                    }
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            return curve;
        }

        private void CreateCurve(ref AnimationCurve p_Curve, List<float> p_ValueBuffer)
        {
            EditorGUI.BeginDisabledGroup(true);

            float max = float.MinValue;
            foreach (float v in p_ValueBuffer)
            {
                if (v > max)
                {
                    max = v;
                }
            }

            max *= 1.2f;

            p_Curve = EditorGUILayout.CurveField(
                p_Curve,
                Color.green,
                new Rect(0, 0, 20, max),
                GUILayout.Height(50),
                GUILayout.MinWidth(m_MinWidth)
            );

            p_Curve.ClearKeys();

            for (int i = 0; i < p_ValueBuffer.Count; ++i)
            {
                p_Curve.AddKey
                    (i, p_ValueBuffer[i]);
            }


            EditorGUI.EndDisabledGroup();
        }

        public void Reset ()
        {
            foreach (AnimationCurve  c in m_Curves)
            {
                c.ClearKeys();
            }
        }
    }
}