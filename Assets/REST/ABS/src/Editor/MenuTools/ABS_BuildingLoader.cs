//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEditor;
using UnityEngine;

//  Dependencies: REST
using REST.Utils;
using static REST.AdvancedBuildSystem.ABS_BuildingElement;
using System.Collections.Generic;

//*********************************************************************


namespace REST.AdvancedBuildSystem.Editor
{
    internal class ABS_BuildingLoader : ABS_EditorWindowBase
    {
        private string m_PersistedDataPath = string.Empty;
        private string m_PersistedData = string.Empty;
        private GUIContent m_PersistedDataPathGUIContent;

        private GameObject m_ParentObject = null;
        private GUIContent m_ParentObjectGUIContent;

        private ABS_BuildingParent m_BuildingParent = null;
        private GUIContent m_BuildingParentGUIContent;

        private ABS_BuildingElementList m_ElementList = null;
        private GUIContent m_ElementListGUIContent;

        private ABS_EditorTabView m_TabView = null;

        public ABS_BuildingLoader() : base()
        {
            m_TabView = new ABS_EditorTabView(3);
            m_TabView.AddCallback("Building Parent", ShowBuildingParentView);
            m_TabView.AddCallback("Building", ShowBuildingView);
        }

        public static void ShowWindow()
        {
            GetWindow<ABS_BuildingLoader>("ABS_BuildingLoader");
        }

        public void OnEnable()
        {
            m_PersistedDataPathGUIContent = new GUIContent("Persisted Data Path", "The path for the ABS_Building's Persisted Data");
            m_ElementListGUIContent = new GUIContent("Container", "Container of ABS_BuildingElement");
            m_ParentObjectGUIContent = new GUIContent("Parent Object", "Parent of created ABS_BuildingParent");
            m_BuildingParentGUIContent = new GUIContent("Building Parent", "Parent of created ABS_Building");
        }

        protected override void OnGUIImpl()
        {
            AddHeaderSection("Building Loader");
            m_TabView.Show(m_EditorStyleContainer);
        }

        private void ShowBuildingView()
        {
            AddPersistedDataSection();
            ABS_EditorUtils.Space();

            m_BuildingParent = ABS_EditorUtils.AddObjectField(m_BuildingParentGUIContent, m_BuildingParent, true);
            ABS_EditorUtils.Space();

            AddListSection();
            ABS_EditorUtils.Space();

            AddButtonSection();
        }

        private void ShowBuildingParentView()
        {
            AddPersistedDataSection();
            ABS_EditorUtils.Space();

            m_ParentObject = ABS_EditorUtils.AddObjectField(m_ParentObjectGUIContent, m_ParentObject, true);
            ABS_EditorUtils.Space();

            AddListSection();
            ABS_EditorUtils.Space();

            AddButtonSection();
        }

        private void ShowBuildingElementView()
        {
            AddPersistedDataSection();
            ABS_EditorUtils.Space();

            AddListSection();
            ABS_EditorUtils.Space();

            AddButtonSection();
        }

        private void AddPersistedDataSection ()
        {
            GUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField(m_PersistedDataPathGUIContent, GUILayout.Width(200));

                bool buttonResult = GUILayout.Button(
                    "Load Persisted Data",
                    m_EditorStyleContainer.SmallDarkButtonStyle,
                    GUILayout.Width(200)
                );
                if (buttonResult)
                {
                    if(ABS_EditorStorageManager.ReadPersistedDataFile(ref m_PersistedData, ref m_PersistedDataPath) != ABS_EditorStorageManager.ErrorCode.Success)
                    {
                        REST_Logging.Warrning("BuildingLoader", $"File '{m_PersistedDataPath}' not found.");
                    }
                }
            }
            GUILayout.EndHorizontal();

            if (string.IsNullOrEmpty(m_PersistedDataPath) || string.IsNullOrEmpty(m_PersistedData))
            {
                EditorGUILayout.HelpBox("Missing PersistedData", MessageType.Error);
            }
            else
            {
                EditorGUILayout.LabelField($"Path  :  {m_PersistedDataPath}");
            }
        }

        private void AddListSection()
        {
            UnityEngine.Object obj = ABS_EditorUtils.AddObjectField(m_ElementListGUIContent, m_ElementList, false);
            if (obj != null)
            {
                m_ElementList = obj as ABS_BuildingElementList;
            }
        }

        private void AddButtonSection()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("This feature is only working if the game is running!", MessageType.Warning);
                return;
            }

            if (m_ElementList == null)
            {
                EditorGUILayout.HelpBox("Missing List", MessageType.Error);
                return;
            }

            ABS_EditorUtils.StartEnableDuringGame();
            {
                bool buttonResult = GUILayout.Button("Load", m_EditorStyleContainer.DarkButtonStyle,
                    new GUILayoutOption[]
                    {
                        GUILayout.ExpandWidth (true)
                    }
                );

                if (buttonResult)
                {
                    if (!string.IsNullOrEmpty(m_PersistedData) && m_ElementList != null)
                    {
                        Load();
                    }
                }
            }
            ABS_EditorUtils.EndEnableDuringGame();
        }

        private void Load ()
        {
            switch (m_TabView.GetCurrentViewIdx())
            {
                case 0: // Building Parent
                    LoadBuildingParent();
                    return;
                case 1: // Building
                    LoadBuilding();
                    return;
                case 2: // Building Element
                    return;
            }
        }

        private void LoadBuildingParent ()
        {
            ABS_BuildingParent tmp = null;
            ABS_PersistencyLoadErrorCode res = ABS_BuildingParent.CreateFromJSON(m_PersistedData, m_ParentObject, m_ElementList, out tmp);
            if (res == ABS_PersistencyLoadErrorCode.Successful)
            {
                REST_Logging.Info("BuildingLoader", "Loading of ABS_BuildingParent was successful!");
            }
            else
            {
                REST_Logging.Warrning("BuildingLoader", $"Loading of ABS_BuildingParent has failed. Error: {res.ToString()}");
            }
        }

        private void LoadBuilding()
        {
            ABS_Building temp = null;
            List<ABS_BuildingElementConnectionData> elementConnections = new List<ABS_BuildingElementConnectionData>();
            ABS_PersistencyLoadErrorCode res = ABS_Building.CreateFromPersistedJSON(m_PersistedData, m_BuildingParent, m_ElementList, out temp, elementConnections);
            if (res == ABS_PersistencyLoadErrorCode.Successful)
            {
                REST_Logging.Info("BuildingLoader", "Loading of ABS_Building was successful!");
            }
            else
            {
                REST_Logging.Warrning("BuildingLoader", $"Loading of ABS_Building has failed. Error: {res.ToString()}");
            }
        }
    }
}