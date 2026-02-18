//*********************************************************************
//  Dependencies: System
using System.Reflection;
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;
using UnityEditor;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem.Editor
{
    internal abstract class ABS_EditorBase : UnityEditor.Editor
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        protected delegate void FunctionDelegate();

        protected SerializedObject m_TargetSerializedObject;

        private Texture2D m_Icon;

        protected ABS_EditorStyleContainer m_EditorStyleContainer = null;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Constructor
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Base class util functions
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        protected TargetType GetTargetObject<TargetType> () where TargetType : class 
        {
            return m_TargetSerializedObject.targetObject as TargetType;
        }

        protected bool HasMultipleLayers(SerializedProperty p_LayerMaskProperty)
        {
            int mask = p_LayerMaskProperty.intValue;
            return (mask & (mask - 1)) != 0;
        }

        protected bool InvokeBoolFunction(in string p_FunctionName)
        {
            MethodInfo isRotationSupportedMethod = m_TargetSerializedObject.targetObject.GetType().GetMethod(p_FunctionName, BindingFlags.Instance | BindingFlags.Public);
            return (bool)isRotationSupportedMethod.Invoke(m_TargetSerializedObject.targetObject, null);
        }

        protected List<T> GetSelectedTargetsComponents<T>() where T : MonoBehaviour
        {
            List<T> result = new List<T>();
            foreach (Object obj in Selection.objects)
            {
                GameObject go = obj as GameObject;
                if (go != null)
                {
                    T target = go.GetComponent<T>();
                    if (target != null)
                    {
                        result.Add(target);
                    }
                }
            }
            return result;
        }

        protected List<T> GetSelectedTargetsScriptableObject<T>() where T : ScriptableObject
        {
            List<T> result = new List<T>();
            foreach (ScriptableObject obj in Selection.objects)
            {
                T target = obj as T;
                if (target != null)
                {
                    result.Add(target);
                }
            }
            return result;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  UnityEditor.Editor Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public virtual void OnEnable()
        {
            m_Icon = ABS_EditorStorageManager.GetImage("BuildingManagerIcon");
            m_TargetSerializedObject = new SerializedObject(target);
            OnEnableImpl();
        }

        public override void OnInspectorGUI()
        {
            if (m_EditorStyleContainer == null)
            {
                m_EditorStyleContainer = ScriptableObject.CreateInstance<ABS_EditorStyleContainer>();
                m_EditorStyleContainer.Init();
            }

            serializedObject.Update();
            OnInspectorGUIImpl();
            serializedObject.ApplyModifiedProperties();
        }

        protected override void OnHeaderGUI()
        {
            if (m_EditorStyleContainer == null)
            {
                m_EditorStyleContainer = ScriptableObject.CreateInstance<ABS_EditorStyleContainer>();
                m_EditorStyleContainer.Init();
            }

            string title = string.Empty;
            OnHeaderGUIImpl(out title);
            ABS_EditorUtils.AddHeaderSection(m_EditorStyleContainer.HeaderTitleStyle, title, m_Icon);
        }

        protected bool ShowAffirmation(in string p_Message)
        {
            int result = EditorUtility.DisplayDialogComplex("Affirmation", p_Message, "Yes", "No", "");
            return result == 0;
        }
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Abstract functions
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        protected abstract void OnInspectorGUIImpl();
        protected abstract void OnEnableImpl();
        protected abstract void OnHeaderGUIImpl(out string p_Title);

    }
}