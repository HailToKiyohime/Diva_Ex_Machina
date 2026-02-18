//*********************************************************************
//  Dependencies: System
using System.Collections.Generic;

//  Dependencies: Unity
using UnityEditor;
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem.Editor
{
    internal abstract class ABS_EntityListEditorBase<EntityListHolderImpl, EntityImpl> : ABS_EditorBase
        where EntityListHolderImpl : class, ABS_IEntityListHolder
        where EntityImpl : class, ABS_IEntity, new()
    {
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Nested Classes
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        protected enum State
        {
            Normal,
            EditMode,
            EntityEditMode
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        protected EntityListHolderImpl m_EntityListHolder = null;
        protected EntityListBase<EntityImpl> m_EntityList = null;
        protected State m_State = State.Normal;

        protected List<bool> m_IsSectionOpen = new List<bool>();

        protected int m_EntityIndexForEdit = -1;
        private List<int> m_EntitiesForRemove = new List<int>();
        private List<int> m_EntitiesForAdd = new List<int>();
        private List<int> m_EntitiesForTop = new List<int>();
        private List<int> m_EntitiesForUp = new List<int>();
        private List<int> m_EntitiesForDown = new List<int>();
        private List<int> m_EntitiesForBottom = new List<int>();

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  ABS_EditorBase Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        protected override void OnEnableImpl()
        {
            m_EntityListHolder = GetTargetObject<EntityListHolderImpl>();
            m_EntityList = m_EntityListHolder.EntityList as EntityListBase<EntityImpl>;

            m_IsSectionOpen.Clear();
            for (int i = 0; i < m_EntityList.EntityCount; ++i)
            {
                m_IsSectionOpen.Add(false);
            }
        }

        protected override void OnInspectorGUIImpl()
        {
            AddBaseSection();
            ABS_EditorUtils.AddSeparatorLine();
            if (m_State == State.EntityEditMode)
            {
                AddEntityEditorSection();
            }
            else
            {
                AddListSection();
            }
            ABS_EditorUtils.AddSeparatorLine();
            AddButonSection();
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Internal Implementation
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private void AddListSection()
        {
            bool changed = false;
            for (int i = 0; i < m_EntityList.EntityCount; ++i)
            {
                ABS_EditorUtils.BoxStart(m_EditorStyleContainer.DarkBoxStyle);
                {
                    GUILayout.BeginHorizontal();
                    {
                        m_IsSectionOpen[i] = EditorGUILayout.Foldout(m_IsSectionOpen[i], m_EntityList.GetName(i));
                        if (m_State == State.EditMode)
                        {
                            if (IsStatic ())
                            {
                                changed |= AddStaticEditButtons(i);
                            }
                            else
                            {
                                changed |= AddDynamicEditButtons(i);
                            }
                        }
                    }
                    GUILayout.EndHorizontal();

                    if (m_IsSectionOpen[i])
                    {
                        ABS_EditorUtils.Space(10);
                        AddEntityDataSection(i);
                    }
                }
                ABS_EditorUtils.BoxEnd();
            }

            if (!IsStatic())
            {
                changed |= HandleChanges();
            }

            if (changed)
            {
                ABS_EditorUtils.Dirty(target);
            }
        }

        private bool AddStaticEditButtons(int p_Index)
        {
            bool buttonPressed = AddEditorButton(p_Index, "Edit");
            if (buttonPressed)
            {
                m_State = State.EntityEditMode;
                m_EntityIndexForEdit = p_Index;
            }

            return buttonPressed;
        }

        private bool AddDynamicEditButtons(int p_Index)
        {
            bool changed = false;
            bool buttonPressed = false;

            ABS_EditorUtils.HorizontalSpace(10);

            buttonPressed = AddEditorButton(p_Index, "Top");
            if (buttonPressed)
            {
                changed = true;
                m_EntitiesForTop.Add(p_Index);
            }

            buttonPressed = AddEditorButton(p_Index, "Up");
            if (buttonPressed)
            {
                changed = true;
                m_EntitiesForUp.Add(p_Index);
            }

            buttonPressed = AddEditorButton(p_Index, "Down");
            if (buttonPressed)
            {
                changed = true;
                m_EntitiesForDown.Add(p_Index);
            }

            buttonPressed = AddEditorButton(p_Index, "Bottom");
            if (buttonPressed)
            {
                changed = true;
                m_EntitiesForBottom.Add(p_Index);
            }

            ABS_EditorUtils.HorizontalSpace(30);

            buttonPressed = AddEditorButton(p_Index, "Edit");
            if (buttonPressed)
            {
                changed = true;
                m_State = State.EntityEditMode;
                m_EntityIndexForEdit = p_Index;
            }

            ABS_EditorUtils.HorizontalSpace();

            buttonPressed = AddEditorButton(p_Index, "Duplicate", 70);
            if (buttonPressed)
            {
                changed = true;
                m_EntitiesForAdd.Add(p_Index);
            }

            ABS_EditorUtils.HorizontalSpace();

            buttonPressed = AddEditorButton(p_Index, "Delete", 70);
            if (buttonPressed)
            {
                changed = true;
                m_EntitiesForRemove.Add(p_Index);
            }

            return changed;
        }

        private bool HandleChanges()
        {
            bool changed = false;

            foreach (int idx in m_EntitiesForRemove)
            {
                m_EntityList.Remove(idx);
                m_IsSectionOpen.RemoveAt(idx);
                changed = true;
            }
            m_EntitiesForRemove.Clear();

            foreach (int idx in m_EntitiesForAdd)
            {
                m_EntityList.Duplicate(idx);
                m_IsSectionOpen.Add(true);
                changed = true;
            }
            m_EntitiesForAdd.Clear();

            foreach (int idx in m_EntitiesForUp)
            {
                if (m_EntityList.Up(idx))
                {
                    bool open = m_IsSectionOpen[idx];
                    m_IsSectionOpen[idx] = m_IsSectionOpen[idx - 1];
                    m_IsSectionOpen[idx - 1] = open;
                    changed = true;
                }
            }
            m_EntitiesForUp.Clear();

            foreach (int idx in m_EntitiesForTop)
            {
                if (m_EntityList.Top(idx))
                {
                    bool open = m_IsSectionOpen[idx];
                    m_IsSectionOpen.RemoveAt(idx);
                    m_IsSectionOpen.Insert(0, open);
                    changed = true;
                }
            }
            m_EntitiesForTop.Clear();

            foreach (int idx in m_EntitiesForDown)
            {
                if (m_EntityList.Down(idx))
                {
                    bool open = m_IsSectionOpen[idx];
                    m_IsSectionOpen[idx] = m_IsSectionOpen[idx + 1];
                    m_IsSectionOpen[idx + 1] = open;
                    changed = true;
                }
            }
            m_EntitiesForDown.Clear();

            foreach (int idx in m_EntitiesForBottom)
            {
                if (m_EntityList.Bottom(idx))
                {
                    bool open = m_IsSectionOpen[idx];
                    m_IsSectionOpen.RemoveAt(idx);
                    m_IsSectionOpen.Add(open);
                    changed = true;
                }
            }
            m_EntitiesForBottom.Clear();

            return changed;
        }

        protected bool AddEditorButton(int p_Index, string p_ButtonText, int p_Width = 50)
        {
            bool buttonResult = GUILayout.Button(
               p_ButtonText,
               m_EditorStyleContainer.SmallDarkButtonStyle,
               GUILayout.Width(p_Width)
            );
            return buttonResult;
        }

        private void AddButonSection()
        {
            if (m_State == State.EntityEditMode)
            {
                GUILayout.BeginHorizontal();
                {
                    bool buttonResult = GUILayout.Button(
                        "Save",
                        m_EditorStyleContainer.GreenButtonStyle,
                        GUILayout.ExpandWidth(true)
                    );
                    if (buttonResult)
                    {
                        m_State = State.EditMode;
                        ABS_EditorUtils.Dirty(target);
                    }
                }
                GUILayout.EndHorizontal();
            }
            else if (m_State == State.EditMode)
            {
                GUILayout.BeginHorizontal();
                {
                    if (!IsStatic())
                    {
                        bool newEntityButtonResult = GUILayout.Button(
                            "New Entity",
                            m_EditorStyleContainer.DarkButtonStyle,
                            GUILayout.ExpandWidth(true)
                        );
                        if (newEntityButtonResult)
                        {
                            m_EntityList.CreateEntity();
                            m_IsSectionOpen.Add(true);
                            ABS_EditorUtils.Dirty(target);
                        }
                    }

                    bool saveButtonResult = GUILayout.Button(
                        "Save",
                        m_EditorStyleContainer.GreenButtonStyle,
                        GUILayout.ExpandWidth(true)
                    );
                    if (saveButtonResult)
                    {
                        ABS_EditorUtils.Dirty(target);
                        m_State = State.Normal;
                    }
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.BeginHorizontal();
                {
                    bool buttonResult = GUILayout.Button(
                        "Edit",
                        m_EditorStyleContainer.DarkButtonStyle,
                        GUILayout.ExpandWidth(true)
                    );
                    if (buttonResult)
                    {
                        m_State = State.EditMode;
                    }

                }
                GUILayout.EndHorizontal();
            }
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Abstract functions
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        protected abstract void AddBaseSection();
        protected abstract void AddEntityEditorSection();
        protected abstract void AddEntityDataSection(int p_EntityIdx);
        protected abstract bool IsStatic();
    }
}
