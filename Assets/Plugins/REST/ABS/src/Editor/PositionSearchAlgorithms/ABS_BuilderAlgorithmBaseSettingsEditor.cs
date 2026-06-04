//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;
using UnityEditor;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem.Editor
{
    [CustomEditor(typeof(ABS_BuilderBaseSettings))]
    internal abstract class BuilderBaseSettingsEditor : ABS_EditorBase
    {
        //----------------------------------------------------------------------------------------------------------------------
        //Literals

        private static readonly string s_NotSupportedText = "  :  Not Supported!";
        private static readonly string s_AllowPositionSearchAtRaycastEndPositionNotSupportedText = "Allow Position Search At Raycast End Position" + s_NotSupportedText;
        private static readonly string s_AlignPositionToGroundNotSupportedText = "Align Position To Ground" + s_NotSupportedText;
        private static readonly string s_PrioritizePreBuiltNotSupportedText = "Prioritize PreBuilt" + s_NotSupportedText;
        private static readonly string s_FoundationLogicNotSupportedText = "Use Foundation Logic" + s_NotSupportedText;
        private static readonly string s_PositionValidationCollisionCheckNotSupportedText = "Collision Based Validation" + s_NotSupportedText;

        //----------------------------------------------------------------------------------------------------------------------
        //Layers
        private SerializedProperty m_LayerCollectionProperty;
        private GUIContent m_LayerCollectionGUIContent;

        //----------------------------------------------------------------------------------------------------------------------
        //Basics
        private SerializedProperty m_SearchRadiusProperty;
        private GUIContent m_SearchRadiusGUIContent;
        private SerializedProperty m_BuildRadiusProperty;
        private GUIContent m_BuildRadiusGUIContent;
        private SerializedProperty m_ElementVisiblityProperty;
        private SerializedProperty m_AllowPositionSearchAtRaycastEndPositionProperty;
        private SerializedProperty m_AlignPositionToGroundProperty;
        private GUIContent m_AlignPositionToGroundGUIContent;
        private SerializedProperty m_PrioritizePreBuiltProperty;
        private GUIContent m_PrioritizePreBuiltGUIContent;
        private SerializedProperty m_UseFoundationLogicProperty;
        private GUIContent m_UseFoundationLogicGUIContent;

        //----------------------------------------------------------------------------------------------------------------------
        //Rotation
        private SerializedProperty m_EnableAlignRotationToBuildingElementsProperty;
        private GUIContent m_EnableAlignRotationToBuildingElementsGUIContent;
        private SerializedProperty m_ElementRotaionAlignmentStrategyProperty;
        private GUIContent m_ElementRotaionAlignmentStrategyGUIContent;

        private SerializedProperty m_AlignRotationToGroundStrategyProperty;
        private GUIContent m_AlignRotationToGroundStrategyGUIContent;
        private SerializedProperty m_MaximumRotationAlignmentProperty;
        private GUIContent m_MaximumRotationAlignmentGUIContent;
        private SerializedProperty m_RotationStrategyProperty;
        private SerializedProperty m_RotationYDegreeProperty;

        //----------------------------------------------------------------------------------------------------------------------
        //Drag Building
        private SerializedProperty m_EnableDragBuildingProperty;
        private SerializedProperty m_DragBuildingLimitXProperty;
        private SerializedProperty m_DragBuildingLimitXAmountProperty;
        private SerializedProperty m_DragBuildingLimitZProperty;
        private SerializedProperty m_DragBuildingLimitZAmountProperty;
        private SerializedProperty m_AllowDragBuildingWithoutHitProperty;

        //----------------------------------------------------------------------------------------------------------------------
        //Reposition Parameters
        private SerializedProperty m_RepositionStrategyProperty;
        private SerializedProperty m_AllowPlacementDuringMovementProperty;
        private SerializedProperty m_RepositionMoveSpeedProperty;
        private SerializedProperty m_RepositionRotateSpeedProperty;

        //----------------------------------------------------------------------------------------------------------------------
        //Element Modification Settings
        private SerializedProperty m_OverrideStrategyProperty;
        private SerializedProperty m_OverrideElementRulesetProperty;

        //----------------------------------------------------------------------------------------------------------------------
        // Validation
        private SerializedProperty m_ColliderSizeModifierProperty;

        private SerializedProperty m_PositionValidationElementCollisionCheckProperty;
        private SerializedProperty m_ElementCollisionFailureHandlingProperty;

        private SerializedProperty m_PositionValidationCollisionCheckProperty;
        private SerializedProperty m_CollisionFailureHandlingProperty;

        private SerializedProperty m_CheckUnderGroundPositionProperty;

        private SerializedProperty m_GroundedCheckProperty;
        private SerializedProperty m_GroundedCheckFailureHandlingProperty;

        private SerializedProperty m_BuildableGroundValidationProperty;
        private SerializedProperty m_BuildableGroundValidationFailureHandlingProperty;
        private SerializedProperty m_BuildableGround_ShouldAllCheckHitProperty;
        private SerializedProperty m_BuildableGround_RangeOffsetProperty;
        private SerializedProperty m_BuildableGround_RangeLimitProperty;

        private SerializedProperty m_AllowBuildingInTheAirProperty;
        private SerializedProperty m_AddMaximumHeightToAirBuildingProperty;
        private SerializedProperty m_AirPositionReferencePointProperty;
        private SerializedProperty m_MaximumAirHeightProperty;

        private SerializedProperty m_SpecialRuleValidationProperty;
        private SerializedProperty m_SpecialRuleValidationFailureHandlingProperty;

        private SerializedProperty m_BuildOnTopOfElementProperty;
        private SerializedProperty m_BuildOnTopOfElementResultHandlingProperty;

        private SerializedProperty m_ShouldSnapToFoundationFailureHandlingProperty;
        private SerializedProperty m_StabiltyFailureHandlingProperty;

        //----------------------------------------------------------------------------------------------------------------------
        //Section
        private bool p_LayerCollectionDetailsSectionVariable = false;

        private ABS_EditorTabView m_TabView = null;

        public BuilderBaseSettingsEditor() : base()
        {
            m_TabView = new ABS_EditorTabView(2);
            m_TabView.AddCallback("Base Settings", AddBasicSettingsProperties);
            m_TabView.AddCallback("Element Modification Settings", AddElementModificationProperties);
            m_TabView.AddCallback("Validation Settings", AddValidationProperties);
            m_TabView.AddCallback("Special", AddSpecialProperties);
        }

        protected override void OnEnableImpl()
        {
            //Visibility
            m_SearchRadiusProperty = serializedObject.FindProperty("m_SearchRadius");
            m_SearchRadiusGUIContent = new GUIContent("Search Radius", "The radius of the search area. The BuildingElement will be found inside this area. " +
                "Bigger area can help finding a position but the performance impact is increasing with the size of this radius. " +
                "Do not recommended to make it too big.");
            m_BuildRadiusProperty = serializedObject.FindProperty("m_BuildRadius");
            m_BuildRadiusGUIContent = new GUIContent("Build Radius", "The search area is where the elements will be searched for snapping. But the build radius is basiccaly a range. " +
                "The BuildingElements can be built if the position is inside the build radius' range.");

            m_ElementVisiblityProperty = serializedObject.FindProperty("m_ElementVisiblity");
            m_AllowPositionSearchAtRaycastEndPositionProperty = serializedObject.FindProperty("m_AllowPositionSearchAtRaycastEndPosition");
            m_AlignPositionToGroundProperty = serializedObject.FindProperty("m_AlignPositionToGround");
            m_AlignPositionToGroundGUIContent = new GUIContent("Align Position To Ground", "Align the position of the element to the ground.");
           
            m_PrioritizePreBuiltProperty = serializedObject.FindProperty("m_PrioritizePreBuilt");
            m_PrioritizePreBuiltGUIContent = new GUIContent("Prioritize PreBuilt", "Proiritize the PreBuilt elements");
            m_UseFoundationLogicProperty = serializedObject.FindProperty("m_UseFoundationLogic");
            m_UseFoundationLogicGUIContent = new GUIContent("Use Foundation Logic", "If you are using a snapping algorithm like AdvancedGrid or SnapPointBased " +
                "then only that BuildingElements can be placed without snapping what signed as foundation." +
                "Every other BuildingElement should snap to an another or it will be blocked.");

            //Layers
            m_LayerCollectionProperty = serializedObject.FindProperty("m_LayerCollection");
            m_LayerCollectionGUIContent = new GUIContent("Layer Collection", "The collection of the layers used by the Manager");

            //Rotation
            m_EnableAlignRotationToBuildingElementsProperty = serializedObject.FindProperty("m_EnableAlignRotationToBuildingElements");
            m_EnableAlignRotationToBuildingElementsGUIContent = new GUIContent("Enable Aligning the Rotation To BuildingElements",
                "Align the Element to the BuildingElement hit by the Raycast");
            m_ElementRotaionAlignmentStrategyProperty = serializedObject.FindProperty("m_ElementRotaionAlignmentStrategy");
            m_ElementRotaionAlignmentStrategyGUIContent = new GUIContent("Element Rotaion Alignment Strategy");
            m_AlignRotationToGroundStrategyProperty = serializedObject.FindProperty("m_AlignRotationToGroundStrategy");
            m_AlignRotationToGroundStrategyGUIContent = new GUIContent("Align Rotation To Ground",
                "Align the Element to the ground. " +
                "Note that it is working in such way the rotation is align to the raycast's hitpoint's normal. " +
                "And only in that case when tha raycast actually hit something.");
            m_MaximumRotationAlignmentProperty = serializedObject.FindProperty("m_MaximumRotationAlignment");
            m_MaximumRotationAlignmentGUIContent = new GUIContent("Maximum Rotation Alignment", 
                "The maximum rotation alignment allowed be the algorithm.");
            m_RotationStrategyProperty = serializedObject.FindProperty("m_RotationStrategy");
            m_RotationYDegreeProperty = serializedObject.FindProperty("m_RotationYDegree");

            //Drag Building
            m_EnableDragBuildingProperty = serializedObject.FindProperty("m_EnableDragBuilding");
            m_DragBuildingLimitXProperty = serializedObject.FindProperty("m_DragBuildingLimitX");
            m_DragBuildingLimitXAmountProperty = serializedObject.FindProperty("m_DragBuildingLimitXAmount");
            m_DragBuildingLimitZProperty = serializedObject.FindProperty("m_DragBuildingLimitZ");
            m_DragBuildingLimitZAmountProperty = serializedObject.FindProperty("m_DragBuildingLimitZAmount");
            m_AllowDragBuildingWithoutHitProperty = serializedObject.FindProperty("m_AllowDragBuildingWithoutHit");

            //Reposition Parameters
            m_RepositionStrategyProperty = serializedObject.FindProperty("m_RepositionStrategy");
            m_AllowPlacementDuringMovementProperty = serializedObject.FindProperty("m_AllowPlacementDuringMovement");
            m_RepositionMoveSpeedProperty = serializedObject.FindProperty("m_RepositionMoveSpeed");
            m_RepositionRotateSpeedProperty = serializedObject.FindProperty("m_RepositionRotateSpeed");

            //Element Modification Settings
            m_OverrideStrategyProperty = serializedObject.FindProperty("m_OverrideStrategy");
            m_OverrideElementRulesetProperty = serializedObject.FindProperty("m_OverrideElementRuleset");

            // Validation
            m_ColliderSizeModifierProperty = serializedObject.FindProperty("m_ColliderSizeModifier");

            m_PositionValidationElementCollisionCheckProperty = serializedObject.FindProperty("m_PositionValidationElementCollisionCheck");
            m_ElementCollisionFailureHandlingProperty = serializedObject.FindProperty("m_ElementCollisionFailureHandling");

            m_PositionValidationCollisionCheckProperty = serializedObject.FindProperty("m_PositionValidationCollisionCheck");
            m_CollisionFailureHandlingProperty = serializedObject.FindProperty("m_CollisionFailureHandling");

            m_CheckUnderGroundPositionProperty = serializedObject.FindProperty("m_CheckUnderGroundPosition");
            m_GroundedCheckProperty = serializedObject.FindProperty("m_GroundedCheck");
            m_GroundedCheckFailureHandlingProperty = serializedObject.FindProperty("m_GroundedCheckFailureHandling");

            m_BuildableGroundValidationProperty = serializedObject.FindProperty("m_BuildableGroundValidation");
            m_BuildableGroundValidationFailureHandlingProperty = serializedObject.FindProperty("m_BuildableGroundValidationFailureHandling");
            m_BuildableGround_ShouldAllCheckHitProperty = serializedObject.FindProperty("m_BuildableGround_ShouldAllCheckHit");
            m_BuildableGround_RangeOffsetProperty = serializedObject.FindProperty("m_BuildableGround_RangeOffset");
            m_BuildableGround_RangeLimitProperty = serializedObject.FindProperty("m_BuildableGround_RangeLimit");

            m_AllowBuildingInTheAirProperty = serializedObject.FindProperty("m_AllowBuildingInTheAir");
            m_AddMaximumHeightToAirBuildingProperty = serializedObject.FindProperty("m_AddMaximumHeightToAirBuilding");
            m_AirPositionReferencePointProperty = serializedObject.FindProperty("m_AirPositionReferencePoint");
            m_MaximumAirHeightProperty = serializedObject.FindProperty("m_MaximumAirHeight");

            m_SpecialRuleValidationProperty = serializedObject.FindProperty("m_SpecialRuleValidation");
            m_SpecialRuleValidationFailureHandlingProperty = serializedObject.FindProperty("m_SpecialRuleValidationFailureHandling");

            m_BuildOnTopOfElementProperty = serializedObject.FindProperty("m_BuildOnTopOfElement");
            m_BuildOnTopOfElementResultHandlingProperty = serializedObject.FindProperty("m_BuildOnTopOfElementResultHandling");

            m_ShouldSnapToFoundationFailureHandlingProperty = serializedObject.FindProperty("m_ShouldSnapToFoundationFailureHandling");
            m_StabiltyFailureHandlingProperty = serializedObject.FindProperty("m_StabiltyFailureHandling");
        }

        protected override void OnHeaderGUI()
        {
            base.OnHeaderGUI();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }

        protected override void OnInspectorGUIImpl()
        {
            m_TabView.Show(m_EditorStyleContainer);
        }

        protected void AddElementModificationProperties()
        {
            ABS_EditorUtils.Space();
            AddOverrideElementProperties();
        }

        private void AddBasicSettingsProperties()
        {
            ABS_EditorUtils.Space();
            AddBasicsProperties();
            ABS_EditorUtils.Space();
            AddLayerProperties();

            ABS_EditorUtils.Space();
            AddRotationProperties();

            ABS_EditorUtils.Space();
            AddDragBuildingProperties();

            ABS_EditorUtils.Space();
            AddRepositionParameters();
        }

        private void AddValidationProperties()
        {
            EditorGUILayout.PropertyField(m_ColliderSizeModifierProperty, new GUIContent("Collider Size Modifier"));
            ABS_EditorUtils.Space();

            EditorGUILayout.LabelField("Element Collision Based Validation", m_EditorStyleContainer.HeadStyleBasicProperties);
            ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
            {
                EditorGUILayout.PropertyField(m_PositionValidationElementCollisionCheckProperty, new GUIContent("Element Collision Based Validation"));
                if (m_PositionValidationElementCollisionCheckProperty.boolValue)
                {
                    EditorGUILayout.PropertyField(m_ElementCollisionFailureHandlingProperty, new GUIContent("Element Collision Failure Handling"));
                }
            }
            ABS_EditorUtils.BoxEnd();

            EditorGUILayout.LabelField("Collision Based Validation", m_EditorStyleContainer.HeadStyleBasicProperties);
            ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
            {
                if (InvokeBoolFunction("IsValidationCollisionCheckSupported"))
                {
                    EditorGUILayout.PropertyField(m_PositionValidationCollisionCheckProperty, new GUIContent("Collision Based Validation"));
                    if (m_PositionValidationCollisionCheckProperty.boolValue)
                    {
                        EditorGUILayout.PropertyField(m_CollisionFailureHandlingProperty, new GUIContent("Collision Failure Handling"));
                    }
                }
                else
                {
                    EditorGUILayout.LabelField(s_PositionValidationCollisionCheckNotSupportedText);
                }
            }
            ABS_EditorUtils.BoxEnd();

            EditorGUILayout.LabelField("UnderGround Validation", m_EditorStyleContainer.HeadStyleBasicProperties);
            ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
            {
                if (InvokeBoolFunction("IsUnderGroundValidationSupported"))
                {
                    EditorGUILayout.PropertyField(m_CheckUnderGroundPositionProperty, new GUIContent("UnderGround Validation"));
                }
                else
                {
                EditorGUILayout.LabelField("UnderGround Validation : Not Supported!");
                }
            }
            ABS_EditorUtils.BoxEnd();
            
            EditorGUILayout.LabelField("Grounded Validation", m_EditorStyleContainer.HeadStyleBasicProperties);
            ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
            {
                if (InvokeBoolFunction("IsGroundedCheckSupported"))
                {
                    EditorGUILayout.PropertyField(m_GroundedCheckProperty, new GUIContent("Grounded Check"));
                    if (m_GroundedCheckProperty.boolValue == true)
                    {
                        EditorGUILayout.PropertyField(m_GroundedCheckFailureHandlingProperty, new GUIContent("Failure Handling"));
                    }
                }
                else
                {
                EditorGUILayout.LabelField("Grounded Validation : Not Supported!");
                }
            }
            ABS_EditorUtils.BoxEnd();

            EditorGUILayout.LabelField("Buildable Ground Validation", m_EditorStyleContainer.HeadStyleBasicProperties);
            ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
            {
                if (InvokeBoolFunction("IsBuildableGroundValidationSupported"))
                {
                    EditorGUILayout.PropertyField(m_BuildableGroundValidationProperty, new GUIContent("Buildable Ground Validation"));
                    if (m_BuildableGroundValidationProperty.boolValue == true)
                    {
                        EditorGUILayout.PropertyField(m_BuildableGroundValidationFailureHandlingProperty, new GUIContent("Buildable Ground Failure Handling"));
                        EditorGUILayout.PropertyField(m_BuildableGround_ShouldAllCheckHitProperty, new GUIContent("Should All Check Hit"));
                        EditorGUILayout.PropertyField(m_BuildableGround_RangeOffsetProperty, new GUIContent("Range Offset"));
                        EditorGUILayout.PropertyField(m_BuildableGround_RangeLimitProperty, new GUIContent("Range Limit"));
                    }
                }
                else
                {
                EditorGUILayout.LabelField("Buildable Ground Validation : Not Supported!");
                }
            }
            ABS_EditorUtils.BoxEnd();

            EditorGUILayout.LabelField("Building In The Air", m_EditorStyleContainer.HeadStyleBasicProperties);
            ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
            {
                if (InvokeBoolFunction("IsAllowBuildingInTheAirSupported"))
                {
                    EditorGUILayout.PropertyField(m_AllowBuildingInTheAirProperty);
                    if (m_AllowBuildingInTheAirProperty.boolValue)
                    {
                            EditorGUILayout.PropertyField(m_AddMaximumHeightToAirBuildingProperty);
                            if (m_AddMaximumHeightToAirBuildingProperty.boolValue)
                            {
                                ABS_EditorUtils.IndentIn();
                                {
                                    EditorGUILayout.PropertyField(m_AirPositionReferencePointProperty);
                                    EditorGUILayout.PropertyField(m_MaximumAirHeightProperty);
                                }
                                ABS_EditorUtils.IndentOut();
                            }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Allow Building In The Air  :  Not Supported!");
                }
            }
            ABS_EditorUtils.BoxEnd();

            EditorGUILayout.LabelField("Special Rule Validation", m_EditorStyleContainer.HeadStyleBasicProperties);
            ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
            {
                if (InvokeBoolFunction("IsSpecialRuleValidationSupported"))
                {
                    EditorGUILayout.PropertyField(m_SpecialRuleValidationProperty, new GUIContent("Special Rule Validation"));
                    if (m_SpecialRuleValidationProperty.boolValue == true)
                    {
                        EditorGUILayout.PropertyField(m_SpecialRuleValidationFailureHandlingProperty, new GUIContent("Special Rule Validation Failure Handling"));
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Special Rule Validation: Not Supported!");
                }
            }
            ABS_EditorUtils.BoxEnd();

            EditorGUILayout.LabelField("Build On Top Of Element", m_EditorStyleContainer.HeadStyleBasicProperties);
            ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
            {
                if (InvokeBoolFunction("IsBuildOnTopOfElementSupported"))
                {
                    EditorGUILayout.PropertyField(m_BuildOnTopOfElementProperty, new GUIContent("Build On Top Of Element"));
                    if (m_BuildOnTopOfElementProperty.boolValue == true)
                    {
                        EditorGUILayout.PropertyField(m_BuildOnTopOfElementResultHandlingProperty, new GUIContent("Result Handling"));
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Build On Top Of Element: Not Supported!");
                }
            }
            ABS_EditorUtils.BoxEnd();

            EditorGUILayout.LabelField("Other", m_EditorStyleContainer.HeadStyleBasicProperties);
            ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
            {
                if (InvokeBoolFunction("IsFoundationLogicSupported"))
                {
                    EditorGUILayout.PropertyField(m_ShouldSnapToFoundationFailureHandlingProperty, new GUIContent("Should Snap To Foundation Failure Handling"));
                }
                else
                {
                    EditorGUILayout.LabelField("Should Snap To Foundation Failure Handling : Not Supported!");
                }
                if (InvokeBoolFunction("IsStabilitySupported"))
                {
                    EditorGUILayout.PropertyField(m_StabiltyFailureHandlingProperty, new GUIContent("Stability Failure Handling"));
                }
                else
                {
                    EditorGUILayout.LabelField("Stability Failure Handling : Not Supported!");
                }
            }
            ABS_EditorUtils.BoxEnd();
        }

        private void AddLayerProperties()
        {
            EditorGUILayout.LabelField("Layers", m_EditorStyleContainer.HeadStyleBasicProperties);
            ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
            {
                ABS_EditorUtils.AddScriptableObjectPropertyWithCreate<ABS_LayerCollection>(
                    ref m_LayerCollectionProperty,
                    m_LayerCollectionGUIContent,
                    m_EditorStyleContainer.SmallDarkButtonStyle,
                    "Missing Layer Collection!",
                    "Save Layer Collection",
                    "NewLayerCollection");

                if (m_LayerCollectionProperty.objectReferenceValue != null)
                {
                    ABS_EditorUtils.Space();
                    ABS_LayerCollectionEditor.DrawSettingsDetails(
                        m_EditorStyleContainer, 
                        ref p_LayerCollectionDetailsSectionVariable, 
                        m_LayerCollectionProperty.objectReferenceValue as ABS_LayerCollection);
                }
            }
            ABS_EditorUtils.BoxEnd();
        }

        private void AddBasicsProperties()
        {
            EditorGUILayout.LabelField("Basics", m_EditorStyleContainer.HeadStyleBasicProperties);
            ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
            {
                ABS_EditorUtils.AddPropertyField(m_SearchRadiusProperty, m_SearchRadiusGUIContent);
                ABS_EditorUtils.AddPropertyField(m_BuildRadiusProperty, m_BuildRadiusGUIContent);

                ABS_EditorUtils.Space();

                EditorGUILayout.PropertyField(m_ElementVisiblityProperty);

                if (InvokeBoolFunction("IsAllowPositionSearchAtRaycastEndPositionSupported"))
                {
                    EditorGUILayout.PropertyField(m_AllowPositionSearchAtRaycastEndPositionProperty);
                }
                else
                {
                    EditorGUILayout.LabelField(s_AllowPositionSearchAtRaycastEndPositionNotSupportedText);
                }

                if (InvokeBoolFunction("IsAlignPositionToGroundSupported"))
                {
                    ABS_EditorUtils.AddPropertyField(m_AlignPositionToGroundProperty, m_AlignPositionToGroundGUIContent);
                }
                else
                {
                    EditorGUILayout.LabelField(s_AlignPositionToGroundNotSupportedText);
                }

                ABS_EditorUtils.Space();

                if (InvokeBoolFunction("IsPrioritizePreBuiltSupported"))
                {
                    ABS_EditorUtils.AddPropertyField(m_PrioritizePreBuiltProperty, m_PrioritizePreBuiltGUIContent);
                }
                else
                {
                    EditorGUILayout.LabelField(s_PrioritizePreBuiltNotSupportedText);
                }

                if (InvokeBoolFunction("IsFoundationLogicSupported"))
                {
                    ABS_EditorUtils.AddPropertyField(m_UseFoundationLogicProperty, m_UseFoundationLogicGUIContent);
                }
                else
                {
                    EditorGUILayout.LabelField(s_FoundationLogicNotSupportedText);
                }
                
            }
            ABS_EditorUtils.BoxEnd();
        }

        private void AddRotationProperties()
        {
            EditorGUILayout.LabelField("Rotation", m_EditorStyleContainer.HeadStyleBasicProperties);
            ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
            {
                EditorGUILayout.PropertyField(m_RotationStrategyProperty, new GUIContent("Rotation Type"));
                if (m_RotationStrategyProperty.enumValueIndex == (int)ABS_RotationStrategy.PlayerRotation
                    || m_RotationStrategyProperty.enumValueIndex == (int)ABS_RotationStrategy.CamerAndPlayerRotation)
                {
                    EditorGUILayout.PropertyField(m_RotationYDegreeProperty);
                    if (360f % m_RotationYDegreeProperty.floatValue > 0f)
                    {
                        EditorGUILayout.HelpBox("The rotation with the given degree can not divide 360 completely. You can not do a full circle. Leftover above 360 degree: " + (360f % m_RotationYDegreeProperty.floatValue) + " degree", MessageType.Warning);
                    }

                    ABS_EditorUtils.Space();
                    ABS_EditorUtils.AddPropertyField(m_EnableAlignRotationToBuildingElementsProperty, m_EnableAlignRotationToBuildingElementsGUIContent);
                    ABS_EditorUtils.AddPropertyField(m_ElementRotaionAlignmentStrategyProperty, m_ElementRotaionAlignmentStrategyGUIContent);
                }
                else if (m_RotationStrategyProperty.enumValueIndex == (int)ABS_RotationStrategy.FixDegree)
                {
                    EditorGUILayout.PropertyField(m_RotationYDegreeProperty);
                }

                ABS_EditorUtils.Space();

                if (InvokeBoolFunction("IsAlignRotationToGroundStrategySupported"))
                {
                    ABS_EditorUtils.AddPropertyField(m_AlignRotationToGroundStrategyProperty, m_AlignRotationToGroundStrategyGUIContent);
                    if (m_AlignRotationToGroundStrategyProperty.enumValueIndex != (int)ABS_AlignRotationStrategy.None)
                    {
                        ABS_EditorUtils.AddPropertyField(m_MaximumRotationAlignmentProperty, m_MaximumRotationAlignmentGUIContent);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField(m_AlignRotationToGroundStrategyGUIContent + "  :  Not Supported!");
                    //The Maximum shouldn't
                    //EditorGUILayout.LabelField(m_MaximumRotationAlignmentGUIContent + "  :  Not Supported!");
                }
            }
            ABS_EditorUtils.BoxEnd();
        }

        private void AddDragBuildingProperties()
        {
            EditorGUILayout.LabelField("Drag Building", m_EditorStyleContainer.HeadStyleBasicProperties);
            ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
            {
                if (InvokeBoolFunction("IsDragBuildingSpecSupported"))
                {
                    EditorGUILayout.PropertyField(m_EnableDragBuildingProperty);
                    if (m_EnableDragBuildingProperty.boolValue)
                    {
                        ABS_EditorUtils.Space();
                        EditorGUILayout.PropertyField(m_DragBuildingLimitXProperty);
                        if (m_DragBuildingLimitXProperty.boolValue)
                        {
                            ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
                            EditorGUILayout.PropertyField(m_DragBuildingLimitXAmountProperty);
                            ABS_EditorUtils.BoxEnd();
                        }
                        EditorGUILayout.PropertyField(m_DragBuildingLimitZProperty);
                        if (m_DragBuildingLimitZProperty.boolValue)
                        {
                            ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
                            EditorGUILayout.PropertyField(m_DragBuildingLimitZAmountProperty);
                            ABS_EditorUtils.BoxEnd();
                        }
                        ABS_EditorUtils.Space();
                        EditorGUILayout.PropertyField(m_AllowDragBuildingWithoutHitProperty);
                        if (m_AllowDragBuildingWithoutHitProperty.boolValue && !m_AllowPositionSearchAtRaycastEndPositionProperty.boolValue)
                        {
                            EditorGUILayout.HelpBox("The \"Allow Position Search At Raycast End Position\" property is stronger." +
                                " Even if you set this property true the drag building will be not allowed without hit", MessageType.Warning);
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Not Supported!");
                }
            }
            ABS_EditorUtils.BoxEnd();
        }

        private void AddRepositionParameters()
        {
            EditorGUILayout.LabelField("Reposition", m_EditorStyleContainer.HeadStyleBasicProperties);
            ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
            {
                if (InvokeBoolFunction("IsRepositionSpecSupported"))
                {
                    EditorGUILayout.PropertyField(m_RepositionStrategyProperty, new GUIContent("Building Element Reposition Type"));
                    if (m_RepositionStrategyProperty.enumValueIndex == (int)ABS_RotationStrategy.PlayerRotation)
                    {
                        ABS_EditorUtils.IndentIn();
                        {
                            EditorGUILayout.PropertyField(m_AllowPlacementDuringMovementProperty);
                            EditorGUILayout.PropertyField(m_RepositionMoveSpeedProperty);
                            EditorGUILayout.PropertyField(m_RepositionRotateSpeedProperty);
                        }
                        ABS_EditorUtils.IndentOut();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Not Supported!");
                }
            }
            ABS_EditorUtils.BoxEnd();
        }


        private void AddOverrideElementProperties()
        {
            EditorGUILayout.LabelField("Override Element", m_EditorStyleContainer.HeadStyleBasicProperties);
            ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
            {
                if (InvokeBoolFunction("IsOverrideElementSpecSupported"))
                {
                    EditorGUILayout.PropertyField(m_OverrideStrategyProperty);
                    if (m_OverrideStrategyProperty.enumValueFlag == (int)ABS_OverrideStrategy.Ruleset)
                    {
                        EditorGUILayout.PropertyField(m_OverrideElementRulesetProperty);
                        if (m_OverrideElementRulesetProperty.objectReferenceValue == null)
                        {
                            EditorGUILayout.HelpBox("Missing OverrideRuleset Object!", MessageType.Error);
                        }
                    }
                    else
                    {
                        m_OverrideElementRulesetProperty.objectReferenceValue = null;
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Not Supported!");
                }
            }
            ABS_EditorUtils.BoxEnd();
        }

        protected static void DrawSettingsDetails(ABS_EditorStyleContainer p_EditorStyleContainer, in ABS_BuilderBaseSettings p_Settings)
        {
            EditorGUILayout.LabelField("Basics", p_EditorStyleContainer.HeadStyleBasicProperties);
            ABS_EditorUtils.IndentIn();
            {
                EditorGUILayout.LabelField("SearchRadius  :  " + p_Settings.SearchRadius + " [Range: 0.1 - 10]");
                EditorGUILayout.LabelField("BuildRadius  :  " + p_Settings.BuildRadius + " [Range: 0.1 - 10]");
                EditorGUILayout.LabelField("ElementVisiblity  :  " + p_Settings.ElementVisiblity.ToString());

                if (p_Settings.IsAllowPositionSearchAtRaycastEndPositionSupported())
                {
                    EditorGUILayout.LabelField("Allow Position Search At  Raycast End Position  :  " + p_Settings.AllowPositionSearchAtRaycastEndPosition.ToString());
                }
                else
                {
                    EditorGUILayout.LabelField(s_AllowPositionSearchAtRaycastEndPositionNotSupportedText);
                }

                if (p_Settings.IsAlignPositionToGroundSupported())
                {
                    EditorGUILayout.LabelField("AlignPositionToGround  :  " + p_Settings.AlignPositionToGround.ToString());
                }
                else
                {
                    EditorGUILayout.LabelField(s_AlignPositionToGroundNotSupportedText);
                }

                if (p_Settings.IsPrioritizePreBuiltSupported())
                {
                    EditorGUILayout.LabelField("Prioritize PreBuilt  :  " + p_Settings.PrioritizePreBuilt.ToString());
                }
                else
                {
                    EditorGUILayout.LabelField(s_PrioritizePreBuiltNotSupportedText);
                }

                if (p_Settings.IsFoundationLogicSupported())
                {
                    EditorGUILayout.LabelField("Use Foundation Logic  :  " + p_Settings.UseFoundationLogic.ToString());
                }
                else
                {
                    EditorGUILayout.LabelField(s_FoundationLogicNotSupportedText);
                }
            }
            ABS_EditorUtils.IndentOut();

            ABS_EditorUtils.Space();

            EditorGUILayout.LabelField("Layer", p_EditorStyleContainer.HeadStyleBasicProperties);
            ABS_EditorUtils.IndentIn();
            {
                EditorGUILayout.LabelField("LayerCollection :");
                ABS_EditorUtils.AddObjectLinkLabel(p_Settings.LayerCollection, 100);
            }
            ABS_EditorUtils.IndentOut();

            ABS_EditorUtils.Space();
           
                EditorGUILayout.LabelField("Rotation", p_EditorStyleContainer.HeadStyleBasicProperties);
                ABS_EditorUtils.IndentIn();
                {
                    EditorGUILayout.LabelField("AlignRotationToBuildingElements  :  " + p_Settings.EnableAlignRotationToBuildingElements.ToString());

                    if (p_Settings.IsAlignRotationToGroundStrategySupported())
                    {
                        EditorGUILayout.LabelField("AlignRotationToGround  :  " + p_Settings.AlignRotationToGroundStrategy.ToString());
                    }
                    else
                    {
                        EditorGUILayout.LabelField("AlignRotationToGround  :  Not Supported!");
                    }
                    EditorGUILayout.LabelField("Elementm_RotationStrategyVisiblity  :  " + p_Settings.RotationStrategy.ToString());
                    EditorGUILayout.LabelField("RotationYDegree  :  " + p_Settings.RotationYDegree.ToString() + "% [Range: 0 - 360]");
                }
                ABS_EditorUtils.IndentOut();

            ABS_EditorUtils.Space();
            if (p_Settings.IsDragBuildingSpecSupported())
            {
                EditorGUILayout.LabelField("Drag Building", p_EditorStyleContainer.HeadStyleBasicProperties);
                ABS_EditorUtils.IndentIn();
                {
                    EditorGUILayout.LabelField("EnableDragBuilding  :  " + p_Settings.EnableDragBuilding.ToString());
                    if (p_Settings.EnableDragBuilding)
                    {
                        EditorGUILayout.LabelField("DragBuildingLimitX  :  " + p_Settings.DragBuildingLimitX.ToString());
                        if (p_Settings.DragBuildingLimitX)
                        {
                            EditorGUILayout.LabelField("DragBuildingLimitXAmount  :  " + p_Settings.DragBuildingLimitXAmount.ToString());
                        }
                        EditorGUILayout.LabelField("DragBuildingLimitZ  :  " + p_Settings.DragBuildingLimitZ.ToString());
                        if (p_Settings.DragBuildingLimitZ)
                        {
                            EditorGUILayout.LabelField("DragBuildingLimitZAmount  :  " + p_Settings.DragBuildingLimitZAmount.ToString());
                        }
                        EditorGUILayout.LabelField("AllowDragBuildingWithoutHit  :  " + p_Settings.AllowDragBuildingWithoutHit.ToString());
                    }
                }
                ABS_EditorUtils.IndentOut();
            }
            else
            {
                EditorGUILayout.LabelField("DragBuilding  :  Not Supported!", p_EditorStyleContainer.HeadStyleBasicProperties);
            }

            ABS_EditorUtils.Space();
            if (p_Settings.IsRepositionSpecSupported())
            {
                EditorGUILayout.LabelField("Reposition", p_EditorStyleContainer.HeadStyleBasicProperties);
                ABS_EditorUtils.IndentIn();
                {
                    EditorGUILayout.LabelField("RepositionStrategy  :  " + p_Settings.RepositionStrategy.ToString());
                    if (p_Settings.RepositionStrategy == ABS_RepositionStrategy.Smooth)
                    {
                        EditorGUILayout.LabelField("RepositionMoveSpeed  :  " + p_Settings.RepositionMoveSpeed.ToString() + " [Range: 1 - 100]");
                        EditorGUILayout.LabelField("RepositionRotateSpeed  :  " + p_Settings.RepositionRotateSpeed.ToString() + " [Range: 1 - 100]");
                    }
                }
                ABS_EditorUtils.IndentOut();
            }
            else
            {
                EditorGUILayout.LabelField("Reposition  :  Not Supported!", p_EditorStyleContainer.HeadStyleBasicProperties);
            }

            ABS_EditorUtils.Space();

            EditorGUILayout.LabelField("Element Modification", p_EditorStyleContainer.HeadStyleBasicProperties);
            ABS_EditorUtils.IndentIn();
            {
                if (p_Settings.IsOverrideElementSpecSupported())
                {
                    EditorGUILayout.LabelField($"Override Element  :  {p_Settings.OverrideStrategy.ToString()}", p_EditorStyleContainer.HeadStyleBasicProperties);
                }
                else
                {
                    EditorGUILayout.LabelField("Override Element  :  Not Supported!", p_EditorStyleContainer.HeadStyleBasicProperties);
                }
            }
            ABS_EditorUtils.IndentOut();

            ABS_EditorUtils.Space();

            EditorGUILayout.LabelField("Position Validation", p_EditorStyleContainer.HeadStyleBasicProperties);
            ABS_EditorUtils.Space();
            EditorGUILayout.LabelField("ColliderSizeModifier  :  " + p_Settings.ColliderSizeModifier.ToString());
            ABS_EditorUtils.Space();
            if (p_Settings.IsValidationCollisionCheckSupported())
            {
                ABS_EditorUtils.IndentIn();
                {
                    EditorGUILayout.LabelField("PositionValidationElementCollisionCheck  :  " + p_Settings.PositionValidationElementCollisionCheck.ToString());
                    if (p_Settings.PositionValidationElementCollisionCheck)
                    {
                        EditorGUILayout.LabelField("ElementValidationFailureHandling  : ", p_Settings.ElementCollisionFailureHandling.ToString());
                    }
                    EditorGUILayout.LabelField("PositionValidationCollisionCheck  :  " + p_Settings.PositionValidationCollisionCheck.ToString());
                    if (p_Settings.PositionValidationCollisionCheck)
                    {
                        EditorGUILayout.LabelField("ValidationFailureHandling  : ", p_Settings.CollisionFailureHandling.ToString());
                        ABS_EditorUtils.AddObjectLinkLabel(p_Settings.LayerCollection, 100);
                    }
                }
                ABS_EditorUtils.IndentOut();
            }
            else
            {
                EditorGUILayout.LabelField("Collision Validation  :  Not Supported!", p_EditorStyleContainer.HeadStyleBasicProperties);
            }

            if (p_Settings.IsUnderGroundValidationSupported())
            {
                ABS_EditorUtils.IndentIn();
                {
                    EditorGUILayout.LabelField("CheckUnderGroundPosition  :  " + p_Settings.CheckUnderGroundPosition.ToString());
                }
                ABS_EditorUtils.IndentOut();
            }
            else
            {
                EditorGUILayout.LabelField("UnderGround Validation  :  Not Supported!", p_EditorStyleContainer.HeadStyleBasicProperties);
            }
            
            if (p_Settings.IsGroundedCheckSupported())
            {
                ABS_EditorUtils.IndentIn();
                {
                    EditorGUILayout.LabelField("CheckGrounded  :  " + p_Settings.GroundedCheck.ToString());
                }
                ABS_EditorUtils.IndentOut();
            }
            else
            {
                EditorGUILayout.LabelField("CheckGrounded Validation  :  Not Supported!", p_EditorStyleContainer.HeadStyleBasicProperties);
            }
            

            if (p_Settings.IsBuildableGroundValidationSupported())
            {
                ABS_EditorUtils.IndentIn();
                {
                    EditorGUILayout.LabelField("Buildable Ground Validation  :  " + p_Settings.BuildableGroundValidation.ToString());
                    EditorGUILayout.LabelField("Buildable Ground Failure Handling  :  " + p_Settings.BuildableGroundValidationFailureHandling.ToString());
                    EditorGUILayout.LabelField("Should All Check Hit  :  " + p_Settings.BuildableGround_ShouldAllCheckHit.ToString());
                    EditorGUILayout.LabelField("Range Offset  :  " + p_Settings.BuildableGround_RangeOffset.ToString());
                    EditorGUILayout.LabelField("Range Limit  :  " + p_Settings.BuildableGround_RangeLimit.ToString());
                }
                ABS_EditorUtils.IndentOut();
            }
            else
            {
                EditorGUILayout.LabelField("Buildable Ground Validation :  Not Supported!", p_EditorStyleContainer.HeadStyleBasicProperties);
            }

            if (p_Settings.IsAllowBuildingInTheAirSupported())
            {
                ABS_EditorUtils.IndentIn();
                {
                    EditorGUILayout.LabelField("AllowBuildingInTheAir  :  " + p_Settings.AllowBuildingInTheAir.ToString());
                    ABS_EditorUtils.IndentIn();
                    {
                        EditorGUILayout.LabelField("AirPositionReferencePoint  :  " + p_Settings.AirPositionReferencePoint.ToString());
                        EditorGUILayout.LabelField("AddMaximumHeightToAirBuilding  :  " + p_Settings.AddMaximumHeightToAirBuilding.ToString());
                        ABS_EditorUtils.IndentIn();
                        {
                            EditorGUILayout.LabelField("MaximumAirHeight  :  " + p_Settings.MaximumAirHeight.ToString());
                        }
                        ABS_EditorUtils.IndentOut();
                    }
                    ABS_EditorUtils.IndentOut();
                }
                ABS_EditorUtils.IndentOut();
            }
            else
            {
                ABS_EditorUtils.IndentIn();
                {
                    EditorGUILayout.LabelField("AllowBuildingInTheAir  :  Not Supported!");
                }
                ABS_EditorUtils.IndentOut();
            }

            if (p_Settings.IsSpecialRuleValidationSupported())
            {
                ABS_EditorUtils.IndentIn();
                {
                    EditorGUILayout.LabelField("Special Rule Validation  :  " + p_Settings.SpecialRuleValidation.ToString());
                    EditorGUILayout.LabelField("Special Rule Validation Failure Handling  :  " + p_Settings.SpecialRuleValidationFailureHandling.ToString());
                }
                ABS_EditorUtils.IndentOut();
            }
            else
            {
                EditorGUILayout.LabelField("Special Rule Validation  :  Not Supported!", p_EditorStyleContainer.HeadStyleBasicProperties);
            }

            if (p_Settings.IsBuildOnTopOfElementSupported())
            {
                ABS_EditorUtils.IndentIn();
                {
                    EditorGUILayout.LabelField("Build On Top Of Element  :  " + p_Settings.BuildOnTopOfElement.ToString());
                    EditorGUILayout.LabelField("Build On Top Of Element Result Handling  :  " + p_Settings.BuildOnTopOfElementResultHandling.ToString());
                }
                ABS_EditorUtils.IndentOut();
            }
            else
            {
                EditorGUILayout.LabelField("Build On Top Of Element :  Not Supported!", p_EditorStyleContainer.HeadStyleBasicProperties);
            }

            ABS_EditorUtils.Space();
            EditorGUILayout.LabelField("Other Validation Failure Handling", p_EditorStyleContainer.HeadStyleBasicProperties);
            ABS_EditorUtils.IndentIn();
            {
                if (p_Settings.IsFoundationLogicSupported())
                {
                    EditorGUILayout.LabelField("Should Snap To Foundation Failure Handling  :  " + p_Settings.ShouldSnapToFoundationFailureHandling.ToString());
                }
                else
                {
                    EditorGUILayout.LabelField("Should Snap To Foundation Failure Handling : Not Supported!");
                }
                if (p_Settings.IsStabilitySupported())
                {
                    EditorGUILayout.LabelField("Stability Failure Handling  :  " + p_Settings.StabiltyFailureHandling.ToString());
                }
                else
                {
                    EditorGUILayout.LabelField("Stability Failure Handling : Not Supported!");
                }
            }
            ABS_EditorUtils.IndentOut();
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Abstract functions
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        protected abstract void AddSpecialProperties();
    }
}
