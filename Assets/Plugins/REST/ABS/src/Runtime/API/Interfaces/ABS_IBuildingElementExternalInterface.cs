//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public interface ABS_IBuildingElementExternalInterface
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  getters / setters
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        /// <summary>
        /// Getter of the BuildingElement's Position Algorithm Settings.
        /// </summary>
        public ABS_BuilderBaseSettings PositionAlgorithmSettings { get; }

        /// <summary>
        /// Getter of the BuildingElement's FinalElement. Nullable!
        /// </summary>
        public ABS_BuildingElement FinalElement { get; }

        /// <summary>
        /// Getter of the BuildingElement's Renderers.
        /// The Highlight feature will modiy these renderers' materials.
        /// </summary>
        public List<Renderer> Renderers { get; }

        /// <summary>
        /// Getter of the BuildingElement's Colliders.
        /// </summary>
        public List<Collider> Collider { get; }

        /// <summary>
        /// Getter of the BuildingElement's SnapToPreBuiltFinalElement.
        /// If it is true then the BuildingElmenet can snap into a PreBuilt element of the Final Element.
        /// For example:
        ///     You have an A element and a B element.
        ///     The A's final element is the B.
        ///     Given a PreBuilt B elemnt in the scene.
        ///     In this case if this property is true then the A can snap into the B PreBuilt element.
        /// Usually the elements can snap only into their PreBuilt versions.
        /// </summary>
        public bool SnapToPreBuiltFinalElement { get; set; }

        /// <summary>
        /// If it is true then the Elemenet is a Foundation.
        /// </summary>
        public bool Foundation { get; set; }

        /// <summary>
        /// If it is true then the Elemenet is always blocked until at least one area doesn't allow it.
        /// </summary>
        public bool ShouldAllowedByArea { get; set; }
        
        /// <summary>
        /// If it is true then the Elemenet is always blocked until it is overriding an another element.
        /// </summary>
        public bool ShouldOverride { get; set; }

        /// <summary>
        /// If it is true then the Elemenet can not be destoryed by the BuildingManager
        /// </summary>
        public bool Indestructible { get; set; }

        /// <summary>
        /// Getter of the BuildingElement's AreaType.
        /// </summary>
        public ABS_BuildingElementAreaType AreaType { get; }

        /// <summary>
        /// Getter of the BuildingElement's Dimension.
        /// It is used during the calcualtion of the algorithms.
        /// </summary>
        public Vector3 Dimension { get; }

        /// <summary>
        /// Getter of the BuildingElement's ParentBuilding.
        /// </summary>
        public ABS_Building ParentBuilding { get; }

        /// <summary>
        /// If it is true then the BuildingElement can be built with drag building.
        /// </summary>
        public bool DragBuildingEnabled { get; }

        /// <summary>
        /// If it is true then the BuildingElement can built on X axis during the drag building
        /// </summary>
        public bool EnabledDragBuildingX { get; }

        /// <summary>
        /// If it is true then the BuildingElement can built on Z axis during the drag building
        /// </summary>
        public bool EnabledDragBuildingZ { get; }

        /// <summary>
        /// Getter of the BuildingElement's AdvancedGridType.
        /// </summary>
        public ABS_AdvancedGridType AdvancedGridType { get; }

        /// <summary>
        /// Getter of the BuildingElement's AdvancedGridAxisType.
        /// </summary>
        public ABS_AdvancedGridAxisType AdvancedGridAxisType { get; }

        /// <summary>
        /// GIf it is true then the element is a PreBuilt element.
        /// With ther setter the PreBuilt state can ba changed.
        /// The change has affect on the:
        ///     Matrials
        ///     Colliders
        ///     BuildingAlgorithms logic
        /// </summary>
        public bool PreBuilt { get; set; }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        // Functions
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        /// <summary>
        /// Add the highlight material to the  BuildingElement's materials.
        /// <param name="p_Index">The index of the highlight matterial from the BuildingElementHighlightCollection.</param>
        /// </summary>
        public void SetHighlightMaterial(int p_Index);

        /// <summary>
        /// Set the BuildingElement's materials to default.
        /// </summary>
        public void SetMaterialToDefault();

        /// <summary>
        /// Destroy the element with the DestroyImmediate function!
        /// The Element will be removed from the parent in the OnDestroy function.
        /// So it is safe to destroy the element's GameObject!
        /// <param name="p_Tracker">If the tracker is not null the tracker will get a BuildingElementDestroyed call with the BuidlingElement.</param>
        /// <param name="p_TriggeredByHistory">If the boolean is true than the OnDestroy will call
        ///     the BuildingElementHistoryDestroyed function instead of the BuildingElementDestroyed.
        /// <param name="p_DontDestroyTheParent">If it is true then the parent buidling will be not destroyed in case it has no element left.</param>
        /// <param name="p_IgnoreStability">If it is true then stability featuer will be not applied and the stability of the elements willé be not refreshed.</param>
        /// </param>
        /// </summary>
        public ABS_DestroyActionElementData Destroy(
            in ABS_BuildingManagerTracker p_Tracker,
            in bool p_TriggeredByHistory,
            in bool p_DontDestroyTheParent,
            in bool p_IgnoreStability);
    }
}