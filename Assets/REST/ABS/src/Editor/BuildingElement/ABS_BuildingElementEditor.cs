//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;
using UnityEditor;

//  Dependencies: REST
using REST.Utils;

//*********************************************************************


namespace REST.AdvancedBuildSystem.Editor
{
    [CustomEditor(typeof(ABS_BuildingElement))]
    [CanEditMultipleObjects]
    internal class ABS_BuildingElementEditor : ABS_EditorBase
    {
        //MetaData
        private SerializedProperty m_PrefabGuidProperty;
        private GUIContent m_PrefabGuidGUIContent;
        private SerializedProperty m_InstanceGuidProperty;
        private GUIContent m_InstanceGuidGUIContent;
        private SerializedProperty m_FixedInstanceGuidProperty;
        private GUIContent m_FixedInstanceGuidGUIContent;
        private SerializedProperty m_FinalElementProperty;
        private GUIContent m_FinalElementGUIContent;

        //Proeprties
        private SerializedProperty m_SnapToPreBuiltFinalElementProperty;
        private GUIContent m_SnapToPreBuiltFinalElementGUIContent;
        private SerializedProperty m_ShouldSnapToFoundationProperty;
        private GUIContent m_ShouldSnapToFoundationGUIContent;
        private SerializedProperty m_PreBuiltProperty;
        private GUIContent m_PreBuiltGUIContent;
        private SerializedProperty m_FoundationProperty;
        private GUIContent m_FoundationGUIContent;
        private SerializedProperty m_AreaTypeProperty;
        private GUIContent m_AreaTypeGUIContent;
        private SerializedProperty m_ShouldAllowedByAreaProperty;
        private GUIContent m_ShouldAllowedByAreaGUIContent;
        private SerializedProperty m_ShouldOverrideProperty;
        private GUIContent m_ShouldOverrideGUIContent;
        private SerializedProperty m_IndestructibleProperty;
        private GUIContent m_IndestructibleGUIContent;
        private SerializedProperty m_CanNotBeAttachTargetProperty;
        private GUIContent m_CanNotBeAttachTargetGUIContent;

        //Build Collider
        private SerializedProperty m_BuildingElementDimensionTypeProperty;
        private GUIContent m_BuildingElementDimensionTypeGUIContent;
        private SerializedProperty m_DimensionProperty;
        private GUIContent m_DimensionGUIContent;
        private SerializedProperty m_DimensionColliderProperty;
        private GUIContent m_DimensionColliderGUIContent;

        //Highlight
        private SerializedProperty m_HighlightCollectionProperty;
        private GUIContent m_HighlightCollectionGUIContent;
        private SerializedProperty m_HighlightStrategyProperty;
        private GUIContent m_HighlightStrategyGUIContent;
        private SerializedProperty m_RenderersProperty;
        private GUIContent m_RendererGUIContent;

        //Drag building
        private SerializedProperty m_DragBuildingEnabledProperty;
        private GUIContent m_DragBuildingEnabledGUIContent;
        private SerializedProperty m_DragBuildingBehaviourProperty;
        private GUIContent m_DragBuildingBehaviourGUIContent;
        private SerializedProperty m_EnabledDragBuildingXProperty;
        private GUIContent m_EnabledDragBuildingXGUIContent;
        private SerializedProperty m_EnabledDragBuildingZProperty;
        private GUIContent m_EnabledDragBuildingZGUIContent;

        //Algorithm properties
        private SerializedProperty m_PositionSearchAlgorithmProperty;
        private GUIContent m_PositionSearchAlgorithmGUIContent;
        private SerializedProperty m_PositionAlgorithmSettingsProperty;
        private GUIContent m_PositionAlgorithmSettingsGUIContent;

        //AdvancedGrid
        private SerializedProperty m_SnapPointRuleSetProperty;
        private GUIContent m_SnapPointRuleSetGUIContent;
        private SerializedProperty m_AdvancedGridTypeProperty;
        private GUIContent m_AdvancedGridTypeGUIContent;
        private SerializedProperty m_AdvancedGridAxisTypeProperty;
        private GUIContent m_AdvancedGridAxisTypeGUIContent;
        private SerializedProperty m_AllowMixedAxisDragBuildingProperty;
        private GUIContent m_AllowMixedAxisDragBuildingGUIContent;
        private SerializedProperty m_StableElementProperty;
        private GUIContent m_StableElementGUIContent;

        //SnapPointBased
        private SerializedProperty m_MeshProperty;
        private GUIContent m_MeshGUIContent;
        private SerializedProperty m_SnapPointTypeProperty;
        private GUIContent m_SnapPointTypeGUIContent;

        //FreeBuilding
        private SerializedProperty m_ShouldAttachedProperty;
        private GUIContent m_ShouldAttachedGUIContent;
        

        //other
        private bool m_ColliderCollectionError = false;
        private string m_ColliderCollectionErrorMessage = string.Empty;
        private bool m_ParentError = false;
        private string m_ParentErrorMessage = string.Empty;

        private ABS_EditorTabView m_TabView = null;

        private bool m_HighlightCollectionDetailsSectionVariable = false;
        private bool m_SelectedElements_AdvancedGrid_SectionVariable = false;
        private bool m_SelectedElements_BasicGrid_SectionVariable = false;
        private bool m_SelectedElements_Free_SectionVariable = false;
        private bool m_SelectedElements_SnapPoint_SectionVariable = false;


        public ABS_BuildingElementEditor () : base()
        {
            m_TabView = new ABS_EditorTabView(3);
            m_TabView.AddCallback("MetaData", ShowMetaDataView);
            m_TabView.AddCallback("Properties", ShowPropertiesView);
            m_TabView.AddCallback("Algorithm Properties", ShowAlgorithmSpecificView);
            m_TabView.AddCallback("Collider", ShowColliderView);
            m_TabView.AddCallback("Highlight", ShowHighlightView);
        }

        protected override void OnEnableImpl()
        {
            m_PositionSearchAlgorithmProperty = serializedObject.FindProperty("m_PositionSearchAlgorithm");
            m_PositionSearchAlgorithmGUIContent = new GUIContent("Position Search Algorithm", "The algorithm used for positioning");
            m_PositionAlgorithmSettingsProperty = serializedObject.FindProperty("m_PositionAlgorithmSettings");
            m_PositionAlgorithmSettingsGUIContent = new GUIContent("Position Algorithm Settings", "The settings scripable object for the algorithm");

            m_PrefabGuidProperty = serializedObject.FindProperty("m_PrefabGuid");
            m_PrefabGuidGUIContent = new GUIContent("Prefab Guid",
                "A unique identifier of the prefab. Two BuildingElemet are equal from the POV of the AdvancedBuildingSystem if their Prefab Guids are equal.");
            m_InstanceGuidProperty = serializedObject.FindProperty("m_InstanceGuid");
            m_InstanceGuidGUIContent = new GUIContent("Instance Guid",
                "A unique identifier of the BuildingElement's instance.");
            m_FixedInstanceGuidProperty = serializedObject.FindProperty("m_FixedInstanceGuid");
            m_FixedInstanceGuidGUIContent = new GUIContent("Fixed InstanceGuid",
                "If the booldean is true then the InstanceGuide will be fixed and not generated." +
                " If the boolean is false then the IntanceGuide Will be generated.");
            m_FinalElementProperty = serializedObject.FindProperty("m_FinalElement");
            m_FinalElementGUIContent = new GUIContent("Final Element", 
                "The Final Element of this BuildingElement will be placed in the scene " +
                "at the finalizing of the building process instead of the current BuildingElement");


            m_SnapToPreBuiltFinalElementProperty = serializedObject.FindProperty("m_SnapToPreBuiltFinalElement");
            m_SnapToPreBuiltFinalElementGUIContent = new GUIContent("Snap To PreBuilt Final Element",
                "Usually an element can only snap into a PreBuiltElement if it is the same element (the Guidss are equal of the two element). " +
                "If this boolean is true the algorithm allow for this BuildingElement to snap into the Final Element's PreBuilt elements.");
            m_ShouldSnapToFoundationProperty = serializedObject.FindProperty("m_ShouldSnapToFoundation");
            m_ShouldSnapToFoundationGUIContent = new GUIContent("Should Snap To Foundation",
                "The placement of the element is allowed only if it is snapping to a Foundation element");

            m_PreBuiltProperty = serializedObject.FindProperty("m_PreBuilt");
            m_PreBuiltGUIContent = new GUIContent("Pre Built", "If the boolean is true the BuildingElement is a PreBuilt element. " +
                "Which means that during the building process a BuildingElement will snap into an anotherone's position if it is a prebuilt element. " +
                "An element can snap into a PreBuilt element if their Guids are equal. The PreBuilt itself will be deleted if an another element snaped into it.");
            m_FoundationProperty = serializedObject.FindProperty("m_Foundation");
            m_FoundationGUIContent = new GUIContent("Foundation", "If the boolean is true than the element is a Foundation. " +
                "If the BuildingManager is using Foundation Logic then the element can be only built if it is a Foundation or " +
                "it is snapping to an another element.");


            m_AreaTypeProperty = serializedObject.FindProperty("m_AreaType");
            m_AreaTypeGUIContent = new GUIContent("Area Type", "The type of the element what used by the BuildingAreas to allow or deny an element.");
            m_ShouldAllowedByAreaProperty = serializedObject.FindProperty("m_ShouldAllowedByArea");
            m_ShouldAllowedByAreaGUIContent = new GUIContent("Should Allowed By Area", "If the bool is true then the element can be only built " +
                "when a BuildingArea allowed it. Every other position is blocked even if they are valid positions.");

            m_ShouldOverrideProperty = serializedObject.FindProperty("m_ShouldOverride");
            m_ShouldOverrideGUIContent = new GUIContent("Should Override", "If the bool is true then the element is always blocked if it doesn't override an another element.");

            m_IndestructibleProperty = serializedObject.FindProperty("m_Indestructible");
            m_IndestructibleGUIContent = new GUIContent("Indestructible", "If the bool is true then the element can not be destroyed by the Manager");

            m_CanNotBeAttachTargetProperty = serializedObject.FindProperty("m_CanNotBeAttachTarget");
            m_CanNotBeAttachTargetGUIContent = new GUIContent("Can Not Be Attach Target", "If the bool is true then the element can not be attach connection target");

            //Collider
            m_BuildingElementDimensionTypeProperty = serializedObject.FindProperty("m_BuildingElementDimensionType");
            m_BuildingElementDimensionTypeGUIContent = new GUIContent("BuildingElement Dimension Type", "What is the source of the dimension.");
            m_DimensionProperty = serializedObject.FindProperty("m_Dimension");
            m_DimensionGUIContent = new GUIContent("Dimension", "The dimension/size of the BuildingElement what is used during the calcualtions");
            m_DimensionColliderProperty = serializedObject.FindProperty("m_DimensionCollider");
            m_DimensionColliderGUIContent = new GUIContent("Dimension Collider", "A Box Collider what's size will be used during the buidling process.");

            //Drag building
            m_DragBuildingEnabledProperty = serializedObject.FindProperty("m_DragBuildingEnabled");
            m_DragBuildingEnabledGUIContent = new GUIContent("Drag Building Enabled", "Enable or disable the drag building for the elements with this MetaData");
            m_DragBuildingBehaviourProperty = serializedObject.FindProperty("m_DragBuildingBehaviour");
            m_DragBuildingBehaviourGUIContent = new GUIContent("Drag Building Behaviour", "The shape of the dragbuilding behaviour.");
            m_EnabledDragBuildingXProperty = serializedObject.FindProperty("m_EnabledDragBuildingX");
            m_EnabledDragBuildingXGUIContent = new GUIContent("Enabled Drag Building On X Axis", "Enable or disable the drag building for the elements with this MetaData On the X ayis");
            m_EnabledDragBuildingZProperty = serializedObject.FindProperty("m_EnabledDragBuildingZ");
            m_EnabledDragBuildingZGUIContent = new GUIContent("Enabled Drag Building On Z Axis", "Enable or disable the drag building for the elements with this MetaData On the Z ayis");

            //Highlight
            m_HighlightCollectionProperty = serializedObject.FindProperty("m_HighlightCollection");
            m_HighlightCollectionGUIContent = new GUIContent("Highlight Collection", "The HighlightCollection Scriptable object what containing the materials used for the Highlight feature");
            m_HighlightStrategyProperty = serializedObject.FindProperty("m_HighlightStrategy");
            m_HighlightStrategyGUIContent = new GUIContent("Highlight Strategy", "The strategy of the Highlight logic. " +
                "So how should the Highlight feature collect the renderers of the BuildingElement. " +
                "The None turn of totally the Highlight feature.");
            m_RenderersProperty = serializedObject.FindProperty("m_Renderers");
            m_RendererGUIContent = new GUIContent("Renderers", "The List of the Renderers what affected by the Highlight feature.");

            //AdvancedGrid
            m_SnapPointRuleSetProperty = serializedObject.FindProperty("m_SnapPointRuleSet");
            m_SnapPointRuleSetGUIContent = new GUIContent("SnapPoint RuleSet", "SnapPoint RuleSet what can tell that what SnapPoint can be used for snapping for that element.");
            m_AdvancedGridTypeProperty = serializedObject.FindProperty("m_AdvancedGridType");
            m_AdvancedGridTypeGUIContent = new GUIContent("Advanced Grid Type", "The type of the element what used for figuring out where should that element snap on the building.");
            m_AdvancedGridAxisTypeProperty = serializedObject.FindProperty("m_AdvancedGridAxisType");
            m_AdvancedGridAxisTypeGUIContent = new GUIContent("Advanced Grid Axis Type", "To which axes can an element be snapped? It can be X, Z or Both");
            m_AllowMixedAxisDragBuildingProperty = serializedObject.FindProperty("m_AllowMixedAxisDragBuilding");
            m_AllowMixedAxisDragBuildingGUIContent = new GUIContent("Allow Mixed Axis Drag Building", "Allow the mixing of the two axis positioning for horizontal and the wall type objects during drag buidling");
            m_StableElementProperty = serializedObject.FindProperty("m_StableElement");
            m_StableElementGUIContent = new GUIContent("Stable Element", "This element has always maximum stability");

            //SnapPointBased
            m_MeshProperty = serializedObject.FindProperty("m_Mesh");
            m_MeshGUIContent = new GUIContent("Mesh", "The Mesh used for the Collision Check");
            m_SnapPointTypeProperty = serializedObject.FindProperty("m_SnapPointType");
            m_SnapPointTypeGUIContent = new GUIContent("SnapPoint Type", "The type of the element what used for figuring out where should that element snap on the building.");

            //FreeBuilding
            m_ShouldAttachedProperty = serializedObject.FindProperty("m_ShouldAttached");
            m_ShouldAttachedGUIContent = new GUIContent("Should Attached", 
                "The element is only allowed to built when it can be connected to an another element by attachment connection");
        }

        protected override void OnInspectorGUIImpl()
        {
            m_TabView.Show(m_EditorStyleContainer);
        }

        private void ShowMetaDataView()
        {
            List<ABS_BuildingElement> targets = GetSelectedTargetsComponents<ABS_BuildingElement>();

            if (targets.Count == 1 && EditorApplication.isPlaying)
            {
                if (targets[0].ParentBuilding == null)
                {
                    EditorGUILayout.LabelField("Parent: NULL");
                    EditorGUILayout.HelpBox("Missing Parent Object", MessageType.Error);
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Parent: ", GUILayout.Width(50));
                        ABS_EditorUtils.AddObjectLinkLabel(targets[0].ParentBuilding, 100);
                        GUILayout.FlexibleSpace();
                    }
                    GUILayout.EndHorizontal();
                }
            }

            ABS_EditorUtils.AddGuidFieldWithCreateButton<ABS_BuildingElement>(
                m_EditorStyleContainer.SmallDarkButtonStyle,
                "New Prefab Guid",
                m_PrefabGuidProperty,
                m_PrefabGuidGUIContent,
                targets,
                GuidSetterForPrefab);


            ABS_EditorUtils.AddPropertyField(m_FixedInstanceGuidProperty, m_FixedInstanceGuidGUIContent);

            if (m_FixedInstanceGuidProperty.boolValue)
            {
                ABS_EditorUtils.AddGuidFieldWithCreateButton<ABS_BuildingElement>(
                    m_EditorStyleContainer.SmallDarkButtonStyle,
                    "New Instance Guid",
                    m_InstanceGuidProperty,
                    m_InstanceGuidGUIContent,
                    targets,
                    GuidSetterForInstance);
            }

            ABS_EditorUtils.Space();
            ABS_EditorUtils.AddPropertyField(m_FinalElementProperty, m_FinalElementGUIContent);
        }

        private void AddAdvancedGridBuildingSettingsDetails(ABS_AdvancedGridBuilderSettings p_Settings)
        {
            if (p_Settings != null)
            {
                ABS_AdvancedGridBuilderSettingsEditor.DrawSettingsDetails(m_EditorStyleContainer, p_Settings);
            }
            else
            {
                EditorGUILayout.HelpBox("Wrong Type of Settings!", MessageType.Error);
            }
        }

        private void AddSnapPointBasedBuildingSettingsDetails(ABS_SnapPointBasedBuilderSettings p_Settings)
        {
            if (p_Settings != null)
            {
                ABS_SnapPointBasedBuilderSettingsEditor.DrawSettingsDetails(m_EditorStyleContainer, p_Settings);
            }
            else
            {
                EditorGUILayout.HelpBox("Wrong Type of Settings!", MessageType.Error);
            }
        }

        private void AddFreeBuilderSettingsDetails(ABS_FreeBuilderSettings p_Settings)
        {
            if (p_Settings != null)
            {
                ABS_FreeBuilderSettingsEditor.DrawSettingsDetails(m_EditorStyleContainer, p_Settings);
            }
            else
            {
                EditorGUILayout.HelpBox("Wrong Type of Settings!", MessageType.Error);
            }
        }

        private void AddBasicGridBuilderSettingsDetails(ABS_BasicGridBuilderSettings p_Settings)
        {
            if (p_Settings != null)
            {
                ABS_BasicGridBuilderSettingsEditor.DrawSettingsDetails(m_EditorStyleContainer, p_Settings);
            }
            else
            {
                EditorGUILayout.HelpBox("Wrong Type of Settings!", MessageType.Error);
            }
        }
        private void GuidSetterForPrefab(string p_Guid, ABS_BuildingElement p_Target)
        {
            p_Target.PrefabGuid = p_Guid;
            REST_Logging.Info("ABS_BuildingElementEditor", $"New Prefab Guid for {target.name} : {p_Guid}");
        }
        private void GuidSetterForInstance(string p_Guid, ABS_BuildingElement p_Target)
        {
            p_Target.InstanceGuid = p_Guid;
            REST_Logging.Info("ABS_BuildingElementEditor", $"New Instance Guid for {target.name} : {p_Guid}");
        }

        private void ShowPropertiesView()
        {
            EditorGUILayout.LabelField("Basics", m_EditorStyleContainer.HeadStyleSectionGroup);
            ABS_EditorUtils.Space(3);
            ABS_EditorUtils.AddPropertyField(m_PreBuiltProperty, m_PreBuiltGUIContent);
            ABS_EditorUtils.AddPropertyField(m_FoundationProperty, m_FoundationGUIContent);
            ABS_EditorUtils.AddPropertyField(m_IndestructibleProperty, m_IndestructibleGUIContent);
            ABS_EditorUtils.AddPropertyField(m_CanNotBeAttachTargetProperty, m_CanNotBeAttachTargetGUIContent);
            ABS_EditorUtils.AddPropertyField(m_AreaTypeProperty, m_AreaTypeGUIContent);

            ABS_EditorUtils.AddSeparatorLine();
            EditorGUILayout.LabelField("Behaviour", m_EditorStyleContainer.HeadStyleSectionGroup);
            ABS_EditorUtils.Space(3);
            ABS_EditorUtils.AddPropertyField(m_SnapToPreBuiltFinalElementProperty, m_SnapToPreBuiltFinalElementGUIContent);
            ABS_EditorUtils.AddPropertyField(m_ShouldSnapToFoundationProperty, m_ShouldSnapToFoundationGUIContent);
            ABS_EditorUtils.AddPropertyField(m_ShouldAllowedByAreaProperty, m_ShouldAllowedByAreaGUIContent);
            ABS_EditorUtils.AddPropertyField(m_ShouldOverrideProperty, m_ShouldOverrideGUIContent);

            ABS_EditorUtils.AddSeparatorLine();
            EditorGUILayout.LabelField("DragBuilding", m_EditorStyleContainer.HeadStyleSectionGroup);
            ABS_EditorUtils.Space(3);
            ABS_EditorUtils.AddPropertyField(m_DragBuildingEnabledProperty, m_DragBuildingEnabledGUIContent);
            if (m_DragBuildingEnabledProperty.boolValue)
            {
                ABS_EditorUtils.AddPropertyField(m_DragBuildingBehaviourProperty, m_DragBuildingBehaviourGUIContent);
                ABS_EditorUtils.AddPropertyField(m_EnabledDragBuildingXProperty, m_EnabledDragBuildingXGUIContent);
                ABS_EditorUtils.AddPropertyField(m_EnabledDragBuildingZProperty, m_EnabledDragBuildingZGUIContent);
            }
        }

        private void ShowHighlightView()
        {
            ABS_EditorUtils.AddScriptableObjectPropertyWithCreate<ABS_BuildingElementHighlightCollection>(
                ref m_HighlightCollectionProperty,
                m_HighlightCollectionGUIContent,
                m_EditorStyleContainer.SmallDarkButtonStyle,
                "Missing Highlight Collection!",
                "Save Highlight Collection",
                "NewHighlightCollection");

            if (m_HighlightCollectionProperty.objectReferenceValue != null)
            {
                m_HighlightCollectionDetailsSectionVariable = EditorGUILayout.BeginFoldoutHeaderGroup(m_HighlightCollectionDetailsSectionVariable, "Details");
                if (m_HighlightCollectionDetailsSectionVariable)
                {
                    ABS_BuildingElementHighlightCollectionEditor.DrawDetails(m_HighlightCollectionProperty.objectReferenceValue as ABS_BuildingElementHighlightCollection);
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
               
            ABS_EditorUtils.Space();

            ABS_EditorUtils.AddPropertyField(m_HighlightStrategyProperty, m_HighlightStrategyGUIContent);
            if (m_HighlightStrategyProperty.enumValueIndex == (int)ABS_HighlightStrategy.Custom)
            {
                ABS_EditorUtils.AddPropertyField(m_RenderersProperty, m_RendererGUIContent);

                GUILayout.BeginHorizontal();
                {
                    bool buttonResult = GUILayout.Button("CollectRenderers");
                    if (buttonResult)
                    {
                        CollectRenderers();
                    }
                }
                GUILayout.EndHorizontal();
            }
        }

        private void ShowColliderView ()
        {
            List<ABS_BuildingElement> targets = GetSelectedTargetsComponents<ABS_BuildingElement>();
            ABS_EditorUtils.AddPropertyField(m_BuildingElementDimensionTypeProperty, m_BuildingElementDimensionTypeGUIContent);
            if ((int)m_BuildingElementDimensionTypeProperty.enumValueFlag == (int)ABS_BuildingElementDimensionType.Fixed)
            {
                ABS_EditorUtils.AddPropertyField(m_DimensionProperty, m_DimensionGUIContent);
            }
            else
            {
                if (targets.Count > 1)
                {
                    bool buttonResult = GUILayout.Button("Find All collider", m_EditorStyleContainer.SmallDarkButtonStyle);
                    if (buttonResult)
                    {
                        CollectBuildCollider();
                    }
                }
                else
                {
                    ABS_EditorUtilsSpecial.AddPropertyFieldWithCustomButton(
                        ref m_DimensionColliderProperty,
                        m_DimensionColliderGUIContent,
                        CollectBuildCollider,
                        m_EditorStyleContainer.SmallDarkButtonStyle,
                        "Find",
                        80);

                    if (m_ColliderCollectionError)
                    {
                        ABS_EditorUtils.HelpBox(MessageType.Warning, m_ColliderCollectionErrorMessage);
                    }
                    else if (m_DimensionColliderProperty.objectReferenceValue != null)
                    {
                        GUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField($"Collider dimension: {((BoxCollider)m_DimensionColliderProperty.objectReferenceValue).size}");

                            bool buttonResult = GUILayout.Button("Convert to Fixed", m_EditorStyleContainer.SmallDarkButtonStyle, GUILayout.Width(150));
                            if (buttonResult)
                            {
                                m_BuildingElementDimensionTypeProperty.enumValueFlag = (int)ABS_BuildingElementDimensionType.Fixed;
                                m_DimensionProperty.vector3Value = ((BoxCollider)m_DimensionColliderProperty.objectReferenceValue).size;
                                ABS_EditorUtils.Dirty(m_TargetSerializedObject.targetObject);
                            }

                            ABS_EditorUtils.FlexibleSpace();
                        }
                        GUILayout.EndHorizontal();
                    }
                }
            }

            ABS_EditorUtils.AddSeparatorLine();

            List<ABS_BuildingElement> advancedGridElements = new List<ABS_BuildingElement>();
            List<ABS_BuildingElement> basicGridElements = new List<ABS_BuildingElement>();
            List<ABS_BuildingElement> freeElements = new List<ABS_BuildingElement>();
            List<ABS_BuildingElement> snappointBasedElements = new List<ABS_BuildingElement>();
            SortTargetElements(targets, advancedGridElements, basicGridElements, freeElements, snappointBasedElements);

            GUILayout.BeginHorizontal();
            {
                bool buttonResult = GUILayout.Button(targets.Count > 1 ? "Refresh Links" : "Refresh Link", m_EditorStyleContainer.SmallDarkButtonStyle);
                if (buttonResult)
                {
                    foreach (ABS_BuildingElement target in targets)
                    {
                        target.RefreshLink();
                        ABS_EditorUtils.Dirty(target);
                    }
                }

#if ABS_ENABLE_NAVMESH
                buttonResult = GUILayout.Button(targets.Count > 1 ? "Refresh NavMeshes" : "Refresh NavMesh", m_EditorStyleContainer.SmallDarkButtonStyle);
                if (buttonResult)
                {
                    foreach (ABS_BuildingElement target in targets)
                    {
                        target.RefreshNavMeshSize();
                    }
                }
#endif
            }
            GUILayout.EndHorizontal();
        }

        private void CollectBuildCollider ()
        {
            List<ABS_BuildingElement> targets = GetSelectedTargetsComponents<ABS_BuildingElement>();
            if (targets.Count == 1)
            {
                BoxCollider[] collider = targets[0].transform.GetComponents<BoxCollider>();
                if (collider == null || collider.Length == 0)
                {
                    m_ColliderCollectionError = true;
                    m_ColliderCollectionErrorMessage = "No BoxCollider has been found!";
                    m_DimensionColliderProperty.objectReferenceValue = null;
                    ABS_EditorUtils.Dirty(m_DimensionColliderProperty.serializedObject.targetObject);
                }
                else if (collider.Length > 1)
                {
                    m_ColliderCollectionError = true;
                    m_ColliderCollectionErrorMessage = "Multiple Box collider has been found";
                    m_DimensionColliderProperty.objectReferenceValue = null;
                    ABS_EditorUtils.Dirty(m_DimensionColliderProperty.serializedObject.targetObject);
                }
                else
                {
                    m_ColliderCollectionError = false;
                    m_DimensionColliderProperty.objectReferenceValue = collider[0];
                    ABS_EditorUtils.Dirty(m_DimensionColliderProperty.serializedObject.targetObject);
                }
            }
            else
            {
                m_ColliderCollectionError = false;
                foreach (var target in targets)
                {
                    BoxCollider[] collider = target.transform.GetComponents<BoxCollider>();
                    if (collider == null || collider.Length == 0)
                    {
                        REST_Logging.Debug("ABS_BuildingElementEditor", $"No BoxCollider has been found for element: {target.name}");
                        target.DimensionCollider = null;
                        ABS_EditorUtils.Dirty(target);
                    }
                    else if (collider.Length > 1)
                    {
                        REST_Logging.Debug("ABS_BuildingElementEditor", $"Multiple Box collider has been found for element: {target.name}");
                        target.DimensionCollider = null;
                        ABS_EditorUtils.Dirty(target);
                    }
                    else
                    {
                        REST_Logging.Debug("ABS_BuildingElementEditor", $"Collider has successfully found for element: {target.name}");
                        target.DimensionCollider = collider[0];
                        ABS_EditorUtils.Dirty(target);
                    }
                }
            }
        }

        private void CollectRenderers ()
        {
            foreach (ABS_BuildingElement target in GetSelectedTargetsComponents<ABS_BuildingElement>())
            {
                CollectRenderers(target);
            }
        }

        public static void CollectRenderers (ABS_BuildingElement p_Target)
        {
            List<Renderer> renderers = new List<Renderer>();
            renderers.AddRange(p_Target.gameObject.GetComponentsInChildren<Renderer>(true));

            p_Target.Renderers = renderers;

            ABS_EditorUtils.Dirty(p_Target);
        }

        private void ShowAlgorithmSpecificView()
        {
            ABS_EditorUtils.StartDisableDuringGame();
            {
                ABS_EditorUtils.AddPropertyField(m_PositionSearchAlgorithmProperty, m_PositionSearchAlgorithmGUIContent);
                ABS_EditorUtils.AddPropertyField(m_PositionAlgorithmSettingsProperty, m_PositionAlgorithmSettingsGUIContent);
            }
            ABS_EditorUtils.EndDisableDuringGame();

            switch (m_PositionSearchAlgorithmProperty.enumValueIndex)
            {
                case (int)ABS_PositionSearchAlgorithm.AdvancedGrid:
                    AddAdvancedGridBuildingSettingsDetails(m_PositionAlgorithmSettingsProperty.objectReferenceValue as ABS_AdvancedGridBuilderSettings);
                    break;
                case (int)ABS_PositionSearchAlgorithm.SnapPointBased:
                    AddSnapPointBasedBuildingSettingsDetails(m_PositionAlgorithmSettingsProperty.objectReferenceValue as ABS_SnapPointBasedBuilderSettings);
                    break;
                case (int)ABS_PositionSearchAlgorithm.Free:
                    AddFreeBuilderSettingsDetails(m_PositionAlgorithmSettingsProperty.objectReferenceValue as ABS_FreeBuilderSettings);
                    break;
                case (int)ABS_PositionSearchAlgorithm.BasicGrid:
                    AddBasicGridBuilderSettingsDetails(m_PositionAlgorithmSettingsProperty.objectReferenceValue as ABS_BasicGridBuilderSettings);
                    break;
                default:
                    EditorGUILayout.HelpBox("Choose a valid Algorithm", MessageType.Error);
                    break;
            }

            ABS_EditorUtils.AddSeparatorLine();
            List<ABS_BuildingElement> targets = GetSelectedTargetsComponents<ABS_BuildingElement>();
            List<ABS_BuildingElement> advancedGridElements = new List<ABS_BuildingElement>();
            List<ABS_BuildingElement> basicGridElements = new List<ABS_BuildingElement>();
            List<ABS_BuildingElement> freeElements = new List<ABS_BuildingElement>();
            List<ABS_BuildingElement> snappointBasedElements = new List<ABS_BuildingElement>();
            SortTargetElements(targets, advancedGridElements, basicGridElements, freeElements, snappointBasedElements);

            if (advancedGridElements.Count > 0)
            {
                EditorGUILayout.LabelField("Advanced Grid Building", m_EditorStyleContainer.HeadStyleBasicProperties);
                AddElementList(advancedGridElements, ref m_SelectedElements_AdvancedGrid_SectionVariable);
                AddAdvancedGridBuilderProperties();
                ABS_EditorUtils.AddSeparatorLine();
            }

            if (basicGridElements.Count > 0)
            {
                EditorGUILayout.LabelField("Basic Grid Building", m_EditorStyleContainer.HeadStyleBasicProperties);
                AddElementList(basicGridElements, ref m_SelectedElements_BasicGrid_SectionVariable);
                EditorGUILayout.LabelField("No Basic Grid Building specific property");
                ABS_EditorUtils.AddSeparatorLine();
            }

            if (freeElements.Count > 0)
            {
                EditorGUILayout.LabelField("Free Building", m_EditorStyleContainer.HeadStyleBasicProperties);
                AddElementList(basicGridElements, ref m_SelectedElements_Free_SectionVariable);
                AddFreeBuilderProperties();
                ABS_EditorUtils.AddSeparatorLine();
            }

            if (snappointBasedElements.Count > 0)
            {
                EditorGUILayout.LabelField("SnapPoint Based Building", m_EditorStyleContainer.HeadStyleBasicProperties);
                AddElementList(snappointBasedElements, ref m_SelectedElements_SnapPoint_SectionVariable);
                AddSnapPointBasedBuilderProperties();
                ABS_EditorUtils.AddSeparatorLine();
            }

            if (advancedGridElements.Count > 0)
            {
                EditorGUILayout.LabelField("Advanced Grid Actions", m_EditorStyleContainer.HeadStyleSectionGroup);
                ABS_EditorUtils.Space();

                GUILayout.BeginHorizontal();
                {
                    bool buttonResult = GUILayout.Button("Snap to Grid (Advanced)");
                    if (buttonResult)
                    {
                        foreach (ABS_BuildingElement target in advancedGridElements)
                        {
                            SetAdvancedGridPosition(target);
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }

            ABS_EditorUtils.Space();

            if (basicGridElements.Count > 0)
            {
                EditorGUILayout.LabelField("BasicGrid Actions", m_EditorStyleContainer.HeadStyleSection);

                ABS_EditorUtils.Space();

                GUILayout.BeginHorizontal();
                {
                    bool buttonResult = GUILayout.Button("Snap to Grid (Basic)");
                    if (buttonResult)
                    {
                        foreach (ABS_BuildingElement target in basicGridElements)
                        {
                            SetBasicGridPosition(target);
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }

            if (m_ParentError)
            {
                EditorGUILayout.HelpBox(m_ParentErrorMessage, MessageType.Error);
            }
        }

        private void AddElementList(List<ABS_BuildingElement> p_list, ref bool p_SectionVariable)
        {
            if (p_list.Count > 1)
            {
                p_SectionVariable = EditorGUILayout.BeginFoldoutHeaderGroup(p_SectionVariable, "Selected Elements");
                if (p_SectionVariable)
                {
                    ABS_EditorUtils.IndentIn();
                    {
                        foreach (ABS_BuildingElement target in p_list)
                        {
                            EditorGUILayout.LabelField(target.name);
                        }
                    }
                    ABS_EditorUtils.IndentOut();
                    ABS_EditorUtils.AddSeparatorLine();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }

        private void SortTargetElements(List<ABS_BuildingElement> p_Targets,
                                        List<ABS_BuildingElement> p_AdvancedGridElements,
                                        List<ABS_BuildingElement> p_BasicGridElements,
                                        List<ABS_BuildingElement> p_FreeElements,
                                        List<ABS_BuildingElement> p_SnapPointElements)
        {
            foreach (ABS_BuildingElement element in p_Targets)
            {
                if (element.PositionSearchAlgorithm == ABS_PositionSearchAlgorithm.AdvancedGrid)
                {
                    p_AdvancedGridElements.Add(element);
                }
                else if (element.PositionSearchAlgorithm == ABS_PositionSearchAlgorithm.BasicGrid)
                {
                    p_BasicGridElements.Add(element);
                }
                else if (element.PositionSearchAlgorithm == ABS_PositionSearchAlgorithm.Free)
                {
                    p_FreeElements.Add(element);
                }
                else if (element.PositionSearchAlgorithm == ABS_PositionSearchAlgorithm.SnapPointBased)
                {
                    p_SnapPointElements.Add(element);
                }
            }
        }

        private void AddAdvancedGridBuilderProperties()
        {
            ABS_EditorUtils.AddPropertyField(m_AdvancedGridTypeProperty, m_AdvancedGridTypeGUIContent);
            if ((int)m_AdvancedGridTypeProperty.enumValueFlag == (int)ABS_AdvancedGridType.Wall
                || (int)m_AdvancedGridTypeProperty.enumValueFlag == (int)ABS_AdvancedGridType.EdgeHorizontal)
            {
                ABS_EditorUtils.AddPropertyField(m_AdvancedGridAxisTypeProperty, m_AdvancedGridAxisTypeGUIContent);
                ABS_EditorUtils.AddPropertyField(m_AllowMixedAxisDragBuildingProperty, m_AllowMixedAxisDragBuildingGUIContent);
            }

            ABS_EditorUtils.Space();

            ABS_EditorUtils.AddPropertyField(m_StableElementProperty, m_StableElementGUIContent);
            ABS_EditorUtils.AddPropertyField(m_SnapPointRuleSetProperty, m_SnapPointRuleSetGUIContent);
            ABS_AdvancedGridSnapPointRuleSet rulelSet = m_SnapPointRuleSetProperty.objectReferenceValue as ABS_AdvancedGridSnapPointRuleSet;
            if (rulelSet != null)
            {
                ABS_AdvancedGridType ruleSetType = rulelSet.Type;
                if ((int)m_AdvancedGridTypeProperty.enumValueFlag != (int)ruleSetType)
                {
                    ABS_EditorUtils.HelpBox(MessageType.Warning, "Not matching AdvancedGridType.");
                }
            }
        }

        private void AddFreeBuilderProperties ()
        {
            ABS_EditorUtils.AddPropertyField(m_ShouldAttachedProperty, m_ShouldAttachedGUIContent);
        }

        private void AddSnapPointBasedBuilderProperties()
        {
            ABS_EditorUtils.AddPropertyField(m_MeshProperty, m_MeshGUIContent);
            ABS_EditorUtils.AddPropertyField(m_SnapPointTypeProperty, m_SnapPointTypeGUIContent);
        }

        private void SetAdvancedGridPosition(ABS_BuildingElement p_Element)
        {
            ABS_AdvancedGridBuilding building = CheckParent(p_Element, ABS_PositionSearchAlgorithm.AdvancedGrid) as ABS_AdvancedGridBuilding;
            if (building == null)
            {
                return;
            }

            p_Element.transform.localPosition = ABS_AdvancedGirdBuilderGridHelper.GetGridPosition(
                p_Element.transform.localPosition - building.BuildingPositionModifier,
                p_Element
            );
            p_Element.transform.localPosition += building.BuildingPositionModifier;

            bool rotationIsNeeded = ABS_AdvancedGirdBuilderGridHelper.RotationByPositionIsNeeded(p_Element, building);
            p_Element.transform.localRotation = rotationIsNeeded
                ? Quaternion.Euler(ABS_AdvancedGirdBuilderGridHelper.s_RotationModifier)
                : Quaternion.identity;


            ABS_EditorUtils.Dirty(p_Element);
        }

        private void SetBasicGridPosition(ABS_BuildingElement p_Element)
        {
            ABS_BasicGridBuilding building = CheckParent(p_Element, ABS_PositionSearchAlgorithm.BasicGrid) as ABS_BasicGridBuilding;
            if (building == null)
            {
                return;
            }

            bool aligned = false;
            p_Element.transform.position = ABS_BasicGridBuilder.GetGridPosition(p_Element.transform.position, p_Element, ref aligned);

            ABS_EditorUtils.Dirty(p_Element);
        }

        private ABS_Building CheckParent(ABS_BuildingElement p_Element, in ABS_PositionSearchAlgorithm p_Type)
        {
            Transform parentTransform = p_Element.gameObject.transform.parent;
            if (parentTransform == null)
            {
                m_ParentError = true;
                m_ParentErrorMessage = "The BuildingElement has to have a parent object";
                REST_Logging.Warrning("ABS_BuildingElementEditor", $"The BuildingElement has to have a parent object. Element: {p_Element.name}");
                return null;
            }

            GameObject parent = parentTransform.gameObject;
            if (parent == null)
            {
                m_ParentError = true;
                m_ParentErrorMessage = "The BuildingElement has to have a parent object";
                REST_Logging.Warrning("ABS_BuildingElementEditor", $"The BuildingElement has to have a parent object. Element: {p_Element.name}");
                return null;
            }

            if (p_Type == ABS_PositionSearchAlgorithm.AdvancedGrid)
            {
                ABS_AdvancedGridBuilding building = parent.GetComponent<ABS_AdvancedGridBuilding>();
                if (building == null)
                {
                    m_ParentError = true;
                    m_ParentErrorMessage = "The BuildingElement has to have a parent object with AdvancedGridBuilding component";
                    REST_Logging.Warrning("ABS_BuildingElementEditor", 
                        $"The BuildingElement has to have a parent object with AdvancedGridBuilding component. Element: {p_Element.name}");
                    return null;
                }
                return building;
            }
            else if (p_Type == ABS_PositionSearchAlgorithm.BasicGrid)
            {
                ABS_BasicGridBuilding building = parent.GetComponent<ABS_BasicGridBuilding>();
                if (building == null)
                {
                    m_ParentError = true;
                    m_ParentErrorMessage = "The BuildingElement has to have a parent object with BasicGridBuilding component";
                    REST_Logging.Warrning("ABS_BuildingElementEditor",
                        $"The BuildingElement has to have a parent object with BasicGridBuilding component. Element: {p_Element.name}");
                    return null;
                }
                m_ParentError = false;
                return building;
            }
            else
            {
                m_ParentError = true;
                m_ParentErrorMessage = "UnSupport Building Type!";
                return null;
            }
        }

        protected override void OnHeaderGUIImpl(out string p_Title)
        {
            p_Title = "BuildingElement";
        }
    }
}