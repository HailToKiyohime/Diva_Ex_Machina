//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using UnityEditor;
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem.Editor
{
    internal abstract class ABS_EditorWindowBase : EditorWindow
    {
        protected ABS_EditorStyleContainer m_EditorStyleContainer = null;

        private void OnEnabled()
        {
            if (m_EditorStyleContainer == null)
            {
                m_EditorStyleContainer = ScriptableObject.CreateInstance<ABS_EditorStyleContainer>();
                m_EditorStyleContainer.Init();
            }

            OnGUIImpl();
        }

        private void OnGUI()
        {
            if (m_EditorStyleContainer == null)
            {
                m_EditorStyleContainer = ScriptableObject.CreateInstance<ABS_EditorStyleContainer>();
                m_EditorStyleContainer.Init();
            }
            OnGUIImpl();
        }

        protected abstract void OnGUIImpl();

        protected void AddHeaderSection(in string p_Title)
        {
            if (m_EditorStyleContainer == null)
            {
                m_EditorStyleContainer = ScriptableObject.CreateInstance<ABS_EditorStyleContainer>();
                m_EditorStyleContainer.Init();
            }
            ABS_EditorUtils.AddHeaderSection(m_EditorStyleContainer.HeaderTitleStyle, p_Title, m_EditorStyleContainer.Icon);
        }

        protected void DrawLayerMaskField (GUIContent p_GUIContent, ref LayerMask p_ResultLayerMask, out int[] p_OriginalLayerIndexes)
        {
            //Because of reasons the layermask can not put to the UI on a regular way
            //so we should write out the layers to choose.
            //The problem is that the Layers can be empty but we want to write out only the not empty layers

            //First we should get a list with only the not empty layers
            int notEmpty = 0;
            int[] notEmptyIndexes = new int[32];
            string[] layerNames = new string[32];
            for (int i = 0; i < 32; ++i)
            {
                string layer = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(layer))
                {
                    notEmptyIndexes[notEmpty] = i;
                    layerNames[notEmpty] = layer;
                    ++notEmpty;
                }
            }

            //Create a list with the size of the not empty layers count
            //And fill up with the layers
            //Because of that we modify the indexes we should remember the original indexes so save them to the p_OriginalLayerIndexes
            string[] layerNamesResult = new string[notEmpty];
            p_OriginalLayerIndexes = new int[notEmpty];
            for (int i = 0; i < notEmpty; ++i)
            {
                layerNamesResult[i] = layerNames[i];
                p_OriginalLayerIndexes[i] = notEmptyIndexes[i];
            }

            //Now get the choosen layers from the developer
            p_ResultLayerMask = EditorGUILayout.MaskField(p_GUIContent, p_ResultLayerMask, layerNamesResult);
        }

        protected LayerMask FixLayerMask (in LayerMask p_LayerMaskToFix, in int[] p_OriginalLayerIndexes)
        {
            //Here we should fix the modified layers from the DrawLayerMaskField
            //the p_LayerMaskToFix hold the choosen layers with wrong indexes
            //p_OriginalLayerIndexes is basically the map what mapping the  choosen layer to it's original index
            int result = 0;
            for (int i = 0; i < p_OriginalLayerIndexes.Length; i++)
            {
                int layerIndexInBinary = 1 << i;

                //Check if the layer's bit was set
                if ((p_LayerMaskToFix & layerIndexInBinary) != 0)
                {
                    //Set the Original index on the result
                    result += (1 << (p_OriginalLayerIndexes[i]));
                }
            }
            return result;
        }

        protected bool GetLayerForBuildingElement(out int p_ResultLayer, in int p_LayerIndexes)
        {
            //For GameObject we should change the layer value to a number with the layer index and not it's value
            //We should get the layer index from iT's Binary
            //Also check if more than one layer is set in the parametar value
            p_ResultLayer = 0;
            bool result = false;
            for (int i = 0; i < 32; i++)
            {
                int layerIndexInBinary = 1 << i;
                if ((p_LayerIndexes & layerIndexInBinary) != 0)
                {
                    p_ResultLayer = i;
                    if (result)
                    {
                        return false;
                    }
                    else
                    {
                        result = true;
                    }
                }
            }

            return true;
        }
    }
}