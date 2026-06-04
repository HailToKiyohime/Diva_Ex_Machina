//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEditor;
using UnityEngine;

//  Dependencies: REST
using REST.AdvancedBuildSystem;
using System.Collections.ObjectModel;

//*********************************************************************


namespace REST.AdvancedBuildSystem.Editor
{
    [CustomEditor(typeof(ABS_BuildingElementHighlightCollection))]
    [CanEditMultipleObjects]
    internal class ABS_BuildingElementHighlightCollectionEditor : ABS_EditorBase
    {
        private SerializedProperty m_PendingHighlightMaterialProperty;
        private GUIContent m_PendingHighlightMaterialGUIContent;
        private SerializedProperty m_BlockedHighlightMaterialProperty;
        private GUIContent m_BlockedHighlightMaterialGUIContent;
        private SerializedProperty m_DestroyHighlightMaterialProperty;
        private GUIContent m_DestroyHighlightMaterialGUIContent;
        private SerializedProperty m_PreBuiltHighlightMaterialProperty;
        private GUIContent m_PreBuiltHighlightMaterialGUIContent;
        private SerializedProperty m_CustomHighlightMaterialsProperty;
        private GUIContent m_CustomHighlightMaterialsGUIContent;

        protected override void OnHeaderGUIImpl(out string p_Title)
        {
            p_Title = "BuildingElement HighlightCollection";
        }
        protected override void OnEnableImpl()
        {
            m_PendingHighlightMaterialProperty = serializedObject.FindProperty("m_PendingHighlightMaterial");
            m_PendingHighlightMaterialGUIContent = new GUIContent("Highlight Material : Pending", "The material used when the element in Pending stage");
            m_BlockedHighlightMaterialProperty = serializedObject.FindProperty("m_BlockedHighlightMaterial");
            m_BlockedHighlightMaterialGUIContent = new GUIContent("Highlight Material : Blocked", "The material used when the element in Blocked stage");
            m_DestroyHighlightMaterialProperty = serializedObject.FindProperty("m_DestroyHighlightMaterial");
            m_DestroyHighlightMaterialGUIContent = new GUIContent("Highlight Material : Destroy", "The material used when the element in Destroy stage");
            m_PreBuiltHighlightMaterialProperty = serializedObject.FindProperty("m_PreBuiltHighlightMaterial");
            m_PreBuiltHighlightMaterialGUIContent = new GUIContent("Highlight Material : PreBuilt", "The material used when the element in PreBuilt stage");
            m_CustomHighlightMaterialsProperty = serializedObject.FindProperty("m_CustomHighlightMaterials");
            m_CustomHighlightMaterialsGUIContent = new GUIContent("Highlight Material : Custom", "The material used when the element in Custom stage");
        }

        protected override void OnInspectorGUIImpl()
        {
            EditorGUILayout.LabelField("Materials properties", m_EditorStyleContainer.HeadStyleSection);
            EditorGUILayout.PropertyField(m_PendingHighlightMaterialProperty, m_PendingHighlightMaterialGUIContent);
            EditorGUILayout.PropertyField(m_BlockedHighlightMaterialProperty, m_BlockedHighlightMaterialGUIContent);
            EditorGUILayout.PropertyField(m_DestroyHighlightMaterialProperty, m_DestroyHighlightMaterialGUIContent);
            EditorGUILayout.PropertyField(m_PreBuiltHighlightMaterialProperty, m_PreBuiltHighlightMaterialGUIContent);

            ABS_EditorUtils.Space();
            EditorGUILayout.PropertyField(m_CustomHighlightMaterialsProperty, m_CustomHighlightMaterialsGUIContent);
        }

        public static void DrawDetails (in ABS_BuildingElementHighlightCollection p_HighlightCollection)
        {
            if (p_HighlightCollection == null)
            {
                EditorGUILayout.HelpBox("Missing HighlightCollection!", MessageType.Warning);
                return;
            }

            Material pending = null;
            Material blocked = null;
            Material destroy = null;
            Material prebuilt = null;
            List<Material> custom = null;

            ABS_EditorUtils.IndentIn();
            {
                pending = p_HighlightCollection.PendingHighlightMaterial;
                blocked = p_HighlightCollection.BlockedHighlightMaterial;
                destroy = p_HighlightCollection.DestroyHighlightMaterial;
                prebuilt = p_HighlightCollection.PreBuiltHighlightMaterial;
                custom = p_HighlightCollection.CustomHighlightMaterials;
            }
            ABS_EditorUtils.IndentOut();

            if (pending == null) EditorGUILayout.LabelField("Pending Highlight Material  :  NULL");
            else EditorGUILayout.LabelField("Pending Highlight Material  : " + pending.name);

            if (blocked == null) EditorGUILayout.LabelField("Blocked Highlight Material  :  NULL");
            else EditorGUILayout.LabelField("Blocked Highlight Material  : " + blocked.name);

            if (destroy == null) EditorGUILayout.LabelField("Destroy Highlight Material  :  NULL");
            else EditorGUILayout.LabelField("Destroy Highlight Material  : " + destroy.name);

            if (prebuilt == null) EditorGUILayout.LabelField("PreBuilt Highlight Material  :  NULL");
            else EditorGUILayout.LabelField("PreBuilt Highlight Material  : " + prebuilt.name);

            if (custom == null) EditorGUILayout.LabelField("Custom Highlight Materials  :  NULL");
            else
            {
                ABS_EditorUtils.IndentIn();
                {
                    int i = 0;
                    foreach (Material material in custom)
                    {
                        EditorGUILayout.LabelField($"{++i}. Highlight Material  : {material.name}");
                    }
                }
                ABS_EditorUtils.IndentOut();
            }
        }
    }
}
