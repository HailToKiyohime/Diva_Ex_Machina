//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;
using UnityEditor;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem.Editor
{
    [CustomEditor(typeof(ABS_BuildingManager))]
    internal class ABS_BuildingManagerEditor : ABS_EditorBase
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Variables
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        //MetaData
        private SerializedProperty m_ElementListProperty;
        private GUIContent m_ElementListGUIContent;
        private SerializedProperty m_BuildingParentProperty;
        private GUIContent m_BuildingParentGUIContent;
        private SerializedProperty m_ObjectPoolProperty;
        private GUIContent m_ObjectPoolGUIContent;
        private SerializedProperty m_SandboxProperty;
        private GUIContent m_SandboxGUIContent;
        private SerializedProperty m_EnableCacheProperty;
        private GUIContent m_EnableCacheGUIContent;
        private SerializedProperty m_TrackerObjectProperty;
        private GUIContent m_TrackerObjectGUIContent;

        //----------------------------------------------------------------------------------------------------------------------
        //Building Behaviour
        private SerializedProperty m_SearchPositionOnResetProperty;
        private GUIContent m_SearchPositionOnResetGUIContent;

        private SerializedProperty m_EnableForcedFallbackProperty;
        private GUIContent m_EnableForcedFallbackGUIContent;
        private SerializedProperty m_ForcedFallbackResetOnElementPlaceProperty;
        private GUIContent m_ForcedFallbackResetOnElementPlaceGUIContent;

        //----------------------------------------------------------------------------------------------------------------------
        //Raycast
        private SerializedProperty m_RaycastModeProperty;
        private GUIContent m_RaycastModeGUIContent;
        private SerializedProperty m_LayerCollectionProperty;
        private GUIContent m_LayerCollectionGUIContent;
        private SerializedProperty m_RaycastDistanceProperty;
        private GUIContent m_RaycastDistanceGUIContent;
        private SerializedProperty m_RaycastOffsetProperty;
        private GUIContent m_RaycastOffsetGUIContent;
        private SerializedProperty m_CameraProperty;
        private GUIContent m_CameraGUIContent;

        //----------------------------------------------------------------------------------------------------------------------
        //Input
        private SerializedProperty m_InputTypeProperty;
        private GUIContent m_InputTypeGUIContent;

        private SerializedProperty m_RotationInputTypeProperty;
        private GUIContent m_RotationInputTypeGUIContent;
        private SerializedProperty m_KeyBasedRotationCycleTimeProperty;
        private GUIContent m_KeyBasedRotationCycleTimeGUIContent;
        private SerializedProperty m_KeyForRotationRightProperty;
        private GUIContent m_KeyForRotationRightGUIContent;
        private SerializedProperty m_KeyForRotationLeftProperty;
        private GUIContent m_KeyForRotationLeftGUIContent;

        private SerializedProperty m_KeyForBuildProperty;
        private GUIContent m_KeyForBuildGUIContent;
        private SerializedProperty m_KeyForDestroyProperty;
        private GUIContent m_KeyForDestroyGUIContent;
        private SerializedProperty m_KeyForDragBuildProperty;
        private GUIContent m_KeyForDragBuildGUIContent;
        private SerializedProperty m_KeyForDragDestroyProperty;
        private GUIContent m_KeyForDragDestroyGUIContent;

        private SerializedProperty m_KeyForModeChangeProperty;
        private GUIContent m_KeyForModeChangeGUIContent;
        private SerializedProperty m_KeyForForcedFallbackProperty;
        private GUIContent m_KeyForForcedFallbackGUIContent;

        private SerializedProperty m_KeyForUndoProperty;
        private GUIContent m_KeyForUndoGUIContent;
        private SerializedProperty m_KeyForRedoProperty;
        private GUIContent m_KeyForRedoGUIContent;

        private SerializedProperty m_KeyForAlignRotationToGroundProperty;
        private GUIContent m_KeyForAlignRotationToGroundGUIContent;
        private SerializedProperty m_KeyForAlignRotationToBuildingElementsProperty;
        private GUIContent m_KeyForAlignRotationToBuildingElementsGUIContent;

        //----------------------------------------------------------------------------------------------------------------------
        //Destroy
        private SerializedProperty m_DestroyTypeProperty;
        private GUIContent m_DestroyTypeGUIContent;
        private SerializedProperty m_DestroyTimerDurationProperty;
        private GUIContent m_DestroyTimerDurationGUIContent;
        private SerializedProperty m_CutTimerOnLookAwayProperty;
        private GUIContent m_CutTimerOnLookAwayGUIContent;
        private SerializedProperty m_MaximumDestoryCountProperty;
        private GUIContent m_MaximumDestoryCountGUIContent;

        //----------------------------------------------------------------------------------------------------------------------
        //Action History
        private SerializedProperty m_IsHistoryEnabledProperty;
        private GUIContent m_IsHistoryEnabledGUIContent;
        private SerializedProperty m_HistoryActionCountProperty;
        private GUIContent m_HistoryActionCountGUIContent;
        private SerializedProperty m_PartialProcessingProperty;
        private GUIContent m_PartialProcessingGUIContent;
        private SerializedProperty m_ClearHistoryInCaseOfErrorProperty;
        private GUIContent m_ClearHistoryInCaseOfErrorGUIContent;

        //----------------------------------------------------------------------------------------------------------------------
        //Statistics
        private SerializedProperty m_StatisticsEnableCounterProperty;
        private GUIContent m_StatisticsEnableCounterGUIContent;
        private SerializedProperty m_StatisticsBuilderCounterProperty;
        private GUIContent m_StatisticsBuilderCounterGUIContent;

        //----------------------------------------------------------------------------------------------------------------------
        //Others
        private bool m_LayerCollectionDetailsSectionVariable = false;

        private ABS_EditorTabView m_TabView = null;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  EditorBase Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_BuildingManagerEditor() : base()
        {
            m_TabView = new ABS_EditorTabView(4);
            m_TabView.AddCallback("MetaData", AddSectionMetaDataProperties);
            m_TabView.AddCallback("Building Behaviour", AddSectionBuildingBehaviourProperties);
            m_TabView.AddCallback("Raycast", AddSectionRaycastProperties);
            m_TabView.AddCallback("Input", AddSectionInputProperties);
            m_TabView.AddCallback("Destroy Properties", AddDestroyProperties);
            m_TabView.AddCallback("Action History", AddHistoryProperties);
            m_TabView.AddCallback("Statistics", AddSectionStatisticsProperties);
        }

        protected override void OnEnableImpl()
        {
            //----------------------------------------------------------------------------------------------------------------------
            //MetaData
            m_ElementListProperty = serializedObject.FindProperty("m_ElementList");
            m_ElementListGUIContent = new GUIContent("BuildingElement List", "The elements what can be built. This list is used for indexing.");

            m_BuildingParentProperty = serializedObject.FindProperty("m_BuildingParent");
            m_BuildingParentGUIContent = new GUIContent("Building Parent", "This object will be the parent of the created buildings in the Hierarchy.");

            m_ObjectPoolProperty = serializedObject.FindProperty("m_ObjectPool");
            m_ObjectPoolGUIContent = new GUIContent("Object Pool", "ObjectPool object");

            m_SandboxProperty = serializedObject.FindProperty("m_Sandbox");
            m_SandboxGUIContent = new GUIContent("Sandbox", "If this property is set then the BuildingManager will ignore the BuildingElements' cost");

            m_EnableCacheProperty = serializedObject.FindProperty("m_EnableCache");
            m_EnableCacheGUIContent = new GUIContent("Enable Caching", "Enabler the caching mechanism. It will increase the performance and the memory usage of the algorithm");

            m_TrackerObjectProperty = serializedObject.FindProperty("m_TrackerObject");
            m_TrackerObjectGUIContent = new GUIContent("Tracker Objects",
                "With these objects you can track the Building Manager's algorithms " +
                "or make decisions to shape the algorithm logic for you expectation.");

            //----------------------------------------------------------------------------------------------------------------------
            //Building Behaviour
            m_SearchPositionOnResetProperty = serializedObject.FindProperty("m_SearchPositionOnReset");
            m_SearchPositionOnResetGUIContent = new GUIContent("Search Position After Reset",
                "After the BuildingElement has been reset a new raycast should be done or not? " +
                "It can be important when you can not call the UpdateImpl function but you need to reset the BuildingElement. " +
                "For example when you has a transparent Inventory and you change the BuildingElement so if you want to change the BuildingSystem's " +
                "BuildingElement this property should be set.");

            m_EnableForcedFallbackProperty = serializedObject.FindProperty("m_EnableForcedFallback");
            m_EnableForcedFallbackGUIContent = new GUIContent("Enable Forced Fallback", "If it is true then the Forced Falllback feature is available. " +
                "The Forced Falllback feature gives to the players the possibility to intentionally change the used building algorithm to the fallback FreeBuilding at any time.");
            m_ForcedFallbackResetOnElementPlaceProperty = serializedObject.FindProperty("m_ForcedFallbackResetOnElementPlace");
            m_ForcedFallbackResetOnElementPlaceGUIContent = new GUIContent("Forced Fallback Reset On Element Place", "Reset the Forced Fallback state to the original after an element has been placed.");

            //----------------------------------------------------------------------------------------------------------------------
            //Raycast
            m_RaycastModeProperty = serializedObject.FindProperty("m_RaycastMode");
            m_RaycastModeGUIContent = new GUIContent("Raycast Mode", "Camera of cursor based raycast.");
            m_LayerCollectionProperty = serializedObject.FindProperty("m_LayerCollection");
            m_LayerCollectionGUIContent = new GUIContent("Layer Collection", "The collection of the layers used by the Manager");
            m_RaycastDistanceProperty = serializedObject.FindProperty("m_RaycastDistance");
            m_RaycastDistanceGUIContent = new GUIContent("Raycast Distance", "The range of the raycast.");
            m_RaycastOffsetProperty = serializedObject.FindProperty("m_RaycastOffset");
            m_RaycastOffsetGUIContent = new GUIContent("Raycast Offset", "Raycast's startpoint offset from the camera.");
            m_CameraProperty = serializedObject.FindProperty("m_Camera");
            m_CameraGUIContent = new GUIContent("Camera", "The Camera what will be used for raycast.");

            //----------------------------------------------------------------------------------------------------------------------
            //Input
            m_InputTypeProperty = serializedObject.FindProperty("m_InputType");
            m_InputTypeGUIContent = new GUIContent("Input Type", "The type of the input system. (Messages, Key, Both)");

            m_RotationInputTypeProperty = serializedObject.FindProperty("m_RotationInputType");
            m_RotationInputTypeGUIContent = new GUIContent("Rotation Input Type", "The type of the rotation input system. (MouseWheel, Button)");
            m_KeyBasedRotationCycleTimeProperty = serializedObject.FindProperty("m_KeyBasedRotationCycleTime");
            m_KeyBasedRotationCycleTimeGUIContent = new GUIContent("Key Based Rotation Cycle Time", "The cycle time of the rotation when the button is held.");
            m_KeyForRotationRightProperty = serializedObject.FindProperty("m_KeyForRotationRight");
            m_KeyForRotationRightGUIContent = new GUIContent("Rotation Right", "The input key for the rotation to right.");
            m_KeyForRotationLeftProperty = serializedObject.FindProperty("m_KeyForRotationLeft");
            m_KeyForRotationLeftGUIContent = new GUIContent("Rotation Left", "The input key for the rotation to left.");

            m_KeyForBuildProperty = serializedObject.FindProperty("m_KeyForBuild");
            m_KeyForBuildGUIContent = new GUIContent("Build", "The input key for the Simple Build feature.");
            m_KeyForDestroyProperty = serializedObject.FindProperty("m_KeyForDestroy");
            m_KeyForDestroyGUIContent = new GUIContent("Destroy", "The input key for the Simple Destroy feature.");
            m_KeyForDragBuildProperty = serializedObject.FindProperty("m_KeyForDragBuild");
            m_KeyForDragBuildGUIContent = new GUIContent("Drag Build", "The input key for the Drag Build feature.");
            m_KeyForDragDestroyProperty = serializedObject.FindProperty("m_KeyForDragDestroy");
            m_KeyForDragDestroyGUIContent = new GUIContent("Drag Destroy", "The input key for the Drag Destroy feature.");

            m_KeyForModeChangeProperty = serializedObject.FindProperty("m_KeyForModeChange");
            m_KeyForModeChangeGUIContent = new GUIContent("Change Mode", "The input key for the Change Mode of the Building Manager (Build or Destroy)");
            m_KeyForForcedFallbackProperty = serializedObject.FindProperty("m_KeyForForcedFallback");
            m_KeyForForcedFallbackGUIContent = new GUIContent("Forced Fallback", "Activate or deactivate the Forced Fallback feature.");

            m_KeyForUndoProperty = serializedObject.FindProperty("m_KeyForUndo");
            m_KeyForUndoGUIContent = new GUIContent("History Undo", "The input key for the History Undo feature.");
            m_KeyForRedoProperty = serializedObject.FindProperty("m_KeyForRedo");
            m_KeyForRedoGUIContent = new GUIContent("History Redo", "The input key for the History Redo feature.");

            m_KeyForAlignRotationToGroundProperty = serializedObject.FindProperty("m_KeyForAlignRotationToGround");
            m_KeyForAlignRotationToGroundGUIContent = new GUIContent("Align Rotation To Ground", "The input key for the rotation alignment to the ground");

            m_KeyForAlignRotationToBuildingElementsProperty = serializedObject.FindProperty("m_KeyForAlignRotationToBuildingElements");
            m_KeyForAlignRotationToBuildingElementsGUIContent = new GUIContent("Align Rotation To BuildingElements", "The input key for the rotation alignment to the BuildingElements");

            //----------------------------------------------------------------------------------------------------------------------
            //Destroy
            m_DestroyTypeProperty = serializedObject.FindProperty("m_DestroyType");
            m_DestroyTypeGUIContent = new GUIContent("Destroy Type", "The type of the Destroy. " +
                "Instant means that the element is destroyed imedietly when the destroy is triggered. " +
                "Timer means that there is a time before the destroy. In case of the Timer the player should hold the destory button until the timer finished for finalize the destroy");
            m_DestroyTimerDurationProperty = serializedObject.FindProperty("m_DestroyTimerDuration");
            m_DestroyTimerDurationGUIContent = new GUIContent("Destroy Timer Duration", "How much time should be waiting befor the destory happened.");
            m_CutTimerOnLookAwayProperty = serializedObject.FindProperty("m_CutTimerOnLookAway");
            m_CutTimerOnLookAwayGUIContent = new GUIContent("Cut Timer On Look Away", "In case of timer setup if the player looks away from the element(s) " +
                "what should be destoryed the timer stoped and the elements will be not destroyed. " +
                "It is based oon the raycast. The palyer is lookin to the elements until the raycast hits any of the signed element. (Signed for destroy)");
            m_MaximumDestoryCountProperty = serializedObject.FindProperty("m_MaximumDestoryCount");
            m_MaximumDestoryCountGUIContent = new GUIContent("Maximum Destory Count", "How much element can be destroyed at the one time.");

            //----------------------------------------------------------------------------------------------------------------------
            //Action History
            m_IsHistoryEnabledProperty = serializedObject.FindProperty("m_IsHistoryEnabled");
            m_IsHistoryEnabledGUIContent = new GUIContent("Enable Action History", "Enable the Action History feature what can be used to undo or redo the actions of the BuildingManager.");
            m_HistoryActionCountProperty = serializedObject.FindProperty("m_HistoryActionCount");
            m_HistoryActionCountGUIContent = new GUIContent("History Action Count", "The maximum count of the saved actions what can used for undo.");
            m_PartialProcessingProperty = serializedObject.FindProperty("m_PartialProcessing");
            m_PartialProcessingGUIContent = new GUIContent("Partial Processing", "If it is true then the Undo, Redo processes can be done partially.");
            m_ClearHistoryInCaseOfErrorProperty = serializedObject.FindProperty("m_ClearHistoryInCaseOfError");
            m_ClearHistoryInCaseOfErrorGUIContent = new GUIContent("Clear History In Case Of Error", "Delete the whole history in case of error not just the wrong action.");

            //----------------------------------------------------------------------------------------------------------------------
            //Statistics
            m_StatisticsEnableCounterProperty = serializedObject.FindProperty("m_StatisticsEnableCounter");
            m_StatisticsEnableCounterGUIContent = new GUIContent("Enable Statistics Counter", "Enable the statistics measurments.");
            m_StatisticsBuilderCounterProperty = serializedObject.FindProperty("m_StatisticsBuilderCounter");
            m_StatisticsBuilderCounterGUIContent = new GUIContent("Builder Statistics Counter", "Enable the measurments of the builder algorithms too not just the manager's.");
        }

        protected override void OnInspectorGUIImpl()
        {
            m_TabView.Show(m_EditorStyleContainer);
        }

        protected override void OnHeaderGUIImpl(out string p_Title)
        {
            p_Title = "BuildingManager";
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Add Section Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private void AddSectionMetaDataProperties()
        {
            ABS_EditorUtils.StartDisableDuringGame();
            {
                ABS_EditorUtils.AddScriptableObjectPropertyWithCreate<ABS_BuildingElementList>(
                    ref m_ElementListProperty,
                    m_ElementListGUIContent,
                    m_EditorStyleContainer.SmallDarkButtonStyle,
                    "Missing BuildingElement List!",
                    "Save BuildingElement List",
                    "NewBuildingElementList");

                ABS_EditorUtils.AddPropertyField(m_BuildingParentProperty, m_BuildingParentGUIContent);

                if (m_BuildingParentProperty.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Missing BuildingParent", MessageType.Error);
                }

                ABS_EditorUtils.AddPropertyField(m_ObjectPoolProperty, m_ObjectPoolGUIContent);

                ABS_EditorUtils.Space();
                ABS_EditorUtils.AddPropertyField(m_TrackerObjectProperty, m_TrackerObjectGUIContent);
                for (int i = 0; i < m_TrackerObjectProperty.arraySize; i++)
                {
                    SerializedProperty element = m_TrackerObjectProperty.GetArrayElementAtIndex(i);
                    GameObject referencedObject = element.objectReferenceValue as GameObject;

                    if (referencedObject != null && referencedObject.transform.GetComponent<ABS_BuildingManagerTracker>() == null)
                    {
                        EditorGUILayout.HelpBox($"The following GameObject doesn't has a BuildingManagerTracker script as component : {referencedObject.name}", MessageType.Error);
                    }
                    }
                }
            ABS_EditorUtils.EndDisableDuringGame();
        }

        private void AddSectionBuildingBehaviourProperties()
        {
            ABS_EditorUtils.AddPropertyField(m_SandboxProperty, m_SandboxGUIContent);
            ABS_EditorUtils.StartDisableDuringGame();
            {
                ABS_EditorUtils.AddPropertyField(m_EnableCacheProperty, m_EnableCacheGUIContent);
            }
            ABS_EditorUtils.EndDisableDuringGame();
            ABS_EditorUtils.Space();
            ABS_EditorUtils.AddPropertyField(m_SearchPositionOnResetProperty, m_SearchPositionOnResetGUIContent);
            ABS_EditorUtils.Space();
            ABS_EditorUtils.AddPropertyField(m_EnableForcedFallbackProperty, m_EnableForcedFallbackGUIContent);
            if (m_EnableForcedFallbackProperty.boolValue)
            {
                ABS_EditorUtils.AddPropertyField(m_ForcedFallbackResetOnElementPlaceProperty, m_ForcedFallbackResetOnElementPlaceGUIContent);
            }
        }

        private void AddSectionRaycastProperties()
        {
            ABS_EditorUtils.AddPropertyField(m_RaycastModeProperty, m_RaycastModeGUIContent);
            if (m_RaycastModeProperty.enumValueIndex == (int)ABS_RaycastMode.Camera)
            {
                ABS_EditorUtils.AddPropertyField(m_CameraProperty, m_CameraGUIContent);
                if (m_CameraProperty.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Missing Camera!", MessageType.Error);
                }
            }

            ABS_EditorUtils.AddScriptableObjectPropertyWithCreate<ABS_LayerCollection>(
                ref m_LayerCollectionProperty,
                m_LayerCollectionGUIContent,
                m_EditorStyleContainer.SmallDarkButtonStyle,
                "Missing Layer Collection!",
                "Save Layer Collection",
                "NewLayerCollection");

            if (m_LayerCollectionProperty.objectReferenceValue != null)
            {
                ABS_LayerCollectionEditor.DrawSettingsDetails(
                    m_EditorStyleContainer,
                    ref m_LayerCollectionDetailsSectionVariable,
                    m_LayerCollectionProperty.objectReferenceValue as ABS_LayerCollection);
            }

            ABS_EditorUtils.AddPropertyField(m_RaycastDistanceProperty, m_RaycastDistanceGUIContent);
            ABS_EditorUtils.AddPropertyField(m_RaycastOffsetProperty, m_RaycastOffsetGUIContent);

        }

        private void AddSectionInputProperties ()
        {
            ABS_EditorUtils.AddPropertyField(m_InputTypeProperty, m_InputTypeGUIContent);
            if (m_InputTypeProperty.enumValueIndex == (int)ABS_InputType.Key
                || m_InputTypeProperty.enumValueIndex == (int)ABS_InputType.Both)
            {
                ABS_EditorUtils.Space(10);
                EditorGUILayout.LabelField("Key Input Settings", m_EditorStyleContainer.HeadStyleSectionGroup);
                ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
                AddKeySettings();
                ABS_EditorUtils.BoxEnd();
            }
        }

        private void AddKeySettings()
        {
            ABS_EditorUtils.AddPropertyField(m_RotationInputTypeProperty, m_RotationInputTypeGUIContent);
            if (m_RotationInputTypeProperty.enumValueIndex == (int)ABS_RotationInputType.Button)
            {
                ABS_EditorUtils.Space();
                ABS_EditorUtils.AddPropertyField(m_KeyBasedRotationCycleTimeProperty, m_KeyBasedRotationCycleTimeGUIContent);
                ABS_EditorUtils.Space();
                ABS_EditorUtils.AddPropertyField(m_KeyForRotationRightProperty, m_KeyForRotationRightGUIContent);
                ABS_EditorUtils.AddPropertyField(m_KeyForRotationLeftProperty, m_KeyForRotationLeftGUIContent);
            }

            ABS_EditorUtils.AddSeparatorLine();

            ABS_EditorUtils.AddPropertyField(m_KeyForBuildProperty, m_KeyForBuildGUIContent);
            ABS_EditorUtils.AddPropertyField(m_KeyForDestroyProperty, m_KeyForDestroyGUIContent);
            ABS_EditorUtils.AddPropertyField(m_KeyForDragBuildProperty, m_KeyForDragBuildGUIContent);
            ABS_EditorUtils.AddPropertyField(m_KeyForDragDestroyProperty, m_KeyForDragDestroyGUIContent);
            ABS_EditorUtils.Space();
            ABS_EditorUtils.AddPropertyField(m_KeyForModeChangeProperty, m_KeyForModeChangeGUIContent);
            ABS_EditorUtils.AddPropertyField(m_KeyForForcedFallbackProperty, m_KeyForForcedFallbackGUIContent);
            ABS_EditorUtils.Space();
            ABS_EditorUtils.AddPropertyField(m_KeyForUndoProperty, m_KeyForUndoGUIContent);
            ABS_EditorUtils.AddPropertyField(m_KeyForRedoProperty, m_KeyForRedoGUIContent);
            ABS_EditorUtils.Space();
            ABS_EditorUtils.AddPropertyField(m_KeyForAlignRotationToGroundProperty, m_KeyForAlignRotationToGroundGUIContent);
            ABS_EditorUtils.AddPropertyField(m_KeyForAlignRotationToBuildingElementsProperty, m_KeyForAlignRotationToBuildingElementsGUIContent);
        }
        private void AddDestroyProperties()
        {
            ABS_EditorUtils.AddPropertyField(m_DestroyTypeProperty, m_DestroyTypeGUIContent);
            if (m_DestroyTypeProperty.enumValueIndex == (int)DestroyType.Timer)
            {
                ABS_EditorUtils.AddPropertyField(m_DestroyTimerDurationProperty, m_DestroyTimerDurationGUIContent);
                ABS_EditorUtils.AddPropertyField(m_CutTimerOnLookAwayProperty, m_CutTimerOnLookAwayGUIContent);
            }
            ABS_EditorUtils.AddPropertyField(m_MaximumDestoryCountProperty, m_MaximumDestoryCountGUIContent);
        }
        
        private void AddHistoryProperties()
        {
            ABS_EditorUtils.AddPropertyField(m_IsHistoryEnabledProperty, m_IsHistoryEnabledGUIContent);
            if (m_IsHistoryEnabledProperty.boolValue)
            {
                ABS_EditorUtils.StartDisableDuringGame();
                {
                    ABS_EditorUtils.AddPropertyField(m_HistoryActionCountProperty, m_HistoryActionCountGUIContent);
                    ABS_EditorUtils.Space();
                    ABS_EditorUtils.AddPropertyField(m_PartialProcessingProperty, m_PartialProcessingGUIContent);
                    ABS_EditorUtils.AddPropertyField(m_ClearHistoryInCaseOfErrorProperty, m_ClearHistoryInCaseOfErrorGUIContent);
                }
                ABS_EditorUtils.EndDisableDuringGame();
            }
        }

        private void AddSectionStatisticsProperties()
        {
            ABS_EditorUtils.StartDisableDuringGame();
            {
                ABS_EditorUtils.AddPropertyField(m_StatisticsEnableCounterProperty, m_StatisticsEnableCounterGUIContent);
                if (m_StatisticsEnableCounterProperty.boolValue)
                {
                    ABS_EditorUtils.AddPropertyField(m_StatisticsBuilderCounterProperty, m_StatisticsBuilderCounterGUIContent);
                }
            }
            ABS_EditorUtils.EndDisableDuringGame();
        }

        public static ABS_BuildingManager CreateBuildingManager()
        {
            GameObject buildingManager = new GameObject("ABS_BuildingManager");
            ABS_BuildingManager manager = buildingManager.AddComponent<ABS_BuildingManager>();

            ABS_EditorUtils.Dirty(manager);

            return manager;
        }

    }
}