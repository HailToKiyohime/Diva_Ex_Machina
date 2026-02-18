//*********************************************************************
//  Dependencies: System
using System;
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEngine;
using UnityEditor;

//  Dependencies: REST

//*********************************************************************


namespace REST.AdvancedBuildSystem.Editor
{
    internal enum ABS_Statistics_DataCategory
    {
        Raycast,
        Overlap,
        Object,
        AdvancedGridBuilding
    }

    internal enum ABS_Statistics_DataType_Basics
    {
        Processed_Time,
        Processed_Frames,
        AVG_Frame_Time
    }

    internal enum ABS_Statistics_DataType_Raycast
    {
        Raycast_Count,
        AVG_Raycast_Count
    }

    internal enum ABS_Statistics_DataType_Overlap
    {
        Overlap_Check_Count,
        AVG_Overlap_Check_Count
    }

    internal enum ABS_Statistics_DataType_Object
    {
        Instantiated_Object_Count,
        Destroyed_Object_Count
    }

    internal enum ABS_Statistics_DataType_AdvancedGridBuilding
    { 
        AVG_Checked_Building,
        AVG_checked_BuildingElements,
        AVG_All_Checked_SnapPoints,
        AVG_SnapPoints_Discarded_By_Range,
        AVG_Failed_Validation,
        AVG_Failed_Custom_Validation,
        AVG_High_Impact_SnapPoints,
        AVG_Validated_SnapPoints,
        Successful_First_SnapPoint_Check
    }

    internal class ABS_Statistics : ABS_EditorWindowBase, ABS_IBuildingManagerStatisticsTracker
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Nested Class
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public class StatisticsData
        {
            public string m_Name = string.Empty;
            public string m_Measure = string.Empty;

            public float m_LastValue = 0f;
            public float m_Min = float.MaxValue;
            public float m_Max = float.MinValue;
            public float m_Avarage = 0f;
            public float m_AvarageCounter = 0f;
            public float m_Summary = 0f;

            private List<float> m_ValueBuffer = new List<float>();

            public StatisticsData(in string p_Name, in string p_Measure)
            {
                m_Name = p_Name;
                m_Measure = p_Measure;
            }

            public List<float> ValueBuffer
            {
                get { return m_ValueBuffer; }
            }

            public void AddData (float p_NewValue)
            {
                m_ValueBuffer.Add(p_NewValue);
                if (m_ValueBuffer.Count > 20)
                {
                    m_ValueBuffer.RemoveAt(0);
                }

                m_LastValue = p_NewValue;

                if (p_NewValue < m_Min)
                {
                    m_Min = p_NewValue;
                }

                if (p_NewValue > m_Max)
                {
                    m_Max = p_NewValue;
                }

                ++m_AvarageCounter;
                m_Summary += p_NewValue;
                m_Avarage = m_Summary / m_AvarageCounter;
            }

            public void Reset ()
            {
                m_LastValue = 0;
                m_Min = float.MaxValue;
                m_Max = float.MinValue;
                m_Avarage = 0;
                m_AvarageCounter = 0;
                m_Summary = 0;
            }

            public string[] GetData ()
            {
                if (string.IsNullOrEmpty(m_Measure))
                {
                    return new string[] {
                        m_Name,
                        Math.Round(m_LastValue, 3).ToString(),
                        Math.Round(m_Min, 3).ToString(),
                        Math.Round(m_Max, 3).ToString(),
                        Math.Round(m_Avarage, 3).ToString(),
                        Math.Round(m_Summary, 3).ToString()
                    };
                }
                else
                {
                    return new string[] {
                        m_Name,
                        $"{Math.Round(m_LastValue, 3)}({m_Measure})",
                        $"{Math.Round(m_Min, 3)}({m_Measure})",
                        $"{Math.Round(m_Max, 3)}({m_Measure})",
                        $"{Math.Round(m_Avarage, 3)}({m_Measure})",
                        $"{Math.Round(m_Summary, 3)}({m_Measure})"
                    };
                }
            }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private List<ABS_BuildingManager> m_ObjectsWithManagers = new List<ABS_BuildingManager>();
        private ABS_BuildingManager m_Manager = null;
        private GUIContent m_ManagerGUIContent;

        private bool m_Problem = false;
        private bool m_Registrated = false;

        private ABS_EditorTableView m_BasicsTable = null;
        private ABS_EditorTableView m_RaycastTable = null;
        private ABS_EditorTableView m_OverlapTable = null;
        private ABS_EditorTableView m_ObjectTable = null;

        private Vector2 m_ScrollPos;

        private StatisticsData m_StatisticsTimerData = new StatisticsData("Processed time", "ms");
        private StatisticsData m_StatisticsFrameData = new StatisticsData("Processed frames", null);
        private StatisticsData m_StatisticsAVGFrameData = new StatisticsData("AVG frame time", "ms");
        private StatisticsData m_StatisticsRaycastCountData = new StatisticsData("Raycast count", null);
        private StatisticsData m_StatisticsAVGRaycastCountData = new StatisticsData("AVG raycast count", null);
        private StatisticsData m_StatisticsOverlapCheckData = new StatisticsData("Overlap check", null);
        private StatisticsData m_StatisticsAVGOverlapCheckData = new StatisticsData("AVG Overlap check", null);
        private StatisticsData m_StatisticsInstantiateObjectData = new StatisticsData("Instantiate Object", null);
        private StatisticsData m_StatisticsDestroyObjectData = new StatisticsData("Destroy Object", null);

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  EditorWindowBase Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public static void ShowWindow()
        {
            GetWindow<ABS_Statistics>("ABS_Statistics");
        }

        public void OnEnable()
        {
            m_ManagerGUIContent = new GUIContent("Manager", "The ABS_BuildingManager analysed by this tool.");
            m_BasicsTable = new ABS_EditorTableView(6, 3, new string[] { "Data ", "Last Value", "Min", "Max", "Average", "Summary"});
            m_RaycastTable = new ABS_EditorTableView(6, 2, new string[] { "Data ", "Last Value", "Min", "Max", "Average", "Summary"});
            m_OverlapTable = new ABS_EditorTableView(6, 2, new string[] { "Data ", "Last Value", "Min", "Max", "Average", "Summary"});
            m_ObjectTable = new ABS_EditorTableView(6, 2, new string[] { "Data ", "Last Value", "Min", "Max", "Average", "Summary"});
        }

        public void OnDisable()
        {
            if (m_Manager)
            {
                m_Manager.UnRegistrateStatTracker(this);
            }
        }
        public void OnDestroy ()
        {
            if (m_Manager)
            {
                m_Manager.UnRegistrateStatTracker(this);
            }
        }

        protected override void OnGUIImpl()
        {
            AddHeaderSection("Statistics");

            m_ScrollPos = ABS_EditorUtils.StartScrollView(m_ScrollPos);
            {
                ABS_EditorUtils.Space();
                AddDataSection();
                ABS_EditorUtils.Space();
                ABS_EditorUtils.AddSeparatorLine();
                ABS_EditorUtils.Space();

                if (!m_Problem && m_Registrated)// && Application.isPlaying)
                {
                    if (m_Manager)
                    {
                        AddStatusInfoSection();
                        ABS_EditorUtils.AddSeparatorLine();
                        ABS_EditorUtils.Space();
                    }

                    AddBasicDataSection();
                    ABS_EditorUtils.Space();
                    AddRaycastDataSection();
                    ABS_EditorUtils.Space();
                    AddOverlapDataSection();
                    ABS_EditorUtils.Space();
                    AddObjectDataSection();
                }
            }
            ABS_EditorUtils.EndScrollView();
        }

        private void AddDataSection()
        {
            ABS_BuildingManager manager = ABS_EditorUtils.AddObjectField(m_ManagerGUIContent, m_Manager, true);
            if (manager == null)
            {
                EditorGUILayout.HelpBox("Missing Manager!", MessageType.Error);

                if (m_Manager != null)
                {
                    m_Manager.UnRegistrateStatTracker(this);
                }
                m_Manager = null;
                m_Registrated = false;
                m_Problem = true;
            }
            else
            {
                if (m_Manager != manager)
                {
                    if (m_Manager != null)
                    {
                        m_Manager.UnRegistrateStatTracker(this);
                    }

                    m_Manager = manager;
                    m_Manager.RegistrateStatTracker(this);

                    m_Registrated = true;
                    m_Problem = false;
                }

                ABS_EditorUtils.Space();
                bool buttonResult = GUILayout.Button("Reset", m_EditorStyleContainer.SmallDarkButtonStyle, GUILayout.Width(120));
                if (buttonResult)
                {
                    m_BasicsTable.Reset();
                    m_RaycastTable.Reset();
                    m_OverlapTable.Reset();
                    m_ObjectTable.Reset();

                    m_StatisticsTimerData.Reset();
                    m_StatisticsFrameData.Reset();
                    m_StatisticsAVGFrameData.Reset();
                    m_StatisticsRaycastCountData.Reset();
                    m_StatisticsAVGRaycastCountData.Reset();
                    m_StatisticsOverlapCheckData.Reset();
                    m_StatisticsAVGOverlapCheckData.Reset();
                    m_StatisticsInstantiateObjectData.Reset();
                    m_StatisticsDestroyObjectData.Reset();
                }
            }
        }

        private void AddStatusInfoSection()
        {
            EditorGUILayout.LabelField("Status", m_EditorStyleContainer.HeadStyleSection);


            GUIStyle coloredTextStyle = new GUIStyle(GUI.skin.label);
            string colorizesString = ABS_EditorStyleContainer.ColorizeText(
                ref coloredTextStyle,
                (m_Manager.ActiveStatus() != ABS_BuildingManagerActiveStatus.Inactive ? "Active" : "Inactive"),
                (m_Manager.ActiveStatus() != ABS_BuildingManagerActiveStatus.Inactive ? ABS_EditorStyleContainer.s_GreenColor : ABS_EditorStyleContainer.s_RedColor));
            EditorGUILayout.LabelField($"Active  :  {colorizesString}", coloredTextStyle);
            EditorGUILayout.LabelField($"Mode  :  {m_Manager.GetMode()}");

            ABS_BuildingElement element = m_Manager.GetActiveBuildingElement();
            if (element == null)
            {
                EditorGUILayout.LabelField("Active ABS_BuildingElement : NULL");
            }
            else
            {
                EditorGUILayout.LabelField("Active ABS_BuildingElement : ");

                ABS_EditorUtils.AddBuildingElementDataLine(element.gameObject, element.name, element.PrefabGuid);
            }
        }

        private void AddBasicDataSection()
        {
            EditorGUILayout.LabelField("Basics", m_EditorStyleContainer.HeadStyleSection);
            m_BasicsTable.CreateTable(
                m_EditorStyleContainer,
                new StatisticsData[] {
                    m_StatisticsTimerData,
                    m_StatisticsFrameData,
                    m_StatisticsAVGFrameData
            });
        }

        private void AddRaycastDataSection()
        { 
            EditorGUILayout.LabelField("Raycast", m_EditorStyleContainer.HeadStyleSection);
            m_RaycastTable.CreateTable(
                m_EditorStyleContainer,
                new StatisticsData[] {
                    m_StatisticsRaycastCountData,
                    m_StatisticsAVGRaycastCountData
            });
        }

        private void AddOverlapDataSection()
        {
            EditorGUILayout.LabelField("Overlap", m_EditorStyleContainer.HeadStyleSection);
            m_OverlapTable.CreateTable(
                m_EditorStyleContainer,
                new StatisticsData[] {
                    m_StatisticsOverlapCheckData,
                    m_StatisticsAVGOverlapCheckData
            });
        }
        
        private void AddObjectDataSection()
        {
            EditorGUILayout.LabelField("Object", m_EditorStyleContainer.HeadStyleSection);
            m_ObjectTable.CreateTable(
                m_EditorStyleContainer,
                new StatisticsData[] {
                    m_StatisticsInstantiateObjectData,
                    m_StatisticsDestroyObjectData
            });
        }

        private void FindAllBuildingManager()
        {
            ABS_BuildingManager[] managers = Resources.FindObjectsOfTypeAll<ABS_BuildingManager>();
            m_ObjectsWithManagers = new List<ABS_BuildingManager>(managers);
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  ABS_IBuildingManagerStatisticsTracker Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public void StatisticsDataCallback(ABS_BuildingManagerStatisticsData p_StatisticsData)
        {
            m_StatisticsTimerData.AddData((int)(p_StatisticsData.StatisticsSummary));
            m_StatisticsFrameData.AddData(p_StatisticsData.StatisticsFrameCounter);
            //TODO
            //m_StatisticsAVGFrameData.AddData(p_StatisticsData.StatisticsSummary / p_StatisticsData.StatisticsFrameCounter);

            m_StatisticsRaycastCountData.AddData(p_StatisticsData.StatisticsRaycastCounter);
            //m_StatisticsAVGRaycastCountData.AddData(p_StatisticsData.StatisticsRaycastCounter / p_StatisticsData.StatisticsFrameCounter);

            m_StatisticsOverlapCheckData.AddData(p_StatisticsData.StatisticsOverlapCheckCount);
            //m_StatisticsAVGOverlapCheckData.AddData(p_StatisticsData.StatisticsOverlapCheckCount / p_StatisticsData.StatisticsFrameCounter);

            m_StatisticsInstantiateObjectData.AddData(p_StatisticsData.StatisticsInstantiatedObject);
            m_StatisticsDestroyObjectData.AddData(p_StatisticsData.StatisticsDestroyedObject);

            this.Repaint();
        }
    }
}