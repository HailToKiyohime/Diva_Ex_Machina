//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;
using UnityEditor;

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem.Editor
{
    internal class ABS_BuildingElementCreator : ABS_EditorWindowBase
    {
        private bool m_UseObjectName = false;
        private GUIContent m_UseObjectNameGUIContent;
        private string m_BuildingElementName = "BuildingElement";
        private GUIContent m_BuildingElementNameGUIContent;

        private ABS_BuilderBaseSettings m_Settings;
        private GUIContent m_SettingsGUIContent;

        private UnityEngine.GameObject m_GameObject;
        private GUIContent m_GameObjectGUIContent;

        private ABS_BuildingElement m_CopyTarget;
        private GUIContent m_CopyTargetGUIContent;

        private Vector2 m_ScrollPos;

        public static void ShowWindow()
        {
            GetWindow<ABS_BuildingElementCreator>("ABS_BuildingElementCreator");
        }

        public void OnEnable()
        {
            m_BuildingElementNameGUIContent = new GUIContent("New BuildingElement Name", "The name of the new BuildingElement");
            m_UseObjectNameGUIContent = new GUIContent("Use Object Name", "Use the provided GameObject's name as the new BuildingElement's name with a \"_BuildingElement\" postfix.");
            m_SettingsGUIContent = new GUIContent("Algorithm Settings", "The BuildingElement's ABS_BuilderBaseSettings");
            m_GameObjectGUIContent = new GUIContent("GameObject", "The BuildingElement's GameObject what ");

            m_CopyTargetGUIContent = new GUIContent("Copy Target", "All of the properties will be copied from the copy target.");
        }

        protected override void OnGUIImpl()
        {
            AddHeaderSection("BuildingElement Creator");

            m_ScrollPos = ABS_EditorUtils.StartScrollView(m_ScrollPos);
            {
                AddMetaDataSection();

                ABS_EditorUtils.Space();
                AddButtonSection();
            }
            ABS_EditorUtils.EndScrollView();
        }

        private void AddMetaDataSection ()
        {
            //Name
            m_UseObjectName = EditorGUILayout.Toggle(m_UseObjectNameGUIContent, m_UseObjectName);
            if (m_UseObjectName)
            {
                if (m_GameObject != null)
                {
                    m_BuildingElementName = $"{m_GameObject.name}_BuildingElement";

                    EditorGUILayout.LabelField($"New BuildingElement's name  : {m_BuildingElementName}");
                }
                else
                {
                    EditorGUILayout.HelpBox("Missing GameObject", MessageType.Error);
                }
            }
            else
            {
                m_BuildingElementName = EditorGUILayout.TextField(m_BuildingElementNameGUIContent, m_BuildingElementName);
            }

            //MetaData
            ABS_EditorUtils.Space();
            UnityEngine.Object obj = ABS_EditorUtils.AddObjectField(m_SettingsGUIContent, m_Settings, false);
            if (obj == null)
            {
                EditorGUILayout.HelpBox("Missing MetaData", MessageType.Error);
            }
            else
            {
                m_Settings = obj as ABS_BuilderBaseSettings;
                if (m_Settings == null)
                {
                    EditorGUILayout.HelpBox("Missing Settings", MessageType.Error);
                }
               // ABS_BuilderBaseSettingsEditor.DrawDetails(m_EditorStyleContainer, ref m_MetaDataDetailsSectionVariable, m_Settings);
            }

            ABS_EditorUtils.Space();

            m_GameObject = ABS_EditorUtils.AddObjectField(m_GameObjectGUIContent, m_GameObject, false);
            if (m_GameObject == null)
            {
                EditorGUILayout.HelpBox("Missing GameObject", MessageType.Error);
            }

            ABS_EditorUtils.AddSeparatorLine();

            m_CopyTarget = ABS_EditorUtils.AddObjectField<ABS_BuildingElement>(m_CopyTargetGUIContent, m_CopyTarget, false);
        }

        private void AddButtonSection()
        {
            bool canCreate = m_GameObject != null && m_Settings != null;
            GUILayout.BeginHorizontal();
            ABS_EditorUtils.StartDisable(!canCreate);
            {
                bool buttonResult = GUILayout.Button(
                    "Instantiate GameObject",
                    m_EditorStyleContainer.DarkButtonStyle,
                    GUILayout.ExpandWidth(true)
                );
                if (buttonResult && canCreate)
                {
                    Instantiate(false);
                }

                bool buttonResult2 = GUILayout.Button(
                    "Create Prefab",
                    m_EditorStyleContainer.DarkButtonStyle,
                    GUILayout.ExpandWidth(true)
                );
                if (buttonResult2 && canCreate)
                {
                    Instantiate(true);
                }
            }
            ABS_EditorUtils.EndDisable();
            GUILayout.EndHorizontal();
        }

        private void Instantiate (in bool p_CreatePrefab)
        {
            GameObject gameObject = new GameObject(m_BuildingElementName);

            Instantiate(m_GameObject, gameObject.transform);

            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            collider.includeLayers = m_Settings.LayerCollection.LayerOfPlayer;

            ABS_BuildingElement buildingElement = gameObject.AddComponent<ABS_BuildingElement>();
            buildingElement.PositionSearchAlgorithm = m_Settings.AlgorithmType;
            buildingElement.PositionAlgorithmSettings = m_Settings;
            buildingElement.GenerateNewPrefabGuid();
            buildingElement.RefreshLink();
            Copy(buildingElement, collider);

            for (int i = 0; i < 32; i++)
            {
                if ((m_Settings.LayerCollection.LayerOfBuildingElement & (1 << i)) != 0)
                {
                    ChangeLayer(gameObject, i);
                }
            }

            if (p_CreatePrefab)
            {
                ABS_EditorStorageManager.ErrorCode err = ABS_EditorStorageManager.SaveObjectAsPrefab(gameObject, m_BuildingElementName);
                if (err != ABS_EditorStorageManager.ErrorCode.Success)
                {
                    REST_Logging.Info("BuildingElementCreator", "The creation of the BuidlingElement was Successful!");
                }
                DestroyImmediate(gameObject);
            }
        }

        private void ChangeLayer(GameObject p_Object, int p_Layer)
        {
            p_Object.layer = p_Layer;
            foreach (Transform child in p_Object.transform)
            {
                ChangeLayer(child.gameObject, p_Layer);
            }
        }


        private void Copy (ABS_BuildingElement p_NewElement, BoxCollider p_Collider)
        {
            if (!m_CopyTarget)
            {
                return;
            }

            p_NewElement.FinalElement = m_CopyTarget.FinalElement;

            p_NewElement.PreBuilt = m_CopyTarget.PreBuilt;
            p_NewElement.Foundation = m_CopyTarget.Foundation;
            p_NewElement.Indestructible = m_CopyTarget.Indestructible;
            p_NewElement.CanNotBeAttachTarget = m_CopyTarget.CanNotBeAttachTarget;
            p_NewElement.AreaType = m_CopyTarget.AreaType;

            p_NewElement.SnapToPreBuiltFinalElement = m_CopyTarget.SnapToPreBuiltFinalElement;
            p_NewElement.ShouldAllowedByArea = m_CopyTarget.ShouldAllowedByArea;
            p_NewElement.ShouldOverride = m_CopyTarget.ShouldOverride;

            p_NewElement.DragBuildingEnabled = m_CopyTarget.DragBuildingEnabled;
            p_NewElement.DragBuildingBehaviour = m_CopyTarget.DragBuildingBehaviour;
            p_NewElement.EnabledDragBuildingX = m_CopyTarget.EnabledDragBuildingX;
            p_NewElement.EnabledDragBuildingZ = m_CopyTarget.EnabledDragBuildingZ;

            p_NewElement.StableElement = m_CopyTarget.StableElement;
            p_NewElement.SnapPointRuleSet = m_CopyTarget.SnapPointRuleSet;
            p_NewElement.ShouldAttached = m_CopyTarget.ShouldAttached;

            p_NewElement.HighlightCollection = m_CopyTarget.HighlightCollection;


            p_Collider.size = m_CopyTarget.Dimension;

            p_NewElement.Dimension = m_CopyTarget.Dimension;
        }
    }
}