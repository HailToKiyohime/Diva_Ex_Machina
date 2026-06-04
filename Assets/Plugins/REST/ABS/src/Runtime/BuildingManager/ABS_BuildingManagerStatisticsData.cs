//*********************************************************************
//  Dependencies: System
using System;

//  Dependencies: Unity
using UnityEngine;

//  Dependencies: REST
using REST.Utils;

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public class ABS_BuildingManagerStatisticsData
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        //Basics
        private long m_StatisticsTimer = 0;
        private long m_StatisticsSummary = 0;
        private long m_StatisticsFrameCounter = 0;

        //Raycast
        private ulong m_StatisticsRaycastCounter = 0;

        //OverlapCheck  
        private ulong m_StatisticsOverlapCheckCount = 0;

        //OverlapCheck
        private ulong m_StatisticsInstantiatedObject = 0;
        private ulong m_StatisticsDestroyedObject = 0;


        private static string s_StatisticsMessageFormat =
            "\n-----------------------------------------------" +
            "\n Basics " +
            "\n-----------------------------------------------" +
            "\nMeasurement  Time : {0} s | {1} ms | {2} ns" +
            "\nSummary Of Update Process Time :  {3} s | {4} ms | {5} ns" +
            "\nSummary Of Update Process Count : {6}" +
            "\nAVG Of Update Process Time : {7} s | {8} ms | {9} ns" +
            "\n-----------------------------------------------" +
            "\n Raycast " +
            "\n-----------------------------------------------" +
            "\nRaycast count : {10}" +
            "\nAVG raycast count : {11}" +
            "\n-----------------------------------------------" +
            "\n Overlap " +
            "\n-----------------------------------------------" +
            "\nOverlap check count : {12}" +
            "\nAVG overlap check count : {13}" +
            "\n-----------------------------------------------" +
            "\n Object " +
            "\n-----------------------------------------------" +
            "\nInstantiated object count : {14}" +
            "\nDestroyed object count : {15}" +
            "\n";

        private static string s_StatisticsNumberColorFormat = "<color=#FFFFFF>{0}</color>";

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Getter / Setter
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public long StatisticsTimer
        {
            get { return m_StatisticsTimer; }
            set { m_StatisticsTimer = value; }
        }

        public long StatisticsSummary
        {
            get { return m_StatisticsSummary; }
            set { m_StatisticsSummary = value; }
        }

        public long StatisticsFrameCounter
        {
            get { return m_StatisticsFrameCounter; }
            set { m_StatisticsFrameCounter = value; }
        }

        public ulong StatisticsRaycastCounter
        {
            get { return m_StatisticsRaycastCounter; }
            set { m_StatisticsRaycastCounter = value; }
        }

        public ulong StatisticsOverlapCheckCount
        {
            get { return m_StatisticsOverlapCheckCount; }
            set { m_StatisticsOverlapCheckCount = value; }
        }

        public ulong StatisticsInstantiatedObject
        {
            get { return m_StatisticsInstantiatedObject; }
            set { m_StatisticsInstantiatedObject = value; }
        }

        public ulong StatisticsDestroyedObject
        {
            get { return m_StatisticsDestroyedObject; }
            set { m_StatisticsDestroyedObject = value; }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public void Print()
        {
            float AVGOfUpdateProcessTime = m_StatisticsSummary / m_StatisticsFrameCounter;

            REST_Logging.Debug("ABS_BuildingManagerStatisticsData",
                   String.Format(
                   s_StatisticsMessageFormat,

                   String.Format(s_StatisticsNumberColorFormat, ConvertToSeconds(m_StatisticsTimer)),
                   String.Format(s_StatisticsNumberColorFormat, ConvertToMiliseconds(m_StatisticsTimer)),
                   String.Format(s_StatisticsNumberColorFormat, m_StatisticsTimer),

                   String.Format(s_StatisticsNumberColorFormat, ConvertToSeconds(m_StatisticsSummary)),
                   String.Format(s_StatisticsNumberColorFormat, ConvertToMiliseconds(m_StatisticsSummary)),
                   String.Format(s_StatisticsNumberColorFormat, m_StatisticsSummary),

                   String.Format(s_StatisticsNumberColorFormat, m_StatisticsFrameCounter),

                   String.Format(s_StatisticsNumberColorFormat, ConvertToSeconds(AVGOfUpdateProcessTime)),
                   String.Format(s_StatisticsNumberColorFormat, ConvertToMiliseconds(AVGOfUpdateProcessTime)),
                   String.Format(s_StatisticsNumberColorFormat, AVGOfUpdateProcessTime),

                   String.Format(s_StatisticsNumberColorFormat, m_StatisticsRaycastCounter),
                   String.Format(s_StatisticsNumberColorFormat, Math.Round((float)m_StatisticsRaycastCounter / m_StatisticsFrameCounter,  2)),
                   String.Format(s_StatisticsNumberColorFormat, m_StatisticsOverlapCheckCount),
                   String.Format(s_StatisticsNumberColorFormat, Math.Round((float)m_StatisticsOverlapCheckCount / m_StatisticsFrameCounter,  2)),
                   String.Format(s_StatisticsNumberColorFormat, m_StatisticsInstantiatedObject),
                   String.Format(s_StatisticsNumberColorFormat, m_StatisticsDestroyedObject)));
        }

        private float ConvertToSeconds (in long p_Nanoseconds)
        {
            return Mathf.Round(p_Nanoseconds / 1000_000f) / 1000f;
        }

        private float ConvertToMiliseconds(in long p_Nanoseconds)
        {
            return Mathf.Round(p_Nanoseconds / 1000f) / 1000f;
        }
        private float ConvertToSeconds(in float p_Nanoseconds)
        {
            return Mathf.Round(p_Nanoseconds / 1000_000f) / 1000f;
        }

        private float ConvertToMiliseconds(in float p_Nanoseconds)
        {
            return Mathf.Round(p_Nanoseconds / 1000f) / 1000f;
        }
    }
}