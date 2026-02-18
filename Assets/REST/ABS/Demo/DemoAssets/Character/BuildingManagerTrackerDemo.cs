//*********************************************************************
//  Dependencies: System
using System;

//  Dependencies: Unity
using UnityEngine;
using UnityEngine.UI;

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem.Demo
{
    public class BuildingManagerTrackerDemo : ABS_BuildingManagerTracker
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Nested Classes
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        [Serializable]
        public class BuildingElementCountMapper
        {
            public ABS_BuildingElement m_BuildingElement;
            public uint m_Count;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Variables
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private bool m_DragBuildingIsOngoing = false;
        private uint m_CurrentDragBECount = 0;
        [SerializeField] private Text m_CrosshairText = null;
        [SerializeField] private Color m_CrosshairCountColor = new Color(120, 0, 220);
        [SerializeField] private Color m_CrosshairErrorMessageColor = new Color(255, 0, 0);


        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  ABS_IBuildingManagerExternalInterface Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private void Awake()
        {
            m_CrosshairText.text = string.Empty;
            m_CrosshairText.color = m_CrosshairErrorMessageColor;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  ABS_BuildingManagerTracker implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        //-----------------------------------------------------------------------------------------------------------------------------------------------
        //Drag ABS_Building Process
        public override void DragBuildingStarted()
        {
            m_CrosshairText.text = "x1";
            m_CrosshairText.color = m_CrosshairCountColor;
            m_CurrentDragBECount = 1;
            m_DragBuildingIsOngoing = true;
        }
        public override void DragBuildingStoped()
        {
            if (m_DragBuildingIsOngoing)
            {
                if (m_CrosshairText != null)
                {
                    m_CrosshairText.text = string.Empty;
                }
                m_CurrentDragBECount = 0;
            }
            m_DragBuildingIsOngoing = false;
        }  
        
        public override void CurrentValidBuildingElements(in uint p_Count)
        {
            if (m_DragBuildingIsOngoing)
            {
                m_CurrentDragBECount = p_Count;
                m_CrosshairText.text = "x" + m_CurrentDragBECount.ToString();
                m_CrosshairText.color = m_CrosshairCountColor;
            }
        }

        public override void SimpleBuildResult(ABS_SimpleBuildingProcessErrorCode p_ErrorCode)
        {
            if (!m_DragBuildingIsOngoing)
            {
                m_CrosshairText.text = p_ErrorCode.ToString();
            }
        }
    }
}