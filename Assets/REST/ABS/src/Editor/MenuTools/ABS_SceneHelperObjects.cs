//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEditor;
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem.Editor
{
    internal class ABS_SceneHelperObjects
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Properties
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private Vector2 m_ScrollPos;

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion Properties
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public void ObjectsView(in ABS_EditorStyleContainer p_EditorStyleContainer)
        {
            m_ScrollPos = ABS_EditorUtils.StartScrollView(m_ScrollPos);
            {
                ABS_EditorUtils.AddSeparatorLine();
                EditorGUILayout.LabelField("Quick Start", p_EditorStyleContainer.HeadStyleSection);
                bool buttonResult = GUILayout.Button($"Setup Advanced Building System", p_EditorStyleContainer.SmallDarkButtonStyle, GUILayout.Width(220));
                if (buttonResult)
                {
                    SetupABS();
                }

                ABS_EditorUtils.AddSeparatorLine();
                EditorGUILayout.LabelField("BuildingManager", p_EditorStyleContainer.HeadStyleSection);
                buttonResult = GUILayout.Button($"Create BuildingManager", p_EditorStyleContainer.SmallDarkButtonStyle, GUILayout.Width(220));
                if (buttonResult)
                {
                    ABS_BuildingManagerEditor.CreateBuildingManager();
                }

                ABS_EditorUtils.AddSeparatorLine();
                EditorGUILayout.LabelField("BuildingParent", p_EditorStyleContainer.HeadStyleSection);
                buttonResult = GUILayout.Button($"Create BuildingParent", p_EditorStyleContainer.SmallDarkButtonStyle, GUILayout.Width(220));
                if (buttonResult)
                {
                    ABS_BuildingParentEditor.CreateBuildingParent();
                }

                ABS_EditorUtils.AddSeparatorLine();
                EditorGUILayout.LabelField("Buildings", p_EditorStyleContainer.HeadStyleSection);

                CreateBuilding<ABS_FreeBuilding>(p_EditorStyleContainer, "ABS_FreeBuilding");
                CreateBuilding<ABS_BasicGridBuilding>(p_EditorStyleContainer, "ABS_BasicGridBuilding");
                CreateBuilding<ABS_AdvancedGridBuilding>(p_EditorStyleContainer, "ABS_AdvancedGridBuilding");
                CreateBuilding<ABS_SnapPointBasedBuilding>(p_EditorStyleContainer, "ABS_SnapPointBasedBuilding");

            }
            ABS_EditorUtils.EndScrollView();
        }

        private void CreateBuilding<BuildingType>(in ABS_EditorStyleContainer p_EditorStyleContainer, in string p_BuildingName)
            where BuildingType : ABS_Building
        {
            ABS_EditorUtils.Space(2);
            bool buttonResult = GUILayout.Button($"Create {p_BuildingName}", p_EditorStyleContainer.SmallDarkButtonStyle, GUILayout.Width(220));
            if (buttonResult)
            {
                ABS_BuildingEditor.CreateBuilding<BuildingType>(p_BuildingName);
            }

        }

        private void SetupABS ()
        {
            GameObject parentObject = new GameObject("Advanced Building System");

            ABS_BuildingManager manager = ABS_BuildingManagerEditor.CreateBuildingManager();
            manager.transform.parent = parentObject.transform;

            ABS_BuildingParent buildingParent = ABS_BuildingParentEditor.CreateBuildingParent();
            buildingParent.transform.parent = parentObject.transform;
            ABS_BuildingParentEditor.CreateGlobalFreeBuilding(buildingParent);
            ABS_BuildingParentEditor.CreateGlobalBasicGridBuilding(buildingParent);

            manager.BuildingParent = buildingParent;

            ABS_EditorUtils.Dirty(parentObject);
            ABS_EditorUtils.Dirty(manager);
            ABS_EditorUtils.Dirty(buildingParent);
        }
    }
}