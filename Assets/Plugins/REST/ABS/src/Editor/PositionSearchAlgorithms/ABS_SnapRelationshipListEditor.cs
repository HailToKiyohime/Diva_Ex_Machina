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
    [CustomEditor(typeof(ABS_SnapRelationshipList))]
    internal class ABS_SnapRelationshipListEditor : ABS_EditorBase
    {
        private SerializedProperty m_SnapRelationshipsProperty;

        protected override void OnEnableImpl()
        {
            m_SnapRelationshipsProperty = serializedObject.FindProperty("m_SnapRelationships");
        }
        protected override void OnHeaderGUIImpl(out string p_Title)
        {
            p_Title = "ABS_SnapRelationship List";
        }

        protected override void OnInspectorGUIImpl()
        {
            EditorGUILayout.PropertyField(m_SnapRelationshipsProperty, new GUIContent("SnapRelationships"));
        }
    }
}