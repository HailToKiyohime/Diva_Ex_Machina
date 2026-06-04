//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public interface ABS_IBuildingManagerExternalInterface
    {
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Input
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        /// <summary>
        /// Getter for the Input Interface of the BuidlingManager
        /// </summary>
        /// <returns> The Input Interface of The ABS_BuildingManager.</returns>
        public ABS_IBuildingManagerInputActionInterface GetInputInterface();

        /// <summary>
        /// Block or unblock the input from the ABS_IBuildingManagerInputActionInterface on the BuildingManager.
        /// </summary>
        /// <param name="p_BlockState">Boolean value that represent the block state of the input interface.</param>
        public void BlockInput(in bool p_BlockState);

        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Tracker
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        /// <summary>
        /// Add a new tracker to the ABS_BuildingManager.
        /// </summary>
        /// <param name="p_Tracker">The new tracker added to the tracker's lists.</param>
        public void AddTracker(in ABS_BuildingManagerTracker p_Tracker);

        /// <summary>
        /// Remove one of the trackers of the ABS_BuildingManager.
        /// </summary>
        /// <param name="p_Tracker">The tracker should be removed from the tracker's lists.</param>
        public void RemoveTracker(in ABS_BuildingManagerTracker p_Tracker);

#if UNITY_EDITOR
        /// <summary>
        /// Add a new statistics tracker to the ABS_BuildingManager.
        /// </summary>
        /// <param name="p_Tracker">The new tracker added to the statistics tracker's lists.</param>
        public void RegistrateStatTracker(in ABS_IBuildingManagerStatisticsTracker p_StatTracker);

        /// <summary>
        /// Remove one of the statistics trackers of the ABS_BuildingManager.
        /// </summary>
        /// <param name="p_Tracker">The tracker should be removed from the statistics tracker's lists.</param>
        public void UnRegistrateStatTracker(in ABS_IBuildingManagerStatisticsTracker p_StatTracker);
#endif

        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  ABS_BuildingManager handling
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        /// <summary>
        /// Activate the ABS_BuildingManager.
        /// This function required to setup the ABS_BuildingManager based on the Settings of the given ABS_BuildingElement.
        /// The given index number will be used to choose the ABS_BuildingElement from the BuidlignElementList.
        /// </summary>
        /// <param name="p_Index">The index of the ABS_BuildingElement in the ABS_BuildingElementList.</param>
        /// <param name="p_BuildMode">The ABS_Building Mode of the BuidlingManager.</param>
        /// <returns>True if the ABS_BuildingManager could properly has been set up.</returns>
        public bool Activate(in uint p_Index, in ABS_BuildingManagerBuildMode p_BuildMode);

        /// <summary>
        /// Activate the ABS_BuildingManager.
        /// This function required to setup the ABS_BuildingManager based on the Settings of the given ABS_BuildingElement.
        /// This is an alternative way to add a ABS_BuildingElement to the ABS_BuildingManager instead of a List of BuildingElements.
        /// </summary>
        /// <param name="p_BuildingElement">The ABS_BuildingElement what will be used for building.</param>
        /// <param name="p_BuildMode">The ABS_Building Mode of the BuidlingManager.</param>
        /// <returns>True if the ABS_BuildingManager could properly has been set up.</returns>
        public bool Activate(in ABS_BuildingElement p_BuildingElement, in ABS_BuildingManagerBuildMode p_BuildMode);

        /// <summary>
        /// Return the activity status of the ABS_BuildingManager.
        /// </summary>
        /// <returns>return the ABS_BuildingManagerActiveStatus of the ABS_BuildingManager.</returns>
        public ABS_BuildingManagerActiveStatus ActiveStatus();

        /// <summary>
        /// Deactivate the ABS_BuildingManager.
        /// This function's call will reset the state of the ABS_BuildingManager.
        /// </summary>
        public void Deactivate();

        /// <summary>
        /// Change the ABS_BuildingManager's Mode.
        /// SimpleBuilding/DragBuilding -> SimpleDestroy
        /// SimpleDestroy/DragDestroy -> SimpleBuilding
        /// </summary>
        public void ChangeMode();

        /// <summary>
        /// With this function you can change the BuildingManagers mode.
        /// SimpleBuild or SimpleDestroy
        /// This function call will trigger the same functionality as the player pressed the "ModeChanger button" (Q by default).
        /// Changeing into the same Mode like During the drag building changeing to Build has no affect.
        /// </summary>
        /// <param name="p_Mode">The target mode of tha change</param>
        /// <returns>Return true if the change was successful</returns>
        public bool ChangeMode(in ABS_BuildingManagerMode p_Mode);

        /// <summary>
        /// Get the current Mode of the ABS_BuildingManager
        /// </summary>
        /// <returns>ABS_BuildingManagerMode the current mode of the Manager</returns>
        public ABS_BuildingManagerMode GetMode();

        /// <summary>
        /// Reset the list of the BuildingElements.
        /// </summary>
        /// <param name="p_ElementList">The target mode of tha change</param>
        public void SetBuildingElementList(in ABS_BuildingElementList p_ElementList);

        /// <summary>
        /// Reset the list of the BuildingElements.
        /// Also reset the ABS_BuildingElement with the new index.
        /// </summary>
        /// <param name="p_ElementList">The target mode of tha change</param>
        /// <param name="p_NewIndex">The new ABS_BuildingElement's index</param>
        public void SetBuildingElementList(in ABS_BuildingElementList p_ElementList, uint p_NewIndex);

        /// <summary>
        /// Return the Current ABS_BuildingElement what used by the ABS_BuildingManager
        /// </summary>
        /// <returns>Current ABS_BuildingElement</returns>
        public ABS_BuildingElement GetActiveBuildingElement();

        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Raycast
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        /// With this function you can change the Camera.
        /// It can be helpfull if you want to change the point of view of the game and still want to continue the building.
        /// </summary>
        /// <param name="p_Camera">The new camera for raycasting.</param>
        /// <returns>False if the RaycastMode does not set to Camera</returns>
        public bool ResetCamera(in Camera p_Camera);

        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  MaximumBuildingElementCount Handling
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        /// <summary>
        /// Enable the ABS_BuildingManager Sandbox mode which means that the MaximumBuildingElementCount number will be ignored.
        /// </summary>
        public void EnableSandbox();

        /// <summary>
        /// Disable the ABS_BuildingManager Sandbox mode which means that the MaximumBuildingElementCount number will be relevant.
        /// </summary>
        public void DisableSandbox();

        /// <summary>
        /// This function is required for changing the maximum element count for the DragBuilding.
        /// </summary>
        /// <param name="p_MaxElementCount">The maximum number of placeable elements at one time.</param>
        public void SetMaximumBuildingElementCount(in uint p_MaxElementCount);

        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Performance
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        /// <summary>
        /// Clear the algorithms' caches to free the used memory.
        /// </summary>
        public void ClearCache();

        /// <summary>
        /// Clear the Action History features data.
        /// </summary>
        public void ClearHistory();

        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Persistency
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        /// <summary>
        /// Get the persistency (save) data of the BuildingManager's BuildingParent.
        /// This function call will be redirected to the BuildingParent.
        /// </summary>
        /// <param name="p_PrettyPrint">If it is true then the JSON willl be formated to PrettyPrint</param>
        /// <returns>
        /// Return null in case of error like the BuildingManager's BuildingParent is null. 
        /// Otherwise return the PersistencyData as a JSON string.
        /// </returns>
        public string ToJSON(in bool p_PrettyPrint);

        /// <summary>
        /// Get the persistency (save) data of the BuildingManager's BuildingParent.
        /// This function call be redirected to the BuildingParent.
        /// </summary>
        /// <returns>
        /// Return null in case of error like the BuildingManager's BuildingParent is null. 
        /// Otherwise return the PersistencyData object of the BuildingParent.
        /// </returns>
        public ABS_PersistedData GetPersistedData();

        /// <summary>
        /// Update the BuildingManager's BuildingParent with a JSON string what should contain the BuildingParent persisted data.
        /// </summary>
        /// <param name="p_PersistedDataJSON">The persisted data JSON as string</param>
        /// <returns>
        /// Return a ABS_PersistencyLoadErrorCode
        /// </returns>
        public ABS_PersistencyLoadErrorCode UpdateFromPersistedJSON(in string p_PersistedDataJSON);
    }
}