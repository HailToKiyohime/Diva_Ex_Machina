//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public class ABS_ActionHistory : ABS_BuildingManagerComponentBase
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private bool m_IsHistoryEnabled = false;
        private uint m_HistoryActionCount = 50;
        private bool m_PartialProcessing = false;
        private bool m_ClearHistoryInCaseOfError = false;

        private List<ABS_ActionBase> m_Actions = null;
        private int m_ActionIndex = 0;
        private ABS_BuildingElementList m_ElementList = null;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Initialization
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        public ABS_ActionHistory(bool p_IsHistoryEnabled,
                                uint p_HistoryActionCount,
                                bool p_PartialProcessing,
                                bool p_ClearHistoryInCaseOfError,
                                ABS_IBuildingManagerInternalInterface p_Manager,
                                ABS_BuildingManagerTracker p_Tracker,
                                ABS_BuildingElementList p_ElementList)
                                : base(p_Manager, p_Tracker)
        {
            m_Actions = new List<ABS_ActionBase>();
            m_HistoryActionCount = p_HistoryActionCount;
            m_ElementList = p_ElementList;
            m_IsHistoryEnabled = p_IsHistoryEnabled;
            m_PartialProcessing = p_PartialProcessing;
            m_ClearHistoryInCaseOfError = p_ClearHistoryInCaseOfError;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // Initialization
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region public Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public void AddAction(ABS_ActionBase p_Action)
        {
            if (!m_IsHistoryEnabled)
            {
                return;
            }

            if (m_ActionIndex != 0)
            {
                m_Actions.RemoveRange(0, m_ActionIndex);
                m_ActionIndex = 0;
            }

            m_Actions.Insert(0, p_Action);

            if (m_Actions.Count > m_HistoryActionCount)
            {
                m_Actions.RemoveAt(m_Actions.Count - 1);
            }
        }

        public ABS_ActionHistoryErrorCodes Undo()
        {
            if (!m_IsHistoryEnabled)
            {
                return ABS_ActionHistoryErrorCodes.Success_FeatureDisabled;
            }

            //We can not undo more action
            if (m_ActionIndex + 1 > m_Actions.Count)
            {
                return ABS_ActionHistoryErrorCodes.Success_NoMoreAction;
            }

            ABS_ActionBase action = m_Actions[m_ActionIndex];
            ABS_ActionHistoryErrorCodes err = ABS_ActionHistoryErrorCodes.Success;
            switch (action.Type)
            {
                case ABS_ActionTypes.Destroy: err = DestroyUndo(action as ABS_DestroyAction); break;
                case ABS_ActionTypes.Build: err = BuildUndo(action as ABS_BuildAction); break;
            }

            if (err != ABS_ActionHistoryErrorCodes.Success)
            {
                if (m_ClearHistoryInCaseOfError)
                {
                    Clear();
                }
                else
                {
                    m_Actions.RemoveAt(m_ActionIndex);
                }

                REST_Logging.Warrning("ABS_ActionHistory", $"ActionType : {action.Type} | result : {err}");
            }
            else
            {
                ++m_ActionIndex;
            }
            return err;
        }

        public ABS_ActionHistoryErrorCodes Redo()
        {
            if (!m_IsHistoryEnabled)
            {
                return ABS_ActionHistoryErrorCodes.Success_FeatureDisabled;
            }

            if (m_ActionIndex == 0)
            {
                return ABS_ActionHistoryErrorCodes.Success_EmptyHistroy;
            }

            --m_ActionIndex;

            ABS_ActionBase action = m_Actions[m_ActionIndex];
            ABS_ActionHistoryErrorCodes err = ABS_ActionHistoryErrorCodes.Success;
            switch (action.Type)
            {
                case ABS_ActionTypes.Destroy: err = DestroyRedo(action as ABS_DestroyAction); break;
                case ABS_ActionTypes.Build: err = BuildRedo(action as ABS_BuildAction); break;
            }

            if (err != ABS_ActionHistoryErrorCodes.Success)
            {
                if (m_ClearHistoryInCaseOfError)
                {
                    Clear();
                }
                else
                {
                    m_Actions.RemoveAt(m_ActionIndex);
                }
                REST_Logging.Warrning($"{this}", $"ActionType : {action.Type} | result : {err}");
            }
            return err;
        }

        public void Clear ()
        {
            m_Actions.Clear();
            m_ActionIndex = 0;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // public Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Destroy
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Destroy Undo
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        //Rebuild everything what destroyed by the Destroy feature
        private ABS_ActionHistoryErrorCodes DestroyUndo(ABS_DestroyAction p_Action)
        {
            List<ABS_BuildingElement> newElements = new List<ABS_BuildingElement>();
            foreach (ABS_ActionElementDataBase<ABS_DestroyActionBuildingData> data in p_Action.Data)
            {
                ABS_ActionHistoryErrorCodes err = DestroyUndoReq(data, newElements);
                if (err != ABS_ActionHistoryErrorCodes.Success)
                {
                    if (m_PartialProcessing)
                    {
                        continue;
                    }
                    else
                    {
                        Rollback_DestroyUndo(newElements);
                        return err;
                    }
                }

                newElements.Add(data.BuildingElementInstance);
            }

            return ABS_ActionHistoryErrorCodes.Success;
        }

        private ABS_ActionHistoryErrorCodes DestroyUndoReq(
            ABS_ActionElementDataBase<ABS_DestroyActionBuildingData> p_ActionElementData,
            List<ABS_BuildingElement> p_NewElements)
        {
            ABS_DestroyActionElementData destroyData = p_ActionElementData as ABS_DestroyActionElementData;
            ABS_BuildingElement newInstance = null;

            ABS_ActionHistoryErrorCodes err = ChechPrefabAvailability(p_ActionElementData);
            if (err != ABS_ActionHistoryErrorCodes.Success)
            {
                return err;
            }

            //First create recreate teh destroyed element
            err = InstantiatePrefab(p_ActionElementData);
            if (err != ABS_ActionHistoryErrorCodes.Success)
            {
                if (p_ActionElementData.BuildingElementInstance != null)
                {
                    Rollback_DestroyUndo_One(p_ActionElementData.BuildingElementInstance);
                }
                return err;
            }
            else
            {
                newInstance = p_ActionElementData.BuildingElementInstance;
                p_NewElements.Add(newInstance);
            }

            //If the element's building is not available then create one
            err = EnsureBuildingAvailability(
                p_ActionElementData.BuildingData,
                newInstance, 
                p_ActionElementData.BuildingData.IsParentDestroyed);
            if (err != ABS_ActionHistoryErrorCodes.Success)
            {
                return err;
            }

            //Add the Element to the Building
            ABS_BuildActionElementData AddElementResult = p_ActionElementData.BuildingData.BuildingInstance.AddBuildingElement(
                            p_Tracker : m_Tracker,
                            p_TriggeredByHistory : true,
                            p_NewElement : newInstance,
                            p_LocalPosition: p_ActionElementData.LocalPosition,
                            p_LocalEulerAngles: p_ActionElementData.LocalEulerAngles,
                            p_Force: false,
                            p_DestroyOld : false);

            if (AddElementResult == null)
            {
                if (p_ActionElementData.BuildingData.BuildingInstance.FreeSpace == 0)
                {
                    return ABS_ActionHistoryErrorCodes.Failed_PositionValidation_NotEnoughSpace;
                }
                else
                {
                    return ABS_ActionHistoryErrorCodes.Failed_PositionValidation_UsedPosition;
                }
            }

            RefreshStabilityIfNeeded(newInstance, p_ActionElementData.BuildingData.BuildingInstance);

            //Recreate destroyed connected elements
            foreach ((ABS_DestroyActionElementData connectedElementData, ABS_BuildingElementConnectionType connectionType) 
                in destroyData.DestroyedConenctedElementData)
            {
                err = DestroyUndoReq(connectedElementData, p_NewElements);
                if (err != ABS_ActionHistoryErrorCodes.Success)
                {
                    if (m_PartialProcessing)
                    {
                        continue;
                    }
                    else
                    {
                        return err;
                    }
                }

                p_NewElements.Add(connectedElementData.BuildingElementInstance);

                //The recreated connected element should rebuild its connections so we do not need it.
                //Do not reconnect the recreated connected element
            }

            //Reconnect other connections
            foreach (ABS_ActionElementConnectionData connectionData in destroyData.LostConnectionsConnectionTarget)
            {
                err = ReconnectElements(newInstance, connectionData, false);
                if (err != ABS_ActionHistoryErrorCodes.Success)
                {
                    if (m_PartialProcessing)
                    {
                        continue;
                    }
                    else
                    {
                        return err;
                    }
                }
            }

            foreach (ABS_ActionElementConnectionData connectionData in destroyData.LostConnectionsConnected)
            {
                err = ReconnectElements(newInstance, connectionData, true);
                if (err != ABS_ActionHistoryErrorCodes.Success)
                {
                    if (m_PartialProcessing)
                    {
                        continue;
                    }
                    else
                    {
                        return err;
                    }
                }
            }

            return ABS_ActionHistoryErrorCodes.Success;
        }

        private void Rollback_DestroyUndo(List<ABS_BuildingElement> p_Elements)
        {
            foreach (ABS_BuildingElement item in p_Elements)
            {
                Rollback_DestroyUndo_One(item);
            }
        }

        private void Rollback_DestroyUndo_One(ABS_BuildingElement p_Element)
        {
            p_Element.Destroy(null, true, false, true);
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // Destroy Undo
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Destroy Redo
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        //Destroy everything what built up by the Destroy Undo feature.
        private ABS_ActionHistoryErrorCodes DestroyRedo(ABS_DestroyAction p_Action)
        {
            foreach (ABS_DestroyActionElementData data in p_Action.Data)
            {
                if (data.BuildingElementInstance == null)
                {
                    ABS_ActionHistoryErrorCodes res = CheckBuildingElementInstanceAvailablity(data);
                    if (res != ABS_ActionHistoryErrorCodes.Success)
                    {
                        return res;
                    }
                }

                if (!m_Tracker.BeforeHistoryDestroy(data.BuildingElementInstance))
                {
                    if (m_PartialProcessing)
                    {
                        continue;
                    }
                    else
                    {
                        return ABS_ActionHistoryErrorCodes.Failed_DeniedByTracker;
                    }
                }
            }

            //We can jsut destroy every element
            //At this point every data must has an instance and the tracker allowed everythink
            //or it can be null but only if there where some problem but the m_PartialProcessing is wnabled so it can be ignored
            foreach (ABS_DestroyActionElementData data in p_Action.Data)
            {
                if (data.BuildingElementInstance != null)
                {
                    data.BuildingElementInstance.Destroy(m_Tracker, true, false, false);
                }
            }

            return ABS_ActionHistoryErrorCodes.Success;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // Destroy Redo
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // Destroy
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Build
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private ABS_ActionHistoryErrorCodes BuildUndo(ABS_BuildAction p_Action)
        {
            ABS_ActionHistoryErrorCodes err = ABS_ActionHistoryErrorCodes.Unkown;
                
            err = BuildUndo_CheckPossibility(p_Action);
            if (err != ABS_ActionHistoryErrorCodes.Success)
            {
                return err;
            }

            foreach (ABS_BuildActionElementData data in p_Action.Data)
            {
                err = BuildUndo_OneElement(p_Action, data);
                if (err != ABS_ActionHistoryErrorCodes.Success)
                {
                    if (m_PartialProcessing)
                    {
                        continue;
                    }
                    else
                    {
                        return err;
                    }
                }
            }

            return ABS_ActionHistoryErrorCodes.Success;
        }

        private ABS_ActionHistoryErrorCodes BuildUndo_CheckPossibility(ABS_BuildAction p_Action)
        {
            //First check every element is available and allow to be destroyed
            foreach (ABS_BuildActionElementData data in p_Action.Data)
            {
                ABS_ActionHistoryErrorCodes err = CheckBuildingElementInstanceAvailablity(data);

                if (err != ABS_ActionHistoryErrorCodes.Success)
                {
                    if (m_PartialProcessing)
                    {
                        continue;
                    }
                    else
                    {
                        return err;
                    }
                }

                if (data.DestroyedElementData != null)
                {
                    err = ChechPrefabAvailability(data.DestroyedElementData);
                    if (err != ABS_ActionHistoryErrorCodes.Success)
                    {
                        return err;
                    }
                }

                if (!m_Tracker.BeforeHistoryDestroy(data.BuildingElementInstance))
                {
                    if (m_PartialProcessing)
                    {
                        continue;
                    }
                    else
                    {
                        return ABS_ActionHistoryErrorCodes.Failed_DeniedByTracker;
                    }
                }
            }

            return ABS_ActionHistoryErrorCodes.Success;
        }

        private ABS_ActionHistoryErrorCodes BuildUndo_OneElement(ABS_BuildAction p_Action, ABS_BuildActionElementData p_ElementData)
        {
            ABS_ActionHistoryErrorCodes err = ABS_ActionHistoryErrorCodes.Unkown;

            if (p_ElementData.DestroyedElementData != null)
            {
                if (p_Action.NewBuilding)
                {
                    REST_Logging.Error($"{this}", "The Building has been created but the element has overriden an already built element which can not happened!");
                }

                err = InstantiatePrefab(p_ElementData.DestroyedElementData);
                if (err != ABS_ActionHistoryErrorCodes.Success)
                {
                    return err;
                }

                err = AddBuildingElement(p_ElementData.DestroyedElementData, true);
                if (err != ABS_ActionHistoryErrorCodes.Success)
                {
                    return err;
                }
            }
            else
            {
                ABS_DestroyActionElementData destroyData = p_ElementData.BuildingElementInstance.Destroy(m_Tracker, true, !p_Action.NewBuilding, true);
            }

            return ABS_ActionHistoryErrorCodes.Success;
        }

        private ABS_ActionHistoryErrorCodes BuildRedo(ABS_BuildAction p_Action)
        {
            ABS_ActionHistoryErrorCodes err = EnsureBuildingAvailability(
                p_Action.BuildingData,
                p_Action.BuildingElementPrefab,
                p_Action.NewBuilding);
            if (err != ABS_ActionHistoryErrorCodes.Success)
            {
                return err;
            }

            err = BuildRedo_CheckPossibility(p_Action);
            if (err != ABS_ActionHistoryErrorCodes.Success)
            {
                return err;
            }

            List<ABS_BuildingElement> createdElements = new List<ABS_BuildingElement>();
            foreach (ABS_BuildActionElementData data in p_Action.Data)
            {
                err = BuildRedo_OneElement(p_Action, data);
                if (err != ABS_ActionHistoryErrorCodes.Success)
                {
                    if (m_PartialProcessing)
                    {
                        continue;
                    }
                    else
                    {
                        return err;
                    }
                }
                createdElements.Add(data.BuildingElementInstance);

                foreach (ABS_ActionElementConnectionData connection in data.ConnectionTargets)
                {
                    err =  ReconnectElements(data.BuildingElementInstance, connection, false);
                    if (err != ABS_ActionHistoryErrorCodes.Success)
                    {
                        if (m_PartialProcessing)
                        {
                            continue;
                        }
                        else
                        {
                            return err;
                        }
                    }
                }
            }

            m_Tracker.BuildingElementHistoryPlaced(createdElements);
            return ABS_ActionHistoryErrorCodes.Success;
        }

        private ABS_ActionHistoryErrorCodes BuildRedo_CheckPossibility(ABS_BuildAction p_Action)
        {
            foreach (ABS_BuildActionElementData data in p_Action.Data)
            {
                bool IsBuildingAllowed = m_Tracker.BeforeHistoryPlace(
                    data.BuildingElementPrefab,
                    data.LocalPosition,
                    data.LocalEulerAngles,
                    data.BuildingData.BuildingInstance);
                if (!IsBuildingAllowed)
                {
                    if (m_PartialProcessing)
                    {
                        continue;
                    }
                    else
                    {
                        return ABS_ActionHistoryErrorCodes.Failed_DeniedByTracker;
                    }
                }

                ABS_ActionHistoryErrorCodes err = ChechPrefabAvailability(data);
                if (err != ABS_ActionHistoryErrorCodes.Success)
                {
                    if (m_PartialProcessing)
                    {
                        continue;
                    }
                    else
                    {
                        return err;
                    }
                }
                
                if (data.DestroyedElementData != null)
                {
                    err = CheckBuildingElementInstanceAvailablity(data.DestroyedElementData);
                    if (err != ABS_ActionHistoryErrorCodes.Success)
                    {
                        if (m_PartialProcessing)
                        {
                            continue;
                        }
                        else
                        {
                            return err;
                        }
                    }
                }
            }

            return ABS_ActionHistoryErrorCodes.Success;
        }

        private ABS_ActionHistoryErrorCodes BuildRedo_OneElement(ABS_BuildAction p_Action, ABS_BuildActionElementData p_ElementData)
        {
            ABS_ActionHistoryErrorCodes err = InstantiatePrefab(p_ElementData);
            if (err != ABS_ActionHistoryErrorCodes.Success)
            {
                return err;
            }

            return AddBuildingElement(p_ElementData, true);
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion Build
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Implementation Utils
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Implementation Utils : BuildingElement
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private ABS_ActionHistoryErrorCodes CheckBuildingElementInstanceAvailablity<BuildingDataType>(
            in ABS_ActionElementDataBase<BuildingDataType> p_ElementData)
            where BuildingDataType : ABS_ActionBuildingDataBase, new()
        {
            ABS_ActionHistoryErrorCodes err = ABS_ActionHistoryErrorCodes.Success;

            if (p_ElementData.BuildingElementInstance == null)
            {
                err = CheckBuildingAvailablity(p_ElementData.BuildingData);
                if (err != ABS_ActionHistoryErrorCodes.Success)
                {
                    p_ElementData.BuildingElementInstance = null;
                    return err;
                }

                p_ElementData.BuildingElementInstance =
                    p_ElementData.BuildingData.BuildingInstance.FindBuildingElementBasedInstanceGuid(p_ElementData.InstanceGuid);
                if (p_ElementData.BuildingElementInstance == null)
                {
                    return ABS_ActionHistoryErrorCodes.Failed_BuildingElementNotAvailable;
                }
            }

            return CheckElementProperties(p_ElementData.BuildingElementInstance, p_ElementData);
        }

        private ABS_ActionHistoryErrorCodes CheckElementProperties<BuildingDataType>(
            ABS_BuildingElement p_Element, 
            ABS_ActionElementDataBase<BuildingDataType> p_Data)
            where BuildingDataType : ABS_ActionBuildingDataBase, new()
        {
            //Check the parent object is still the same
            if (p_Element.ParentBuilding != p_Data.BuildingData.BuildingInstance)
            {
                return ABS_ActionHistoryErrorCodes.Failed_DataValidation_WrongParentObject;
            }

            if (string.Compare(p_Element.PrefabGuid, p_Data.PrefabGuid) != 0)
            {
                return ABS_ActionHistoryErrorCodes.Failed_DataValidation_WrongPrefabGuid;
            }

            if (string.Compare(p_Element.InstanceGuid, p_Data.InstanceGuid) != 0)
            {
                return ABS_ActionHistoryErrorCodes.Failed_DataValidation_WrongInstanceGuid;
            }

            //Check if the position is still the same under the parent
            if (!REST_Vector3EqualityComparer.Static_Equals(p_Element.transform.localPosition, p_Data.LocalPosition))
            {
                return ABS_ActionHistoryErrorCodes.Failed_DataValidation_WrongLocalPosition;
            }

            //Check if the rotation is still the same under the parent
            if (!REST_Vector3EqualityComparer.Static_Equals(p_Element.transform.localEulerAngles, p_Data.LocalEulerAngles))
            {
                return ABS_ActionHistoryErrorCodes.Failed_DataValidation_WrongLocalEulerAngles;
            }

            return ABS_ActionHistoryErrorCodes.Success;
        }

        private ABS_ActionHistoryErrorCodes ChechPrefabAvailability<BuildingDataType>(
            ABS_ActionElementDataBase<BuildingDataType> p_ElementData)
            where BuildingDataType : ABS_ActionBuildingDataWithPropertiesBase, new()
        {
            ABS_BuildingElement prefab = p_ElementData.BuildingElementPrefab;
            if (prefab == null)
            {
                if(!m_ElementList.BuildingElementsDict.TryGetValue(p_ElementData.PrefabGuid, out prefab) || prefab == null)
                {
                    REST_Logging.Warrning($"{this}",
                        $"Can not instantiate the element " +
                        $"because of the element's guid is not included by the ABS_BuildingElement List. " +
                        $"Prefabguid : {p_ElementData.PrefabGuid}");
                    return ABS_ActionHistoryErrorCodes.Failed_DataValidation_UnkownGUID;
                }
                p_ElementData.BuildingElementPrefab = prefab;
            }

            return ABS_ActionHistoryErrorCodes.Success;
        }

        private ABS_ActionHistoryErrorCodes InstantiatePrefab<BuildingDataType>(
            ABS_ActionElementDataBase<BuildingDataType> p_ElementData)
            where BuildingDataType : ABS_ActionBuildingDataWithPropertiesBase, new()
        {
            if (p_ElementData.BuildingElementInstance != null)
            {
                REST_Logging.Error($"{this}", $"The Instance is already exists.\n" +
                    $"\nName: {p_ElementData.BuildingElementInstance.name} " +
                    $"\nInstanceGuid: {p_ElementData.BuildingElementInstance.InstanceGuid}");
                return ABS_ActionHistoryErrorCodes.Unkown;
            }

            p_ElementData.BuildingElementInstance = ABS_BuildingElement.InstantiateClone(p_ElementData.BuildingElementPrefab);
            p_ElementData.BuildingElementInstance.InstanceGuid = p_ElementData.InstanceGuid;
            p_ElementData.BuildingElementInstance.PreBuilt = p_ElementData.Prebuilt;
            p_ElementData.BuildingElementInstance.Stable = p_ElementData.Stable;
            p_ElementData.BuildingElementInstance.StabilityLevel = p_ElementData.Stability;

            return ABS_ActionHistoryErrorCodes.Success;
        }

        private ABS_ActionHistoryErrorCodes AddBuildingElement<BuildingDataType>(
            ABS_ActionElementDataBase<BuildingDataType> p_ElementData, 
            bool p_DestroyOldObject)
            where BuildingDataType : ABS_ActionBuildingDataBase, new()
        {
            if (p_ElementData.BuildingData.BuildingInstance.AddBuildingElement(
                    p_Tracker : m_Tracker,
                    p_TriggeredByHistory : true,
                    p_NewElement : p_ElementData.BuildingElementInstance,
                    p_LocalPosition: p_ElementData.LocalPosition,
                    p_LocalEulerAngles: p_ElementData.LocalEulerAngles,
                    p_Force : p_DestroyOldObject,
                    p_DestroyOld: p_DestroyOldObject) == null)
            {
                return ABS_ActionHistoryErrorCodes.Failed_PositionValidation_UsedPosition;
            }
            return ABS_ActionHistoryErrorCodes.Success;
        }

        private ABS_ActionHistoryErrorCodes ReconnectElements(
            in ABS_BuildingElement p_RecreatedElement,
            in ABS_ActionElementConnectionData p_ConnectionData,
            in bool p_IsElementTheTarget)
        {
            if (p_RecreatedElement == null)
            {
                return ABS_ActionHistoryErrorCodes.Unkown;
            }

            if (p_ConnectionData.BuildingElementInstance == null)
            {
                ABS_ActionHistoryErrorCodes res = CheckBuildingElementInstanceAvailablity(p_ConnectionData);
                if (res != ABS_ActionHistoryErrorCodes.Success)
                {
                    return res;
                }
            }

            if (p_IsElementTheTarget)
            {
                p_RecreatedElement.ConnectElement(p_ConnectionData.ConnectionType, p_ConnectionData.BuildingElementInstance);
            }
            else
            {
                p_ConnectionData.BuildingElementInstance.ConnectElement(p_ConnectionData.ConnectionType, p_RecreatedElement);
            }

            return ABS_ActionHistoryErrorCodes.Success;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // Implementation Utils : BuildingElement
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region Implementation Utils : Building
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private void RefreshStabilityIfNeeded (ABS_BuildingElement p_ElementInstance, ABS_Building p_BuildlingInstance)
        {
            if (p_ElementInstance.PositionAlgorithmSettings.IsStabilitySupported())
            {
                switch (p_ElementInstance.PositionSearchAlgorithm)
                {
                    case ABS_PositionSearchAlgorithm.AdvancedGrid:
                        {
                            ABS_AdvancedGridBuilding building = (p_BuildlingInstance as ABS_AdvancedGridBuilding);
                            building.RefreshStabilityForElement(p_ElementInstance);
                        }
                        return;
                    case ABS_PositionSearchAlgorithm.SnapPointBased:
                    case ABS_PositionSearchAlgorithm.BasicGrid:
                    case ABS_PositionSearchAlgorithm.Free:
                    default:
                        return;
                }
            }
        }

        private ABS_ActionHistoryErrorCodes CheckBuildingAvailablity(ABS_ActionBuildingDataBase p_BuildingData)
        {
            if (p_BuildingData.BuildingInstance == null)
            {
                if (p_BuildingData.BuildingParent == null)
                {
                    return ABS_ActionHistoryErrorCodes.Failed_BuildingParentNotAvailable;
                }

                p_BuildingData.BuildingInstance =
                    p_BuildingData.BuildingParent.GetBuilding(p_BuildingData.BuildingInstanceGuid);
                if (p_BuildingData.BuildingInstance == null)
                {
                    return ABS_ActionHistoryErrorCodes.Failed_BuildingNotAvailable;
                }
            }

            return ABS_ActionHistoryErrorCodes.Success;
        }

        private ABS_ActionHistoryErrorCodes EnsureBuildingAvailability (
            in ABS_ActionBuildingDataWithPropertiesBase p_BuildingData, 
            in ABS_BuildingElement p_TargetElement,
            in bool p_CreationIsNeeded)
        {
            if (p_CreationIsNeeded && p_BuildingData.BuildingInstance == null)
            {
                ABS_ActionHistoryErrorCodes err = ReCreateBuilding(p_BuildingData, p_TargetElement);
                if (err != ABS_ActionHistoryErrorCodes.Success)
                {
                    return err;
                }
            }

            return CheckBuildingAvailablity(p_BuildingData);
        }

        private ABS_ActionHistoryErrorCodes ReCreateBuilding (in ABS_ActionBuildingDataWithPropertiesBase p_Data, in ABS_BuildingElement p_TargetElement)
        {
            if (p_Data.BuildingParent == null)
            {
                return ABS_ActionHistoryErrorCodes.Failed_BuildingParentNotAvailable;
            }

            p_Data.BuildingInstance = p_Data.BuildingParent.GetParentForNewBuildingElementHistory(
                p_TargetElement: p_TargetElement,
                p_Position : p_Data.ParentPosition,
                p_EulerAngles : p_Data.ParentEulerAngles,
                p_Manager : m_Manager,
                p_Tracker : m_Tracker,
                p_BuildingInstanceGuid: p_Data.BuildingInstanceGuid);

            p_Data.BuildingInstance.MaximumElementCount = p_Data.MaximumElementCount;
            p_Data.BuildingInstance.EnableCache = p_Data.UseCache;

            return ABS_ActionHistoryErrorCodes.Success;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // Implementation Utils : Building
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion // Implementation Utils
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    }
}

