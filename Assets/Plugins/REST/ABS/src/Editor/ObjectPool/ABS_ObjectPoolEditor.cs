//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;
using UnityEditor;

//  Dependencies: REST
using REST.AdvancedBuildSystem;

//*********************************************************************

namespace REST.AdvancedBuildSystem.Editor
{
    [CustomEditor(typeof(ABS_ObjectPool))]
    internal class ABS_ObjectPoolEditor : ABS_EditorBase
    {
        private SerializedProperty m_BuildingElementProperty;
        private GUIContent m_BuildingElementGUIContent;

        protected override void OnEnableImpl()
        {
            m_BuildingElementProperty = serializedObject.FindProperty("m_BuildingElement");
            m_BuildingElementGUIContent = new GUIContent("BuildingElement", "The BuildingElment object of the pool");
        }

        protected override void OnInspectorGUIImpl()
        {
            ABS_BuildingElement element = m_BuildingElementProperty.objectReferenceValue as ABS_BuildingElement;
            if (element != null)
            {
                ABS_EditorUtils.AddBuildingElementDataLine(
                    element.gameObject,
                    element.PrefabGuid,
                    ""
                );

                ABS_EditorUtils.Space();
                EditorGUILayout.LabelField($"Count Inactive {(m_TargetSerializedObject.targetObject as ABS_ObjectPool).InactiveElementCount}");
            }
            else
            {
                ABS_EditorUtils.AddPropertyField(m_BuildingElementProperty, m_BuildingElementGUIContent);
            }
        }

        protected override void OnHeaderGUIImpl(out string p_Title)
        {
            p_Title = "ObjectPool";
        }
    }
}
