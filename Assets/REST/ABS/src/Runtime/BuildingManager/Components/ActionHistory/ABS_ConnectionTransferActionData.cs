//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity

//  Dependencies: REST

//*********************************************************************


namespace REST.AdvancedBuildSystem
{
    public class ABS_ConnectionTransferActionData
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        //The element was connected to these elements
        private List<ABS_ActionElementConnectionData> m_LostConnectionsConnectionTarget = null;

        //These elements was connect to the element
        private List<ABS_ActionElementConnectionData> m_LostConnectionsConnected = null;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Getter / Setter
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public List<ABS_ActionElementConnectionData> LostConnectionsConnectionTarget => m_LostConnectionsConnectionTarget;
        public List<ABS_ActionElementConnectionData> LostConnectionsConnected => m_LostConnectionsConnected;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public void AddConenctionTargetData(List<ABS_ActionElementConnectionData> p_Data)
        {
            m_LostConnectionsConnectionTarget = p_Data;
        }

        public void AddConnectedData(List<ABS_ActionElementConnectionData> p_Data)
        {
            m_LostConnectionsConnected = p_Data;
        }
    }
}