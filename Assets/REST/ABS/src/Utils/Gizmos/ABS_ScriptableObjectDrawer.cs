//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public class ABS_ScriptableObjectDrawer : MonoBehaviour
    {
#if UNITY_EDITOR
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private ABS_DrawableScriptableObject m_DrawableObject = null;
        private static ABS_ScriptableObjectDrawer s_Instance = null;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementaion
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private void OnDrawGizmos()
        {
            if (UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() == null)
            {
                s_Instance = null;
                return;
            }
            else if (s_Instance == null || s_Instance != this)
            {
                DestroyImmediate(s_Instance);
                s_Instance = this;
            }

            if (m_DrawableObject != null)
            {
                m_DrawableObject.OnDrawGizmos();
            }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Static Implementaion
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public static void Clear()
        {
            if (s_Instance != null)
            {
                s_Instance.m_DrawableObject = null;
            }
            else
            {
                REST_Logging.Warrning("ABS_ScriptableObjectDrawer", "Null Instance");
            }
        }

        public static Transform Transform 
        {
            get
            {
                if (s_Instance != null)
                {
                    return ABS_ScriptableObjectDrawer.s_Instance.transform;
                }
                else
                {
                    REST_Logging.Warrning("ABS_ScriptableObjectDrawer", "Null Instance");
                    return null;
                }
            }
        }

        public static ABS_ScriptableObjectDrawer Instance
        {
            get
            {
                return s_Instance;
            }
        }

        public static ABS_DrawableScriptableObject DrawableObject
        {
            get
            {
                if (s_Instance != null)
                {
                    return ABS_ScriptableObjectDrawer.s_Instance.m_DrawableObject;
                }
                else
                {
                    REST_Logging.Warrning("ABS_ScriptableObjectDrawer", "Null Instance");
                    return null;
                }
            }
            set
            {
                if (s_Instance != null)
                {
                    ABS_ScriptableObjectDrawer.s_Instance.m_DrawableObject = value;
                }
                else
                {
                    REST_Logging.Warrning("ABS_ScriptableObjectDrawer", "Null Instance");
                }
            }
        }
#endif
    }
}