//*********************************************************************
//  Dependencies: System
using System;
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;
using UnityEditor;

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem.Editor
{
    internal class ABS_EditorUtils : UnityEditor.Editor
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Static Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private static readonly int s_IconMaxSize = 150;

        public delegate void SetGuid<T>(string p_Guid, T p_Target) where T : MonoBehaviour;

        public delegate void DrawDetailsCallback(UnityEngine.Object p_DrawTarget);

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Static Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public static void Space(float p_Space = 6f)
        {
            EditorGUILayout.Space(p_Space);
        }

        public static void HorizontalSpace(float p_Space = 5)
        {
            GUILayout.Space(p_Space);
        }

        public static void BoxStart(GUIStyle p_BoxStyle)
        {
            StartHorizontal(p_BoxStyle);
            GUILayout.Space(10);
            EditorGUILayout.BeginVertical();
        }

        public static void BoxEnd()
        {
            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
            EndHorizontal();
        }

        public static void Dirty(UnityEngine.Object p_Object)
        {
            EditorUtility.SetDirty(p_Object);
        }

        public static void Dirty(UnityEngine.Object[] p_Object)
        {
            foreach (UnityEngine.Object obj in p_Object)
            {
                Dirty(obj);
            }
        }

        public static void StartHorizontal(GUIStyle p_Style = null)
        {
            if (p_Style == null)
            {
                EditorGUILayout.BeginHorizontal();
            }
            else
            {
                EditorGUILayout.BeginHorizontal(p_Style);
            }
        }

        public static void FlexibleSpace()
        {
            GUILayout.FlexibleSpace();
        }

        public static void EndHorizontal()
        {
            EditorGUILayout.EndHorizontal();
        }

        public static void StartDisable(in bool p_Disable)
        {
            EditorGUI.BeginDisabledGroup(p_Disable);
        }

        public static void EndDisable()
        {
            EditorGUI.EndDisabledGroup();
        }

        public static void StartDisableDuringGame()
        {
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
        }

        public static void EndDisableDuringGame()
        {
            EditorGUI.EndDisabledGroup();
        }

        public static void StartEnableDuringGame()
        { 
            EditorGUI.BeginDisabledGroup(!EditorApplication.isPlaying);
        }

        public static void EndEnableDuringGame()
        {
            EditorGUI.EndDisabledGroup();
        }

        public static void AddSeparatorLine()
        {
            EditorGUILayout.LabelField(string.Empty, GUI.skin.horizontalSlider);
        }

        public static void IndentIn()
        {
            EditorGUI.indentLevel++;
        }

        public static void IndentOut()
        {
            EditorGUI.indentLevel--;
        }

        public static Vector2 StartScrollView(in Vector2 p_ScrollPos)
        {
            return EditorGUILayout.BeginScrollView(p_ScrollPos, GUILayout.ExpandHeight(true));
        }

        public static void EndScrollView()
        {
            EditorGUILayout.EndScrollView();
        }

        public static void HelpBox(in MessageType p_MessageType, in string p_Message)
        {
            EditorGUILayout.HelpBox(p_Message, p_MessageType);
        }

        public static void AddBuildingElementDataLine (GameObject p_GameObject, string p_PrefabGuid, string p_Prefix)
        {
            ABS_EditorUtils.StartHorizontal();
            {
                if (!string.IsNullOrEmpty(p_Prefix))
                {
                    EditorGUILayout.LabelField(p_Prefix, GUILayout.Height(50), GUILayout.Width(70));
                }

                ABS_EditorUtils.AddPreViewImage(p_GameObject, 50, 50);
                EditorGUILayout.LabelField($" {p_GameObject.name} | Prefab Guid: {p_PrefabGuid}", GUILayout.Height(50));
            }
            ABS_EditorUtils.EndHorizontal();
        }

        public static void AddBuildingElementDataLine(GameObject p_GameObject, string p_Prefix)
        {
            ABS_EditorUtils.StartHorizontal();
            {
                if (!string.IsNullOrEmpty(p_Prefix))
                {
                    EditorGUILayout.LabelField(p_Prefix, GUILayout.Height(50), GUILayout.Width(70));
                }
                ABS_EditorUtils.AddPreViewImage(p_GameObject, 50, 50);
                EditorGUILayout.LabelField($" {p_GameObject.name}", GUILayout.Height(50));
            }
            ABS_EditorUtils.EndHorizontal();
        }

        public static void AddPreViewImage (GameObject p_Object, int p_Width, int p_Height)
        {
            Texture2D previewImage = ABS_EditorStorageManager.GetPreviewImage(p_Object);
            if (previewImage != null)
            {
                GUIStyle labelStyle = new GUIStyle();
                labelStyle.normal.background = previewImage;
                labelStyle.fixedWidth = p_Width;
                labelStyle.fixedHeight = p_Height;

                EditorGUILayout.LabelField("", labelStyle, GUILayout.Width(p_Width), GUILayout.Height(p_Height));
            }
        }

        public static void AddObjectLinkLabel (UnityEngine.Object p_Target, float p_Width)
        {
            GUIStyle clickableLabelStyle = new GUIStyle(GUI.skin.label)
            {
                padding = new RectOffset(30, 0, 0, 0),
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Overflow,
                wordWrap = true,
                richText = true,
            };

            string linkText = $"<b><color=#3678F2>{p_Target.name}</color></b>";
            GUIContent content = new GUIContent(linkText);
            Rect labelRect = GUILayoutUtility.GetRect(
                content,
                clickableLabelStyle,
                GUILayout.MinWidth(20),
                GUILayout.MaxWidth(p_Width),
                GUILayout.ExpandWidth(true),
                GUILayout.MinHeight(20)
            );
            if (Event.current.type == EventType.MouseDown && labelRect.Contains(Event.current.mousePosition))
            {
                Selection.activeObject = p_Target;
                Event.current.Use();
            }
            GUI.Label(labelRect, linkText, clickableLabelStyle);
        }

        public static void PrintColorLabel(in string p_Message, in UnityEngine.Color p_Color)
        {
            int red = Mathf.RoundToInt(p_Color.r * 255);
            int green = Mathf.RoundToInt(p_Color.g * 255);
            int blue = Mathf.RoundToInt(p_Color.b * 255);
            int alpha = Mathf.RoundToInt(p_Color.a * 255);

            GUIStyle coloredTextStyle = new GUIStyle(GUI.skin.label);
            coloredTextStyle.richText = true;
            
            UnityEngine.Texture2D squareTexture = new UnityEngine.Texture2D(1, 1);

            var pixels = squareTexture.GetPixels();
            pixels[0] = p_Color;

            squareTexture.SetPixels(pixels);
            squareTexture.Apply();

            GUIStyle squareStyle = new GUIStyle(GUI.skin.label);
            squareStyle.normal.background = squareTexture;

            GUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField(
                    $"{p_Message} <color=#DC143C>R: {red}</color>," +
                    $" <color=green>G: {green}</color>," +
                    $" <color=#4169e1>B: {blue}</color>," +
                    $" <color=wite>A: {alpha}</color>", 
                    coloredTextStyle);
                EditorGUILayout.LabelField("", squareStyle, GUILayout.Width(100));
            }
            GUILayout.EndHorizontal();
        }

        public static void WriteOutLayerMaskDetails(in string p_Message, in LayerMask p_Layers)
        {
            EditorGUILayout.LabelField(p_Message + "  :");
            for (int i = 0; i < 32; i++)
            {
                int layer = 1 << i;
                if ((p_Layers & layer) != 0)
                {
                    EditorGUILayout.LabelField("        *  " + LayerMask.LayerToName(i));
                }
            }
        }

        public static void AddHeaderSection(GUIStyle p_HeaderTitleStyle, in string p_Title, in Texture2D p_Icon)
        {
            GUILayout.BeginHorizontal("In BigTitle");
            {
                var iconSize = Mathf.Min(EditorGUIUtility.currentViewWidth / 3f - 20f, s_IconMaxSize);
                if (p_Icon != null)
                {
                    GUILayout.Label(p_Icon, GUILayout.Width(iconSize), GUILayout.Height(iconSize));
                }

                ABS_EditorUtils.HorizontalSpace(10);
                
                GUILayout.Label(p_Title, p_HeaderTitleStyle, GUILayout.Height(iconSize));
            }
            GUILayout.EndHorizontal();
        }

        public static TYPE LayoutEnumPopup<TYPE>(string p_Message, TYPE p_DefaultValue) where TYPE : System.Enum
        {
            System.Enum tmpValue = p_DefaultValue;
            tmpValue = EditorGUILayout.EnumPopup(p_Message, tmpValue);
            TYPE[] values = (TYPE[])Enum.GetValues(typeof(TYPE));
            foreach (TYPE type in values)
            {
                if (tmpValue.CompareTo(type) == 0)
                {
                    return type;
                }
            }
            return values[0];
        }

        public static T AddObjectField<T>(string p_Message, T p_Object, bool p_AllowSceneObject) where T : UnityEngine.Object
        {
            return EditorGUILayout.ObjectField(p_Message, p_Object, typeof(T), p_AllowSceneObject) as T;
        }
         
        public static T AddObjectField<T>(GUIContent p_GuiContent, T p_Object, bool p_AllowSceneObjec) where T : UnityEngine.Object
        {
            return EditorGUILayout.ObjectField(p_GuiContent, p_Object, typeof(T), p_AllowSceneObjec) as T;
        }

        public static bool AddBooleanField(string p_Message, bool p_DefaultValue)
        {
            return EditorGUILayout.Toggle(p_Message, p_DefaultValue);
        }

        public static int AddIntegerField(GUIContent p_GuiContent, int p_DefaultValue)
        {
            return EditorGUILayout.IntField(p_GuiContent, p_DefaultValue);
        }

        public static float AddFloatField(GUIContent p_GuiContent, float p_DefaultValue)
        {
            return EditorGUILayout.FloatField(p_GuiContent, p_DefaultValue);
        }

        public static Vector2 AddVector2Field(GUIContent p_GuiContent, Vector2 p_DefaultValue)
        {
            return EditorGUILayout.Vector2Field(p_GuiContent, p_DefaultValue);
        }

        public static Vector3 AddVector3Field(GUIContent p_GuiContent, Vector3 p_DefaultValue)
        {
            return EditorGUILayout.Vector3Field(p_GuiContent, p_DefaultValue);
        }

        public static EnumType AddEnumPopup<EnumType>(GUIContent p_GuiContent, EnumType p_EnumValue)
            where EnumType : System.Enum
        {
            return (EnumType)EditorGUILayout.EnumPopup(p_GuiContent, p_EnumValue);
        }

        public static void AddPropertyField(SerializedProperty p_Property, in GUIContent p_GUIContent)
        {
            EditorGUILayout.PropertyField(p_Property, p_GUIContent);
        }

        public static void AddObjectPropertyField(SerializedProperty p_Property, in GUIContent p_GUIContent, in string p_MissingText)
        {
            EditorGUILayout.PropertyField(p_Property, p_GUIContent);
            if (p_Property.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(p_MissingText, MessageType.Error);
            }
        }

        public static void AddScriptableObjectPropertyWithCreate<T> (ref SerializedProperty p_Property,
                                                                    GUIContent p_GUIContent,
                                                                    GUIStyle p_ButtonStyle, 
                                                                    in string p_MissingText, 
                                                                    in string p_SaveTitle, 
                                                                    in string p_NewFileName) 
                                                                    where T : UnityEngine.ScriptableObject
        {
            ABS_EditorUtils.StartHorizontal();
            {
                EditorGUILayout.PropertyField(p_Property, p_GUIContent);

                ABS_EditorUtils.HorizontalSpace(10);
                bool buttonResult2 = GUILayout.Button("Create", p_ButtonStyle, GUILayout.Width(60));
                T result = null;
                if (buttonResult2)
                {
                    result = ABS_EditorStorageManager.SaveScriptableObject<T>(p_SaveTitle, p_NewFileName);
                    p_Property.objectReferenceValue = result;
                }
            }
            ABS_EditorUtils.EndHorizontal();

            if (p_Property.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(p_MissingText, MessageType.Error);
            }
        }

        public static void AddGuidFieldWithCreateButton<T> (GUIStyle p_ButtonStyle, 
                                                            string p_ButtonText, 
                                                            SerializedProperty p_GuidProperty, 
                                                            GUIContent m_GuidGUIContent, 
                                                            List<T> p_Targets, 
                                                            SetGuid<T> p_Setter)
                                                            where T : ABS_SaveableMonobehaviour
        {
            GUILayout.BeginHorizontal();
            {
                if (p_Targets.Count == 1)
                {
                    ABS_EditorUtils.AddPropertyField(p_GuidProperty, m_GuidGUIContent);
                }
                else
                {
                    EditorGUILayout.LabelField($"{m_GuidGUIContent}  :  Multiply target, can't show the Guid!");
                }

                bool buttonResult = GUILayout.Button(p_ButtonText, p_ButtonStyle, GUILayout.Width(120));
                if (buttonResult)
                {
                    foreach (T target in p_Targets)
                    {
                        p_Setter(REST_IDManager.CreateGuid(), target);
                        Dirty(target);
                    }
                }
            }

            GUILayout.EndHorizontal();
            if (string.IsNullOrEmpty(p_GuidProperty.stringValue))
            {
                EditorGUILayout.HelpBox("Empty Guid!", MessageType.Error);
            }
        }
    }
}