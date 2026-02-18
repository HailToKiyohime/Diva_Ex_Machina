//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;
using UnityEditor;

//  Dependencies: REST

//*********************************************************************


namespace REST.AdvancedBuildSystem.Editor
{
    internal class ABS_MenuManager : Object
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  priority
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //
        //  Create
        //      GameObjects
        //          New ABS_BuildingManager
        //          New ABS_BuildingParent
        //          New ABS_BuildingArea
        //          New ABS_Building (ABS_FreeBuilding)
        //          New ABS_Building (BasicGrid)
        //          New ABS_Building (AdvancedGridBuilding)
        //          New ABS_Building (SnapPointBased)
        //      ScriptableObjects
        //          ABS_Building Algorithm Settings
        //              New ABS_FreeBuilder Settings
        //              New ABS_BasicGridBuilder Settings
        //              New AdvancedGridBuildier Settings
        //              New ABS_SnapPointBasedBuilder Settings
        //          ABS_BuildingElement
        //              New ABS_BuildingElement List
        //              New Highlight Collection
        //          SnapPoint
        //              New SnapPoint
        //              New SnapPoint List
        //          ABS_BuildingArea
        //              New Ruleset
        //          ABS_BuildingManager Settings
        //              New OverrideElement RuleSet
        //              New Layer Collection
        //              New Input Settings
        //  Tools
        //      BuildingElementCreator
        //      BuildingLoader
        //      Statistics
        //      SceneHelper
        //
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  GameObjects
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        [MenuItem("Tools/Advanced Building System/Create/GameObjects/New BuildingManager", priority = 1)]
        public static void NewBuildingManager()
        {
            ABS_BuildingManagerEditor.CreateBuildingManager();
        }

        [MenuItem("Tools/Advanced Building System/Create/GameObjects/New BuildingParent", priority = 2)]
        public static void NewBuildingParent()
        {
            ABS_BuildingParentEditor.CreateBuildingParent();
        }

        [MenuItem("Tools/Advanced Building System/Create/GameObjects/New BuildingArea", priority = 3)]
        public static void NewBuildingArea()
        {
            GameObject buildingManager = new GameObject("ABS_BuildingArea");
            buildingManager.AddComponent<ABS_BuildingArea>();
        }

        [MenuItem("Tools/Advanced Building System/Create/GameObjects/New Building (FreeBuilding)", priority = 14)]
        public static void NewFreeBuilding()
        {
            ABS_BuildingEditor.CreateBuilding<ABS_FreeBuilding>("ABS_FreeBuilding");
        }
        
        [MenuItem("Tools/Advanced Building System/Create/GameObjects/New Building (BasicGrid)", priority = 15)]
        public static void NewBasicGridBuilding()
        {
            ABS_BuildingEditor.CreateBuilding<ABS_BasicGridBuilding>("ABS_BasicGridBuilding");
        }
        
        [MenuItem("Tools/Advanced Building System/Create/GameObjects/New Building (AdvancedGrid)", priority = 16)]
        public static void NewAdvancedGridBuilding()
        {
            ABS_BuildingEditor.CreateBuilding<ABS_AdvancedGridBuilding>("ABS_AdvancedGridBuilding");
        }
        [MenuItem("Tools/Advanced Building System/Create/GameObjects/New Building (SnapPointBased)", priority = 17)]
        public static void NewSnapPointBasedBuilding()
        {
            ABS_BuildingEditor.CreateBuilding<ABS_SnapPointBasedBuilding>("ABS_SnapPointBasedBuilding");
        }


        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  ScriptableObjects
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        [MenuItem("Tools/Advanced Building System/Create/ScriptableObjects/Building Algorithm Settings/New FreeBuilder Settings", priority = 1)]
        public static void NewFreeBuilderSettings()
        {
            ABS_EditorStorageManager.SaveScriptableObject<ABS_FreeBuilderSettings>("Save FreeBuilder Settings", "NewFreeBuilderSettings");
        }

        [MenuItem("Tools/Advanced Building System/Create/ScriptableObjects/Building Algorithm Settings/New BasicGridBuilder Settings", priority = 2)]
        public static void NewBasicGridBuilderSettings()
        {
            ABS_EditorStorageManager.SaveScriptableObject<ABS_BasicGridBuilderSettings>("Save BasicGridBuilder Settings", "NewBasicGridBuilderSettings");
        }

        [MenuItem("Tools/Advanced Building System/Create/ScriptableObjects/Building Algorithm Settings/New AdvancedGridBuildier Settings", priority = 3)]
        public static void NewAdvancedGridBuildierSettings()
        {
            ABS_EditorStorageManager.SaveScriptableObject<ABS_AdvancedGridBuilderSettings>("Save AdvancedGridBuilder Settings", "NewAdvancedGridBuilderSettings");
        }

        [MenuItem("Tools/Advanced Building System/Create/ScriptableObjects/Building Algorithm Settings/New SnapPointBasedBuilder Settings", priority = 4)]
        public static void NewSnapPointBasedBuilderSettings()
        {
            ABS_EditorStorageManager.SaveScriptableObject<ABS_SnapPointBasedBuilderSettings>("Save SnapPointBasedBuilder Settings", "NewSnapPointBasedBuilderSettings");
        }

        [MenuItem("Tools/Advanced Building System/Create/ScriptableObjects/BuildingElement/New BuildingElement List", priority = 2)]
        public static void NewBuildingElementList()
        {
            ABS_EditorStorageManager.SaveScriptableObject<ABS_BuildingElementList>("Save BuildingElement List", "NewBuildingElementList");
        }

        [MenuItem("Tools/Advanced Building System/Create/ScriptableObjects/BuildingElement/New Highlight Collection", priority = 2)]
        public static void NewHighlightCollection()
        {
            ABS_EditorStorageManager.SaveScriptableObject<ABS_BuildingElementList>("Save Highlight Collection", "NewHighlightCollection");
        }


        [MenuItem("Tools/Advanced Building System/Create/ScriptableObjects/SnapPoint/New SnapRelationship", priority = 1)]
        public static void NewSnapRelationship()
        {
            ABS_EditorStorageManager.SaveScriptableObject<ABS_SnapRelationship>("Save SnapRelationship", "NewSnapRelationship");
        }

        [MenuItem("Tools/Advanced Building System/Create/ScriptableObjects/SnapPoint/New SnapRelationship List", priority = 2)]
        public static void NewSnapRelationshipList()
        {
            ABS_EditorStorageManager.SaveScriptableObject<ABS_SnapRelationshipList>("Save SnapRelationship List", "NewSnapRelationshipList");
        }

        [MenuItem("Tools/Advanced Building System/Create/ScriptableObjects/BuildingArea/New Ruleset", priority = 1)]
        public static void NewBuildingAreaRuleset()
        {
            ABS_EditorStorageManager.SaveScriptableObject<ABS_BuildingAreaRuleset>("Save BuildingArea Ruleset", "NewBuildingAreaRuleset");
        }

        [MenuItem("Tools/Advanced Building System/Create/ScriptableObjects/BuildingManager Settings/New OverrideElement Ruleset", priority = 1)]
        public static void NewOverrideElementRuleset()
        {
            ABS_EditorStorageManager.SaveScriptableObject<ABS_OverrideElementRuleset>("Save New OverrideElement Ruleset", "NewOverrideElementRuleset");
        }

        [MenuItem("Tools/Advanced Building System/Create/ScriptableObjects/BuildingManager Settings/New Layer Collection", priority = 2)]
        public static void NewLayerCollection()
        {
            ABS_EditorStorageManager.SaveScriptableObject<ABS_LayerCollection>("Save Layer Collection", "NewLayerCollection");
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Tools
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        [MenuItem("Tools/Advanced Building System/Tools/BuildingElementCreator", priority = 1)]
        public static void ShowWindow_BuildingElementCreator()
        {
            ABS_BuildingElementCreator.ShowWindow();
        }

        [MenuItem("Tools/Advanced Building System/Tools/BuildingLoader", priority = 2)]
        public static void ShowWindow_BuildingLoader()
        {
            ABS_BuildingLoader.ShowWindow();
        }

        [MenuItem("Tools/Advanced Building System/Tools/Statistics", priority = 3)]
        public static void ShowWindow_Statistics()
        {
            ABS_Statistics.ShowWindow();
        }

        [MenuItem("Tools/Advanced Building System/Tools/SceneHelper", priority = 4)]
        public static void ShowWindow_SceneHelper()
        {
            ABS_SceneHelper.ShowWindow();
        }
    }
}