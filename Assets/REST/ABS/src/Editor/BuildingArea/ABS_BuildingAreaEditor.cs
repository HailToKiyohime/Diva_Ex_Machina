//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;
using UnityEditor;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem.Editor
{
    [CustomEditor(typeof(ABS_BuildingArea))]
    [CanEditMultipleObjects]
    internal class ABS_BuildingAreaEditor : ABS_EditorBase
    {
        private SerializedProperty m_RulesProperty;
        private GUIContent m_RuleGUIContent;

        private SerializedProperty m_ShapeProperty;
        private GUIContent m_ShapGUIContent;
        private SerializedProperty m_SphereSizeProperty;
        private GUIContent m_SphereSizeGUIContent;
        private SerializedProperty m_BoxSizeProperty;
        private GUIContent m_BoxSizeGUIContent;

        private SerializedProperty m_LayerCollectionProperty;
        private GUIContent m_LayerCollectionGUIContent;

        private bool m_LayerCollectionDetailsSectionVariable = false;

        protected override void OnHeaderGUIImpl(out string p_Title)
        {
            p_Title = "Building Area";
        }

        protected override void OnEnableImpl()
        {
            m_RulesProperty = serializedObject.FindProperty("m_Rules");
            m_RuleGUIContent = new GUIContent("BuildingArea Ruleset", "The Area's rules. (ScriptableObject: BuildingAreaRuleset)");

            m_ShapeProperty = serializedObject.FindProperty("m_Shape");
            m_ShapGUIContent = new GUIContent("Area Shape", "The Spahe of the Area. (Box or Sphere)");
            m_SphereSizeProperty = serializedObject.FindProperty("m_SphereSize");
            m_SphereSizeGUIContent = new GUIContent("Sphere Size", "The Area's size in float");
            m_BoxSizeProperty = serializedObject.FindProperty("m_BoxSize");
            m_BoxSizeGUIContent = new GUIContent("Box Size", "The Area's size in Vector3");

            m_LayerCollectionProperty = serializedObject.FindProperty("m_LayerCollection");
            m_LayerCollectionGUIContent = new GUIContent("Layer Collection", "The collection of the layers used by the Manager");
        }

        protected override void OnInspectorGUIImpl()
        {
            ABS_EditorUtils.AddScriptableObjectPropertyWithCreate<ABS_BuildingAreaRuleset>(
                ref m_RulesProperty,
                m_RuleGUIContent,
                m_EditorStyleContainer.SmallDarkButtonStyle,
                "Missing BuildingArea Ruleset!",
                "Save BuildingArea Ruleset",
                "NewBuildingAreaRuleset");

            ABS_EditorUtils.Space();
            ABS_EditorUtils.AddPropertyField(m_ShapeProperty, m_ShapGUIContent);
            ABS_EditorUtils.IndentIn();
            {
                if (m_ShapeProperty.enumValueIndex == (int)ABS_BuildingArea.AreaShape.Sphere)
                {
                    ABS_EditorUtils.AddPropertyField(m_SphereSizeProperty, m_SphereSizeGUIContent);
                }
                else
                {
                    ABS_EditorUtils.AddPropertyField(m_BoxSizeProperty, m_BoxSizeGUIContent);
                }
            }
            ABS_EditorUtils.IndentOut();


            ABS_EditorUtils.Space();
            ABS_EditorUtils.AddScriptableObjectPropertyWithCreate<ABS_LayerCollection>(
                ref m_LayerCollectionProperty,
                m_LayerCollectionGUIContent,
                m_EditorStyleContainer.SmallDarkButtonStyle,
                "Missing Layer Collection!",
                "Save Layer Collection",
                "NewLayerCollection");

            if (m_LayerCollectionProperty.objectReferenceValue != null)
            {
                ABS_LayerCollectionEditor.DrawSettingsDetails(
                    m_EditorStyleContainer,
                    ref m_LayerCollectionDetailsSectionVariable,
                    m_LayerCollectionProperty.objectReferenceValue as ABS_LayerCollection);
            }
        }
    }
}
