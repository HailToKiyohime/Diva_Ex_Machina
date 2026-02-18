//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;
using UnityEditor;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem.Editor
{
    public class ABS_EditorUtilsSpecial : UnityEditor.Editor
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Delegates
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public delegate void CustomPropertyButtonAction();

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion // Delegates
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region PropertyField
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static void AddPropertyField_Boolean(
            in SerializedProperty p_BooleanProperty,
            in GUIContent p_BooleanGUIContent,
            float p_LabelWidth)
        {
            ABS_EditorUtils.StartHorizontal();
            {
                EditorGUILayout.LabelField(p_BooleanGUIContent, GUILayout.MinWidth(p_LabelWidth));
                EditorGUILayout.PropertyField(p_BooleanProperty, GUIContent.none, GUILayout.Width(30));
                ABS_EditorUtils.FlexibleSpace();
            }
            ABS_EditorUtils.EndHorizontal();
        }

        public static void AddPropertyField_BooleanWithColor(
            in SerializedProperty p_BooleanProperty,
            in GUIContent p_BooleanGUIContent,
            in SerializedProperty p_ColorProperty,
            float p_LabelWidth)
        {
            ABS_EditorUtils.StartHorizontal();
            {
                EditorGUILayout.LabelField(p_BooleanGUIContent, GUILayout.MinWidth(p_LabelWidth));

                EditorGUILayout.PropertyField(p_BooleanProperty, GUIContent.none, GUILayout.Width(30));

                if (p_BooleanProperty.boolValue)
                    if (p_BooleanProperty.boolValue)

                        if (p_BooleanProperty.boolValue)



                        {
                            EditorGUILayout.PropertyField(p_ColorProperty, GUIContent.none, GUILayout.Width(200));
                }

                ABS_EditorUtils.FlexibleSpace();
            }
            ABS_EditorUtils.EndHorizontal();
        }

        public static void AddPropertyFieldWithCustomButton(ref SerializedProperty p_Property,
                                                             GUIContent p_GUIContent,
                                                             CustomPropertyButtonAction p_Callback,
                                                             GUIStyle p_ButtonStyle,
                                                             string p_ButtonText,
                                                             int p_Width = 120)
        {
            ABS_EditorUtils.StartHorizontal();
            {
                EditorGUILayout.PropertyField(p_Property, p_GUIContent);

                bool buttonResult = GUILayout.Button(p_ButtonText, p_ButtonStyle, GUILayout.Width(p_Width));
                if (buttonResult)
                {
                    p_Callback();
                }
            }
            ABS_EditorUtils.EndHorizontal();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion // PropertyField
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}