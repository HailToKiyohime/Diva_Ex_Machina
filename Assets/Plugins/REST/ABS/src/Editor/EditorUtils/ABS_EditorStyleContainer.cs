//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;
using UnityEditor;
using TMPro;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem.Editor
{
    internal class ABS_EditorStyleContainer : UnityEditor.Editor
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Variables
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        //--------------------------------------------------------------
        //Styles

        private GUIStyle m_TitleStyle = null;
        private GUIStyle m_HeaderTitleStyle = null;
        private GUIStyle m_HeadStyleSection = null;
        private GUIStyle m_HeadStyleSectionGroup = null;
        private GUIStyle m_HeadStyleBasicProperties = null;
        private GUIStyle m_HeadStyleSpecificProperties = null;
        private GUIStyle m_HeadStyleGizmos = null;

        private GUIStyle m_ColoredHeaderStyle_Red = null;

        //--------------------------------------------------------------
        //Buttons

        private GUIStyle m_DarkButtonStyle;
        private GUIStyle m_GreenButtonStyle;
        private GUIStyle m_BlueButtonStyle;
        private GUIStyle m_SmallDarkButtonStyle;
        private GUIStyle m_SmallRedButtonStyle;
        private GUIStyle m_SmallGreenButtonStyle;
        private GUIStyle m_SmallBlueButtonStyle;

        //--------------------------------------------------------------
        //Box

        private GUIStyle m_DarkBoxStyle;

        //--------------------------------------------------------------
        //Textures

        //Bigger number means lighter
        private Texture2D m_DarkTexture_1;
        private Texture2D m_DarkTexture_2;

        private Texture2D m_RedTexture;
        private Texture2D m_GreenTexture;
        private Texture2D m_BlueTexture;

        private Texture2D m_Icon;

        //--------------------------------------------------------------
        //Colors

        //Bigger number means lighter
        public static readonly UnityEngine.Color s_DarkColor_1 = new UnityEngine.Color(40f / 255f, 40f / 255f, 40f / 255f, 1);
        public static readonly UnityEngine.Color s_DarkColor_2 = new UnityEngine.Color(50f / 255f, 50f / 255f, 50f / 255f, 1);

        public static readonly UnityEngine.Color s_RedColor = new UnityEngine.Color(239f / 255f, 62f / 255f, 85f / 255f, 1);
        public static readonly UnityEngine.Color s_GreenColor = new UnityEngine.Color(85f / 255f, 167f / 255f, 87f / 255f, 1);
        public static readonly UnityEngine.Color s_BlueColor = new UnityEngine.Color(0f / 255f, 100f / 255f, 255f / 255f, 1);

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Getter / Setter
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        //--------------------------------------------------------------
        //Buttons
        public GUIStyle DarkButtonStyle { get { return m_DarkButtonStyle; } }
        public GUIStyle GreenButtonStyle { get { return m_GreenButtonStyle; } }
        public GUIStyle BlueButtonStyle { get { return m_BlueButtonStyle; } }
        public GUIStyle SmallDarkButtonStyle { get { return m_SmallDarkButtonStyle; } }
        public GUIStyle SmallRedButtonStyle { get { return m_SmallRedButtonStyle; } }
        public GUIStyle SmallGreenButtonStyle { get { return m_SmallGreenButtonStyle; } }
        public GUIStyle SmallBlueButtonStyle { get { return m_SmallBlueButtonStyle; } }

        //--------------------------------------------------------------
        //Box
        public GUIStyle DarkBoxStyle { get { return m_DarkBoxStyle; } }

        //--------------------------------------------------------------
        //Styles
        public GUIStyle TitleStyle { get { return m_TitleStyle; } }
        public GUIStyle HeaderTitleStyle { get { return m_HeaderTitleStyle; } }
        public GUIStyle HeadStyleSection { get { return m_HeadStyleSection; } }
        public GUIStyle HeadStyleSectionGroup { get { return m_HeadStyleSectionGroup; } }
        public GUIStyle HeadStyleBasicProperties { get { return m_HeadStyleBasicProperties; } }
        public GUIStyle HeadStyleSpecificProperties { get { return m_HeadStyleSpecificProperties; } }
        public GUIStyle HeadStyleGizmos { get { return m_HeadStyleGizmos; } }
        public GUIStyle ColoredHeaderStyle_Red { get { return m_ColoredHeaderStyle_Red; } }

        //--------------------------------------------------------------
        //Textures
        public Texture2D Icon { get { return m_Icon; } }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Main Logic
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public void Init()
        {
            m_Icon = ABS_EditorStorageManager.GetImage("BuildingManagerIcon");

            //Terxture
            CreateTexture(s_DarkColor_1, ref m_DarkTexture_1);
            CreateTexture(s_DarkColor_2, ref m_DarkTexture_2);
            CreateTexture(s_RedColor, ref m_RedTexture);
            CreateTexture(s_GreenColor, ref m_GreenTexture);
            CreateTexture(s_BlueColor, ref m_BlueTexture);

            //Button
            CreateButtonStyle(ref m_DarkButtonStyle, m_DarkTexture_1);
            CreateButtonStyle(ref m_GreenButtonStyle, m_GreenTexture);
            CreateButtonStyle(ref m_BlueButtonStyle, m_BlueTexture);
            CreateSmallButtonStyle(ref m_SmallDarkButtonStyle, m_DarkTexture_1);
            CreateSmallButtonStyle(ref m_SmallRedButtonStyle, m_RedTexture);
            CreateSmallButtonStyle(ref m_SmallGreenButtonStyle, m_GreenTexture);
            CreateSmallButtonStyle(ref m_SmallBlueButtonStyle, m_BlueTexture);

            m_DarkBoxStyle = new GUIStyle(GUI.skin.box);
            AddBorder(ref m_DarkBoxStyle, m_DarkTexture_2);
            m_DarkBoxStyle.padding.left = 5;
            m_DarkBoxStyle.padding.right = 5;
            m_DarkBoxStyle.padding.top = 5;
            m_DarkBoxStyle.padding.bottom = 5;

            m_TitleStyle = new GUIStyle(EditorStyles.label);
            m_TitleStyle.wordWrap = true;
            m_TitleStyle.fontSize = 26;

            m_HeaderTitleStyle = new GUIStyle(GUI.skin.label);
            m_HeaderTitleStyle.fontStyle = UnityEngine.FontStyle.Bold;
            m_HeaderTitleStyle.fontSize = 25;
            m_HeaderTitleStyle.alignment = TextAnchor.MiddleLeft;
            
            m_HeadStyleSection = new GUIStyle(GUI.skin.label);
            m_HeadStyleSection.fontStyle = UnityEngine.FontStyle.Bold;
            m_HeadStyleSection.fontSize = 14;

            m_HeadStyleSectionGroup = new GUIStyle(GUI.skin.label);
            m_HeadStyleSectionGroup.fontStyle = UnityEngine.FontStyle.Bold;
            m_HeadStyleSectionGroup.fontSize = 14;

            m_HeadStyleBasicProperties = new GUIStyle(GUI.skin.label);
            m_HeadStyleBasicProperties.fontStyle = UnityEngine.FontStyle.Bold;
            m_HeadStyleBasicProperties.fontSize = 12;
            m_HeadStyleBasicProperties.normal.textColor = UnityEngine.Color.cyan;

            m_HeadStyleSpecificProperties = new GUIStyle(GUI.skin.label);
            m_HeadStyleSpecificProperties.fontStyle = UnityEngine.FontStyle.Bold;
            m_HeadStyleSpecificProperties.fontSize = 12;
            m_HeadStyleSpecificProperties.normal.textColor = UnityEngine.Color.green;

            m_HeadStyleGizmos = new GUIStyle(GUI.skin.label);
            m_HeadStyleGizmos.fontStyle = UnityEngine.FontStyle.Bold;
            m_HeadStyleGizmos.fontSize = 12;
            m_HeadStyleGizmos.normal.textColor = UnityEngine.Color.yellow;

            m_ColoredHeaderStyle_Red = new GUIStyle(EditorStyles.foldoutHeader);
            m_ColoredHeaderStyle_Red.normal.textColor = s_RedColor;
            m_ColoredHeaderStyle_Red.focused.textColor = s_RedColor;
            m_ColoredHeaderStyle_Red.hover.textColor = s_RedColor;
            m_ColoredHeaderStyle_Red.active.textColor = s_RedColor;

            m_ColoredHeaderStyle_Red.onActive.textColor = s_RedColor;
            m_ColoredHeaderStyle_Red.onFocused.textColor = s_RedColor;
            m_ColoredHeaderStyle_Red.onHover.textColor = s_RedColor;
            m_ColoredHeaderStyle_Red.onActive.textColor = s_RedColor;

            m_ColoredHeaderStyle_Red.fontStyle = FontStyle.Bold;
            m_ColoredHeaderStyle_Red.alignment = TextAnchor.MiddleLeft;
            m_ColoredHeaderStyle_Red.padding.left = 16;

        }

        public static string ColorizeText(ref GUIStyle p_Style, string p_Text, UnityEngine.Color p_Color)
        {
            p_Style.richText = true;
            return $"<b><color=#{ColorUtility.ToHtmlStringRGBA(p_Color)}>{p_Text}</color></b>";
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Private Functions
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private void AddBorder(ref GUIStyle p_Style, Texture2D p_Texture)
        { 
            p_Style.padding = new RectOffset(0, 0, 0, 0);

            p_Style.border.left = 10;
            p_Style.border.right = 10;
            p_Style.border.top = 10;
            p_Style.border.bottom = 10;

            p_Style.normal.background = p_Texture;
        }

        private void CreateSmallButtonStyle(ref GUIStyle p_Button, Texture2D p_Texture)
        {
            CreateButtonStyle(ref p_Button, p_Texture);

            p_Button.fontSize = 11;
            p_Button.fixedHeight = 15;
        }

        private void CreateButtonStyle(ref GUIStyle p_Button, Texture2D p_Texture)
        {
            p_Button = new GUIStyle(EditorStyles.miniButton);
            p_Button.fontSize = 14;
            p_Button.fixedHeight = 25;
            p_Button.normal.textColor = UnityEngine.Color.white;
            p_Button.alignment = TextAnchor.MiddleCenter;

            AddBorder(ref p_Button, p_Texture);
        }

        private void CreateTexture(UnityEngine.Color p_Color, ref Texture2D p_Texture) 
        {
            p_Texture = new Texture2D(50, 50);
            for (int y = 0; y < p_Texture.height; y++)
            {
                for (int x = 0; x < p_Texture.width; x++)
                {
                    if (y == 0 || x == 0 || y == p_Texture.height - 1 || x == p_Texture.width - 1)
                    {
                        p_Texture.SetPixel(x, y, UnityEngine.Color.black);
                    }
                    else
                    {
                        p_Texture.SetPixel(x, y, p_Color);
                    }
                }
            }
            p_Texture.Apply();
        }
    }
}