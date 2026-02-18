//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem.Editor
{
    [CustomEditor(typeof(ABS_FreeBuilderSettings))]
    internal class ABS_FreeBuilderSettingsEditor : BuilderBaseSettingsEditor
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private SerializedProperty m_EnableAttachementConnectionProperty;
        private GUIContent m_EnableAttachementConnectionGUIContent;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation of BuilderBaseSettingsEditor
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public override void OnEnable()
        {
            base.OnEnable();

            m_EnableAttachementConnectionProperty = serializedObject.FindProperty("m_EnableAttachementConnection");
            m_EnableAttachementConnectionGUIContent = new GUIContent("Enable Attachement Connection",
                "Allow for the FreeBuilding algorithm to create Attachment Connections between the elements");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            base.OnInspectorGUI();
            serializedObject.ApplyModifiedProperties();
        }

        protected override void OnHeaderGUIImpl(out string p_Title)
        {
            p_Title = "Free Building Settings";
        }

        public static new void DrawSettingsDetails(ABS_EditorStyleContainer p_EditorStyleContainer, in ABS_BuilderBaseSettings p_Settings)
        {
            //No property
        }

        protected override void AddSpecialProperties()
        {
            ABS_EditorUtils.AddSeparatorLine();
            EditorGUILayout.LabelField("Attachment Connection", m_EditorStyleContainer.HeadStyleSection);
            ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
            {
                EditorGUILayout.PropertyField(m_EnableAttachementConnectionProperty, m_EnableAttachementConnectionGUIContent);
                if (m_EnableAttachementConnectionProperty.boolValue == true)
                {
                    if (GetTargetObject<ABS_FreeBuilderSettings>().BuildOnTopOfElement == false)
                    {
                        EditorGUILayout.HelpBox("For the Attachment Connection the BuildOnTopOfElement validation must be enabled", MessageType.Error);
                    }
                    else if (GetTargetObject<ABS_FreeBuilderSettings>().BuildOnTopOfElementResultHandling ==
                        ABS_m_BuildOnTopOfElementResultHandling.Block)
                    {
                        EditorGUILayout.HelpBox("For the Attachment Connection the result handling of the BuildOnTopOfElement validation must be allow or needed", MessageType.Error);
                    }
                }
            }
            ABS_EditorUtils.BoxEnd();
        }
    }
}
