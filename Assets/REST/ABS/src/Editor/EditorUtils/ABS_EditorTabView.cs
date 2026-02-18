//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************


namespace REST.AdvancedBuildSystem.Editor
{
    internal class ABS_EditorTabView
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Delegate
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public delegate void ShowViewCallback();

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private List<(string, ShowViewCallback)> m_ViewCallbacks = null;
        private int m_HeaderRowCount = 3;

        private static int m_CurrentView = 0;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Init
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_EditorTabView (int p_HeaderRowCount)
        {
            m_ViewCallbacks = new List<(string, ShowViewCallback)>();
            m_HeaderRowCount = p_HeaderRowCount;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public void AddCallback(in string p_HeaderName, ShowViewCallback p_Callback)
        {
            m_ViewCallbacks.Add((p_HeaderName, p_Callback));
        }

        public void Show (ABS_EditorStyleContainer p_StyleContainer)
        {
            if (m_CurrentView >= m_ViewCallbacks.Count)
            {
                m_CurrentView = 0;
            }

            for (int i = 0; i < m_ViewCallbacks.Count; ++i)
            {
                if (i == 0)
                {
                    ABS_EditorUtils.StartHorizontal();
                }
                else if (i % m_HeaderRowCount == 0)
                {
                    ABS_EditorUtils.EndHorizontal();
                    ABS_EditorUtils.StartHorizontal();
                }

                bool buttonResult = GUILayout.Button(m_ViewCallbacks[i].Item1, 
                    (i == m_CurrentView ? p_StyleContainer.SmallGreenButtonStyle : p_StyleContainer.SmallDarkButtonStyle));
                if (buttonResult)
                {
                    m_CurrentView = i;
                }
            }
            ABS_EditorUtils.EndHorizontal();

            ABS_EditorUtils.Space();

            ABS_EditorUtils.BoxStart(p_StyleContainer.DarkBoxStyle);
            {
                m_ViewCallbacks[m_CurrentView].Item2();
            }
            ABS_EditorUtils.BoxEnd();
        }

        public int GetCurrentViewIdx()
        {
            return m_CurrentView;
        }
    }
}
