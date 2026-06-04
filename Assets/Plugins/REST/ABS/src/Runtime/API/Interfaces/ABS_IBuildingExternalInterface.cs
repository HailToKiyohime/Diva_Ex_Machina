//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;
using System.Collections.ObjectModel;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public interface ABS_IBuildingExternalInterface
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  getters / setters
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        /// <summary>
        /// Getter of the BuildingElements of the ABS_Building.
        /// </summary>
        public ReadOnlyDictionary<Vector3, ABS_BuildingElement> Elements { get; }

        /// <summary>
        /// Getter of the ABS_PositionSearchAlgorithm type of the ABS_Building.
        /// </summary>
        public ABS_PositionSearchAlgorithm PositionSearchAlgorithmType { get; }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Life Cycle
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        /// <summary>
        /// Destroy the Buidling with all of it's BuildingElements as well. 
        /// <param name="p_Tracker">Tracker object for event callbacks</param>
        /// <param name="p_TriggeredByHistory">If the boolean is true than the OnDestroy will call
        ///     the BuildingWillBeHistoryDestroyed function instead of the BuildingWillBeDestroyed.
        /// </param>
        /// </summary>
        public void Destroy(ABS_BuildingManagerTracker p_Tracker, in bool p_TriggeredByHistory);

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Add
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        /// <summary>
        /// Add  ABS_BuildingElement to the ABS_Building.
        /// Be careful with this function! You can easily break the logic of the AdvancedBuildingSystem if you provide wrong data.
        /// Do not Recommend to use this function if you don't know exactly what data should be provided here.
        /// This function will change the BuidlingElement's rotation, position parent, materials etc.
        /// <param name="p_Tracker">Tracker object for event callbacks</param>
        /// <param name="p_TriggeredByHistory">If the boolean is true than the History callback functions will ba called.</param>
        /// <param name="p_NewElement">The ABS_BuildingElement added to the ABS_Building.</param>
        /// <param name="p_Position">The world position of the new ABS_BuildingElement. It should match to the rules of the ABS_Building!</param>
        /// <param name="p_Rotation">The EulerAngles of the new ABS_BuildingElement.
        /// <param name="p_UseLocal">Use the position and rotation as Local instead.</param>
        /// <param name="p_Force">If the position is already used then in case of force true the old one will be replaced and destroyed by the new one.</param>
        /// <param name="p_DestroyOld">In case of Force true the old element will be destroyed if this is true.</param>
        /// <param name="p_Override">True if the action was an override action</param>
        /// <param name="p_Prebuilt">True if the action built over an prebuilt element</param>
        /// <returns>Return a datafile what contains the build action's data or null it the action can not happened</returns>
        /// </summary>
        public ABS_BuildActionElementData AddBuildingElement(ABS_BuildingManagerTracker p_Tracker, 
                                                            bool p_TriggeredByHistory, 
                                                            ABS_BuildingElement p_NewElement, 
                                                            Vector3 p_Position,
                                                            Vector3 p_Rotation,
                                                            bool p_Force,
                                                            bool p_DestroyOld = true);

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Remove
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        /// <summary>
        /// Remove a ABS_BuildingElement from the ABS_Building.
        /// <param name="p_Tracker">Tracker object for event callbacks</param>
        /// <param name="p_TriggeredByHistory">If the boolean is true than the History callback functions will ba called.</param>
        /// <param name="p_IgnoreStability">If the boolean is true then the stability of the elments will be not refreshed</param>
        /// <param name="p_ElementToRemove">The ABS_BuildingElement removed from the ABS_Building.</param>
        /// <param name="p_IsBuildingDestroyed">Out boolean parameter what will be true 
        ///     if there is no ABS_BuildingElement left so the ABS_Building will be destroyed.</param>
        /// <returns>True if the element is successfully found and removed; otherwise, false.</returns>
        /// </summary>
        public void RemoveBuildingElement(
            ABS_DestroyActionElementData p_BaseDestroyActionData,
            ABS_BuildingManagerTracker p_Tracker,
            bool p_TriggeredByHistory,
            bool p_IgnoreStability,
            in ABS_BuildingElement p_ElementToRemove,
            bool p_DontDestroyTheParent,
            out bool p_IsBuildingDestroyed);

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Search / Check / Find
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        /// <summary>
        /// Return the ABS_BuildingElement from the ABS_Building at the given position.
        /// <param name="p_Position">The position of the ABS_BuildingElement</param>
        /// <param name="p_UseLocal">If true the p_Position used as localposition otherwise it is used as worldposition.</param>
        /// <returns>Return the ABS_BuildingElement if the position is successfully found; otherwise, return null.</returns>
        /// </summary>
        public ABS_BuildingElement FindBuildingElement(in Vector3 p_Position, in bool p_TransformPositionToLocal);

        /// <summary>
        /// Find the given ABS_BuildingElement and return a bool about the result.
        /// <param name="p_Element">The Element for find.</param>
        /// <returns>True if the elment is found, false otherwise.</returns>
        /// </summary>
        public bool ContainsBuildingElement(in ABS_BuildingElement p_Element);

        /// <summary>
        /// Find the given ABS_BuildingElement and return it's position
        /// <param name="p_Element">The Element for find.</param>
        /// <param name="p_PositionResult">The reult position of the element.</param>
        /// <returns>True if the element has been found, false otherwise</returns>
        /// </summary>
        public bool GetPositionOfBuildingElement(in ABS_BuildingElement p_Element, out Vector3 p_PositionResult);

        /// <summary>
        /// Search for the ABS_BuildingElement what has the same InstanceGuid.
        /// <param name="p_InstanceGuid">The instanceGuid for find.</param>
        /// <returns>The found element or null if it doesn't found.</returns>
        /// </summary>
        public ABS_BuildingElement FindBuildingElementBasedInstanceGuid(in string p_InstanceGuid);

        /// <summary>
        /// Search for all of the ABS_BuildingElement what has the same prefabGuid as the given one.
        /// <param name="p_PrefabGuid">The prefabGuid for find.</param>
        /// <returns>A list of the found elements.</returns>
        /// </summary>
        public List<ABS_BuildingElement> FindAllBuildingElementBasedPrefab(in string p_PrefabGuid);

        /// <summary>
        /// Search for all of the ABS_BuildingElement what has the same ABS_BuilderBaseSettings.
        /// <param name="p_Settings">The ABS_BuilderBaseSettings for find.</param>
        /// <returns>A list of the found elements.</returns>
        /// </summary>
        public List<ABS_BuildingElement> FindAllBuildingElement(in ABS_BuilderBaseSettings p_Settings);

        /// <summary>
        /// Search for all of the ABS_BuildingElement what has the same AreaType as the given one.
        /// <param name="p_AreaType">The AreaType for find.</param>
        /// <returns>A list of the found elements.</returns>
        /// </summary>
        public List<ABS_BuildingElement> FindAllBuildingElement(in ABS_BuildingElementAreaType p_AreaType);

        /// <summary>
        /// Search for all of the PreBuilt ABS_BuildingElement.
        /// <returns>A list of the found elements.</returns>
        /// </summary>
        public List<ABS_BuildingElement> FindAllPreBuiltBuildingElement();

        /// <summary>
        /// Search for all of the foundation ABS_BuildingElement.
        /// <param name="p_Inverse">If it is true then the function will return every element what is not a foundation.</param>
        /// <returns>A list of the found elements.</returns>
        /// </summary>
        public List<ABS_BuildingElement> FindAllFoundationBuildingElement(in bool p_Inverse = false);

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Element Modifications
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        /// <summary>
        /// Find all of the and replace all of the BuildingElements.
        /// Requirements:
        ///     - The provided BuildingElements must be not null.
        ///     - The elements can not be the same element so the guids have to be different.
        ///     - The elements’ main algorithm should be matching with the ABS_Building’s type
        ///     - The elements’ ABS_AdvancedGridType and ABS_AdvancedGridAxisType should be matching in case of AdvancedGridBuilding
        /// <param name="p_ReplaceTarget">The ABS_BuildingElement what will be searched and repalced.</param>
        /// <param name="p_ReplaceElement">The final element what will be placed after the replace.</param>
        /// <param name="p_DestroyOld">If it is true the old elements what have been replaced are destroyed immedietly after the replace.</param>
        /// <returns>
        ///     Return a list of ABS_BuildingElement pairs where the item1 is the old replaced element and the item2 is the new element. 
        ///     If the p_DestroyOld was true then the pair's first element is already destroyed!
        ///     If any requirement is not fullfiled, then null will be returned.
        /// </returns>
        /// </summary>
        public List<(ABS_BuildingElement, ABS_BuildingElement)> FindAllAndReplaceElements(in ABS_BuildingElement p_ReplaceTarget, in ABS_BuildingElement p_ReplaceElement, in bool p_DestroyOld);

        /// <summary>
        /// Change all of the BuildingElements of the ABS_Building to PreBuilt.
        /// If any of the ABS_BuildingElement was already PreBuilt then it will be untouched.
        /// <returns>Return a list of the changed BuildingElements.</returns>
        /// </summary>
        public List<ABS_BuildingElement> MakePreBuilt();

        /// <summary>
        /// Change all of the PreBuilt BuildingElements of the ABS_Building to normal.
        /// If any of the ABS_BuildingElement was already normal then it will be untouched.
        /// <returns>Return a list of the changed BuildingElements.</returns>
        /// </summary>
        public List<ABS_BuildingElement> RemovePreBuilt();

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Material Modifications
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        /// <summary>
        /// Change all of the BuildingElements' material of the ABS_Building to Default.
        /// </summary>
        public void SetMaterialToDefault();

        /// <summary>
        /// Change all of the BuildingElements' material of the ABS_Building to the provided material based on it's current state.
        /// </summary>
        public void SetMaterialBasedOnState();

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Cache
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        /// <summary>
        /// Clear the building's cache.
        /// </summary>
        public void ClearCache();

        /// <summary>
        /// Enable the caching of the buidling. If it was alreaday enabled then this function has no effect.
        /// </summary>
        public void EnableCahce();

        /// <summary>
        /// Disable the caching of the buidling. If it was alreaday disable then this function has no effect.
        /// The Cache will be cleared on disable.
        /// </summary>
        public void DisableCahce();
    }
}
