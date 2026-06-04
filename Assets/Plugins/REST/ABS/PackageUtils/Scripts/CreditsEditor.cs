//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;
using UnityEditor;

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem.PackageUtils
{
#if UNITY_EDITOR
    [CustomEditor(typeof(Credits))]
	public class CreditsEditor : Editor
    {
        [MenuItem("Tools/Advanced Building System/Credits", priority = 1000000)]
        static Credits SelectReadme()
        {
            var ids = AssetDatabase.FindAssets("Credits t:Credits");
            if (ids.Length == 1)
            {
                var creditsObject = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(ids[0]));

                Selection.objects = new UnityEngine.Object[] { creditsObject };

                return (Credits)creditsObject;
            }
            else
            {
                REST_Logging.Warrning("CreditsEditor", "Couldn't find a credits");
                return null;
            }
        }

        public override void OnInspectorGUI()
		{
			var credits = (Credits)target;
			Init();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(credits.title, TitleStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(16f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("In this Credits window every third party asset or resource has been mentioned what had been used for the Advanced Building System.", BodyStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(32f);

            var iconWidth = Mathf.Min((EditorGUIUtility.currentViewWidth / 3f - 20f) / 3, 128f);

			foreach (var section in credits.sections)
			{
				if (!string.IsNullOrEmpty(section.heading))
				{
					GUILayout.Label(section.heading, HeadingStyle);
                }

                EditorGUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Space(10);
                EditorGUILayout.BeginVertical();

                if (section.icon.Length > 0)
				{
					EditorGUILayout.BeginHorizontal();
					{
						foreach (var icon in section.icon)
						{
							GUILayout.Label(icon, GUILayout.Width(iconWidth), GUILayout.Height(iconWidth));
						}
					}
					GUILayout.EndHorizontal();
				}
				if (!string.IsNullOrEmpty(section.linkText))
				{
					if (LinkLabel(new GUIContent(section.linkText)))
					{
						Application.OpenURL(section.url);
					}
                }
                EditorGUILayout.EndVertical();
                GUILayout.Space(10);
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(16f);
			}
		}

		bool m_Initialized;

		GUIStyle LinkStyle { get { return m_LinkStyle; } }
		[SerializeField] GUIStyle m_LinkStyle;

		GUIStyle TitleStyle { get { return m_TitleStyle; } }
		[SerializeField] GUIStyle m_TitleStyle;

		GUIStyle HeadingStyle { get { return m_HeadingStyle; } }
		[SerializeField] GUIStyle m_HeadingStyle;

		GUIStyle BodyStyle { get { return m_BodyStyle; } }
		[SerializeField] GUIStyle m_BodyStyle;

		void Init()
		{
			if (m_Initialized)
				return;
			m_BodyStyle = new GUIStyle(EditorStyles.label);
			m_BodyStyle.wordWrap = true;
			m_BodyStyle.fontSize = 14;

			m_TitleStyle = new GUIStyle(m_BodyStyle);
			m_TitleStyle.fontSize = 26;

			m_HeadingStyle = new GUIStyle(m_BodyStyle);
			m_HeadingStyle.fontSize = 18;

			m_LinkStyle = new GUIStyle(m_BodyStyle);
			m_LinkStyle.wordWrap = false;
			// Match selection color which works nicely for both light and dark skins
			m_LinkStyle.normal.textColor = new Color(0x00 / 255f, 0x78 / 255f, 0xDA / 255f, 1f);
			m_LinkStyle.stretchWidth = false;

			m_Initialized = true;
		}

		bool LinkLabel(GUIContent label, params GUILayoutOption[] options)
		{
			var position = GUILayoutUtility.GetRect(label, LinkStyle, options);

			Handles.BeginGUI();
			Handles.color = LinkStyle.normal.textColor;
			Handles.DrawLine(new Vector3(position.xMin, position.yMax), new Vector3(position.xMax, position.yMax));
			Handles.color = Color.white;
			Handles.EndGUI();

			EditorGUIUtility.AddCursorRect(position, MouseCursor.Link);

			return GUI.Button(position, label, LinkStyle);
		}
    }
#endif
}