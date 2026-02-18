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
    [CustomEditor(typeof(ABS_LayerCollection))]
    internal class ABS_LayerCollectionEditor : ABS_EditorBase
    {
        private SerializedProperty m_LayerOfBuildingElementProperty;
        private GUIContent m_LayerOfBuildingElementGUIContent;
        private SerializedProperty m_LayerOfPlayerProperty;
        private GUIContent m_LayerOfPlayerGUIContent;
        private SerializedProperty m_LayerOfGroundProperty;
        private GUIContent m_LayerOfGroundtGUIContent;
        private SerializedProperty m_BlockingLayersProperty;
        private GUIContent m_BlockingLayersGUIContent;
        private SerializedProperty m_RaycastHitLayersProperty;
        private GUIContent m_RaycastHitLayersGUIContent;

        protected override void OnEnableImpl()
        {
            m_LayerOfBuildingElementProperty = serializedObject.FindProperty("m_LayerOfBuildingElement");
            m_LayerOfBuildingElementGUIContent = new GUIContent("Layer Of Building Element", "The layer of the Building Elements. It should be only 1 layer.");

            m_LayerOfPlayerProperty = serializedObject.FindProperty("m_LayerOfPlayer");
            m_LayerOfPlayerGUIContent = new GUIContent("Layer Of Player", 
                "The layer of the PLayer. It should be only 1 layer. " +
                "It is used to block the building if the player is colliding the drag building element.");

            m_LayerOfGroundProperty = serializedObject.FindProperty("m_LayerOfGround");
            m_LayerOfGroundtGUIContent = new GUIContent("Layer Of Ground", "The layer of the Ground");

            m_BlockingLayersProperty = serializedObject.FindProperty("m_BlockingLayers");
            m_BlockingLayersGUIContent = new GUIContent("Blocking Layers", "The blocking layers. It is working similar way as the player's layer.");

            m_RaycastHitLayersProperty = serializedObject.FindProperty("m_RaycastHitLayers");
            m_RaycastHitLayersGUIContent = new GUIContent("Raycast Hit Layers", "Which layers should be hit by the Raycast?");

        }

        protected override void OnHeaderGUIImpl(out string p_Title)
        {
            p_Title = "Layer Collection";
        }

        protected override void OnInspectorGUIImpl()
        {
            ABS_EditorUtils.AddPropertyField(m_LayerOfBuildingElementProperty, m_LayerOfBuildingElementGUIContent);
            if (HasMultipleLayers(m_LayerOfBuildingElementProperty))
            {
                EditorGUILayout.HelpBox("Only one layer can be selected!", MessageType.Error);
            }

            ABS_EditorUtils.AddPropertyField(m_LayerOfPlayerProperty, m_LayerOfPlayerGUIContent);
            if (HasMultipleLayers(m_LayerOfPlayerProperty))
            {
                EditorGUILayout.HelpBox("Only one layer can be selected!", MessageType.Error);
            }

            ABS_EditorUtils.AddPropertyField(m_LayerOfGroundProperty, m_LayerOfGroundtGUIContent);

            ABS_EditorUtils.AddPropertyField(m_BlockingLayersProperty, m_BlockingLayersGUIContent);
            ABS_EditorUtils.AddPropertyField(m_RaycastHitLayersProperty, m_RaycastHitLayersGUIContent);
        }

        public static void DrawLayerCollectionDetails(in ABS_LayerCollection p_LayerCollection)
        {
            ABS_EditorUtils.WriteOutLayerMaskDetails("Layer Of Building Element", p_LayerCollection.LayerOfBuildingElement);
            ABS_EditorUtils.WriteOutLayerMaskDetails("Layer Of Player", p_LayerCollection.LayerOfPlayer);
            ABS_EditorUtils.WriteOutLayerMaskDetails("Layer Of Ground", p_LayerCollection.LayerOfGround);
            ABS_EditorUtils.WriteOutLayerMaskDetails("Blocking Layers", p_LayerCollection.BlockingLayers);
            ABS_EditorUtils.WriteOutLayerMaskDetails("Raycast Hit Layers", p_LayerCollection.RaycastHitLayers);
        }
        
        public static void DrawSettingsDetails(ABS_EditorStyleContainer p_EditorStyleContainer, ref bool p_DetailsSectionVariable, in ABS_LayerCollection p_LayerCollection)
        {
            p_DetailsSectionVariable = EditorGUILayout.BeginFoldoutHeaderGroup(p_DetailsSectionVariable, "Details");
            if (p_DetailsSectionVariable)
            {
                ABS_EditorUtils.BoxStart(p_EditorStyleContainer.DarkBoxStyle);
                {
                    DrawLayerCollectionDetails(p_LayerCollection);
                }
                ABS_EditorUtils.BoxEnd();
                ABS_EditorUtils.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }

}