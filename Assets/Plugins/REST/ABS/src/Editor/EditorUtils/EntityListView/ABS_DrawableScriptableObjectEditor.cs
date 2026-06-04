//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEditor.SceneManagement;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem.Editor
{
    internal abstract class ABS_DrawableScriptableObjectEditor<DrawableScriptableObject, DrawableEntity> 
        : ABS_EntityListEditorBase<DrawableScriptableObject, DrawableEntity>, IDrawer
        where DrawableScriptableObject : ABS_DrawableScriptableObject, ABS_IEntityListHolder
        where DrawableEntity : class, ABS_IEntity, new()

    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private DrawableScriptableObject m_DrawableObject = null;

        protected List<ABS_BuildingElement> m_DrawnElements = new List<ABS_BuildingElement>();

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Internal Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_BuildingElement AddDrawnElement(ABS_BuildingElement p_Element, string m_Name = "GhostElement")
        {
            ABS_BuildingElement element = null;
            element = Instantiate(p_Element.gameObject, ABS_ScriptableObjectDrawer.Transform).GetComponent<ABS_BuildingElement>();
            element.name = m_Name;
            m_DrawnElements.Add(element);
            return element;
        }

        public void RemoveDrawnElement(ABS_BuildingElement p_Element)
        {
            m_DrawnElements.Remove(p_Element);
            DestroyImmediate(p_Element.gameObject);
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  UnityEditor.Editor Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public void OnDestroy()
        {
            ABS_ScriptableObjectDrawer.Clear();

            foreach (ABS_BuildingElement element in m_DrawnElements)
            {
                if (element != null)
                {
                    DestroyImmediate(element.gameObject);
                }
            }

            m_DrawnElements.Clear();

            if (m_DrawableObject != null)
            {
                m_DrawableObject.ResetDrawFucntion();
            }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  ABS_EditorBase Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        protected override void OnEnableImpl()
        {
            base.OnEnableImpl();
            m_DrawableObject = GetTargetObject<DrawableScriptableObject>();
            m_DrawableObject.ResetDrawFucntion();
            m_DrawableObject.AddDrawer(this);

            if (PrefabStageUtility.GetCurrentPrefabStage() == null)
            {
                return;
            }

            ABS_ScriptableObjectDrawer.DrawableObject = m_DrawableObject;
        }
        
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  IDrawer Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public void OnDrawGizmos()
        {
            OnDrawGizmosImpl();
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Abstract functions
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        protected abstract void OnDrawGizmosImpl();
    }
}
