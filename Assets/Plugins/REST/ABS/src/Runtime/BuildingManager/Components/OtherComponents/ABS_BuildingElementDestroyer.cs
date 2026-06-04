//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public enum DestroyType
    {
        Instant,
        Timer
    }

    public class ABS_BuildingElementDestroyer : ABS_BuildingManagerComponentBase
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private float m_DestroyTimer;
        private bool m_DestroyIsOngoing = false;
        private bool m_BufferingIsOngoing = false;
        private ABS_BuildingElement m_LastHitBuildingElement = null;
        private List<ABS_BuildingElement> m_LastHitBuildingElementBuffer = new List<ABS_BuildingElement>();

        private ABS_ActionHistory m_ActionHistory = null;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Initialization
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public ABS_BuildingElementDestroyer(
            ABS_IBuildingManagerInternalInterface p_Manager, 
            ABS_BuildingManagerTracker p_Tracker, 
            ABS_ActionHistory p_ActionHistory)
            : base(p_Manager, p_Tracker)
        {
            m_ActionHistory = p_ActionHistory;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Public Function
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public void ResetDestroyer()
        {
            StopDestroying();
            ResetLastHitBuildingElement();
            ResetAllHitBuildingElement();
        }

        public void UpdateDestroyLogic()
        {
            if (!m_DestroyIsOngoing)
            {
                HandleRaycastHit();
            }
        }

        public void DestroyKeyIsPressed()
        {
            if (m_BufferingIsOngoing)
            {
                List<ABS_BuildingElement> canceledDestroy = new List<ABS_BuildingElement>();
                foreach (ABS_BuildingElement element in m_LastHitBuildingElementBuffer)
                {
                    if (!CanDestroyed(element))
                    {
                        canceledDestroy.Add(element);
                    }
                }

                foreach (ABS_BuildingElement canceledElement in canceledDestroy)
                {
                    canceledElement.State = ABS_BuildingElementState.NORMAL;
                    m_LastHitBuildingElementBuffer.Remove(canceledElement);
                }

                if (m_LastHitBuildingElementBuffer.Count > 0)
                {
                    if (m_Manager.DestroyType == DestroyType.Timer)
                    {
                        //start timer
                        m_DestroyTimer = m_Manager.DestroyTimerDuration;
                        m_DestroyIsOngoing = true;

                        m_Tracker.DestroyTimerStarted(m_Manager.DestroyTimerDuration, m_LastHitBuildingElementBuffer);
                    }
                    else
                    {
                        Destory();
                    }
                }
            }
            else
            {
                if (m_LastHitBuildingElement != null)
                {
                    if (!CanDestroyed(m_LastHitBuildingElement))
                    {
                        m_LastHitBuildingElement.State = ABS_BuildingElementState.NORMAL;
                        m_LastHitBuildingElement = null;
                    }
                    else
                    {
                        if (m_Manager.DestroyType == DestroyType.Timer)
                        {
                            //start timer
                            m_DestroyTimer = m_Manager.DestroyTimerDuration;
                            m_DestroyIsOngoing = true;
                            m_Tracker.DestroyTimerStarted(m_Manager.DestroyTimerDuration, new List<ABS_BuildingElement> { m_LastHitBuildingElement });
                        }
                        else
                        {
                            Destory();
                        }
                    }
                }
            }
        }

        public void DestroyKeyIsHeld()
        {
            if (m_DestroyIsOngoing)
            {
                if (m_Manager.CutTimerOnLookAway
                    && (m_Manager.Raycaster.HitTransform == null
                        || m_Manager.Raycaster.HitTransform.gameObject != m_LastHitBuildingElement.gameObject))
                {
                    ResetDestroyer();
                    return;
                }

                m_DestroyTimer -= Time.deltaTime;
                if (m_DestroyTimer <= 0)
                {
                    Destory();
                    m_BufferingIsOngoing = false;
                }
                else
                {
                    m_Tracker.DestroyTimerIsOngoing(m_DestroyTimer);
                }
            }
        }

        public void DestroyKeyIsReleased()
        {
            if (m_DestroyIsOngoing)
            {
                m_DestroyTimer -= Time.deltaTime;
                if (m_DestroyTimer <= 0)
                {
                    Destory();
                    m_BufferingIsOngoing = false;
                }
                else
                {
                    StopDestroying();
                }
            }
        }

        public void DragKeyIsPressed()
        {
            if (m_DestroyIsOngoing)
            {
                return;
            }

            if (m_LastHitBuildingElement != null
                    && !m_LastHitBuildingElementBuffer.Contains(m_LastHitBuildingElement))
            {
                m_LastHitBuildingElementBuffer.Add(m_LastHitBuildingElement);
            }

            m_BufferingIsOngoing = true;
        }

        public void DragKeyIsReleased()
        {
            if (m_DestroyIsOngoing)
            {
                return;
            }

            ResetAllHitBuildingElement();
        }


        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Private Function
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private bool CanDestroyed(ABS_BuildingElement p_Element)
        {
            return m_Tracker.BeforeDestroy(p_Element)
                    && (p_Element.ConnectedElements.Count == 0 
                        || m_Tracker.CanBuildingElementWithConnectionsDestroyed(p_Element, p_Element.ConnectedElements));
        }

        private void HandleRaycastHit ()
        {
            Transform hit = m_Manager.Raycaster.HitTransform;
            if (hit == null
                || hit.gameObject == null
                || (m_Manager.LayerCollection.LayerOfBuildingElement !=
                    (m_Manager.LayerCollection.LayerOfBuildingElement | (1 << hit.gameObject.layer))))
            {
                ResetLastHitBuildingElement();
            }

            ABS_BuildingElement newHitElement = m_Manager.Raycaster.BuildingElement;

            if (m_LastHitBuildingElement == null
                || newHitElement == null
                || m_LastHitBuildingElement.gameObject != newHitElement.gameObject)
            {
                ResetLastHitBuildingElement();

                if (newHitElement != null && !newHitElement.Indestructible)
                {
                    m_LastHitBuildingElement = newHitElement;
                    if (m_BufferingIsOngoing && !m_LastHitBuildingElementBuffer.Contains(m_LastHitBuildingElement))
                    {
                        if (m_LastHitBuildingElementBuffer.Count < m_Manager.MaximumDestoryCount)
                        {
                            m_LastHitBuildingElement.State = ABS_BuildingElementState.SIGNEDFORDELETE;
                            m_LastHitBuildingElementBuffer.Add(m_LastHitBuildingElement);
                        }
                    }
                    else
                    {
                        m_LastHitBuildingElement.State = ABS_BuildingElementState.SIGNEDFORDELETE;
                    }
                }
            }
        }


        private void Destory()
        {
            if (m_BufferingIsOngoing)
            {
                ABS_DestroyAction action = new ABS_DestroyAction(m_Tracker);
                foreach (ABS_BuildingElement element in m_LastHitBuildingElementBuffer)
                {
                    if (element != null && CanDestroyed(element))
                    {
                        ABS_DestroyActionElementData actionData = element.Destroy(m_Tracker, false, false, false);
                        action.AddData(actionData);
                    }
                }

                //If No element passed the custom validation we do not add an empty action to the history
                if (action.Data.Count != 0)
                {
                    m_ActionHistory.AddAction(action);
                }
                m_LastHitBuildingElementBuffer.Clear();
            }
            else
            {
                if (CanDestroyed(m_LastHitBuildingElement))
                {
                    ABS_DestroyAction action = new ABS_DestroyAction(m_Tracker);
                    ABS_DestroyActionElementData actionData = m_LastHitBuildingElement.Destroy(m_Tracker, false, false, false);
                    action.AddData(actionData);
                    m_ActionHistory.AddAction(action);
                }
                m_LastHitBuildingElement = null;
            }
            StopDestroying();
        }

        private void StopDestroying()
        {
            if (m_DestroyIsOngoing)
            {
                m_Tracker.DestroyTimerStoped();
            }
            m_DestroyIsOngoing = false;
        }

        private void ResetLastHitBuildingElement()
        {
            if (!m_DestroyIsOngoing)
            {
                if (m_LastHitBuildingElement)
                {
                    if (!m_BufferingIsOngoing)
                    {
                        m_LastHitBuildingElement.State = ABS_BuildingElementState.NORMAL;
                    }
                    m_LastHitBuildingElement = null;
                }
            }
        }

        private void ResetAllHitBuildingElement()
        {
            foreach (ABS_BuildingElement element in m_LastHitBuildingElementBuffer)
            {
                if (m_LastHitBuildingElement == null || element != m_LastHitBuildingElement)
                {
                    element.State = ABS_BuildingElementState.NORMAL;
                }
            }
            m_LastHitBuildingElementBuffer.Clear();
            m_BufferingIsOngoing = false;
        }
    }
}