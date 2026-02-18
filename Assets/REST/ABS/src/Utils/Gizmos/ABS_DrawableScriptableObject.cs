//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public interface IDrawer
    {
        public void OnDrawGizmos();
    }

    public class ABS_DrawableScriptableObject : ScriptableObject
    {
#if UNITY_EDITOR
        public delegate void OnDrawGizmosImpl();
        private OnDrawGizmosImpl m_DrawGizmosFunction;

        public void OnDrawGizmos()
        {
            if (m_DrawGizmosFunction != null)
            {
                m_DrawGizmosFunction.Invoke();
            }
        }

        public void AddDrawer (IDrawer p_Drawer)
        {
            m_DrawGizmosFunction += p_Drawer.OnDrawGizmos;
        }
        public void ResetDrawFucntion()
        {
            m_DrawGizmosFunction = null;
        }
#endif
    }
}